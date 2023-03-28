using System.Collections.Generic;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Nationalities
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
            public int Compare(in SearchResult x, in SearchResult y)
            {
                var d = y.Length - x.Length;
                if (d != 0)
                    return (d);

                return (x.StartIndex - y.StartIndex);
            }
        }

        [M(O.AggressiveInlining)] public SearchResult( int startIndex, int length )
        {
            StartIndex = startIndex;
            Length = length;
        }
        public int StartIndex { [M(O.AggressiveInlining)] get; }
        public int Length { [M(O.AggressiveInlining)] get; }
        [M(O.AggressiveInlining)] public int EndIndex() => StartIndex + Length;
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}]";
#endif
    }

    /// <summary>
    ///
    /// </summary>
    internal sealed class NationalitiesSearcher
    {
        #region [.ctor().]
        private IWordsChainDictionary _WordsChainDict;
        public NationalitiesSearcher( IWordsChainDictionary wcd ) => _WordsChainDict = wcd;
        #endregion

        #region [.public method's.]
        [M(O.AggressiveInlining)] public bool TryFindAll( List<word_t> words, out IReadOnlyCollection<SearchResult> results )
        {
            var ss = default(SortedSetByRef<SearchResult>);

            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                if ( _WordsChainDict.TryGetFirst( words, index, out var length ) )
                {
                    if ( ss == null ) ss = new SortedSetByRef<SearchResult>( SearchResult.Comparer.Instance );

                    ss.AddEx( index, length );
                }
            }
            results = ss;
            return (ss != null);
        }
        [M(O.AggressiveInlining)] public bool TryFindFirst2Rigth( List<word_t> words, int startIndex, int maxDistance, out SearchResult result )
        {
        LOOP:
            if ( _WordsChainDict.TryGetFirst( words, startIndex, out var length ) )
            {
                result = new SearchResult( startIndex, length );
                return (true);
            }
            else if ( maxDistance > 0 && words.Count > startIndex )
            {
                maxDistance--;
                startIndex++;
                goto LOOP;
            }

            result = default;
            return (false);
        }
        #endregion
#if DEBUG
        public override string ToString() => $"{_WordsChainDict}";
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class NationalitiesExtensions
    {
        [M(O.AggressiveInlining)] public static void AddEx( this SortedSetByRef<SearchResult> ss, int startIndex, int length )
        {
            var sr = new SearchResult( startIndex, length );
            if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
            {
                ss.Remove( in exists );
            }
            ss.Add( in sr );
        }
        [M(O.AggressiveInlining)] public static bool IsLastCharIsLetter( this StringBuilder sb ) => (0 < sb.Length) && (sb[ sb.Length - 1 ].IsLetter());
    }
}
