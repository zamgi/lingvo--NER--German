using System.Collections.Generic;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using ngram_t = Lingvo.NER.Rules.BankAccounts.IBANSearcher.ngram_t;
using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.BankAccounts
{
    /// <summary>
    /// 
    /// </summary>
    internal readonly struct IBANSearchResult
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Comparer : IComparerByRef< IBANSearchResult >
        {
            public static Comparer Instance { get; } = new Comparer();
            private Comparer() { }
            public int Compare( in IBANSearchResult x, in IBANSearchResult y ) => (y.StartIndex - x.StartIndex);
        }

        [M(O.AggressiveInlining)] public IBANSearchResult( int startIndex, ngram_t[] ngrams )
        {
            StartIndex = startIndex - ngrams.Length + 1;
            Length     = ngrams.Length;
        }
        public int       StartIndex { [M(O.AggressiveInlining)] get; }
        public int       Length     { [M(O.AggressiveInlining)] get; }
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}]";
#endif
    }

    /// <summary>
    ///
    /// </summary>
    internal static class IBANSearcher
    {
        /// <summary>
        /// 
        /// </summary>
        internal enum ngramType : byte
        {
            __UNDEFINED__,

            fullValue,

            prefixValue,
            validNumber,
        }
        /// <summary>
        /// 
        /// </summary>
        internal struct ngram_t
        {
            public const int FULL_VALUE_LEN       = 22;
            public const int PREFIX_VALUE_LEN     = 4;
            public const int VALID_NUMBER_LEN     = 4;
            public const int END_VALID_NUMBER_LEN = 2;

            /// <summary>
            /// 
            /// </summary>
            public sealed class EqualityComparer : IEqualityComparerByRef< ngram_t >
            {
                public static EqualityComparer Instance { get; } = new EqualityComparer();
                private EqualityComparer() { }

                [M(O.AggressiveInlining)] public static bool _Equals_( in ngram_t x, in ngram_t y )
                {
                    if ( x.type == y.type )
                    {
                        if ( x.length.HasValue )
                        {
                            return (x.length.Value == y.length.GetValueOrDefault( x.length.Value ));
                        }
                        return (true);
                    }
                    return (false);
                }
                public bool Equals( in ngram_t x, in ngram_t y ) => _Equals_( in x, in y );
                public int GetHashCode( in ngram_t obj ) => obj.type.GetHashCode();
            }

            public static ngram_t UNDEFINED() => new ngram_t() { type = ngramType.__UNDEFINED__ };
            public static ngram_t FullValue() => new ngram_t() { type = ngramType.fullValue, length = FULL_VALUE_LEN };
            public static ngram_t PrefixValue() => new ngram_t() { type = ngramType.prefixValue, length = PREFIX_VALUE_LEN };
            public static ngram_t ValidNumber() => new ngram_t() { type = ngramType.validNumber, length = VALID_NUMBER_LEN };
            public static ngram_t EndValidNumber() => new ngram_t() { type = ngramType.validNumber, length = END_VALID_NUMBER_LEN };

            public ngramType type;
            public int?      length;
#if DEBUG
            public override string ToString() => $"{type}{(length.HasValue ? $", {length.Value}" : null)}";
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
                //'DE84574700470167154400'
                yield return (new[] { ngram_t.FullValue() });

                //'DE84 5747 0047 0167 1544 00'
                yield return (new[] { ngram_t.PrefixValue(), ngram_t.ValidNumber(), ngram_t.ValidNumber(), ngram_t.ValidNumber(), ngram_t.ValidNumber(), ngram_t.EndValidNumber() });
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
        static IBANSearcher() => _Root = TreeNode.BuildTree( Model.GetNgrams() );
        #endregion

        #region [.public method's.]
        public static bool TryFindAll( List< word_t > words, out IReadOnlyCollection< IBANSearchResult > results )
        {
            var ss = default(SortedSetByRef< IBANSearchResult >);
            var node = _Root;
            var finder = Finder.Create( _Root );

            var ng = new ngram_t();
            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                var success = Classify( words[ index ], ref ng );
                if ( !success && (node == _Root) )
                {
                    continue;
                }

                node = finder.Find( in ng );

                if ( node.HasNgrams )
                {
                    if ( ss == null ) ss = new SortedSetByRef< IBANSearchResult >( IBANSearchResult.Comparer.Instance );

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
            //if ( !w.IsOutputTypeOther() )
            //{
            //    ng.type = ngramType.__UNDEFINED__;
            //    return (false);
            //}

            switch ( w.nerInputType )
            {
                case NerInputType.NumCapital:
                {
                    switch ( w.length )
                    {
                        case ngram_t.FULL_VALUE_LEN:
                            if ( Has_DE_Prefix( w.valueOriginal ) )
                            {
                                ng.length = ngram_t.FULL_VALUE_LEN;
                                ng.type   = ngramType.fullValue;
                                return (true);
                            }
                        break;

                        case ngram_t.PREFIX_VALUE_LEN:
                            if ( Has_DE_Prefix( w.valueOriginal ) )
                            {
                                ng.length = ngram_t.PREFIX_VALUE_LEN;
                                ng.type   = ngramType.prefixValue;
                                return (true);
                            }
                        break;
                    }
                }
                break;

                case NerInputType.Num:
                {
                    if ( w.IsOutputTypeOther() && w.IsExtraWordTypeIntegerNumber() ) //&& (w.length == ) )
                    {
                        ng.length = w.length;
                        ng.type   = ngramType.validNumber;
                        return (true);
                    }
                }
                break;
            }

            ng.type = ngramType.__UNDEFINED__;
            return (false);
        }

        [M(O.AggressiveInlining)] private static bool Has_DE_Prefix( string s ) => ((s[ 0 ] == 'D') && (s[ 1 ] == 'E'));
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class IBANSearcherExtensions
    {
        [M(O.AggressiveInlining)] public static void AddEx( this SortedSetByRef< IBANSearchResult > ss, int startIndex, ngram_t[] ngrams )
        {
            var sr = new IBANSearchResult( startIndex, ngrams );
            if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
            {
                ss.Remove( in exists );
            }
            ss.Add( in sr );
        }        
    }
}