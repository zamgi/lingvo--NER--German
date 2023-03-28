using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.CustomerNumbers
{
    /// <summary>
    /// 
    /// </summary>
    internal enum TextPreambleTypeEnum : byte
    {
        __UNDEFINED__,

        CustomerNumber,
        //ContractNumber,
        //InvoiceNumber,
        //FileNumber // Aktenzeichen
    }

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

        [M(O.AggressiveInlining)] public ByTextPreamble_SearchResult( int startIndex, int length
            , TextPreambleTypeEnum textPreambleType, int preambleWordIndex, string customerNumberValue )
        {
            StartIndex          = startIndex;
            Length              = length;
            TextPreambleType    = textPreambleType;
            PreambleWordIndex   = preambleWordIndex;
            CustomerNumberValue = customerNumberValue;
        }

        public int                  StartIndex          { [M(O.AggressiveInlining)] get; }        
        public int                  Length              { [M(O.AggressiveInlining)] get; }
        public int                  PreambleWordIndex   { [M(O.AggressiveInlining)] get; }
        public TextPreambleTypeEnum TextPreambleType    { [M(O.AggressiveInlining)] get; }
        public string               CustomerNumberValue { [M(O.AggressiveInlining)] get; }
        [M(O.AggressiveInlining)] public int EndIndex() => StartIndex + Length;
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}, {TextPreambleType}, '{CustomerNumberValue}']"; 
#endif
    }

    /// <summary>
    ///
    /// </summary>
    unsafe internal sealed class CustomerNumbersSearcher_ByTextPreamble
    {
        /// <summary>
        /// 
        /// </summary>
        internal sealed class WordsChainDictionary
        {
            private Map< string, WordsChainDictionary > _Slots;
            private TextPreambleTypeEnum? _TextPreambleType;
            public WordsChainDictionary() => _Slots = new Map< string, WordsChainDictionary >();

            #region [.append words.]
            public void Add( IList< word_t > words, TextPreambleTypeEnum textPreambleType )
            {
                var startIndex = 0;
                for ( WordsChainDictionary _this = this, _this_next; ; )
                {
                    #region [.save.]
                    if ( words.Count == startIndex )
	                {
                        if ( _this._TextPreambleType.HasValue )
                        {
                            if ( _this._TextPreambleType.Value == textPreambleType )
                            {
                                return;
                            }

                            throw (new InvalidDataException());
                        }
                        _this._TextPreambleType = textPreambleType;
                        return;
                    }
                    #endregion

                    var word = words[ startIndex ].valueUpper;
                    if ( !_this._Slots.TryGetValue( word, out _this_next ) )
                    {
                        //add next word in chain
                        _this_next = new WordsChainDictionary();
                        _this._Slots.Add( word, _this_next );
                    }                
                    _this = _this_next;
                    startIndex++;
                }
            }
            public void Add( string word, TextPreambleTypeEnum textPreambleType )
            {
                if ( !_Slots.TryGetValue( word, out var next ) )
                {
                    //add next word in chain
                    next = new WordsChainDictionary() { _TextPreambleType = textPreambleType };
                    _Slots.Add( word, next );
                }
                else
                {
                    if ( next._TextPreambleType.HasValue ) throw (new InvalidDataException());
                    next._TextPreambleType = textPreambleType;
                }
            }
            #endregion

            #region [.try get.]
            [M(O.AggressiveInlining)] public bool TryGetFirst( IList< word_t > words, int startIndex, out (TextPreambleTypeEnum textPreambleType, int length) x )
            {
                x = default;

                var startIndex_saved = startIndex;
                for ( WordsChainDictionary _this = this, _this_next; ; )
                {
                    #region [.get.]
                    if ( _this._TextPreambleType.HasValue )
                    {
                        x = (_this._TextPreambleType.Value, startIndex - startIndex_saved);
                        //--return (true);
                    }
                    #endregion

                    if ( words.Count == startIndex )
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

                return (x.length != 0);// && ( x.textPreambleType != TextPreambleTypeEnum.__UNDEFINED__);
            }
            #endregion
#if DEBUG
            public override string ToString() => _TextPreambleType.HasValue ? _TextPreambleType.Value.ToString() : $"count: {_Slots.Count}";
#endif
        }

        #region [.cctor().]
        private static WordsChainDictionary _TextPreambles;
        private static Set< char >          _AllowedPunctuation_BetweenDigits;
        private static Set< char >          _AllowedPunctuation_AfterTextPreamble;
        private static CharType*            _CTM;
        static CustomerNumbersSearcher_ByTextPreamble()
        {
            Init_TextPreambles();

            _AllowedPunctuation_BetweenDigits     = xlat.GetHyphens().Concat( new[] { '.' } ).ToSet();
            _AllowedPunctuation_AfterTextPreamble = xlat.GetHyphens().Concat( new[] { ':' } ).ToSet();
            _CTM = xlat_Unsafe.Inst._CHARTYPE_MAP;
        }

        private static void Init_TextPreambles()
        {
            _TextPreambles = new WordsChainDictionary();

            // TODO: separate by different preamble types (Customer, contact, file etc)

            using var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate();
            var secondWords = new[] { "Lautet", "Ist", "Nummer", "Number", "Nr", "No" };

            #region [.firstWords_1.]
            var firstWords_1 = new[]
            {
                "Stromanschluß"  ,
                "Stromanschluss" ,
                "Gasanschluss"   ,
                "Anschlussnummer",
                "Anschlussnr"    ,
                "Anschlußnr"     ,
                "Anschlußnummer" ,
                "Kunden-Nr"      ,
                "KdNr"           ,
                "Kundennummer"   ,
                "Customer-ID"    ,
                "Customer"       ,
                "CustomerNr"     ,
                //"Cust.Nr"        ,
                "Cust-Nr"        ,
                "Customer-Nr"    ,
                "Vertragsnummer" ,
                "Vertragsnr"     ,
                "Vertrags"       ,
                "Aktenzeichen"   ,
            };

            //-1-//
            foreach ( var w in firstWords_1 )
            {
                _TextPreambles.Add_WithDot( w, TextPreambleTypeEnum.CustomerNumber );
            }

            //-2-//
            foreach ( var w1 in firstWords_1 )
            {
                foreach ( var w2 in secondWords )
                {
                    _TextPreambles.Add( tokenizer.Run_NoSentsNoUrlsAllocate( w1 + ' ' + w2 ), TextPreambleTypeEnum.CustomerNumber );
                }
            }
            #endregion

            #region [.firstWords_2.]
            var firstWords_2 = new[]
            {
                "Cust.Nr" ,
                "Cust.-Nr",
            };

            //-1-//
            foreach ( var w in firstWords_2 )
            {
                _TextPreambles.Add_WithDot( tokenizer.Run_NoSentsNoUrlsAllocate( w ), TextPreambleTypeEnum.CustomerNumber );
            }

            //-2-//
            foreach ( var w1 in firstWords_2 )
            {
                foreach ( var w2 in secondWords )
                {
                    _TextPreambles.Add( tokenizer.Run_NoSentsNoUrlsAllocate( w1 + ' ' + w2 ), TextPreambleTypeEnum.CustomerNumber );
                }
            }
            #endregion
        }
        #endregion

        #region [.ctor().]
        //private ICustomerNumbersModel _Model;
        private StringBuilder _Buffer;
        public CustomerNumbersSearcher_ByTextPreamble()// ICustomerNumbersModel model )
        {
            //_Model  = model;
            _Buffer = new StringBuilder( 100 );
        }
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
        [M(O.AggressiveInlining)] private bool IsAllowedPunctuation_BetweenDigits( word_t w )
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
        [M(O.AggressiveInlining)] private bool IsAllowedPunctuation_AfterTextPreamble( word_t w ) => ((w.length == 1) && _AllowedPunctuation_AfterTextPreamble.Contains( w.valueUpper[ 0 ] ));
        [M(O.AggressiveInlining)] private int TryFindCustomerNumber( List< word_t > words, int startIndex, out string customerNumber )
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
                    _Buffer.Append( w.valueOriginal );
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

            customerNumber = _Buffer.ToString(); 
            _Buffer.Clear();
            return (wordCount);
        }

        public bool TryFindAll( List< word_t > words, out IReadOnlyCollection< ByTextPreamble_SearchResult > results )
        {
            var ss = default(SortedSetByRef< ByTextPreamble_SearchResult >);
            var customerNumberValue = default(string);

            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                var w = words[ index ];
                if ( w.IsInputTypeNum() || w.IsExtraWordTypePunctuation() ) //!w.IsOutputTypeOther() )
                {
                    continue;
                }

                if ( !_TextPreambles.TryGetFirst( words, index, out var x ) )
                {
                    continue;
                }
                var preambleWordIndex = index;

                var startIndex = index + x.length;
                if ( (startIndex < len) && IsAllowedPunctuation_AfterTextPreamble( words[ startIndex ] ) )
                {
                    startIndex++;
                }

                var wordCount = 0;
                switch ( x.textPreambleType )
                {
                    case TextPreambleTypeEnum.CustomerNumber:
                        wordCount = TryFindCustomerNumber( words, startIndex, out customerNumberValue );
                        break;
                }

                if ( 0 < wordCount )
                {
                    if ( ss == null ) ss = new SortedSetByRef< ByTextPreamble_SearchResult >( ByTextPreamble_SearchResult.Comparer.Instance );

                    ss.AddEx( startIndex, wordCount, x.textPreambleType, preambleWordIndex, customerNumberValue );

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
        [M(O.AggressiveInlining)] public static void Add_WithDot( this CustomerNumbersSearcher_ByTextPreamble.WordsChainDictionary wd, string key, TextPreambleTypeEnum tpt )
        {
            StringsHelper.ToUpperInvariantInPlace( key );
            wd.Add( key, tpt );
            wd.Add( key + '.', tpt );
        }
        [M(O.AggressiveInlining)] public static void Add_WithDot( this CustomerNumbersSearcher_ByTextPreamble.WordsChainDictionary wd, IList< word_t > words, TextPreambleTypeEnum tpt )
        {
            if ( words.Any() )
            {
                wd.Add( words, tpt );
                words.Last().valueUpper += '.';
                wd.Add( words, tpt );
            }
        }

        [M(O.AggressiveInlining)] public static void AddEx( this SortedSetByRef< ByTextPreamble_SearchResult > ss, int startIndex, int length
            , TextPreambleTypeEnum tpt, int preambleWordIndex, string customerNumberValue )
        {
            var sr = new ByTextPreamble_SearchResult( startIndex, length, tpt, preambleWordIndex, customerNumberValue );
            if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
            {
                ss.Remove( in exists );
            }
            ss.Add( in sr );
        }
    }
}