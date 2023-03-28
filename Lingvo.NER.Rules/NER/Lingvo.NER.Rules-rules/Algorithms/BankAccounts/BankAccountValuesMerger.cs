using System.Collections.Generic;

using Lingvo.NER.Rules.core.Infrastructure;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.BankAccounts
{
    /// <summary>
    /// 
    /// </summary>
    internal struct BankAccountValueTuple
    {
        [M(O.AggressiveInlining)] public BankAccountValueTuple( BankAccountWord _word, in ByTextPreamble_SearchResult _sr )
        {
            word = _word;
            sr   = _sr;
        }
        public BankAccountWord             word;
        public ByTextPreamble_SearchResult sr;
#if DEBUG
        public override string ToString() => $"{sr}; {word}";
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal readonly struct BankAccountValue_SearchResult
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Comparer : IComparerByRef< BankAccountValue_SearchResult >
        {
            public static Comparer Instance { get; } = new Comparer();
            private Comparer() { }
            public int Compare( in BankAccountValue_SearchResult x, in BankAccountValue_SearchResult y ) => (y.StartIndex - x.StartIndex);
        }

        [M(O.AggressiveInlining)] public BankAccountValue_SearchResult( int startIndex, TextPreambleTypeEnum[] ngrams )
        {
            StartIndex = startIndex - ngrams.Length + 1;
            Length     = ngrams.Length;
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
    internal static class BankAccountValuesMerger
    {
        /// <summary>
        ///
        /// </summary>
        private sealed class TreeNode
        {
            /// <summary>
            /// 
            /// </summary>
            private sealed class ngramsArray_EqualityComparer : IEqualityComparer< TextPreambleTypeEnum[] >
            {
                public static ngramsArray_EqualityComparer Instance { get; } = new ngramsArray_EqualityComparer();
                private ngramsArray_EqualityComparer() { }
                
                public bool Equals( TextPreambleTypeEnum[] x, TextPreambleTypeEnum[] y )
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
                public int GetHashCode( TextPreambleTypeEnum[] obj ) => obj.Length;
            }

            public static TreeNode BuildTree( IEnumerable< TextPreambleTypeEnum[] > ngrams )
            {
                var transitions_root_nodes = default(Map< TextPreambleTypeEnum, TreeNode >.ReadOnlyCollection4Values);
                var transitions_nodes      = default(Map< TextPreambleTypeEnum, TreeNode >.ReadOnlyCollection4Values);

                // Build keyword tree and transition function
                var root = new TreeNode( null, TextPreambleTypeEnum.__UNDEFINED__ );
                foreach ( var ngramsArray in ngrams )
                {
                    // add pattern to tree
                    var node = root;
                    foreach ( var ngram in ngramsArray )
                    {
                        var nodeNew = node.GetTransition( ngram );
                        if ( nodeNew == null )
                        {
                            nodeNew = new TreeNode( node, ngram );
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
                        var ngram = node.Ngram;

                        while ( (r != null) && !r.ContainsTransition( ngram ) )
                        {
                            r = r.Failure;
                        }
                        if ( r == null )
                        {
                            node.Failure = root;
                        }
                        else
                        {
                            node.Failure = r.GetTransition( ngram );
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
            private Map< TextPreambleTypeEnum, TreeNode > _TransDict;

            public TextPreambleTypeEnum Ngram { [M(O.AggressiveInlining)] get; private set; }
            public TreeNode Parent  { [M(O.AggressiveInlining)] get; private set; }
            public TreeNode Failure { [M(O.AggressiveInlining)] get; internal set; }

            public bool HasNgrams { [M(O.AggressiveInlining)] get => (Ngrams != null); }
            public Set< TextPreambleTypeEnum[] > Ngrams { [M(O.AggressiveInlining)] get; private set; }
            [M(O.AggressiveInlining)] public bool TryGetAllTransitions( ref Map< TextPreambleTypeEnum, TreeNode >.ReadOnlyCollection4Values trs )
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
            public TreeNode( TreeNode parent, TextPreambleTypeEnum ngarm )
            {
                Ngram  = ngarm;
                Parent = parent;
            }

            public void AddNgrams( TextPreambleTypeEnum[] ngrams )
            {
                if ( Ngrams == null )
                {
                    Ngrams = new Set< TextPreambleTypeEnum[] >( ngramsArray_EqualityComparer.Instance );
                }
                Ngrams.Add( ngrams );
            }
            public void AddTransition( TreeNode node )
            {
                if ( _TransDict == null )
                {
                    _TransDict = new Map< TextPreambleTypeEnum, TreeNode >();
                }
                _TransDict.Add( node.Ngram, node );
            }
            [M(O.AggressiveInlining)] public bool ContainsTransition( TextPreambleTypeEnum ngram ) => ((_TransDict != null) && _TransDict.ContainsKey( ngram ));
            [M(O.AggressiveInlining)] public TreeNode GetTransition( TextPreambleTypeEnum ngram ) => ((_TransDict != null) && _TransDict.TryGetValue( ngram, out var node ) ? node : null);            
            #endregion
#if DEBUG
            public override string ToString() => $"{((Ngram == TextPreambleTypeEnum.__UNDEFINED__) ? "ROOT" : Ngram.ToString())}, transitions(descendants): {(_TransDict?.Count).GetValueOrDefault()}, ngrams: {(Ngrams?.Count).GetValueOrDefault()}"; 
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private struct Finder
        {
            private TreeNode _Node;
            [M(O.AggressiveInlining)] public static Finder Create( int _ ) => new Finder() { _Node = _Root };

            [M(O.AggressiveInlining)] public TreeNode Find( TextPreambleTypeEnum ngram )
            {
                TreeNode transNode;
                do
                {
                    transNode = _Node.GetTransition( ngram );
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

        #region [.cctor().]
        private static TreeNode _Root;
        static BankAccountValuesMerger()
        {
            var ngrams = Permutator.GetPermutations( new[] { TextPreambleTypeEnum.BankCode, TextPreambleTypeEnum.AccountNumber, TextPreambleTypeEnum.BankName, TextPreambleTypeEnum.AccountOwner } );
            ngrams.AddRange( Permutator.GetPermutations( new[] { TextPreambleTypeEnum.BankCode, TextPreambleTypeEnum.AccountNumber, TextPreambleTypeEnum.BankName } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { TextPreambleTypeEnum.BankCode, TextPreambleTypeEnum.AccountNumber, TextPreambleTypeEnum.AccountOwner } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { TextPreambleTypeEnum.BankCode, TextPreambleTypeEnum.AccountNumber } ) );

            _Root = TreeNode.BuildTree( ngrams );
        }
        #endregion

        #region [.public method's.]
        public static bool TryFindAll( DirectAccessList< BankAccountValueTuple > tuples, out IReadOnlyCollection< BankAccountValue_SearchResult > results )
        {
            var ss = default(SortedSetByRef< BankAccountValue_SearchResult >);
            var finder = Finder.Create( 0 );

            for ( int index = 0, len = tuples.Count; index < len; index++ )
            {
                ref readonly var t = ref tuples._Items[ index ];
                var node = finder.Find( t.sr.TextPreambleType );

                if ( node.HasNgrams )
                {
                    if ( ss == null ) ss = new SortedSetByRef< BankAccountValue_SearchResult >( BankAccountValue_SearchResult.Comparer.Instance );

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
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class BankAccountValuesMergerExtensions
    {
        [M(O.AggressiveInlining)] public static void AddEx( this SortedSetByRef< BankAccountValue_SearchResult > ss, int startIndex, TextPreambleTypeEnum[] ngrams )
        {
            var sr = new BankAccountValue_SearchResult( startIndex, ngrams );
            if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
            {
                ss.Remove( in exists );
            }
            ss.Add( in sr );
        }        
    }
}
