using System;
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
    unsafe internal sealed class TaxIdentificationsSearcher_ByTextPreamble_Old
    {
        #region [.cctor().]
        private static WordsChainDictionary _TextPreambles;
        private static Set< char >          _AllowedPunctuation_BetweenDigits;
        private static Set< char >          _AllowedPunctuation_AfterTextPreamble;
        static TaxIdentificationsSearcher_ByTextPreamble_Old()
        {
            Init_TextPreambles();

            _AllowedPunctuation_BetweenDigits     = xlat.GetHyphens().Concat( new[] { '/' } ).ToSet();
            _AllowedPunctuation_AfterTextPreamble = xlat.GetHyphens().Concat( new[] { ':' } ).ToSet();
        }

        private static void Init_TextPreambles()
        {
            var words = new[] 
            { 
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
        private ITaxIdentificationsModel _Model;
        public TaxIdentificationsSearcher_ByTextPreamble_Old( ITaxIdentificationsModel model )
        {
            _Buffer = new StringBuilder( 100 );
            _Model  = model ?? throw (new ArgumentNullException( nameof(model) ));
        }
        #endregion

        [M(O.AggressiveInlining)] private static bool IsDigit( char ch ) => ('0' <= ch) && (ch <= '9');
        [M(O.AggressiveInlining)] private static bool ContainsOnlyDigits( string value )
        {
            fixed ( char* base_ptr = value )
            {
                for ( var value_ptr = base_ptr; ; value_ptr++ )
                {
                    var ch = *value_ptr;
                    if ( ch == '\0' )
                    {
                        return (true);
                    }
                    if ( !IsDigit( ch ) )
                    {
                        return (false);
                    }                    
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

        [M(O.AggressiveInlining)] private bool CheckTaxIdentification( string taxIdentification ) 
        {
            //The Tax No. can vary in length from 10-13 digits.
            const int MIN_LENGTH = 10;
            const int MAX_LENGTH = 13;

            if ( (taxIdentification.Length < MIN_LENGTH) || (MAX_LENGTH < taxIdentification.Length) )
            {
                return (false);
            }

            const int PREAMBLE_LEN_2 = 2;
            const int PREAMBLE_LEN_3 = 3;

            var s       = _Buffer.Clear().Append( taxIdentification, 0, PREAMBLE_LEN_3 ).ToString();
            var success = _Model.IsOldTaxIdentifications( s, out var length ) && (PREAMBLE_LEN_3 == length);
            if ( !success )
            {
                s       = _Buffer.Clear().Append( taxIdentification, 0, PREAMBLE_LEN_2 ).ToString();
                success = _Model.IsOldTaxIdentifications( s, out length ) && (PREAMBLE_LEN_2 == length);
            }
#if DEBUG
            Debug.Assert( !success || (length == PREAMBLE_LEN_2 || length == PREAMBLE_LEN_3) );
#endif
            _Buffer.Clear();
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
                    if ( (3 < w.length) && ContainsOnlyDigits( w.valueOriginal ) )
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
                if ( (startIndex < len) && IsAllowedPunctuation_AfterTextPreamble( words[ startIndex ] ) )
                {
                    startIndex++;
                }

                var wordCount = TryFindTaxIdentification( words, startIndex, out var taxIdentification );
                if ( (0 < wordCount) && CheckTaxIdentification( taxIdentification ) )
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
}