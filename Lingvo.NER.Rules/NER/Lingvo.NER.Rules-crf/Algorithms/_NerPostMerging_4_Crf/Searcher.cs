#if (!WITHOUT_CRF)

using System.Collections.Generic;

using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.NerPostMerging_4_Crf
{
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

            public bool Equals( in ngram_t x, in ngram_t y )
            {
                var len = x.NerOutputTypes.Length;
                if ( len != y.NerOutputTypes.Length )
                {
                    return (false);
                }

                for ( int i = 0; i < len; i++ )
                {
                    if ( x.NerOutputTypes[ i ] != y.NerOutputTypes[ i ] )
                    {
                        return (false);
                    }
                }
                return (true);
            }
            public int GetHashCode( in ngram_t obj ) => obj.NerOutputTypes.Length;
        }

        [M(O.AggressiveInlining)] public ngram_t( NerOutputType[] nerOutputTypes, NerOutputType resultNerOutputType )
        {
            NerOutputTypes      = nerOutputTypes;
            ResultNerOutputType = resultNerOutputType;
        }

        public NerOutputType[] NerOutputTypes      { [M(O.AggressiveInlining)] get; }
        public NerOutputType   ResultNerOutputType { [M(O.AggressiveInlining)] get; }
#if DEBUG
        public override string ToString() => $"nerOutputTypes: {NerOutputTypes.Length} => '{ResultNerOutputType}'"; 
#endif
    }

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

                d = x.StartIndex - y.StartIndex;
                if ( d != 0 )
                    return (d);

                return (y.NerOutputType - x.NerOutputType);
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
            StartIndex    = startIndex - ngram.NerOutputTypes.Length + 1;
            Length        = ngram.NerOutputTypes.Length;
            NerOutputType = ngram.ResultNerOutputType;
        }

        public int           StartIndex    { [M(O.AggressiveInlining)] get; }
        public int           Length        { [M(O.AggressiveInlining)] get; }
        public NerOutputType NerOutputType { [M(O.AggressiveInlining)] get; }
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}], NerOutputType: '{NerOutputType}'"; 
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class Searcher
    {
        /// <summary>
        ///
        /// </summary>
        private sealed class TreeNode
        {
            public static TreeNode BuildTree( IList< ngram_t > ngrams )
            {
                var transitions_root_nodes = default(Map< NerOutputType, TreeNode >.ReadOnlyCollection4Values);
                var transitions_nodes      = default(Map< NerOutputType, TreeNode >.ReadOnlyCollection4Values);
                var failure_ngrams         = default(SetByRef< ngram_t >);

                // Build keyword tree and transition function
                var root = new TreeNode();
                foreach ( var ngram in ngrams )
                {
                    // add pattern to tree
                    var node = root;
                    foreach ( var nerOutputType in ngram.NerOutputTypes )
                    {
                        if ( !node.TryGetTransition( nerOutputType, out var nodeNew ) )
                        {
                            nodeNew = new TreeNode( node, nerOutputType );
                            node.AddTransition( nodeNew );
                        }
                        node = nodeNew;
                    }
                    node.AddNgram( in ngram );
                }

                // Find failure functions
                var nodes = new List< TreeNode >();
                // level 1 nodes - fail to root node
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
                            var nr = node.Failure = r.GetTransition( nerOutputType );
                            if ( (nr != null) && nr.TryGetNgrams( ref failure_ngrams ) )
                            {
                                foreach ( var ng in failure_ngrams )
                                {
                                    node.AddNgram( in ng );
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

            public void AddNgram( in ngram_t ngram )
            {
                if ( _Ngrams == null )
                {
                    _Ngrams = new SetByRef< ngram_t >( ngram_t.EqualityComparer.Instance );
                }
                _Ngrams.Add( in ngram );
            }
            public void AddTransition( TreeNode node )
            {
                if ( _TransDict == null )
                {
                    _TransDict = new Map< NerOutputType, TreeNode >();
                }
                _TransDict.Add( node.NerOutputType, node );
            }
            [M(O.AggressiveInlining)] public TreeNode GetTransition( NerOutputType nerOutputType ) => ((_TransDict != null) && _TransDict.TryGetValue( nerOutputType, out var node ) ? node : null);
            [M(O.AggressiveInlining)] public bool TryGetTransition( NerOutputType nerOutputType, out TreeNode node )
            {
                if ( _TransDict != null )
                {
                    return (_TransDict.TryGetValue( nerOutputType, out node ));
                }
                node = default;
                return (false);
            }
            [M(O.AggressiveInlining)] public bool ContainsTransition( NerOutputType nerOutputType ) => ((_TransDict != null) && _TransDict.ContainsKey( nerOutputType ));
            #endregion

            #region [.props.]
            private Map< NerOutputType, TreeNode > _TransDict;
            private SetByRef< ngram_t > _Ngrams;

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
            [M(O.AggressiveInlining)] public bool TryGetNgrams( ref SetByRef< ngram_t > ngs )
            {
                if ( _Ngrams != null )
                {
                    ngs = _Ngrams;
                    return (true);
                }
                return (false);
            }
            public bool HasNgrams { [M(O.AggressiveInlining)] get => (_Ngrams != null); }
            public SetByRef< ngram_t > Ngrams { [M(O.AggressiveInlining)] get => _Ngrams; }
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

            [M(O.AggressiveInlining)] public bool Find( word_t word, out TreeNode node )
            {
                TreeNode transNode;
                do
                {
                    if ( word.IsWordInNerChain )
                    {
                        node = null;
                        return (false);
                    }
                    transNode = _Node.GetTransition( word.nerOutputType );
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
        public Searcher( IList< ngram_t > ngrams ) => _Root = TreeNode.BuildTree( ngrams );
        #endregion

        #region [.methods.]
        [M(O.AggressiveInlining)] public bool TryFindAll( List< word_t > words, out IReadOnlyCollection< SearchResult > results ) 
        {
            var ss = default(SortedSetByRef< SearchResult >);
            var finder = Finder.Create( _Root );

            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                if ( finder.Find( words[ index ], out var node ) && node.HasNgrams )
                {
                    if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );

                    switch ( node.Ngrams.Count )
                    {
                        case 1:
                            ss.Add( new SearchResult( index, node.Ngrams.First ) );
                        break;

                        default:
                            foreach ( var ngram in node.Ngrams )
                            {
                                ss.Add( new SearchResult( index, in ngram ) );
                            }
                        break;
                    }
                }
            }
            results = ss;
            return (ss != null);
        }
        #endregion
#if DEBUG
        public override string ToString() => $"[{_Root}]"; 
#endif
    }
}
#endif