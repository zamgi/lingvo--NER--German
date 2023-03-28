using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;
using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Birthdays
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

        public SearchResult( int startIndex, int length ) : this()
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
    internal enum ngramType : byte
    {
        __UNDEFINED__,

        text,
        validNumber,
    }
    /// <summary>
    /// 
    /// </summary>
    internal struct ngram_t
    { 
        /// <summary>
        /// 
        /// </summary>
        public sealed class EqualityComparer : IEqualityComparer< ngram_t >
        {
            public static EqualityComparer Instance { get; } = new EqualityComparer();
            private EqualityComparer() { }

            public bool Equals( ngram_t x, ngram_t y ) => Equals( in x, in y );
            [M(O.AggressiveInlining)] public bool Equals( in ngram_t x, in ngram_t y )
            {
                switch ( x.type )
                {
                    case ngramType.text: return ((y.type == ngramType.text) ? (string.Compare( x.value, y.value, true ) == 0) : false);
                    default:             return (x.type == y.type);
                }
            }
            public int GetHashCode( ngram_t obj )
            {
                switch ( obj.type )
                {
                    case ngramType.text: return (obj.type.GetHashCode() ^ obj.value.GetHashCode());
                    default:             return (obj.type.GetHashCode());
                }
            }
        }

        public static ngram_t Number() => new ngram_t() { type = ngramType.validNumber };
        public static ngram_t Text( string _value ) => new ngram_t() { type = ngramType.text, value = _value };
        public static ngram_t UNDEFINED() => new ngram_t() { type = ngramType.__UNDEFINED__ };

        public string    value;
        public ngramType type;
#if DEBUG
        public override string ToString()
        {
            switch ( type )
            {
                case ngramType.text: return ($"'{value}'");
                default:             return ("NUM");
            }
        }
#endif
    }

    /// <summary>
    ///
    /// </summary>
    internal sealed class DatesSearcher
    {
        /// <summary>
        ///
        /// </summary>
        private sealed class TreeNode
        {
            /// <summary>
            /// 
            /// </summary>
            private sealed class ngrams_IEqualityComparer : IEqualityComparer< ngram_t[] >
            {
                public static ngrams_IEqualityComparer Instance { get; } = new ngrams_IEqualityComparer();

                private ngram_t.EqualityComparer _Comparer;
                private ngrams_IEqualityComparer() => _Comparer = ngram_t.EqualityComparer.Instance;
                
                public bool Equals( ngram_t[] x, ngram_t[] y )
                {
                    var len = x.Length;
                    if ( len != y.Length )
                    {
                        return (false);
                    }

                    for ( int i = 0; i < len; i++ )
                    {
                        if ( !_Comparer.Equals( in x[ i ], in y[ i ] ) )
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
                var root = new TreeNode( null, ngram_t.UNDEFINED() );
                foreach ( var ngram in ngrams )
                {
                    var node = root;
                    foreach ( var ng in ngram )
                    {
                        var nodeNew = node.GetTransition( in ng );
                        if ( nodeNew == null )
                        {
                            nodeNew = new TreeNode( node, in ng );
                            node.AddTransition( nodeNew );
                        }
                        node = nodeNew;
                    }
                    node.AddNgrams( ngram );
                }

                var nodes = new List< TreeNode >();
                var transitions_root_nodes = root.Transitions;
                if ( transitions_root_nodes != null )
                {
                    nodes.Capacity = transitions_root_nodes.Count;

                    foreach ( var node in transitions_root_nodes )
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

                while ( nodes.Count != 0 )
                {
                    var newNodes = new List< TreeNode >( nodes.Count );
                    foreach ( var node in nodes )
                    {
                        var failNode = node.Parent.Failure;
                        ref var ng = ref node.Ngram;

                        while ( (failNode != null) && !failNode.ContainsTransition( in ng ) )
                        {
                            failNode = failNode.Failure;
                        }

                        if ( failNode == null )
                        {
                            node.Failure = root;
                        }
                        else
                        {
                            node.Failure = failNode.GetTransition( in ng );
                            var failure_ngrams = node.Failure?.Ngrams;
                            if ( failure_ngrams != null )
                            {
                                foreach ( var ngs in failure_ngrams )
                                {
                                    node.AddNgrams( ngs );
                                }
                            }
                        }

                        var transitions_nodes = node.Transitions;
                        if ( transitions_nodes != null )
                        {
                            foreach ( var transNode in transitions_nodes )
                            {
                                newNodes.Add( transNode );
                            }
                        }
                    }
                    nodes = newNodes;
                }
                root.Failure = root;

                return (root);
            }

            #region [.props.]
            private Dictionary< ngram_t, TreeNode > _TransDict;
            private Set< ngram_t[] > _Ngrams;

            private ngram_t _Ngram;
            public ref ngram_t Ngram { [M(O.AggressiveInlining)] get => ref _Ngram; }
            public TreeNode Parent  { [M(O.AggressiveInlining)] get; private set; }
            public TreeNode Failure { [M(O.AggressiveInlining)] get; internal set; }

            public bool HasNgrams { [M(O.AggressiveInlining)] get => (_Ngrams != null); }
            public ICollection< ngram_t[] > Ngrams { [M(O.AggressiveInlining)] get => _Ngrams; }
            public ICollection< TreeNode > Transitions { [M(O.AggressiveInlining)] get => _TransDict?.Values; }
            #endregion

            #region [.ctor() & methods.]
            public TreeNode( TreeNode parent, in ngram_t word )
            {
                _Ngram = word;
                Parent = parent;                
            }

            public void AddNgrams( ngram_t[] ngrams )
            {
                if ( _Ngrams == null ) _Ngrams = new Set< ngram_t[] >( ngrams_IEqualityComparer.Instance );
                _Ngrams.Add( ngrams );
            }
            public void AddTransition( TreeNode node )
            {
                if ( _TransDict == null ) _TransDict = new Dictionary< ngram_t, TreeNode >( ngram_t.EqualityComparer.Instance );
                _TransDict.Add( node._Ngram, node );
            }
            public bool ContainsTransition( in ngram_t ngram ) => ((_TransDict != null) && _TransDict.ContainsKey( ngram ));

            public TreeNode GetTransition( in ngram_t ngram ) => ((_TransDict != null) && _TransDict.TryGetValue( ngram, out var node ) ? node : null);
#if DEBUG
            public override string ToString() => $"{((_Ngram.type == ngramType.__UNDEFINED__) ? "ROOT" : _Ngram.ToString())}, transitions(descendants): {(_TransDict?.Count).GetValueOrDefault()}, ngrams: {(_Ngrams?.Count).GetValueOrDefault()}";
#endif
            #endregion
        }

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
        public DatesSearcher( params DateTimeFormatInfo[] dateTimeFormats ) => _Root = TreeNode.BuildTree( DatesSearcherExtensions.GetNgrams( dateTimeFormats ) );
        #endregion

        #region [.public method's.]
        public bool TryFindAll( List< word_t > words, out IReadOnlyCollection< SearchResult > results )
        {
            var ss = default( SortedSetByRef< SearchResult >);
            var node   = _Root;
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

                    foreach ( var ngrams in node.Ngrams )
                    {
                        ss.AddEx( index, ngrams );
                    }
                }
            }

            results = ss;
            return (ss != null);
        }
        public bool TryFindFirst( List< word_t > words, int startIndex, out SearchResult result )
        {
            result = default;
            var node   = _Root;
            var finder = Finder.Create( _Root );

            var ng = new ngram_t();
            for ( int index = startIndex, len = words.Count; index < len; index++ )
            {
                if ( !Classify( words[ index ], ref ng ) && (node == _Root) )
                {
                    break; 
                }

                node = finder.Find( in ng );
                if ( node.HasNgrams )
                {
                    foreach ( var ngrams in node.Ngrams )
                    {
                        var sr = new SearchResult( index - ngrams.Length + 1, ngrams.Length );
                        if ( sr.StartIndex != startIndex )
                        {
                            goto EXIT;
                        }
                        if ( result.Length < sr.Length ) result = sr;
                    }

                    if ( result.StartIndex != startIndex )
                    {
                        return (false);
                    }
                }
            }
        EXIT:
            return ((result.StartIndex == startIndex) && (result.Length != 0));

        }
        #endregion

        #region [.text classifier.]
        [M(O.AggressiveInlining)] private static bool Classify( word_t w, ref ngram_t ng )
        {
            switch ( w.nerInputType )
            {
                case NerInputType.Num:
                if ( w.IsOutputTypeOther() && w.IsExtraWordTypeIntegerNumber() )
                {
                    ng.type = ngramType.validNumber;
                    return (true);
                }
                break;

                default: //case NerInputType.Other:
                    ng.type  = ngramType.text;
                    ng.value = w.valueUpper;
                    return (true);
            }
            return (false);

            #region comm. prev.
            /*
            switch ( w.OutputType )
            {
                case OutputType.ValidNumber:
                if ( w.IsOnlyIntegerPositive() )
                {
                    ng.type = ngramType.validNumber;
                    return (true);
                }
                break;

                case OutputType.Other:
                if ( !w.IsInputTypeNum() )
                {
                    ng.type  = ngramType.text;
                    ng.value = w.valueUpper;
                    return (true);
                }
                break;
            }
            return (false);
            //*/
            #endregion
        }
        #endregion
#if DEBUG
        public override string ToString() => $"[{_Root}]"; 
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class DatesSearcherExtensions
    {
        [M(O.AggressiveInlining)] public static void AddEx( this SortedSetByRef< SearchResult > ss, int index, ngram_t[] ngrams )
        {
            var sr = new SearchResult( index - ngrams.Length + 1, ngrams.Length );
            if ( ss.TryGetValue( sr, out var exists ) && (exists.Length < sr.Length) )
            {
                ss.Remove( exists );
            }
            ss.Add( sr );
        }

        [M(O.AggressiveInlining)] private static IEnumerable< ngram_t[] > getPatterns( string month, string year )
        {
            //'22 апр. 2018 г.'
            yield return (new[] { ngram_t.Number(), 
                                  ngram_t.Text( month ),
                                  ngram_t.Number(),
                                  ngram_t.Text( year ) });
            //'22 апр. 2018'
            yield return (new[] { ngram_t.Number(),
                                  ngram_t.Text( month ),
                                  ngram_t.Number(), });
            //'2018 г. 22 апр.'
            yield return (new[] { ngram_t.Number(),
                                  ngram_t.Text( year ),
                                  ngram_t.Number(),
                                  ngram_t.Text( month ), });
            //'апр. 22 г. 2018'
            yield return (new[] { ngram_t.Text( month ),
                                  ngram_t.Number(),
                                  ngram_t.Text( year ),
                                  ngram_t.Number(), });
            //'г. 2018 апр. 22'
            yield return (new[] { ngram_t.Text(  year ),
                                  ngram_t.Number(),
                                  ngram_t.Text( month ),
                                  ngram_t.Number(), });
        }
        [M(O.AggressiveInlining)] private static IEnumerable< ngram_t[] > getPatterns( string month )
        {
            const string COMMA = ",";
            const string DOT   = ".";

            //'22 апр. 2018', '2018 апр. 22'
            yield return (new[] { ngram_t.Number(),
                                  ngram_t.Text( month ),
                                  ngram_t.Number(), });
            //25. Januar 1979
            yield return (new[] { ngram_t.Number(),
                                  ngram_t.Text( DOT ),
                                  ngram_t.Text( month ),
                                  ngram_t.Number(), });
            //'22 апр., 2018'
            yield return (new[] { ngram_t.Number(),
                                  ngram_t.Text( month ),
                                  ngram_t.Text( COMMA ),
                                  ngram_t.Number(), });
            //'апр. 22, 2018'
            yield return (new[] { ngram_t.Text( month ),
                                  ngram_t.Number(),
                                  ngram_t.Text( COMMA ),
                                  ngram_t.Number(), });
            //'2018, апр. 22'
            yield return (new[] { ngram_t.Number(),
                                  ngram_t.Text( COMMA ),
                                  ngram_t.Text( month ),
                                  ngram_t.Number(), });
        }
        [M(O.AggressiveInlining)] private static IEnumerable< ngram_t[] > getPatterns4OnlyNumbers()
        {
            const string SLASH = "/";
            const string DOT   = ".";

            //'11.11.1971'
            yield return (new[] { ngram_t.Number(),
                                  ngram_t.Text( DOT ),
                                  ngram_t.Number(),
                                  ngram_t.Text( DOT ),
                                  ngram_t.Number(), });
            //'11/11/1971'
            yield return (new[] { ngram_t.Number(),
                                  ngram_t.Text( SLASH ),
                                  ngram_t.Number(),
                                  ngram_t.Text( SLASH ),
                                  ngram_t.Number(), });
        }

        [M(O.AggressiveInlining)] private static void AddPatterns4OnlyNumbers( this SimplyLinkedList< ngram_t[] > lst ) 
        {
            foreach ( var p in getPatterns4OnlyNumbers() )
            {
                lst.Add( p );
            }
        }
        [M(O.AggressiveInlining)] private static void AddPatterns( this SimplyLinkedList< ngram_t[] > lst, string month, string year )
        {
            foreach ( var p in getPatterns( month, year ) )
            {
                lst.Add( p );
            }
        }
        [M(O.AggressiveInlining)] private static void AddPatterns( this SimplyLinkedList< ngram_t[] > lst, IEnumerable< string > months )
        {
            foreach ( var month in months )
            {
                var m = TrimStartDigitAndToUpper( month );
                if ( !m.IsNullOrEmpty() )
                {
                    foreach ( var p in getPatterns( m ) )
                    {
                        lst.Add( p );
                    }
                }
            }
        }
        [M(O.AggressiveInlining)] unsafe private static string TrimStartDigitAndToUpper( string s )
        {
            fixed ( char* _base = s )
            {
                if ( (*_base).IsDigit() )
                {
                    for ( var ptr = _base + 1; ; ptr++ )
                    {
                        var ch = *ptr;
                        if ( ch == '\0' )
                        {
                            return (string.Empty);
                        }
                        if ( !ch.IsDigit() )
                        {
                            var new_s = new string( ptr );
                            StringsHelper.ToUpperInvariantInPlace( new_s );
                            return (new_s);
                        }
                    }
                }
                else
                {
                    return (s.ToUpperInvariant());
                }
            }            
        }
        [M(O.AggressiveInlining)] private static void AddPatterns( this SimplyLinkedList< ngram_t[] > lst, DateTimeFormatInfo dtfi )
        {
            lst.AddPatterns( dtfi.MonthGenitiveNames );
            lst.AddPatterns( dtfi.MonthNames );
            lst.AddPatterns( dtfi.AbbreviatedMonthGenitiveNames );
            lst.AddPatterns( dtfi.AbbreviatedMonthNames );
        }
        [M(O.AggressiveInlining)] private static void AddPatterns_4_de( this SimplyLinkedList< ngram_t[] > lst )
        {
            var years  = new[] { "J", "JAHR", "JAHRES" };
            var months = new[] { "JANUAR"   , "JAN",
                                 "FEBRUAR"  , "FEB",
                                 "MARZ"     , "MÄRZ", "MRZ",
                                 "APRIL"    , "APR",
                                 "MAI"      , //"MAI",
                                 "JUNI"     , "JUN",
                                 "JULI"     , "JUL",
                                 "AUGUST"   , "AUG",
                                 "SEPTEMBER", "SEP",
                                 "OKTOBER"  , "OKT",
                                 "NOVEMBER" , "NOV",
                                 "DEZEMBER" , "DEZ", };
            #region comm.
            //var months = new[] { "ЯНВ" , "ЯНВАРЬ" , "ЯНВАРЯ",
            //                     "ФЕВ" , "ФЕВРАЛЬ", "ФЕВРАЛЯ",
            //                     "МАРТ", "МАРТА"  ,
            //                     "АПР" , "АПРЕЛЬ" , "АПРЕЛЯ",
            //                     "МАЙ" , "МАЯ"    ,
            //                     "ИЮНЬ", "ИЮНЯ"   ,
            //                     "ИЮЛЬ", "ИЮЛЯ"   ,
            //                     "АВГ" , "АВГУСТ" , "АВГУСТА",
            //                     "СЕН" , "СЕНТ"   , "СЕНТЯБРЬ", "СЕНТЯБРЯ",
            //                     "ОКТ" , "ОКТЯБРЬ", "ОКТЯБРЯ",
            //                     "НОЯБ", "НОЯБРЬ" , "НОЯБРЯ",
            //                     "ДЕК" , "ДЕКАБРЬ", "ДЕКАБРЯ" }; 
            #endregion

            foreach ( var month in months )
            {
                foreach ( var year in years )
                {
                    lst.AddPatterns( month      , year );
                    lst.AddPatterns( month + '.', year );
                    lst.AddPatterns( month      , year + '.' );
                    lst.AddPatterns( month + '.', year + '.' );
                }
            }
        }

        public static IEnumerable< ngram_t[] > GetNgrams( IEnumerable< DateTimeFormatInfo > dateTimeFormats )
        {
            var ngrams = new SimplyLinkedList< ngram_t[] >();

            foreach ( var dateTimeFormat in dateTimeFormats )
            {
                ngrams.AddPatterns( dateTimeFormat );
            }
            ngrams.AddPatterns_4_de();
            ngrams.AddPatterns4OnlyNumbers();

            return (ngrams);
        }
    }
}
