using System.Collections.Generic;
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
    unsafe internal sealed class TaxIdentificationsRecognizer
    {
        #region [.ctor().]
        private StringBuilder                                 _ValueOriginalBuff;
        private TaxIdentificationsSearcher_ByTextPreamble     _ByTextPreambleSearcher;
        private TaxIdentificationsSearcher_ByTextPreamble_Old _ByTextPreambleSearcher_Old;
        public TaxIdentificationsRecognizer( ITaxIdentificationsModel model )
        {
            _ValueOriginalBuff = new StringBuilder( 100 );

            _ByTextPreambleSearcher     = new TaxIdentificationsSearcher_ByTextPreamble();
            _ByTextPreambleSearcher_Old = new TaxIdentificationsSearcher_ByTextPreamble_Old( model );
        }
        #endregion

        #region [.By-Text-Preamble.]
        [M(O.AggressiveInlining)] private void Run_ByTextPreambleSearcher( List< word_t > words )
        {
            if ( _ByTextPreambleSearcher.TryFindAll( words, out var results ) )
            {
                foreach ( var sr in results )
                {
                    var w1 = words[ sr.StartIndex ];
                    if ( CanProcess( w1 ) )
                    {
                        CreateTaxIdentificationWord( words, w1, in sr, TaxIdentificationTypeEnum.New );
                    }
                }

                #region [.remove merged words.]
                words.RemoveWhereValueOriginalIsNull();
                #endregion
            }

            if ( _ByTextPreambleSearcher_Old.TryFindAll( words, out results ) )
            {
                foreach ( var sr in results )
                {
                    var w1 = words[ sr.StartIndex ];
                    if ( CanProcess( w1 ) )
                    {
                        CreateTaxIdentificationWord( words, w1, in sr, TaxIdentificationTypeEnum.Old );
                    }
                }

                #region [.remove merged words.]
                words.RemoveWhereValueOriginalIsNull();
                #endregion
            }
        }      
        #endregion

        [M(O.AggressiveInlining)] public void Run( List< word_t > words ) => Run_ByTextPreambleSearcher( words );

        [M(O.AggressiveInlining)] private static bool CanProcess( word_t w ) => ((w.valueUpper != null) && w.IsOutputTypeOther()); //!w.IsTaxIdentification());
        [M(O.AggressiveInlining)] private TaxIdentificationWord CreateTaxIdentificationWord( List< word_t > words, word_t w1, in ByTextPreamble_SearchResult sr
            , TaxIdentificationTypeEnum taxIdentificationType )
        {
            var t = default(word_t);
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                t = words[ i ];                
                _ValueOriginalBuff.Append( t.valueOriginal );
                t.ClearValuesAndNerChain();
            }

            var tiw = new TaxIdentificationWord( w1.startIndex, sr.TaxIdentification, taxIdentificationType )
            {
                valueOriginal = _ValueOriginalBuff.ToString(),
                valueUpper    = sr.TaxIdentification,
                length        = (t.startIndex - w1.startIndex) + t.length,
            };
            words[ sr.StartIndex ] = tiw;
            for ( var i = sr.PreambleWordIndex; i < sr.StartIndex; i++ )
            {
                words[ i ].ClearOutputTypeAndNerChain();
            }

            _ValueOriginalBuff.Clear();
            return (tiw);
        }
    }
}
