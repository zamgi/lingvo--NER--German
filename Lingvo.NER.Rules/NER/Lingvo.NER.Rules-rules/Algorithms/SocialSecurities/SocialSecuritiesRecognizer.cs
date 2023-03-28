using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.SocialSecurities
{
    /// <summary>
    /// 
    /// </summary>
    unsafe internal sealed class SocialSecuritiesRecognizer
    {
        #region [.cctor().]
        //private static CharType* _CTM;
        private const int    LETTER_MULTIPLIERS_1 = 2;
        private const int    LETTER_MULTIPLIERS_2 = 1;
        private static int[] DIGIT_MULTIPLIERS;
        static SocialSecuritiesRecognizer()
        {
            //_CTM         = xlat_Unsafe.Inst._CHARTYPE_MAP;
            DIGIT_MULTIPLIERS = new[] { 2, 1, 2, 5, 7, 1, 2, 1, /*letters digits one & two*//*2, 1,*/-1, 2, 1 };
        }
        #endregion

        #region [.ctor().]
        private ISocialSecuritiesModel _Model;
        private StringBuilder          _ValueUpperBuff;
        private StringBuilder          _ValueOriginalBuff;
        public SocialSecuritiesRecognizer( ISocialSecuritiesModel model )
        {
            _Model             = model ?? throw (new ArgumentNullException( nameof(model) ));
            _ValueUpperBuff    = new StringBuilder( 100 );
            _ValueOriginalBuff = new StringBuilder( 100 );
        }
        #endregion

        public void Run( List< word_t > words )
        {
            if ( SocialSecuritiesSearcher.TryFindAll( words, out var results ) )
            {
                foreach ( var sr in results )
                {
                    var w1 = words[ sr.StartIndex ];
                    if ( CanProcess( w1 ) )
                    {
                        TryCreateSocialSecurityWord( words, w1, in sr );
                    }
                }

                #region [.remove merged words.]
                words.RemoveWhereValueOriginalIsNull();
                #endregion
            }
        }

        [M(O.AggressiveInlining)] private static bool CanProcess( word_t w ) => ((w.valueUpper != null) && w.IsOutputTypeOther()); //!w.IsSocialSecurity());
        [M(O.AggressiveInlining)] private static bool IsDigit( char ch ) => ('0' <= ch) && (ch <= '9');
        [M(O.AggressiveInlining)] private static bool IsLetter( char ch ) => ('A' <= ch) && (ch <= 'Z');
        [M(O.AggressiveInlining)] private static bool IsDigitOrLetter( char ch ) => (IsDigit( ch ) || IsLetter( ch ));
        //[M(O.AggressiveInlining)] private static bool IsLastWordStickToNext( List< word_t > words, in SearchResult sr )
        //{
        //    var endIndex = sr.EndIndex();            
        //    if ( endIndex < words.Count )
        //    {
        //        var lastIndex = endIndex - 1;
        //        return (words[ lastIndex ].endIndex() == words[ endIndex ].startIndex);
        //    }
        //    return (false);
        //}
        [M(O.AggressiveInlining)] private static bool IsLastWordStickToNextNotPunctuation( List< word_t > words, in SearchResult sr )
        {
            var endIndex = sr.EndIndex();            
            if ( endIndex < words.Count )
            {
                var word_next = words[ endIndex ];
                if ( !word_next.IsExtraWordTypePunctuation() )
                {
                    var lastIndex = endIndex - 1;
                    return (words[ lastIndex ].endIndex() == word_next.startIndex);
                }                
            }
            return (false);
        }
        [M(O.AggressiveInlining)] private static bool IsLastWordEndsWithDigit( List< word_t > words, in SearchResult sr ) => IsDigit( words[ sr.EndIndex() - 1 ].valueOriginal.LastChar() );

        [M(O.AggressiveInlining)] private static int CrossSum( int i )
        {
            var sum = 0;
            for ( ; 0 < i; )
            {
                //sum += (i % 10);
                //i /= 10;

                i = Math.DivRem( i, 10, out var remainder );
                sum += remainder;
            }
            return (sum);
        }
        [M(O.AggressiveInlining)] private static int CrossSum_Full( int i )
        {
            var sum = CrossSum( i );
            for ( ; 9 < sum; )
            {
                sum = CrossSum( sum );
            }
            return (sum);
        }
        [M(O.AggressiveInlining)] private static bool CheckByCheckDigit( string socialSecurityNumber )
        {
            #region [.last digit.]
            var endIndex  = socialSecurityNumber.Length - 1;
            var last_char = socialSecurityNumber[ endIndex ];
            if ( !IsDigit( last_char ) )
            {
                return (false);
            }
#if DEBUG
            Debug.Assert( endIndex == DIGIT_MULTIPLIERS.Length );
#endif
            #endregion

            var sum = 0;
            for ( var i = 0; i < endIndex; i++ )
            {
                var ch = socialSecurityNumber[ i ];
                if ( IsDigit( ch ) )
                {
                    var number = (ch - '0') * DIGIT_MULTIPLIERS[ i ];
                    sum += CrossSum_Full( number );
                }
                else if ( IsLetter( ch ) )
                {
                    var number = (ch - 'A' + 1);
#if DEBUG
                    Debug.Assert( (1 <= number) && (number <= 26) );
#endif
                         if ( 20 <= number ) sum += CrossSum_Full( LETTER_MULTIPLIERS_1 * 2 ) + CrossSum_Full( LETTER_MULTIPLIERS_2 * (number % 10) );
                    else if ( 10 <= number ) sum += CrossSum_Full( LETTER_MULTIPLIERS_1 * 1 ) + CrossSum_Full( LETTER_MULTIPLIERS_2 * (number % 10) );
                    else                     sum += CrossSum_Full( LETTER_MULTIPLIERS_1 * 0 ) + CrossSum_Full( LETTER_MULTIPLIERS_2 * number );
                }
                else
                {
                    return (false);
                }
            }

            var checkDigit      = (sum % 10);
            var last_checkDigit = last_char - '0';
            var success         = (checkDigit == last_checkDigit);
            return (success);
        }

        [M(O.AggressiveInlining)] private void TryCreateSocialSecurityWord( List< word_t > words, word_t w1, in SearchResult sr )
        {
            if ( _Model.IsSocialSecurityPreamble( w1.valueOriginal ) && !IsLastWordStickToNextNotPunctuation( words, in sr ) && IsLastWordEndsWithDigit( words, in sr ) )
            {
                #region [.-1-.]
                var t = default(word_t);
                for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
                {
                    t = words[ i ];
                    foreach ( var ch in t.valueUpper )
                    {
                        if ( IsDigitOrLetter( ch ) )
                        {
                            _ValueUpperBuff.Append( ch );
                        }
                    }
                    _ValueOriginalBuff.Append( t.valueOriginal );
                }
                var socialSecurityNumber = _ValueUpperBuff.ToString();
                #endregion

                #region [.-2-.]
                if ( CheckByCheckDigit( socialSecurityNumber ) )
                {
                    for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
                    {
                        t = words[ i ];
                        _ValueOriginalBuff.Append( t.valueOriginal );
                        t.ClearValuesAndNerChain();
                    }

                    var ssw = new SocialSecurityWord( w1.startIndex, socialSecurityNumber )
                    {
                        valueOriginal = _ValueOriginalBuff.ToString(),
                        valueUpper    = socialSecurityNumber,
                        length        = (t.startIndex - w1.startIndex) + t.length,
                    };

                    words[ sr.StartIndex ] = ssw;
                }
                #endregion

                _ValueUpperBuff   .Clear();
                _ValueOriginalBuff.Clear();
            }
        }
    }
}
