using System;
using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Layers;
using Lingvo.NER.NeuralNetwork.Models;
using Lingvo.NER.NeuralNetwork.Networks;
using Lingvo.NER.NeuralNetwork.Text;
using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork.Applications
{
    /// <summary>
    /// 
    /// </summary>
    public static class Encoder
    {
        private static List< List< string > > InsertCLSToken( List< List< string > > tokens )
        {
            var newTokens = new List< List< string > >( tokens.Count );
            foreach ( var t in tokens )
            {
                var r = new List< string >( t.Count + 1 );
                r.Add( BuildInTokens.CLS );
                r.AddRange( t );

                newTokens.Add( r );
            }
            return (newTokens);
        }

        public static (MultiProcessorNetworkWrapper< IEncoder > encoder, int contextDim) CreateEncoders( Model model, Options options, RoundArray< int > raDeviceIds )
        {
            int contextDim;
            MultiProcessorNetworkWrapper< IEncoder > encoder;
            if ( model.EncoderType == EncoderTypeEnums.BiLSTM )
            {
                encoder = new MultiProcessorNetworkWrapper< IEncoder >(
                    new BiEncoder( "BiLSTMEncoder", model.HiddenDim, model.EncoderEmbeddingDim, model.EncoderLayerDepth, raDeviceIds.GetNextItem(), isTrainable: options.IsEncoderTrainable ), raDeviceIds.ToArray() );

                contextDim = model.HiddenDim * 2;
            }
            else
            {
                encoder = new MultiProcessorNetworkWrapper< IEncoder >(
                    new TransformerEncoder( "TransformerEncoder", model.MultiHeadNum, model.HiddenDim, model.EncoderEmbeddingDim, model.EncoderLayerDepth, options.DropoutRatio, raDeviceIds.GetNextItem(),
                    isTrainable: options.IsEncoderTrainable, learningRateFactor: options.EncoderStartLearningRateFactor ), raDeviceIds.ToArray() );

                contextDim = model.HiddenDim;
            }
            return (encoder, contextDim);
        }

        public static WeightTensor Run( ComputeGraphTensor computeGraph, CorpusBatch corpusBatch, IEncoder encoder, Model model, ShuffleEnums shuffleType,
            WeightTensor srcEmbedding, WeightTensor posEmbedding, WeightTensor segmentEmbedding, List<List<int>> srcSntsIds, float[] originalSrcLengths )
        {
            // Reset networks
            encoder.Reset( computeGraph.GetWeightFactory(), srcSntsIds.Count );

            //Build contextual feature if they exist
            WeightTensor contextTensor = null;
            for ( int i = 1, len = corpusBatch.GetSrcGroupSize(); i < len; i++ )
            {
                var contextCLSOutput = BuildTensorForSourceTokenGroupAt( computeGraph, corpusBatch, shuffleType, encoder, model, srcEmbedding, posEmbedding, segmentEmbedding, i );
                if ( contextTensor == null )
                {
                    contextTensor = contextCLSOutput;
                }
                else
                {
                    contextTensor = computeGraph.Add( contextTensor, contextCLSOutput );
                }
            }

            WeightTensor encOutput = InnerRunner( computeGraph, srcSntsIds, originalSrcLengths, shuffleType, encoder, model, srcEmbedding, posEmbedding, segmentEmbedding, contextTensor );
            return (encOutput);
        }

        public static WeightTensor BuildTensorForSourceTokenGroupAt( ComputeGraphTensor computeGraph, CorpusBatch corpusBatch, ShuffleEnums shuffleType, IEncoder encoder, Model model, WeightTensor srcEmbedding, WeightTensor posEmbedding, WeightTensor segmentEmbedding, int groupId )
        {
            var contextTokens            = InsertCLSToken( corpusBatch.GetSrcTokens( groupId ) );
            var originalSrcContextLength = BuildInTokens.PadSentences( contextTokens );
            var contextTokenIds          = model.SrcVocab.GetWordIndex( contextTokens );

            WeightTensor encContextOutput = InnerRunner( computeGraph, contextTokenIds, originalSrcContextLength, shuffleType, encoder, model, srcEmbedding, posEmbedding, segmentEmbedding );

            var contextPaddedLen = contextTokens[ 0 ].Count;
            var batchSize        = corpusBatch.GetBatchSize();
            var contextCLSIdxs   = new float[ batchSize ];
            for ( int j = 0; j < batchSize; j++ )
            {
                contextCLSIdxs[ j ] = j * contextPaddedLen;
            }

            WeightTensor contextCLSOutput = computeGraph.IndexSelect( encContextOutput, contextCLSIdxs );
            return (contextCLSOutput);
        }

        private static WeightTensor InnerRunner( ComputeGraphTensor computeGraph, List<List<int>> srcTokensList, float[] originalSrcLengths, ShuffleEnums shuffleType, IEncoder encoder, Model model,
           WeightTensor srcEmbedding, WeightTensor posEmbedding, WeightTensor segmentEmbedding, WeightTensor contextEmbeddings = null )
        {
            int batchSize = srcTokensList.Count;
            int srcSeqPaddedLen = srcTokensList[ 0 ].Count;
            WeightTensor srcSelfMask = (shuffleType == ShuffleEnums.NoPaddingInSrc || shuffleType == ShuffleEnums.NoPadding || batchSize == 1) ? null : computeGraph.BuildPadSelfMask( srcSeqPaddedLen, originalSrcLengths ); // The length of source sentences are same in a single mini-batch, so we don't have source mask.

            // Encoding input source sentences
            var encOutput = RunEncoder( computeGraph, srcTokensList, encoder, model, srcEmbedding, srcSelfMask, posEmbedding, segmentEmbedding, contextEmbeddings );
            if ( srcSelfMask != null )
            {
                srcSelfMask.Dispose();
            }
            return (encOutput);
        }

        /// <summary>
        /// Encode source sentences and output encoded weights
        /// </summary>
        private static WeightTensor RunEncoder( ComputeGraphTensor g, List<List<int>> seqs, IEncoder encoder, Model model, WeightTensor embeddings, WeightTensor selfMask, WeightTensor posEmbeddings,
            WeightTensor segmentEmbeddings, WeightTensor contextEmbeddings )
        {
            int batchSize = seqs.Count;
            var inputEmbs = TensorUtils.CreateTokensEmbeddings( seqs, g, embeddings, segmentEmbeddings, contextEmbeddings, model.SrcVocab, model.ApplyContextEmbeddingsToEntireSequence, (float) Math.Sqrt( embeddings.Columns ) );

            if ( model.EncoderType == EncoderTypeEnums.Transformer )
            {
                inputEmbs = PositionEmbedding.AddPositionEmbedding( g, posEmbeddings, batchSize, inputEmbs, 0.0f );
            }

            var encOutput = encoder.Encode( inputEmbs, batchSize, g, selfMask );
            return (encOutput);
        }
    }
}
