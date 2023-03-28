using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.core.Infrastructure;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Address
{
    /// <summary>
    /// 
    /// </summary>
    [Flags] public enum StreetWordType
    {
        __UNDEFINED__ = 0,

        StreetPostfix    = 1,
        StreetEndKeyWord = (1 << 1),

        StreetDictOneWord = (1 << 2),
        StreetDictFirstWordOfMultiWord = (1 << 3),
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags] public enum CityWordType
    {
        __UNDEFINED__ = 0,

        CityOneWord = 1,
        CityFirstWordOfMultiWord = (1 << 1),
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IAddressModel
    {
        StreetWordType IsStreetWordType( string value );
        StreetWordType GetStreetPostfixWordType( string value );
        bool IsStreetMultiWord( List< word_t > words, int startIndex, out (string streetFullValue, int length) t );

        bool IsZipCode( string value );
        bool IsZipCode( int value );

        CityWordType IsCityWordType( string value );
        bool IsCityMultiWord( List< word_t > words, int startIndex, out (string cityFullValue, int length) t );
        bool TryFindAllCityMultiWords( List< word_t > words, out IReadOnlyCollection< MultiWord_SearchResult< string > > results );
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class AddressModel : IAddressModel
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class StreetPostfix_t
        {
            private Map< char, StreetPostfix_t > _Prev;
            public bool IsLeaf        { [M(O.AggressiveInlining)] get; private set; }
            public bool OnlyFullValue { [M(O.AggressiveInlining)] get; private set; }
            public bool HasPrev       { [M(O.AggressiveInlining)] get => (_Prev != null); }
            [M(O.AggressiveInlining)] public bool TryGetValue( char key, out StreetPostfix_t sp ) => _Prev.TryGetValue( key, out sp );

            public void Add( string value, int startIndex, bool onlyFullValue )
            {
                var sp = this;
                for ( ; 0 <= startIndex; startIndex-- )
                {                    
                    var prev_ch = value[ startIndex ];
                    if ( !sp.HasPrev )
                    {
                        sp._Prev = new Map< char, StreetPostfix_t >();

                        var prev_sp = new StreetPostfix_t();
                        sp._Prev.Add( prev_ch, prev_sp );
                        sp = prev_sp;
                    }
                    else if ( sp._Prev.TryGetValue( prev_ch, out var exists_sp ) )
                    {
                        sp = exists_sp;
                    }
                    else
                    {
                        var prev_sp = new StreetPostfix_t();
                        sp._Prev.Add( prev_ch, prev_sp );
                        sp = prev_sp;
                    }
                }
                sp.IsLeaf        = true;
                sp.OnlyFullValue = onlyFullValue;
            }
#if DEBUG
            private IList< string > AsStrings()
            {
                if ( HasPrev )
                {
                    var lst = new List< string >( _Prev.Count );
                    foreach ( var p in _Prev )
                    {
                        var text = p.Key +
                                   (IsLeaf ? $"(+{(OnlyFullValue ? "[F]" : null)})" : null);
                        var ss = p.Value.AsStrings();
                        foreach ( var s in ss )
                        {
                            lst.Add( s + text );
                        }
                    }
                    return (lst);
                }
                return (new[] { (IsLeaf ? $"(+{(OnlyFullValue ? "[F]" : null)})" : null) });
            }
            public string AsString( char ch ) => string.Join( $"{ch}, ", AsStrings() ) + ch;
            public override string ToString() => string.Join( ", ", AsStrings() );
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        private struct StreetPostfixes_t
        {
            public static StreetPostfixes_t Create() => new StreetPostfixes_t() { _Roots = new Map< char, StreetPostfix_t >() };

            private Map< char, StreetPostfix_t > _Roots;
            public void Add( string value, bool onlyFullValue = false )
            {
                if ( value.IsNullOrEmpty() ) return;

                var idx = value.Length - 1;
                var ch  = value[ idx ];
                if ( !_Roots.TryGetValue( ch, out var exists_sp ) )
                {
                    exists_sp = new StreetPostfix_t();
                    _Roots.Add( ch, exists_sp );
                }
                exists_sp.Add( value, idx - 1, onlyFullValue );
            }
            public StreetWordType GetStreetWordType( string value )
            {
                //---if ( value.IsNullOrEmpty() ) return (StreetWordType.__UNDEFINED__);

                var idx = value.Length - 1;
                var ch  = value[ idx ];
                if ( _Roots.TryGetValue( ch, out var exists_sp ) )
                {
                    for ( idx--; ; idx-- )
                    {
                        if ( idx < 0 )
                        {
                            return (exists_sp.IsLeaf ? StreetWordType.StreetEndKeyWord : StreetWordType.__UNDEFINED__);
                        }
                        if ( !exists_sp.HasPrev )
                        {
                            return ((!exists_sp.OnlyFullValue && exists_sp.IsLeaf) ? StreetWordType.StreetPostfix : StreetWordType.__UNDEFINED__);
                        }

                        ch = value[ idx ];                        
                        if ( !exists_sp.TryGetValue( ch, out var _exists_sp ) )
                        {
                            break;
                        }
                        exists_sp = _exists_sp;
                    }
                }
                return (StreetWordType.__UNDEFINED__);
            }
#if DEBUG
            public override string ToString() => _Roots.Count.ToString();
            public IList< string > AsStrings() => _Roots.Select( p => p.Value.AsString( p.Key ) ).ToArray();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public struct InputParams
        {
            public (string Filename, int? Capacity) ZipCodes { get; set; }
            public (string Filename, int? CapacityOneWord, int? CapacityMultiWord) Cities  { get; set; }
            public (string Filename, int? CapacityOneWord, int? CapacityMultiWord) Streets { get; set; }
        }
        /// <summary>
        /// 
        /// </summary>
        public struct InputParams_2
        {
            public (StreamReader Sr, int? Capacity) ZipCodes { get; set; }
            public (StreamReader Sr, int? CapacityOneWord, int? CapacityMultiWord) Cities  { get; set; }
            public (StreamReader Sr, int? CapacityOneWord, int? CapacityMultiWord) Streets { get; set; }
        }

        #region [.cctor().]
        private static StreetPostfixes_t _StreetPostfixes;
        private static int               _StreetPostfixMinLength;

        static AddressModel() => Init_StreetPostfixes();
        #endregion

        #region [.ctor().]
        private Set< int > _ZipCodes;
        private Map< string, CityWordType > _CityWords;
        private int                         _CityWordMinLength;
        private int                         _CityWordMaxLength;
        private MultiWordSearcher< string > _CityMultiWordSearcher;

        private Map< string, StreetWordType > _StreetWords;
        private int                           _StreetWordMinLength;
        private int                           _StreetWordMaxLength;
        private MultiWordSearcher< string >   _StreetMultiWordSearcher;

        public AddressModel( in InputParams ip )
        {            
            Init_StreetsByDict( in ip );
            Init_Cities( in ip );
            Init_ZipCodes( in ip );
        }
        public AddressModel( in InputParams_2 ip )
        {
            Init_StreetsByDict( ip.Streets .Sr, ip.Streets .CapacityOneWord, ip.Streets.CapacityMultiWord );
            Init_Cities       ( ip.Cities  .Sr, ip.Cities  .CapacityOneWord, ip.Cities .CapacityMultiWord );
            Init_ZipCodes     ( ip.ZipCodes.Sr, ip.ZipCodes.Capacity );
        }
        #endregion

        #region [.Street Postfixes.]
        private static void Init_StreetPostfixes()
        {
            _StreetPostfixMinLength = int.MaxValue;
            _StreetPostfixes = StreetPostfixes_t.Create();
            var sb = new StringBuilder();
            add_StreetPostfix_with_first_upper_and_hyphens( "str"    , sb, add_end_dot: true );
            add_StreetPostfix_with_first_upper_and_hyphens( "straße" , sb );
            add_StreetPostfix_with_first_upper_and_hyphens( "strasse", sb );
            add_StreetPostfix_with_first_upper_and_hyphens( "allee"  , sb );
            add_StreetPostfix_with_first_upper_and_hyphens( "platz"  , sb, add_end_dot: true );
            add_StreetPostfix_with_first_upper_and_hyphens( "weg"    , sb );
            add_StreetPostfix_with_first_upper_and_hyphens( "ring"   , sb );
            add_StreetPostfix_with_first_upper_and_hyphens( "markt"  , sb );
            add_StreetPostfix_with_first_upper_and_hyphens( "berg"   , sb );
        }
        private static void add_StreetPostfix_with_first_upper_and_hyphens( string value, StringBuilder buff, bool add_end_dot = false )
        {
            const char DOT = '.';
            if ( value.Length < _StreetPostfixMinLength )
            {
                _StreetPostfixMinLength = value.Length;
            }
            
            _StreetPostfixes.Add( value ); //"allee"
            if ( add_end_dot )
            {
                _StreetPostfixes.Add( buff.Clear().Append( value ).Append( DOT ).ToString() ); //"allee."
            }

            buff.Clear().Append( value );
            buff[ 0 ] = xlat.UPPER_INVARIANT_MAP[ buff[ 0 ] ];
            value = buff.ToString();
            _StreetPostfixes.Add( value, onlyFullValue: true ); //"Allee"
            if ( add_end_dot )
            {
                _StreetPostfixes.Add( buff.Append( DOT ).ToString(), onlyFullValue: true ); //"Allee."
            }

            foreach ( var hyphen in xlat.GetHyphens() )
            {
                var v = buff.Clear().Append( hyphen ).Append( value ).ToString();
                _StreetPostfixes.Add( v ); //"-Allee"
                if ( add_end_dot )
                {
                    _StreetPostfixes.Add( buff.Append( DOT ).ToString() ); //"-Allee."
                }
            }
        }

        [M(O.AggressiveInlining)] public StreetWordType IsStreetWordType( string value )
        {
            if ( value != null )
            {
                //Streets by dict
                if ( (_StreetWordMinLength <= value.Length) && (value.Length <= _StreetWordMaxLength) &&
                     _StreetWords.TryGetValue( value, out var streetWordType )
                   )
                {
                    return (streetWordType);
                }

                //Streets by postfix
                if ( _StreetPostfixMinLength <= value.Length )
                {
                    return (_StreetPostfixes.GetStreetWordType( value ));
                }
            }
            return (StreetWordType.__UNDEFINED__);
        }
        public StreetWordType GetStreetPostfixWordType( string value ) => ((value != null) ? _StreetPostfixes.GetStreetWordType( value ) : StreetWordType.__UNDEFINED__);
        #endregion

        #region [.Streets by dict.]
        private void Init_StreetsByDict( in InputParams ip )
        {
            using ( var sr = new StreamReader( ip.Streets.Filename ) )
            {
                Init_StreetsByDict( sr, ip.Streets.CapacityOneWord, ip.Streets.CapacityMultiWord );
            }
        }
        private void Init_StreetsByDict( StreamReader sr, int? capacityOneWord, int? capacityMultiWord )
        {
            _StreetWords        = new Map< string, StreetWordType >( capacityOneWord.GetValueOrDefault() );
            var multiWordNgrams = new List< MultiWord_Ngram< string > >( capacityMultiWord.GetValueOrDefault() );
            var vc              = new VersionCombiner< string >();

            using ( var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate() )
            {
                var DASH = MultiWordSearcher< string >.DASH;
                var seps = new[] { ' ', '/', MultiWordSearcher< string >.DASH_CHAR/*'-'*/ };
                //var dahses = xlat.GetHyphens().Select( dash => dash.ToString() ).ToArray();

                for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                {
                    var (onlyLetters, hasWhiteSpaceOrDot) = StringsHelper.ContainsOnlyLettersOrWhiteSpaceOrDot( line );
                    if ( onlyLetters )
                    {
                        _StreetWords.AddOrUpdateEx( line, StreetWordType.StreetDictOneWord );
                    }
                    else
                    {
                        if ( !hasWhiteSpaceOrDot )
                        {
                            _StreetWords.AddOrUpdateEx( line, StreetWordType.StreetDictOneWord );
                        }

                        var words = tokenizer.Run_NoSentsNoUrlsAllocate( line );
                        if ( 1 < words.Count )
                        {
                            var w1 = words[ 0 ];
                            if ( 1 < w1.length )
                            {
                                #region [.1.]
                                var array_1 = (from w in words select w.valueOriginal).ToArray( words.Count );

                                _StreetWords.AddOrUpdateEx( array_1[ 0 ], StreetWordType.StreetDictFirstWordOfMultiWord );
                                multiWordNgrams.AddEx( array_1, line );

                                foreach ( var arr in vc.GetVersions( array_1, DASH, returnOriginArray: false ) )
                                {
                                    multiWordNgrams.AddEx( arr, line );
                                }
                                #endregion
                            }
                            else
                            {
                                words.Clear();
                            }
                        }

                        #region [.2.]
                        var array_2 = line.Split( seps, StringSplitOptions.RemoveEmptyEntries );
                        if ( (array_2.Length == 1) || (hasWhiteSpaceOrDot && (words.Count == array_2.Length)) )
                        {
                            continue;
                        }

                        var firstWord = array_2[ 0 ];
                        if ( 1 < firstWord.Length )
                        {
                            _StreetWords.AddOrUpdateEx( firstWord, StreetWordType.StreetDictFirstWordOfMultiWord );
                            multiWordNgrams.AddEx( array_2, line );

                            foreach ( var arr in vc.GetVersions( array_2, DASH, returnOriginArray: false ) )
                            {
                                multiWordNgrams.AddEx( arr, line );
                            }

                            //foreach ( var dash in dahses )
                            //foreach ( var array_3 in vc.GetVersions( array, dash, returnOriginArray: false ) )
                            //{
                            //    multiWordNgrams.AddEx( array_3, line );
                            //}
                        }
                        #endregion
                    }
                }
            }

            _StreetMultiWordSearcher = new MultiWordSearcher< string >( multiWordNgrams );
            (_StreetWordMinLength, _StreetWordMaxLength) = _StreetWords.GetMinMaxLength();
        }

        [M(O.AggressiveInlining)] public bool IsStreetMultiWord( List< word_t > words, int startIndex, out (string streetFullValue, int length) t ) 
        {
#if DEBUG && _FALSE_
            var s1 = _StreetMultiWordSearcher.TryFindAll( words, startIndex, out var ss );
            var s2 = _StreetMultiWordSearcher.TryFindFirst( words, startIndex, out var sr );
            Debug.Assert( s1 == s2 ); 
            if ( s1 || s2 )
            {
                //---Debug.Assert( ss.Count == 1 );
                Debug.Assert( MultiWord_SearchResult< string >.Comparer.Instance.Compare( ss.First(), sr ) == 0 );

                t = (streetFullValue: sr.Value, length: sr.Length);
                return (true);
            }
#else
            if ( _StreetMultiWordSearcher.TryFindFirst( words, startIndex, out var sr ) )
            {
                t = (streetFullValue: sr.Value, length: sr.Length);
                return (true);
            }
#endif
            t = default; 
            return (false); 
        }
        #endregion

        #region [.Zip Codes.]
        private void Init_ZipCodes( in InputParams ip )
        {
            #region comm. [.binary format.]
            /*
            using ( var fs = File.OpenRead( ip.ZipCodes.Filename ) )
            using ( var br = new BinaryReader( fs ) )
            {
                var len = (int) (fs.Length / sizeof(int));

                _ZipCodes = new Set< int >( len );

                for ( int i = 0; i < len; i++ )
                {
                    var zipCode = br.ReadInt32();
                    _ZipCodes.Add( zipCode );
                }
            }
            */
            #endregion

            #region [.string format.]
            //*
            using ( var sr = new StreamReader( ip.ZipCodes.Filename ) )
            {
                Init_ZipCodes( sr, ip.ZipCodes.Capacity );
            }
            //*/
            #endregion
        }
        private void Init_ZipCodes( StreamReader sr, int? capacity )
        {
            #region [.string format.]
            //*
            _ZipCodes = new Set< int >( capacity.GetValueOrDefault() );

            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                if ( int.TryParse( line, out var zipCode ) || int.TryParse( line.Trim(), out zipCode ) )
                {
                    _ZipCodes.Add( zipCode );
                }
            }
            //*/
            #endregion
        }

        public bool IsZipCode( string value ) => int.TryParse( value, out var i ) && _ZipCodes.Contains( i );
        public bool IsZipCode( int value ) => _ZipCodes.Contains( value );
        #endregion

        #region [.Cities.]
        private void Init_Cities( in InputParams ip )
        {
            using ( var sr = new StreamReader( ip.Cities.Filename ) )
            {
                Init_Cities( sr, ip.Cities.CapacityOneWord, ip.Cities.CapacityMultiWord );
            }
        }
        private void Init_Cities( StreamReader sr, int? capacityOneWord, int? capacityMultiWord )
        {
            _CityWords          = new Map< string, CityWordType >( capacityOneWord.GetValueOrDefault() + capacityMultiWord.GetValueOrDefault() );
            var multiWordNgrams = new List< MultiWord_Ngram< string > >( capacityMultiWord.GetValueOrDefault() * 5 ); // 10 / 9 );
            var vc              = new VersionCombiner< string >();

            var seps = new[] { ' ', '-', '/' };
            var dahses = xlat.GetHyphens().Select( dash => dash.ToString() ).ToArray();
                
            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                var (onlyLetters, hasWhiteSpace) = StringsHelper.ContainsOnlyLettersOrWhiteSpace( line );
                if ( onlyLetters )
                {
                    _CityWords.AddOrUpdateEx( line, CityWordType.CityOneWord );
                }
                else
                {
                    if ( !hasWhiteSpace )
                    {
                        _CityWords.AddOrUpdateEx( line, CityWordType.CityOneWord );
                    }

                    var array = line.Split( seps, StringSplitOptions.RemoveEmptyEntries );
#if DEBUG
                    Debug.Assert( 1 < array.Length );
                    foreach ( var s in array )
                    {
                        Debug.Assert( StringsHelper.ContainsOnlyLettersOrWhiteSpace( s ).OnlyLetters || 
                                        ((s.Length == 1) && (xlat_Unsafe.Inst.IsPunctuation( s[ 0 ] ))) 
                                    );
                    }
#endif
                    _CityWords.AddOrUpdateEx( array[ 0 ], CityWordType.CityFirstWordOfMultiWord );
                    multiWordNgrams.AddEx( array, line );

                    foreach ( var dash in dahses )
                    foreach ( var array_2 in vc.GetVersions( array, dash, returnOriginArray: false ) )
                    {
                        multiWordNgrams.AddEx( array_2, line );
                    }
                }
            }
            
            _CityMultiWordSearcher = new MultiWordSearcher< string >( multiWordNgrams );
            (_CityWordMinLength, _CityWordMaxLength) = _CityWords.GetMinMaxLength();
        }

        public CityWordType IsCityWordType( string value )
        {
            if ( (value != null) && (_CityWordMinLength <= value.Length) && (value.Length <= _CityWordMaxLength) && 
                 _CityWords.TryGetValue( value, out var cityWordType )
               )
            {
                return (cityWordType);
            }
            return (CityWordType.__UNDEFINED__);
        }

        [M(O.AggressiveInlining)] public bool IsCityMultiWord( List< word_t > words, int startIndex, out (string cityFullValue, int length) t ) 
        {
#if DEBUG && _FALSE_
            var s1 = _CityMultiWordSearcher.TryFindAll( words, startIndex, out var ss );
            var s2 = _CityMultiWordSearcher.TryFindFirst( words, startIndex, out var sr );
            Debug.Assert( s1 == s2 ); 
            if ( s1 || s2 )
            {
                //---Debug.Assert( ss.Count == 1 );
                Debug.Assert( MultiWord_SearchResult< string >.Comparer.Instance.Compare( ss.First(), sr ) == 0 );

                t = (cityFullValue: sr.Value, length: sr.Length);
                return (true);
            }
#else
            if ( _CityMultiWordSearcher.TryFindFirst( words, startIndex, out var sr ) )
            {
                t = (cityFullValue: sr.Value, length: sr.Length);
                return (true);
            }
#endif
            t = default; 
            return (false); 
        }
        public bool TryFindAllCityMultiWords( List< word_t > words, out IReadOnlyCollection< MultiWord_SearchResult< string > > results ) => _CityMultiWordSearcher.TryFindAll( words, 0, out results );
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class AddressExtensions
    {
        [M(O.AggressiveInlining)] public static void AddOrUpdateEx( this Map< string, CityWordType > cityWords, string firstWord, CityWordType cityWordType )
        {
            if ( !cityWords.TryAdd( firstWord, cityWordType, out var cityWordTypeExists ) && (cityWordTypeExists != cityWordType) )
            {
                cityWords.TryUpdate( firstWord, cityWordTypeExists | cityWordType );
            }
        }
        [M(O.AggressiveInlining)] public static void AddOrUpdateEx( this Map< string, StreetWordType > streetWords, string firstWord, StreetWordType streetWordType )
        {
            if ( !streetWords.TryAdd( firstWord, streetWordType, out var streetWordTypeExists ) && (streetWordTypeExists != streetWordType) )
            {
                streetWords.TryUpdate( firstWord, streetWordTypeExists | streetWordType );
            }
        }
        [M(O.AggressiveInlining)] public static (int minLength, int maxLength) GetMinMaxLength< T >( this Map< string, T > map )
        {
            var minLength = int.MaxValue;
            var maxLength = int.MinValue;
            foreach ( var p in map )
            {
                if ( p.Key.Length < minLength )
                {
                    minLength = p.Key.Length;
                }

                if ( maxLength < p.Key.Length )
                {
                    maxLength = p.Key.Length;
                }
            }
            return (minLength, maxLength);
        }
        [M(O.AggressiveInlining)] public static void AddEx( this List< MultiWord_Ngram< string > > multiWordNgrams, string[] array, string value ) => multiWordNgrams.Add( new MultiWord_Ngram< string >( array, value ) );
    }
}
