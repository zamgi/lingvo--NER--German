using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lingvo.NER.NeuralNetwork.Tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.NeuralNetwork.NerPostMerging
{
    using tuple = ngram_2_t.tuple;

    /// <summary>
    ///
    /// </summary>
    public static class NerPostMerger
    {
        ///// <summary>
        ///// 
        ///// </summary>
        //public enum NerPostMergerTypeEnum
        //{
        //    Merge,
        //    BuildChains,
        //}
        /// <summary>
        /// 
        /// </summary>
        public enum NerPostMergerReturnTypeEnum
        {
            AllWords,
            NerWordsOnly,
        }

        private static Searcher   _Searcher;
        private static Searcher_2 _Searcher_2;
        private static Searcher_2 _Searcher_2_UpperCase;
        static NerPostMerger()
        {
            #region [.Searcher.]
            const int MAX_CHAINT_LEN = 50;

            var b_ner_types = new[] 
            { 
                NNerOutputType.B_PER,
                NNerOutputType.B_ORG,
                NNerOutputType.B_LOC,
                NNerOutputType.B_MISC,

                NNerOutputType.I_PER,
                NNerOutputType.I_ORG,
                NNerOutputType.I_LOC,
                NNerOutputType.I_MISC,
            };
            static NNerOutputType get_i_ner_types( NNerOutputType b_ner_type )
            {
                switch ( b_ner_type )
                {
                    case NNerOutputType.B_PER : case NNerOutputType.I_PER : return (NNerOutputType.I_PER);
                    case NNerOutputType.B_ORG : case NNerOutputType.I_ORG : return (NNerOutputType.I_ORG);
                    case NNerOutputType.B_LOC : case NNerOutputType.I_LOC : return (NNerOutputType.I_LOC);
                    case NNerOutputType.B_MISC: case NNerOutputType.I_MISC: return (NNerOutputType.I_MISC);
                    default: throw (new ArgumentException( b_ner_type.ToString() ));
                }
            };
            static NNerOutputType[] create_ner_types_array( NNerOutputType b_ner_type, int len )
            {
                var nnerOutputTypes = new NNerOutputType[ len ];
                    nnerOutputTypes[ 0 ] = b_ner_type;
                var i_ner_types = get_i_ner_types( b_ner_type );
                for ( var i = 1; i < len; i++ )
                {
                    nnerOutputTypes[ i ] = i_ner_types;
                }
                return (nnerOutputTypes);
            };

            var ngrams = new List< ngram_t >( b_ner_types.Length * MAX_CHAINT_LEN );

            foreach ( var b_ner_type in b_ner_types )
            {
                for ( var i = 1; i <= MAX_CHAINT_LEN; i++ )
                {
                    var nnerOutputTypes = create_ner_types_array( b_ner_type, i );
                    ngrams.Add( new ngram_t( nnerOutputTypes, b_ner_type.ToNerOutputType() ) );
                }
            }

            _Searcher = new Searcher( ngrams );
            #endregion

            #region [.Searcher_2.]
            var ngrams_2 = new ngram_2_t[]
            {
                new ngram_2_t( new tuple[] { tuple.Create( "Von" ), tuple.Create( "der" )                      , tuple.Create( NNerOutputType.B_PER ) }, NerOutputType.PERSON ),
                new ngram_2_t( new tuple[] { tuple.Create( "Von" ), tuple.Create( "der" )                      , tuple.Create( NNerOutputType.I_PER ) }, NerOutputType.PERSON ),
                new ngram_2_t( new tuple[] { tuple.Create( "Von" ), tuple.Create( NNerOutputType.B_PER, "der" ), tuple.Create( NNerOutputType.I_PER ) }, NerOutputType.PERSON ),
                new ngram_2_t( new tuple[] { tuple.Create( "Von" ), tuple.Create( NNerOutputType.B_PER, "der" ), tuple.Create( NNerOutputType.B_PER ) }, NerOutputType.PERSON ),
                new ngram_2_t( new tuple[] { tuple.Create( "Von" ), tuple.Create( NNerOutputType.I_PER, "der" ), tuple.Create( NNerOutputType.I_PER ) }, NerOutputType.PERSON ),

                new ngram_2_t( new tuple[] { tuple.Create( "Von" ), tuple.Create( NNerOutputType.I_ORG, "der" ), tuple.Create( NNerOutputType.I_PER ) }, NerOutputType.PERSON ),
                new ngram_2_t( new tuple[] { tuple.Create( NNerOutputType.B_PER, "Von" ), tuple.Create( NNerOutputType.I_ORG, "der" ), tuple.Create( NNerOutputType.I_PER ) }, NerOutputType.PERSON ),

                new ngram_2_t( new tuple[] { tuple.Create( NNerOutputType.B_PER, "Von" ), tuple.Create( "der" ), tuple.Create( NNerOutputType.B_PER ) }, NerOutputType.PERSON ),
                new ngram_2_t( new tuple[] { tuple.Create( NNerOutputType.B_PER, "Von" ), tuple.Create( "der" ), tuple.Create( NNerOutputType.I_PER ) }, NerOutputType.PERSON ),
            };
            _Searcher_2 = new Searcher_2( ngrams_2, upperCase: false );
            #endregion

            #region [.Searcher_2_UpperCase.]
            var ngrams_2_upper = new List< ngram_2_t >( ngrams_2.Length );
            foreach ( var ng in ngrams_2 )
            {
                var tuples_upper = ng.Tuples.Select( t => tuple.Create( t.NNerOutputType, t.Value?.ToUpperInvariant() ) ).ToArray();
                ngrams_2_upper.Add( new ngram_2_t( tuples_upper, ng.ResultNerOutputType ) );
            }
            _Searcher_2_UpperCase = new Searcher_2( ngrams_2_upper, upperCase: true );
            #endregion
        }

        //public static void Run( List< word_t > words, NerPostMergerTypeEnum mt, NerPostMergerReturnTypeEnum rt = NerPostMergerReturnTypeEnum.AllWords )
        //{
        //    switch ( mt )
        //    {
        //        case NerPostMergerTypeEnum.Merge      : Run_Merge( words, rt ); break;
        //        case NerPostMergerTypeEnum.BuildChains: Run_BuildChains( words, rt ); break;
        //        default: 
        //            throw (new ArgumentException( mt.ToString() ));
        //    }
        //}
        public static void Run_Merge( List< word_t > words, bool upperCase, NerPostMergerReturnTypeEnum rt = NerPostMergerReturnTypeEnum.AllWords )
        {
            var searcher_2 = (upperCase ? _Searcher_2_UpperCase : _Searcher_2);
            Run_Searcher_2( searcher_2, words );

            if ( _Searcher.TryFindAll_2( words, out var ss ) )
            {
                var was_merged_chain = false;
                foreach ( var sr in ss.GetValues() )
                {
                    was_merged_chain |= Merge( sr, words );
                }
                if ( was_merged_chain )
                {
                    words.RemoveWhereValueOriginalIsNull();
                }
            }

            LeaveNerWordsOnlyIfNeed( words, rt );
        }
        //public static void Run_BuildChains( List< word_t > words, NerPostMergerReturnTypeEnum rt = NerPostMergerReturnTypeEnum.AllWords )
        //{
        //    Run_Searcher_2( words );

        //    if ( _Searcher.TryFindAll( words, out var ss ) )
        //    {
        //        foreach ( var sr in ss )
        //        {
        //            BuildChains( sr, words );
        //        }
        //    }

        //    LeaveNerWordsOnlyIfNeed( words, rt );
        //}

        [M(O.AggressiveInlining)] private static void LeaveNerWordsOnlyIfNeed( List< word_t > words, NerPostMergerReturnTypeEnum rt )
        {
            if ( rt == NerPostMergerReturnTypeEnum.NerWordsOnly )
            {
                for ( var i = words.Count - 1; 0 <= i; i-- )
                {
                    if ( words[ i ].IsNerOutputTypeOther() )
                    {
                        words.RemoveAt_Ex( i );
                    }
                }
            }
        }

        private static void Run_Searcher_2( Searcher_2 searcher_2, List< word_t > words )
        {
            if ( searcher_2.TryFindAll( words, out var ss ) )
            {
                foreach ( var sr in ss )
                {
                    CorrectMarkup( sr, words );
                }
            }
        }
        [M(O.AggressiveInlining)] private static void CorrectMarkup( in SearchResult sr, List< word_t > words )
        {
            (NNerOutputType b_nt, NNerOutputType i_nt) x;
            switch ( sr.NerOutputType )
            {
                case NerOutputType.PERSON       : x = (NNerOutputType.B_PER , NNerOutputType.I_PER);  break;
                case NerOutputType.ORGANIZATION : x = (NNerOutputType.B_ORG , NNerOutputType.I_ORG);  break;
                case NerOutputType.LOCATION     : x = (NNerOutputType.B_LOC , NNerOutputType.I_LOC);  break;
                case NerOutputType.MISCELLANEOUS: x = (NNerOutputType.B_MISC, NNerOutputType.I_MISC); break;
                default: throw (new ArgumentException( sr.NerOutputType.ToString() ));
            }

            words[ sr.StartIndex ].nnerOutputType = x.b_nt;
            for ( int i = sr.StartIndex + 1, len = i + sr.Length - 1; i < len; i++ )
            {
                words[ i ].nnerOutputType = x.i_nt;
            }
        }

        [M(O.AggressiveInlining)] private static void BuildChains( in SearchResult sr, List< word_t > words )
        {
            var w1 = words[ sr.StartIndex ];
            if ( w1.IsSkip() )
                return;

            switch ( sr.Length )
            {
                case 1:
                    w1.nerOutputType = sr.NerOutputType; //w1.nnerOutputType.ToNerOutputType();
                break;

                case 2:
                {
                    var w2 = words[ sr.StartIndex + 1 ];
                                        
                    w1.SetNextPrev( w2, sr.NerOutputType );
                }
                break;

                case 3:
                {
                    var w2 = words[ sr.StartIndex + 1 ];
                    var w3 = words[ sr.StartIndex + 2 ];

                    w1.SetNextPrev( w2, sr.NerOutputType );
                    w2.SetNextPrev( w3, sr.NerOutputType );
                }
                break;

                case 4:
                {
                    var w2 = words[ sr.StartIndex + 1 ];
                    var w3 = words[ sr.StartIndex + 2 ];
                    var w4 = words[ sr.StartIndex + 3 ];

                    w1.SetNextPrev( w2, sr.NerOutputType );
                    w2.SetNextPrev( w3, sr.NerOutputType );
                    w3.SetNextPrev( w4, sr.NerOutputType );
                }
                break;

                case 5:
                {
                    var w2 = words[ sr.StartIndex + 1 ];
                    var w3 = words[ sr.StartIndex + 2 ];
                    var w4 = words[ sr.StartIndex + 3 ];
                    var w5 = words[ sr.StartIndex + 4 ];

                    w1.SetNextPrev( w2, sr.NerOutputType );
                    w2.SetNextPrev( w3, sr.NerOutputType );
                    w3.SetNextPrev( w4, sr.NerOutputType );
                    w4.SetNextPrev( w5, sr.NerOutputType );
                }
                break;

                default:
                {
                    for ( int i = sr.StartIndex + 1, len = i + sr.Length - 1; i < len; i++ )
                    {
                        var w = words[ i ];
                        w1.SetNextPrev( w, sr.NerOutputType );
                        w1 = w;
                    }
                }
                break;
            }
        }
        [M(O.AggressiveInlining)] private static bool Merge( in SearchResult sr, List< word_t > words )
        {
            var w1 = words[ sr.StartIndex ];
            if ( w1.IsSkip() )
                return (false);

            if ( sr.Length == 1 )
            {
                if ( !w1.IsPunctuation() )
                {
                    w1.nerOutputType = sr.NerOutputType;
                }
                //---w1.nerOutputType = w1.IsPunctuation() ? NerOutputType.Other : sr.NerOutputType;
                return (false);
            }

            if ( w1.IsPunctuation() )
            {
                //---w1.nerOutputType = NerOutputType.Other;
                if ( words.Count <= sr.StartIndex )
                {
                    return (false);
                }
                var sr_2 = new SearchResult( sr.StartIndex + 1, sr.Length - 1, sr.NerOutputType );
                return (Merge( sr_2, words ));
            }            

            const char SPACE = ' ';
            var valueUpperBuff    = new StringBuilder( 256 );
            var valueOriginalBuff = new StringBuilder( 256 );

            var t = default(word_t);
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                t = words[ i ];
                valueUpperBuff   .Append( t.valueUpper    ).Append( SPACE );
                valueOriginalBuff.Append( t.valueOriginal ).Append( SPACE );
                t.ClearValuesAndNerChain();
            }

            var nw = new word_t() 
            {
                startIndex    = w1.startIndex,
                length        = (t.startIndex - w1.startIndex) + t.length,
                nerOutputType = sr.NerOutputType,
                valueOriginal = valueOriginalBuff.ToStringWithoutLastChar(),
                valueUpper    = valueUpperBuff   .ToStringWithoutLastChar(),
            };
            words[ sr.StartIndex ] = nw;
            return (true);
        }

        //-------------------------------------------------------------------------------------------------//
        [M(O.AggressiveInlining)] private static string ToStringWithoutLastChar( this StringBuilder sb ) => sb.ToString( 0, sb.Length - 1 );
        [M(O.AggressiveInlining)] private static void ClearValuesAndNerChain( this word_t w )
        {
            w.valueUpper = w.valueOriginal = null;
            w.ResetNextPrev();
        }
        [M(O.AggressiveInlining)] private static void ClearOutputTypeAndNerChain( this word_t w )
        {
            w.nerOutputType = NerOutputType.Other;
            w.ResetNextPrev();
        }
        private static void RemoveWhereValueOriginalIsNull( this List< word_t > words )
        {
            for ( int i = words.Count - 1; 0 <= i; i-- )
            {
                if ( words[ i ].valueOriginal == null )
                {
                    words.RemoveAt_Ex( i );
                }
            }
        }
        private static void RemoveWhereValueOriginalIsNull( this List< word_t > words, out int removeCount )
        {
            removeCount = 0;
            for ( int i = words.Count - 1; 0 <= i; i-- )
            {
                if ( words[ i ].valueOriginal == null )
                {
                    words.RemoveAt_Ex( i );
                    removeCount++;
                }
            }
        }
        [M(O.AggressiveInlining)] private static void RemoveAt_Ex( this List< word_t > words, int removeIndex )
        {
            //-1-//
            //var w = words[ removeIndex ];
            //---w.removeFromChain();

            //-2-//
            words.RemoveAt( removeIndex );
        }

        [M(O.AggressiveInlining)] private static bool IsSkip( this word_t w ) => w.IsWordInNerChain || !w.IsNerOutputTypeOther() || w.IsValueOriginalNullOrEmpty();
        [M(O.AggressiveInlining)] private static bool IsNerOutputTypeOther( this word_t w ) => (w.nerOutputType == NerOutputType.Other);
        [M(O.AggressiveInlining)] private static bool IsPunctuation( this word_t w ) => ((w.extraWordType & ExtraWordType.Punctuation) == ExtraWordType.Punctuation);
        [M(O.AggressiveInlining)] private static bool IsValueOriginalNullOrEmpty( this word_t w ) => string.IsNullOrEmpty( w.valueOriginal );
    }
}
