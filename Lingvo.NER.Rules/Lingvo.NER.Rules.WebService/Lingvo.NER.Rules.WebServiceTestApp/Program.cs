using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Lingvo.NER.Rules.WebService;

namespace Lingvo.NER.Rules.WebServiceTestApp
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static string BASE_URL => ConfigurationManager.AppSettings[ "BASE_URL" ];

        private static async Task Main( string[] args )
        {
            Console.WriteLine( $"BASE_URL: '{BASE_URL}'\r\n" );

            using var httpClient = new HttpClient();
            var client = new NERRulesWebServiceClient( httpClient, BASE_URL );

            try
            {
                await Run__NER_1( client, MAX_FILE_SIZE_IN_MB: 25 ).CAX();
            }
            catch ( Exception ex )
            {
                CONSOLE.WriteLineError( "ERROR: " + ex );
            }
            CONSOLE.WriteLine( "\r\n\r\n[.....finita.....]\r\n\r\n", ConsoleColor.DarkGray );
            CONSOLE.ReadLine();
        }

        private static IEnumerable< string > GetFile_4_NER( CancellationTokenSource cts, bool print2ConsoleTitle, int MAX_FILE_SIZE_IN_BYTES )
        {
            //var paths = DriveInfo.GetDrives().Where( di => di.DriveType == DriveType.Fixed ).Select( di => di.RootDirectory.FullName ).ToArray();
            //
            var paths = new[] { @"..\..\..\[docs-examples]" };
            //---------------------------------------------------------------//

            //const string SEARCH_PATTERN = "*.txt";
            var file_extensions = new HashSet< string >( new[] { ".txt", ".cs", ".rtf" /*, ".doc", ".docx", ".xlsx", ".xls", ".pdf"*/ }, StringComparer.InvariantCultureIgnoreCase ); 

            if ( print2ConsoleTitle )
            {
                Console.Title = $"start search files ('{string.Join( "', '", file_extensions )}') by paths: '{string.Join( "', '", paths )}'...";
            }

            var eo = new EnumerationOptions()
            {
                IgnoreInaccessible       = true,
                RecurseSubdirectories    = true,
                ReturnSpecialDirectories = true,
            };
            var seq = Enumerable.Empty< string >();
            foreach ( var path in paths )
            {
                seq = seq.Concat( Directory.EnumerateFiles( path, "*.*", eo ) );
            }

            var n  = 0;
            var sw = Stopwatch.StartNew();
            foreach ( var fileName in seq )
            {
                if ( cts.IsCancellationRequested ) yield break;

                if ( print2ConsoleTitle && (((++n % 5_000) == 0) || (1_500 <= sw.ElapsedMilliseconds)) )
                {
                    Console.Title = fileName;
                    sw.Restart();
                }
                var fi = new FileInfo( fileName );
                if ( !file_extensions.Contains( fi.Extension ?? string.Empty ) )
                {
                    continue;
                }
                if ( MAX_FILE_SIZE_IN_BYTES < fi.Length )
                {
                    continue;
                }
                yield return (fileName); 
            }
        }
        private static async Task Run__NER_1( NERRulesWebServiceClient client, int MAX_FILE_SIZE_IN_MB )
        {
            var sw = Stopwatch.StartNew();
            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
            {
                e.Cancel = true;
                cts.Cancel_NoThrow();
            };

            var machineName = Environment.MachineName;
            var files = GetFile_4_NER( cts, print2ConsoleTitle: true, (1024 * 1024 * MAX_FILE_SIZE_IN_MB) ).ToList();
            var filesCount = files.Count;
#if DEBUG
            var dop = 1; //Environment.ProcessorCount;
#else
            var dop = Environment.ProcessorCount;
#endif
            var fileNumber  = 0;
            var errorNumber = 0;
            await files.ForEachAsync( dop, cts.Token, async (fileName, ct) =>
            {
                if ( !TryReadAllText( fileName, out var text ) )
                {
                    return;
                }

                try
                {
                    var result = await client.Run( text, ct ).CAX();
                    
                    CONSOLE.WriteLine( $"processed '{fileName}' => NER: {(result.Words?.Count).GetValueOrDefault()}" );
                }
                catch ( Exception ex )
                {
                    CONSOLE.WriteLineError( ex.ToString() );
                    Interlocked.Increment( ref errorNumber );
                }

                Console.Title = $"processed files: {Interlocked.Increment( ref fileNumber )} of {filesCount}, (errors: {Volatile.Read( ref errorNumber )})...";
                //---Thread.Sleep( 100 );
            })
            .CAX();

            Console.Title = $"total processed files: {fileNumber}, (errors: {errorNumber}), (elapsed: {sw.Elapsed()}).";
        }
        private static bool TryReadAllText( string fileName, out string text )
        {
            if ( (fileName.StartsWith_Ex( @"C:\Windows\WinSxS\" ) || 
                  fileName.StartsWith_Ex( @"C:\Windows\servicing\LCU\" )) && fileName.EndsWith_Ex( @"\license.rtf" ) )
            {
                text = default;
                return (false);
            }

            try
            {
                if ( fileName.EndsWith_Ex( ".txt" ) ||
                     fileName.EndsWith_Ex( ".cs"  )
                   )
                {
                    text = File.ReadAllText( fileName );
                }
                /*
                else if ( FilterReader.TryCreate( fileName, out var filterReader, out var error ) )
                {
                    using ( filterReader )
                    {
                        text = filterReader.ReadToEnd();
                    }                    
                }
                //*/
                else
                {
                    text = default;
                }
                return (!text.IsNullOrWhiteSpace());
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( ex );
            }
            text = default;
            return (false);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static bool IsNullOrEmpty( this string s ) => string.IsNullOrEmpty( s );
        public static bool IsNullOrWhiteSpace( this string s ) => string.IsNullOrWhiteSpace( s );

        public static TimeSpan Elapsed( this Stopwatch sw )
        {
            sw.Stop();
            return (sw.Elapsed);
        }
        public static void Cancel_NoThrow( this CancellationTokenSource cts )
        {
            try
            {
                cts.Cancel();
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( ex );//suppress
            }
        }
        public static T[] ToArray< T >( this IEnumerable< T > seq, int len )
        {
            var a = new T[ len ];
            var i = 0;
            foreach ( var t in seq )
            {
                a[ i++ ] = t;
            }
            return (a);
        }
        public static bool StartsWith_Ex( this string s1, string s2 ) => s1.StartsWith( s2, StringComparison.InvariantCultureIgnoreCase );
        public static bool EndsWith_Ex( this string s1, string s2 ) => s1.EndsWith( s2, StringComparison.InvariantCultureIgnoreCase );
    }
}
