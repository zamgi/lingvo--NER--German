using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.BankAccounts
{
    /// <summary>
    /// 
    /// </summary>
    internal enum TextPreambleTypeEnum : byte
    {
        __UNDEFINED__,

        BankCode,
        AccountNumber,

        BankName,
        AccountOwner,
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
            , TextPreambleTypeEnum textPreambleType, int preambleWordIndex, string bankAccountValue )
        {
            StartIndex        = startIndex;
            Length            = length;
            TextPreambleType  = textPreambleType;
            PreambleWordIndex = preambleWordIndex;
            BankAccountValue  = bankAccountValue;
        }

        public int                  StartIndex        { [M(O.AggressiveInlining)] get; }        
        public int                  Length            { [M(O.AggressiveInlining)] get; }
        public int                  PreambleWordIndex { [M(O.AggressiveInlining)] get; }
        public TextPreambleTypeEnum TextPreambleType  { [M(O.AggressiveInlining)] get; }
        public string               BankAccountValue  { [M(O.AggressiveInlining)] get; }
        [M(O.AggressiveInlining)] public int EndIndex() => StartIndex + Length;
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}, {TextPreambleType}, '{BankAccountValue}']"; 
#endif
    }

    /// <summary>
    ///
    /// </summary>
    unsafe internal sealed class BankAccountsSearcher_ByTextPreamble
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
            /*public void Add( IList< string > words, TextPreambleTypeEnum textPreambleType )
            {
                var startIndex = 0;
                for ( WordsChainDictionary _this = this, _this_next; ; )
                {
                    #region [.save.]
                    if ( words.Count == startIndex )
	                {
                        if ( _this._TextPreambleType.HasValue ) throw (new InvalidDataException());
                        _this._TextPreambleType = textPreambleType;
                        return;
                    }
                    #endregion

                    var word = words[ startIndex ];
                    if ( !_this._Slots.TryGetValue( word, out _this_next ) )
                    {
                        //add next word in chain
                        _this_next = new WordsChainDictionary();
                        _this._Slots.Add( word, _this_next );
                    }                
                    _this = _this_next;
                    startIndex++;
                }
            }*/
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
            [M(O.AggressiveInlining)] public bool Contains_AsFirstInChain( word_t word ) => _Slots.ContainsKey( word.valueUpper );

            //[M(O.AggressiveInlining)] public bool TryGet( IList< string > words, out TextPreambleTypeEnum textPreambleType )
            //{
            //    var startIndex = 0;
            //    for ( WordsChainDictionary _this = this, _this_next; ; )
            //    {
            //        #region [.get.]
            //        if ( _this._TextPreambleType.HasValue )
            //        {
            //            textPreambleType = _this._TextPreambleType.Value;
            //            return (true);
            //        }
            //        #endregion

            //        if ( words.Count == startIndex )
            //        {
            //            break;
            //        }

            //        if ( !_this._Slots.TryGetValue( words[ startIndex ], out _this_next ) )
            //        {
            //            break;
            //        }
            //        _this = _this_next;
            //        startIndex++;
            //    }

            //    textPreambleType = default;
            //    return (false);
            //}
            //[M(O.AggressiveInlining)] public bool TryGet( IList< string > words, IReadOnlyCollection< TextPreambleTypeEnum > textPreambleTypes )
            //{
            //    textPreambleTypes.Clear();

            //    var startIndex = 0;
            //    for ( WordsChainDictionary _this = this, _this_next; ; )
            //    {
            //        #region [.get.]
            //        if ( _this._TextPreambleType.HasValue )
            //        {
            //            textPreambleTypes.Add( _this._TextPreambleType.Value );
            //        }
            //        #endregion

            //        if ( words.Count == startIndex )
            //        {
            //            break;
            //        }

            //        var word = words[ startIndex ];
            //        if ( !_this._Slots.TryGetValue( word, out _this_next ) )
            //        {
            //            break;
            //        }
            //        _this = _this_next;
            //        startIndex++;
            //    }

            //    return (textPreambleTypes.Count != 0);
            //}

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
            [M(O.AggressiveInlining)] public bool TryGet( IList< word_t > words, ICollection< TextPreambleTypeEnum > textPreambleTypes )
            {
                textPreambleTypes.Clear();

                var startIndex = 0;
                for ( WordsChainDictionary _this = this, _this_next; ; )
                {
                    #region [.get.]
                    if ( _this._TextPreambleType.HasValue )
                    {
                        textPreambleTypes.Add( _this._TextPreambleType.Value );
                    }
                    #endregion

                    if ( words.Count == startIndex )
                    {
                        break;
                    }

                    var word = words[ startIndex ].valueUpper;
                    if ( !_this._Slots.TryGetValue( word, out _this_next ) )
                    {
                        break;
                    }
                    _this = _this_next;
                    startIndex++;
                }

                return (textPreambleTypes.Count != 0);
            }
            #endregion
#if DEBUG
            public override string ToString() => (_TextPreambleType.HasValue ? _TextPreambleType.Value.ToString() : $"count: {_Slots.Count}");
#endif
        }

        #region [.cctor().]
        private const int BANK_CODE_LEN          = 8;
        private const int ACCOUNT_NUMBER_MAX_LEN = 10;
        private const int BANK_NAME_OR_ACCOUNT_OWNER_MAX_WORD_COUNT = 25;

        private static WordsChainDictionary _TextPreambles;
        private static Set< char >          _AllowedPunctuation_AfterTextPreamble;
        static BankAccountsSearcher_ByTextPreamble()
        {
            Init_TextPreambles();

            _AllowedPunctuation_AfterTextPreamble = xlat.GetHyphens().Concat( new[] { ':', '.' } ).ToSet();
        }

        private static void Init_TextPreambles()
        {
            _TextPreambles = new WordsChainDictionary();

            _TextPreambles.Add_WithDot( "Bankleitzahl", TextPreambleTypeEnum.BankCode );
            _TextPreambles.Add_WithDot( "BLZ"         , TextPreambleTypeEnum.BankCode );

            _TextPreambles.Add_WithDot( "Kontonummer", TextPreambleTypeEnum.AccountNumber );
            _TextPreambles.Add_WithDot( "KontoNr"    , TextPreambleTypeEnum.AccountNumber );
            _TextPreambles.Add_WithDot( "Konto-Nr"   , TextPreambleTypeEnum.AccountNumber );
            _TextPreambles.Add_WithDot( "Konto"      , TextPreambleTypeEnum.AccountNumber );
            _TextPreambles.Add_WithDot( "Kto"        , TextPreambleTypeEnum.AccountNumber );

            
            _TextPreambles.Add_WithDot( "Bank"        , TextPreambleTypeEnum.BankName );
            _TextPreambles.Add_WithDot( "Geldinstitut", TextPreambleTypeEnum.BankName );

            _TextPreambles.Add_WithDot( "Kontoinhaber", TextPreambleTypeEnum.AccountOwner );

            using ( var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate() )
            {
                _TextPreambles.Add_WithDot( tokenizer.Run_NoSentsNoUrlsAllocate( "Kto.-Nr" ), TextPreambleTypeEnum.AccountNumber );
                _TextPreambles.Add_WithDot( tokenizer.Run_NoSentsNoUrlsAllocate( "Kto - Nr" ), TextPreambleTypeEnum.AccountNumber );
            }
        }
        #endregion

        #region [.ctor().]
        private IBankAccountsModel _Model;
        private StringBuilder      _Buffer;
        public BankAccountsSearcher_ByTextPreamble( IBankAccountsModel model )
        {
            _Model  = model;
            _Buffer = new StringBuilder( 100 );
        }
        #endregion

        [M(O.AggressiveInlining)] private bool IsAllowedPunctuation_AfterTextPreamble( word_t w ) => ((w.length == 1) && _AllowedPunctuation_AfterTextPreamble.Contains( w.valueUpper[ 0 ] ));
        [M(O.AggressiveInlining)] private int TryFindBankCode( List< word_t > words, int startIndex, out string bankCode )
        {
            var wordCount = 0;
            for ( var len = words.Count; startIndex < len; startIndex++  )
            {
                var w = words[ startIndex ];
                if ( !w.IsExtraWordTypeIntegerNumber() )
                {
                    break;
                }

                wordCount++;
                _Buffer.Append( w.valueOriginal );
                if ( BANK_CODE_LEN < _Buffer.Length )
                {
                    bankCode = default;
                    return (0);
                }
            }

            if ( _Buffer.Length == BANK_CODE_LEN )
            {
                bankCode = _Buffer.ToString(); _Buffer.Clear();
                if ( _Model.IsBankCode( bankCode ) )
                {
                    return (wordCount);
                }
            }
            bankCode = default;
            return (0);
        }
        [M(O.AggressiveInlining)] private int TryFindAccountNumber( List< word_t > words, int startIndex, out string accountNumber )
        {
            var wordCount = 0;
            for ( var len = words.Count; startIndex < len; startIndex++  )
            {
                var w = words[ startIndex ];
                if ( !w.IsExtraWordTypeIntegerNumber() )
                {
                    break;
                }

                wordCount++;
                _Buffer.Append( w.valueOriginal );
                if ( ACCOUNT_NUMBER_MAX_LEN < _Buffer.Length )
                {
                    accountNumber = default;
                    return (0);
                }
            }

            if ( _Buffer.Length <= ACCOUNT_NUMBER_MAX_LEN )
            {
                accountNumber = _Buffer.ToString(); _Buffer.Clear();
                return (wordCount);
            }
            accountNumber = default;
            return (0);
        }
        [M(O.AggressiveInlining)] private int TryFindBankName( List< word_t > words, int startIndex )
        {
            var wordCount = 0;
            for ( var len = words.Count; startIndex < len; startIndex++  )
            {
                var w = words[ startIndex ];
                var (onlyLetters, hasHyphenOrDot) = StringsHelper.ContainsOnlyLettersOrHyphenOrDot( w.valueOriginal );
                if ( (!onlyLetters && !hasHyphenOrDot) || _TextPreambles.Contains_AsFirstInChain( w ) )
                {
                    break;
                }

                if ( BANK_NAME_OR_ACCOUNT_OWNER_MAX_WORD_COUNT < ++wordCount )
                {
                    return (0);
                }
            }

            return (wordCount);
        }
        [M(O.AggressiveInlining)] private int TryFindAccountOwner( List< word_t > words, int startIndex )
        {
            var wordCount = 0;
            for ( var len = words.Count; startIndex < len; startIndex++  )
            {
                var w = words[ startIndex ];
                var ct = StringsHelper.GetCharTypes( w.valueOriginal );
                if ( (ct & CharType.IsLetter) != CharType.IsLetter || _TextPreambles.Contains_AsFirstInChain( w ) )
                {
                    break;
                }

                if ( BANK_NAME_OR_ACCOUNT_OWNER_MAX_WORD_COUNT < ++wordCount )
                {
                    return (0);
                }
            }

            return (wordCount);
        }
        
        public bool TryFindAll( List< word_t > words, out IReadOnlyCollection< ByTextPreamble_SearchResult > results )
        {
            var ss = default(SortedSetByRef< ByTextPreamble_SearchResult >);
            var bankAccountValue = default(string);

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

                int wordCount;
                switch ( x.textPreambleType )
                {
                    case TextPreambleTypeEnum.BankCode:
                        wordCount = TryFindBankCode( words, startIndex, out bankAccountValue );
                    break;

                    case TextPreambleTypeEnum.AccountNumber:
                        wordCount = TryFindAccountNumber( words, startIndex, out bankAccountValue );
                    break;

                    case TextPreambleTypeEnum.BankName:
                        wordCount = TryFindBankName( words, startIndex );
                    break;

                    case TextPreambleTypeEnum.AccountOwner:
                        wordCount = TryFindAccountOwner( words, startIndex );
                    break;

                    default: wordCount = 0; break;
                }

                if ( 0 < wordCount )
                {
                    if ( ss == null ) ss = new SortedSetByRef< ByTextPreamble_SearchResult >( ByTextPreamble_SearchResult.Comparer.Instance );

                    ss.AddEx( startIndex, wordCount, x.textPreambleType, preambleWordIndex, bankAccountValue );

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
        [M(O.AggressiveInlining)] public static void Add_WithDot( this BankAccountsSearcher_ByTextPreamble.WordsChainDictionary wd, string key, TextPreambleTypeEnum tpt )
        {
            StringsHelper.ToUpperInvariantInPlace( key );
            wd.Add( key, tpt );
            wd.Add( key + '.', tpt );
        }
        [M(O.AggressiveInlining)] public static void Add_WithDot( this BankAccountsSearcher_ByTextPreamble.WordsChainDictionary wd, IList< word_t > words, TextPreambleTypeEnum tpt )
        {
            if ( words.Any() )
            {
                wd.Add( words, tpt );
                words.Last().valueUpper += '.';
                wd.Add( words, tpt );
            }
        }
        [M(O.AggressiveInlining)] public static void AddEx( this SortedSetByRef< ByTextPreamble_SearchResult > ss, int startIndex, int length
            , TextPreambleTypeEnum tpt, int preambleWordIndex, string bankAccountValue )
        {
            var sr = new ByTextPreamble_SearchResult( startIndex, length, tpt, preambleWordIndex, bankAccountValue );
            if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
            {
                ss.Remove( in exists );
            }
            ss.Add( in sr );
        }        
    }
}