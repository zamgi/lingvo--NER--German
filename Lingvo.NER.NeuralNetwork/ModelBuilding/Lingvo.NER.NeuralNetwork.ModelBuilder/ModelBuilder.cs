using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Lingvo.NER.NeuralNetwork.Applications;
using Lingvo.NER.NeuralNetwork.Networks;
using Lingvo.NER.NeuralNetwork.Optimizer;
using Lingvo.NER.NeuralNetwork.Text;
using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork.ModelBuilder
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static void Main( string[] args )
        {
            try
            {
                var opts = OptionsExtensions.ReadInputOptions< Options >( args, 
                    _ => Logger.LogFile = $"Lingvo.NER.NeuralNetwork.ModelBuilder__({Misc.GetTimeStamp( DateTime.Now )}).log",
                    "train.json" );

                Run_Train( opts );
                //Validator.Run_Validate( opts );
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
                Console.ResetColor();
            }
        }

        private static void Run_Train( Options opts )
        {
            //Console.WriteLine( Environment.CurrentDirectory );
            //var r = (new ExternalValidatorRunner( opts.ExternalValidator )).ExternalValidateRoutine( default ); Console.WriteLine( r );
            //return;

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                Console.WriteLine( "\r\n cancel operation..." );
                cts.Cancel_NoThrow( e );
            };

            // Load train corpus
            using var trainCorpus = new Corpus( opts.TrainCorpusPath, opts.BatchSize, opts.ShuffleBlockSize, opts.MaxTrainSentLength, opts.ShuffleType, opts.TooLongSequence, cts.Token );

            // Load valid corpus
            using var validCorpus = !opts.ValidCorpusPath.IsNullOrEmpty() ? new Corpus( opts.ValidCorpusPath, opts.BatchSize, opts.ShuffleBlockSize, opts.MaxPredictSentLength, opts.ShuffleType, opts.TooLongSequence, cts.Token ) : null;

            // Load or build vocabulary
            Vocab srcVocab;
            Vocab tgtVocab;
            if ( !opts.SrcVocab.IsNullOrEmpty() && !opts.TgtVocab.IsNullOrEmpty() )
            {
                // Vocabulary files are specified, so we load them
                srcVocab = new Vocab( opts.SrcVocab, ignoreCase: false );
                tgtVocab = new Vocab( opts.TgtVocab, ignoreCase: false );
            }
            else
            {
                // We don't specify vocabulary, so we build it from train corpus
                (srcVocab, tgtVocab) = trainCorpus.BuildVocabs( vocabIgnoreCase: false, opts.SrcVocabSize );
            }

            // Create learning rate
            ILearningRate learningRate = new DecayLearningRate( opts.StartLearningRate, opts.WarmUpSteps, opts.WeightsUpdateCount );

            // Create optimizer
            IOptimizer optimizer = Misc.CreateOptimizer( opts );

            // Create metrics
            var metrics = Validator.CreateFromTgtVocab_MultiLabelsFscoreMetric( tgtVocab );

            SeqLabel sl;
            if ( !File.Exists( opts.ModelFilePath ) )
            {
                //New training
                sl = SeqLabel.Create4Train( opts, srcVocab, tgtVocab );
            }
            else
            {
                //Incremental training
                Logger.WriteLine( $"Loading model from '{opts.ModelFilePath}'..." );
                sl = SeqLabel.Create4Train( opts );
            }

            // Add event handler for monitoring
            sl.StatusUpdateWatcher += Misc.StatusUpdateWatcher;

            ExternalValidatorRunner evr = null;
            if ( !opts.ExternalValidator.FileName.IsNullOrWhiteSpace() )
            {
                Logger.WriteLine( $"Will be using External validator '{opts.ExternalValidator.FileName}'." );
                evr = new ExternalValidatorRunner( opts.ExternalValidator );
            }

            // Kick off training
            sl.Train( opts.MaxEpochNum, trainCorpus, validCorpus, learningRate, metrics, optimizer, cancellationToken: cts.Token, evr.GetExternalValidateRoutine() );
        }
        private static ExternalValidateDelegate GetExternalValidateRoutine( this ExternalValidatorRunner evr ) => (evr != null) ? evr.ExternalValidateRoutine : null;

        /// <summary>
        /// 
        /// </summary>
        private sealed class ExternalValidatorRunner
        {
            private string _FileName;
            private string _Arguments;
            private string _WorkingDirectory;
            public ExternalValidatorRunner( string fileName, string arguments, string workingDirectory )
            {
                _FileName  = fileName;
                _Arguments = arguments;
                _WorkingDirectory = workingDirectory ?? string.Empty;
            }
            public ExternalValidatorRunner( in Options.ExternalValidator_t opts )
            {
                _FileName = opts.FileName;
                _Arguments = opts.Arguments;
                _WorkingDirectory = opts.WorkingDirectory ?? string.Empty;
            }
            public Validator.Result ExternalValidateRoutine( CancellationToken ct )
            {
                try
                {
                    var pipeTask = PipeIPC.Server__in.RunDataReceiver( PipeIPC.PIPE_NAME_1, ct );

                    var runExternalValidateTask = Task.Run( () =>
                    {
                        var psi = new ProcessStartInfo( _FileName, _Arguments )
                        {
                            WorkingDirectory = _WorkingDirectory,
                            UseShellExecute  = true,
                            WindowStyle      = ProcessWindowStyle.Minimized //.Hidden
                        };
                        using ( var p = Process.Start( psi ) )
                        {
                            p.WaitForExit();
                        }
                    });
                    var waitCancelTask = Task.Delay( 1_000 * 60 * 30 /*30 min.*/ /*Timeout.Infinite*/, ct );

                    var tasks = new[] { pipeTask, runExternalValidateTask, waitCancelTask };
                    var taskIdx = Task.WaitAny( tasks, ct );
                    switch ( taskIdx )
                    {
                        case 0: //pipeTask
                            var (json, error) = pipeTask.Result;
                            if ( error != null )
                            {
                                throw (error);
                            }
                            var result = JsonConvert.DeserializeObject< Validator.Result >( json );
                            return (result);

                        case 1:
                            if ( runExternalValidateTask.Exception != null )
                            {
                                throw (runExternalValidateTask.Exception);
                            }
                            throw (new Exception( $"Error while running external validator. fn='{_FileName}', args='{_Arguments}'." ));

                        case 2:
                        default:
                            throw (new TimeoutException( $"Timeout while running external validator." ));
                    }
                }
                catch ( Exception ex )
                {
                    var fc = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine( "[EXTERNAL_VALIDATE_ROUTINE]: " + ex );
                    Console.ForegroundColor = fc;

                    return (default);
                }            
            }
        }
    }
}
