using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Address
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
            public int Compare( in SearchResult x, in SearchResult y )
            {
                #region [comm. -sort by substring-legth-]
                /* 
                var d = y.Length - x.Length;
                if ( d != 0 )
                    return (d);
                */
                #endregion

                return (y.StartIndex - x.StartIndex);
            }
        }

        [M(O.AggressiveInlining)] public SearchResult( int startIndex, ngram_t[] ngrams
                                                     , in (string cityFullValue, int length) cityMultiWords
                                                     , in (string streetFullValue, int length) streetMultiWords )
        {
            StartIndex       = startIndex - ngrams.Length + 1 - cityMultiWords.length - streetMultiWords.length;
            Length           = ngrams.Length                  + cityMultiWords.length + streetMultiWords.length;
            Ngrams           = ngrams;
            CityMultiWords   = cityMultiWords;
            StreetMultiWords = streetMultiWords;
        }
        [M(O.AggressiveInlining)] public SearchResult( int startIndex, ngram_t[] ngrams )
        {
            StartIndex       = startIndex - ngrams.Length + 1;
            Length           = ngrams.Length;
            Ngrams           = ngrams;
            CityMultiWords   = default;
            StreetMultiWords = default;
        }
        public int       StartIndex { [M(O.AggressiveInlining)] get; }
        public int       Length     { [M(O.AggressiveInlining)] get; }
        public ngram_t[] Ngrams     { [M(O.AggressiveInlining)] get; }
        public (string cityFullValue, int length)   CityMultiWords   { [M(O.AggressiveInlining)] get; }
        public (string streetFullValue, int length) StreetMultiWords { [M(O.AggressiveInlining)] get; }
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}, {Ngrams.Length}]"; 
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal enum ngramType : byte
    {
        __UNDEFINED__,

        possibleStreetFirstWord,
        streetPostfix,
        streetEndKeyWord,
        streetDictOneWord,
        streetDictFirstWord,

        houseNumber,
        zipCodeNumber,

        city,
        cityFirstWord,

        letter,

        //comma,
        //dash,
        //vertStick,
        //semicolon,

        punctuation,
    }
    /// <summary>
    /// 
    /// </summary>
    internal struct ngram_t
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class EqualityComparer : IEqualityComparerByRef< ngram_t >, IEqualityComparer< ngram_t >
        {
            public static EqualityComparer Instance { get; } = new EqualityComparer();
            private EqualityComparer() { }

            [M(O.AggressiveInlining)] public static bool _Equals_( in ngram_t x, in ngram_t y )
            {
                if ( x.type == y.type )
                {
                    //if ( x.type == ngramType.code_49 )
                    //{
                    //    return (x.value == y.value);
                    //}
                    if ( x.length.HasValue )
                    {
                        return (x.length.Value == y.length.GetValueOrDefault( x.length.Value ));
                    }
                    return (true);
                }
                return (false);
            }
            public bool Equals( in ngram_t x, in ngram_t y )
            {
                if ( x.type == y.type )
                {
                    //if ( x.type == ngramType.code_49 )
                    //{
                    //    return (x.value == y.value);
                    //}
                    if ( x.length.HasValue )
                    {
                        return (x.length.Value == y.length.GetValueOrDefault( x.length.Value ));
                    }
                    return (true);
                }
                return (false);
            }
            public int GetHashCode( in ngram_t obj ) => obj.type.GetHashCode(); // ^ obj.length.GetValueOrDefault( -1 ).GetHashCode();

            public bool Equals( ngram_t x, ngram_t y ) => _Equals_( in x, in y );
            public int GetHashCode( ngram_t obj ) => obj.type.GetHashCode();
        }

        public const int  ZIP_CODE_LEN = 5;
        public const int  ONE_LEN      = 1;
        public const char COMMA        = ',';
        public const char VERT_STICK   = '|';
        public const char SEMICOLON    = ';';

        public static IReadOnlyCollection< char > AllowedPunctuation;
        static ngram_t()
        {
            var allowedPunctuation = xlat.GetHyphens().Concat( new[] { COMMA, VERT_STICK, SEMICOLON } ).ToList();

            AllowedPunctuation = new Set< char >( allowedPunctuation );
        }

        public static ngram_t UNDEFINED() => new ngram_t() { type = ngramType.__UNDEFINED__ };
        public static ngram_t ZipCodeNumber() => new ngram_t() { type = ngramType.zipCodeNumber, length = ZIP_CODE_LEN };
        public static ngram_t HouseNumber() => new ngram_t() { type = ngramType.houseNumber };
        public static ngram_t StreetPostfix() => new ngram_t() { type = ngramType.streetPostfix };
        public static ngram_t PossibleStreetFirstWord() => new ngram_t() { type = ngramType.possibleStreetFirstWord }; 
        public static ngram_t StreetEndKeyWord() => new ngram_t() { type = ngramType.streetEndKeyWord };
        public static ngram_t StreetDictOneWord() => new ngram_t() { type = ngramType.streetDictOneWord };
        public static ngram_t StreetDictFirstWord() => new ngram_t() { type = ngramType.streetDictFirstWord };
        public static ngram_t City() => new ngram_t() { type = ngramType.city };
        public static ngram_t CityFirstWord() => new ngram_t() { type = ngramType.cityFirstWord };
        public static ngram_t Letter() => new ngram_t() { type = ngramType.letter, length = ONE_LEN };

        public static ngram_t Punctuation() => new ngram_t() { type = ngramType.punctuation, length = ONE_LEN };
        //public static ngram_t Comma()     => new ngram_t() { type = ngramType.comma    , length = ONE_LEN };
        //public static ngram_t Dash()      => new ngram_t() { type = ngramType.dash     , length = ONE_LEN };
        //public static ngram_t VertStick() => new ngram_t() { type = ngramType.vertStick, length = ONE_LEN };
        //public static ngram_t Semicolon() => new ngram_t() { type = ngramType.semicolon, length = ONE_LEN }; 

        public ngramType type;
        public int?      length;
        //public string    value;

        public ngramType another_type;
#if DEBUG
        public override string ToString() => $"{type}{(length.HasValue ? $", {length.Value}" : null)}";
#endif
    }

    /// <summary>
    ///
    /// </summary>
    internal sealed class TreeNode
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
        private MapByRef< ngram_t, TreeNode > _TransDict; //---private Dictionary< ngram_t, TreeNode > _TransDict;
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
    internal struct Finder
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

    /// <summary>
    /// 
    /// </summary>
    internal sealed class AddressRecognizer
    {
        #region [.ctor().]
        private const char SPACE = ' ';
        private CityZipCodeSearcher       _CityZipCodeSearcher;
        private StreetHouseNumberSearcher _StreetHouseNumberSearcher;
        private StringBuilder             _ValueUpperBuff;
        private StringBuilder             _ValueOriginalBuff;
        public AddressRecognizer( IAddressModel model )
        {
            _CityZipCodeSearcher       = new CityZipCodeSearcher( model );
            _StreetHouseNumberSearcher = new StreetHouseNumberSearcher( model );

            _ValueUpperBuff    = new StringBuilder( 100 );
            _ValueOriginalBuff = new StringBuilder( 100 );
        }
        #endregion

        public void Recognize_FullAddress( List< word_t > words )
        {
            //#1 => (City-ZipCode) + (Street-HouseNumber)
            if ( _CityZipCodeSearcher.TryFindAll( words, out var cityZipCodes ) )
            {
                var wasCreateAddressWord = false;
                foreach ( var sr_1 in cityZipCodes )
                {
                    var w1 = words[ sr_1.StartIndex ];
                    if ( !CanProcess( w1 ) )
                        continue;

                    if ( !_StreetHouseNumberSearcher.TryFindFirst2Rigth( words, sr_1.StartIndex + sr_1.Length, out var sr_2 ) )
                        continue;

                    CreateAddressWord( words, w1, in sr_1, in sr_2 );

                    wasCreateAddressWord = true;
                }


                #region [.remove merged words.]
                if ( wasCreateAddressWord )
                {
                    words.RemoveWhereValueOriginalIsNull();
                }
                #endregion

                //#2 => (Street-HouseNumber) + (City-ZipCode)
                if ( _StreetHouseNumberSearcher.TryFindAll( words, out var streetHouseNumbers ) )
                {
                    wasCreateAddressWord = false;
                    foreach ( var sr_1 in streetHouseNumbers )
                    {
                        var w1 = words[ sr_1.StartIndex ];
                        if ( !CanProcess ( w1 ) )
                            continue;

                        if ( !_CityZipCodeSearcher.TryFindFirst2Rigth( words, sr_1.StartIndex + sr_1.Length, out var sr_2 ) )
                            continue;

                        CreateAddressWord( words, w1, in sr_1, in sr_2 );

                        wasCreateAddressWord = true;
                    }

                    #region [.remove merged words.]
                    if ( wasCreateAddressWord )
                    {
                        words.RemoveWhereValueOriginalIsNull();
                    }
                    #endregion
                }
            }
        }

        public void Recognize_CityZipCodeStreetOnly( List< word_t > words )
        {
            //#1 => (City-ZipCode) + (Street)
            if ( _CityZipCodeSearcher.TryFindAll( words, out var cityZipCodes ) )
            {
                var wasCreateAddressWord = false;
                foreach ( var sr_1 in cityZipCodes )
                {
                    var w1 = words[ sr_1.StartIndex ];
                    if ( !CanProcess( w1 ) )
                        continue;

                    if ( !_StreetHouseNumberSearcher.TryFindFirst2RigthStreetOnly( words, sr_1.StartIndex + sr_1.Length, out var sr_2 ) )
                        continue;

                    CreateAddressWord( words, w1, in sr_1, in sr_2 );

                    wasCreateAddressWord = true;
                }

                #region [.remove merged words.]
                if ( wasCreateAddressWord )
                {
                    words.RemoveWhereValueOriginalIsNull();
                }
                #endregion

                //#2 => (Street) + (City-ZipCode)
                if ( _StreetHouseNumberSearcher.TryFindStreetOnly( words, out var streetOnlys ) )
                {
                    wasCreateAddressWord = false;
                    foreach ( var sr_1 in streetOnlys )
                    {
                        var w1 = words[ sr_1.StartIndex ];
                        if ( !CanProcess ( w1 ) )
                            continue;

                        if ( !_CityZipCodeSearcher.TryFindFirst2Rigth( words, sr_1.StartIndex + sr_1.Length, out var sr_2 ) )
                            continue;

                        CreateAddressWord( words, w1, in sr_1, in sr_2 );

                        wasCreateAddressWord = true;
                    }

                    #region [.remove merged words.]
                    if ( wasCreateAddressWord )
                    {
                        words.RemoveWhereValueOriginalIsNull();
                    }
                    #endregion
                }
            }
        }
        public void Recognize_CityOnlyStreetHouseNumber( List< word_t > words )
        {
            //#1 => (City) + (Street-HouseNumber)
            if ( _CityZipCodeSearcher.TryFindCityOnly( words, out var cityOnlys ) )
            {
                var wasCreateAddressWord = false;
                foreach ( var sr_1 in cityOnlys )
                {
                    var w1 = words[ sr_1.StartIndex ];
                    if ( !CanProcess( w1 ) )
                        continue;

                    if ( !_StreetHouseNumberSearcher.TryFindFirst2Rigth( words, sr_1.StartIndex + sr_1.Length, out var sr_2 ) )
                        continue;

                    CreateAddressWord( words, w1, in sr_1, in sr_2 );

                    wasCreateAddressWord = true;
                }

                #region [.remove merged words.]
                if ( wasCreateAddressWord )
                {
                    words.RemoveWhereValueOriginalIsNull();
                }
                #endregion

                //#2 => (Street-HouseNumber) + (City)
                if ( _StreetHouseNumberSearcher.TryFindAll( words, out var streetHouseNumbers ) )
                {
                    wasCreateAddressWord = false;
                    foreach ( var sr_1 in streetHouseNumbers )
                    {
                        var w1 = words[ sr_1.StartIndex ];
                        if ( !CanProcess ( w1 ) )
                            continue;

                        if ( !_CityZipCodeSearcher.TryFindFirst2RigthCityOnly( words, sr_1.StartIndex + sr_1.Length, out var sr_2 ) )
                            continue;

                        CreateAddressWord( words, w1, in sr_1, in sr_2 );

                        wasCreateAddressWord = true;
                    }

                    #region [.remove merged words.]
                    if ( wasCreateAddressWord )
                    {
                        words.RemoveWhereValueOriginalIsNull();
                    }
                    #endregion
                }
            }
        }

        public void Recognize_CityZipCode( List< word_t > words )
        {
            //#1 => (City-ZipCode) 
            if ( _CityZipCodeSearcher.TryFindAll( words, out var cityZipCodes ) )
            {
                var wasCreateAddressWord = false;
                foreach ( var sr in cityZipCodes )
                {
                    var w1 = words[ sr.StartIndex ];
                    if ( CanProcess( w1 ) )
                    {
                        CreateAddressWord( words, w1, in sr );

                        wasCreateAddressWord = true;
                    }
                }

                #region [.remove merged words.]
                if ( wasCreateAddressWord )
                {
                    words.RemoveWhereValueOriginalIsNull();
                }
                #endregion
            }
        }
        public void Recognize_StreetHouseNumber( List< word_t > words )
        {
            //#1 => (Street-HouseNumber)
            if ( _StreetHouseNumberSearcher.TryFindAll( words, out var streetHouseNumbers ) )
            {
                var wasCreateAddressWord = false;
                foreach ( var sr in streetHouseNumbers )
                {
                    var w1 = words[ sr.StartIndex ];
                    if ( CanProcess( w1 ) )
                    {
                        CreateAddressWord( words, w1, in sr );

                        wasCreateAddressWord = true;
                    }
                }

                #region [.remove merged words.]
                if ( wasCreateAddressWord )
                {
                    words.RemoveWhereValueOriginalIsNull();
                }
                #endregion
            }
        }

        public void Recognize_CityOnly( List< word_t > words )
        {
            //#1 => (City) 
            if ( _CityZipCodeSearcher.TryFindCityOnly( words, out var cityOnlys ) )
            {
                var wasCreateAddressWord = false;
                foreach ( var sr in cityOnlys )
                {
                    var w1 = words[ sr.StartIndex ];
                    if ( CanProcess( w1 ) )
                    {
                        CreateAddressWord( words, w1, in sr );

                        wasCreateAddressWord = true;
                    }
                }

                #region [.remove merged words.]
                if ( wasCreateAddressWord )
                {
                    words.RemoveWhereValueOriginalIsNull();
                }
                #endregion
            }
        }
        public void Recognize_StreetOnly( List< word_t > words )
        {
            //#1 => (Street)
            if ( _StreetHouseNumberSearcher.TryFindStreetOnly( words, out var streetOnlys ) )
            {
                var wasCreateAddressWord = false;
                foreach ( var sr in streetOnlys )
                {
                    var w1 = words[ sr.StartIndex ];
                    if ( CanProcess( w1 ) )
                    {
                        CreateAddressWord( words, w1, in sr );

                        wasCreateAddressWord = true;
                    }
                }

                #region [.remove merged words.]
                if ( wasCreateAddressWord )
                {
                    words.RemoveWhereValueOriginalIsNull();
                }
                #endregion
            }
        }

        [M(O.AggressiveInlining)] private static bool CanProcess( word_t w ) => ((w.valueUpper != null) && w.IsOutputTypeOther()); //!w.IsAddress());
        [M(O.AggressiveInlining)] private void CreateAddressWord( List< word_t > words,  word_t w1, in SearchResult sr_1, in SearchResult sr_2 )
        {
            var aw = new AddressWord( w1.startIndex );

            AddressWord_SetFields( aw, words, in sr_1 );
            AddressWord_SetFields( aw, words, in sr_2 );

            var t = default(word_t);
            for ( int i = sr_1.StartIndex, j = sr_1.Length; 0 < j; j--, i++ )
            {
                t = words[ i ];
                _ValueUpperBuff   .Append( t.valueUpper    ).Append( SPACE );
                _ValueOriginalBuff.Append( t.valueOriginal ).Append( SPACE );
                t.ClearValuesAndNerChain();
            }
            for ( int i = sr_2.StartIndex, j = sr_2.Length; 0 < j; j--, i++ )
            {
                t = words[ i ];
                _ValueUpperBuff   .Append( t.valueUpper    ).Append( SPACE );
                _ValueOriginalBuff.Append( t.valueOriginal ).Append( SPACE );
                t.ClearValuesAndNerChain();
            }

            aw.valueOriginal = _ValueOriginalBuff.ToString( 0, _ValueOriginalBuff.Length - 1 );
            aw.valueUpper    = _ValueUpperBuff   .ToString( 0, _ValueUpperBuff   .Length - 1 );
            aw.length        = (t.startIndex - w1.startIndex) + t.length;
            words[ sr_1.StartIndex ] = aw;

            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();
        }
        [M(O.AggressiveInlining)] private void CreateAddressWord( List< word_t > words,  word_t w1, in SearchResult sr )
        {
            var aw = new AddressWord( w1.startIndex );

            AddressWord_SetFields( aw, words, in sr );

            var t = default(word_t);
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                t = words[ i ];
                _ValueUpperBuff   .Append( t.valueUpper    ).Append( SPACE );
                _ValueOriginalBuff.Append( t.valueOriginal ).Append( SPACE );
                t.ClearValuesAndNerChain();
            }

            aw.valueOriginal = _ValueOriginalBuff.ToString( 0, _ValueOriginalBuff.Length - 1 );
            aw.valueUpper    = _ValueUpperBuff   .ToString( 0, _ValueUpperBuff   .Length - 1 );
            aw.length        = (t.startIndex - w1.startIndex) + t.length;
            words[ sr.StartIndex ] = aw;

            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();
        }
        private static void AddressWord_SetFields( AddressWord aw, List< word_t > words, in SearchResult sr )
        {
            var ngramIdx   = 0;
            var ngramCount = sr.Ngrams.Length;
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                var value = words[ i ].valueOriginal;
                switch ( sr.Ngrams[ ngramIdx++ ].type )
                {
                    case ngramType.city:
                        aw.City = value;
                    break;
                    case ngramType.cityFirstWord:
                        aw.City = sr.CityMultiWords.cityFullValue;
                        i += sr.CityMultiWords.length;
                    break;

                    case ngramType.possibleStreetFirstWord:
                        aw.Street = value;
                    break;
                    case ngramType.streetEndKeyWord:
                        aw.Street += ' ' + value;
                    break;

                    case ngramType.streetDictOneWord:
                    case ngramType.streetPostfix:
                        aw.Street = value;
                    break;

                    case ngramType.streetDictFirstWord:
                        aw.Street = sr.StreetMultiWords.streetFullValue;
                        i += sr.StreetMultiWords.length;
                    break;

                    case ngramType.houseNumber:
                        aw.HouseNumber = value;
                    break;
                    case ngramType.letter:
                        aw.HouseNumber += value;
                    break;

                    case ngramType.zipCodeNumber:
                        aw.ZipCodeNumber = value;
                    break;
                }

                if ( ngramCount <= ngramIdx )
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class AddressRecognizerExtensions
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
        [M(O.AggressiveInlining)] public static void AddEx_City( this SortedSetByRef< SearchResult > ss, int startIndex, ngram_t[] ngrams
                                                               , in (string cityFullValue, int length) cityMultiWords )
        {
            var sr = new SearchResult( startIndex, ngrams, in cityMultiWords, default );
            if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
            {
                ss.Remove( in exists );
            }
            ss.Add( in sr );
        }
        [M(O.AggressiveInlining)] public static void AddEx_Street( this SortedSetByRef< SearchResult > ss, int startIndex, ngram_t[] ngrams
                                                                 , in (string streetFullValue, int length) streetMultiWords )
        {
            var sr = new SearchResult( startIndex, ngrams, default, in streetMultiWords );
            if ( ss.TryGetValue( in sr, out var exists ) && (exists.Length < sr.Length) )
            {
                ss.Remove( in exists );
            }
            ss.Add( in sr );
        }
    }
}
