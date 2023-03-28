using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.TaxIdentifications
{
    /// <summary>
    /// 
    /// </summary>
    internal readonly struct ByTextPreamble_SearchResult
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Comparer : IComparerByRef< ByTextPreamble_SearchResult >
        {
            public static Comparer Instance { get; } = new Comparer();
            private Comparer() { }
            public int Compare( in ByTextPreamble_SearchResult x, in ByTextPreamble_SearchResult y ) => (y.StartIndex - x.StartIndex);
        }

        [M(O.AggressiveInlining)] public ByTextPreamble_SearchResult( int startIndex, int length, int preambleWordIndex, string taxIdentification )
        {
            StartIndex        = startIndex;
            Length            = length;
            PreambleWordIndex = preambleWordIndex;
            TaxIdentification = taxIdentification;
        }

        public int    StartIndex        { [M(O.AggressiveInlining)] get; }        
        public int    Length            { [M(O.AggressiveInlining)] get; }
        public int    PreambleWordIndex { [M(O.AggressiveInlining)] get; }
        public string TaxIdentification { [M(O.AggressiveInlining)] get; }
        [M(O.AggressiveInlining)] public int EndIndex() => StartIndex + Length;
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}, '{TaxIdentification}']"; 
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class WordsChainDictionary
    {
        private Map< string, WordsChainDictionary > _Slots;
        private bool _IsLeaf;
        public WordsChainDictionary( int capacity ) => _Slots = Map< string, WordsChainDictionary >.CreateWithCloserCapacity( capacity );
        public WordsChainDictionary() => _Slots = new Map< string, WordsChainDictionary >();

        #region [.append words.]
        public void Add( IList< word_t > words )
        {
            var idx = 0;
            var count = words.Count;
            for ( WordsChainDictionary _this = this, _this_next; ; )
            {
                #region [.save.]
                if ( count == idx )
	            {
                    _this._IsLeaf = true;
                    return;
                }
                #endregion

                var v = words[ idx ].valueUpper;
                if ( !_this._Slots.TryGetValue( v, out _this_next ) )
                {
                    //add next word in chain
                    _this_next = new WordsChainDictionary();
                    _this._Slots.Add( v, _this_next );
                }                
                _this = _this_next;
                idx++;
            }
        }
        #endregion

        #region [.try get.]
        [M(O.AggressiveInlining)] public bool TryGetFirst( IList< word_t > words, int startIndex, out int length )
        {
            length = default;

            var startIndex_saved = startIndex;
            var count = words.Count;
            for ( WordsChainDictionary _this = this, _this_next; ; )
            {
                #region [.get.]
                if ( _this._IsLeaf )
                {
                    length = (startIndex - startIndex_saved);
                    //--return (true);
                }
                #endregion

                if ( count == startIndex )
                {
                    break;
                }

                if ( !_this._Slots.TryGetValue( words[ startIndex ].valueUpper, out _this_next ) )
                {
                    break;
                }
                _this = _this_next;
                startIndex++;
            }

            return (length != 0);
        }
        #endregion
#if DEBUG
        public override string ToString() => (_IsLeaf ? "true" : $"count: {_Slots.Count}");
#endif
    }

    /// <summary>
    ///
    /// </summary>
    unsafe internal sealed class TaxIdentificationsSearcher_ByTextPreamble
    {
        #region [.cctor().]
        private static WordsChainDictionary _TextPreambles;
        private static Set< char >          _AllowedPunctuation_BetweenDigits;
        private static Set< char >          _AllowedPunctuation_AfterTextPreamble;
        private static CharType*            _CTM;
        static TaxIdentificationsSearcher_ByTextPreamble()
        {
            Init_TextPreambles();

            _AllowedPunctuation_BetweenDigits     = xlat.GetHyphens().Concat( new[] { '/' } ).ToSet();
            _AllowedPunctuation_AfterTextPreamble = xlat.GetHyphens().Concat( new[] { ':' } ).ToSet();
            _CTM = xlat_Unsafe.Inst._CHARTYPE_MAP;
        }

        private static void Init_TextPreambles()
        {
            var words = new[] 
            { 
                "Steueridentifikationsnummer",
                "Steueridentifikationsnr.",
                "Steueridentifikationsnr",
                "Steuerliche Identifikationsnummer",
                "Steuerliche Identifikationsnr.",
                "Steuerliche Identifikationsnr",
                "Steuer ID",
                "Steuer-ID",
                "Steuer ID Nummer",
                "Steuer ID Nr.",
                "Steuer ID Nr",
                "Steuer-ID Nummer",
                "Steuer-ID Nr.",
                "Steuer-ID Nr",
                "IdNr",
                "IdNr.",
                "Steuer-IdNr",
                "Steuer-IdNr.",
                "Steuer IdNr",
                "Steuer IdNr.",
                "Bundesweite Identifikationsnummer",
                "Bundesweite Identifikationsnr.",
                "Bundesweite Identifikationsnr",
                "Persönliche Identifikationsnummer",
                "Persönliche Identifikationsnr.",
                "Persönliche Identifikationsnr",
                "Tax ID",
                "Tax-ID",
                "TIN",
                "Tax Identification Number",
                "Tax Identification No.",
                "Tax Identification No",
                //------------------------------------------------//
                "Steuernummer",
                "Steuer Nummer",
                "Steuernr.",
                "Steuernr",
                "StNr",
                "St.Nr.",
                "StNr.",
                "St.Nr",
                "St-Nr",
                "St-Nr.",
                "St.-Nr.",
                "St.-Nr",
                "Tax Number",
                "Tax No.",
                "Tax No"
            };

            _TextPreambles = new WordsChainDictionary( words.Length );

            using var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate();

            foreach ( var w in words )
            {
                var tokens = tokenizer.Run_NoSentsNoUrlsAllocate( w );
                _TextPreambles.Add( tokens ); //.Add_WithDot( tokens );

                if ( tokens.Last().valueUpper.LastChar() == '.' )
                {
                    tokens = tokenizer.Run_NoSentsNoUrlsAllocate( w + " 1234567" );
                    tokens.RemoveAt_Ex( tokens.Count - 1 );
                    _TextPreambles.Add( tokens ); //.Add_WithDot( tokenizer.Run_NoSentsNoUrlsAllocate( w + " ." ) );
                }
            }
        }
        #endregion

        #region [.ctor().]
        private StringBuilder _Buffer;
        public TaxIdentificationsSearcher_ByTextPreamble() => _Buffer = new StringBuilder( 100 );
        #endregion

        [M(O.AggressiveInlining)] private static bool ContainsOnlyUpperLettersAndDigits( string value )
        {
            fixed ( char* base_ptr = value )
            {
                var hasDigits = false;
                for ( var value_ptr = base_ptr; ; value_ptr++ )
                {
                    var ch = *value_ptr;
                    if ( ch == '\0' )
                    {
                        return (hasDigits);
                    }
                    var ct = _CTM[ ch ];
                    if ( (ct & CharType.IsLetter) == CharType.IsLetter && 
                         (ct & CharType.IsUpper ) == CharType.IsUpper
                       )
                    {
                        continue;
                    }

                    if ( (ct & CharType.IsDigit ) == CharType.IsDigit )
                    {
                        hasDigits = true;
                        continue;
                    }
                    return (false);
                }
            }
        }
        [M(O.AggressiveInlining)] private static bool IsAllowedPunctuation_BetweenDigits( word_t w )
        {
            if ( w.length == 1 )
            {
                return (_AllowedPunctuation_BetweenDigits.Contains( w.valueUpper[ 0 ] ));
            }

            if ( 3 <= w.length )
            {
                return (_AllowedPunctuation_BetweenDigits.Contains( w.valueUpper[ 0 ] ) &&
                        _AllowedPunctuation_BetweenDigits.Contains( w.valueUpper[ w.length - 1 ] ));
            }

            return (false);
        }
        [M(O.AggressiveInlining)] private static bool IsAllowedPunctuation_AfterTextPreamble( word_t w ) => ((w.length == 1) && _AllowedPunctuation_AfterTextPreamble.Contains( w.valueUpper[ 0 ] ));

        [M(O.AggressiveInlining)] private static bool IsDigit( char ch ) => ('0' <= ch) && (ch <= '9');
        [M(O.AggressiveInlining)] private static bool CheckTaxIdentificationByCheckDigit( string taxIdentification ) 
        {
            //always a 11 digit code.
            const int LENGTH = 11;

            if ( taxIdentification.Length != LENGTH )
            {
                return (false);
            }

            #region [.last digit.]
            var endIndex  = taxIdentification.Length - 1;
            var last_char = taxIdentification[ endIndex ];
            if ( !IsDigit( last_char ) )
            {
                return (false);
            }
            #endregion

	        const int N = 11;
	        const int M = 10;
	        
            var product = M;
            for ( int i = 0; i < endIndex; i++ )
            {
                var ch = taxIdentification[ i ];
                if ( !IsDigit( ch ) )
                {
                    return (false);
                }
#if DEBUG
                Debug.Assert( IsDigit( ch ) );
#endif
                var sum = ((ch - '0') + product) % M;
                if ( sum == 0 )
                {
                    sum = M;
                }
                product = (2 * sum) % N;
            }

	        var checkDigit = N - product;
            if ( checkDigit == 10 ) checkDigit = 0;

            var last_checkDigit = last_char - '0';
            var success         = (checkDigit == last_checkDigit);
	        return (success);
        }
        [M(O.AggressiveInlining)] private int TryFindTaxIdentification( List< word_t > words, int startIndex, out string taxIdentification )
        {
            var wordCount = 0;
            for ( var len = words.Count; startIndex < len; startIndex++  )
            {
                var w = words[ startIndex ];

                if ( w.IsExtraWordTypeIntegerNumber() )
                {
                    wordCount++;
                    _Buffer.Append( w.valueOriginal );
                }
                else if ( (0 < wordCount) && IsAllowedPunctuation_BetweenDigits( w ) )
                {
                    wordCount++;
                    //---_Buffer.Append( w.valueOriginal );
                }
                else
                {
                    if ( (3 < w.length) && ContainsOnlyUpperLettersAndDigits( w.valueOriginal ) )
                    {
                        wordCount++;
                        _Buffer.Append( w.valueOriginal );
                    }
                    break;
                }
            }

            taxIdentification = _Buffer.ToString(); _Buffer.Clear();
            return (wordCount);
        }

        public bool TryFindAll( List< word_t > words, out IReadOnlyCollection< ByTextPreamble_SearchResult > results )
        {
            var ss = default(SortedSetByRef< ByTextPreamble_SearchResult >);

            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                var w = words[ index ];
                if ( w.IsInputTypeNum() || w.IsExtraWordTypePunctuation() ) //!w.IsOutputTypeOther() )
                {
                    continue;
                }

                if ( !_TextPreambles.TryGetFirst( words, index, out var length ) )
                {
                    continue;
                }
                var preambleWordIndex = index;

                var startIndex = index + length;
                if ( (startIndex < words.Count) && IsAllowedPunctuation_AfterTextPreamble( words[ startIndex ] ) )
                {
                    startIndex++;
                }

                var wordCount = TryFindTaxIdentification( words, startIndex, out var taxIdentification );
                if ( (0 < wordCount) && CheckTaxIdentificationByCheckDigit( taxIdentification ) )
                {
                    if ( ss == null ) ss = new SortedSetByRef< ByTextPreamble_SearchResult >( ByTextPreamble_SearchResult.Comparer.Instance );

                    ss.AddEx( startIndex, wordCount, preambleWordIndex, taxIdentification );

                    index = startIndex + wordCount - 1;
                }
            }

            results = ss;
            return (ss != null);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class ByTextPreambleExtensions
    {
        [M(O.AggressiveInlining)] public static void AddEx( this SortedSetByRef< ByTextPreamble_SearchResult > ss, int startIndex, int length, int preambleWordIndex, string taxIdentification )
        {
            var sr = new ByTextPreamble_SearchResult( startIndex, length, preambleWordIndex, taxIdentification );
            if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
            {
                ss.Remove( in exists );
            }
            ss.Add( in sr );
        }
    }
}