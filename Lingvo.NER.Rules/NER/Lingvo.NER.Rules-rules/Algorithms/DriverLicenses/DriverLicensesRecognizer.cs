using System;
using System.Collections.Generic;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.DriverLicenses
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class DriverLicensesRecognizer
    {
        #region [.ctor().]
        private IDriverLicensesModel _Model;
        public DriverLicensesRecognizer( IDriverLicensesModel model ) => _Model = model ?? throw (new ArgumentNullException( nameof(model) ));
        #endregion

        public void Run( List< word_t > words )
        {
            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                var w1 = words[ index ];
                if ( CanProcess( w1 ) )
                {
                    TryCreateDriverLicenseWord( words, w1, index );
                }
            }
        }

        [M(O.AggressiveInlining)] private static bool IsDigit( char ch ) => ('0' <= ch) && (ch <= '9');
        [M(O.AggressiveInlining)] private static bool IsLetter( char ch ) => ('A' <= ch) && (ch <= 'Z');
        [M(O.AggressiveInlining)] private static bool IsDigitOrLetter( char ch ) => IsDigit( ch ) || IsLetter( ch );
        [M(O.AggressiveInlining)] private static bool CheckByCheckDigit( string driverLicense )
        {
            const int LENGTH = 9;

            var sum = 0;
            for ( var i = 0; i < LENGTH; i++ )
            {
                var ch = driverLicense[ i ];
                int number;
                if ( IsDigit( ch ) )
                {
                    number = ch - '0';
                }
                else if ( IsLetter( ch ) )
                {
                    number = ch - 'A' + 10;
                }
                else
                {
                    return (false);
                }

                number *= (LENGTH - i);
                sum += number;
            }


            var last_char = driverLicense[ LENGTH ];
            var checkDigit = sum % 11;
            if ( checkDigit == 10 )
            {
                return (last_char == 'X');
            }

            var checkNumber = last_char - '0';
            var success     = (checkDigit == checkNumber);
            return (success);
        }
        [M(O.AggressiveInlining)] private bool CheckDriverLicense( string driverLicense )
        {
            /*
            No separation, always in one block. Always exactly 11 digits.

            */
            if ( (driverLicense.Length == 11) &&
                 _Model.IsDriverLicensePreamble( driverLicense, out var len ) && (len == 4) &&
                 CheckByCheckDigit( driverLicense ) &&
                 IsDigitOrLetter( driverLicense[ driverLicense.Length - 1 ] )
               )
            {
                return (true);
            }

            return (false);
        }

        [M(O.AggressiveInlining)] private static bool CanProcess( word_t w ) => ((w.valueUpper != null) && w.IsOutputTypeOther() && (w.nerInputType == NerInputType.NumCapital)); //!w.IsDriverLicense());
        [M(O.AggressiveInlining)] private void TryCreateDriverLicenseWord( List< word_t > words, word_t w1, int startIndex )
        {
            if ( CheckDriverLicense( w1.valueOriginal ) )
            {
                var dlw = new DriverLicenseWord( w1.startIndex, w1.valueOriginal )
                {
                    valueOriginal = w1.valueOriginal,
                    valueUpper    = w1.valueUpper,
                    length        = w1.length,
                };
                w1.ClearValuesAndNerChain();
                words[ startIndex ] = dlw;
            }
        }
    }
}
