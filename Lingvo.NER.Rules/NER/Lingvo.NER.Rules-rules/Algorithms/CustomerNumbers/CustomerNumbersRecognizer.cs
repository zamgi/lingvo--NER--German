using System.Collections.Generic;
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
    unsafe internal sealed class CustomerNumbersRecognizer
    {
        #region [.ctor().]
        private const char   SPACE     = ' ';
        private const string SPACE_STR = " ";
        private CustomerNumbersSearcher_ByTextPreamble       _ByTextPreambleSearcher;
        private StringBuilder                                _ValueUpperBuff;
        private StringBuilder                                _ValueOriginalBuff;
        private DirectAccessList< CustomerNumberValueTuple > _SeqWords;
        public CustomerNumbersRecognizer()
        {
            _ByTextPreambleSearcher = new CustomerNumbersSearcher_ByTextPreamble();

            _ValueUpperBuff    = new StringBuilder( 100 );
            _ValueOriginalBuff = new StringBuilder( 100 );
            _SeqWords          = new DirectAccessList< CustomerNumberValueTuple >( 100 );
        }
        #endregion

        #region [.By-Text-Preamble.]
        private void Run_ByTextPreambleSearcher( List< word_t > words )
        {
            if ( _ByTextPreambleSearcher.TryFindAll( words, out var results ) )
            {
                foreach ( var sr in results )
                {
                    var w1 = words[ sr.StartIndex ];
                    switch ( sr.TextPreambleType )
                    {
                        default:
                        case TextPreambleTypeEnum.CustomerNumber:
                            if ( CanProcess( w1 ) )
                            {
                                _SeqWords.AddByRef( CreateCustomerNumberValueTuple( words, w1, in sr ) );
                            }
                        break;
                    }
                }

                Merge_SeqWords( words );

                #region [.remove merged words.]
                words.RemoveWhereValueOriginalIsNull();
                #endregion
            }
        }
        private void Merge_SeqWords( List< word_t > words )
        {
            var len = _SeqWords.Count;
            if ( 0 < len )
            {
                if ( 1 < len )
                {
                    if ( CustomerNumberValuesMerger.TryFindAll( _SeqWords, out var results ) )
                    {
                        foreach ( var sr in results )
                        {
                            if ( !Is_SeqWords_Has_Max_Distance_Between_Words( in sr ) ) continue;

                            Merge_SeqWords( words, in sr );
                        }
                    }
                }

                ReNer_NotCompletedSeqWords();

                _SeqWords.Clear();
            }
        }
        [M(O.AggressiveInlining)] private bool Is_SeqWords_Has_Max_Distance_Between_Words( in CustomerNumberValue_SearchResult sr )
        {
            const int MAX_DISTANCE_BETWEEN_WORDS_IN_SEQ = 5;

            var t = _SeqWords[ sr.StartIndex ];
            if ( t.word.valueOriginal == null )
            {
                return (false);
            }
            for ( int i = sr.StartIndex + 1, len = sr.EndIndex(); i < len; i++ )
            {
                ref readonly var t_prev = ref _SeqWords._Items[ i ];
                if ( (t_prev.word.valueOriginal == null) ||
                     MAX_DISTANCE_BETWEEN_WORDS_IN_SEQ < (t.sr.StartIndex - t_prev.sr.EndIndex()) )
                {
                    return (false);
                }
                t = t_prev;
            }

            return (true);
        }
        [M(O.AggressiveInlining)] private void Merge_SeqWords( List< word_t > words, in CustomerNumberValue_SearchResult sr )
        {
            ref readonly var t1 = ref _SeqWords._Items[ sr.StartIndex + sr.Length - 1 ];
            if ( t1.word.valueOriginal == null ) return;
            var cnw = new CustomerNumberWord( words[ t1.sr.PreambleWordIndex ].startIndex /*t1.word.startIndex*/ );//, CustomerNumberTypeEnum.CustomerNumber );

            #region [.#1.]
            for ( int i = sr.StartIndex, len = sr.EndIndex(); i < len; i++ )
            {
                ref readonly var t = ref _SeqWords._Items[ i ];
                switch ( t.sr.TextPreambleType )
                {
                    case TextPreambleTypeEnum.CustomerNumber: 
                        cnw.CustomerNumber = t.word.CustomerNumber;
                        break;
                }
            }
            #endregion

            #region [.#2.]
            ref readonly var t_end = ref _SeqWords._Items[ sr.StartIndex ];

            var w = default(word_t);
            for ( int i = t1.sr.PreambleWordIndex, len = t_end.sr.EndIndex(); i < len; i++ )
            {                
                w = words[ i ];
                _ValueUpperBuff   .Append( w.valueUpper    ).Append( SPACE );
                _ValueOriginalBuff.Append( w.valueOriginal ).Append( SPACE );
                w.ClearValuesAndNerChain();
            }
            #endregion

            cnw.valueOriginal = _ValueOriginalBuff.ToString( 0, _ValueOriginalBuff.Length - 1 );
            cnw.valueUpper    = _ValueUpperBuff   .ToString( 0, _ValueUpperBuff   .Length - 1 );
            cnw.length        = (w.startIndex - cnw.startIndex) + w.length;

            words[ t1.sr.PreambleWordIndex ] = cnw;

            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();
        }
        private void ReNer_NotCompletedSeqWords()
        {
            for ( var i = _SeqWords.Count - 1; 0 <= i; i-- )
            {
                ref readonly var t = ref _SeqWords._Items[ i ];

                var w = t.word;
                if ( w.CustomerNumber.IsNullOrEmpty() )
                {
                    w.ClearOutputTypeAndNerChain();
                }
            }
        }
        #endregion

        [M(O.AggressiveInlining)] public void Run( List< word_t > words ) => Run_ByTextPreambleSearcher( words );

        [M(O.AggressiveInlining)] private static bool CanProcess( word_t w ) => ((w.valueUpper != null) && w.IsOutputTypeOther()); //!w.IsCustomerNumber());
        [M(O.AggressiveInlining)] private CustomerNumberValueTuple CreateCustomerNumberValueTuple( List< word_t > words, word_t w1, in ByTextPreamble_SearchResult sr )
        {
            #region comm.
            //static void remove_space_around_punctuation( StringBuilder buff )
            //{
            //    for ( int i = 0, len = buff.Length - 1; i <= len; i++ )
            //    {
            //        if ( buff[ i ].IsPunctuation() )
            //        {
            //            if ( (0 < i) && buff[ i - 1 ].IsWhiteSpace() )
            //            {
            //                buff.Remove( i - 1, 1 );
            //                len--;
            //                i--;
            //            }

            //            if ( (i < len) && buff[ i + 1 ].IsWhiteSpace() )
            //            {
            //                buff.Remove( i + 1, 1 );
            //                len--;
            //                i--;
            //            }
            //        }
            //    }
            //};
            #endregion

            var t = default(word_t);
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                t = words[ i ];
                _ValueUpperBuff   .Append( t.valueUpper    ).Append( SPACE );
                _ValueOriginalBuff.Append( t.valueOriginal ).Append( SPACE );
                t.ClearValuesAndNerChain();
            }

            //remove_space_around_punctuation( _ValueUpperBuff    );
            //remove_space_around_punctuation( _ValueOriginalBuff );
            _ValueUpperBuff   .Replace( SPACE_STR, string.Empty );
            _ValueOriginalBuff.Replace( SPACE_STR, string.Empty );

            var cnw = new CustomerNumberWord( w1.startIndex )
            {
                valueOriginal = _ValueOriginalBuff.ToString(),
                valueUpper    = _ValueUpperBuff   .ToString(),
                length        = (t.startIndex - w1.startIndex) + t.length,
            };
            switch ( sr.TextPreambleType )
            {
                case TextPreambleTypeEnum.CustomerNumber: 
                    cnw.CustomerNumber = cnw.valueOriginal; 
                    break; 
            }
            words[ sr.StartIndex ] = cnw;
            words[ sr.PreambleWordIndex ].ClearOutputTypeAndNerChain();

            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();

            return (new CustomerNumberValueTuple( cnw, in sr ));
        }
    }
}
