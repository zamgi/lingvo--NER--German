using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.CreditCards
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class CreditCardsRecognizer
    {
        #region [.ctor().]
        private StringBuilder _ValueUpperBuff;
        private StringBuilder _ValueOriginalBuff;
        public CreditCardsRecognizer()
        {
            _ValueUpperBuff    = new StringBuilder( 100 );
            _ValueOriginalBuff = new StringBuilder( 100 );
        }
        #endregion

        public void Run( List< word_t > words )
        {
            if ( CreditCardsSearcher.TryFindAll( words, out var results ) )
            {
                foreach ( var sr in results )
                {
                    var w1 = words[ sr.StartIndex ];
                    if ( CanProcess( w1 ) )
                    {
                        CreateCreditCardWord( words, w1, in sr );
                    }
                }

                #region [.remove merged words.]
                words.RemoveWhereValueOriginalIsNull();
                #endregion
            }
        }

        [M(O.AggressiveInlining)] private static bool CheckByLuhn( string cardNumber )
        {
            var last_char = cardNumber[ cardNumber.Length - 1 ];
#if DEBUG
            Debug.Assert( '0' <= last_char && last_char <= '9' );
#endif
            var num = last_char - '0';

            var sum = 0;
            for ( int i = cardNumber.Length - 1 - 1; 0 <= i; i-- )
            {
#if DEBUG
                Debug.Assert( '0' <= cardNumber[ i ] && cardNumber[ i ] <= '9' );
#endif
                var number = cardNumber[ i ] - '0';  // переводим цифру из char в int
                if ( (i % 2) == 0 )                  // если позиция цифры чётное, то:
                {
                    number <<= 1;  // умножаем цифру на 2

                    if ( number > 9 ) // согласно алгоритму, ни одно число не должно быть больше 9
                    {
                        number -= 9; // второй вариант сведения к единичному разряду
                    }
                }

                sum += number; // прибавляем к sum номера согласно алгоритму
            }

            var checkDigit = 10 - (sum % 10);
            if ( checkDigit == 10 ) checkDigit = 0;

            var success = (checkDigit == num);
            return (success);

            //var success = ((sum % 10) == 0); // если проверочная сумма чётно делится на 10, то: номер карты введён верно!
            //return (success);
        }
        [M(O.AggressiveInlining)] private static bool CheckByLuhn__PREV( string cardNumber )
        {
            var sum = 0;
            for ( int i = cardNumber.Length - 1; 0 <= i; i-- )
            {
#if DEBUG
                Debug.Assert( '0' <= cardNumber[ i ] && cardNumber[ i ] <= '9' );
#endif
                var number = cardNumber[ i ] - '0';  // переводим цифру из char в int
                if ( (i % 2) == 0 )                  // если позиция цифры чётное, то:
                {
                    number <<= 1;  // умножаем цифру на 2

                    if ( number > 9 ) // согласно алгоритму, ни одно число не должно быть больше 9
                    {
                        number -= 9; // второй вариант сведения к единичному разряду
                    }
                }

                sum += number; // прибавляем к sum номера согласно алгоритму
            }

            var success = ((sum % 10) == 0); // если проверочная сумма чётно делится на 10, то: номер карты введён верно!
            return (success);
        }

        [M(O.AggressiveInlining)] private static bool CanProcess( word_t w ) => ((w.valueUpper != null) && w.IsOutputTypeOther()); //!w.IsCreditCard());
        [M(O.AggressiveInlining)] private void CreateCreditCardWord( List< word_t > words,  word_t w1, in SearchResult sr )
        {
            var t = default(word_t);
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                t = words[ i ];
                _ValueUpperBuff   .Append( t.valueUpper    );
                _ValueOriginalBuff.Append( t.valueOriginal );
            }

            var creditCardNumber = _ValueUpperBuff.ToString();
            if ( CheckByLuhn( creditCardNumber ) )
            {
                var pcw = new CreditCardWord( w1.startIndex, creditCardNumber )
                {
                    valueOriginal = _ValueOriginalBuff.ToString(),
                    valueUpper    = creditCardNumber,
                    length        = (t.startIndex - w1.startIndex) + t.length,
                };

                for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
                {
                    words[ i ].ClearValuesAndNerChain();
                }

                words[ sr.StartIndex ] = pcw;
            }

            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();
        }
    }
}
