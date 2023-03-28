using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.Algorithms;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.PhoneNumbers
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
            public int Compare( in SearchResult x, in SearchResult y ) => (y.StartIndex - x.StartIndex);
        }

        [M(O.AggressiveInlining)] public SearchResult( int startIndex, int length )
        {
            StartIndex = startIndex;
            Length     = length;
        }

        public int StartIndex { [M(O.AggressiveInlining)] get; }
        public int Length     { [M(O.AggressiveInlining)] get; }
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}]";
#endif
    }
    /// <summary>
    /// 
    /// </summary>
    internal readonly struct SearchResult_v2
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Comparer : IComparerByRef< SearchResult_v2 >
        {
            public static Comparer Instance { get; } = new Comparer();
            private Comparer() { }
            public int Compare( in SearchResult_v2 x, in SearchResult_v2 y ) => (y.StartIndex - x.StartIndex);
        }

        [M(O.AggressiveInlining)] public SearchResult_v2( int startIndex, int length, PhoneNumberTypeEnum phoneNumberType, int preambleWordIndex )
        {
            StartIndex        = startIndex;
            Length            = length;
            PhoneNumberType   = phoneNumberType;
            PreambleWordIndex = preambleWordIndex;
        }

        public int StartIndex { [M(O.AggressiveInlining)] get; }
        public int Length     { [M(O.AggressiveInlining)] get; }
        public int PreambleWordIndex { [M(O.AggressiveInlining)] get; }
        public PhoneNumberTypeEnum PhoneNumberType { [M(O.AggressiveInlining)] get; }
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}, {PhoneNumberType}]"; 
#endif
    }

    /// <summary>
    ///
    /// </summary>
    internal sealed class PhoneNumbersSearcher
    {
        /// <summary>
        /// 
        /// </summary>
        private enum ngramType : byte
        {
            __UNDEFINED__,

            code_49,
            validNumber,
            dash,
            bracketLeft,
            bracketRight,
            plus,
            slash,
        }
        /// <summary>
        /// 
        /// </summary>
        private struct ngram_t
        {
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
                        if ( x.type == ngramType.code_49 )
                        {
                            return (x.value == y.value);
                        }
                        if ( x.length.HasValue )
                        {
                            return (x.length.Value == y.length.GetValueOrDefault( x.length.Value ));
                        }
                        return (true);
                    }
                    return (false);
                }
                public bool Equals( in ngram_t x, in ngram_t y ) => _Equals_( in x, in y );
                public int GetHashCode( in ngram_t obj ) => obj.type.GetHashCode(); // ^ obj.length.GetValueOrDefault( -1 ).GetHashCode();
            }

            public static ngram_t UNDEFINED() => new ngram_t() { type = ngramType.__UNDEFINED__ };
            public static ngram_t Code_49() => new ngram_t() { type = ngramType.code_49 };
            public static ngram_t Number( int len ) => new ngram_t() { type = ngramType.validNumber, length = len };
            public static ngram_t NumberAnyLen() => new ngram_t() { type = ngramType.validNumber };
            public static ngram_t Dash() => new ngram_t() { type = ngramType.dash, length = 1 };
            public static ngram_t BracketLeft() => new ngram_t() { type = ngramType.bracketLeft, length = 1 };
            public static ngram_t BracketRight() => new ngram_t() { type = ngramType.bracketRight, length = 1 };
            public static ngram_t Plus() => new ngram_t() { type = ngramType.plus, length = 1 };
            public static ngram_t Slash() => new ngram_t() { type = ngramType.slash, length = 1 };

            public ngramType type;
            public int?      length;
            public string    value;
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
                var transitions_root_nodes = root.Transitions;
                if ( transitions_root_nodes != null )
                {
                    nodes.Capacity = transitions_root_nodes.Count;

                    foreach ( TreeNode node in transitions_root_nodes )
                    {
                        node.Failure = root;
                        var transitions_nodes = node.Transitions;
                        if ( transitions_nodes != null )
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
                        var transitions_nodes = node.Transitions;
                        if ( transitions_nodes != null )
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
            public IReadOnlyCollection< ngram_t[] > Ngrams { [M(O.AggressiveInlining)] get => _Ngrams; }
            public IReadOnlyCollection< TreeNode > Transitions { [M(O.AggressiveInlining)] get => _TransDict?.GetValues(); }
            #endregion

            #region [.ctor() & methods.]
            public TreeNode( TreeNode parent, in ngram_t ngarm ) => (_Ngram, Parent) = (ngarm, parent);

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
            #region [.ctor().]
            private Model( IEnumerable< ngram_t[] > ngrams ) => Root = TreeNode.BuildTree( ngrams );
            #endregion

            #region [.Instance.]
            private static volatile Model _Instance;
            public static Model Instance
            {
                get
                {
                    if ( _Instance == null )
                    {
                        lock ( typeof(Model) )
                        {
                            if ( _Instance == null )
                            {
                                _Instance = new Model( GetNgrams() );
                            }
                        }
                    }
                    return (_Instance);
                }
            }

            public static void ResetInstanceToNull()
            {
                if ( _Instance != null )
                {
                    lock ( typeof(Model) )
                    {
                        _Instance = null;
                    }
                }
            }
            #endregion

            public TreeNode Root  { get; }

            /*private static IEnumerable< ngram_t[] > GetNgrams__PREV()
            {
                #region [.#1.]
                //'8 (495) 123 - 45 - 67'
                yield return (new[] { ngram_t.Number( 1 ),
                                      ngram_t.BracketLeft(), ngram_t.Number( 3 ), ngram_t.BracketRight(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ), });
                //'8 (495) - 123 - 45 - 67'
                yield return (new[] { ngram_t.Number( 1 ),
                                      ngram_t.BracketLeft(), ngram_t.Number( 3 ), ngram_t.BracketRight(),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ), });
                //'8 - (495) 123 - 45 - 67'
                yield return (new[] { ngram_t.Number( 1 ),
                                      ngram_t.Dash(),
                                      ngram_t.BracketLeft(), ngram_t.Number( 3 ), ngram_t.BracketRight(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ), });
                //'8 - (495) - 123 - 45 - 67'
                yield return (new[] { ngram_t.Number( 1 ),
                                      ngram_t.Dash(),
                                      ngram_t.BracketLeft(), ngram_t.Number( 3 ), ngram_t.BracketRight(),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ), });
                #endregion

                #region [.#2.]
                //'+8 (495) 123 - 45 - 67'
                yield return (new[] { ngram_t.Plus(),
                                      ngram_t.Number( 1 ),
                                      ngram_t.BracketLeft(), ngram_t.Number( 3 ), ngram_t.BracketRight(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ), });
                //'+8 (495) - 123 - 45 - 67'
                yield return (new[] { ngram_t.Plus(),
                                      ngram_t.Number( 1 ),
                                      ngram_t.BracketLeft(), ngram_t.Number( 3 ), ngram_t.BracketRight(),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ), });
                //'+8 - (495) 123 - 45 - 67'
                yield return (new[] { ngram_t.Plus(),
                                      ngram_t.Number( 1 ),
                                      ngram_t.Dash(),
                                      ngram_t.BracketLeft(), ngram_t.Number( 3 ), ngram_t.BracketRight(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ), });
                //'+8 - (495) - 123 - 45 - 67'
                yield return (new[] { ngram_t.Plus(),
                                      ngram_t.Number( 1 ),
                                      ngram_t.Dash(),
                                      ngram_t.BracketLeft(), ngram_t.Number( 3 ), ngram_t.BracketRight(),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ), });
                #endregion

                #region [.#3.]
                //'8 - 495 - 123 - 45 - 67'
                yield return (new[] { ngram_t.Number( 1 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ), });
                //'8495 - 123 - 45 - 67'
                yield return (new[] { ngram_t.Number( 4 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ), });
                #endregion

                #region [.#4.]
                //'+8 - 495 - 123 - 45 - 67'
                yield return (new[] { ngram_t.Plus(),
                                      ngram_t.Number( 1 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ), });
                //'+8495 - 123 - 45 - 67'
                yield return (new[] { ngram_t.Plus(),
                                      ngram_t.Number( 4 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ), });
                #endregion

                #region [.#5.]
                //'123 - 45 - 67'
                yield return (new[] { ngram_t.Number( 3 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ),
                                      ngram_t.Dash(),
                                      ngram_t.Number( 2 ), });
                #endregion

                #region [.#6.]
                //'+4915140513399'
                yield return (new[] { ngram_t.Plus(),
                                      ngram_t.Number( 13 ), });
                #endregion
            }*/
            private static IEnumerable< ngram_t[] > GetNgrams()
            {
                #region [.#1.]
                //'+4915140513399'
                yield return (new[] { ngram_t.Plus(), ngram_t.Number( 13 ), });
                yield return (new[] { ngram_t.Plus(), ngram_t.Number( 14 ), });
                yield return (new[] { ngram_t.Plus(), ngram_t.Number( 15 ), });

                //'+49 15140513399'
                yield return (new[] { ngram_t.Plus(), ngram_t.Code_49(), ngram_t.NumberAnyLen(), });
                #endregion

                #region [.#2.]
                //'+49-211-447-299-22'
                yield return (new[] { ngram_t.Plus(), ngram_t.Code_49(),//Number( 2 ),
                                      ngram_t.Dash(), ngram_t.NumberAnyLen(),//( 3 ),
                                      ngram_t.Dash(), ngram_t.NumberAnyLen(),//( 3 ),
                                      ngram_t.Dash(), ngram_t.NumberAnyLen(),//( 3 ),
                                      ngram_t.Dash(), ngram_t.NumberAnyLen(),//( 2 ), 
                                    });

                //'+49 173 72 828 82'
                yield return (new[] { ngram_t.Plus(), ngram_t.Code_49(),//Number( 2 ),
                                      ngram_t.NumberAnyLen(),
                                      ngram_t.NumberAnyLen(),
                                      ngram_t.NumberAnyLen(),
                                      ngram_t.NumberAnyLen(), });

                //'+49 221 993835-64, +49 89 5999075- 13 , +49 89 5999075 - 22, +49 231 438-07'
                yield return (new[] { ngram_t.Plus(), ngram_t.Code_49(),//Number( 2 ),
                                      ngram_t.NumberAnyLen(),
                                      ngram_t.NumberAnyLen(), ngram_t.Dash(),
                                      ngram_t.NumberAnyLen(), });

                //'+49 203 604 2088'
                yield return (new[] { ngram_t.Plus(), ngram_t.Code_49(),//Number( 2 ),
                                      ngram_t.NumberAnyLen(),
                                      ngram_t.NumberAnyLen(), 
                                      ngram_t.NumberAnyLen(), });

                //'+49 (2331) 904162 , +49 (2331) 904174'
                yield return (new[] { ngram_t.Plus(), ngram_t.Code_49(),//Number( 2 ),
                                      ngram_t.BracketLeft(), ngram_t.NumberAnyLen(), ngram_t.BracketRight(),
                                      ngram_t.NumberAnyLen(), });

                //'+49 (231) 547 2284 , +49 (231) 547 3202'
                yield return (new[] { ngram_t.Plus(), ngram_t.Code_49(),//Number( 2 ),
                                      ngram_t.BracketLeft(), ngram_t.NumberAnyLen(), ngram_t.BracketRight(),
                                      ngram_t.NumberAnyLen(), ngram_t.NumberAnyLen(), });

                //'+49 162 5264901'
                yield return (new[] { ngram_t.Plus(), ngram_t.Code_49(),//Number( 2 ),
                                      ngram_t.NumberAnyLen(),
                                      ngram_t.NumberAnyLen(), });
                #endregion

                #region [.#3.]
                //'+49 (0) 6196 908 1009', '+49 (0) 176 1885 2189'
                yield return (new[] { ngram_t.Plus(), ngram_t.Code_49(),//Number( 2 ),
                                      ngram_t.BracketLeft(), ngram_t.Number( 1 ), ngram_t.BracketRight(),
                                      ngram_t.NumberAnyLen(),
                                      ngram_t.NumberAnyLen(),
                                      ngram_t.NumberAnyLen(), });

                //'+49(0) 208-8836872-0', '+49(0) 208-8836872-2'
                yield return (new[] { ngram_t.Plus(), ngram_t.Code_49(),//Number( 2 ),
                                      ngram_t.BracketLeft(), ngram_t.Number( 1 ), ngram_t.BracketRight(),
                                      ngram_t.NumberAnyLen(), ngram_t.Dash(),
                                      ngram_t.NumberAnyLen(), ngram_t.Dash(),
                                      ngram_t.NumberAnyLen(), });

                //'+49(0) 151-40513399'
                yield return (new[] { ngram_t.Plus(), ngram_t.Code_49(),//Number( 2 ),
                                      ngram_t.BracketLeft(), ngram_t.Number( 1 ), ngram_t.BracketRight(),
                                      ngram_t.NumberAnyLen(), ngram_t.Dash(),
                                      ngram_t.NumberAnyLen(), });
                #endregion

                #region [.#4.]
                //'0800 / 9100 300', 0800 / 88 88 710, 0180 / 58 57 11 00, 0180 / 58 57 11 09
                yield return (new[] { ngram_t.Number( 4 ), ngram_t.Slash(), ngram_t.NumberAnyLen(), ngram_t.NumberAnyLen(), });
                yield return (new[] { ngram_t.Number( 4 ), ngram_t.Slash(), ngram_t.NumberAnyLen(), ngram_t.NumberAnyLen(), ngram_t.NumberAnyLen(), });
                yield return (new[] { ngram_t.Number( 4 ), ngram_t.Slash(), ngram_t.NumberAnyLen(), ngram_t.NumberAnyLen(), ngram_t.NumberAnyLen(), ngram_t.NumberAnyLen(), });

                //'0208/45002-211, 0208/45002-399'
                yield return (new[] { ngram_t.Number( 4 ), ngram_t.Slash(), ngram_t.NumberAnyLen(), ngram_t.Dash(), ngram_t.NumberAnyLen(), });
                #endregion

                #region [.#5.]
                //'0 71 32/34 14-60'
                yield return (new[] { ngram_t.Number( 1 ), ngram_t.Number( 2 ), ngram_t.Number( 2 ), ngram_t.Slash(),
                                      ngram_t.Number( 2 ), ngram_t.Number( 2 ), ngram_t.Dash(), ngram_t.Number( 2 ), });

                //'(0 70 42) 28 85 85'
                yield return (new[] { ngram_t.BracketLeft(), ngram_t.Number( 1 ), ngram_t.Number( 2 ), ngram_t.Number( 2 ), ngram_t.BracketRight(),
                                      ngram_t.Number( 2 ), ngram_t.Number( 2 ), ngram_t.Number( 2 ), });
                #endregion

                #region [.#6.]
                //'0176 96266349' - ?!?!?!?!?!
                yield return (new[] { ngram_t.Number( 4 ), ngram_t.Number( 8 ), });
                #endregion
            }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        private struct Finder
        {
            private TreeNode _Root;
            private TreeNode _Node;
            [M(O.AggressiveInlining)] public static Finder Create( TreeNode root ) => new Finder() { _Root = root, _Node = root };

            [M(O.AggressiveInlining)] public TreeNode Find( in ngram_t ng )
            {
                TreeNode transNode;
                do
                {
                    transNode = _Node.GetTransition( in ng );
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
        private TreeNode _Root;
        public PhoneNumbersSearcher()
        {
            _Root = Model.Instance.Root;

            #region comm.
            //(int nullTransitionsCount, int notNullTransitionsCount) x( TreeNode node )
            //{
            //    var nullTransitionsCount    = 0;
            //    var notNullTransitionsCount = 0;
            //    if ( !node.Transitions.Any() )
            //    {
            //        nullTransitionsCount = 1;
            //    }
            //    else
            //    {
            //        notNullTransitionsCount = 1;
            //        foreach ( var n in node.Transitions )
            //        {
            //            var _t = x( n );
            //            nullTransitionsCount    += _t.nullTransitionsCount;
            //            notNullTransitionsCount += _t.notNullTransitionsCount;
            //        }
            //    }
            //    return (nullTransitionsCount, notNullTransitionsCount);
            //};
            //var t = x( _Root ); 
            #endregion
        }
        #endregion

        #region [.public methods.]
        public IReadOnlyCollection< SearchResult > FindAll( List< word_t > words )
        {
            var ss = default(SortedSetByRef< SearchResult >);
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
                    if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );

                    foreach ( var ngrams in node.Ngrams )
                    {
                        var sr = new SearchResult( index - ngrams.Length + 1, ngrams.Length );
                        if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
                        {
                            ss.Remove( in exists );
                        }
                        ss.Add( in sr );
                    }
                }
            }
            return (ss);
        }
        #endregion

        #region [.text classifier.]
        [M(O.AggressiveInlining)] private static bool Classify( word_t w, ref ngram_t ng )
        {
            const char BRACKET_LEFT  = '(';
            const char BRACKET_RIGHT = ')';
            const char PLUS          = '+';
            const char SLASH         = '/';
            const string CODE_49     = "49";

            if ( !w.IsOutputTypeOther() )
            {
                ng.type = ngramType.__UNDEFINED__;
                return (false);
            }

            switch ( w.nerInputType )
            {
                case NerInputType.Num:
                {
                    if ( w.IsExtraWordTypeIntegerNumber() )
                    {
                        ng.length = w.valueUpper.Length;
                        ng.type   = ((ng.length == 2) && (w.valueUpper == CODE_49)) ? ngramType.code_49 : ngramType.validNumber;                        
                        return (true);
                    }
                    ng.type = ngramType.__UNDEFINED__;
                    return (false);

                    #region comm.
                    /*
                    var v = w.valueUpper;
                    var si = 0;
                    switch ( v[ si ] )
                    {
                        case '+': case '-': si++; break;
                    }
                    for ( int i = si, len = v.Length; i < len; i++ )
                    {
                        if ( !v[ i ].IsDigit() )
                        {
                            ng.type = ngramType.__UNDEFINED__;
                            return (false);
                        }
                    }
                    ng.type   = ngramType.validNumber;
                    ng.length = v.Length - si;
                    return (true);
                    */
                    #endregion
                }

                case NerInputType.Other:
                {
                    if ( w.length == 1 )
                    {
                        ng.length = 1;
                        var ch = w.valueUpper.FirstChar();
                        switch ( ch )
                        {
                            case BRACKET_LEFT : ng.type = ngramType.bracketLeft;  return (true);
                            case BRACKET_RIGHT: ng.type = ngramType.bracketRight; return (true);
                            case PLUS         : ng.type = ngramType.plus;         return (true);
                            case SLASH        : ng.type = ngramType.slash;        return (true);
                            default: 
                                if ( ch.IsHyphen() )
                                {
                                    ng.type = ngramType.dash;
                                    return (true);
                                }
                            break;
                        }
                    }
                }
                break;
            }

            ng.type = ngramType.__UNDEFINED__;
            return (false);
        }
        #endregion

        #region [.model instance.]
        public static void ResetModelInstanceToNull() => Model.ResetInstanceToNull();
        #endregion
#if DEBUG
        public override string ToString() => $"[{_Root}]";
#endif
    }

    /// <summary>
    ///
    /// </summary>
    unsafe internal sealed class PhoneNumbersSearcher_ByTextPreamble
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class WordsChainDictionary
        {
            private Map< string, WordsChainDictionary > _Slots;
            private PhoneNumberTypeEnum? _PhoneNumberTypeEnum;
            public WordsChainDictionary( int capacity ) => _Slots = Map< string, WordsChainDictionary >.CreateWithCloserCapacity( capacity );
            private WordsChainDictionary() => _Slots = new Map< string, WordsChainDictionary >();

            #region [.append words.]      
            public void Add( IList< word_t > words, PhoneNumberTypeEnum pnt )
            {
                var startIndex = 0;
                for ( WordsChainDictionary _this = this, _this_next; ; )
                {
                    #region [.save.]
                    if ( words.Count == startIndex )
	                {
                        if ( _this._PhoneNumberTypeEnum.HasValue )
                        {
                            if ( _this._PhoneNumberTypeEnum.Value == pnt )
                            {
                                return;
                            }

                            throw (new InvalidDataException());
                        }
                        _this._PhoneNumberTypeEnum = pnt;
                        return;
                    }
                    #endregion

                    var v = words[ startIndex ].valueUpper;
                    if ( !_this._Slots.TryGetValue( v, out _this_next ) )
                    {
                        //add next word in chain
                        _this_next = new WordsChainDictionary();
                        _this._Slots.Add( v, _this_next );
                    }                
                    _this = _this_next;
                    startIndex++;
                }
            }
            #endregion

            #region [.try get.]
            [M(O.AggressiveInlining)] public bool TryGetFirst( IList< word_t > words, int startIndex, out (PhoneNumberTypeEnum phoneNumberType, int length) x )
            {
                x = default;

                var startIndex_saved = startIndex;
                for ( WordsChainDictionary _this = this, _this_next; ; )
                {
                    #region [.get.]
                    if ( _this._PhoneNumberTypeEnum.HasValue )
                    {
                        x = (_this._PhoneNumberTypeEnum.Value, startIndex - startIndex_saved);
                    }
                    #endregion

                    if ( words.Count == startIndex )
                    {
                        break;
                    }

                    if ( !_this._Slots.TryGetValue( words[ startIndex ].valueUpper, out _this_next ) )
                    {
                        break;
                    }
                    _this = _this_next;
                    startIndex++;
                }

                return (x.length != 0);// && (x.pnt != PhoneNumberTypeEnum.__UNDEFINED__);
            }
            #endregion
#if DEBUG
            public override string ToString() => (_PhoneNumberTypeEnum.HasValue ? _PhoneNumberTypeEnum.Value.ToString() : $"count: {_Slots.Count}");
#endif
        }

        #region [.cctor().]
        private const char PLUS          = '+';
        private const char LEFT_BRACKET  = '(';
        private const char RIGHT_BRACKET = ')';

        private static WordsChainDictionary _TextPreambles;
        private static Set< char > _AllowedPunctuation_AfterTextPreamble;
        private static Set< char > _AllowedPunctuation_BetweenDigits;
        static PhoneNumbersSearcher_ByTextPreamble()
        {
            _AllowedPunctuation_AfterTextPreamble = xlat.GetHyphens().Concat( new[] { ':', '.' } ).ToSet();
            _AllowedPunctuation_BetweenDigits     = xlat.GetHyphens().Concat( new[] { '/', LEFT_BRACKET, RIGHT_BRACKET } ).ToSet();
         
            //------------------------------------------------------------------------------//
            var tuples = new[] 
            {
                (w: "Cell Phone", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Cell Phone No", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Cell Phone No.", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Cell Phone Number", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Contact", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Contact No.", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Fax", pnt: PhoneNumberTypeEnum.Fax),
                (w: "Festnetz", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Festnetznr", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Festnetznr.", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Festnetznummer", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Handy", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Handynr", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Handynr.", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Handynummer", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Kontakt", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Kontaktnr", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Kontaktnr.", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Kontaktnummer", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Landline", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Landline no", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Landline no.", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Mobil", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Mobile", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Mobile", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Mobile No", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Mobile No.", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Mobile Number", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Mobile Phone", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Mobile Phone No", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Mobile Phone No.", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Mobile Phone Number", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "mobilen Nummer", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Mobilfunknr", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Mobilfunknr.", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Mobilfunknummer", pnt: PhoneNumberTypeEnum.Mobile),
                (w: "Phone", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Phone", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Phone No", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Phone No.", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Phone Number", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Tel", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Tel", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Tel Nr", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Tel Nr.", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Tel-Nr", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Tel-Nr.", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Tel.", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Tel.-Nr", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Tel.-Nr.", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Tel.Nr", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Tel.Nr.", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Telefax", pnt: PhoneNumberTypeEnum.Fax),
                (w: "Telefon", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Telefonnr", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Telefonnr.", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Telefonnummer", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Telephone", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Telephone", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Telephone No", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Telephone No.", pnt: PhoneNumberTypeEnum.Telephone),
                (w: "Telephone Number", pnt: PhoneNumberTypeEnum.Telephone),
            };

            _TextPreambles = new WordsChainDictionary( tuples.Length );

            using var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate();

            foreach ( var (w, pnt) in tuples )
            {
                var tokens = tokenizer.Run_NoSentsNoUrlsAllocate( w );
                _TextPreambles.Add( tokens, pnt );

                if ( tokens.Last().valueUpper.LastChar() == '.' )
                {
                    tokens = tokenizer.Run_NoSentsNoUrlsAllocate( w + " 1234567" );
                    tokens.RemoveAt_Ex( tokens.Count - 1 );
                    _TextPreambles.Add( tokens, pnt );
                }
            }
        }
        #endregion

        #region [.ctor().]
        private BracketBalancer _BracketBalancer;
        public PhoneNumbersSearcher_ByTextPreamble() => _BracketBalancer = new BracketBalancer( new[] { (LEFT_BRACKET, RIGHT_BRACKET) } );
        #endregion

        [M(O.AggressiveInlining)] private bool IsPlus( word_t w ) => ((w.length == 1) && (w.valueUpper[ 0 ] == PLUS));
        [M(O.AggressiveInlining)] private bool IsAllowedPunctuation_AfterTextPreamble( word_t w ) => ((w.length == 1) && _AllowedPunctuation_AfterTextPreamble.Contains( w.valueUpper[ 0 ] ));
        [M(O.AggressiveInlining)] private bool IsAllowedPunctuation_BetweenDigits( word_t w ) => ((w.length == 1) && _AllowedPunctuation_BetweenDigits.Contains( w.valueUpper[ 0 ] ));
        
        public bool TryFindAll( List< word_t > words, out IReadOnlyCollection< SearchResult_v2 > results )
        {
            var ss = default(SortedSetByRef< SearchResult_v2 >);

            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                var w = words[ index ];
                if ( w.IsInputTypeNum() || w.IsExtraWordTypePunctuation() ) //!w.IsOutputTypeOther() )
                {
                    continue;
                }

                if ( !_TextPreambles.TryGetFirst( words, index, out var x ) )
                {
                    continue;
                }
                var preambleWordIndex = index;

                var startIndex = index + x.length;
                var words_count = 0;
                if ( (startIndex < len) && IsAllowedPunctuation_AfterTextPreamble( words[ startIndex ] ) )
                {
                    startIndex++;
                }
                var i = startIndex;
                if ( (i < len) && IsPlus( words[ i ] ) )
                {
                    i++;
                    words_count++;
                }
                _BracketBalancer.Reset();
                var prev_is_punct = false;
                for ( ; i < len; i++  )
                {
                    w = words[ i ];
                    if ( w.IsExtraWordTypePunctuation() )
                    {
                        if ( prev_is_punct || !IsAllowedPunctuation_BetweenDigits( w ) )
                        {
                            break;
                        }
                        else
                        {
                            if ( !_BracketBalancer.Process( w.valueOriginal[ 0 ], i, out var err0 ) )
                            {
                                words_count = err0.SourceIndex - startIndex;
                            }

                            prev_is_punct = true;
                            words_count++;
                            continue;
                        }
                    }

                    if ( w.IsExtraWordTypeIntegerNumber() )
                    {
                        prev_is_punct = false;
                        words_count++;
                        continue;
                    }

                    break;
                }
                if ( _BracketBalancer.TryGetFirstError( out var err ) )
                {
                    words_count = err.SourceIndex - startIndex;
                }
                else if ( prev_is_punct )
                {
                    words_count--;
                }

                const int MIN_WORDS_COUNT = 1; //3;
                if ( MIN_WORDS_COUNT <= words_count )
                {
                    if ( ss == null ) ss = new SortedSetByRef< SearchResult_v2 >( SearchResult_v2.Comparer.Instance );

                    var sr = new SearchResult_v2( startIndex, words_count, x.phoneNumberType, preambleWordIndex );
                    if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
                    {
                        ss.Remove( in exists );
                    }
                    ss.Add( in sr );

                    index = startIndex + words_count - 1;
                }
            }

            results = ss;
            return (ss != null);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class PhoneNumbersRecognizer
    {
        private PhoneNumbersSearcher _Searcher;
        private PhoneNumbersSearcher_ByTextPreamble _SearcherByPreamble;
        private StringBuilder        _ValueUpperBuff;
        private StringBuilder        _ValueOriginalBuff;
        private IPhoneNumbersModel   _Model;
        public PhoneNumbersRecognizer( IPhoneNumbersModel model )
        {
            _Searcher           = new PhoneNumbersSearcher();
            _SearcherByPreamble = new PhoneNumbersSearcher_ByTextPreamble();
            _Model              = model;

            _ValueUpperBuff    = new StringBuilder( 100 );
            _ValueOriginalBuff = new StringBuilder( 100 );
        }

        private void Run_Searcher( List< word_t > words )
        {
            var phoneNumbers = _Searcher.FindAll( words );
            if ( phoneNumbers == null )
                return;

            foreach ( var sr in phoneNumbers )
            {
                var w1 = words[ sr.StartIndex ];
                if ( !CanProcess( w1 ) )
                {
                    continue;
                }

                #region [.core of mean.]
                if ( w1.IsExtraWordTypeIntegerNumber() )
                {
                    _ValueUpperBuff.Append( w1.valueUpper );
                }
                _ValueOriginalBuff.Append( w1.valueOriginal );

                var t = default(word_t);
                for ( int i = sr.StartIndex + 1, j = sr.Length; 1 < j; j--, i++ )
                {
                    t = words[ i ];
                    if ( t.IsExtraWordTypeIntegerNumber() )
                    {
                        _ValueUpperBuff.Append( t.valueUpper );
                    }
                    //---_ValueOriginalBuff.Append( t.valueOriginal );
                    //---t.ClearValuesAndNerChain();
                }

                var valueUpper = _ValueUpperBuff.ToString();
                var cityAreaCodeStartIndex = 0;
                if ( 3 < _ValueUpperBuff.Length )
                {
                    switch ( _ValueUpperBuff[ 0 ] )
                    {
                        case '0': cityAreaCodeStartIndex = 1; break;
                        case '4':
                            if ( _ValueUpperBuff[ 1 ] == '9' )
                            {
                                cityAreaCodeStartIndex = (_ValueUpperBuff[ 2 ] == '0') ? 3 : 2;
                            }                        
                        break;
                    }
                }
                if ( _Model.IsValid( valueUpper, cityAreaCodeStartIndex, out var cityAreaName ) )
                {
                    for ( int i = sr.StartIndex + 1, j = sr.Length; 1 < j; j--, i++ )
                    {
                        t = words[ i ];
                        _ValueOriginalBuff.Append( t.valueOriginal );
                        t.ClearValuesAndNerChain();
                    }

                    var pw = new PhoneNumberWord( w1.startIndex, (t.startIndex - w1.startIndex) + t.length, cityAreaName )
                    {
                        valueOriginal = _ValueOriginalBuff.ToString(),
                        valueUpper    = valueUpper,
                    };
                    words[ sr.StartIndex ] = pw;
                    w1.ClearValuesAndNerChain();
                }
#if DEBUG
                else
                {
                    Debug.WriteLine( $"PhoneNumber invalid: '{valueUpper}', ({string.Join( " ", words.Select( w => w.valueOriginal ) )})" );
                }
#endif
                _ValueUpperBuff   .Clear();
                _ValueOriginalBuff.Clear();
                #endregion
            }

            #region [.remove merged words.]
            words.RemoveWhereValueOriginalIsNull();
            #endregion
        }
        private void Run_SearcherByPreamble( List< word_t > words )
        {
            if ( !_SearcherByPreamble.TryFindAll( words, out var phoneNumbers ) )
                return;

            foreach ( var sr in phoneNumbers )
            {
                var w1 = words[ sr.StartIndex ];
                if ( !CanProcess( w1 ) )
                {
                    continue;
                }

                #region [.core of mean.]
                //_ValueUpperBuff .Append( w1.valueUpper    );
                _ValueOriginalBuff.Append( w1.valueOriginal );

                var t = default(word_t);
                if ( 1 < sr.Length )
                {
                    for ( int i = sr.StartIndex + 1, j = sr.Length; 1 < j; j--, i++ )
                    {
                        t = words[ i ];
                        //_ValueUpperBuff .Append( t.valueUpper    );
                        _ValueOriginalBuff.Append( t.valueOriginal );
                        t.ClearValuesAndNerChain();
                    }
                }
                else
                {
                    t = w1;
                }

                var valueOriginal = _ValueOriginalBuff.ToString();
                var pw = new PhoneNumberWord( w1.startIndex, (t.startIndex - w1.startIndex) + t.length, sr.PhoneNumberType )
                {
                    valueOriginal = valueOriginal, //_ValueOriginalBuff.ToString(),
                    valueUpper    = valueOriginal, //_ValueUpperBuff   .ToString(),
                };
                words[ sr.StartIndex ] = pw;
                w1.ClearValuesAndNerChain();
                for ( var i = sr.PreambleWordIndex; i < sr.StartIndex; i++ )
                {
                    words[ i ].ClearOutputTypeAndNerChain();
                }

                //_ValueUpperBuff.Clear();
                _ValueOriginalBuff.Clear();
                #endregion
            }

            #region [.remove merged words.]
            words.RemoveWhereValueOriginalIsNull();
            #endregion
        }
        [M(O.AggressiveInlining)] private static bool CanProcess( word_t w ) => ((w.valueUpper != null) && w.IsOutputTypeOther()); //!w.IsPhoneNumber());

        public void Run( List< word_t > words )
        {
            //-1-//
            Run_SearcherByPreamble( words );

            //-2-//
            Run_Searcher( words );
        }
    }
}
