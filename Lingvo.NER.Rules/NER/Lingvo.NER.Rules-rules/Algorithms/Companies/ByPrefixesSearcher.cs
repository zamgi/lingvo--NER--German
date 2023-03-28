using System.Collections.Generic;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Companies
{
    /// <summary>
    /// 6. Если после слов (PREFIX)
    ///    Stiftung
    ///    Stiftung für
    ///    Stiftung zur
    ///     
    ///  Идет слово с заглавной буквы, то это является частью названия:
    ///  Stiftung Bundeskanzler-Adenauer-Haus
    /// </summary>
    internal sealed class /*struct*/ ByPrefixesSearcher
    {
        #region [.ctor().]
        private IWcd_Find2Right _Wcd;
        public ByPrefixesSearcher( IWcd_Find2Right wcd ) => _Wcd = wcd;
        #endregion

        #region [.public method's.]
        [M(O.AggressiveInlining)] private bool TryGetEnd( List< word_t > words, int idx, out int length )
        {            
            if ( idx < words.Count )
            {                
                var w = words[ idx ];
                if ( w.IsOutputTypeOther() && !w.IsExtraWordTypePunctuation() && w.IsFirstLetterUpper() && !_Wcd.Contains( words, idx ) )
                {
                    length = 1;
                    var prev_w = default(word_t);
                    for ( idx++; idx < words.Count; idx++ )
                    {
                        w = words[ idx ];
                        if ( !w.IsOutputTypeOther() ) break;

                        if ( w.IsExtraWordTypeDash() )
                        {
                            ;
                        }
                        else if ( !w.IsFirstLetterUpper() || _Wcd.Contains( words, idx ) ) 
                        {
                            break;
                        }
                        length++;

                        prev_w = w;
                    }
                    if ( (prev_w != null) && prev_w.IsExtraWordTypeDash() )
                    {
                        length--;
                    }
                    return (true);
                }
            }
            length = default;
            return (false);
        }
        [M(O.AggressiveInlining)] public bool TryFind2Rigth( List< word_t > words, int startIndex, ref SearchResult result )
        {
            if ( _Wcd.TryFind2Right( words, startIndex, out var length ) && TryGetEnd( words, startIndex + length, out var length_2 ) )
            {
                result = new SearchResult( startIndex, length + length_2 );
                return (true);
            }
            return (false);
        }

        #region [.Begin-End method's.]
        //*
        private List< word_t > _Words;
        private SearchResult _SearchResult;
        [M(O.AggressiveInlining)] public ref readonly SearchResult GetSearchResult() => ref _SearchResult;
        [M(O.AggressiveInlining)] public void Begin( List< word_t > words ) => _Words = words;
        [M(O.AggressiveInlining)] public void End() => _Words = default;
        [M(O.AggressiveInlining)] private bool TryGetEnd( int idx, out int length )
        {
            if ( idx < _Words.Count )
            {
                var w = _Words[ idx ];
                if ( w.IsOutputTypeOther() && !w.IsExtraWordTypePunctuation() && w.IsFirstLetterUpper() && !_Wcd.Contains( _Words, idx ) )
                {
                    length = 1;
                    var prev_w = default(word_t);
                    for ( idx++; idx < _Words.Count; idx++ )
                    {
                        w = _Words[ idx ];
                        if ( !w.IsOutputTypeOther() ) break;

                        if ( w.IsExtraWordTypeDash() )
                        {
                            ;
                        }
                        else if ( !w.IsFirstLetterUpper() || _Wcd.Contains( _Words, idx ) )
                        {
                            break;
                        }
                        length++;

                        prev_w = w;
                    }
                    if ( (prev_w != null) && prev_w.IsExtraWordTypeDash() )
                    {
                        length--;
                    }
                    return (true);
                }
            }
            length = default;
            return (false);
        }
        [M(O.AggressiveInlining)] public bool TryFind2Rigth( int startIndex )
        {
            if ( _Wcd.TryFind2Right( _Words, startIndex, out var length ) && TryGetEnd( startIndex + length, out var length_2 ) )
            {
                _SearchResult = new SearchResult( startIndex, length + length_2 );
                return (true);
            }
            return (false);
        }
        //*/
        #endregion
        #endregion
#if DEBUG
        public override string ToString() => $"{_Wcd}";
#endif
    }
}
