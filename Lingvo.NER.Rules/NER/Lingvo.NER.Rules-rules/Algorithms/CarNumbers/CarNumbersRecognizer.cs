using System;
using System.Collections.Generic;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.CarNumbers
{
    /// <summary>
    /// 
    /// </summary>
    unsafe internal sealed class CarNumbersRecognizer
    {
        #region [.cctor().]
        private static CharType* _CTM;
        static CarNumbersRecognizer() => _CTM = xlat_Unsafe.Inst._CHARTYPE_MAP;
        #endregion

        #region [.ctor().]
        private ICarNumbersModel _Model;
        private StringBuilder    _ValueUpperBuff;
        private StringBuilder    _ValueOriginalBuff;
        public CarNumbersRecognizer( ICarNumbersModel model )
        {
            _Model             = model ?? throw (new ArgumentNullException( nameof(model) ));
            _ValueUpperBuff    = new StringBuilder( 100 );
            _ValueOriginalBuff = new StringBuilder( 100 );
        }
        #endregion

        public void Run( List< word_t > words )
        {
            if ( CarNumbersSearcher.TryFindAll( words, out var results ) )
            {
                foreach ( var sr in results )
                {
                    var w1 = words[ sr.StartIndex ];
                    if ( CanProcess( w1 ) )
                    {
                        TryCreateCarNumberWord( words, w1, in sr );
                    }
                }

                #region [.remove merged words.]
                words.RemoveWhereValueOriginalIsNull();
                #endregion
            }
        }

        [M(O.AggressiveInlining)] private static bool CanProcess( word_t w ) => ((w.valueUpper != null) && w.IsOutputTypeOther()); //!w.IsCarNumber());
        [M(O.AggressiveInlining)] private static bool IsDigit( char ch ) => ('0' <= ch) && (ch <= '9');
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

        [M(O.AggressiveInlining)] private void TryCreateCarNumberWord( List< word_t > words, word_t w1, in SearchResult sr )
        {
            #region [.get preamble.]
            //area code (1-3 letters)
            const int MAX_PREAMLBE_LEN = 3;

            foreach ( var ch in w1.valueOriginal )
            {
                var ct = _CTM[ ch ];
                if ( (ct & CharType.IsLetter) != CharType.IsLetter )
                {
                    if ( (ct & CharType.IsHyphen) == CharType.IsHyphen )
                    {
                        break;
                    }
                    _ValueOriginalBuff.Clear();
                    return;
                }

                _ValueOriginalBuff.Append( ch );

                if ( MAX_PREAMLBE_LEN < _ValueOriginalBuff.Length )
                {
                    _ValueOriginalBuff.Clear();
                    return;
                }
            }
            #endregion

            var preamble = _ValueOriginalBuff.ToString(); _ValueOriginalBuff.Clear();
            if ( _Model.IsCarNumberPreamble( preamble ) && IsLastWordEndsWithDigit( words, in sr ) )
            {
                if ( IsLastWordStickToNextNotPunctuation( words, in sr ) ) //---if ( IsLastWordStickToNext( words, in sr ) )
                {
                    var endIndex = sr.EndIndex();
                    var t = words[ endIndex ];
                    if ( t.length != 1 ) return;
                    var ch = t.valueUpper[ 0 ];
                    if ( (ch != 'E') && (ch != 'H') ) return;

                    for ( int i = sr.StartIndex, j = sr.Length + 1; 0 < j; j--, i++ )
                    {
                        t = words[ i ];
                        _ValueUpperBuff   .Append( t.valueUpper    );
                        _ValueOriginalBuff.Append( t.valueOriginal );
                        t.ClearValuesAndNerChain();
                    }
                    var carNumber = _ValueUpperBuff.ToString();

                    var pcw = new CarNumberWord( w1.startIndex, carNumber )
                    {
                        valueOriginal = _ValueOriginalBuff.ToString(),
                        valueUpper    = carNumber,
                        length        = (t.startIndex - w1.startIndex) + t.length,
                    };
                    words[ sr.StartIndex ] = pcw;

                    _ValueUpperBuff   .Clear();
                    _ValueOriginalBuff.Clear();
                }
                else
                {
                    //"MH" + "MT-" + "2105"
                    if ( sr.Length == 3 )
                    {
                        var x = words[ sr.StartIndex + 1 ];
                        if ( (x.length == 3) && !x.valueOriginal.LastChar().IsHyphen() )
                        {
                            return;
                        }
                    }

                    var t = default(word_t);
                    for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
                    {
                        t = words[ i ];
                        _ValueUpperBuff   .Append( t.valueUpper    );
                        _ValueOriginalBuff.Append( t.valueOriginal );
                        t.ClearValuesAndNerChain();
                    }
                    var carNumber = _ValueUpperBuff.ToString();

                    var pcw = new CarNumberWord( w1.startIndex, carNumber )
                    {
                        valueOriginal = _ValueOriginalBuff.ToString(),
                        valueUpper    = carNumber,
                        length        = (t.startIndex - w1.startIndex) + t.length,
                    };
                    words[ sr.StartIndex ] = pcw;

                    _ValueUpperBuff   .Clear();
                    _ValueOriginalBuff.Clear();
                }
            }
        }
    }
}
