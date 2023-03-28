using System.Collections.Generic;

using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.NerPostMerging
{
    /// <summary>
    /// 
    /// </summary>
    internal readonly struct SearchResult
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Comparer : IComparerByRef< SearchResult >
        {
            public static Comparer Instance { get; } = new Comparer();
            private Comparer() { }
            public int Compare( in SearchResult x, in SearchResult y ) => (x.StartIndex - y.StartIndex);
        }

        [M(O.AggressiveInlining)] public SearchResult( int startIndex, int length )
        {
            StartIndex = startIndex;
            Length     = length;
        }

        public int StartIndex { [M(O.AggressiveInlining)] get; }
        public int Length     { [M(O.AggressiveInlining)] get; }
        [M(O.AggressiveInlining)] public int EndIndex() => StartIndex + Length;
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}]"; 
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class Searcher
    {
        #region [.ctor().]
        private int _MaxDistanceBetweenWords;
        public Searcher( int maxDistanceBetweenWords ) => _MaxDistanceBetweenWords = maxDistanceBetweenWords;
        #endregion

        #region [.methods.]
        [M(O.AggressiveInlining)] public bool TryFindAll( DirectAccessList< (word_t w, int orderNum) > nerWords, out IReadOnlyCollection< SearchResult > results ) 
        {
            var ss = default(SortedSetByRef< SearchResult >);
            
            for ( int index = 0, len = nerWords.Count; index < len; index++ )
            {
                ref readonly var t = ref nerWords._Items[ index ];
                if ( t.w.nerOutputType != NerOutputType.Name )
                {
                    continue;
                }

                var prev_orderNum = -1;
                var startIndex    = index;
                for ( index++; index < len; index++ )
                {
                    t = ref nerWords._Items[ index ];
                    if ( (t.w.nerOutputType == NerOutputType.Name) || (t.w.nerOutputType == NerOutputType.Other) )
                    {
                        break;
                    }
                    if ( (_MaxDistanceBetweenWords < (t.orderNum - prev_orderNum)) && (prev_orderNum != -1) )
                    {
                        break;
                    }
                    prev_orderNum = t.orderNum;
                }

                var length = (index - startIndex);
                if ( 1 < length )
                {
                    if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );
                    ss.AddEx( startIndex, length );                    
                }
                index--;
            }

            results = ss;
            return (ss != null);
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Searcher_v2_Extensions
    {
        [M(O.AggressiveInlining)] public static void AddEx( this SortedSetByRef< SearchResult > ss, int startIndex, int length )
        {
            var sr = new SearchResult( startIndex, length );
            if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
            {
                ss.Remove( in exists );
            }
            ss.Add( in sr );
        }        
    }
}
