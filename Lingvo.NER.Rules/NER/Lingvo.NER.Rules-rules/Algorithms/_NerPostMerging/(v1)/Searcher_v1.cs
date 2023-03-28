using System.Collections.Generic;

using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.NerPostMerging
{
    /// <summary>
    /// 
    /// </summary>
    internal readonly struct SearchResult_v1
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Comparer : IComparerByRef< SearchResult_v1 >
        {
            public static Comparer Instance { get; } = new Comparer();
            private Comparer() { }
            public int Compare( in SearchResult_v1 x, in SearchResult_v1 y ) => (x.StartIndex - y.StartIndex);
        }

        [M(O.AggressiveInlining)] public SearchResult_v1( int startIndex, NerOutputType[] ngram )
        {
            StartIndex    = startIndex - ngram.Length + 1;
            Length        = ngram.Length;
#if DEBUG
            Ngram = ngram; 
#endif
        }

        public int StartIndex { [M(O.AggressiveInlining)] get; }
        public int Length     { [M(O.AggressiveInlining)] get; }
        [M(O.AggressiveInlining)] public int EndIndex() => StartIndex + Length;
#if DEBUG
        public NerOutputType[] Ngram { [M(O.AggressiveInlining)] get; }
        public override string ToString() => $"[{StartIndex}:{Length}], Ngram: '{string.Join( "', '", Ngram )}'"; 
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class Searcher_v1
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class ngrams_EqualityComparer : IEqualityComparer< NerOutputType[] >
        {
            public static ngrams_EqualityComparer Instance { get; } = new ngrams_EqualityComparer();
            private ngrams_EqualityComparer() { }

            public bool Equals( NerOutputType[] x, NerOutputType[] y )
            {
                var len = x.Length;
                if ( len != y.Length )
                {
                    return (false);
                }

                for ( int i = 0; i < len; i++ )
                {
                    if ( x[ i ] != y[ i ] )
                    {
                        return (false);
                    }
                }
                return (true);
            }
            public int GetHashCode( NerOutputType[] obj ) => obj.Length;
        }

        /// <summary>
        ///
        /// </summary>
        private sealed class TreeNode
        {
            public static TreeNode BuildTree( IList< NerOutputType[] > ngrams )
            {
                var transitions_root_nodes = default(Map< NerOutputType, TreeNode >.ReadOnlyCollection4Values);
                var transitions_nodes      = default(Map< NerOutputType, TreeNode >.ReadOnlyCollection4Values);
                var failure_ngrams         = default(Set< NerOutputType[] >);

                var root = new TreeNode();
                foreach ( var ngram in ngrams )
                {
                    var node = root;
                    foreach ( var nerOutputType in ngram )
                    {
                        var nodeNew = node.GetTransition( nerOutputType );
                        if ( nodeNew == null )
                        {
                            nodeNew = new TreeNode( node, nerOutputType );
                            node.AddTransition( nodeNew );
                        }
                        node = nodeNew;
                    }
                    node.AddNgram( ngram );
                }

                var nodes = new List< TreeNode >();
                if ( root.TryGetAllTransitions( ref transitions_root_nodes ) )
                {
                    nodes.Capacity = transitions_root_nodes.Count;

                    foreach ( var node in transitions_root_nodes )
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
                        var nerOutputType = node.NerOutputType;

                        while ( (r != null) && !r.ContainsTransition( nerOutputType ) )
                        {
                            r = r.Failure;
                        }
                        if ( r == null )
                        {
                            node.Failure = root;
                        }
                        else
                        {
                            node.Failure = r.GetTransition( nerOutputType );
                            if ( node.Failure.TryGetNgrams( ref failure_ngrams ) )
                            {
                                foreach ( var ng in failure_ngrams )
                                {
                                    node.AddNgram( ng );
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

            #region [.ctor() & methods.]
            public TreeNode() : this( null, NerOutputType.Other ) { }
            public TreeNode( TreeNode parent, NerOutputType nerOutputType )
            {
                NerOutputType = nerOutputType;
                Parent        = parent;                                
            }

            public void AddNgram( NerOutputType[] ngram )
            {
                if ( _Ngrams == null ) _Ngrams = new Set< NerOutputType[] >( ngrams_EqualityComparer.Instance );
                _Ngrams.Add( ngram );
            }
            public void AddTransition( TreeNode node )
            {
                if ( _TransDict == null ) _TransDict = new Map< NerOutputType, TreeNode >();
                _TransDict.Add( node.NerOutputType, node );
            }
            [M(O.AggressiveInlining)] public TreeNode GetTransition( NerOutputType nerOutputType ) => ((_TransDict != null) && _TransDict.TryGetValue( nerOutputType, out var node ) ? node : null);
            [M(O.AggressiveInlining)] public bool ContainsTransition( NerOutputType nerOutputType ) => ((_TransDict != null) && _TransDict.ContainsKey( nerOutputType ));
            #endregion

            #region [.props.]
            private Map< NerOutputType, TreeNode > _TransDict;
            private Set< NerOutputType[] > _Ngrams;

            public NerOutputType NerOutputType { [M(O.AggressiveInlining)] get; private set; }
            public TreeNode      Parent        { [M(O.AggressiveInlining)] get; private set; }
            public TreeNode      Failure       { [M(O.AggressiveInlining)] get; internal set; }
            [M(O.AggressiveInlining)] public bool TryGetAllTransitions( ref Map< NerOutputType, TreeNode >.ReadOnlyCollection4Values trs )
            {
                if ( _TransDict != null )
                {
                    trs = _TransDict.GetValues();
                    return (true);
                }
                return (false);
            }
            [M(O.AggressiveInlining)] public bool TryGetNgrams( ref Set< NerOutputType[] > ngs )
            {
                if ( _Ngrams != null )
                {
                    ngs = _Ngrams;
                    return (true);
                }
                return (false);
            }
            public bool HasNgrams { [M(O.AggressiveInlining)] get => (_Ngrams != null); }
            public Set< NerOutputType[] > Ngrams { [M(O.AggressiveInlining)] get => _Ngrams; }
            #endregion
#if DEBUG
            public override string ToString() => (((Parent != null) ? ('\'' + NerOutputType.ToString() + '\'') : "ROOT") + ", transitions(descendants): " + ((_TransDict != null) ? _TransDict.Count : 0) + ", ngrams: " + ((_Ngrams != null) ? _Ngrams.Count : 0) );
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private struct Finder
        {
            private TreeNode _Root;
            private TreeNode _Node;
            public static Finder Create( TreeNode root ) => new Finder() { _Root = root, _Node = root };

            [M(O.AggressiveInlining)] public bool Find( NerOutputType nerOutputType, out TreeNode node )
            {
                TreeNode transNode;
                do
                {
                    transNode = _Node.GetTransition( nerOutputType );
                    if ( _Node == _Root )
                    {
                        break;
                    }
                    if ( transNode == null )
                    {
                        _Node = _Node.Failure;
                    }
                }
                while ( transNode == null );
                if ( transNode != null )
                {
                    _Node = transNode;
                }
                node = _Node;
                return (true);
            }
        }

        #region [.ctor().]
        private TreeNode _Root;
        private int      _MaxDistanceBetweenWords;
        public Searcher_v1( IList< NerOutputType[] > ngrams, int maxDistanceBetweenWords )
        {
            _Root = TreeNode.BuildTree( ngrams );
            _MaxDistanceBetweenWords = maxDistanceBetweenWords;
        }
        #endregion

        #region [.methods.]
        [M(O.AggressiveInlining)] public bool TryFindAll( DirectAccessList< (word_t w, int orderNum) > nerWords, out IReadOnlyCollection< SearchResult_v1 > results ) 
        {
            var ss = default(SortedSetByRef< SearchResult_v1 >);
            var finder = Finder.Create( _Root );

            var prev_orderNum = -1;
            for ( int index = 0, len = nerWords.Count; index < len; index++ )
            {
                ref readonly var t = ref nerWords._Items[ index ];

                if ( (((t.orderNum - prev_orderNum) <= _MaxDistanceBetweenWords) || (prev_orderNum == -1)) && 
                     finder.Find( t.w.nerOutputType, out var node ) && node.HasNgrams 
                   )
                {
                    if ( ss == null ) ss = new SortedSetByRef< SearchResult_v1 >( SearchResult_v1.Comparer.Instance );

                    switch ( node.Ngrams.Count )
                    {
                        case 1:
                            ss.AddEx( index, node.Ngrams.First );
                        break;

                        default:
                            foreach ( var ngram in node.Ngrams )
                            {
                                ss.AddEx( index, ngram );
                            }
                        break;
                    }
                }
                prev_orderNum = t.orderNum;
            }
            results = ss;
            return (ss != null);
        }
        #endregion
#if DEBUG
        public override string ToString() => $"[{_Root}]"; 
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Searcher_v1_Extensions
    {
        [M(O.AggressiveInlining)] public static void AddEx( this SortedSetByRef< SearchResult_v1 > ss, int startIndex, NerOutputType[] ngram )
        {
            var sr = new SearchResult_v1( startIndex, ngram );
            if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
            {
                ss.Remove( in exists );
            }
            ss.Add( in sr );
        }        
    }
}
