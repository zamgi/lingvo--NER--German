using Lingvo.NER.NeuralNetwork.Layers;

namespace Lingvo.NER.NeuralNetwork.Networks
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEncoder : INeuralUnit
    {
        WeightTensor Encode( WeightTensor rawInput, int batchSize, ComputeGraphTensor g, WeightTensor srcSelfMask );
        void Reset( WeightTensorFactory weightFactory, int batchSize );
    }
}
