using Lingvo.NER.NeuralNetwork.Networks;

namespace Lingvo.NER.NeuralNetwork.Layers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IFeedForwardLayer : INeuralUnit
    {
        WeightTensor Process( WeightTensor inputT, int batchSize, ComputeGraphTensor g, float alpha = 1.0f );
    }
}
