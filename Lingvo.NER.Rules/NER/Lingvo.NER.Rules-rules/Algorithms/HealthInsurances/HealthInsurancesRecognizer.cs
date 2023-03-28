using System;
using System.Collections.Generic;
using System.Diagnostics;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.HealthInsurances
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class HealthInsurancesRecognizer
    {
        #region [.ctor().]
        public HealthInsurancesRecognizer() { }
        #endregion

        public void Run( List< word_t > words )
        {
            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                var w1 = words[ index ];
                if ( CanProcess( w1 ) )
                {
                    TryCreateHealthInsuranceWord( words, w1, index );
                }
            }
        }

        [M(O.AggressiveInlining)] private static bool IsDigit( char ch ) => ('0' <= ch) && (ch <= '9');
        [M(O.AggressiveInlining)] private static bool IsLetter( char ch ) => ('A' <= ch) && (ch <= 'Z');
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

        [M(O.AggressiveInlining)] private static bool CheckByCheckDigit( string healthInsuranceNumber )
        {
            #region [.last digit.]
            var endIndex  = healthInsuranceNumber.Length - 1;
            var last_char = healthInsuranceNumber[ endIndex ];
            if ( !IsDigit( last_char ) )
            {
                return (false);
            }
            #endregion

            #region [.first letter.]
            var number = healthInsuranceNumber[ 0 ] - 'A' + 1;
#if DEBUG
            Debug.Assert( IsLetter( healthInsuranceNumber[ 0 ] ) && (1 <= number) && (number <= 26) );
#endif
            var sum = 0;
                 if ( 20 <= number ) sum += CrossSum_Full( 1 * 2 ) + CrossSum_Full( 2 * (number % 10) );
            else if ( 10 <= number ) sum += CrossSum_Full( 1 * 1 ) + CrossSum_Full( 2 * (number % 10) );
            else                     sum += CrossSum_Full( 1 * 0 ) + CrossSum_Full( 2 * number );

            //var sum = CrossSum( number );
            #endregion

            var multiplier = 1;
            for ( var i = 1; i < endIndex; i++ )
            {
                var ch = healthInsuranceNumber[ i ];
                if ( !IsDigit( ch ) )
                {
                    return (false);
                }

                number = (ch - '0') * multiplier;
                sum += CrossSum_Full( number );

                //multiplier = (multiplier == 1) ? 2 : 1;
                multiplier ^= 0x3;
            }

            var checkDigit      = (sum % 10);
            var last_checkDigit = last_char - '0';
            var success         = (checkDigit == last_checkDigit);
            return (success);
        }
        [M(O.AggressiveInlining)] private static bool CheckHealthInsuranceNumber( string healthInsuranceNumber )
        {
            /*
            Person Code (9 digits + 1 check number):
                It consists of – as mentioned - 10 alphanumeric digits.
                First digit is always a letter (All letters are possible except for German Umlaute Ä, Ö, Ü and ß)
                The last digit is the check number and always a number (see below).
                Then follow 8 random numbers
                Digit 10 is a check number based on the so called Modulo-10 procedure which is a modified Luhn algorithm with weight on 1-2-1-2-1-2-1-2-1-2.
                    The letter is replaced by a number (A=01, B=02 ... Z=26)
                    This 2-digit number and the 8 random numbers are multiplied alternating from left to right with 1 and 2 (as mentioned above)
                    From each product the cross sum is calculated. Then the sum of these 10 cross sums is calculated.
                    The last digit of this sum is the check number.
                        Example:
                            I52606455
                        Letter I=09
                            (0*1)+(9*2)+(5*1)+(2*2)+(6*1)+(0*2)+(6*1)+(4*2)+(5*1)+(5*2)
                        Multiplied: 0+18+5+4+6+0+6+8+5+10
                        Cross Sums 0+9+5+4+6+0+6+8+5+1
                        Sum: 44
                        Check Number: 4
                Examples check number correct:
                    I526064554        
            */
            if ( (healthInsuranceNumber.Length == 10) && IsLetter( healthInsuranceNumber[ 0 ] ) && CheckByCheckDigit( healthInsuranceNumber ) )
            {
                return (true);
            }

            return (false);
        }

        [M(O.AggressiveInlining)] private static bool CanProcess( word_t w ) => ((w.valueUpper != null) && w.IsOutputTypeOther()); //!w.IsHealthInsurance());
        [M(O.AggressiveInlining)] private void TryCreateHealthInsuranceWord( List< word_t > words, word_t w1, int startIndex )
        {
            if ( CheckHealthInsuranceNumber( w1.valueOriginal ) )
            {
                var hiw = new HealthInsuranceWord( w1.startIndex, w1.valueOriginal )
                {
                    valueOriginal = w1.valueOriginal,
                    valueUpper    = w1.valueUpper,
                    length        = w1.length,
                };
                w1.ClearValuesAndNerChain();
                words[ startIndex ] = hiw;
            }
        }
    }
}
