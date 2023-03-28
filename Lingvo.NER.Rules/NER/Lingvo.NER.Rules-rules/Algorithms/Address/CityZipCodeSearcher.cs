using System.Collections.Generic;
using System.Linq;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.core.Infrastructure;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Address
{
    /// <summary>
    ///
    /// </summary>
    internal sealed class CityZipCodeSearcher
    {
        #region [.model.]
        /// <summary>
        /// 
        /// </summary>
        private sealed class Model4FindAll
        {
            #region [.ctor().]
            public static Model4FindAll Instance { get; } = new Model4FindAll( GetNgrams() );
            private Model4FindAll( IEnumerable< ngram_t[] > ngrams ) => Root = TreeNode.BuildTree( ngrams );
            public TreeNode Root { get; }
            #endregion

            //---public static ngram_t[] get_separ_ngrams() => new[] { ngram_t.Dash(), ngram_t.VertStick(), ngram_t.Comma(), ngram_t.Semicolon() };
            public static IEnumerable< ngram_t[] > GetNgrams()
            {
                //'Postleitzahl  (Индекс)  Ort  (город)'
                //'Postleitzahl  (Индекс),  Ort  (город)'

                var citie_ngrams = new[] { ngram_t.City(), ngram_t.CityFirstWord() };
                var punctuation_ngram = ngram_t.Punctuation();
                //---var separ_ngrams = get_separ_ngrams();

                var vc = new VersionCombiner< ngram_t >( ngram_t.EqualityComparer.Instance );

                foreach ( var city_ngram in citie_ngrams )
                {
                    //---foreach ( var separ_ng in separ_ngrams )
                    //---{
                    //'(Индекс), (Город)'
                    var base_ngrams = new[] { ngram_t.ZipCodeNumber(), city_ngram };
                    foreach ( var ngrams in vc.GetVersions( base_ngrams, punctuation_ngram /*separ_ng*/ ) ) yield return (ngrams);

                    //'(Город), (Индекс)'
                    base_ngrams = new[] { city_ngram, ngram_t.ZipCodeNumber() };
                    foreach ( var ngrams in vc.GetVersions( base_ngrams, punctuation_ngram /*separ_ng*/ ) ) yield return (ngrams);
                    //---}
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private sealed class Model4Find2Rigth
        {
            #region [.ctor().]
            public static Model4Find2Rigth Instance { get; } = new Model4Find2Rigth( GetNgrams() );
            private Model4Find2Rigth( IEnumerable< ngram_t[] > ngrams ) => Root = TreeNode.BuildTree( ngrams );
            public TreeNode Root { get; }
            #endregion

            private static IEnumerable< ngram_t[] > GetNgrams()
            {
                //', Postleitzahl  (Индекс)  Ort  (город)'
                //', Postleitzahl  (Индекс),  Ort  (город)'

                var punctuation_ngrams = new[] { ngram_t.Punctuation() };
                //---var separ_ngrams = Model4FindAll.get_separ_ngrams();

                foreach ( var ngrams in Model4FindAll.GetNgrams() )
                {
                    //'(Индекс), (Город)'
                    //'(Город), (Индекс)'
                    yield return (ngrams);

                    //---foreach ( var separ_ng in separ_ngrams )
                    //---{
                    //', (Индекс), (Город)'
                    //', (Город), (Индекс)'                        
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

        public CityZipCodeSearcher( IAddressModel model )
        {
            _Model           = model;
            _Root4FindAll    = Model4FindAll   .Instance.Root;
            _Root4Find2Rigth = Model4Find2Rigth.Instance.Root;
        }
        #endregion

        #region [.public method's.]
        [M(O.AggressiveInlining)] public bool TryFindAll( List< word_t > words, out IReadOnlyCollection< SearchResult > results )
        {
            var ss = default(SortedSetByRef< SearchResult >);
            var node = _Root4FindAll;
            var finder = Finder.Create( _Root4FindAll );

            var ng = new ngram_t();
            var cityMultiWords = default((string cityFullValue, int length));
            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                var success = Classify( words[ index ], ref ng );
                if ( !success && (node == _Root4FindAll) )
                {
                    cityMultiWords = default;
                    continue;
                }

                switch ( ng.type )
                {
                    case ngramType.cityFirstWord:
                        if ( _Model.IsCityMultiWord( words, index, out cityMultiWords ) )
                        {
                            cityMultiWords.length--;
                            index += cityMultiWords.length;
                        }
                        else if ( ng.another_type == ngramType.city )
                        { 
                            ng.type = ng.another_type;
                        }
                        else
                        {
                            ng.type = ngramType.__UNDEFINED__;
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
                            ss.AddEx_City( index, node.Ngrams.First, in cityMultiWords );
                        break;

                        default:
                            foreach ( var ngrams in node.Ngrams )
                            {
                                ss.AddEx_City( index, ngrams, in cityMultiWords );
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
            var cityMultiWords = default((string cityFullValue, int length));
            for ( int index = startIndex, len = words.Count; index < len; index++ )
            {
                var success = Classify( words[ index ], ref ng );
                if ( !success && (node == _Root4Find2Rigth) )
                {
                    break;
                }

                switch ( ng.type )
                {
                    case ngramType.cityFirstWord:
                        if ( _Model.IsCityMultiWord( words, index, out cityMultiWords ) )
                        {
                            cityMultiWords.length--;
                            index += cityMultiWords.length;
                        }
                        else if ( ng.another_type == ngramType.city )
                        { 
                            ng.type = ng.another_type;
                        }
                        else
                        {
                            ng.type = ngramType.__UNDEFINED__;
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
                            var sr = new SearchResult( index, node.Ngrams.First, in cityMultiWords, default );
                            if ( result.Length < sr.Length ) result = sr;
                        }
                        break;

                        default:
                            foreach ( var ngrams in node.Ngrams )
                            {
                                var sr = new SearchResult( index, ngrams, in cityMultiWords, default );
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


        private static ngram_t[] _City_SINGLE_NGRAMS          = new[] { ngram_t.City() };
        private static ngram_t[] _CityFirstWord_SINGLE_NGRAMS = new[] { ngram_t.CityFirstWord() };
        [M(O.AggressiveInlining)] public bool TryFindCityOnly( List< word_t > words, out IReadOnlyCollection< SearchResult > results )
        {
            var ss = default(SortedSetByRef< SearchResult >);

            var ng = new ngram_t();
            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                if ( !Classify( words[ index ], ref ng ) )
                {
                    continue;
                }

                switch ( ng.type )
                {
                    case ngramType.cityFirstWord:
                        if ( _Model.IsCityMultiWord( words, index, out var cityMultiWords ) )
                        {
                            cityMultiWords.length--;
                            index += cityMultiWords.length;

                            if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );
                            ss.AddEx_City( index, _CityFirstWord_SINGLE_NGRAMS, in cityMultiWords );
                        }
                        else if ( ng.another_type == ngramType.city )
                        { 
                            ng.type = ng.another_type;
                            goto case ngramType.city;
                        }
                    break;

                    case ngramType.city:
                    {
                        if ( ss == null ) ss = new SortedSetByRef< SearchResult >( SearchResult.Comparer.Instance );
                        ss.AddEx( index, _City_SINGLE_NGRAMS );                        
                    }
                    break;
                }
            }
            results = ss;
            return (ss != null);
        }
        [M(O.AggressiveInlining)] public bool TryFindFirst2RigthCityOnly( List< word_t > words, int startIndex, out SearchResult result )
        {
            result = default;

            var ng = new ngram_t();
            for ( int index = startIndex, startIndex_saved = startIndex, len = words.Count; index < len; index++ )
            {
                if ( !Classify( words[ index ], ref ng ) )
                {
                    break;
                }

                switch ( ng.type )
                {
                    case ngramType.punctuation:
                        if ( startIndex == startIndex_saved )
                        {
                            startIndex++;
                        }
                    break;

                    case ngramType.cityFirstWord:
                        if ( _Model.IsCityMultiWord( words, index, out var cityMultiWords ) )
                        {
                            cityMultiWords.length--;
                            index += cityMultiWords.length;

                            var sr = new SearchResult( index, _CityFirstWord_SINGLE_NGRAMS, in cityMultiWords, default );
                            if ( result.Length < sr.Length ) result = sr;
                            if ( result.StartIndex != startIndex )
                            {
                                return (false);
                            }
                        }
                        else if ( ng.another_type == ngramType.city )
                        { 
                            ng.type = ng.another_type;
                            goto case ngramType.city;
                        }
                    break;

                    case ngramType.city:
                    {
                        var sr = new SearchResult( index, _City_SINGLE_NGRAMS );
                        if ( result.Length < sr.Length ) result = sr;
                        if ( result.StartIndex != startIndex )
                        {
                            return (false);
                        }
                    }
                    break;
                }
            }
            return (result.Length != 0);
        }
        #endregion

        #region [.text classifier.]
        [M(O.AggressiveInlining)] private bool Classify( word_t w, ref ngram_t ng )
        {
            switch ( w.nerInputType )
            {
                case NerInputType.Num:
                {
                    if ( w.IsExtraWordTypeIntegerNumber() && w.IsOutputTypeOther() &&
                         (w.length == ngram_t.ZIP_CODE_LEN) && _Model.IsZipCode( w.valueUpper ) 
                       )
                    {
                        ng.length = ngram_t.ZIP_CODE_LEN;
                        ng.type   = ngramType.zipCodeNumber;
                        return (true);
                    }
                }
                break;

                //default: //case NerInputType.Other:
                case NerInputType.MixCapital:
                case NerInputType.MixCapitalWithDot:                
                case NerInputType.LatinFirstCapital:
                //case NerInputType.FirstCapital:
                {
                    if ( (ng.type != ngramType.cityFirstWord) && (ng.type != ngramType.city) ) //if prev.
                    {
                        var cwt = _Model.IsCityWordType( w.valueOriginal );
                        switch ( cwt )
                        { 
                            case CityWordType.CityOneWord:
                                ng.type = ngramType.city;
                                return (true);

                            case CityWordType.CityFirstWordOfMultiWord:
                                ng.type = ngramType.cityFirstWord;
                                return (true);

                            case (CityWordType.CityOneWord | CityWordType.CityFirstWordOfMultiWord):
                                ng.type         = ngramType.cityFirstWord;
                                ng.another_type = ngramType.city;
                                return (true);
                        }
                    }
                }
                break;

                case NerInputType.OneCapital:
                case NerInputType.OneCapitalWithDot:
                case NerInputType.Other:
                {
                    if ( (w.length == ngram_t.ONE_LEN) && ngram_t.AllowedPunctuation.Contains( w.valueOriginal[ 0 ] ) )
                    {
                        ng.length = ngram_t.ONE_LEN;
                        ng.type   = ngramType.punctuation;
                        return (true);

                        #region comm.
                        //if ( w.IsExtraWordTypeComma() || w.IsExtraWordTypeDash() )
                        //{
                        //    ng.length = ngram_t.ONE_LEN;
                        //    ng.type   = ngramType.punctuation;
                        //    return (true);
                        //}
                        //switch ( w.valueOriginal[ 0 ] )
                        //{
                        //    case ngram_t.VERT_STICK:
                        //    case ngram_t.SEMICOLON:
                        //        ng.length = ngram_t.ONE_LEN;
                        //        ng.type   = ngramType.punctuation;
                        //        return (true);
                        //}
                        #endregion
                    }
                }
                break;
            }

            ng.type = ngramType.__UNDEFINED__;
            return (false);
        }
        #endregion
#if DEBUG
        public override string ToString() => $"[{_Root4FindAll}; {_Root4Find2Rigth}]";
#endif
    }
}
