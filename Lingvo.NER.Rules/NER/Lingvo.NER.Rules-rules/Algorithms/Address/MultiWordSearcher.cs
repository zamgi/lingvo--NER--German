using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Address
{
    /// <summary>
    /// 
    /// </summary>
    internal struct MultiWord_Ngram< TValue >
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class EqualityComparer : IEqualityComparer< MultiWord_Ngram< TValue > >
        {
            public static readonly EqualityComparer Instance = new EqualityComparer();
            private EqualityComparer() { }
            public bool Equals( MultiWord_Ngram< TValue > x, MultiWord_Ngram< TValue > y )
            {
                var len = x.Words.Length;
                if ( len != y.Words.Length )
                {
                    return (false);
                }

                for ( int i = 0; i < len; i++ )
                {
                    if ( !string.Equals( x.Words[ i ], y.Words[ i ] ) )
                    {
                        return (false);
                    }
                }
                return (true);
            }
            public int GetHashCode( MultiWord_Ngram< TValue > obj ) => obj.Words.Length;
        }

        [M(O.AggressiveInlining)] public MultiWord_Ngram( string[] words, TValue value )
        {
            Words = words;
            Value = value;
        }
        public string[] Words { [M(O.AggressiveInlining)] get; }
        public TValue   Value { [M(O.AggressiveInlining)] get; }
#if DEBUG
        public override string ToString() => $"'{string.Join( "' '", Words )}' ({Words.Length}), '{Value}'"; 
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public struct MultiWord_SearchResult< TValue >
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Comparer : IComparerByRef< MultiWord_SearchResult< TValue > >
        {
            public static Comparer Instance { get; } = new Comparer();
            private Comparer() { }
            public int Compare( in MultiWord_SearchResult< TValue > x, in MultiWord_SearchResult< TValue > y )
            {
                var d = y.Length - x.Length;
                if ( d != 0 )
                    return (d);

                return (x.StartIndex - y.StartIndex);
            }
        }

        [M(O.AggressiveInlining)] internal MultiWord_SearchResult( int startIndex, in MultiWord_Ngram< TValue > ngram )
        {
            StartIndex = startIndex - ngram.Words.Length + 1;
            Length     = ngram.Words.Length;
            Value      = ngram.Value;
        }

        public int    StartIndex { [M(O.AggressiveInlining)] get; }
        public int    Length     { [M(O.AggressiveInlining)] get; }
        public TValue Value      { [M(O.AggressiveInlining)] get; }
#if DEBUG
        public override string ToString()
        {
            var s = Value.ToString();
            return (s.IsNullOrEmpty() ? $"[{StartIndex}:{Length}]" : $"[{StartIndex}:{Length}], value: '{s}'");
        }
#endif
    }

    /// <summary>
    ///
    /// </summary>
    internal sealed class MultiWordSearcher< TValue >
    {
        /// <summary>
        ///
        /// </summary>
        private sealed class TreeNode
        {
            #region [.ctor() & methods.]
            [M(O.AggressiveInlining)] public TreeNode( TreeNode parent, string word )
            {
                Word   = word;
                Parent = parent;                
            }

            [M(O.AggressiveInlining)] public void AddNgram( MultiWord_Ngram< TValue > ngram )
            {
                if ( _Ngrams == null )
                {
                    _Ngrams = new Set< MultiWord_Ngram< TValue > >( MultiWord_Ngram< TValue >.EqualityComparer.Instance );
                }
                _Ngrams.Add( ngram );
            }
            [M(O.AggressiveInlining)] public void AddTransition( TreeNode node )
            {
                if ( _TransDict == null )
                {
                    _TransDict = new Map< string, TreeNode >();
                }
                _TransDict.Add( node.Word, node );
            }

            [M(O.AggressiveInlining)] public TreeNode GetTransition( string word ) => ((_TransDict != null) && _TransDict.TryGetValue( word, out var node )) ? node : null;
            [M(O.AggressiveInlining)] public bool ContainsTransition( string word ) => ((_TransDict != null) && _TransDict.ContainsKey( word ));
            #endregion

            public static TreeNode BuildTree( IEnumerable< MultiWord_Ngram< TValue > > ngrams )
            {
                var transitions_root_nodes = default(Map< string, TreeNode >.ReadOnlyCollection4Values);
                var transitions_nodes      = default(Map< string, TreeNode >.ReadOnlyCollection4Values);

                // Build keyword tree and transition function
                var root = new TreeNode( null, null );
                foreach ( var ngram in ngrams )
                {
                    // add pattern to tree
                    var node = root;
                    foreach ( string word in ngram.Words )
                    {
                        var nodeNew = node.GetTransition( word );
                        #region comm.
                        //TreeNode nodeNew = null;
                        //var transitions_nodes = node.Transitions;
                        //if ( transitions_nodes != null )
                        //{
                        //    foreach ( var trans in transitions_nodes )
                        //    {
                        //        if ( trans.Word == word )
                        //        {
                        //            nodeNew = trans;
                        //            break;
                        //        }
                        //    }
                        //} 
                        #endregion

                        if ( nodeNew == null )
                        {
                            nodeNew = new TreeNode( node, word );
                            node.AddTransition( nodeNew );
                        }
                        node = nodeNew;
                    }
                    node.AddNgram( ngram );
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
                        var word = node.Word;

                        while ( (r != null) && !r.ContainsTransition( word ) )
                        {
                            r = r.Failure;
                        }
                        if ( r == null )
                        {
                            node.Failure = root;
                        }
                        else
                        {
                            node.Failure = r.GetTransition( word );
                            var failure_ngrams = node.Failure?.Ngrams;
                            if ( failure_ngrams != null )
                            {
                                foreach ( var ngram in failure_ngrams )
                                {
                                    node.AddNgram( ngram );
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
            private Map< string, TreeNode > _TransDict;
            private Set< MultiWord_Ngram< TValue > > _Ngrams;

            public string Word { [M(O.AggressiveInlining)] get; private set; }
            public TreeNode Parent { [M(O.AggressiveInlining)] get; private set; }
            public TreeNode Failure { [M(O.AggressiveInlining)] get; internal set; }
            [M(O.AggressiveInlining)] public bool TryGetAllTransitions( ref Map< string, TreeNode >.ReadOnlyCollection4Values trs )
            {
                if ( _TransDict != null )
                {
                    trs = _TransDict.GetValues();
                    return (true);
                }
                return (false);
            }
            public Set< MultiWord_Ngram< TValue > > Ngrams { [M(O.AggressiveInlining)] get => _Ngrams; }
            public bool HasNgrams { [M(O.AggressiveInlining)] get => (_Ngrams != null); }
            #endregion
#if DEBUG
            public override string ToString() => ( ((Word != null) ? ('\'' + Word + '\'') : "ROOT") + ", transitions(descendants): " + ((_TransDict != null) ? _TransDict.Count : 0) + ", ngrams: " + ((_Ngrams != null) ? _Ngrams.Count : 0));
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

            [M(O.AggressiveInlining)] public TreeNode Find( string word )
            {
                if ( word == null )
                {
                    _Node = _Root;
                    return (_Node);
                }

                TreeNode transNode;
                do
                {
                    transNode = _Node.GetTransition( word );
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
                return (_Node);
            }
        }

        #region [.ctor().]
        public const string DASH      = "-";
        public const char   DASH_CHAR = '-';

        private TreeNode _Root;
        public MultiWordSearcher( IList< MultiWord_Ngram< TValue > > ngrams ) => _Root = TreeNode.BuildTree( ngrams );
        #endregion

        [M(O.AggressiveInlining)] public bool TryFindAll( List< word_t > words, int startIndex, out IReadOnlyCollection< MultiWord_SearchResult< TValue > > results )
        {
            var ss = default(SortedSetByRef< MultiWord_SearchResult< TValue > >);
            var finder = Finder.Create( _Root );

            for ( int len = words.Count; startIndex < len; startIndex++ )
            {
                var node = finder.Find( words[ startIndex ].valueOriginal );
                if ( node.HasNgrams )
                {
                    if ( ss == null ) ss = new SortedSetByRef< MultiWord_SearchResult< TValue > >( MultiWord_SearchResult< TValue >.Comparer.Instance );

                    switch ( node.Ngrams.Count )
                    {
                        case 1:
                        {
                            var sr = new MultiWord_SearchResult< TValue >( startIndex, node.Ngrams.First );
                            if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
                            {
                                ss.Remove( in exists );
                            }
                            var r = ss.Add( in sr );
#if DEBUG
                            Debug.Assert( r );
#endif
                        }
                        break;

                        default:
                            foreach ( var ngram in node.Ngrams )
                            {
                                var sr = new MultiWord_SearchResult< TValue >( startIndex, in ngram );
                                if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
                                {
                                    ss.Remove( in exists );
                                }
                                var r = ss.Add( in sr );
#if DEBUG
                                Debug.Assert( r ); 
#endif
                            }
                        break;
                    }
                }
            }
            results = ss;
            return (ss != null);
        }
        [M(O.AggressiveInlining)] public bool TryFindFirst( List< word_t > words, int startIndex, out MultiWord_SearchResult< TValue > result )
        {
            result = default;
            var finder = Finder.Create( _Root );

            for ( int index = startIndex, len = words.Count; index < len; index++ )
            {
                var w = words[ index ];
                var node = finder.Find( w.IsExtraWordTypeDash() ? DASH : w.valueOriginal );
                if ( node.HasNgrams )
                {
                    switch ( node.Ngrams.Count )
                    {
                        case 1:
                        {
                            var sr = new MultiWord_SearchResult< TValue >( index, node.Ngrams.First );
                            if ( result.Length < sr.Length ) result = sr;
                            }
                        break;

                        default:
                            foreach ( var ngram in node.Ngrams )
                            {
                                var sr = new MultiWord_SearchResult< TValue >( index, in ngram );
                                if ( result.Length < sr.Length ) result = sr;
                        }
                        break;
                    }
                    if ( result.StartIndex != startIndex )
                    {
                        return (false);
                    }
                }
                else if ( node == _Root )
                {
                    break;
                }
            }
            return (result.Length != 0);
        }
#if DEBUG
        public override string ToString() => _Root.ToString();
#endif
    }
}
