using System;
using System.Collections.Generic;
using System.Diagnostics;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.PassportIdCardNumbers
{
    /// <summary>
    /// 
    /// </summary>
    unsafe internal sealed class PassportIdCardNumbersRecognizer
    {
        #region [.ctor().]
        private static Set< char > _StartChars_4_New;
        private static Set< char > _ContainsChars_4_New;
        private static Set< char > _StartChars_4_Child;
        private static CharType* _CTM;
        static PassportIdCardNumbersRecognizer()
        {
            _CTM = xlat_Unsafe.Inst._CHARTYPE_MAP;
            _StartChars_4_New    = new Set< char >( new[] { 'C', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'R', 'T', 'V', 'W', 'X', 'Y' } );
            _ContainsChars_4_New = new Set< char >( new[] { 'C', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'R', 'T', 'V', 'W', 'X', 'Y', 'Z' } );
            _StartChars_4_Child  = new Set< char >( new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G' } );
        }

        private IPassportIdCardNumbersModel _Model;
        public PassportIdCardNumbersRecognizer( IPassportIdCardNumbersModel model ) => _Model = model ?? throw (new ArgumentNullException( nameof(model) ));
        #endregion

        public void Run( List< word_t > words )
        {
            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                var w1 = words[ index ];
                if ( CanProcess( w1 ) )
                {
                    TryCreatePassportIdCardNumberWord( words, w1, index );
                }
            }
        }

        [M(O.AggressiveInlining)] private static bool IsDigit( char ch ) => ('0' <= ch) && (ch <= '9');
        [M(O.AggressiveInlining)] private static bool HasOnlyDigits( string s, int startIndex )
        {
            for ( var i = s.Length - 1; startIndex <= i; i-- )
            {
                if ( !IsDigit( s[ i ] ) )
                {
                    return (false);
                }
            }
            return (true);
        }
        [M(O.AggressiveInlining)] private static bool HasOnlyCharsOrDigits_4_New( string s, int startIndex )
        {
            var has_any_digit  = false;
            var has_any_letter = false;
            for ( var i = s.Length - 1; startIndex <= i; i-- )
            {
                var ch = s[ i ];
                if ( IsDigit( ch ) )
                {
                    has_any_digit = true;
                }
                else if ( _ContainsChars_4_New.Contains( ch ) )
                {
                    has_any_letter = true;
                }
                else
                {
                    return (false);
                }
            }
            return (has_any_digit && has_any_letter);
        }
        [M(O.AggressiveInlining)] private static bool CheckByCheckDigit( string s )
        {
            static int get_multiplier( ref int index )
            {
                if ( 2 < ++index ) index = 0;
                switch ( index )
                {
                    case 0: return (7);
                    case 1: return (3);
                    case 2: return (1);
                    default: throw (new ArgumentException( nameof(index) ));
                }
            };

            var last_char = s[ s.Length - 1 ];
#if DEBUG
            Debug.Assert( IsDigit( last_char ) );
#endif
            var last_CheckDigit = last_char - '0';

            var sum = 0;
            for ( int i = 0, idx = -1, len = s.Length - 1 - 1; i <= len; i++ )
            {
                var ch = s[ i ];
                int num;
                if ( IsDigit( ch ) ) //digit
                {
                    num = ch - '0';
                }
                else //letter
                {
#if DEBUG
                    Debug.Assert( ('A' <= ch) && (ch <= 'Z') ); 
#endif
                    num = ch - 'A' + 10;
                }

                sum += num * get_multiplier( ref idx ); 
            }

            var checkDigit = (sum % 10);
            var success = (checkDigit == last_CheckDigit);
            return (success);
        }

        [M(O.AggressiveInlining)] private bool Is_Valid_4_Child( string passportIdCardNumber )
            => _StartChars_4_Child.Contains( passportIdCardNumber[ 0 ] ) && HasOnlyDigits( passportIdCardNumber, 1 );

        [M(O.AggressiveInlining)] private bool Is_Valid_4_Old( string passportIdCardNumber )
            => _Model.IsOldPassportIdCardNumbers( passportIdCardNumber, out var length ) && HasOnlyDigits( passportIdCardNumber, length );

        [M(O.AggressiveInlining)] private bool Is_Valid_4_New( string passportIdCardNumber )
            => _StartChars_4_New.Contains( passportIdCardNumber[ 0 ] ) && HasOnlyCharsOrDigits_4_New( passportIdCardNumber, 1 );

        [M(O.AggressiveInlining)] private bool CheckPassportIdCardNumbers( string passportIdCardNumber )
        {
            /*
            [Новые номера ID-карт и паспортов]
            1) Могут использваться (содержать)  только цифры или следующие буквы: C, F, G, H, J, K, L, M, N, P, R, T, V, W, X, Y, Z
            2) Номера паспортов, ID-карт состоят из 9 символов либо – с добавлением контрольной цифры в конце – из 10 символов. 
               Всегда начитаются с одной из букв C, F, G, H, J, K, L, M, N, P, R, T, V, W, X, Y
            3) Временные и детские документы начинаются с одной из букв A, B, C, D, E, F, G, за которыми следует последовательность из 7 цифр (либо из 8 с контрольной цифрой в конце)      

            [Старые номера ID-карт и паспортов]
            1) состоят из 9 символов либо – с добавлением контрольной цифры в конце – из 10 символов
            2) Словарь кодов-префиксов смотри в файле "old_PassportIdCardNumbers.txt"
            */
            switch ( passportIdCardNumber.Length )
            {
                //Child
                case 8:
                    if ( Is_Valid_4_Child( passportIdCardNumber ) )
                    {
                        return (true);
                    }
                break;
                
                //Old - New - [Child + CheckDigit]
                case 9:
                    //[Child + CheckDigit]
                    if ( Is_Valid_4_Child( passportIdCardNumber ) && CheckByCheckDigit( passportIdCardNumber ) )
                    {
                        return (true);
                    }

                    //Old
                    if ( Is_Valid_4_Old( passportIdCardNumber ) )
                    {
                        return (true);
                    }

                    //New
                    if ( Is_Valid_4_New( passportIdCardNumber ) )
                    {
                        return (true);
                    }
                break;

                //Old-New + CheckDigit
                case 10:
                    //Old + CheckDigit
                    if ( Is_Valid_4_Old( passportIdCardNumber ) && CheckByCheckDigit( passportIdCardNumber ) )
                    {
                        return (true);
                    }

                    //New + CheckDigit
                    if ( Is_Valid_4_New( passportIdCardNumber ) && CheckByCheckDigit( passportIdCardNumber ) )
                    {
                        return (true);
                    }
                break;
            }

            return (false);
        }

        [M(O.AggressiveInlining)] private static bool CanProcess( word_t w ) => ((w.valueUpper != null) && w.IsOutputTypeOther()); //!w.IsPassportIdCardNumber());
        [M(O.AggressiveInlining)] private void TryCreatePassportIdCardNumberWord( List< word_t > words, word_t w1, int startIndex )
        {
            if ( CheckPassportIdCardNumbers( w1.valueOriginal ) )
            {
                var pnw = new PassportIdCardNumberWord( w1.startIndex, w1.valueOriginal )
                {
                    valueOriginal = w1.valueOriginal,
                    valueUpper    = w1.valueUpper,
                    length        = w1.length,
                };
                w1.ClearValuesAndNerChain();
                words[ startIndex ] = pnw;
            }
        }
    }
}
