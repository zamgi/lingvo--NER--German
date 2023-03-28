using System.Collections.Generic;
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
    unsafe internal sealed class StreetHouseNumberSearcher
    {
        #region [.model.]
        /// <summary>
        /// 
        /// </summary>
        private sealed class Model4FindAll
        {
            #region [.ctor().]
            public static Model4FindAll Instance { get; } = new Model4FindAll( GetNgrams() );
            private Model4FindAll( IEnumerable< ngram_t[] > ngrams ) => Root  = TreeNode.BuildTree( ngrams );
            public TreeNode Root { get; }
            #endregion

            //---public static ngram_t[] get_separ_ngrams() => new[] { ngram_t.Dash(), ngram_t.VertStick(), ngram_t.Comma(), ngram_t.Semicolon() };
            public static IEnumerable< ngram_t[] > GetNgrams()
            {
                //'Straße  (улица)  Haus  (дом)'
                //'Straße  (улица),  Haus  (дом)'

                var street_ngrams = new[] { ngram_t.StreetPostfix(), ngram_t.StreetDictOneWord(), ngram_t.StreetDictFirstWord() };
                var punctuation_ngram = ngram_t.Punctuation();
                //---var separ_ngrams = get_separ_ngrams();

                foreach ( var street_ngram in street_ngrams )
                {
                    //'(Улица) (Дом)'
                    yield return (new[] { street_ngram, ngram_t.HouseNumber() });
                    //'(Улица) (Дом)+letter'
                    yield return (new[] { street_ngram, ngram_t.HouseNumber(), ngram_t.Letter() });

                    //---// yield return (new[] { street_ngram, ngram_t.HouseNumber(), punctuation_ngram, ngram_t.HouseNumber() });

                    //---foreach ( var separ_ngram in separ_ngrams )
                    //---{
                    //'(Улица), (Дом)'
                    yield return (new[] { street_ngram, punctuation_ngram /*separ_ngram*/, ngram_t.HouseNumber() });
                    //'(Улица), (Дом)+letter'
                    yield return (new[] { street_ngram, punctuation_ngram /*separ_ngram*/, ngram_t.HouseNumber(), ngram_t.Letter() });
                    //---}
                }

                //'(Улица) (Дом)'
                yield return (new[] { ngram_t.PossibleStreetFirstWord(), 
                                      ngram_t.StreetEndKeyWord()       , ngram_t.HouseNumber() });
                //'(Улица) (Дом)+letter'
                yield return (new[] { ngram_t.PossibleStreetFirstWord(),
                                      ngram_t.StreetEndKeyWord()       , ngram_t.HouseNumber(), ngram_t.Letter() });

                //---foreach ( var separ_ngram in separ_ngrams )
                //---{
                //'(Улица), (Дом)'
                yield return (new[] { ngram_t.PossibleStreetFirstWord(),
                                      ngram_t.StreetEndKeyWord()       , punctuation_ngram /*separ_ngram*/, ngram_t.HouseNumber() });
                //'(Улица), (Дом)+letter'
                yield return (new[] { ngram_t.PossibleStreetFirstWord(),
                                      ngram_t.StreetEndKeyWord()       , punctuation_ngram /*separ_ngram*/, ngram_t.HouseNumber(), ngram_t.Letter() });
                //---}
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class Model4Find2Rigth
        {
            #region [.ctor().]
            public static Model4Find2Rigth Instance { get; } = new Model4Find2Rigth( GetNgrams() );
            private Model4Find2Rigth( IEnumerable< ngram_t[] > ngrams ) => Root  = TreeNode.BuildTree( ngrams );
            public TreeNode Root { get; }
            #endregion

            private static IEnumerable< ngram_t[] > GetNgrams()
            {
                //', Straße  (улица)  Haus  (дом)'
                //', Straße  (улица),  Haus  (дом)'

                var punctuation_ngrams = new[] { ngram_t.Punctuation() };
                //---var separ_ngrams = Model4FindAll.get_separ_ngrams();
                
                foreach ( var ngrams in Model4FindAll.GetNgrams() )
                {
                    //'(Улица), (Дом)'
                    yield return (ngrams);

                    //---foreach ( var separ_ng in separ_ngrams )
                    //---{
                    //', (Улица) (Дом)'
                    var base_ngrams = punctuation_ngrams.Concat( ngrams ).ToArray( ngrams.Length + 1 ); //---Enumerable.Repeat( separ_ng, 1 ).Concat( ngrams ).ToArray( ngrams.Length + 1 );
                    yield return (base_ngrams);
                    //---}
                }
            }
        }
        #endregion

        #region [.ctor().]
        private TreeNode      _Root4FindAll;
        private TreeNode      _Root4Find2Rigth;
        private IAddressModel _Model;
        private CharType*     _CTM;

        public StreetHouseNumberSearcher( IAddressModel model )
        {
            _Model           = model;
            _Root4FindAll    = Model4FindAll   .Instance.Root;
            _Root4Find2Rigth = Model4Find2Rigth.Instance.Root;
            _CTM             = xlat_Unsafe.Inst._CHARTYPE_MAP;
        }
        #endregion

        #region [.public method's.]
        [M(O.AggressiveInlining)] public bool TryFindAll( List< word_t > words, out IReadOnlyCollection< SearchResult > results )
        {
            var ss = default(SortedSetByRef< SearchResult >);
            var node = _Root4FindAll;
            var finder = Finder.Create( _Root4FindAll );

            var ng = new ngram_t();
            var streetMultiWords = default((string streetFullValue, int length));
            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                var success = Classify( words[ index ], ref ng );
                if ( !success && (node == _Root4FindAll) )
                {
                    streetMultiWords = default;
                    continue;
                }

                switch ( ng.type )
                {
                    case ngramType.streetDictFirstWord:
                        if ( _Model.IsStreetMultiWord( words, index, out streetMultiWords ) )
                        {
                            streetMultiWords.length--;
                            index += streetMultiWords.length;
                        }
                        else
                        {
                            switch ( ng.another_type )
                            {
                                case ngramType.streetDictOneWord:
                                case ngramType.streetPostfix    :
                                case ngramType.streetEndKeyWord : 
                                case ngramType.possibleStreetFirstWord:  
                                         ng.type = ng.another_type;         break;
                                default: ng.type = ngramType.__UNDEFINED__; break;
                            }
                        }
                    break;
                }

                node = finder.Find( in ng );

                if ( node.HasNgrams )
                {
                    if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );

                    switch ( node.Ngrams.Count )
                    {
                        case 1:
                            ss.AddEx_Street( index, node.Ngrams.First, in streetMultiWords );
                        break;

                        default:
                            foreach ( var ngrams in node.Ngrams )
                            {
                                ss.AddEx_Street( index, ngrams, in streetMultiWords );
                            }
                        break;
                    }
                }
            }
            results = ss;
            return (ss != null);
        }
        [M(O.AggressiveInlining)] public bool TryFindFirst2Rigth( List< word_t > words, int startIndex, out SearchResult result )
        {
            result = default;
            var node = _Root4Find2Rigth;
            var finder = Finder.Create( _Root4Find2Rigth );

            var ng = new ngram_t();
            var streetMultiWords = default((string streetFullValue, int length));
            for ( int index = startIndex, len = words.Count; index < len; index++ )
            {
                var success = Classify( words[ index ], ref ng );
                if ( !success && (node == _Root4Find2Rigth) )
                {
                    break;
                }

                switch ( ng.type )
                {
                    case ngramType.streetDictFirstWord:
                        if ( _Model.IsStreetMultiWord( words, index, out streetMultiWords ) )
                        {
                            streetMultiWords.length--;
                            index += streetMultiWords.length;
                        }
                        else
                        {
                            switch ( ng.another_type )
                            {
                                case ngramType.streetDictOneWord:
                                case ngramType.streetPostfix    :
                                case ngramType.streetEndKeyWord : 
                                case ngramType.possibleStreetFirstWord: 
                                         ng.type = ng.another_type;         break;
                                default: ng.type = ngramType.__UNDEFINED__; break;
                            }
                        }
                    break;
                }

                node = finder.Find( in ng );

                if ( node.HasNgrams )
                {
                    switch ( node.Ngrams.Count )
                    {
                        case 1:
                        {
                            var sr = new SearchResult( index, node.Ngrams.First, default, in streetMultiWords );
                            if ( result.Length < sr.Length ) result = sr;
                        }
                        break;

                        default:
                            foreach ( var ngrams in node.Ngrams )
                            {
                                var sr = new SearchResult( index, ngrams, default, in streetMultiWords );
                                if ( result.Length < sr.Length ) result = sr;
                            }
                        break;
                    }
                    if ( result.StartIndex != startIndex )
                    {
                        return (false);
                    }
                }
                else if ( node == _Root4Find2Rigth )
                {
                    break;
                }
            }
            return (result.Length != 0);
        }


        private static ngram_t[] _StreetDictFirstWord_SINGLE_NGRAMS = new[] { ngram_t.StreetDictFirstWord() };
        private static ngram_t[] _StreetDictOneWord_SINGLE_NGRAMS   = new[] { ngram_t.StreetDictOneWord() };
        private static ngram_t[] _StreetPostfix_SINGLE_NGRAMS       = new[] { ngram_t.StreetPostfix() };
        private static ngram_t[] _StreetEndKeyWord_SINGLE_NGRAMS    = new[] { ngram_t.PossibleStreetFirstWord(), ngram_t.StreetEndKeyWord() };
        [M(O.AggressiveInlining)] public bool TryFindStreetOnly( List< word_t > words, out IReadOnlyCollection< SearchResult > results )
        {
            var ss = default(SortedSetByRef< SearchResult >);

            var ng = new ngram_t();
            var prev_ng_type = ngramType.__UNDEFINED__;
            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                var success = Classify( words[ index ], ref ng );
                if ( !success )
                {
                    prev_ng_type = ngramType.__UNDEFINED__;
                    continue;
                }

             AGAIN:
                switch ( ng.type )
                {
                    case ngramType.streetDictFirstWord:
                        if ( _Model.IsStreetMultiWord( words, index, out var streetMultiWords ) )
                        {
                            streetMultiWords.length--;
                            index += streetMultiWords.length;

                            if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );
                            ss.AddEx_Street( index, _StreetDictFirstWord_SINGLE_NGRAMS, in streetMultiWords );
                        }
                        else
                        {
                            switch ( ng.another_type )
                            {
                                case ngramType.streetDictOneWord: 
                                case ngramType.streetPostfix    : 
                                case ngramType.streetEndKeyWord : 
                                         ng.type = ng.another_type;         goto AGAIN;
                                case ngramType.possibleStreetFirstWord: 
                                         ng.type = ng.another_type;         break;
                                default: ng.type = ngramType.__UNDEFINED__; break;
                            }
                        }
                    break;

                    case ngramType.streetDictOneWord:
                        if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );
                        ss.AddEx( index, _StreetDictOneWord_SINGLE_NGRAMS ); 
                    break;

                    case ngramType.streetPostfix:
                        if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );
                        ss.AddEx( index, _StreetPostfix_SINGLE_NGRAMS ); 
                    break;

                    case ngramType.streetEndKeyWord:
                        if ( prev_ng_type == ngramType.possibleStreetFirstWord )
                        {
                            if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );
                            ss.AddEx( index, _StreetEndKeyWord_SINGLE_NGRAMS ); 
                        }
                    break;
                }
                prev_ng_type = ng.type;
            }
            results = ss;
            return (ss != null);
        }
        [M(O.AggressiveInlining)] public bool TryFindFirst2RigthStreetOnly( List< word_t > words, int startIndex, out SearchResult result )
        {
            result = default;

            var ng = new ngram_t();
            var prev_ng_type = ngramType.__UNDEFINED__;            
            for ( int index = startIndex, startIndex_saved = startIndex, len = words.Count; index < len; index++ )
            {
                var success = Classify( words[ index ], ref ng );
                if ( !success )
                {
                    break;
                }

            AGAIN:
                switch ( ng.type )
                {
                    case ngramType.punctuation:
                        if ( startIndex == startIndex_saved )
                        {
                            startIndex++;
                        }
                    break;

                    case ngramType.streetDictFirstWord:
                        if ( _Model.IsStreetMultiWord( words, index, out var streetMultiWords ) )
                        {
                            streetMultiWords.length--;
                            index += streetMultiWords.length;

                            var sr = new SearchResult( index, _StreetDictFirstWord_SINGLE_NGRAMS, default, in streetMultiWords );
                            if ( result.Length < sr.Length ) result = sr;
                            if ( result.StartIndex != startIndex )
                            {
                                return (false);
                            }
                        }
                        else
                        {
                            switch ( ng.another_type )
                            {
                                case ngramType.streetDictOneWord: 
                                case ngramType.streetPostfix    : 
                                case ngramType.streetEndKeyWord : 
                                         ng.type = ng.another_type;         goto AGAIN;
                                case ngramType.possibleStreetFirstWord: 
                                         ng.type = ng.another_type;         break;
                                default: ng.type = ngramType.__UNDEFINED__; break;
                            }
                        }
                    break;

                    case ngramType.streetDictOneWord:
                    {
                        var sr = new SearchResult( index, _StreetDictOneWord_SINGLE_NGRAMS );
                        if ( result.Length < sr.Length ) result = sr;
                        if ( result.StartIndex != startIndex )
                        {
                            return (false);
                        }
                    }
                    break;

                    case ngramType.streetPostfix:
                    {
                        var sr = new SearchResult( index, _StreetPostfix_SINGLE_NGRAMS );
                        if ( result.Length < sr.Length ) result = sr;
                        if ( result.StartIndex != startIndex )
                        {
                            return (false);
                        }
                    }
                    break;

                    case ngramType.streetEndKeyWord:
                        if ( prev_ng_type == ngramType.possibleStreetFirstWord )
                        {
                            var sr = new SearchResult( index, _StreetEndKeyWord_SINGLE_NGRAMS );
                            if ( result.Length < sr.Length ) result = sr;
                            if ( result.StartIndex != startIndex )
                            {
                                return (false);
                            }
                        }
                    break;
                }
                prev_ng_type = ng.type;
            }
            return (result.Length != 0);
        }
        #endregion

        #region [.text classifier.]
        [M(O.AggressiveInlining)] private bool Classify( word_t w, ref ngram_t ng )
        {
            if ( w.valueOriginal == null )
            {
                ng.type = ngramType.__UNDEFINED__;
                return (false);
            }

            switch ( w.nerInputType )
            {
                case NerInputType.Num:
                    if ( w.IsOutputTypeOther() )
                    {
                        if ( w.IsExtraWordTypeIntegerNumber() || IsValidHouseNumber( w.valueOriginal ) )
                        {
                            ng.type = ngramType.houseNumber;
                            return (true);
                        }
                        else
                        {
                            goto case NerInputType.LatinFirstCapital; //"1. Kanal" => "1."
                        }
                    }
                break;


                case NerInputType.NumCapital:
                    if ( IsValidHouseNumber( w.valueUpper ) )
                    {
                        ng.type = ngramType.houseNumber;
                        return (true);
                    }
                break;

                //default: //case NerInputType.Other:
                case NerInputType.MixCapital:
                case NerInputType.MixCapitalWithDot:
                case NerInputType.LatinFirstCapital:
                case NerInputType.AllCapital:
                case NerInputType.FirstLowerWithUpper:
                //case NerInputType.FirstCapital:
                    if ( IsStreetWordType( w.valueOriginal, ref ng ) )
                    {
                        return (true);
                    }
                    ng.type = ngramType.possibleStreetFirstWord;
                    return (true);

                case NerInputType.OneCapital:
                case NerInputType.OneCapitalWithDot:
                    if ( IsStreetWordType( w.valueOriginal, ref ng ) )
                    {
                        return (true);
                    }
                    goto case NerInputType.Other;

                case NerInputType.LatinCapital:
                case NerInputType.Other:
                    if ( w.length == ngram_t.ONE_LEN )
                    {
                        var ch = w.valueOriginal[ 0 ];
                        if ( ngram_t.AllowedPunctuation.Contains( ch ) )
                        {
                            ng.length = ngram_t.ONE_LEN;
                            ng.type   = ngramType.punctuation;
                            return (true);
                        }
                        if ( (_CTM[ ch ] & CharType.IsLetter) == CharType.IsLetter )
                        {
                            ng.length = ngram_t.ONE_LEN;
                            ng.type   = ngramType.letter;
                            return (true);
                        }
                    }
                    else if ( IsValidHouseNumber( w.valueUpper ) )
                    {
                        ng.type = ngramType.houseNumber;
                        return (true);
                    }
                    else if ( IsStreetWordType( w.valueOriginal, ref ng ) )
                    {
                        return (true);
                    }
                break;
            }

            ng.type = ngramType.__UNDEFINED__;
            return (false);
        }
        [M(O.AggressiveInlining)] private bool IsValidHouseNumber( string value )
        {
            fixed ( char* _base = value )
            {
                for ( var i = value.Length - 1; 0 < i; i-- )
                {
                    var ct = _CTM[ _base[ i ] ];
                    if ( (ct & CharType.IsDigit ) != CharType.IsDigit  &&
                         (ct & CharType.IsLetter) != CharType.IsLetter &&
                         (ct & CharType.IsHyphen) != CharType.IsHyphen
                       )
                    {
                        return (false);
                    }
                }
                return ((_CTM[ _base[ 0 ] ] & CharType.IsDigit) == CharType.IsDigit);
            }
        }
        [M(O.AggressiveInlining)] private bool IsStreetWordType( string value, ref ngram_t ng )
        {
            var swt = _Model.IsStreetWordType( value );
            switch ( swt )
            {
                case StreetWordType.StreetPostfix:
                    ng.type = ngramType.streetPostfix;
                    return (true);

                case StreetWordType.StreetEndKeyWord:
                    ng.type = ngramType.streetEndKeyWord;
                    return (true);

                case StreetWordType.StreetDictOneWord:
                    ng.type = ngramType.streetDictOneWord;
                    //ng.another_type = GetStreetPostfixWordType( w.valueOriginal );
                    return (true);

                case StreetWordType.StreetDictFirstWordOfMultiWord:
                    ng.type         = ngramType.streetDictFirstWord;
                    ng.another_type = GetStreetPostfixWordType( value );
                    return (true);

                case (StreetWordType.StreetDictOneWord | StreetWordType.StreetDictFirstWordOfMultiWord):
                    ng.type         = ngramType.streetDictFirstWord;
                    ng.another_type = ngramType.streetDictOneWord;
                    return (true);
            }
            return (false);
        }
        [M(O.AggressiveInlining)] private ngramType GetStreetPostfixWordType( string value )
        {
            var swt = _Model.GetStreetPostfixWordType( value );
            switch ( swt )
            {
                case StreetWordType.StreetPostfix   : return (ngramType.streetPostfix);
                case StreetWordType.StreetEndKeyWord: return (ngramType.streetEndKeyWord);
                default                             : return (ngramType.possibleStreetFirstWord); //return (ngramType.__UNDEFINED__);
            }
        }
        #endregion
#if DEBUG
        public override string ToString() => $"[{_Root4FindAll}; {_Root4Find2Rigth}]";
#endif
    }
}
