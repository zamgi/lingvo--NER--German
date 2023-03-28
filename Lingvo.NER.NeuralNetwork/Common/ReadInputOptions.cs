using System;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Lingvo.NER.NeuralNetwork.Applications;
using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork
{
    /// <summary>
    /// 
    /// </summary>
    internal static class OptionsExtensions
    {
        public static Options ReadInputOptions( string[] args, string DEFAULT_CONFIG_FILENAME = null ) => ReadInputOptions< Options >( args, null, DEFAULT_CONFIG_FILENAME );
        public static T ReadInputOptions< T >( string[] args, string DEFAULT_CONFIG_FILENAME = null ) where T : Options, new() => ReadInputOptions< T >( args, null, DEFAULT_CONFIG_FILENAME );
        public static T ReadInputOptions< T >( string[] args, Action< T > processOptsAction, string DEFAULT_CONFIG_FILENAME = null ) where T : Options, new()
        {
            #region [.read input params.]
            Logger.WriteLine( $"Command Line = '{string.Join( " ", args )}'" );

            var opts = new T();
            ArgParser.Parse( args, opts );

            if ( (DEFAULT_CONFIG_FILENAME != null) && opts.ConfigFilePath.IsNullOrEmpty() )
            {
                     if ( File.Exists(          DEFAULT_CONFIG_FILENAME ) ) opts.ConfigFilePath =          DEFAULT_CONFIG_FILENAME;
                else if ( File.Exists( @"..\" + DEFAULT_CONFIG_FILENAME ) ) opts.ConfigFilePath = @"..\" + DEFAULT_CONFIG_FILENAME;
            }

            if ( !opts.ConfigFilePath.IsNullOrEmpty() )
            {
                Logger.WriteLine( $"Loading config file from '{opts.ConfigFilePath}'" );
                opts = JsonConvert.DeserializeObject< T >( File.ReadAllText( opts.ConfigFilePath ) );
            }
            if ( !opts.CurrentDirectory.IsNullOrEmpty() )
            {
                try { Environment.CurrentDirectory = opts.CurrentDirectory; }
                catch
                {
                    if ( opts.CurrentDirectory.StartsWith( @"..\" ) )
                    {
                        Environment.CurrentDirectory = opts.CurrentDirectory.Substring( @"..\".Length );
                    }
                    else
                    {
                        throw;
                    }                        
                }
            }

            processOptsAction?.Invoke( opts );

            var jss = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                Converters        = new[] { new StringEnumConverter() },
            };
            Logger.WriteLine( $"Configs: {JsonConvert.SerializeObject( opts, Formatting.Indented, jss )}" );

            return (opts);
            #endregion
        }
    }
}

