using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

using _Timer_ = System.Timers.Timer;
using _Compression_Stream_ = System.IO.Compression.DeflateStream;
//using _Compression_Stream_ = System.IO.Compression.GZipStream;
//using _Compression_Stream_ = System.IO.Compression.BrotliStream;

namespace Lingvo.NER.Rules.ResourceConverter
{
    /// <summary>
    /// 
    /// </summary>
    internal static class ResourceFilesConverter
    {
        public static void/*async Task*/ WriteCompressedFiles( string sourceFolder, string destFolder, Func<string, bool> isProcessFileFunc = null )
        {
            if ( isProcessFileFunc == null ) isProcessFileFunc = ( fn ) => true;
            //-------------------------------------------------------//

            Console.Title = "begin compress...";
            var total_sw = Stopwatch.StartNew();
            using var timer = new _Timer_( 1_000 );
            timer.Elapsed += ( s, e ) => Console.Title = $"compress: {total_sw.Elapsed}";
            timer.Start();
            //-------------------------------------------------------//

            var read_dir  = Path.GetFullPath( sourceFolder ).TrimEnd( '\\', '/' );
            var write_dir = Path.GetFullPath( destFolder   ).TrimEnd( '\\', '/' );
            //---if ( !Directory.Exists( write_dir ) ) Directory.CreateDirectory( write_dir );

            var files = (from fn in Directory.EnumerateFiles( read_dir, "*", SearchOption.AllDirectories )
                         where isProcessFileFunc( fn )
                         select fn
                        ).ToList();
            var n = 0;
            var sw = new Stopwatch();
            var read_total_size = 0L;
            var write_total_size = 0L;
            foreach ( var read_fn in files )
            {
                var dir = Path.GetDirectoryName( read_fn.Substring( read_dir.Length + 1 ) );
                var write_fn = Path.Combine( write_dir, dir );
                if ( !Directory.Exists( write_fn ) ) Directory.CreateDirectory( write_fn );
                write_fn = Path.Combine( write_fn, Path.GetFileNameWithoutExtension( read_fn )/*Path.GetFileName( read_fn )*/ + ".bin" );

                var fi = new FileInfo( read_fn );
                Interlocked.Add( ref read_total_size, fi.Length );
                Console.Write( $"{Interlocked.Increment( ref n )} of {files.Count}). '{write_fn}', {fi.DisplayFileSize()}..." );

                sw.Restart();
                using ( var read_fs = File.OpenRead( read_fn ) )
                using ( var write_fs = File.OpenWrite( write_fn ) )
                using ( var write_cs = new _Compression_Stream_( write_fs, CompressionLevel.Optimal ) )
                {
                    write_fs.SetLength( 0 );
                    //await read_fs.CopyToAsync( write_cs ).ConfigureAwait( false );
                    read_fs.CopyTo( write_cs );
                }
                sw.Stop();

                fi = new FileInfo( write_fn );
                Interlocked.Add( ref write_total_size, fi.Length );
                Console.WriteLine( $"=> {fi.DisplayFileSize()}, {sw.Elapsed}." );
            }

            timer.Stop();
            total_sw.Stop();
            Console.Title = $"end compress: {total_sw.Elapsed}";
            Console.WriteLine( $"\r\n end compress: {total_sw.Elapsed}, read-size: {read_total_size.DisplayFileSize()} => write-size: {write_total_size.DisplayFileSize()}.\r\n" );
        }
        public static IEnumerable<(string fileName, StreamReader streamReader)> ReadDecompressedFiles( string inputFolder, Func<string, bool> isProcessFileFunc = null )
        {
            if ( isProcessFileFunc == null ) isProcessFileFunc = ( fn ) => true;
            //-------------------------------------------------------//

            var read_dir = Path.GetFullPath( inputFolder );

            var files = from fn in Directory.EnumerateFiles( read_dir, "*", SearchOption.AllDirectories )
                        where isProcessFileFunc( fn )
                        select fn;

            foreach ( var read_fn in files )
            {
                var read_fs = File.OpenRead( read_fn );
                var write_cs = new _Compression_Stream_( read_fs, CompressionMode.Decompress );
                var sr = new StreamReader( write_cs );

                yield return (read_fn, sr);
            }
        }

        public static void WriteCompressedSingleFile( string sourceDirectoryName, string destinationArchiveFileName, Func< string, bool > isProcessFileFunc = null, bool includeBaseDirectory = false )
        {
            if ( isProcessFileFunc == null ) isProcessFileFunc = (fn) => true;
            //-------------------------------------------------------//

            Console.Title = "begin compress single file...";
            var total_sw = Stopwatch.StartNew();
            using var timer = new _Timer_( 1_000 );
            timer.Elapsed += (s, e) => Console.Title = $"compress single file: {total_sw.Elapsed}";
            timer.Start();
            //-------------------------------------------------------//

            var read_total_size = 0L;

            var beginProcessFileAction = new Action< (int n, FileInfo fi) >( t =>
            {
                read_total_size += t.fi.Length;
                Console.Write( $"{t.n}). '{t.fi.FullName}', {t.fi.DisplayFileSize()}..." );
            });
            var endProcessFileAction   = new Action< (TimeSpan elapsed, FileInfo fi) >( t =>
            {
                Console.WriteLine( $"=> {t.elapsed}." );
            });
            ZipFileExtensions_Adv.WriteCompressedSingleFile( sourceDirectoryName, destinationArchiveFileName, beginProcessFileAction, endProcessFileAction, isProcessFileFunc, includeBaseDirectory );
        
            //-------------------------------------------------------//
            timer.Stop();
            total_sw.Stop();
            Console.Title = $"end compress single file: {total_sw.Elapsed}";
            Console.WriteLine( $"\r\n end compress single file: {total_sw.Elapsed}, read-size: {read_total_size.DisplayFileSize()} => write-size: {new FileInfo( destinationArchiveFileName ).DisplayFileSize()}.\r\n" );
        }
        /// <summary>
        /// 
        /// </summary>
        [Flags] public enum ReadEntryNameTypeEnum
        {
            __UNDEFINED__ = 0,

            Name     = 1,
            FullName = 2,
        }
        public static IReadOnlyDictionary< string, byte[] > ReadCompressedSingleFile( string fileName, ReadEntryNameTypeEnum readEntryNameType = ReadEntryNameTypeEnum.Name )
            => ZipFileExtensions_Adv.ReadFile( fileName, (ZipFileExtensions_Adv.ReadEntryNameTypeEnum) readEntryNameType );

        private static string DisplayFileSize( this FileInfo fi ) => DisplayFileSize( fi.Length );
        private static string DisplayFileSize( this long sizeInBytes )
        {
            const float KILOBYTE = 1024;
            const float MEGABYTE = KILOBYTE * KILOBYTE;
            const float GIGABYTE = MEGABYTE * KILOBYTE;

            //if ( fileInfo == null )
            //    return ("NULL");

            if ( GIGABYTE < sizeInBytes )
                return ((sizeInBytes / GIGABYTE).ToString( "N2" ) + " GB");
            if ( MEGABYTE < sizeInBytes )
                return ((sizeInBytes / MEGABYTE).ToString( "N2" ) + " MB");
            if ( KILOBYTE < sizeInBytes )
                return ((sizeInBytes / KILOBYTE).ToString( "N2" ) + " KB");
            return ((sizeInBytes / KILOBYTE).ToString( "N1" ) + " KB");
            //return (sizeInBytes.ToString("N0") + " bytes");
        }
    }
}
