using System.Collections.Generic;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Companies
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class CompaniesRecognizer
    {
        const int MAX_DISTANCE_FROM_SUFFIX = 4;

        #region [.ctor().]
        private const char SPACE = ' ';
        private ByVocabSearcher    _ByVocabSearcher;
        private ByPrefixesSearcher _ByPrefixesSearcher;
        private BySuffixesSearcher _BySuffixesSearcher;
        private StringBuilder _ValueUpperBuff;
        private StringBuilder _ValueOriginalBuff;

        public CompaniesRecognizer( ICompaniesModel model )
        {
            _ByVocabSearcher    = new ByVocabSearcher( model.CompanyVocab );
            _ByPrefixesSearcher = new ByPrefixesSearcher( model.Prefixes );
            _BySuffixesSearcher = new BySuffixesSearcher( MAX_DISTANCE_FROM_SUFFIX, model.Suffixes, model.PrefixesPrevSuffixes, model.ExpandPreambles );

            _ValueUpperBuff    = new StringBuilder( 100 );
            _ValueOriginalBuff = new StringBuilder( 100 );
        }
        #endregion

        #region [.public method's.]        
        public void Run( List< word_t > words )
        {
            _ByVocabSearcher   .Begin( words );
            _ByPrefixesSearcher.Begin( words );
            //---------------------------------------//

            var has = false;
            var sr  = default(SearchResult);
            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                var w = words[ index ];
                if (  !w.IsOutputTypeOtherOrUrl() /*w.IsInputTypeNum() ||*/ ) /*(w.valueUpper == null) ||*/  /*|| w.IsExtraWordTypePunctuation()*/
                {
                    continue;
                }

                #region [.By Vocab.]
                if ( _ByVocabSearcher.TryFind2Rigth( index ) ) //---if ( _ByVocabSearcher.TryFind2Rigth( words, index, ref sr ) )
                {
                    //---CreateCompanyWord( words, in sr );
                    ref readonly var sr_ = ref _ByVocabSearcher.GetSearchResult();
                    CreateCompanyWord( words, in sr_ );
                    index = sr.EndIndex() - 1;
                    has   = true;
                    continue;
                }
                #endregion

                #region [.By Suffixes.]
                if ( _BySuffixesSearcher.TryFind( words, index, ref sr ) )
                {
                    CreateCompanyWord( words, in sr );
                    index = sr.EndIndex() - 1;
                    has   = true;
                    continue;
                }
                #endregion

                #region [.By Prefixes.]
                if ( _ByPrefixesSearcher.TryFind2Rigth( index ) ) //---if ( _ByPrefixesSearcher.TryFind2Rigth( words, index, ref sr ) )
                {
                    //---CreateCompanyWord( words, in sr );
                    ref readonly var sr_ = ref _ByPrefixesSearcher.GetSearchResult();
                    CreateCompanyWord( words, in sr_ );
                    index = sr.EndIndex() - 1;
                    has   = true;
                    continue;
                }
                #endregion
            }

            #region [.remove merged words.]
            if ( has )
            {
                words.RemoveWhereValueOriginalIsNull();
            }
            #endregion


            //---------------------------------------//
            _ByVocabSearcher   .End();
            _ByPrefixesSearcher.End();
        }
        #endregion

        [M(O.AggressiveInlining)] private void CreateCompanyWord( List< word_t > words, in SearchResult sr )
        {
            [M(O.AggressiveInlining)] static bool need_space_after_last_char( StringBuilder sb )
            {
                if ( 0 < sb.Length )
                {
                    var ch = sb[ sb.Length - 1 ];
                    switch ( ch )
                    {
                        case '&': case ')': case '.': case '!': case '"': case '\'': return (true);
                        default: return (ch.IsLetterOrDigit());
                    }
                }
                return (false);
            }
            [M(O.AggressiveInlining)] static bool need_space_before( word_t w, out bool is_letter_with_dot )
            {
                is_letter_with_dot = false;
                var ch = w.valueOriginal[ 0 ];
                switch ( ch )
                {
                    case '&': case '(': return (true);
                    default:
                        var is_letter = ch.IsLetter();
                        if ( is_letter && (w.length == 2) && (w.valueOriginal[ 1 ] == '.') )
                        {
                            is_letter_with_dot = true;
                            return (false);
                        }
                        return (is_letter || ch.IsDigit());
                }
            }

            var t = default(word_t);
            var prev_is_letter_with_dot = false;
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                t = words[ i ];
                _ValueUpperBuff.Append( t.valueUpper ).Append( SPACE );

                var is_need_space_before = need_space_before( t, out var is_letter_with_dot );
                if ( (need_space_after_last_char( _ValueOriginalBuff ) && is_need_space_before) || (!prev_is_letter_with_dot && is_letter_with_dot) )
                {
                    if ( 0 < _ValueOriginalBuff.Length ) _ValueOriginalBuff.Append( SPACE );
                }
                _ValueOriginalBuff.Append( t.valueOriginal );

                t.ClearValuesAndNerChain();
                prev_is_letter_with_dot = is_letter_with_dot;
            }

            var startIndex = words[ sr.StartIndex ].startIndex;
            var nw = new CompanyWord( startIndex )
            {
                valueOriginal = _ValueOriginalBuff.ToString(),//---( 0, _ValueOriginalBuff.Length - (_ValueOriginalBuff.IsLastCharIsWhiteSpace() ? 1 : 0) ),
                valueUpper    = _ValueUpperBuff   .ToString( 0, _ValueUpperBuff.Length - 1 ),
                length        = (t.startIndex - startIndex) + t.length,
            };
            words[ sr.StartIndex ] = nw;

            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();
        }
    }
}
