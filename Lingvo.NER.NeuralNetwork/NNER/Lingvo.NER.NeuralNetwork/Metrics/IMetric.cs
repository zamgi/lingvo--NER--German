using System.Collections.Generic;

namespace Lingvo.NER.NeuralNetwork.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMetric
    {
        void Evaluate( List<List<string>> refTokens, List<string> hypTokens );
        void ClearStatus();
        string Name { get; }
        string GetScoreStr();
        double GetPrimaryScore();
        (double primaryScore, string text) GetScore();
    }
}
