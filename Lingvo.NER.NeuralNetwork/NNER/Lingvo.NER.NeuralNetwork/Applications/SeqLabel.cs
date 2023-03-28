using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Lingvo.NER.NeuralNetwork.Applications;
using Lingvo.NER.NeuralNetwork.Layers;
using Lingvo.NER.NeuralNetwork.Models;
using Lingvo.NER.NeuralNetwork.Networks;
using Lingvo.NER.NeuralNetwork.Text;
using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork
{
    /// <summary>
    /// 
    /// </summary>
    public class SeqLabel : BaseSeq2SeqFramework< SeqLabelModel >
    {
        private MultiProcessorNetworkWrapper< WeightTensor >    _SrcEmbedding; //The embeddings over devices for target
        private MultiProcessorNetworkWrapper< IEncoder >         _Encoder; //The encoders over devices. It can be LSTM, BiLSTM or Transformer
        private MultiProcessorNetworkWrapper< FeedForwardLayer > _FFLayer; //The feed forward layers over over devices.
        private MultiProcessorNetworkWrapper< WeightTensor >    _PosEmbedding;

        private readonly ShuffleEnums _ShuffleType;
        private readonly Options _Options;

        /// <summary>
        /// Create4Predict
        /// </summary>
        private SeqLabel( Options opts ) : base( opts )
        {
            if ( !File.Exists( opts.ModelFilePath ) ) throw (new FileNotFoundException( $"Model '{opts.ModelFilePath}' doesn't exist." ));

            _Options     = opts;
            _ShuffleType = opts.ShuffleType;
            _Model       = LoadModel4Predict();

            _Model.ShowModelInfo();
        }
        /// <summary>
        /// Create4Train
        /// </summary>
        private SeqLabel( Options opts, Vocab srcVocab, Vocab tgtVocab ) : base( opts )
        {
            _Options     = opts;
            _ShuffleType = opts.ShuffleType;

            if ( File.Exists( _Options.ModelFilePath ) )
            {
                if ( (srcVocab != null) || (tgtVocab != null) )
                {
                    throw (new ArgumentException( $"Model '{_Options.ModelFilePath}' exists and it includes vocabulary, so input vocabulary must be null." ));
                }

                // Model file exists, so we load it from file.
                _Model = LoadModel4Train();
            }
            else
            {
                // Model doesn't exist, we create it and initlaize parameters
                _Model = new SeqLabelModel( opts.HiddenSize, opts.EmbeddingDim, opts.EncoderLayerDepth, opts.MultiHeadNum, opts.EncoderType, srcVocab, tgtVocab );

                //Initializng weights in encoders and decoders
                CreateTrainParameters( _Model );
            }

            _Model.ShowModelInfo();
        }
        public static SeqLabel Create4Predict( Options opts ) => new SeqLabel( opts );
        public static SeqLabel Create4Train( Options opts, Vocab srcVocab = null, Vocab tgtVocab = null ) => new SeqLabel( opts, srcVocab, tgtVocab );

        private SeqLabelModel LoadModel4Train() => base.LoadModelRoutine< Model_4_ProtoBufSerializer >( CreateTrainParameters, SeqLabelModel.Create );
        private SeqLabelModel LoadModel4Predict() => base.LoadModelRoutine< Model_4_ProtoBufSerializer >( CreatePredictParameters, SeqLabelModel.Create );
        private void CreateTrainParameters( Model model ) => CreateParametersRoutine( model, _Options.MaxTrainSentLength );
        private void CreatePredictParameters( Model model ) => CreateParametersRoutine( model, _Options.MaxPredictSentLength );
        private void CreateParametersRoutine( Model model, int maxSentLength )
        {
            Logger.WriteLine( $"Creating encoders and decoders..." );
            var raDeviceIds = new RoundArray< int >( DeviceIds );

            int contextDim;
            (_Encoder, contextDim) = Encoder.CreateEncoders( model, _Options, raDeviceIds );
            _FFLayer = new MultiProcessorNetworkWrapper< FeedForwardLayer >( new FeedForwardLayer( "FeedForward", contextDim, model.ClsVocab.Count, dropoutRatio: 0.0f, deviceId: raDeviceIds.GetNextItem(), isTrainable: true ), DeviceIds );

            _SrcEmbedding = new MultiProcessorNetworkWrapper< WeightTensor >( new WeightTensor( new long[ 2 ] { model.SrcVocab.Count, model.EncoderEmbeddingDim }, raDeviceIds.GetNextItem(), normType: NormType.Uniform, name: "SrcEmbeddings", isTrainable: true ), DeviceIds );

            if ( model.EncoderType == EncoderTypeEnums.Transformer )
            {
                var row    = maxSentLength + 2;
                var column = model.EncoderEmbeddingDim;
                _PosEmbedding = new MultiProcessorNetworkWrapper<WeightTensor>( PositionEmbedding.BuildPositionWeightTensor( row, column, raDeviceIds.GetNextItem(), "PosEmbedding", false ), DeviceIds, true );
            }
            else
            {
                _PosEmbedding = null;
            }
        }

        /// <summary>
        /// Get networks on specific devices
        /// </summary>
        private (IEncoder encoder, WeightTensor srcEmbedding, WeightTensor posEmbedding, FeedForwardLayer decoderFFLayer) GetNetworksOnDeviceAt( int deviceIdIdx ) 
            => (_Encoder.GetNetworkOnDevice( deviceIdIdx ), _SrcEmbedding.GetNetworkOnDevice( deviceIdIdx ), _PosEmbedding?.GetNetworkOnDevice( deviceIdIdx ), _FFLayer.GetNetworkOnDevice( deviceIdIdx ));

        /// <summary>
        /// Run forward part on given single device
        /// </summary>
        /// <param name="g">The computing graph for current device. It gets created and passed by the framework</param>
        /// <param name="srcSnts">A batch of input tokenized sentences in source side</param>
        /// <param name="tgtSnts">A batch of output tokenized sentences in target side. In training mode, it inputs target tokens, otherwise, it outputs target tokens generated by decoder</param>
        /// <param name="deviceIdIdx">The index of current device</param>
        /// <returns>The cost of forward part</returns>
        public override NetworkResult RunForwardOnSingleDevice( ComputeGraphTensor g, CorpusBatch corpusBatch, int deviceIdIdx, bool isTraining, bool returnWordClassInfos = false )
        {
            var srcSnts = corpusBatch.GetSrcTokens( 0 );
            var tgtSnts = corpusBatch.GetTgtTokens( 0 );

            (IEncoder encoder, WeightTensor srcEmbedding, WeightTensor posEmbedding, FeedForwardLayer decoderFFLayer) = GetNetworksOnDeviceAt( deviceIdIdx );

            var srcVocab = _Model.SrcVocab;
            var clsVocab = _Model.ClsVocab;

            // Reset networks
            encoder.Reset( g.GetWeightFactory(), srcSnts.Count );

            var originalSrcLengths = BuildInTokens.PadSentences( srcSnts );
            var srcTokensList = srcVocab.GetWordIndex( srcSnts );

            BuildInTokens.PadSentences_2( tgtSnts ); //---BuildInTokens.PadSentences( tgtSnts );
            int seqLen    = srcSnts[ 0 ].Count;
            int batchSize = srcSnts.Count;

            // Encoding input source sentences
            WeightTensor encOutput = Encoder.Run( g, corpusBatch, encoder, _Model, _ShuffleType, srcEmbedding, posEmbedding, null, srcTokensList, originalSrcLengths );
            WeightTensor ffLayer   = decoderFFLayer.Process( encOutput, batchSize, g );

            float cost = 0.0f;
            var output_2 = default(List< List< NetworkResult.ClassesInfo > >);            
            using ( WeightTensor probs = g.Softmax( ffLayer, runGradients: false, inPlace: true ) )
            {
                if ( isTraining )
                {
                    var indices = new long[ 2 ];

                    //Calculate loss for each word in the batch
                    for ( int k = 0; k < batchSize; k++ )
                    {
                        if ( tgtSnts.Count <= k )
                        {
                            throw (new IndexOutOfRangeException( $"Sequence #'{k}' is out of range in target sequences (size '{tgtSnts.Count})'. Source sequences batch size is '{srcSnts.Count}'" ));
                        }

                        var tgtSnts__k = tgtSnts[ k ];
                        for ( int j = 0; j < seqLen; j++ )
                        {
                            if ( tgtSnts__k.Count <= j )
                            {
                                var srcSnts__k = srcSnts[ k ];
                                throw (new IndexOutOfRangeException( $"Token offset '{j}' is out of range in current target sequence (size = '{tgtSnts__k.Count}' text = '{string.Join( ' ', tgtSnts__k )}'). Source sequence size is '{srcSnts__k.Count}' text is {string.Join( ' ', srcSnts__k )}" ));
                            }

                            int ix_targets_k_j = clsVocab.GetWordIndex( tgtSnts__k[ j ] );
                            indices[ 0 ] = k * seqLen + j;
                            indices[ 1 ] = ix_targets_k_j;
                            float score_k = probs.GetWeightAt( indices/*new long[] { k * seqLen + j, ix_targets_k_j }*/ );
                            cost += (float) -Math.Log( score_k );

                            probs.SetWeightAt( score_k - 1, indices/*new long[] { k * seqLen + j, ix_targets_k_j }*/ );
                        }
                    }

                    ffLayer.CopyWeightsToGradients( probs );
                }
                else
                {
                    // Output "i"th target word
                    using var targetIdxTensor = g.Argmax( probs, 1 );
                    float[] targetIdx = targetIdxTensor.ToWeightArray();
                    List< string > targetWords = clsVocab.ConvertIdsToString( targetIdx );

                    if ( (batchSize == 1) && returnWordClassInfos )
                    {
                        var probs_array  = probs.ToWeightArray();
                        var output_2_lst = new List< NetworkResult.ClassesInfo >( batchSize );
                        output_2 = new List< List< NetworkResult.ClassesInfo > >{ output_2_lst };

                        Debug.Assert( probs.Columns == clsVocab.Count );
                        Debug.Assert( probs.Rows    == targetWords.Count );

                        tgtSnts[ 0 ] = targetWords;

                        var srcSnt = srcSnts[ 0 ];
                        var cols = probs.Columns;
                        var wordCount = 0;
                        var inputBatchCount = srcSnt.Count;
                        var wordClasses = new List< NetworkResult.WordClassInfo >( inputBatchCount );
                        for ( int i = 0, len = inputBatchCount; i < len; i++ )
                        {
                            var word = srcSnt[ i ];

                            #region [.wordsInDictRatio.]
                            if ( BuildInTokens.IsPreDefinedToken( word ) )
                            {
                                inputBatchCount--;
                            }
                            else if ( srcVocab.ContainsWord( word ) )
                            {
                                wordCount++;
                            }
                            #endregion

                            var classes = new List< NetworkResult.ClassInfo >( cols - Vocab.START_MEANING_INDEX );                            
                            for ( int c = Vocab.START_MEANING_INDEX; c < cols; c++ )
                            {
                                var className = clsVocab.ConvertIdsToString( c );
                                var idx = i * cols + c;
                                var prob = probs_array[ idx ];
                                classes.Add( new NetworkResult.ClassInfo() { ClassName = className, Probability = prob } );
                            }
                            classes.Sort( NetworkResult.ClassInfo.ComparerByProbability.Inst );
                            wordClasses.Add( new NetworkResult.WordClassInfo() { Word = word, Classes = classes } );
                        }
                        var wordsInDictRatio = (0 < inputBatchCount) ? ((1.0f * wordCount) / inputBatchCount) : 0;                       
                        output_2_lst.Add( new NetworkResult.ClassesInfo() { WordClasses = wordClasses, WordsInDictRatio = wordsInDictRatio } );
                    }
                    else
                    {
                        for ( int k = 0; k < batchSize; k++ )
                        {
                            tgtSnts[ k ] = targetWords.GetRange( k * seqLen, seqLen );
                        }
                    }
                }
            }

            var nr = new NetworkResult()
            {
                Cost     = cost,
                Output   = new List< List< List< string > > >(),
                Output_2 = output_2,
            };
            nr.Output.Add( tgtSnts );
            return (nr);
        }

        //--------------------------------------------------------------------//
        private static Predicate< string > _BuildInTokens_IsPreDefinedTokenPredicate = new Predicate< string >( s => string.IsNullOrEmpty( s ) || BuildInTokens.IsPreDefinedToken( s ) );
        private static Predicate< NetworkResult.WordClassInfo > _BuildInTokens_IsPreDefinedTokenPredicate_2 = new Predicate< NetworkResult.WordClassInfo >( t => string.IsNullOrEmpty( t.Word ) || BuildInTokens.IsPreDefinedToken( t.Word ) );
        public (List< string > labelTokens, NetworkResult.ClassesInfo classesInfos) Predict( List< string > inputTokens, bool returnWordClassInfos = false )
        {
            Debug.Assert( inputTokens.Count <= _Options.MaxPredictSentLength );

            var batch = new CorpusBatch( inputTokens );
            var nr = RunPredictRoutine( batch, returnWordClassInfos );
            var labelTokens = nr.Output[ 0 ][ 0 ];
                labelTokens.RemoveAll( _BuildInTokens_IsPreDefinedTokenPredicate );
            var classesInfos = (nr.Output_2 != null) ? nr.Output_2[ 0 ][ 0 ] : default;
                classesInfos.WordClasses?.RemoveAll( _BuildInTokens_IsPreDefinedTokenPredicate_2 );
            return (labelTokens, classesInfos);
        }
        private (List< string > labelTokens, NetworkResult.ClassesInfo classesInfos) Predict_Internal( List< string > inputTokens, bool returnWordClassInfos )
        {
            Debug.Assert( inputTokens.Count <= _Options.MaxPredictSentLength );

            var batch = new CorpusBatch( inputTokens );
            var nr = RunPredictRoutine( batch, returnWordClassInfos );
            var labelTokens  = nr.Output[ 0 ][ 0 ];
            var classesInfos = (nr.Output_2 != null) ? nr.Output_2[ 0 ][ 0 ] : default;
            return (labelTokens, classesInfos);
        }

        public (List< string > labelTokens, NetworkResult.ClassesInfo classesInfos) Predict_Full( List< string > inputTokens, int? maxPredictSentLength = null, float cutDropout = 0.1f, bool returnWordClassInfos = false )
        {
            var maxPredictSentLen = Math.Min( _Options.MaxPredictSentLength, maxPredictSentLength.GetValueOrDefault( _Options.MaxPredictSentLength ) );

            var d = inputTokens.Count - maxPredictSentLen;
            if ( 0 < d )
            {
                if ( (maxPredictSentLen * cutDropout) < d )
                {
                    return (Predict_Full_Routine( inputTokens, maxPredictSentLen, returnWordClassInfos ));
                }
                else
                {
                    inputTokens.RemoveRange( maxPredictSentLen/*inputTokens.Count - d*/, d );
                }
            }

            return (Predict( inputTokens, returnWordClassInfos ));
        }
        private (List< string > labelTokens, NetworkResult.ClassesInfo classesInfos) Predict_Full_Routine( List< string > inputTokens, int maxPredictSentLength, bool returnWordClassInfos )
        {
            var partCount = Math.DivRem( inputTokens.Count, maxPredictSentLength, out var rem );
            var labelTokens  = new List< string >( inputTokens.Count );
            var wordClasses = returnWordClassInfos ? new List< NetworkResult.WordClassInfo >() : default;
            var wordsInDictRatios = returnWordClassInfos ? new List< float >() : default;
            for ( var i = 0; i < partCount; i++ )
            {
                var words_part = inputTokens.GetRange( i * maxPredictSentLength, maxPredictSentLength );
                var (tt, ci) = Predict_Internal( words_part, returnWordClassInfos );
                labelTokens.AddRange( tt );
                if ( returnWordClassInfos )
                {
                    wordClasses.AddRange( ci.WordClasses );
                    wordsInDictRatios.Add( ci.WordsInDictRatio );
                }
            }
            if ( rem != 0 )
            {
                var words_part = inputTokens.GetRange( partCount * maxPredictSentLength, rem );
                var (tt, ci) = Predict_Internal( words_part, returnWordClassInfos );
                labelTokens.AddRange( tt );
                if ( returnWordClassInfos )
                {
                    wordClasses.AddRange( ci.WordClasses );
                    wordsInDictRatios.Add( ci.WordsInDictRatio );
                }
            }

            labelTokens.RemoveAll( _BuildInTokens_IsPreDefinedTokenPredicate );
            wordClasses?.RemoveAll( _BuildInTokens_IsPreDefinedTokenPredicate_2 );
            var classesInfos = returnWordClassInfos ? new NetworkResult.ClassesInfo() { WordClasses = wordClasses, WordsInDictRatio = wordsInDictRatios.Sum() / wordsInDictRatios.Count } : default;
            return (labelTokens, classesInfos);
        }
    }
}
