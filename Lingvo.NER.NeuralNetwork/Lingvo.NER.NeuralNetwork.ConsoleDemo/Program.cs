using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Lingvo.NER.NeuralNetwork.Applications;
using Lingvo.NER.NeuralNetwork.NerPostMerging;
using Lingvo.NER.NeuralNetwork.Tokenizing;
using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork.ConsoleDemo
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static void Main( string[] args )
        {
            try
            {
                var opts = OptionsExtensions.ReadInputOptions( args, "predict.json" );

                //tokenizer( opts );

                if ( args.Any( a => a == "valid.json" ) )
                {
                    var vr = Validator.Run_Validate( opts );
                    Console.WriteLine( vr );
                }
                else
                {
                    if ( opts.OutputFile.IsNullOrEmpty() ) opts.OutputFile = "output_ner_de.txt";
                    Run_Predict( opts );
                }
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
                Console.ResetColor();
            }
        }

        private static void tokenizer( Options opts )
        {
            var tokenizerConfig = new TokenizerConfig( opts.SentSplitterResourcesXmlFilename, opts.UrlDetectorResourcesXmlFilename );
            using var tokenizer = new Tokenizer( tokenizerConfig, replaceNumsOnPlaceholder: true );

            {
                var text         = "Herr R.G. ihm"; //"Von der Leyen sprach sich nach längerer";
                var words        = tokenizer.Run_NoSentsAllocate( text );
                var output_words = new[] { "O", "O", "B-PER", "O", "O", "O", "O" };

                words.SetNNerOutputType( output_words );
                NerPostMerger.Run_Merge( words, opts.ModelUpperCase );
            }
            {
                var text         = "Von der Leyen sprach sich nach längerer";
                var words        = tokenizer.Run_NoSentsAllocate( text );
                var output_words = new[] { "O", "B-PER", "I-PER", "O", "O", "O", "O" };

                words.SetNNerOutputType( output_words );
                NerPostMerger.Run_Merge( words, opts.ModelUpperCase );
            }
        }
        unsafe private static void Run_Predict( Options opts )
        {
            Logger.WriteLine( $"Test model '{opts.ModelFilePath}' by input corpus '{opts.InputTestFile}'" );
            Logger.WriteLine( $"Output to '{Path.GetFullPath( opts.OutputFile )}'" );
            Console.WriteLine();

            var predictor = new Predictor( opts );

            var data_sents_raw = File.ReadLines( opts.InputTestFile );

            using var sw = new StreamWriter( opts.OutputFile, append: false );
            var n = 0;
#if DEBUG
            var po = new ParallelOptions() { MaxDegreeOfParallelism = 1 };
#else
            var po = new ParallelOptions() { MaxDegreeOfParallelism = 1 }; // Environment.ProcessorCount };
#endif
            var tokenizerConfig = new TokenizerConfig( opts.SentSplitterResourcesXmlFilename, opts.UrlDetectorResourcesXmlFilename );
            Parallel.ForEach( data_sents_raw, po,
            () => new Tokenizer( tokenizerConfig, replaceNumsOnPlaceholder: true ),
            (line, _, _, tokenizer) =>
            {
                if ( line.IsNullOrWhiteSpace() ) return (tokenizer);

                var words = tokenizer.Run_NoSentsAllocate( line );
                if ( words.Count <= 0 ) return (tokenizer);

                var input_tokens = Tokenizer.ToNerInputTokens( words, opts.ModelUpperCase );
                var (output_words, clsInfo) = predictor.Predict_2( input_tokens );

                Debug.WriteLine( $"WordsInDictRatio: {clsInfo.WordsInDictRatio}" );
                foreach ( var wc in clsInfo.WordClasses )
                {
                    Debug.WriteLine( $"'{wc.Word}' => {string.Join(", ", wc.Classes.Select( c => $"{c.ClassName} ({c.Probability:F4})" ) )}" );
                }

                #region comm. [.output to console & file - v1.]
                /*
                lock ( sw )
                {
                    sw.WriteLine( $"{++n})." ); Console.WriteLine( $"{n})." );

                    if ( output_words.Count <= words.Count )
                    {
                        Span< int > max_lens = stackalloc int[ output_words.Count ];

                        var len = output_words.Count - 1;
                        for ( var i = 0; i <= len; i++ )
                        {
                            max_lens[ i ] = Math.Max( words[ i ].valueOriginal.Length, output_words[ i ].Length ) + 1;
                        }

                        for ( var i = 0; i <= len; i++ )
                        {
                            var s = words[ i ].valueOriginal.PadRight( max_lens[ i ] );
                            sw.Write( s ); Console.Write( s );
                        }
                        sw.WriteLine(); Console.WriteLine();

                        for ( var i = 0; i <= len; i++ )
                        {
                            var s = output_words[ i ];
                            if ( s.Length == 1 && s[ 0 ] == 'O' ) s = "-";
                            s = s.PadRight( max_lens[ i ] );
                            sw.Write( s ); Console.Write( s );
                        }
                        sw.WriteLine(); Console.WriteLine();
                    }
                    else
                    {
                        sw.WriteLine( string.Join( " ", words.Select( w => w.valueOriginal ) ) ); Console.WriteLine( string.Join( " ", words.Select( w => w.valueOriginal ) ) );
                        sw.WriteLine( string.Join( " ", output_words  ) ); Console.WriteLine( string.Join( " ", output_words  ) );
                    }
                    sw.WriteLine(); Console.WriteLine();
                    sw.Flush();
                }
                //*/
                #endregion

                words.SetNNerOutputType( output_words );
                NerPostMerger.Run_Merge( words, opts.ModelUpperCase );

                #region [.output to console & file - v2.]
                lock ( sw )
                {
                    sw.WriteLine( $"{++n})." ); Console.WriteLine( $"{n})." );
                    { 
                        Span< int > max_lens = stackalloc int[ words.Count ];

                        var len = words.Count - 1;
                        for ( var i = 0; i <= len; i++ )
                        {
                            var w = words[ i ];
                            max_lens[ i ] = Math.Max( w.valueOriginal.Length, w.nerOutputType.ToText().Length ) + 1;
                        }

                        for ( var i = 0; i <= len; i++ )
                        {
                            var s = words[ i ].valueOriginal.PadRight( max_lens[ i ] );
                            sw.Write( s ); Console.Write( s );
                        }
                        sw.WriteLine(); Console.WriteLine();

                        for ( var i = 0; i <= len; i++ )
                        {
                            var w = words[ i ];
                            var s = w.nerOutputType.ToText();
                            //if ( w.nerOutputType == NerOutputType.Other ) s = "-";
                            //s = s.PadRight( max_lens[ i ] );
                            if ( w.nerOutputType == NerOutputType.Other ) s = new string( ' ', max_lens[ i ] );
                            else s = s.PadRight( max_lens[ i ] - 1, '-' ) + ' ';
                            sw.Write( s ); Console.Write( s );
                        }
                        sw.WriteLine(); Console.WriteLine();
                    }
                    sw.WriteLine(); Console.WriteLine();
                    sw.Flush();
                }
                #endregion

                return (tokenizer);
            },
            (tokenizer) => tokenizer.Dispose()
            );
        }

    }
}
