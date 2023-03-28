using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Lingvo.NER.Rules.core;

namespace Lingvo.NER.Rules.PhoneNumbers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPhoneNumbersModel
    {
        bool IsValid( string cityAreaCode, out string cityAreaName );
        bool IsValid( string cityAreaCode, int cityAreaCodeStartIndex, out string cityAreaName );
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class PhoneNumbersModel : IPhoneNumbersModel
    {
        private SortedStringList_WithValueAndSearchByPart _SSL;
        public PhoneNumbersModel( in (string phoneNumbersFileName, int? capacity) t ) : this( t.phoneNumbersFileName, t.capacity ) { }
        public PhoneNumbersModel( string phoneNumbersFileName, int? capacity = null )
        {
            _SSL = new SortedStringList_WithValueAndSearchByPart( capacity.GetValueOrDefault( 100 ) );

            LoadUseStreamReader( phoneNumbersFileName );
            //---LoadUseMMF( phoneNumbersFileName );
        }
        public PhoneNumbersModel( StreamReader sr, int? capacity = null )
        {
            _SSL = new SortedStringList_WithValueAndSearchByPart( capacity.GetValueOrDefault( 100 ) );

            LoadUseStreamReader( sr );
        }

        #region [.load model.]
        private void LoadUseStreamReader( string phoneNumbersFileName )
        {
            using ( var sr = new StreamReader( phoneNumbersFileName ) )
            {
                LoadUseStreamReader( sr );
            }
        }
        private void LoadUseStreamReader( StreamReader sr )
        {
            const char SEPARATOR = ';';

            var buff_1 = new StringBuilder( 100 );
            var buff_2 = new StringBuilder( 100 );
            //var xlat = xlat_Unsafe.Inst;
            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                if ( line.IsNullOrEmpty() /*|| !xlat.IsDigit( line[ 0 ] )*/ ) continue;

                var idx = line.IndexOf( SEPARATOR );
                if ( idx != -1 )
                {
                    var a1 = buff_1.Clear().Append( line, 0, idx++ ).ToString();
                    var a2 = buff_2.Clear().Append( line, idx, line.Length - idx ).ToString();
#if DEBUG
                    var success = _SSL.TryAdd( a1, a2 );
                    //---Debug.Assert( success );
#else
                    _SSL.TryAdd( a1, a2 );
#endif
                }
                else
                {
#if DEBUG
                    var success = _SSL.TryAdd( line, null );
                    Debug.Assert( success );
#else
                    _SSL.TryAdd( line, null );
#endif
                }
            }
        }

        #region comm.
        /*
        unsafe private void LoadUseMMF( string phoneNumbersFileName, bool skipFirstLine = false )
        {
            const int BUFFER_SIZE = 0x2000;

            using ( var fs       = new FileStream( phoneNumbersFileName, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, FileOptions.SequentialScan ) )
            using ( var mmf      = MemoryMappedFile.CreateFromFile( fs, null, 0L, MemoryMappedFileAccess.Read, new MemoryMappedFileSecurity(), HandleInheritability.None, true ) )
            using ( var accessor = mmf.CreateViewAccessor( 0L, 0L, MemoryMappedFileAccess.Read ) )
            {
                byte* buffer = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer( ref buffer );

                if ( skipFirstLine )
                {
                    SkipUntilEndLine( ref buffer, BUFFER_SIZE );
                }

                for ( byte* endBuffer = buffer + fs.Length; buffer < endBuffer; )
                {
                    var cityAreaCode = ReadAsStringUntilSeparators( ref buffer, BUFFER_SIZE );
                    var cityAreaName = ReadAsStringUntilSeparators( ref buffer, BUFFER_SIZE );

                    if ( cityAreaCode != null )
                    {
#if DEBUG
                        var success = _SSL.TryAdd( cityAreaCode, cityAreaName );
                        Debug.Assert( success );
#else
                        _SSL.TryAdd( cityAreaCode, cityAreaName );
#endif
                    }

                    if ( !SkipUntilEndLine( ref buffer, BUFFER_SIZE ) )
                    {
                        break;
                    }
                }
            }
        }
        unsafe private static string ReadAsStringUntilSeparators( ref byte* buffer, int bufferSize )
        {
            for ( var idx = 0; ; idx++ )
            {
                if ( bufferSize < idx )
                {
                    throw (new InvalidDataException( $"WTF?!?!: [{bufferSize} < {idx}]" ));
                }

                switch ( buffer[ idx ] )
                {
                    case (byte) ';': 
                    case (byte) '\r': 
                    case (byte) '\n':
                    case (byte) '\0':
                        var value = (0 < idx) ? Encoding.UTF8.GetString( buffer, idx ) : null; //var value = new string( bufferCharPtr, 0, idx ); //IntPtr textPtr = StringsHelper.AllocHGlobalAndCopy( bufferCharPtr, idx );
                        buffer += (idx + 1);
                        return (value);
                }
            }
        }
        unsafe private static bool SkipUntilEndLine( ref byte* buffer, int bufferSize )
        {
            for ( var idx = 0; ; idx++ )
            {
                if ( bufferSize < idx )
                {
                    throw (new InvalidDataException( $"WTF?!?!: [{bufferSize} < {idx}]" ));
                }

                switch ( buffer[ idx ] )
                {
                    case (byte) '\n':
                        buffer += (idx + 1);
                        return (true);

                    case (byte) '\0':
                        buffer += (idx + 1);
                        return (false);
                }
            }
        }
        //*/
        #endregion
        #endregion

        public bool IsValid( string cityAreaCode, out string cityAreaName ) => _SSL.TryGetValueByPart( cityAreaCode, 0, out cityAreaName );
        public bool IsValid( string cityAreaCode, int cityAreaCodeStartIndex, out string cityAreaName ) => _SSL.TryGetValueByPart( cityAreaCode, cityAreaCodeStartIndex, out cityAreaName );
#if DEBUG
        public override string ToString() => _SSL.Count.ToString();
#endif
#if DEBUG
        public void SelfTest()
        {
            foreach ( var (key, value) in _SSL )
            {
                var success = _SSL.TryGetValueByPart( key, 0, out var existsValue );
                Debug.Assert( success && (value == existsValue) );
            }
        }
#endif
    }
}
