using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Lingvo.NER.NeuralNetwork.NerPostMerging;
using Lingvo.NER.NeuralNetwork.Tokenizing;
using Lingvo.NER.NeuralNetwork.Utils;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.NeuralNetwork
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class NNER_Processor : IDisposable
    {
        private Predictor _Predictor;
        private Tokenizer _Tokenizer;
        public NNER_Processor( Predictor predictor, TokenizerConfig tokenizerConfig )
        {
            _Predictor = predictor ?? throw (new ArgumentNullException( nameof(predictor) ));
            _Tokenizer = new Tokenizer( tokenizerConfig, replaceNumsOnPlaceholder: true );
        }
        public NNER_Processor( TokenizerConfig tokenizerConfig ) => _Tokenizer = new Tokenizer( tokenizerConfig, replaceNumsOnPlaceholder: true );
        public static NNER_Processor Create( Predictor predictor, TokenizerConfig tokenizerConfig )
            => ((predictor != null) ? new NNER_Processor( predictor, tokenizerConfig ) : new NNER_Processor( tokenizerConfig ));
        public void Dispose() => _Tokenizer.Dispose();

        [M(O.AggressiveInlining)] public bool TryProcessText( string text, out List< word_t > nerWords ) => TryProcessText( text, _Predictor, out nerWords );
        /*{
            if ( !_Tokenizer.TryTokenizeBySents( text, out var input_sents ) )
            {
                nerWords = default;
                return (false);
            }

            if ( input_sents.Count == 1 )
            {
                var input_words  = input_sents[ 0 ];
                var output_words = _Predictor.Predict( input_words.Select( w => w.valueOriginal ).ToList( input_words.Count ) );

                input_words.SetNNerOutputType( output_words );
                NerPostMerger.Run_Merge( input_words, NerPostMerger.NerPostMergerReturnTypeEnum.NerWordsOnly );

                nerWords = input_words;
                return (0 < input_words.Count);
            }
            else
            {
                var sd  = new SortedDictionary< long, IList< word_t > >();
                var cnt = 0;
                Parallel.ForEach( input_sents, (input_words, _, i) =>
                {
                    var output_words = _Predictor.Predict( input_words.Select( w => w.valueOriginal ).ToList( input_words.Count ) );

                    input_words.SetNNerOutputType( output_words );
                    NerPostMerger.Run_Merge( input_words, NerPostMerger.NerPostMergerReturnTypeEnum.NerWordsOnly );

                    if ( 0 < input_words.Count )
                    {
                        sd.AddWithLock( i, input_words );
                        Interlocked.Add( ref cnt, input_words.Count );
                    }
                });
                if ( 0 < cnt )
                {
                    nerWords = sd.Values.SelectMany( t => t ).ToList( cnt );
                    return (true);
                }
                nerWords = default;
                return (false);
            }
        }
        //*/
        public bool TryProcessText( string text, Predictor predictor, out List< word_t > nerWords )
        {
            if ( !_Tokenizer.TryTokenizeBySents( text, out var input_sents ) )
            {
                nerWords = default;
                return (false);
            }

            if ( input_sents.Count == 1 )
            {
                var input_words  = input_sents[ 0 ];
                var input_tokens = Tokenizer.ToNerInputTokens( input_words, predictor.ModelUpperCase );
                var output_words = predictor.Predict( input_tokens );

                input_words.SetNNerOutputType( output_words );
                NerPostMerger.Run_Merge( input_words, predictor.ModelUpperCase, NerPostMerger.NerPostMergerReturnTypeEnum.NerWordsOnly );

                nerWords = input_words;
                return (0 < input_words.Count);
            }
            else
            {
                var sd  = new SortedDictionary< long, IList< word_t > >();
                var cnt = 0;
#if DEBUG
                var po = new ParallelOptions() { MaxDegreeOfParallelism = 1 }; //Environment.ProcessorCount };
#else
                var po = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
#endif
                Parallel.ForEach( input_sents, po, (input_words, _, i) =>
                {
                    var input_tokens = Tokenizer.ToNerInputTokens( input_words, predictor.ModelUpperCase );
                    var output_words = predictor.Predict( input_tokens );

                    input_words.SetNNerOutputType( output_words );
                    NerPostMerger.Run_Merge( input_words, predictor.ModelUpperCase, NerPostMerger.NerPostMergerReturnTypeEnum.NerWordsOnly );

                    if ( 0 < input_words.Count )
                    {
                        sd.AddWithLock( i, input_words );
                        Interlocked.Add( ref cnt, input_words.Count );
                    }
                });
                if ( 0 < cnt )
                {
                    nerWords = sd.Values.SelectMany( t => t ).ToList( cnt );
                    return (true);
                }
                nerWords = default;
                return (false);
            }
        }
    }
}

