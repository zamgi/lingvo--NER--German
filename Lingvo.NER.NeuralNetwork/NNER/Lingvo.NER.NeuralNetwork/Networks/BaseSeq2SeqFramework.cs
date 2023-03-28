﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Lingvo.NER.NeuralNetwork.Applications;
using Lingvo.NER.NeuralNetwork.Metrics;
using Lingvo.NER.NeuralNetwork.Models;
using Lingvo.NER.NeuralNetwork.Optimizer;
using Lingvo.NER.NeuralNetwork.Text;
using Lingvo.NER.NeuralNetwork.Utils;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.NeuralNetwork.Networks
{
    /// <summary>
    /// 
    /// </summary>
    public class NetworkResult
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly struct ClassInfo
        {
            public sealed class ComparerByProbability : IComparer< ClassInfo >
            {
                public static ComparerByProbability Inst { get; } = new ComparerByProbability();
                private ComparerByProbability() { }

                public int Compare( ClassInfo x, ClassInfo y )
                {
                    var d = y.Probability.CompareTo( x.Probability );
                    if ( d == 0 )
                        d = string.Compare( x.ClassName, y.ClassName );
                    return (d);
                }
            }

            public string ClassName   { get; init; }
            public double Probability { get; init; }
            public override string ToString() => $"('{ClassName}', {Probability:F4})";
        }
        /// <summary>
        /// 
        /// </summary>
        public readonly struct WordClassInfo
        {
            public string Word { get; init; }
            public List< ClassInfo > Classes { get; init; }
            public override string ToString() => $"'{Word}', ({string.Join( ", ", Classes )}";
        }
        /// <summary>
        /// 
        /// </summary>
        public readonly struct ClassesInfo
        {
            public List< WordClassInfo > WordClasses { get; init; }
            public float WordsInDictRatio { get; init; }
            public override string ToString() => $"({string.Join( ", ", WordClasses )}, WordsInDictRatio: {WordsInDictRatio})";
        }

        public float Cost;
        public List<List<List<string>>> Output; // (beam_size, batch_size, seq_len)
        public List<List<ClassesInfo>> Output_2;

        public NetworkResult() { }

        public void RemoveDuplicatedEOS()
        {
            if ( Output != null )
            {
                foreach ( var item in Output )
                {
                    RemoveDuplicatedEOS( item );
                }
            }
        }
        private static void RemoveDuplicatedEOS( List<List<string>> snts )
        {
            foreach ( var snt in snts )
            {
                for ( int i = 0; i < snt.Count; i++ )
                {
                    if ( snt[ i ] == BuildInTokens.EOS )
                    {
                        snt.RemoveRange( i, snt.Count - i );
                        snt.Add( BuildInTokens.EOS );
                        break;
                    }
                }
            }
        }

        public void AppendResult( NetworkResult nr )
        {
            var cnt = nr.Output.Count;
            while ( this.Output.Count < cnt )
            {
                this.Output.Add( new List< List< string > >() );
            }

            for ( var i /*beamIdx*/ = 0; i < cnt; i++ )
            {
                var output_i    = this.Output[ i ];
                var nr_output_i = nr.Output[ i ];
                var cnt_i       = nr_output_i.Count;
                for ( var j /*batchIdx*/ = 0; j < cnt_i; j++ )
                {
                    output_i.Add( nr_output_i[ j ] );
                }
            }

            if ( this.Output_2 == null )
            {
                this.Output_2 = nr.Output_2;
            }
            else if ( nr.Output_2 != null )
            {
                this.Output_2.AddRange( nr.Output_2 );
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public delegate Validator.Result ExternalValidateDelegate( CancellationToken ct );

    /// <summary>
    /// This is a framework for neural network training. It includes many core parts, such as backward propagation, parameters updates, 
    /// memory management, computing graph managment, corpus shuffle & batching, I/O for model, logging & monitoring, checkpoints.
    /// You need to create your network inherited from this class, implmenet forward part only and pass it to TrainOneEpoch method for training
    /// </summary>
    public abstract class BaseSeq2SeqFramework< T > where T : Model
    {
        public event EventHandler< CostEventArg >       StatusUpdateWatcher;
        public event EventHandler< EvaluationEventArg > EvaluationWatcher;
        public event EventHandler< CostEventArg >       EpochEndWatcher;

        private readonly int[] _DeviceIds;
        internal T _Model;

        private readonly float  _RegStrength                      = 1e-10f; // L2 regularization strength
        private readonly float  _ValidIntervalHours               = 1.0f;
        private readonly int    _PrimaryTaskId                    = 0;
        private DateTime        _LastCheckPointDateTime           = DateTime.Now;
        private int             _UpdateFreq                       = 1;
        private double          _AvgCostPerWordInTotalInLastEpoch = 10000.0;
        private SortedList< string, IMultiProcessorNetworkWrapper > _Name2Network;
        private string  _ModelFilePath;
        private int     _WeightsUpdateCount;
        private Options _Options;

        public int[] DeviceIds => _DeviceIds;

        protected BaseSeq2SeqFramework( Options opts )
        {
            _Options       = opts ?? throw (new ArgumentNullException( nameof(opts) ));
            _ModelFilePath = opts.ModelFilePath;

            _DeviceIds = (!opts.DeviceIds.IsNullOrWhiteSpace() ? opts.DeviceIds.Split( ',' ).Select( x => int.Parse( x ) ).ToArray() : new[] { 0 });

            var cudaCompilerOptions = (opts.CompilerOptions.IsNullOrEmpty() ? null : opts.CompilerOptions.Split( ' ', StringSplitOptions.RemoveEmptyEntries ));
            TensorAllocator.InitDevices( opts.ProcessorType, _DeviceIds, opts.MemoryUsageRatio, cudaCompilerOptions );

            _ValidIntervalHours = opts.ValidIntervalHours;
            _UpdateFreq         = opts.UpdateFreq;
            _PrimaryTaskId      = 0;
        }

        public abstract NetworkResult RunForwardOnSingleDevice( ComputeGraphTensor computeGraph, CorpusBatch corpusBatch, int deviceIdIdx, bool isTraining, bool returnWordClassInfos = false );

        public ComputeGraphTensor CreateComputGraph( int deviceIdIdx, bool needBack = true )
        {
            if ( (deviceIdIdx < 0) || (DeviceIds.Length <= deviceIdIdx) )
            {
                throw (new ArgumentOutOfRangeException( $"Index '{deviceIdIdx}' is out of deviceId range. DeviceId length is '{DeviceIds.Length}'" ));
            }

            // Create computing graph instance and return it
            return (new ComputeGraphTensor( DeviceIds[ deviceIdIdx ], needBack ));
        }

        public bool SaveModel( bool createBackupPrevious = false, string suffix = "" ) => SaveModelImpl( _Model, createBackupPrevious, suffix );
        protected virtual bool SaveModelImpl( T model, bool createBackupPrevious = false, string suffix = "" ) => SaveModelRoutine( model, Model_4_ProtoBufSerializer.Create, createBackupPrevious, suffix );
        protected bool SaveModelRoutine< ProtoBuf_T >( T model, Func< T, ProtoBuf_T > createModel4SerializeFunc, bool createBackupPrevious = false, string suffix = "" )
        {
            var modelFilePath = _ModelFilePath + suffix;
            var fn = Path.GetFullPath( modelFilePath );
            var dir = Path.GetDirectoryName( fn ); if ( !Directory.Exists( dir ) ) Directory.CreateDirectory( dir );
            try
            {
                Logger.WriteLine( $"Saving model to '{fn}'" );

                if ( createBackupPrevious && File.Exists( fn ) )
                {
                    File.Copy( fn, $"{fn}.bak", true );
                }

                using ( var fs = new FileStream( modelFilePath, FileMode.Create, FileAccess.Write ) )
                {
                    SaveParameters( model );

                    var model_4_serialize = createModel4SerializeFunc( model );
                    ProtoBuf.Serializer.Serialize( fs, model_4_serialize );
                }

                model.ClearWeights();

                return (true);
            }
            catch ( Exception ex )
            {
                Logger.WriteWarnLine( $"Failed to save model to file. Exception = '{ex.Message}', Call stack = '{ex.StackTrace}'" );
                return (false);
            }
        }
        protected T LoadModelRoutine< ProtoBuf_T >( Action< T > initializeParametersFunc, Func< ProtoBuf_T, T > createModelFunc )
        {
            Logger.WriteLine( $"Loading model from '{_ModelFilePath}'..." );
            T model = default;

            using ( var fs = new FileStream( _ModelFilePath, FileMode.Open, FileAccess.Read ) )
            {
                var model_4_serialize = ProtoBuf.Serializer.Deserialize<ProtoBuf_T>( fs );
                model = createModelFunc( model_4_serialize );

                //Initialize parameters on devices
                initializeParametersFunc( model );

                // Load embedding and weights from given model
                // All networks and tensors which are MultiProcessorNetworkWrapper<T> will be loaded from given stream
                LoadParameters( model );
            }

            //For multi-GPUs, copying weights from default device to other all devices
            CopyWeightsFromDefaultDeviceToAllOtherDevices();

            model.ClearWeights();

            return (model);
        }

        public void Train( int maxTrainingEpoch, Corpus trainCorpus, Corpus validCorpus, ILearningRate learningRate, IList< IMetric > metrics, IOptimizer optimizer
            , CancellationToken cancellationToken, ExternalValidateDelegate externalValidateRoutine )
            => Train( maxTrainingEpoch, trainCorpus, validCorpus, learningRate, new Dictionary< int, IList< IMetric > > { { 0, metrics } }, optimizer, cancellationToken, externalValidateRoutine );
        private void Train( int maxTrainingEpoch, Corpus trainCorpus, Corpus validCorpus, ILearningRate learningRate, Dictionary< int, IList< IMetric > > taskId2metrics, IOptimizer optimizer
            , CancellationToken cancellationToken, ExternalValidateDelegate externalValidateRoutine )
        {
            Logger.WriteLine( "Start to train..." );
            var wasRunValidateAndSaveModel = false;
            for ( var i = 0; (i < maxTrainingEpoch) && !cancellationToken.IsCancellationRequested; i++ )
            {
                // Train one epoch over given devices. Forward part is implemented in RunForwardOnSingleDevice function in below, 
                // backward, weights updates and other parts are implemented in the framework. You can see them in BaseSeq2SeqFramework.cs
                wasRunValidateAndSaveModel |= TrainOneEpoch( i, trainCorpus, validCorpus, learningRate, optimizer, taskId2metrics, externalValidateRoutine, cancellationToken );
            }

            if ( !cancellationToken.IsCancellationRequested )
            {
                if ( !File.Exists( _ModelFilePath ) )
                {
                    SaveModel( createBackupPrevious: true );
                }
                else if ( !wasRunValidateAndSaveModel )
                {
                    RunValidateAndSaveModel( validCorpus, taskId2metrics, avgCostPerWordInTotal: default, externalValidateRoutine, cancellationToken );
                }
            }
        }

        private bool TrainOneEpoch( int epochNum, Corpus trainCorpus, Corpus validCorpus, ILearningRate learningRate, IOptimizer optimizer
            , Dictionary< int, IList< IMetric > > taskId2metrics, ExternalValidateDelegate externalValidateRoutine, CancellationToken ct )
        {
            var    processedLineInTotal  = 0;
            var    startDateTime         = DateTime.Now;
            double costInTotal           = 0.0;
            long   srcWordCntsInTotal    = 0;
            long   tgtWordCntsInTotal    = 0;
            double avgCostPerWordInTotal = 0.0;
            int    updatesInOneEpoch     = 0;
            float  learningRateVal       = 0.0f;
            var    wasRunValidateAndSaveModel = false;

            Logger.WriteLine( $"Start to process training corpus." );
            var corpusBatchs = new List< CorpusBatch >();
            var lastCallStatusUpdateWatcherDateTime = DateTime.Now;

            foreach ( var batch in trainCorpus.GetSntPairBatchs() )
            {
                corpusBatchs.Add( batch );
                if ( corpusBatchs.Count == (_DeviceIds.Length * _UpdateFreq) )
                {
                    // Copy weights from weights kept in default device to all other devices
                    CopyWeightsFromDefaultDeviceToAllOtherDevices();

                    var batchSplitFactor = 1;
                    for ( var runNetwordSuccssed = false; !runNetwordSuccssed; )
                    {
                        try
                        {
                            (float cost, int srcWordCnt, int tgtWordCnt, int processedLine) = RunNetwork( corpusBatchs, batchSplitFactor );
                            processedLineInTotal += processedLine;
                            srcWordCntsInTotal += srcWordCnt;
                            tgtWordCntsInTotal += tgtWordCnt;

                            //Sum up gradients in all devices, and kept it in default device for parameters optmization
                            SumGradientsToTensorsInDefaultDevice();

                            //Optmize parameters
                            learningRateVal = learningRate.GetCurrentLearningRate();
                            List<WeightTensor> models = GetParametersFromDefaultDevice();

                            _WeightsUpdateCount++;
                            optimizer.UpdateWeights( models, processedLine, learningRateVal, _RegStrength, _WeightsUpdateCount );

                            costInTotal += cost;
                            updatesInOneEpoch++;
                            avgCostPerWordInTotal = costInTotal / updatesInOneEpoch;
                            if ( ((_WeightsUpdateCount % 100) == 0) || (1 < (DateTime.Now - lastCallStatusUpdateWatcherDateTime).TotalMinutes) )
                            {
                                StatusUpdateWatcher?.Invoke( this, new CostEventArg()
                                {
                                    LearningRate              = learningRateVal,
                                    AvgCostInTotal            = avgCostPerWordInTotal,
                                    Epoch                     = epochNum,
                                    Update                    = _WeightsUpdateCount,
                                    ProcessedSentencesInTotal = processedLineInTotal,
                                    ProcessedWordsInTotal     = srcWordCntsInTotal + tgtWordCntsInTotal,
                                    StartDateTime             = startDateTime,
                                    LastCallStatusUpdateWatcherDateTime = lastCallStatusUpdateWatcherDateTime,
                                });
                                lastCallStatusUpdateWatcherDateTime = DateTime.Now;
                            }

                            runNetwordSuccssed = true;
                        }
                        catch ( AggregateException ex )
                        {
                            if ( ex.InnerExceptions != null )
                            {
                                var oomMessage            = default(string);
                                var isOutOfMemException   = false;
                                var isArithmeticException = false;
                                foreach ( var excep in ex.InnerExceptions )
                                {
                                    if ( excep is OutOfMemoryException )
                                    {
                                        GC.Collect();
                                        isOutOfMemException = true;
                                        oomMessage = excep.Message;
                                        break;
                                    }
                                    else if ( excep is ArithmeticException )
                                    {
                                        isArithmeticException = true;
                                        oomMessage = excep.Message;
                                        break;
                                    }
                                }

                                if ( isOutOfMemException )
                                {
                                    if ( !TryToSplitBatchFactor( corpusBatchs, batchSplitFactor, oomMessage, out batchSplitFactor ) )
                                    {
                                        break;
                                    }
                                }
                                else if ( isArithmeticException )
                                {
                                    Logger.WriteLine( $"Arithmetic exception: '{ex.Message}'" );
                                    break;
                                }
                                else
                                {
                                    Logger.WriteErrorLine( $"Exception: {ex.Message}, Call stack: {ex.StackTrace}" );
                                    throw;
                                }
                            }
                            else
                            {
                                Logger.WriteErrorLine( $"Exception: {ex.Message}, Call stack: {ex.StackTrace}" );
                                throw;
                            }

                        }
                        catch ( OutOfMemoryException ex )
                        {
                            GC.Collect();
                            if ( !TryToSplitBatchFactor( corpusBatchs, batchSplitFactor, ex.Message, out batchSplitFactor ) )
                            {
                                break;
                            }
                        }
                        catch ( ArithmeticException ex )
                        {
                            Logger.WriteLine( $"Arithmetic exception: '{ex.Message}'" );
                            break;
                        }
                        catch ( Exception ex )
                        {
                            Logger.WriteErrorLine( $"Exception: {ex.Message}, Call stack: {ex.StackTrace}" );
                            throw;
                        }
                    }

                    // Evaluate model every hour and save it if we could get a better one.
                    var ts = DateTime.Now - _LastCheckPointDateTime;
                    if ( _ValidIntervalHours < ts.TotalHours )
                    {
                        RunValidateAndSaveModel( validCorpus, taskId2metrics, avgCostPerWordInTotal, externalValidateRoutine, ct );
                        _LastCheckPointDateTime = DateTime.Now;
                        wasRunValidateAndSaveModel = true;
                    }

                    corpusBatchs.Clear();
                }
            }

            Logger.WriteInfoLine( ConsoleColor.Green, $"Epoch '{epochNum}' took '{DateTime.Now - startDateTime}' time to finish. AvgCost = {avgCostPerWordInTotal:F6}, AvgCostInLastEpoch = {_AvgCostPerWordInTotalInLastEpoch:F6}" );
            _AvgCostPerWordInTotalInLastEpoch = avgCostPerWordInTotal;

            EpochEndWatcher?.Invoke( this, new CostEventArg()
            {
                LearningRate              = learningRateVal,
                AvgCostInTotal            = avgCostPerWordInTotal,
                Epoch                     = epochNum,
                Update                    = _WeightsUpdateCount,
                ProcessedSentencesInTotal = processedLineInTotal,
                ProcessedWordsInTotal     = srcWordCntsInTotal + tgtWordCntsInTotal,
                StartDateTime             = startDateTime
            });

            return (wasRunValidateAndSaveModel);
        }

        private static bool TryToSplitBatchFactor( List< CorpusBatch > corpusBatchs, int batchSplitFactor, string message, out int newBatchSplitFactor )
        {
            var maxBatchSize  = 0;
            var maxTokenCount = 0;
            foreach ( var batch in corpusBatchs )
            {
                var tokenCount = batch.SrcTokenCount + batch.TgtTokenCount;
                if ( maxTokenCount < tokenCount ) maxTokenCount = tokenCount;
                var batchSize = batch.GetBatchSize();
                if ( maxBatchSize < batchSize ) maxBatchSize = batchSize;
            }

            newBatchSplitFactor = batchSplitFactor * 2;
            Logger.WriteLine( $" {message} Retrying with batch split factor '{newBatchSplitFactor}'. Max batch size '{maxBatchSize}' (max batch size will be '{maxBatchSize / newBatchSplitFactor}'), Max token size '{maxTokenCount}'" );

            var success = (newBatchSplitFactor < maxBatchSize);
            if ( !success )
            {
                Logger.WriteLine( $"Batch split factor is larger than batch size, so ignore current mini-batch." );
            }
            return (success);
        }

        private (float cost, int srcWordCnt, int tgtWordCnt, int processedLine) RunNetwork( List< CorpusBatch > corpusBatchs, int batchSplitFactor )
        {
            float cost          = 0.0f;
            var   processedLine = 0;
            var   srcWordCnts   = 0;
            var   tgtWordCnts   = 0;

            //Clear gradient over all devices
            ZeroGradientOnAllDevices();

            var currBatchIdx = -1;

            // Run forward and backward on all available processors
            var local_lock = new object();
            Parallel.For( 0, _DeviceIds.Length, /*new ParallelOptions() { MaxDegreeOfParallelism = _DeviceIds.Length },*/ i =>
            {
                for ( var localCurrBatchIdx = Interlocked.Increment( ref currBatchIdx );
                          localCurrBatchIdx < corpusBatchs.Count;
                          localCurrBatchIdx = Interlocked.Increment( ref currBatchIdx ) )
                {
                    try
                    {
                        var sntPairBatch_i = corpusBatchs[ localCurrBatchIdx ];
                        var batchSize      = sntPairBatch_i.GetBatchSize();
                        var batchSegSize   = Math.DivRem( batchSize, batchSplitFactor, out var remainBatchSegSize );
                        if ( 0 < batchSegSize )
                        {
                            for ( int k = 0; k < batchSplitFactor; k++ )
                            {
                                var batch = sntPairBatch_i.GetRange( k * batchSegSize, batchSegSize );

                                NetworkResult nr;
                                // Create a new computing graph instance
                                using ( ComputeGraphTensor computeGraph_i = CreateComputGraph( i ) )
                                {
                                    // Run forward part
                                    nr = RunForwardOnSingleDevice( computeGraph_i, batch, i, isTraining: true );
                                    // Run backward part and compute gradients
                                    computeGraph_i.Backward();
                                }

                                lock ( local_lock )
                                {
                                    cost          += nr.Cost;
                                    srcWordCnts   += sntPairBatch_i.SrcTokenCount;
                                    tgtWordCnts   += sntPairBatch_i.TgtTokenCount;
                                    processedLine += batchSegSize;
                                }
                            }
                        }

                        if ( 0 < remainBatchSegSize )
                        {
                            var batch = sntPairBatch_i.GetRange( batchSize - remainBatchSegSize, remainBatchSegSize );

                            NetworkResult nr;
                            // Create a new computing graph instance
                            using ( ComputeGraphTensor computeGraph_i = CreateComputGraph( i ) )
                            {
                                // Run forward part
                                nr = RunForwardOnSingleDevice( computeGraph_i, batch, i, isTraining: true );
                                // Run backward part and compute gradients
                                computeGraph_i.Backward();
                            }

                            lock ( local_lock )
                            {
                                cost          += nr.Cost;
                                srcWordCnts   += sntPairBatch_i.SrcTokenCount;
                                tgtWordCnts   += sntPairBatch_i.TgtTokenCount;
                                processedLine += batchSegSize;
                            }
                        }
                    }
                    catch ( OutOfMemoryException ex )
                    {
                        Debug.WriteLine( ex );
                        GC.Collect();
                        throw;
                    }
                    catch ( Exception ex )
                    {
                        Logger.WriteErrorLine( $"Exception: '{ex.Message}', Call stack: '{ex.StackTrace}'" );
                        throw;
                    }
                }
            });

            return (cost / processedLine, srcWordCnts, tgtWordCnts, processedLine);
        }

        private static NetworkResult MergeResults( SortedDictionary< int, NetworkResult > batchId2Results )
        {
            var res = new NetworkResult() { Output = new List<List<List<string>>>() };
            foreach ( var nr in batchId2Results.Values )
            {
                res.AppendResult( nr );
            }
            return (res);
        }

        protected NetworkResult RunPredictRoutine( CorpusBatch corpusBatch, bool returnWordClassInfos = false )
        {
#if DEBUG
            if ( corpusBatch == null ) throw (new ArgumentNullException( nameof(corpusBatch) ));
#endif
            try
            {
                var batchId2Result = new SortedDictionary< int, NetworkResult >();
                var dataSizePerGPU = Math.DivRem( corpusBatch.GetBatchSize(), _DeviceIds.Length, out var dataSizePerGPUMod );

                if ( 0 < dataSizePerGPU )
                {
                    Parallel.For( 0, _DeviceIds.Length, gpuIdx =>
                    {
                         try
                         {
                             var batch = corpusBatch.GetRange( gpuIdx * dataSizePerGPU, dataSizePerGPU );

                             NetworkResult nr;
                             // Create a new computing graph instance
                             using ( ComputeGraphTensor computeGraph = CreateComputGraph( gpuIdx, needBack: false ) )
                             {
                                 // Run forward part
                                 nr = RunForwardOnSingleDevice( computeGraph, batch, gpuIdx, isTraining: false, returnWordClassInfos );
                             }

                             batchId2Result.AddWithLock( gpuIdx, nr );
                         }
                         catch ( Exception ex )
                         {
                             Logger.WriteErrorLine( $"Test error at processor '{gpuIdx}'. Exception = '{ex.Message}', Call Stack = '{ex.StackTrace}'" );
                             throw;
                         }
                    });
                }

                if ( 0 < dataSizePerGPUMod )
                {
                    var batch = corpusBatch.GetRange( _DeviceIds.Length * dataSizePerGPU, dataSizePerGPUMod );

                    NetworkResult nr;
                    // Create a new computing graph instance
                    using ( ComputeGraphTensor computeGraph = CreateComputGraph( 0, needBack: false ) )
                    {
                        // Run forward part
                        nr = RunForwardOnSingleDevice( computeGraph, batch, 0, isTraining: false );
                    }

                    batchId2Result.Add( _DeviceIds.Length, nr );
                }

                return (MergeResults( batchId2Result ));
            }
            catch ( Exception ex )
            {
                Logger.WriteErrorLine( $"Exception = '{ex.Message}', Call Stack = '{ex.StackTrace}'" );
                throw;
            }
        }

        private void RunValidateAndSaveModel( Corpus validCorpus, Dictionary< int, IList< IMetric > > taskId2metrics, double avgCostPerWordInTotal
            , ExternalValidateDelegate externalValidateRoutine, CancellationToken ct )
        {
            if ( externalValidateRoutine != null )
            {
                //save before ExternalValidation
                SaveModel( createBackupPrevious: false, suffix: ".latest" );

                var isBetterModel = RunExternalValidateRoutine( externalValidateRoutine, ct );
                if ( isBetterModel || !File.Exists( _ModelFilePath ) )
                {
                    SaveModel( createBackupPrevious: true );
                }
            }
            else
            {
                if ( validCorpus != null )
                {
                    ReleaseGradientOnAllDevices();

                    // The valid corpus is provided, so evaluate the model.
                    var isBetterModel = RunValidateRoutine( validCorpus, taskId2metrics );
                    if ( isBetterModel || !File.Exists( _ModelFilePath ) )
                    {
                        SaveModel( createBackupPrevious: true );
                    }
                }
                else if ( (avgCostPerWordInTotal < _AvgCostPerWordInTotalInLastEpoch) || !File.Exists( _ModelFilePath ) )
                {
                    // We don't have valid corpus, so if we could have lower cost, save the model
                    SaveModel( createBackupPrevious: true );
                }

                SaveModel( createBackupPrevious: false, suffix: ".latest" );
            }
        }

        public void Validate( Corpus validCorpus, IList< IMetric > metrics ) => RunValidateRoutine( validCorpus, new Dictionary< int, IList< IMetric > > { { 0, metrics } } );
        public void Validate( Corpus validCorpus, Dictionary< int, IList< IMetric > > taskId2metrics ) => RunValidateRoutine( validCorpus, taskId2metrics );

        /// <summary>
        /// Evaluate the quality of model on valid corpus.
        /// </summary>
        /// <param name="validCorpus">valid corpus to measure the quality of model</param>
        /// <param name="runNetworkFunc">The network to run on specific device</param>
        /// <param name="metrics">A set of metrics. The first one is the primary metric</param>
        /// <param name="outputToFile">It indicates if valid corpus and results should be dumped to files</param>
        /// <returns>true if we get a better result on primary metric, otherwise, false</returns>
        private bool RunValidateRoutine( Corpus validCorpus, Dictionary< int, IList< IMetric > > taskId2metrics )
        {
            var sents = (!_Options.ValidationOutputFileName.IsNullOrWhiteSpace() ? new List< (List< string > src, List< string > @ref, List< string > hyp) >() : null);

            // Clear inner status of each metrics
            foreach ( var metrics in taskId2metrics.Values )
            {
                foreach ( IMetric metric in metrics )
                {
                    metric.ClearStatus();
                }
            }

            CopyWeightsFromDefaultDeviceToAllOtherDevices();
            
            var n = 0; Console.WriteLine();
            var is_run_validate_parallel = _Options.TryRunValidateParallel && (DeviceIds.Length == 1);
            Console.WriteLine( $"begin validate{(is_run_validate_parallel ? " parallel" : null)} ({_Options.ProcessorType})..." );
            if ( is_run_validate_parallel )
            {
                Parallel.ForEach( validCorpus.GetSntPairBatchs(), 
                () => new List< CorpusBatch >(),
                (p, _, corpusBatchs) =>
                {
                    corpusBatchs.Clear(); corpusBatchs.Add( p );

                    RunValidateRoutineCore( taskId2metrics, corpusBatchs, sents );
                    if ( (Interlocked.Increment( ref n ) % 10) == 0 )
                    {
                        Console.Write( $"{n * validCorpus.BatchSize}, " );
                    }
                    return (corpusBatchs);
                },
                _ => {});
                Console.Write( $"{n * validCorpus.BatchSize}, " );
            }
            else
            {
                var corpusBatchs = new List< CorpusBatch >();
                foreach ( var batch in validCorpus.GetSntPairBatchs() )
                {
                    corpusBatchs.Add( batch );
                    if ( corpusBatchs.Count == DeviceIds.Length )
                    {
                        RunValidateRoutineCore( taskId2metrics, corpusBatchs, sents );
                        corpusBatchs.Clear();
                        if ( (++n % 10) == 0 )
                        {
                            Console.Write( $"{n * validCorpus.BatchSize}, " );
                        }
                    }
                }
                if ( corpusBatchs.AnyEx() )
                {
                    RunValidateRoutineCore( taskId2metrics, corpusBatchs, sents );
                    Console.Write( $"{++n * validCorpus.BatchSize}, " );
                }
            }
            Console.WriteLine( "end validate." );

            var isBetterModel = false;
            if ( taskId2metrics.AnyEx() )
            {
                var sb = new StringBuilder();
                var metricList = new List< IMetric >( taskId2metrics.Count );
                foreach ( var p in taskId2metrics ) // Run metrics for each task
                {
                    var taskId  = p.Key;
                    var metrics = p.Value;
                    metricList.AddRange( metrics );

                    var max_len = metrics.Any() ? metrics.Max( m => m.Name.Length ) : 0;
                    foreach ( IMetric metric in metrics )
                    {
                        sb.AppendLine( $"{metric.Name.PadRight( max_len, ' ')} = {metric.GetScoreStr()}" );
                    }

                    var m_0   = metrics[ 0 ];
                    var score = m_0.GetPrimaryScore();
                    if ( (_Model.BestPrimaryScore < score) && (taskId == _PrimaryTaskId) ) // The first metric in the primary task is the primary metric
                    {
                        if ( 0.0f < _Model.BestPrimaryScore )
                        {
                            sb.AppendLine( $"We got a better primary metric '{m_0.Name}', score '{score:F}'. The previous score is '{_Model.BestPrimaryScore:F}'" );
                        }

                        //We have a better primary score on valid set
                        _Model.BestPrimaryScore = score;
                        isBetterModel = true;
                    }
                }
                Logger.WriteInfoLine( ConsoleColor.Yellow, Environment.NewLine + sb.ToString() );

                EvaluationWatcher?.Invoke( this, new EvaluationEventArg()
                {
                    Title       = $"Evaluation result for model '{_ModelFilePath}'",
                    Message     = sb.ToString(),
                    Metrics     = metricList,
                    BetterModel = isBetterModel,
                    Color       = ConsoleColor.Green
                });
            }

            if ( !_Options.ValidationOutputFileName.IsNullOrWhiteSpace() )
            {
                var fn = Path.GetFullPath( _Options.ValidationOutputFileName );
                Logger.WriteInfoLine( $"Save validation results to file: '{fn}'." );
                Directory.CreateDirectory( Path.GetDirectoryName( fn ) );

                using var sw = new StreamWriter( fn, append: false );
                foreach ( var t in sents )
                {
                    #region comm.
                    /*
                    if ( t.src?.Count != t.@ref?.Count || t.@ref?.Count != t.hyp?.Count )
                    {
                        using var sw_xxx = new StreamWriter( @"E:\xxx.txt", append: true );
                        var len_xxx = Math.Max( Math.Max( t.src.Count, t.@ref.Count ), t.hyp.Count );
                        var max_lens_xxx = new int[ len_xxx ];
                        for ( var i = 0; i < len_xxx; i++ )
                        {
                            var s = (i < t.src .Count) ? t.src [ i ].Length : 0;
                            var r = (i < t.@ref.Count) ? t.@ref[ i ].Length : 0;
                            var h = (i < t.hyp .Count) ? t.hyp [ i ].Length : 0;
                            max_lens_xxx[ i ] = Math.Max( Math.Max( s, r ), h ) + 1;
                        }
                        sw_xxx.Write( "src: " ); for ( var i = 0; i < t.src .Count; i++ ) sw_xxx.Write( t.src [ i ].PadRight( max_lens_xxx[ i ] ) ); sw_xxx.WriteLine();
                        sw_xxx.Write( "ref: " ); for ( var i = 0; i < t.@ref.Count; i++ ) sw_xxx.Write( t.@ref[ i ].PadRight( max_lens_xxx[ i ] ) ); sw_xxx.WriteLine();
                        sw_xxx.Write( "hyp: " ); for ( var i = 0; i < t.hyp .Count; i++ ) sw_xxx.Write( t.hyp [ i ].PadRight( max_lens_xxx[ i ] ) ); sw_xxx.WriteLine();
                        sw_xxx.WriteLine();

                        Debugger.Launch();
                    }
                    Debug.Assert( t.src?.Count == t.@ref?.Count && t.@ref?.Count == t.hyp?.Count );
                    //*/
                    #endregion

                    var len = Math.Max( Math.Max( t.src.Count, t.@ref.Count ), t.hyp.Count );
                    var max_lens = new int[ len ];
                    for ( var i = 0; i < len; i++ )
                    {
                        var s = (i < t.src .Count) ? t.src [ i ].Length : 0;
                        var r = (i < t.@ref.Count) ? t.@ref[ i ].Length : 0;
                        var h = (i < t.hyp .Count) ? t.hyp [ i ].Length : 0;
                        max_lens[ i ] = Math.Max( Math.Max( s, r ), h ) + 1;
                    }
                    sw.Write( "src: " ); for ( var i = 0; i < t.src .Count; i++ ) sw.Write( t.src [ i ].PadRight( max_lens[ i ] ) ); sw.WriteLine();
                    sw.Write( "ref: " ); for ( var i = 0; i < t.@ref.Count; i++ ) sw.Write( t.@ref[ i ].PadRight( max_lens[ i ] ) ); sw.WriteLine();
                    sw.Write( "hyp: " ); for ( var i = 0; i < t.hyp .Count; i++ ) sw.Write( t.hyp [ i ].PadRight( max_lens[ i ] ) ); sw.WriteLine();
                    sw.WriteLine();
                }
            }

            return (isBetterModel);
        }
        private bool RunExternalValidateRoutine( ExternalValidateDelegate externalValidateRoutine, CancellationToken ct )
        {
            Logger.WriteInfoLine( $"start external validation..." );
            var mis = externalValidateRoutine?.Invoke( ct ).MetricInfos;
            Logger.WriteInfoLine( "end external validation." );

            var isBetterModel = false;
            if ( mis.AnyEx() )
            {
                var sb = new StringBuilder( 1_000 );
                foreach ( var mi in mis ) // Run metrics for each task
                {
                    sb.AppendLine( $"{mi.MetricName} = {mi.Text}" );

                    if ( _Model.BestPrimaryScore < mi.Score )
                    {
                        if ( 0.0f < _Model.BestPrimaryScore )
                        {
                            sb.AppendLine( $"We got a better primary metric '{mi.MetricName}', score '{mi.Score:F}'. The previous score is '{_Model.BestPrimaryScore:F}'" );
                        }

                        //We have a better primary score on valid set
                        _Model.BestPrimaryScore = mi.Score;
                        isBetterModel = true;
                    }
                }
                Logger.WriteInfoLine( ConsoleColor.Yellow, Environment.NewLine + sb.ToString() );

                EvaluationWatcher?.Invoke( this, new EvaluationEventArg()
                {
                    Title       = $"Evaluation result for model '{_ModelFilePath}'",
                    Message     = sb.ToString(),
                    Metrics     = Enumerable.Empty< IMetric >().ToList(),
                    BetterModel = isBetterModel,
                    Color       = ConsoleColor.Green
                });
            }

            return (isBetterModel);
        }
        private void RunValidateRoutineCore( Dictionary< int, IList< IMetric > > metrics, List< CorpusBatch > corpusBatchs
            , List< (List< string > src, List< string > @ref, List< string > hyp) > sents )
        {
            var outputToFile = (sents != null);
            if ( _DeviceIds.Length == 1 )
            {
                RunValidateRoutineCoreKernel( _DeviceIds[ 0 ], metrics, corpusBatchs, sents );
            }
            else
            {
                // Run forward on all available processors
                Parallel.For( 0, _DeviceIds.Length, devId => RunValidateRoutineCoreKernel( devId, metrics, corpusBatchs, sents ) );
            }
        }
        private void RunValidateRoutineCoreKernel( int devId, Dictionary< int, IList< IMetric > > metrics, List< CorpusBatch > corpusBatchs
            , List< (List< string > src, List< string > @ref, List< string > hyp) > sents )
        {
            if ( corpusBatchs.Count <= devId )
            {
                return;
            }

            try
            {
                var outputToFile = (sents != null);

                var corpusBatch         = corpusBatchs[ devId ];
                var corpusBatchForValid = corpusBatch.CloneSrcTokens();

                // Create a new computing graph instance
                NetworkResult nr;
                using ( ComputeGraphTensor computeGraph = CreateComputGraph( devId, needBack: false ) )
                {
                    // Run forward part
                    nr = RunForwardOnSingleDevice( computeGraph, corpusBatchForValid, devId, isTraining: false );
                }

                lock ( metrics )
                {
                    var srcTkns       = corpusBatch.GetSrcTokens( 0 );
                    var refTokens_lst = new List< List< string > >( 1 ) { null };
                    var newSents      = (outputToFile ? new (List< string > src, List< string > @ref, List< string > hyp)[ corpusBatch.GetBatchSize() ] : null);

                    var metrics_k = metrics[ 0 ];
                    var hypTkns = nr.Output[ 0 ];
                    var refTkns = corpusBatch.GetTgtTokens( 0 );

                    for ( int j = 0; j < hypTkns.Count; j++ )
                    {
                        foreach ( IMetric metric in metrics_k )
                        {
                            if ( refTkns.Count <= j )
                            {
                                throw (new InvalidDataException( $"Ref token only has '{refTkns.Count}' batch, however, it try to access batch '{j}'. Hyp token has '{hypTkns.Count}' tokens, Batch Size = '{corpusBatch.GetBatchSize()}'" ));
                            }

                            try
                            {
                                refTokens_lst[ 0 ] = refTkns[ j ];
                                metric.Evaluate( refTokens_lst/*new List<List<string>>() { refTkns[ j ] }*/, hypTkns[ j ] );
                            }
                            catch ( Exception ex )
                            {
                                Logger.WriteLine( $"Exception = '{ex.Message}', Ref = '{string.Join( " ", refTkns[ j ] )}' Hyp = '{string.Join( " ", hypTkns[ j ] )}'" );
                                throw;
                            }
                        }
                    }

                    if ( outputToFile )
                    {
                        for ( int j = 0; j < srcTkns.Count; j++ )
                        {
                            ref var newSents_j = ref newSents[ j ];
                            newSents_j.src = srcTkns[ j ].TakeWhile( w => !BuildInTokens.IsPreDefinedToken( w ) ).ToList();

                            if ( newSents_j.@ref == null ) newSents_j.@ref = new List<string>();
                            newSents_j.@ref.AddRange( refTkns[ j ]/*.TakeWhile( w => !BuildInTokens.IsPreDefinedToken( w ) )*/ );

                            if ( newSents_j.hyp == null ) newSents_j.hyp = new List<string>();
                            //---newSents_j.hyp.AddRange( hypTkns[ j ].TakeWhile( w => !BuildInTokens.IsPreDefinedToken( w ) ) );
                            newSents_j.hyp.AddRange( hypTkns[ j ].Select( w => BuildInTokens.IsPreDefinedToken( w ) ? " " : w ) );
                        }
                        sents.AddRange( newSents );
                    }
                }
            }
            catch ( OutOfMemoryException ex )
            {
                GC.Collect(); // Collect unused tensor objects and free GPU memory
                Logger.WriteErrorLine( $"Skip current batch for validation due to {ex.Message}" );
            }
            catch ( Exception ex )
            {
                Logger.WriteErrorLine( $"Exception: '{ex.Message}', Call stack: '{ex.StackTrace}'" );
            }
        }

        internal virtual void SaveParameters()
        {
            _Model.ClearWeights();

            RegisterTrainableParameters();

            var setNetworkWrapper = new HashSet<IMultiProcessorNetworkWrapper>( _Name2Network.Count );
            foreach ( IMultiProcessorNetworkWrapper mpnw in _Name2Network.Values )
            {
                // One network wrapper may have multi-names, so we only save one copy of it
                if ( setNetworkWrapper.Add( mpnw ) )
                {
                    mpnw.Save( _Model );
                }
            }
        }
        internal virtual void LoadParameters()
        {
            RegisterTrainableParameters();
            foreach ( KeyValuePair<string, IMultiProcessorNetworkWrapper> p in _Name2Network )
            {
                var name = p.Key;
                var mpnw = p.Value;

                Logger.WriteLine( $"Loading parameter '{name}'" );
                mpnw.Load( _Model );
            }
        }

        protected virtual void SaveParameters( Model model )
        {
            model.ClearWeights();

            RegisterTrainableParameters();

            var setNetworkWrapper = new HashSet<IMultiProcessorNetworkWrapper>( _Name2Network.Count );
            foreach ( IMultiProcessorNetworkWrapper mpnw in _Name2Network.Values )
            {
                // One network wrapper may have multi-names, so we only save one copy of it
                if ( setNetworkWrapper.Add( mpnw ) )
                {
                    mpnw.Save( model );
                }
            }
        }
        protected virtual void LoadParameters( Model model )
        {
            RegisterTrainableParameters();
            foreach ( KeyValuePair<string, IMultiProcessorNetworkWrapper> p in _Name2Network )
            {
                var name = p.Key;
                var mpnw = p.Value;

                Logger.WriteLine( $"Loading parameter '{name}'" );
                mpnw.Load( model );
            }
        }

        /// <summary>
        /// Copy weights from default device to all other devices
        /// </summary>
        internal void CopyWeightsFromDefaultDeviceToAllOtherDevices()
        {
            RegisterTrainableParameters();
            foreach ( IMultiProcessorNetworkWrapper mpnw in _Name2Network.Values )
            {
                mpnw.SyncWeights();
            }
        }

        /// <summary>
        /// Sum up gradients in all devices and keep them in the default device
        /// </summary>
        internal void SumGradientsToTensorsInDefaultDevice()
        {
            RegisterTrainableParameters();
            foreach ( IMultiProcessorNetworkWrapper mpnw in _Name2Network.Values )
            {
                mpnw.SumGradientsToNetworkOnDefaultDevice();
            }
        }
        internal List< WeightTensor > GetParametersFromDefaultDevice()
        {
            RegisterTrainableParameters();
            var result = new List< WeightTensor >( _Name2Network.Count );
            foreach ( IMultiProcessorNetworkWrapper mpnw in _Name2Network.Values )
            {
                result.AddRange( mpnw.GetNeuralUnitOnDefaultDevice().GetParams() );
            }
            return (result);
        }
        internal void ZeroGradientOnAllDevices()
        {
            RegisterTrainableParameters();
            foreach ( IMultiProcessorNetworkWrapper mpnw in _Name2Network.Values )
            {
                mpnw.ZeroGradientsOnAllDevices();
            }
        }
        internal void ReleaseGradientOnAllDevices()
        {
            RegisterTrainableParameters();
            foreach ( IMultiProcessorNetworkWrapper mpnw in _Name2Network.Values )
            {
                mpnw.ReleaseGradientsOnAllDevices();
            }
        }
        [M(O.AggressiveInlining)] private void RegisterTrainableParameters( /*object obj*/ )
        {
            if ( _Name2Network != null )
            {
                return;
            }

            Logger.WriteLine( $"Registering trainable parameters." );
            var obj = this;
            _Name2Network = new SortedList< string, IMultiProcessorNetworkWrapper >();

            foreach ( FieldInfo fi in obj.GetType().GetFields( BindingFlags.NonPublic | BindingFlags.Instance ) )
            {
                var childValue = fi.GetValue( obj );
                var name       = fi.Name;
                Register( childValue, name );
            }
            foreach ( PropertyInfo pi in obj.GetType().GetProperties( BindingFlags.NonPublic | BindingFlags.Instance ) )
            {
                var childValue = pi.GetValue( obj );
                var name       = pi.Name;
                Register( childValue, name );
            }
        }
        private void Register( object childValue, string name )
        {
            if ( childValue is IMultiProcessorNetworkWrapper networks )
            {
                Debug.Assert( !name.IsNullOrWhiteSpace() );
                Debug.Assert( !_Name2Network.ContainsKey( name ) );

                _Name2Network.Add( name, networks );
                Logger.WriteLine( $"Register network '{name}'" );
            }
            else if ( childValue is IMultiProcessorNetworkWrapper[] networksArray )
            {
                for ( var i = 0; i < networksArray.Length; i++ )
                {
                    var name2   = $"{name}_{i}";
                    var network = networksArray[ i ];

                    Debug.Assert( !_Name2Network.ContainsKey( name2 ) );

                    _Name2Network.Add( name2, network );
                    Logger.WriteLine( $"Register network '{name2}'" );
                }                
            }
        }
    }
}
