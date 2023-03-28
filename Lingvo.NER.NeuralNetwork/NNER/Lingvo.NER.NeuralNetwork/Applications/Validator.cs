using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Lingvo.NER.NeuralNetwork.Applications;
using Lingvo.NER.NeuralNetwork.Metrics;
using Lingvo.NER.NeuralNetwork.Text;
using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork
{
    /// <summary>
    /// 
    /// </summary>
    public static class Validator
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly struct MetricInfo
        {
            public string MetricName { get; init; }
            public double Score      { get; init; }
            public string Text       { get; init; }
            public override string ToString() => $"{MetricName}, Score={Score:F4}, {Text}";
        }
        /// <summary>
        /// 
        /// </summary>
        public readonly struct Result
        {
            public IReadOnlyList< MetricInfo > MetricInfos { get; init; }
            public override string ToString() => (MetricInfos.AnyEx() ? string.Join("; ", MetricInfos ) : "-");
        }

        public static Result Run_Validate( Options opts )
        {
            Logger.WriteLine( $"Evaluate model '{opts.ModelFilePath}' by valid corpus '{opts.ValidCorpusPath}'" );

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                Console.WriteLine( "\r\n cancel operation..." );
                cts.Cancel_NoThrow( e );
            };

            // Load valid corpus
            using var validCorpus = new Corpus( opts.ValidCorpusPath, opts.BatchSize, opts.ShuffleBlockSize, opts.MaxPredictSentLength, opts.ShuffleType, opts.TooLongSequence, cts.Token );

            // Create metrics
            var metrics = CreateFromTgtVocab_MultiLabelsFscoreMetric( validCorpus );

            var sl = SeqLabel.Create4Predict( opts );
            sl.Validate( validCorpus, metrics );

            var mis = (from m in metrics
                          let t = m.GetScore()
                          select new MetricInfo()
                          {
                             MetricName = m.Name,
                             Score      = t.primaryScore,
                             Text       = t.text
                          }
                         ).ToList();
            return (new Result() { MetricInfos = mis });
        }

        public static IList< IMetric > CreateFromTgtVocab_SeqLabelFscoreMetric( Corpus corpus, bool vocabIgnoreCase = false )
        {
            var tgtVocab = corpus.BuildTargetVocab( vocabIgnoreCase );
            return (CreateFromTgtVocab_SeqLabelFscoreMetric( tgtVocab ));
        }
        public static IList< IMetric > CreateFromTgtVocab_SeqLabelFscoreMetric( Vocab tgtVocab )
        {
            var metrics = (from word in tgtVocab.Items.OrderBy( NerTagToOrderPos )
                           where !BuildInTokens.IsPreDefinedToken( word )
                           select (IMetric) new SeqLabelFscoreMetric( word )
                          ).ToList( tgtVocab.Items.Count );
            return (metrics);
        }
        public static IList< IMetric > CreateFromTgtVocab_MultiLabelsFscoreMetric( Corpus corpus, bool vocabIgnoreCase = false )
        {
            var tgtVocab = corpus.BuildTargetVocab( vocabIgnoreCase );
            return (CreateFromTgtVocab_MultiLabelsFscoreMetric( tgtVocab ));
        }
        public static IList< IMetric > CreateFromTgtVocab_MultiLabelsFscoreMetric( Vocab tgtVocab )
        {
            var nerLabels = from word in tgtVocab.Items.OrderBy( NerTagToOrderPos )
                            where !BuildInTokens.IsPreDefinedToken( word ) && (word != "O")
                            select word;
            var metrics = new[]
            {
                new MultiLabelsFscoreMetric( nerLabels, "all" ),
                new MultiLabelsFscoreMetric( nerLabels.Where( w => (w == "B-PER") || (w == "I-PER") ), "PER" )
            };
            return (metrics);
        }

        public static int NerTagToOrderPos( string nerTag )
        {
            switch ( nerTag )
            {
                case "B-PER":  return (0);
                case "I-PER":  return (1);
                case "B-LOC":  return (2);
                case "I-LOC":  return (3);
                case "B-ORG":  return (4);
                case "I-ORG":  return (5);
                case "B-MISC": return (6);
                case "I-MISC": return (7);
                default: /* "O" */ return (int.MaxValue); 
            }
        }

        public static void Cancel_NoThrow( this CancellationTokenSource cts, ConsoleCancelEventArgs e = null )
        {
            if ( e != null ) e.Cancel = true;
            try { cts.Cancel(); } catch {; }
        }
    }
}

