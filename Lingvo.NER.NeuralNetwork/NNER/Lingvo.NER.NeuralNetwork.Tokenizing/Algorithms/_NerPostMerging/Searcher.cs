using System.Collections.Generic;
using System.Linq;

using Lingvo.NER.NeuralNetwork.Tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.NeuralNetwork.NerPostMerging
{
    /// <summary>
    /// 
    /// </summary>
    internal readonly struct ngram_t
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class EqualityComparer : IEqualityComparerByRef< ngram_t >
        {
            public static EqualityComparer Inst { [M(O.AggressiveInlining)] get; } = new EqualityComparer();
            private EqualityComparer() { }

            public bool Equals( in ngram_t x, in ngram_t y )
            {
                var len = x.NNerOutputTypes.Length;
                if ( len != y.NNerOutputTypes.Length )
                {
                    return (false);
                }

                for ( var i = 0; i < len; i++ )
                {
                    if ( x.NNerOutputTypes[ i ] != y.NNerOutputTypes[ i ] )
                    {
                        return (false);
                    }
                }
                return (true);
            }
            public int GetHashCode( in ngram_t obj ) => obj.NNerOutputTypes.Length;
        }

        [M(O.AggressiveInlining)] public ngram_t( NNerOutputType[] nnerOutputTypes, NerOutputType resultNerOutputType )
        {
            NNerOutputTypes     = nnerOutputTypes;
            ResultNerOutputType = resultNerOutputType;
        }
        public NNerOutputType[] NNerOutputTypes     { [M(O.AggressiveInlining)] get; }
        public NerOutputType    ResultNerOutputType { [M(O.AggressiveInlining)] get; }
#if DEBUG
        public override string ToString() => $"NNerOutputTypes: {NNerOutputTypes.Length} => '{ResultNerOutputType}'"; 
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class Searcher : ISearcher
    {
        /// <summary>
        ///
        /// </summary>
        private sealed class TreeNode
        {
            public static TreeNode BuildTree( IEnumerable< ngram_t > ngrams )
            {
                var transitions_root_nodes = default(Map< NNerOutputType, TreeNode >.ReadOnlyCollection4Values);
                var transitions_nodes      = default(Map< NNerOutputType, TreeNode >.ReadOnlyCollection4Values);
                var failure_ngrams         = default(SetByRef< ngram_t >);

                // Build keyword tree and transition function
                var root = new TreeNode();
                foreach ( var ngram in ngrams )
                {
                    // add pattern to tree
                    var node = root;
                    foreach ( var nnerOutputType in ngram.NNerOutputTypes )
                    {
                        if ( !node.TryGetTransition( nnerOutputType, out var nodeNew ) )
                        {
                            nodeNew = new TreeNode( node, nnerOutputType );
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
                        var nnerOutputType = node.NNerOutputType;

                        while ( (r != null) && !r.ContainsTransition( nnerOutputType ) )
                        {
                            r = r.Failure;
                        }
                        if ( r == null )
                        {
                            node.Failure = root;
                        }
                        else
                        {
                            var nr = node.Failure = r.GetTransition( nnerOutputType );
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
            public TreeNode() : this( null, NNerOutputType.Other ) { }
            public TreeNode( TreeNode parent, NNerOutputType nnerOutputType )
            {
                NNerOutputType = nnerOutputType;
                Parent        = parent;                                
            }

            public void AddNgram( in ngram_t ngram )
            {
                if ( _Ngrams == null )
                {
                    _Ngrams = new SetByRef< ngram_t >( ngram_t.EqualityComparer.Inst );
                }
                _Ngrams.Add( in ngram );
            }
            public void AddTransition( TreeNode node )
            {
                if ( _TransDict == null )
                {
                    _TransDict = new Map< NNerOutputType, TreeNode >();
                }
                _TransDict.Add( node.NNerOutputType, node );
            }
            [M(O.AggressiveInlining)] public TreeNode GetTransition( NNerOutputType nnerOutputType ) => ((_TransDict != null) && _TransDict.TryGetValue( nnerOutputType, out var node ) ? node : null);
            [M(O.AggressiveInlining)] public bool TryGetTransition( NNerOutputType nnerOutputType, out TreeNode node )
            {
                if ( _TransDict != null )
                {
                    return (_TransDict.TryGetValue( nnerOutputType, out node ));
                }
                node = default;
                return (false);
            }
            [M(O.AggressiveInlining)] public bool ContainsTransition( NNerOutputType nnerOutputType ) => ((_TransDict != null) && _TransDict.ContainsKey( nnerOutputType ));
            #endregion

            #region [.props.]
            private Map< NNerOutputType, TreeNode > _TransDict;
            private SetByRef< ngram_t > _Ngrams;

            public NNerOutputType NNerOutputType { [M(O.AggressiveInlining)] get; private set; }
            public TreeNode       Parent         { [M(O.AggressiveInlining)] get; private set; }
            public TreeNode       Failure        { [M(O.AggressiveInlining)] get; internal set; }
            [M(O.AggressiveInlining)] public bool TryGetAllTransitions( ref Map< NNerOutputType, TreeNode >.ReadOnlyCollection4Values trs )
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
            public override string ToString() => (((Parent != null) ? ('\'' + NNerOutputType.ToString() + '\'') : "ROOT") + ", transitions(descendants): " + ((_TransDict != null) ? _TransDict.Count : 0) + ", ngrams: " + ((_Ngrams != null) ? _Ngrams.Count : 0) );
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private struct Finder
        {
            private TreeNode _Root;
            private TreeNode _Node;
            [M(O.AggressiveInlining)] public static Finder Create( TreeNode root ) => new Finder() { _Root = root, _Node = root };

            [M(O.AggressiveInlining)] public bool Find( word_t word, out TreeNode node )
            {
                TreeNode transNode;
                do
                {
                    //if ( word.IsWordInNerChain )
                    //{
                    //    node = null;
                    //    return (false);
                    //}
                    transNode = _Node.GetTransition( word.nnerOutputType );
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
        public Searcher( IEnumerable< ngram_t > ngrams ) => _Root = TreeNode.BuildTree( ngrams );
        #endregion

        #region [.methods.]
        [M(O.AggressiveInlining)] public bool TryFindAll( IList< word_t > words, out IReadOnlyCollection< SearchResult > results ) 
        {
            var ss = default(SortedSetByRef< SearchResult >);
            var finder = Finder.Create( _Root );

            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                if ( finder.Find( words[ index ], out var node ) && node.HasNgrams )
                {
                    if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Inst );

                    switch ( node.Ngrams.Count )
                    {
                        case 1:
                            ss.Add( new SearchResult( index, node.Ngrams.First ) );
                            //ss.AddEx( index, node.Ngrams.First );
                            break;

                        default:
                            foreach ( var ngram in node.Ngrams )
                            {
                                ss.Add( new SearchResult( index, ngram ) );
                                //ss.AddEx( index, ngram );
                            }
                        break;
                    }
                }
            }
            results = ss;
            return (ss != null);
        }
        [M(O.AggressiveInlining)] public bool TryFindAll_2( IList< word_t > words, out IntervalList< SearchResult > results ) 
        {
            var ss = default(IntervalList< SearchResult >);
            var finder = Finder.Create( _Root );

            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                if ( finder.Find( words[ index ], out var node ) && node.HasNgrams )
                {
                    if ( ss == null ) ss = new IntervalList< SearchResult >();

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
    internal static class SearcherExtensions
    {
        [M(O.AggressiveInlining)] public static void AddEx( this IntervalList< SearchResult > ss, int startIndex, in ngram_t ngram )
        {            
            var sr = new SearchResult( startIndex, ngram );
            var t  = (sr.StartIndex, sr.Length);
            if ( ss.TryGetValue( t, out var exists ) )
            {
                if ( exists.Length < sr.Length )
                {
                    ss.Remove( t );
                    ss.Add( t, sr );
                }
            }
            else
            {
                ss.Add( t, sr );
            }            
        }

        //[M(O.AggressiveInlining)] public static void AddEx( this SortedSetByRef< SearchResult > ss, int startIndex, in ngram_t ngram )
        //{
        //    var sr = new SearchResult( startIndex, ngram );
        //    if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
        //    {
        //        ss.Remove( in exists );
        //    }
        //    else if ( ss.TryGetValue( new SearchResult( sr.StartIndex, sr.Length - 1, default ), out exists ) && (exists.Length < sr.Length) )
        //    {
        //        ss.Remove( in exists );
        //    }
        //    ss.Add( in sr );
        //}
    }
}
