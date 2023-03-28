using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Networks;

namespace Lingvo.NER.NeuralNetwork.Optimizer
{
    /// <summary>
    /// 
    /// </summary>
    public interface IOptimizer
    {
        void UpdateWeights( List<WeightTensor> model, int batchSize, float step_size, float regc, int iter );
    }
}
