using System.Collections.Generic;

using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.NerPostMerging
{
    /// <summary>
    ///
    /// </summary>
    internal static class NerPostMerger
    {
        private const int MAX_DISTANCE_BETWEEN_ENTITIES_IN_SEP_WORDS = 7;

        private static Searcher _Searcher;
        static NerPostMerger() => _Searcher = new Searcher( MAX_DISTANCE_BETWEEN_ENTITIES_IN_SEP_WORDS );

        public static void Run( DirectAccessList< (word_t w, int orderNum) > nerWords, List< NerUnitedEntity > nerUnitedEntities )
        {
            if ( _Searcher.TryFindAll( nerWords, out var ss ) )
            {
                foreach ( var sr in ss )
                {
                    if ( NerUnitedEntity.TryCreate( nerWords, in sr, out var nue ) )
                    {
                        nerUnitedEntities.Add( nue );
                    }
                }
            }
        }
    }
}
