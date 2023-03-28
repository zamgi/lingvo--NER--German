using System;
using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Metrics;

namespace Lingvo.NER.NeuralNetwork
{
    /// <summary>
    /// 
    /// </summary>
    public class CostEventArg : EventArgs
    {
        public double AvgCostInTotal         { get; set; }
        public int Epoch                     { get; set; }
        public int Update                    { get; set; }
        public int ProcessedSentencesInTotal { get; set; }
        public long ProcessedWordsInTotal    { get; set; }        
        public float LearningRate            { get; set; }
        public DateTime StartDateTime        { get; set; }
        public DateTime LastCallStatusUpdateWatcherDateTime { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class EvaluationEventArg : EventArgs
    {
        public string          Message     { get; set; }
        public ConsoleColor    Color       { get; set; }
        public string          Title       { get; set; }
        public List< IMetric > Metrics     { get; set; }
        public bool            BetterModel { get; set; }
    }
}
