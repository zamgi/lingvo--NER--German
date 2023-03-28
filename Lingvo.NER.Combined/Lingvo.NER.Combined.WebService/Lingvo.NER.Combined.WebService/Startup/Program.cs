using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

using Lingvo.NER.NeuralNetwork;
using Lingvo.NER.NeuralNetwork.Tokenizing;
using Lingvo.NER.Rules;
using _ModelInfoConfig_ = Lingvo.NER.Combined.WebService.ConcurrentFactory.ModelInfoConfig;

namespace Lingvo.NER.Combined.WebService
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        public const string SERVICE_NAME = "Lingvo.NER.Combined.WebService";

        //private static bool USE_NNER { get; } = (bool.TryParse( ConfigurationManager.AppSettings[ "USE_NNER" ] ?? bool.TrueString, out var b ) && b);

        private static NNERConfig ReadNNERConfig( string[] args, int DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT )
        {
            const string DEFAULT_CONFIG_FILENAME = "ner_de_settings.json";

            var opts = OptionsExtensions.ReadInputOptions< NNERConfig >( args, (opts) =>
            {
                if ( !opts.CONCURRENT_FACTORY_INSTANCE_COUNT.HasValue ) opts.CONCURRENT_FACTORY_INSTANCE_COUNT = DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT;
            }
            , DEFAULT_CONFIG_FILENAME );
            return (opts);
        }

        private static IReadOnlyDictionary< string, _ModelInfoConfig_ > CreateNNERModelInfoConfigs( NNERConfig opts )
        {
            if ( opts.ModelInfos == null ) throw (new ArgumentNullException( nameof(opts.ModelInfos) ));

            var slByType = new Dictionary< string, _ModelInfoConfig_ >( opts.ModelInfos.Count );
            foreach ( var p in opts.ModelInfos )
            {
                var modelType = p.Key;
                var modelInfo = p.Value;

                if ( modelType.IsNullOrEmpty() || modelInfo.ModelFilePath.IsNullOrEmpty() ) continue;

                var saved_ModelFilePath = opts.ModelFilePath;
                {
                    opts.ModelFilePath = modelInfo.ModelFilePath;
                    Predictor predictor = null;
                    if ( modelInfo.LoadImmediate ) //---if ( !modelInfo.DelayLoad )
                    {
                        var sl    = SeqLabel.Create4Predict( opts );
                        predictor = new Predictor( sl, modelInfo.UpperCase );
                    }

                    slByType[ modelType ] = new _ModelInfoConfig_()
                    {
                        predictor     = predictor,
                        modelFilePath = Path.GetFullPath( modelInfo.ModelFilePath ),
                        upperCase     = modelInfo.UpperCase,
                    };
                }
                opts.ModelFilePath = saved_ModelFilePath;
            }
            return (slByType);
        }

        //private static (bool Use, TokenizerConfig TokenizerConfig) CreateNNERIfUse( string[] args, bool use_nner, int DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT )
        //{
        //    if ( use_nner )
        //    {
        //        var opts = ReadNNERConfig( args, DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT );
        //        var nnerTokenizerConfig = new TokenizerConfig( opts.SentSplitterResourcesXmlFilename, opts.UrlDetectorResourcesXmlFilename );
        //        return (true, nnerTokenizerConfig);
        //    }
        //    return (default);
        //}
        //private static async Task< NERCombinedConfig_ForOuterNNERPredictor > CreateNERCombinedConfig( string[] args, bool use_nner, int DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT )
        //{
        //    var nner_task                    = Task.Run( () => CreateNNERIfUse( args, use_nner, DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT ) );
        //    var nerRulesProcessorConfig_task = Config.Inst.CreateNerProcessorConfig_AsyncEx();

        //    await Task.WhenAll( nner_task, nerRulesProcessorConfig_task ).CAX();

        //    var nner                    = nner_task.Result;
        //    var nerRulesProcessorConfig = nerRulesProcessorConfig_task.Result;

        //    var config = new NERCombinedConfig_ForOuterNNERPredictor()
        //    {
        //        NerRules_ProcessorConfig = nerRulesProcessorConfig,
        //        //NerRules_UsedRecognizerTypeEnum = NerProcessor.UsedRecognizerTypeEnum.All_Without_Crf,
        //        NNER_TokenizerConfig     = nner.TokenizerConfig,
        //    };
        //    return (config);
        //}
        private static async Task< NERCombinedConfig_ForOuterNNERPredictor > CreateNERCombinedConfig( NNERConfig nnerCfg, int DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT )
        {
            var nnerTokenizerConfig_task     = Task.Run( () => new TokenizerConfig( nnerCfg.SentSplitterResourcesXmlFilename, nnerCfg.UrlDetectorResourcesXmlFilename ) );
            var nerRulesProcessorConfig_task = Config.Inst.CreateNerProcessorConfig_AsyncEx();

            await Task.WhenAll( nnerTokenizerConfig_task, nerRulesProcessorConfig_task ).CAX();

            var nnerTokenizerConfig     = nnerTokenizerConfig_task.Result;
            var nerRulesProcessorConfig = nerRulesProcessorConfig_task.Result;

            var config = new NERCombinedConfig_ForOuterNNERPredictor()
            {
                NerRules_ProcessorConfig = nerRulesProcessorConfig,
                NNER_TokenizerConfig     = nnerTokenizerConfig,
            };
            return (config);
        }

        private static async Task Main( string[] args )
        {
            var DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT = Environment.ProcessorCount;

            var hostApplicationLifetime = default(IHostApplicationLifetime);
            var logger                  = default(ILogger);
            try
            {
                #region comm.
//#if DEBUG
                //if ( !Debugger.IsAttached )
                //{
                //    Debugger.Launch();
                //} 
//#endif 
                #endregion
                Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );
                //-------------------------------------------------------------------------//
                var saved_CurrentDirectory = Environment.CurrentDirectory;
                    var sw = Stopwatch.StartNew();
                    var nnerCfg = ReadNNERConfig( args, DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT );
                    var nerCombinedCfg = await CreateNERCombinedConfig( nnerCfg, DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT ).CAX();

                    var instanceCount = nnerCfg.CONCURRENT_FACTORY_INSTANCE_COUNT.GetValueOrDefault( DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT );
                    var nnerModelInfoByType = CreateNNERModelInfoConfigs( nnerCfg );

                    var concurrentFactory = new ConcurrentFactory( nnerModelInfoByType, nnerCfg, nerCombinedCfg, instanceCount );
                    Console.WriteLine( $"load model's elapsed: {sw.StopElapsed()}\r\n" );
                Environment.CurrentDirectory = saved_CurrentDirectory;
                //---------------------------------------------------------------//

                var host = Host.CreateDefaultBuilder( args )
                               .ConfigureLogging( loggingBuilder => loggingBuilder.ClearProviders().AddDebug().AddConsole().AddEventSourceLogger().AddEventLog( new EventLogSettings() { LogName = SERVICE_NAME, SourceName = SERVICE_NAME } ) )
                               .ConfigureServices( (hostContext, services) => services.AddSingleton( concurrentFactory ) )
                               .ConfigureWebHostDefaults( webBuilder => webBuilder.UseStartup< Startup >() )
                               .Build();
                hostApplicationLifetime = host.Services.GetService< IHostApplicationLifetime >();
                logger                  = host.Services.GetService< ILoggerFactory >()?.CreateLogger( SERVICE_NAME );
                await host.RunAsync();
            }
            catch ( OperationCanceledException ex ) when ((hostApplicationLifetime?.ApplicationStopping.IsCancellationRequested).GetValueOrDefault())
            {
                Debug.WriteLine( ex ); //suppress
            }
            catch ( Exception ex ) when (logger != null)
            {
                logger.LogCritical( ex, "Global exception handler" );
            }
        }


        public static bool IsNullOrEmpty( this string s ) => string.IsNullOrEmpty( s );
        public static bool IsNullOrWhiteSpace( this string s ) => string.IsNullOrWhiteSpace( s );
        public static ConfiguredTaskAwaitable< T > CAX< T >( this Task< T > task ) => task.ConfigureAwait( false );
        public static ConfiguredTaskAwaitable CAX( this Task task ) => task.ConfigureAwait( false );
    }
}
