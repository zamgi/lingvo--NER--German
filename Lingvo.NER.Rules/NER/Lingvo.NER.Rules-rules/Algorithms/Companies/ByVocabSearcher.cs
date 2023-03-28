using System.Collections.Generic;

using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Companies
{
    /// <summary>
    /// 8. Будет словарь с названиями компаний, их адресов и телефонов
    /// </summary>
    internal sealed class /*struct*/ ByVocabSearcher
    {
        #region [.ctor().]
        private IWcd_Find2Right _Wcd;
        public ByVocabSearcher( IWcd_Find2Right wcd ) => _Wcd = wcd;
        #endregion

        #region [.public method's.]
        [M(O.AggressiveInlining)] public bool TryFind2Rigth( List< word_t > words, int startIndex, ref SearchResult result )
        {
            if ( _Wcd.TryFind2Right( words, startIndex, out var length ) )
            {
                result = new SearchResult( startIndex, length );
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
        [M(O.AggressiveInlining)] public bool TryFind2Rigth( int startIndex )
        {
            if ( _Wcd.TryFind2Right( _Words, startIndex, out var length ) )
            {
                _SearchResult = new SearchResult( startIndex, length );
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
