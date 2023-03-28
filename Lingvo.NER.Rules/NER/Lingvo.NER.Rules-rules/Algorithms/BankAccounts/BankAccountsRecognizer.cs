using System.Collections.Generic;
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
    internal sealed class BankAccountsRecognizer
    {
        #region [.ctor().]
        private const char   SPACE     = ' ';
        private const string SPACE_STR = " ";
        private BankAccountsSearcher_ByTextPreamble _ByTextPreambleSearcher;
        private StringBuilder                       _ValueUpperBuff;
        private StringBuilder                       _ValueOriginalBuff;
        private DirectAccessList< BankAccountValueTuple > _SeqWords;
        public BankAccountsRecognizer( IBankAccountsModel model )
        {
            _ByTextPreambleSearcher = new BankAccountsSearcher_ByTextPreamble( model );

            _ValueUpperBuff    = new StringBuilder( 100 );
            _ValueOriginalBuff = new StringBuilder( 100 );
            _SeqWords          = new DirectAccessList< BankAccountValueTuple >( 100 );
        }
        #endregion

        #region [.IBAN.]
        private void Run_IBANSearcher( List< word_t > words )
        {
            if ( IBANSearcher.TryFindAll( words, out var results ) )
            {
                foreach ( var sr in results )
                {
                    var w1 = words[ sr.StartIndex ];
                    if ( CanProcess( w1 ) )
                    {
                        CreateBankAccountWord( words, w1, in sr );
                    }
                }

                #region [.remove merged words.]
                words.RemoveWhereValueOriginalIsNull();
                #endregion
            }
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
                        case TextPreambleTypeEnum.BankName:
                        case TextPreambleTypeEnum.AccountOwner:
                            if ( CanProcess_4_BankNameOrAccountOwner( w1 ) )
                            {
                                _SeqWords.AddByRef( CreateBankAccountWord( words, w1, in sr ) );
                            }                        
                        break;

                        default:
                        case TextPreambleTypeEnum.BankCode:
                        case TextPreambleTypeEnum.AccountNumber:
                            if ( CanProcess( w1 ) )
                            {
                                _SeqWords.AddByRef( CreateBankAccountWord( words, w1, in sr ) );
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
                    if ( BankAccountValuesMerger.TryFindAll( _SeqWords, out var results ) )
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
        [M(O.AggressiveInlining)] private bool Is_SeqWords_Has_Max_Distance_Between_Words( in BankAccountValue_SearchResult sr )
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
        [M(O.AggressiveInlining)] private void Merge_SeqWords( List< word_t > words, in BankAccountValue_SearchResult sr )
        {
            ref readonly var t1 = ref _SeqWords._Items[ sr.StartIndex + sr.Length - 1 ];
            if ( t1.word.valueOriginal == null ) return;
            var baw = new BankAccountWord( words[ t1.sr.PreambleWordIndex ].startIndex /*t1.word.startIndex*/, BankAccountTypeEnum.BankCode_AccountNumber );

            #region [.#1.]
            for ( int i = sr.StartIndex, len = sr.EndIndex(); i < len; i++ )
            {
                ref readonly var t = ref _SeqWords._Items[ i ];
                switch ( t.sr.TextPreambleType )
                {
                    case TextPreambleTypeEnum.BankCode     : baw.BankCode      = t.word.BankCode;      break;
                    case TextPreambleTypeEnum.AccountNumber: baw.AccountNumber = t.word.AccountNumber; break;
                    case TextPreambleTypeEnum.BankName     : baw.BankName      = t.word.BankName;      break;
                    case TextPreambleTypeEnum.AccountOwner : baw.AccountOwner  = t.word.AccountOwner;  break;
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

            baw.valueOriginal = _ValueOriginalBuff.ToString( 0, _ValueOriginalBuff.Length - 1 );
            baw.valueUpper    = _ValueUpperBuff   .ToString( 0, _ValueUpperBuff   .Length - 1 );
            baw.length        = (w.startIndex - baw.startIndex) + w.length;

            words[ t1.sr.PreambleWordIndex ] = baw;

            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();
        }
        private void ReNer_NotCompletedSeqWords()
        {
            for ( var i = _SeqWords.Count - 1; 0 <= i; i-- )
            {
                ref readonly var t = ref _SeqWords._Items[ i ];

                var w = t.word;
                if ( w.IBAN.IsNullOrEmpty() && w.BankCode.IsNullOrEmpty() && w.AccountNumber.IsNullOrEmpty() )
                {
                    w.ClearOutputTypeAndNerChain();
                }
            }
        }
        #endregion

        public void Run( List< word_t > words )
        {
            //-1-//
            Run_IBANSearcher( words );

            //-2-//
            Run_ByTextPreambleSearcher( words );
        }

        [M(O.AggressiveInlining)] private static bool CanProcess( word_t w ) => ((w.valueUpper != null) && w.IsOutputTypeOther()); //!w.IsAccountNumber());
        [M(O.AggressiveInlining)] private static bool CanProcess_4_BankNameOrAccountOwner( word_t w )
        {
            if ( w.valueUpper != null )
            {
                switch ( w.nerOutputType )
                {
                    case NerOutputType.Address:
                    case NerOutputType.Url:
                    case NerOutputType.Email:
                    case NerOutputType.PhoneNumber:
                        break;

                    default:
                        return (true);
                }
            }
            return (false);
        }
        [M(O.AggressiveInlining)] private void CreateBankAccountWord( List< word_t > words,  word_t w1, in IBANSearchResult sr )
        {
            var t = default(word_t);
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                t = words[ i ];
                _ValueUpperBuff   .Append( t.valueUpper    ).Append( SPACE );
                _ValueOriginalBuff.Append( t.valueOriginal ).Append( SPACE );
                t.ClearValuesAndNerChain();
            }

            var baw = new BankAccountWord( w1.startIndex, BankAccountTypeEnum.IBAN )
            {
                valueOriginal = _ValueOriginalBuff.ToString( 0, _ValueOriginalBuff.Length - 1 ),
                valueUpper    = _ValueUpperBuff   .ToString( 0, _ValueUpperBuff   .Length - 1 ),
                length        = (t.startIndex - w1.startIndex) + t.length,
            };
            baw.IBAN = _ValueOriginalBuff.Replace( SPACE_STR, string.Empty ).ToString();
            if ( (4 + 4 + 4) <= baw.IBAN.Length )
            {
                baw.BankCode      = baw.IBAN.Substring( 4, 4 + 4 );
                baw.AccountNumber = baw.IBAN.Substring( 4 + 4 + 4 );
            }
            words[ sr.StartIndex ] = baw;

            #region [.re-NER prev preamle-word.]
            static bool is_IBAN_prefix( string s ) => (s == "IBAN");
            static bool is_IBAN_punctuation( word_t x ) => (x.length == 1) && (x.IsExtraWordTypeColon() || x.IsExtraWordTypeDash());

            if ( 0 < sr.StartIndex )
            {
                var w = words[ sr.StartIndex - 1 ];
                if ( (1 < sr.StartIndex) && is_IBAN_punctuation( w ) )
                {
                    w = words[ sr.StartIndex - 2 ];
                    if ( is_IBAN_prefix( w.valueOriginal ) )
                    {
                        w.ClearOutputTypeAndNerChain();
                    }
                }
                else if ( is_IBAN_prefix( w.valueOriginal ) )
                {
                    w.ClearOutputTypeAndNerChain();
                }
            }
            #endregion

            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();
        }
        [M(O.AggressiveInlining)] private BankAccountValueTuple CreateBankAccountWord( List< word_t > words,  word_t w1, in ByTextPreamble_SearchResult sr )
        {
            var t = default(word_t);
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                t = words[ i ];
                _ValueUpperBuff   .Append( t.valueUpper    ).Append( SPACE );
                _ValueOriginalBuff.Append( t.valueOriginal ).Append( SPACE );
                t.ClearValuesAndNerChain();
            }

            var baw = new BankAccountWord( w1.startIndex, BankAccountTypeEnum.BankCode_AccountNumber )
            {
                valueOriginal = _ValueOriginalBuff.ToString( 0, _ValueOriginalBuff.Length - 1 ),
                valueUpper    = _ValueUpperBuff   .ToString( 0, _ValueUpperBuff   .Length - 1 ),
                length        = (t.startIndex - w1.startIndex) + t.length,
            };
            switch ( sr.TextPreambleType )
            {
                case TextPreambleTypeEnum.BankCode     : baw.BankCode      = sr.BankAccountValue; break;
                case TextPreambleTypeEnum.AccountNumber: baw.AccountNumber = sr.BankAccountValue; break;
                case TextPreambleTypeEnum.BankName     : baw.BankName      = baw.valueOriginal; break; //sr.BankAccountValue; break;
                case TextPreambleTypeEnum.AccountOwner : baw.AccountOwner  = baw.valueOriginal; break; //sr.BankAccountValue; break;
            }
            words[ sr.StartIndex ] = baw;
            words[ sr.PreambleWordIndex ].ClearOutputTypeAndNerChain();

            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();

            return (new BankAccountValueTuple( baw, in sr ));
        }
    }
}
