using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

using Lingvo.NER.NeuralNetwork.Tokenizing;
using _ModelInfoConfig_ = Lingvo.NER.NeuralNetwork.WebService.ConcurrentFactory.ModelInfoConfig;

namespace Lingvo.NER.NeuralNetwork.WebService
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        public const string SERVICE_NAME = "Lingvo.NER.NeuralNetwork.WebService";

        private static Config ReadInputOptions( string[] args, int DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT )
        {
            const string DEFAULT_CONFIG_FILENAME = "ner_de_settings.json";

            var opts = OptionsExtensions.ReadInputOptions< Config >( args, (opts) =>
            {
                if ( !opts.CONCURRENT_FACTORY_INSTANCE_COUNT.HasValue ) opts.CONCURRENT_FACTORY_INSTANCE_COUNT = DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT;
            }
            , DEFAULT_CONFIG_FILENAME );
            return (opts);
        }
        private static IReadOnlyDictionary< string, _ModelInfoConfig_ > CreateModelInfoConfigs( Config opts )
        {
            if ( opts.ModelInfos == null ) throw (new ArgumentNullException( nameof(opts.ModelInfos) ));

            var tokenizerConfig = new TokenizerConfig( opts.SentSplitterResourcesXmlFilename, opts.UrlDetectorResourcesXmlFilename );
            var tokenizer_replcaeNumsOnPlaceholder    = new Tokenizer( tokenizerConfig, replaceNumsOnPlaceholder: true  );
            var tokenizer_no_replcaeNumsOnPlaceholder = new Tokenizer( tokenizerConfig, replaceNumsOnPlaceholder: false );

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
                        tokenizer     = modelInfo.DontReplaceNumsOnPlaceholders ? tokenizer_no_replcaeNumsOnPlaceholder : tokenizer_replcaeNumsOnPlaceholder,
                        upperCase     = modelInfo.UpperCase
                    };
                }
                opts.ModelFilePath = saved_ModelFilePath;
            }
            return (slByType);
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
                    var opts = ReadInputOptions( args, DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT );

                    var sw = Stopwatch.StartNew();
                    var instanceCount = opts.CONCURRENT_FACTORY_INSTANCE_COUNT.GetValueOrDefault( DEFAULT_CONCURRENT_FACTORY_INSTANCE_COUNT );
                    var slByType      = CreateModelInfoConfigs( opts );

                    var concurrentFactory = new ConcurrentFactory( slByType, opts, instanceCount );
                    Console.WriteLine( $"load model elapsed: {sw.StopElapsed()}\r\n" );
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
    }
}
