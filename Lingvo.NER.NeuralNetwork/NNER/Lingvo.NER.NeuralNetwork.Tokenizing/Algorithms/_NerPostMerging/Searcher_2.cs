using System.Collections.Generic;
using System.Diagnostics;

using Lingvo.NER.NeuralNetwork.Tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.NeuralNetwork.NerPostMerging
{
    using tuple = ngram_2_t.tuple;

    /// <summary>
    /// 
    /// </summary>
    internal readonly struct ngram_2_t
    {
        /// <summary>
        /// 
        /// </summary>
        public struct tuple
        {
            /// <summary>
            /// 
            /// </summary>
            public sealed class EqualityComparer : IEqualityComparerByRef< tuple >//, IEqualityComparer< tuple >
            {
                public static EqualityComparer Inst { [M(O.AggressiveInlining)] get; } = new EqualityComparer();
                private EqualityComparer() { }

                [M(O.AggressiveInlining)] public static bool _Equals_( in tuple x, in tuple y )
                {
                    if ( x.NNerOutputType == y.NNerOutputType )
                    {
                        if ( x.NNerOutputType == NNerOutputType.Other )
                        {
                            return (x.Value == y.Value);
                        }

                        if ( (x.Value != null) && (y.Value != null) )
                        {
                            return (x.Value == y.Value);
                        }
                        return (true);
                    }
                    return (false);
                }
                [M(O.AggressiveInlining)] public static int _GetHashCode_( in tuple t ) => t.NNerOutputType.GetHashCode();// ^ (t.Value ?? string.Empty).GetHashCode();

                public bool Equals( in tuple x, in tuple y ) => _Equals_( x, y );
                public int GetHashCode( in tuple t ) => _GetHashCode_( t );
            }

            public static tuple Create( NNerOutputType nt )
            {
                Debug.Assert( nt != NNerOutputType.Other );

                var t = new tuple()
                {
                    NNerOutputType = nt,
                    Value          = null,
                };
                return (t);
            }
            public static tuple Create( string v )
            {
                Debug.Assert( v != null );

                var t = new tuple()
                {
                    NNerOutputType = NNerOutputType.Other,
                    Value          = v,
                };
                return (t);
            }
            [M(O.AggressiveInlining)] public static tuple Create( NNerOutputType nt, string v )
            {
                //Debug.Assert( ((nt == NNerOutputType.Other) && (v != null)) || ((nt != NNerOutputType.Other) && (v == null)) );

                var t = new tuple()
                {
                    NNerOutputType = nt,
                    Value          = v,
                };
                return (t);
            }
            public NNerOutputType NNerOutputType { [M(O.AggressiveInlining)] get; private set; }
            public string         Value          { [M(O.AggressiveInlining)] get; private set; }

            public override string ToString() => $"{NNerOutputType}, '{Value}'";
        }

        /// <summary>
        /// 
        /// </summary>
        public sealed class EqualityComparer : IEqualityComparerByRef< ngram_2_t >
        {
            public static EqualityComparer Inst { [M(O.AggressiveInlining)] get; } = new EqualityComparer();
            private EqualityComparer() { }

            public bool Equals( in ngram_2_t x, in ngram_2_t y )
            {
                var len = x.Tuples.Length;
                if ( len != y.Tuples.Length )
                {
                    return (false);
                }

                for ( var i = 0; i < len; i++ )
                {
                    ref readonly var x_t = ref x.Tuples[ i ];
                    ref readonly var y_t = ref y.Tuples[ i ];
                    if ( !tuple.EqualityComparer._Equals_( x_t, y_t ) )
                    {
                        return (false);
                    }
                }
                return (true);
            }
            public int GetHashCode( in ngram_2_t obj ) => obj.Tuples.Length;
        }

        [M(O.AggressiveInlining)] public ngram_2_t( tuple[] tuples, NerOutputType resultNerOutputType )
        {
            Tuples              = tuples;
            ResultNerOutputType = resultNerOutputType;
        }
        public tuple[]       Tuples              { [M(O.AggressiveInlining)] get; }
        public NerOutputType ResultNerOutputType { [M(O.AggressiveInlining)] get; }
#if DEBUG
        public override string ToString() => $"Tuples: {Tuples.Length} => '{ResultNerOutputType}'"; 
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class Searcher_2 : ISearcher
    {
        /// <summary>
        ///
        /// </summary>
        private sealed class TreeNode
        {
            public static TreeNode BuildTree( IEnumerable< ngram_2_t > ngrams )
            {
                var transitions_root_nodes = default(MapByRef< tuple, TreeNode >.ReadOnlyCollection4Values);
                var transitions_nodes      = default(MapByRef< tuple, TreeNode >.ReadOnlyCollection4Values);
                var failure_ngrams         = default(SetByRef< ngram_2_t >);

                // Build keyword tree and transition function
                var root = new TreeNode();
                foreach ( var ngram in ngrams )
                {
                    // add pattern to tree
                    var node = root;
                    foreach ( var t in ngram.Tuples )
                    {
                        if ( !node.TryGetTransition( t, out var nodeNew ) )
                        {
                            nodeNew = new TreeNode( node, t );
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
                        ref readonly var t = ref node.Tuple;

                        while ( (r != null) && !r.ContainsTransition( t ) )
                        {
                            r = r.Failure;
                        }
                        if ( r == null )
                        {
                            node.Failure = root;
                        }
                        else
                        {
                            var nr = node.Failure = r.GetTransition( t );
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
            public TreeNode() : this( null, default ) { }
            public TreeNode( TreeNode parent, in tuple t )
            {
                _Tuple = t;
                Parent = parent;                                
            }

            public void AddNgram( in ngram_2_t ngram )
            {
                if ( _Ngrams == null )
                {
                    _Ngrams = new SetByRef< ngram_2_t >( ngram_2_t.EqualityComparer.Inst );
                }
                _Ngrams.Add( in ngram );
            }
            public void AddTransition( TreeNode node )
            {
                if ( _TransDict == null )
                {
                    _TransDict = new MapByRef< tuple, TreeNode >( tuple.EqualityComparer.Inst );
                }
                _TransDict.AddByRef( node._Tuple, node );
            }
            [M(O.AggressiveInlining)] public TreeNode GetTransition( in tuple t ) => ((_TransDict != null) && _TransDict.TryGetValue( t, out var node ) ? node : null);
            [M(O.AggressiveInlining)] public TreeNode GetTransition( NNerOutputType nt, string v ) => ((_TransDict != null) && _TransDict.TryGetValue( tuple.Create( nt, v ), out var node ) ? node : null);
            [M(O.AggressiveInlining)] public bool TryGetTransition( in tuple t, out TreeNode node )
            {
                if ( _TransDict != null )
                {
                    return (_TransDict.TryGetValue( t, out node ));
                }
                node = default;
                return (false);
            }
            [M(O.AggressiveInlining)] public bool ContainsTransition( in tuple t ) => ((_TransDict != null) && _TransDict.ContainsKey( t ));
            #endregion

            #region [.props.]
            private MapByRef< tuple, TreeNode > _TransDict;
            private SetByRef< ngram_2_t > _Ngrams;

            private tuple _Tuple;
            public ref readonly tuple Tuple { [M(O.AggressiveInlining)] get => ref _Tuple; }
            public TreeNode Parent  { [M(O.AggressiveInlining)] get; private set; }
            public TreeNode Failure { [M(O.AggressiveInlining)] get; internal set; }
            [M(O.AggressiveInlining)] public bool TryGetAllTransitions( ref MapByRef< tuple, TreeNode >.ReadOnlyCollection4Values trs )
            {
                if ( _TransDict != null )
                {
                    trs = _TransDict.GetValues();
                    return (true);
                }
                return (false);
            }
            [M(O.AggressiveInlining)] public bool TryGetNgrams( ref SetByRef< ngram_2_t > ngs )
            {
                if ( _Ngrams != null )
                {
                    ngs = _Ngrams;
                    return (true);
                }
                return (false);
            }
            public bool HasNgrams { [M(O.AggressiveInlining)] get => (_Ngrams != null); }
            public SetByRef< ngram_2_t > Ngrams { [M(O.AggressiveInlining)] get => _Ngrams; }
            #endregion
#if DEBUG
            public override string ToString() => (((Parent != null) ? ('\'' + _Tuple.ToString() + '\'') : "ROOT") + ", transitions(descendants): " + ((_TransDict != null) ? _TransDict.Count : 0) + ", ngrams: " + ((_Ngrams != null) ? _Ngrams.Count : 0) );
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private struct Finder
        {
            private TreeNode _Root;
            private TreeNode _Node;
            private bool     _UpperCase;
            [M(O.AggressiveInlining)] public static Finder Create( TreeNode root, bool upperCase ) => new Finder() { _Root = root, _Node = root, _UpperCase = upperCase };

            [M(O.AggressiveInlining)] public bool Find( word_t w, out TreeNode node )
            {
                TreeNode transNode;
                do
                {
                    transNode = _Node.GetTransition( w.nnerOutputType, (_UpperCase ? w.valueUpper : w.valueOriginal) );
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
        private bool     _UpperCase;
        public Searcher_2( IEnumerable< ngram_2_t > ngrams, bool upperCase )
        {
            _Root      = TreeNode.BuildTree( ngrams );
            _UpperCase = upperCase;
        }
        #endregion

        #region [.methods.]
        [M(O.AggressiveInlining)] public bool TryFindAll( IList< word_t > words, out IReadOnlyCollection< SearchResult > results ) 
        {
            var ss = default(SortedSetByRef< SearchResult >);
            var finder = Finder.Create( _Root, _UpperCase );

            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                if ( finder.Find( words[ index ], out var node ) && node.HasNgrams )
                {
                    if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Inst );

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
