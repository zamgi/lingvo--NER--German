using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

#if NETCOREAPP
using System.Buffers;
#endif

namespace System.IO.Compression
{
    /// <summary>
    /// 
    /// </summary>
    public static class ZipFileExtensions_Adv
    {
        public static void WriteCompressedSingleFile( string sourceDirectoryName, string destinationArchiveFileName
            , Action< (int n           , FileInfo fi) > beginProcessFileAction = null
            , Action< (TimeSpan elapsed, FileInfo fi) > endProcessFileAction   = null
            , Func< string, bool > isProcessFileFunc = null, bool includeBaseDirectory = false )
        {
            if ( isProcessFileFunc == null ) isProcessFileFunc = (fn) => true;
            //-------------------------------------------------------//

            const CompressionLevel COMPRESSION_LEVEL = CompressionLevel.Optimal;

            // Rely on Path.GetFullPath for validation of sourceDirectoryName and destinationArchive
            // Checking of compressionLevel is passed down to DeflateStream and the IDeflater implementation
            // as it is a pluggable component that completely encapsulates the meaning of compressionLevel.

            sourceDirectoryName        = Path.GetFullPath( sourceDirectoryName );
            destinationArchiveFileName = Path.GetFullPath( destinationArchiveFileName );

            //add files and directories
            var di = new DirectoryInfo( sourceDirectoryName );
            var basePath = di.FullName;
            if ( includeBaseDirectory && (di.Parent != null) ) basePath = di.Parent.FullName;

            var n = 0;
            var sw = new Stopwatch();
            using ( var archive = OpenZipArchive( destinationArchiveFileName, ZipArchiveMode.Create, entryNameEncoding: Encoding.UTF8  ) )
            {
                // Windows' MaxPath (260) is used as an arbitrary default capacity, as it is likely
                // to be greater than the length of typical entry names from the file system, even
                // on non-Windows platforms. The capacity will be increased, if needed.
                const int DEFAULT_CAPACITY = 260;
#if NETCOREAPP
            var entryNameBuffer = ArrayPool< char >.Shared.Rent( DEFAULT_CAPACITY );
            try
            {                
#else
                var entryNameBuffer = new char[ DEFAULT_CAPACITY ];
#endif
                foreach ( var file in di.EnumerateFileSystemInfos( "*", SearchOption.AllDirectories ) )
                {
                    if ( !isProcessFileFunc( file.FullName ) ) continue;

                    var entryNameLength = file.FullName.Length - basePath.Length;
                    Debug.Assert( 0 < entryNameLength );

                    if ( file is FileInfo fi )
                    {
                        sw.Restart();
                        beginProcessFileAction?.Invoke( (++n, fi) );

                        // Create entry for file:
                        var entryName = EntryFromPath( file.FullName, basePath.Length, entryNameLength, ref entryNameBuffer );
                        var entry = archive.CreateEntryFromFile( file.FullName, entryName, COMPRESSION_LEVEL );

                        sw.Stop();
                        endProcessFileAction?.Invoke( (sw.Elapsed, fi) );
                    }
                    #region comm.
                    /*
                    else if ( (file is DirectoryInfo possiblyEmpty) && ZipFileExtensions.IsDirEmpty( possiblyEmpty ) )
                    {
                        // FullName never returns a directory separator character on the end,
                        // but Zip archives require it to specify an explicit directory:
                        var entryName = ZipFileExtensions.EntryFromPath( file.FullName, basePath.Length, entryNameLength, ref entryNameBuffer, appendPathSeparator: true );
                        archive.CreateEntry( entryName );
                    }
                    */ 
                    #endregion
                }
#if NETCOREAPP
            }
            finally
            {
                ArrayPool< char >.Shared.Return( entryNameBuffer );
            }
#endif

            }
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
        public static IReadOnlyDictionary< string, byte[] > ReadFile( string fileName, ReadEntryNameTypeEnum readEntryNameType = ReadEntryNameTypeEnum.Name )
        {
            if ( readEntryNameType == ReadEntryNameTypeEnum.__UNDEFINED__ ) readEntryNameType = ReadEntryNameTypeEnum.Name;

            using ( var archive = OpenZipArchive( fileName, ZipArchiveMode.Read, entryNameEncoding: Encoding.UTF8  ) )            
            {
                var d = new Dictionary< string, byte[] >( archive.Entries.Count * 2 );

                foreach ( var entry in archive.Entries )
                {
                    var bytes = new byte[ entry.Length ];
                    //using ( var ms = new MemoryStream( (int) entry.Length ) )
                    using ( var s = entry.Open() )
                    {
                        var rb = s.Read( bytes, 0, bytes.Length );
                        Debug.Assert( rb == bytes.Length );
                        //s.CopyTo( ms );

                        //var bytes = ms.ToArray();
                        if ( (readEntryNameType & ReadEntryNameTypeEnum.Name    ) == ReadEntryNameTypeEnum.Name     ) d[ entry.Name     ] = bytes;
                        if ( (readEntryNameType & ReadEntryNameTypeEnum.FullName) == ReadEntryNameTypeEnum.FullName ) d[ entry.FullName ] = bytes;
                    }
                }

                return (d);
            }
        }


        private static ZipArchive OpenZipArchive( string archiveFileName, ZipArchiveMode mode, Encoding entryNameEncoding )
        {
            // Relies on FileStream's ctor for checking of archiveFileName

            FileMode   fileMode;
            FileAccess access;
            FileShare  fileShare;

            switch ( mode )
            {
                case ZipArchiveMode.Read:
                    fileMode  = FileMode  .Open;
                    access    = FileAccess.Read;
                    fileShare = FileShare .Read;
                    break;

                case ZipArchiveMode.Create:
                    fileMode  = FileMode  .OpenOrCreate; //FileMode  .CreateNew;
                    access    = FileAccess.Write;
                    fileShare = FileShare .None;
                    break;

                case ZipArchiveMode.Update:
                    fileMode  = FileMode  .OpenOrCreate;
                    access    = FileAccess.ReadWrite;
                    fileShare = FileShare .None;
                    break;

                default:
                    throw new ArgumentOutOfRangeException( nameof(mode) );
            }

            // Suppress CA2000: fs gets passed to the new ZipArchive, which stores it internally.
            // The stream will then be owned by the archive and be disposed when the archive is disposed.
            // If the ctor completes without throwing, we know fs has been successfully stores in the archive;
            // If the ctor throws, we need to close it here.

            var fs = new FileStream( archiveFileName, fileMode, access, fileShare, bufferSize: 0x1000, useAsync: false );
            try
            {
                if ( (access & FileAccess.Write) == FileAccess.Write ) fs.SetLength( 0 );
                return (new ZipArchive( fs, mode, leaveOpen: false, entryNameEncoding: entryNameEncoding ));
            }
            catch
            {
                fs.Dispose();
                throw;
            }
        }
        private static ZipArchiveEntry CreateEntryFromFile( this ZipArchive destination, string sourceFileName, string entryName, CompressionLevel? compressionLevel )
        {
            // Checking of compressionLevel is passed down to DeflateStream and the IDeflater implementation
            // as it is a pluggable component that completely encapsulates the meaning of compressionLevel.

            // Argument checking gets passed down to FileStream's ctor and CreateEntry

            using ( var fs = new FileStream( sourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0x1000, useAsync: false ) )
            {
                ZipArchiveEntry entry = compressionLevel.HasValue
                                    ? destination.CreateEntry( entryName, compressionLevel.Value )
                                    : destination.CreateEntry( entryName );

                var lastWrite = File.GetLastWriteTime( sourceFileName );

                // If file to be archived has an invalid last modified time, use the first datetime representable in the Zip timestamp format
                // (midnight on January 1, 1980):
                if ( lastWrite.Year < 1980 || lastWrite.Year > 2107 )
                {
                    lastWrite = new DateTime( 1980, 1, 1, 0, 0, 0 );
                }
                entry.LastWriteTime = lastWrite;

                //SetExternalAttributes( fs, entry );

                using ( var es = entry.Open() )
                {
                    fs.CopyTo( es );
                }
                return (entry);
            }
        }
        //private static void SetExternalAttributes( FileStream fs, ZipArchiveEntry entry )
        //{
        //    Interop.Sys.FileStatus status;
        //    Interop.CheckIo( Interop.Sys.FStat( fs.SafeFileHandle, out status ), fs.Name );

        //    entry.ExternalAttributes |= status.Mode << 16;
        //}

        private static string EntryFromPath( string entry, int offset, int length, ref char[] buffer, bool appendPathSeparator = false )
        {
            // Per the .ZIP File Format Specification 4.4.17.1 all slashes should be forward slashes
            const char   PATH_SEPARATOR_CHAR   = '/';
            const string PATH_SEPARATOR_STRING = "/";

            Debug.Assert( length <= entry.Length - offset );
            Debug.Assert( buffer != null );

            // Remove any leading slashes from the entry name:
            while ( length > 0 )
            {
                if ( entry[ offset ] != Path.DirectorySeparatorChar &&
                     entry[ offset ] != Path.AltDirectorySeparatorChar )
                {
                    break;
                }
                offset++;
                length--;
            }

            if ( length == 0 )
            {
                return (appendPathSeparator ? PATH_SEPARATOR_STRING : string.Empty);
            }

            var resultLength = appendPathSeparator ? length + 1 : length;
            EnsureCapacity( ref buffer, resultLength );
            entry.CopyTo( offset, buffer, 0, length );

            // '/' is a more broadly recognized directory separator on all platforms (eg: mac, linux)
            // We don't use Path.DirectorySeparatorChar or AltDirectorySeparatorChar because this is
            // explicitly trying to standardize to '/'
            for ( int i = 0; i < length; i++ )
            {
                var ch = buffer[ i ];
                if ( ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar )
                {
                    buffer[ i ] = PATH_SEPARATOR_CHAR;
                }
            }

            if ( appendPathSeparator )
            {
                buffer[ length ] = PATH_SEPARATOR_CHAR;
            }

            return (new string( buffer, 0, resultLength ));
        }
        private static void EnsureCapacity( ref char[] buffer, int min )
        {
            Debug.Assert( buffer != null );
            Debug.Assert( 0 < min );

            if ( buffer.Length < min )
            {
                var newCapacity = buffer.Length * 2;
                if ( newCapacity < min ) newCapacity = min;
#if NETCOREAPP
                var oldBuffer = buffer;
                buffer = ArrayPool< char >.Shared.Rent( newCapacity );
                ArrayPool< char >.Shared.Return( oldBuffer );
#else
                buffer = new char[ newCapacity ];
#endif
            }
        }
        /*private static bool IsDirEmpty( DirectoryInfo possiblyEmptyDir )
        {
            using ( var enumerator = Directory.EnumerateFileSystemEntries( possiblyEmptyDir.FullName ).GetEnumerator() )
            {
                return (!enumerator.MoveNext());
            }
        }*/
    }
}

