using System;
using System.Collections.Generic;
using System.Diagnostics;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;
using _NerRules_Processor              = Lingvo.NER.Rules.NerProcessor;
using _NerRules_ProcessorConfig        = Lingvo.NER.Rules.NerProcessorConfig;
using _NerRules_UsedRecognizerTypeEnum = Lingvo.NER.Rules.NerProcessor.UsedRecognizerTypeEnum;
using _NerRules_word_t                 = Lingvo.NER.Rules.tokenizing.word_t;
using _NerRules_UnitedEntity           = Lingvo.NER.Rules.NerPostMerging.NerUnitedEntity;
using _NerRules_OutputType             = Lingvo.NER.Rules.NerOutputType;
using _NNER_Predictor                  = Lingvo.NER.NeuralNetwork.Predictor;
using _NNER_TokenizerConfig            = Lingvo.NER.NeuralNetwork.Tokenizing.TokenizerConfig;
using _NNER_word_t                     = Lingvo.NER.NeuralNetwork.Tokenizing.word_t;
using _NNER_Processor                  = Lingvo.NER.NeuralNetwork.NNER_Processor;
using _NNER_OutputType                 = Lingvo.NER.NeuralNetwork.Tokenizing.NerOutputType;

namespace Lingvo.NER.Combined
{
    /// <summary>
    /// 
    /// </summary>
    public struct NERCombinedConfig
    {
        public _NerRules_ProcessorConfig NerRules_ProcessorConfig { get; set; }

        public _NerRules_UsedRecognizerTypeEnum? __NerRules_UsedRecognizerTypeEnum;
        public _NerRules_UsedRecognizerTypeEnum NerRules_UsedRecognizerTypeEnum
        {
            get => __NerRules_UsedRecognizerTypeEnum.GetValueOrDefault( _NerRules_UsedRecognizerTypeEnum.All_Without_Crf );
            set => __NerRules_UsedRecognizerTypeEnum = value;
        }

        public bool                  NNER_Use             { get; set; }
        public _NNER_Predictor       NNER_Predictor       { get; set; }
        public _NNER_TokenizerConfig NNER_TokenizerConfig { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public struct NERCombinedConfig_ForOuterNNERPredictor
    {
        public _NerRules_ProcessorConfig NerRules_ProcessorConfig { get; set; }

        public _NerRules_UsedRecognizerTypeEnum? __NerRules_UsedRecognizerTypeEnum;
        public _NerRules_UsedRecognizerTypeEnum NerRules_UsedRecognizerTypeEnum
        {
            get => __NerRules_UsedRecognizerTypeEnum.GetValueOrDefault( _NerRules_UsedRecognizerTypeEnum.All_Without_Crf );
            set => __NerRules_UsedRecognizerTypeEnum = value;
        }

        public _NNER_TokenizerConfig NNER_TokenizerConfig { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class NERCombined_Processor : IDisposable
    {
        #region [.ctor().]
        private _NerRules_Processor _NerRules_Processor;
        private _NNER_Processor     _NNER_Processor;
        private _NNER_Predictor     _NNER_Predictor;
        private List< _NerRules_word_t > _NerWords;
        private IntervalList< (_NerRules_word_t w, int i) > _WordsIntervalListBuf;
        public NERCombined_Processor( in NERCombinedConfig cfg )
        {
            _NerRules_Processor = new _NerRules_Processor( cfg.NerRules_ProcessorConfig, cfg.NerRules_UsedRecognizerTypeEnum );
            _NNER_Processor     = (cfg.NNER_Use ? _NNER_Processor.Create( cfg.NNER_Predictor, cfg.NNER_TokenizerConfig ) : null);
            _NNER_Predictor     = (cfg.NNER_Use ? cfg.NNER_Predictor : null);

            _WordsIntervalListBuf = new IntervalList< (_NerRules_word_t w, int i) >( 0x100 );
            _NerWords             = new List< _NerRules_word_t >( 0x100 );
        }
        public static NERCombined_Processor CreateForOuterNNERPredictor( in NERCombinedConfig_ForOuterNNERPredictor cfg ) 
            => new NERCombined_Processor( new NERCombinedConfig() 
            { 
                NerRules_ProcessorConfig        = cfg.NerRules_ProcessorConfig, 
                NerRules_UsedRecognizerTypeEnum = cfg.NerRules_UsedRecognizerTypeEnum,
                NNER_Use                        = true,
                NNER_TokenizerConfig            = cfg.NNER_TokenizerConfig,
            });
        public void Dispose()
        {
            _NerRules_Processor.Dispose();
            _NNER_Processor?.Dispose();
        }
        #endregion

        public bool NNER_Use { [M(O.AggressiveInlining)] get => (_NNER_Processor != null); }

        public (List< _NerRules_word_t > nerWords, List< _NerRules_UnitedEntity > nerUnitedEntities, int relevanceRanking) ProcessText_WithNerRules( string text ) => _NerRules_Processor.Run_UseSimpleSentsAllocate_v2( text );
        public List< _NNER_word_t > ProcessText_WithNNER( string text ) => (_NNER_Processor.TryProcessText( text, out var nerWords ) ? nerWords : null);

        public List< _NerRules_word_t > ProcessText( string text ) => ProcessText( text, _NNER_Predictor );
        public List< _NerRules_word_t > ProcessText( string text, _NNER_Predictor predictor )
        {
            var nerWords = _NerRules_Processor.Run_UseSimpleSentsAllocate_v2( text ).nerWords;
            if ( NNER_Use && _NNER_Processor.TryProcessText( text, predictor, out var nerWords_2 ) )
            {
                if ( nerWords.Count == 0 )
                {
                    foreach ( var w in nerWords_2 )
                    {
                        if ( TryConvertIfNotOther( w, out var nw ) )
                        {
                            nerWords.Add( nw );
                        }
                    }
                }
                else
                {
                    #region [.v1.]
                    Fill( _WordsIntervalListBuf, nerWords );
                    foreach ( var w in nerWords_2 )
                    {
                        if ( !TryConvertIfNotOther( w, out var nw ) ) continue;

                        if ( _WordsIntervalListBuf.TryGetValue( (nw.startIndex, nw.length), out var exists ) )
                        {
                            if ( is_contains_inside( nw, exists.w ) )
                            {
                                nerWords[ exists.i ] = nw;
                            }
                            #region comm.
                            //else if ( !is_contains_inside_2( exists.w, nw ) )
                            //{
                            //    nerWords.Add( nw );
                            //}

                            //if ( is_nerOutputType_equals( nw, exists.w ) )
                            //{
                            //    if ( is_contains_inside( nw, exists.w ) )
                            //    {
                            //        nerWords[ exists.i ] = nw;
                            //    }
                            //}
                            //else
                            //{
                            //    nerWords.Add( nw );
                            //}
                            #endregion
                        }
                        else
                        {
                            nerWords.Add( nw );
                        }
                    }
                    nerWords.Sort( word_by_startIndex_Comparer.Inst );

                    _WordsIntervalListBuf.Clear();
                    #endregion
                }
            }

            return (Fill( _NerWords, nerWords ));
        }

        [M(O.AggressiveInlining)] private static bool is_contains_inside( _NerRules_word_t x, _NerRules_word_t y ) => (x.startIndex <= y.startIndex) && (y.length < x.length);
        [M(O.AggressiveInlining)] private static bool is_nerOutputType_equals( _NerRules_word_t x, _NerRules_word_t y )
        {
            [M(O.AggressiveInlining)] static bool is_name_nerOutputType( _NerRules_word_t z ) => z.nerOutputType switch
            {
                _NerRules_OutputType.PERSON__NNER => true,
                _NerRules_OutputType.Name => true,
                _NerRules_OutputType.NAME__Crf => true,
                _ => false
            };

            if ( x.nerOutputType == y.nerOutputType )
            {
                return (true);
            }
            return (is_name_nerOutputType( x ) && is_name_nerOutputType( y ));
        }
        [M(O.AggressiveInlining)] static _NerRules_OutputType To_NerRules_OutputType( _NNER_OutputType nt )
            => nt switch
            { 
                _NNER_OutputType.Other         => _NerRules_OutputType.Other,
                _NNER_OutputType.Email         => _NerRules_OutputType.Email,
                _NNER_OutputType.Url           => _NerRules_OutputType.Url,
                _NNER_OutputType.PERSON        => _NerRules_OutputType.PERSON__NNER,
                _NNER_OutputType.ORGANIZATION  => _NerRules_OutputType.ORGANIZATION__NNER,
                _NNER_OutputType.LOCATION      => _NerRules_OutputType.LOCATION__NNER,
                _NNER_OutputType.MISCELLANEOUS => _NerRules_OutputType.MISCELLANEOUS__NNER,
                _ => throw (new ArgumentException( nt.ToString() ))
            };
        [M(O.AggressiveInlining)] private static _NerRules_word_t Convert( _NNER_word_t w )
            => new _NerRules_word_t()
            {
                nerOutputType = To_NerRules_OutputType( w.nerOutputType ),
                startIndex    = w.startIndex,
                length        = w.length,
                valueOriginal = w.valueOriginal,
                valueUpper    = w.valueUpper,
            };
        [M(O.AggressiveInlining)] private static bool TryConvertIfNotOther( _NNER_word_t w, out _NerRules_word_t nw )
        {
            if ( w.nerOutputType != _NNER_OutputType.Other )
            {
                nw = Convert( w );
                return (true);
            }
            nw = default;
            return (false);
        }
        [M(O.AggressiveInlining)] private static void Fill( IntervalList< (_NerRules_word_t w, int i) > lst, List< _NerRules_word_t > words )
        {
            lst.Clear();
            for ( var i = 0; i < words.Count; i++ )
            {
                var w = words[ i ];
                lst.Add( (w.startIndex, w.length), (w, i) );
            }
        }
        [M(O.AggressiveInlining)] private static List< _NerRules_word_t > Fill( List< _NerRules_word_t > dest, List< _NerRules_word_t > src )
        {
            dest.Clear();
            dest.AddRange( src );
            return (dest);
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class word_by_startIndex_Comparer : IComparer< _NerRules_word_t >
        {
            public static word_by_startIndex_Comparer Inst { [M(O.AggressiveInlining)] get; } = new word_by_startIndex_Comparer();
            private word_by_startIndex_Comparer() { }
            public int Compare( _NerRules_word_t x, _NerRules_word_t y ) => (x.startIndex - y.startIndex);
        }
    }
}

