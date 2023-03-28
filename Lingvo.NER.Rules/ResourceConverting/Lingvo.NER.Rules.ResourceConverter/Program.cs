using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using _Compression_Stream_ = System.IO.Compression.DeflateStream;
//using _Compression_Stream_ = System.IO.Compression.GZipStream;
//using _Compression_Stream_ = System.IO.Compression.BrotliStream;

namespace Lingvo.NER.Rules.ResourceConverter
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static string SOURCE_FOLDER => Path.GetFullPath( ConfigurationManager.AppSettings[ "SOURCE_FOLDER" ] );
        private static string DEST_FOLDER   => Path.GetFullPath( ConfigurationManager.AppSettings[ "DEST_FOLDER" ] );
        private static string DEST_FILENAME => Path.GetFullPath( ConfigurationManager.AppSettings[ "DEST_FILENAME" ] );

        private static bool IsNeedProcessedFile( string fn )
        {
            var ext = Path.GetExtension( fn )?.ToLower();
            return (!fn.Contains( @"\(examples)\", StringComparison.InvariantCultureIgnoreCase ) && ((ext == ".txt") || (ext == ".xml") /*|| (ext == ".csv")*/));
        }

        private static async Task Main( string[] args )
        {
            try
            {
                ResourceFilesConverter.WriteCompressedFiles     ( SOURCE_FOLDER, DEST_FOLDER  , IsNeedProcessedFile );
                ResourceFilesConverter.WriteCompressedSingleFile( SOURCE_FOLDER, DEST_FILENAME, IsNeedProcessedFile );
                var d = ResourceFilesConverter.ReadCompressedSingleFile( DEST_FILENAME );
                Decompress_Test();

                //AssemblyLoader_WithDecompress();

                #region [.GC.Collect.]
                GC.Collect();
                GC.WaitForPendingFinalizers();
#if NETCOREAPP
                GC.GetTotalAllocatedBytes();
#else
                GC.GetTotalMemory( forceFullCollection: true );
#endif
                GC.Collect();
                #endregion

                await Task.CompletedTask.CAX();
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( ex );
                Console.ResetColor();
            }
            //-----------------------------------------------------------//
            Console.WriteLine( Environment.NewLine + "[.....finish.....]" );
            Console.ReadLine();
        }
        private static void Decompress_Test()
        {
            Console.Title = "begin decompress...";
            var total_sw = Stopwatch.StartNew();
            using var timer = new System.Timers.Timer( 1_000 );
            timer.Elapsed += (s, e) => Console.Title = $"decompress: {total_sw.Elapsed}";
            timer.Start();
            //-------------------------------------------------------//

            var n   = 0;
            var cnt = 0;
            var sw  = new Stopwatch();

            var reader = ResourceFilesConverter.ReadDecompressedFiles( @"..\..\..\[resources-bin]\" );
            foreach ( var t in reader )
            {
                using ( var sr = t.streamReader )
                {
                    Console.Write( $"{++n}). '{t.fileName}', {new FileInfo( t.fileName ).DisplayFileSize()}..." );
                    cnt = 0;
                    sw.Restart();
                    for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                    {
                        cnt++;
                    }
                    sw.Stop();
                    Console.WriteLine( $"=> line count: {cnt}, {sw.Elapsed}." );
                }
            }

            timer.Stop();
            total_sw.Stop();
            Console.Title = $"end decompress: {total_sw.Elapsed}";
            Console.WriteLine( $"\r\n end decompress: {total_sw.Elapsed}.\r\n" );
        }

        private static void AssemblyLoader_WithDecompress()
        {
            var total_sw = Stopwatch.StartNew();
#if NETCOREAPP
            var assemblyPath = Path.GetFullPath( Path.Combine( @"..\..\Lingvo.NER.Rules.Resources\bin\net7.0\", "Lingvo.NER.Rules.Resources.dll" ) );
#else
            var assemblyPath = Path.GetFullPath( Path.Combine( @"..\..\Lingvo.NER.Rules.Resources\bin\net4.8\", "Lingvo.NER.Rules.Resources.dll" ) );
#endif

            using var ral = new ResourceAssemblyLoader( assemblyPath, "Lingvo.NER.Rules.Resources.Resources" );

            static StreamReader to_StreamReader( byte[] b )
            {
                var ms = new MemoryStream( b );
                var cs = new _Compression_Stream_( ms, CompressionMode.Decompress );
                var sr = new StreamReader( cs );
                return (sr);
            };

            foreach ( var (name, bytes) in ral.GetAllProperties< byte[] >() )
            {
                using ( var sr = to_StreamReader( bytes ) )
                {
                    var cnt = 0;
                    for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                    {
                        cnt++;
                    }
                    Console.WriteLine( $"'{name}' line count: {cnt}" );
                }
            }

            Console.WriteLine( $"\r\n end: {total_sw.Elapsed}.\r\n" );
        }


        private static string DisplayFileSize( this FileInfo fi ) => DisplayFileSize( fi.Length );
        private static string DisplayFileSize( this long sizeInBytes )
        {
            const float KILOBYTE = 1024;
            const float MEGABYTE = KILOBYTE * KILOBYTE;
            const float GIGABYTE = MEGABYTE * KILOBYTE;

            //if ( fileInfo == null )
            //    return ("NULL");

            if ( GIGABYTE < sizeInBytes )
                return ( (sizeInBytes / GIGABYTE).ToString("N2") + " GB");
            if ( MEGABYTE < sizeInBytes )
                return ( (sizeInBytes / MEGABYTE).ToString("N2") + " MB");
            if ( KILOBYTE < sizeInBytes )
                return ( (sizeInBytes / KILOBYTE).ToString("N2") + " KB");
            return ((sizeInBytes / KILOBYTE).ToString("N1") + " KB");
            //return (sizeInBytes.ToString("N0") + " bytes");
        }


        public static ConfiguredTaskAwaitable< T > CAX< T >( this Task< T > task ) => task.ConfigureAwait( false );
        public static ConfiguredTaskAwaitable CAX( this Task task ) => task.ConfigureAwait( false );
        public static ConfiguredValueTaskAwaitable< T > CAX< T >( this ValueTask< T > task ) => task.ConfigureAwait( false );
        public static ConfiguredValueTaskAwaitable CAX( this ValueTask task ) => task.ConfigureAwait( false );
    }
}

