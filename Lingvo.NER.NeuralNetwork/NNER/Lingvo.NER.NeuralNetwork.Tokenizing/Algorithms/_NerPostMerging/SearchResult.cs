using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.NeuralNetwork.NerPostMerging
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
            public static Comparer Inst { [M(O.AggressiveInlining)] get; } = new Comparer();
            private Comparer() { }
            public int Compare( in SearchResult x, in SearchResult y )
            {
                //var d = y.Length - x.Length;
                //if ( d != 0 )
                //    return (d);

                //return (y.StartIndex - x.StartIndex);

                var d = y.Length - x.Length;
                if ( d != 0 )
                    return (d);

                return (x.StartIndex - y.StartIndex);
            }
        }

        [M(O.AggressiveInlining)] public SearchResult( int startIndex, int length, NerOutputType nerOutputType )
        {
            StartIndex    = startIndex;
            Length        = length;
            NerOutputType = nerOutputType;
        }
        [M(O.AggressiveInlining)] public SearchResult( int startIndex, in ngram_t ngram )
        {
            StartIndex    = startIndex - ngram.NNerOutputTypes.Length + 1;
            Length        = ngram.NNerOutputTypes.Length;
            NerOutputType = ngram.ResultNerOutputType;
        }
        [M(O.AggressiveInlining)] public SearchResult( int startIndex, in ngram_2_t ngram )
        {
            StartIndex    = startIndex - ngram.Tuples.Length + 1;
            Length        = ngram.Tuples.Length;
            NerOutputType = ngram.ResultNerOutputType;
        }

        public int           StartIndex    { [M(O.AggressiveInlining)] get; }
        public int           Length        { [M(O.AggressiveInlining)] get; }
        public NerOutputType NerOutputType { [M(O.AggressiveInlining)] get; }
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}]"; 
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal interface ISearcher
    {
        bool TryFindAll( IList< word_t > words, out IReadOnlyCollection< SearchResult > results );
    }
}
