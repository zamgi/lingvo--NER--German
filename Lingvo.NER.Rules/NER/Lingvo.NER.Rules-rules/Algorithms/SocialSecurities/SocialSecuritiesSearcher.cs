using System.Collections.Generic;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using ngram_t = Lingvo.NER.Rules.SocialSecurities.SocialSecuritiesSearcher.ngram_t;
using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.SocialSecurities
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
            public int Compare( in SearchResult x, in SearchResult y ) //=> (y.StartIndex - x.StartIndex);
            {
                var d = y.Length - x.Length;
                if ( d != 0 )
                    return (d);

                return (y.StartIndex - x.StartIndex);
            }
        }

        [M(O.AggressiveInlining)] public SearchResult( int startIndex, ngram_t[] ngrams )
        {
            StartIndex = startIndex - ngrams.Length + 1;
            Length     = ngrams.Length;
        }
        public int       StartIndex { [M(O.AggressiveInlining)] get; }
        public int       Length     { [M(O.AggressiveInlining)] get; }
        [M(O.AggressiveInlining)] public int EndIndex() => StartIndex + Length;
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}]";
#endif
    }

    /// <summary>
    ///
    /// </summary>
    internal static class SocialSecuritiesSearcher
    {
        /// <summary>
        /// 
        /// </summary>
        internal enum ngramType : byte
        {
            __UNDEFINED__,

            validNumber,
            letters,
            mixed,
            hyphen,
            slash,
            hyphen_letter_hyphen
        }
        /// <summary>
        /// 
        /// </summary>
        internal struct ngram_t
        {
            /// <summary>
            /// 
            /// </summary>
            public sealed class EqualityComparer : IEqualityComparerByRef< ngram_t >
            {
                public static EqualityComparer Instance { get; } = new EqualityComparer();
                private EqualityComparer() { }

                [M(O.AggressiveInlining)] public static bool _Equals_( in ngram_t x, in ngram_t y ) => (x.type == y.type) && (x.length == y.length);
                public bool Equals( in ngram_t x, in ngram_t y ) => _Equals_( in x, in y );
                public int GetHashCode( in ngram_t obj ) => obj.type.GetHashCode();
            }

            public static ngram_t UNDEFINED() => new ngram_t() { type = ngramType.__UNDEFINED__ };
            public static ngram_t ValidNumber( int len ) => new ngram_t() { type = ngramType.validNumber, length = len };
            public static ngram_t Letters( int len ) => new ngram_t() { type = ngramType.letters, length = len };
            public static ngram_t Mixed( int len ) => new ngram_t() { type = ngramType.mixed, length = len };
            public static ngram_t Hyphen() => new ngram_t() { type = ngramType.hyphen, length = 1 };
            public static ngram_t Slash() => new ngram_t() { type = ngramType.slash, length = 1 };
            public static ngram_t Hyphen_Letter_Hyphen() => new ngram_t() { type = ngramType.hyphen_letter_hyphen, length = 1 };

            public ngramType type;
            public int       length;
#if DEBUG
            public override string ToString() => $"{type}, {length}";
#endif
        }

        #region [.model.]
        /// <summary>
        ///
        /// </summary>
        private sealed class TreeNode
        {
            /// <summary>
            /// 
            /// </summary>
            private sealed class ngramsArray_EqualityComparer : IEqualityComparer< ngram_t[] >
            {
                public static ngramsArray_EqualityComparer Instance { get; } = new ngramsArray_EqualityComparer();
                private ngramsArray_EqualityComparer() { }
                
                public bool Equals( ngram_t[] x, ngram_t[] y )
                {
                    var len = x.Length;
                    if ( len != y.Length )
                    {
                        return (false);
                    }

                    for ( int i = 0; i < len; i++ )
                    {
                        if ( !ngram_t.EqualityComparer._Equals_( in x[ i ], in y[ i ] ) )
                        {
                            return (false);
                        }
                    }
                    return (true);
                }
                public int GetHashCode( ngram_t[] obj ) => obj.Length;
            }

            public static TreeNode BuildTree( IEnumerable< ngram_t[] > ngrams )
            {
                var transitions_root_nodes = default(MapByRef< ngram_t, TreeNode >.ReadOnlyCollection4Values);
                var transitions_nodes      = default(MapByRef< ngram_t, TreeNode >.ReadOnlyCollection4Values);

                // Build keyword tree and transition function
                var root = new TreeNode( null, ngram_t.UNDEFINED() );
                foreach ( var ngramsArray in ngrams )
                {
                    // add pattern to tree
                    var node = root;
                    foreach ( var ngram in ngramsArray )
                    {
                        var nodeNew = node.GetTransition( in ngram );
                        #region comm.
                        //TreeNode nodeNew = null;
                        //foreach ( TreeNode trans in node.Transitions )
                        //{
                        //    if ( ngram_t.EqualityComparer.Equals( in trans.Ngram, in ngram ) )
                        //    {
                        //        nodeNew = trans;
                        //        break;
                        //    }
                        //} 
                        #endregion

                        if ( nodeNew == null )
                        {
                            nodeNew = new TreeNode( node, in ngram );
                            node.AddTransition( nodeNew );
                        }
                        node = nodeNew;
                    }
                    node.AddNgrams( ngramsArray );
                }

                // Find failure functions
                var nodes = new List< TreeNode >();
                // level 1 nodes - fail to root node
                if ( root.TryGetAllTransitions( ref transitions_root_nodes ) )
                {
                    nodes.Capacity = transitions_root_nodes.Count;

                    foreach ( TreeNode node in transitions_root_nodes )
                    {
                        node.Failure = root;
                        if ( node.TryGetAllTransitions( ref transitions_nodes ) )
                        {
                            foreach ( var trans in transitions_nodes )
                            {
                                nodes.Add( trans );
                            }
                        }
                    }
                }

                // other nodes - using BFS
                while ( nodes.Count != 0 )
                {
                    var newNodes = new List< TreeNode >( nodes.Count );
                    foreach ( var node in nodes )
                    {
                        var r = node.Parent.Failure;
                        ref var ngram = ref node.Ngram;

                        while ( (r != null) && !r.ContainsTransition( in ngram ) )
                        {
                            r = r.Failure;
                        }
                        if ( r == null )
                        {
                            node.Failure = root;
                        }
                        else
                        {
                            node.Failure = r.GetTransition( in ngram );
                            var failure_ngrams = node.Failure?.Ngrams;
                            if ( failure_ngrams != null )
                            {
                                foreach ( var ngs in failure_ngrams )
                                {
                                    node.AddNgrams( ngs );
                                }
                            }
                        }

                        // add child nodes to BFS list 
                        if ( node.TryGetAllTransitions( ref transitions_nodes ) )
                        {
                            foreach ( var child in transitions_nodes )
                            {
                                newNodes.Add( child );
                            }
                        }
                    }
                    nodes = newNodes;
                }
                root.Failure = root;

                return (root);
            }

            #region [.props.]
            private MapByRef< ngram_t, TreeNode > _TransDict;
            private Set< ngram_t[] > _Ngrams;

            private ngram_t _Ngram;
            public ref ngram_t Ngram { [M(O.AggressiveInlining)] get => ref _Ngram; }
            public TreeNode Parent  { [M(O.AggressiveInlining)] get; private set; }
            public TreeNode Failure { [M(O.AggressiveInlining)] get; internal set; }

            public bool HasNgrams { [M(O.AggressiveInlining)] get => (_Ngrams != null); }
            public Set< ngram_t[] > Ngrams { [M(O.AggressiveInlining)] get => _Ngrams; }
            [M(O.AggressiveInlining)] public bool TryGetAllTransitions( ref MapByRef< ngram_t, TreeNode >.ReadOnlyCollection4Values trs )
            {
                if ( _TransDict != null )
                {
                    trs = _TransDict.GetValues();
                    return (true);
                }
                return (false);
            }
            #endregion

            #region [.ctor() & methods.]
            public TreeNode( TreeNode parent, in ngram_t ngarm )
            {
                _Ngram = ngarm;
                Parent = parent;
            }

            public void AddNgrams( ngram_t[] ngrams )
            {
                if ( _Ngrams == null )
                {
                    _Ngrams = new Set< ngram_t[] >( ngramsArray_EqualityComparer.Instance );
                }
                _Ngrams.Add( ngrams );
            }
            public void AddTransition( TreeNode node )
            {
                if ( _TransDict == null )
                {
                    _TransDict = new MapByRef< ngram_t, TreeNode >( ngram_t.EqualityComparer.Instance );
                }
                _TransDict.Add( in node.Ngram, node );
            }
            [M(O.AggressiveInlining)] public bool ContainsTransition( in ngram_t ngram ) => ((_TransDict != null) && _TransDict.ContainsKey( in ngram ));
            [M(O.AggressiveInlining)] public TreeNode GetTransition( in ngram_t ngram ) => ((_TransDict != null) && _TransDict.TryGetValue( in ngram, out var node ) ? node : null);            
            #endregion
#if DEBUG
            public override string ToString() => $"{((Ngram.type == ngramType.__UNDEFINED__) ? "ROOT" : Ngram.ToString())}, transitions(descendants): {(_TransDict?.Count).GetValueOrDefault()}, ngrams: {(_Ngrams?.Count).GetValueOrDefault()}"; 
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class Model
        {
            public static IEnumerable< ngram_t[] > GetNgrams()
            {
                /*
                always a 12 digit code.

                Separation:
                    There are four possible variants:
                        Very common: SPACE:
                            53 270139 W 032
                        Common: /
                            53/270139/W/032
                        Uncommon: - or no space:
                            53-270139-W-032
                            53270139W032
                */

                //'53270139W032'
                yield return (new[] { ngram_t.Mixed( 12 ) });

                //'53-270139-W-032'
                yield return (new[] { ngram_t.ValidNumber( 2 ), ngram_t.Hyphen(), ngram_t.ValidNumber( 6 ), ngram_t.Hyphen(), ngram_t.Letters( 1 ), ngram_t.Hyphen(), ngram_t.ValidNumber( 3 ) });
                yield return (new[] { ngram_t.ValidNumber( 2 ), ngram_t.Hyphen(), ngram_t.ValidNumber( 6 ), ngram_t.Hyphen_Letter_Hyphen(), ngram_t.ValidNumber( 3 ) });

                //'53/270139/W/032'
                yield return (new[] { ngram_t.ValidNumber( 2 ), ngram_t.Slash(), ngram_t.ValidNumber( 6 ), ngram_t.Slash(), ngram_t.Letters( 1 ), ngram_t.Slash(), ngram_t.ValidNumber( 3 ) });

                //'53 270139 W 032'
                yield return (new[] { ngram_t.ValidNumber( 2 ), ngram_t.ValidNumber( 6 ), ngram_t.Letters( 1 ), ngram_t.ValidNumber( 3 ) });
            }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        private struct Finder
        {
            private TreeNode __Root__;
            private TreeNode __Node__;
            [M(O.AggressiveInlining)] public static Finder Create( TreeNode root ) => new Finder() { __Root__ = root, __Node__ = root };

            [M(O.AggressiveInlining)] public TreeNode Find( in ngram_t ng )
            {
                TreeNode transNode;
                do
                {
                    transNode = __Node__.GetTransition( in ng );
                    if ( __Node__ == __Root__ )
                    {
                        break;
                    }
                    if ( transNode == null )
                    {
                        __Node__ = __Node__.Failure;
                    }
                }
                while ( transNode == null );
                if ( transNode != null )
                {
                    __Node__ = transNode;
                }
                return (__Node__);
            }
        }

        #region [.ctor().]
        private static TreeNode _Root;
        static SocialSecuritiesSearcher() => _Root = TreeNode.BuildTree( Model.GetNgrams() );
        #endregion

        #region [.public method's.]
        public static bool TryFindAll( List< word_t > words, out IReadOnlyCollection< SearchResult > results )
        {
            var ss = default(SortedSetByRef< SearchResult >);
            var node = _Root;
            var finder = Finder.Create( _Root );

            var ng = new ngram_t();
            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                if ( !Classify( words[ index ], ref ng ) && (node == _Root) )
                {
                    continue;
                }

                node = finder.Find( in ng );
                if ( node.HasNgrams )
                {
                    if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );

                    switch ( node.Ngrams.Count )
                    {
                        case 1:
                            ss.AddEx( index, node.Ngrams.First );
                        break;

                        default:
                            foreach ( var ngrams in node.Ngrams )
                            {
                                ss.AddEx( index, ngrams );
                            }
                        break;
                    }
                }
            }
            results = ss;
            return (ss != null);
        }
        #endregion

        #region [.text classifier.]
        [M(O.AggressiveInlining)] private static bool Classify( word_t w, ref ngram_t ng )
        {
            switch ( w.nerInputType )
            {
                case NerInputType.NumCapital:
                    ng.length = w.length;
                    ng.type   = ngramType.mixed;
                    return (true);

                case NerInputType.Num:
                    if ( w.IsOutputTypeOther() && w.IsExtraWordTypeIntegerNumber() )
                    {
                        ng.length = w.length;
                        ng.type   = ngramType.validNumber;
                        return (true);
                    }
                break;

                case NerInputType.LatinCapital:
                case NerInputType.AllCapital:
                case NerInputType.OneCapital:
                    ng.length = w.length;
                    ng.type   = ngramType.letters;
                    return (true);

                case NerInputType.Other:
                    switch ( w.length )
                    {
                        case 1:
                        if ( w.IsExtraWordTypeDash() )
                        {
                            ng.length = w.length;
                            ng.type   = ngramType.hyphen;
                            return (true);
                        }
                        if ( w.valueOriginal[ 0 ] == '/' )
                        {
                            ng.length = 1;
                            ng.type   = ngramType.slash;
                            return (true);
                        }
                        break;

                        case 3:
                        var v = w.valueOriginal;
                        if ( v[ 0 ].IsHyphen() && v[ 1 ].IsLetter() && v[ 2 ].IsHyphen() )
                        {
                            ng.length = 1;
                            ng.type   = ngramType.hyphen_letter_hyphen;
                            return (true);
                        }
                        break;
                    }
                break;
            }

            ng.type = ngramType.__UNDEFINED__;
            return (false);
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class SocialSecuritiesSearcherExtensions
    {
        [M(O.AggressiveInlining)] public static void AddEx( this SortedSetByRef< SearchResult > ss, int startIndex, ngram_t[] ngrams )
        {
            var sr = new SearchResult( startIndex, ngrams );
            if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
            {
                ss.Remove( in exists );
            }
            ss.Add( in sr );
        }        
    }
}