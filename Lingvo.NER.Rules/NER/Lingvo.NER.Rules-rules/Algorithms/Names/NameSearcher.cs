using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Names
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
            public int Compare( in SearchResult x, in SearchResult y )
            {
                var d = y.Length - x.Length;
                if ( d != 0 )
                    return (d);

                return (x.StartIndex - y.StartIndex);
            }
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
    internal sealed class NameSearcher
    {
        #region [.ctor().]
        private IWordsChainDictionary _WordsChainDict;
        public NameSearcher( IWordsChainDictionary wcd ) => _WordsChainDict = wcd;
        #endregion

        #region [.public method's.]
        [M(O.AggressiveInlining)] public bool TryFindAll( List< word_t > words, out IReadOnlyCollection< SearchResult > results )
        {
            var ss = default(SortedSetByRef< SearchResult >);

            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                if ( _WordsChainDict.TryGetFirst( words, index, out var length ) )
                {
                    if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );

                    ss.AddEx( index, length );
                }
            }
            results = ss;
            return (ss != null);
        }
        [M(O.AggressiveInlining)] public bool TryFindFirst2Rigth( List< word_t > words, int startIndex, out SearchResult result )
        {
            if ( _WordsChainDict.TryGetFirst( words, startIndex, out var length ) )
            {
                result = new SearchResult( startIndex, length );
                return (true);
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
    internal sealed class SurNameSearcher : IDisposable
    {
        #region [.cctor().]
        private static char[] DASHES_CHARS;
        static SurNameSearcher() => DASHES_CHARS = xlat.GetHyphens().ToArray();
        #endregion

        #region [.ctor().]
        private IWordsChainDictionary _WordsChainDict;
        private Tokenizer _Tokenizer;
        private StringBuilder _Buf;
        public SurNameSearcher( IWordsChainDictionary wcd )
        {
            _WordsChainDict = wcd;
            _Tokenizer      = Tokenizer.Create4NoSentsNoUrlsAllocate();
            _Buf            = new StringBuilder();
        }
        public void Dispose() => _Tokenizer.Dispose();
        #endregion

        #region [.public method's.]
        [M(O.AggressiveInlining)] public bool TryFindAll( List< word_t > words, out IReadOnlyCollection< SearchResult > results )
        {
            var ss = default(SortedSetByRef< SearchResult >);

            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                if ( _WordsChainDict.TryGetFirst( words, index, out var length ) )
                {
                    //It is necessary that any two surnames can be combined. Type Müller-Spüller, Spüller-Müller etc.
                    SpecialCase42SurNamesCombinedOverDash( words, index, ref length );

                    if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );

                    ss.AddEx( index, length );
                }
                else if ( TrySplit2PairByDash( words[ index ] ) )
                {
                    if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );

                    ss.AddEx( index, 1 );
                }
            }
            results = ss;
            return (ss != null);
        }
        [M(O.AggressiveInlining)] public bool TryFindFirst2Rigth( List< word_t > words, int startIndex, out SearchResult result )
        {
            if ( _WordsChainDict.TryGetFirst( words, startIndex, out var length ) )
            {
                //It is necessary that any two surnames can be combined. Type Müller-Spüller, Spüller-Müller etc.
                SpecialCase42SurNamesCombinedOverDash( words, startIndex, ref length );

                result = new SearchResult( startIndex, length );
                return (true);
            }
            else if ( TrySplit2PairByDash( words, startIndex ) )
            {
                result = new SearchResult( startIndex, 1 );
                return (true);
            }

            result = default;
            return (false);
        }

        [M(O.AggressiveInlining)] private void SpecialCase42SurNamesCombinedOverDash( List< word_t > words, int startIndex, ref int length )
        {
            //It is necessary that any two surnames can be combined. Type Müller-Spüller, Spüller-Müller etc.
            if ( length == 1 )
            {
                startIndex++;
                if ( (startIndex < words.Count - 1) && 
                     words[ startIndex ].IsExtraWordTypeDash() && 
                     _WordsChainDict.TryGetFirst( words, startIndex + 1, out var length_2 ) && 
                     (length_2 == 1)
                   )
                {
                    length = 1 + 1 + 1;
                }
            }
        }

        //It is necessary that any two surnames can be combined. Type Müller-Spüller, Spüller-Müller etc.
        [M(O.AggressiveInlining)] private bool TrySplit2PairByDash( List< word_t > words, int startIndex ) => (startIndex < words.Count) && TrySplit2PairByDash( words[ startIndex ] );
        [M(O.AggressiveInlining)] private bool TrySplit2PairByDash( word_t w )
        {
            var v = w.valueOriginal;
            var i = v.IndexOfAny( DASHES_CHARS );
            if ( i != -1 )
            {
                v = _Buf.Clear().Append( v, 0, i ).Append( ' ' ).Append( v, ++i, v.Length - i ).ToString();
                var words = _Tokenizer.Run_NoSentsNoUrlsAllocate( v );
                if ( (/*1 < */words.Count == 2) &&
                     _WordsChainDict.TryGetFirst( words[ 0 ] ) && //_WordsChainDict.TryGetFirst( words, 0, out var length ) && (length == 1) &&
                     _WordsChainDict.TryGetFirst( words[ 1 ] )    //_WordsChainDict.TryGetFirst( words, 1, out     length ) && (length == 1) 
                   )
                {
                    return (true);
                }
            }

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
    internal static class NamesExtensions
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
        [M(O.AggressiveInlining)] public static bool IsLastCharIsLetter( this StringBuilder sb ) => (0 < sb.Length) && (sb[ sb.Length - 1 ].IsLetter());
    }
}
