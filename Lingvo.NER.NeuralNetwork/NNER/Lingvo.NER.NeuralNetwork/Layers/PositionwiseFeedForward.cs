using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Models;
using Lingvo.NER.NeuralNetwork.Networks;

namespace Lingvo.NER.NeuralNetwork.Layers
{
    /// <summary>
    /// 
    /// </summary>
    internal class PositionwiseFeedForward
    {
        //WARNING!!! - DON'T RENAME ANY FIELD'S!!!

        private readonly LayerNormalization layerNorm2;
        private readonly FeedForwardLayer feedForwardLayer1;
        private readonly FeedForwardLayer feedForwardLayer2;

        private readonly string m_name;
        private readonly float  m_dropoutRatio;

        public PositionwiseFeedForward( string name, int hiddenDim, float dropoutRatio, int deviceId, bool isTrainable, float learningRateFactor = 1.0f )
        {
            m_name         = name;
            m_dropoutRatio = dropoutRatio;

            layerNorm2        = new LayerNormalization( $"{name}.{nameof(layerNorm2)}", hiddenDim, deviceId, isTrainable, learningRateFactor: learningRateFactor );
            feedForwardLayer1 = new FeedForwardLayer( $"{name}.{nameof(feedForwardLayer1)}", hiddenDim, hiddenDim * 4, m_dropoutRatio, deviceId, isTrainable, learningRateFactor: learningRateFactor );
            feedForwardLayer2 = new FeedForwardLayer( $"{name}.{nameof(feedForwardLayer2)}", hiddenDim * 4, hiddenDim, m_dropoutRatio, deviceId, isTrainable, learningRateFactor: learningRateFactor );
        }

        public WeightTensor Perform( WeightTensor input, int batchSize, ComputeGraphTensor graph )
        {
            using ComputeGraphTensor g = graph.CreateSubGraph( $"{m_name}_PositionwiseFeedForward" );
            var inputNorm = layerNorm2.Norm( input, g );

            //Feed forward
            WeightTensor ffnResult = feedForwardLayer1.Process( inputNorm, batchSize, g );
            WeightTensor reluFFNResult = g.Relu( ffnResult, inPlace: true );
            WeightTensor ffn2Result = feedForwardLayer2.Process( reluFFNResult, batchSize, g );

            //Skip connection and layer normaliztion
            WeightTensor addFFNResult = graph.Add( ffn2Result, input, inPlace: true );

            return (addFFNResult);
        }

        public virtual List<WeightTensor> GetParams()
        {
            var response = new List<WeightTensor>();

            response.AddRange( layerNorm2.GetParams() );
            response.AddRange( feedForwardLayer1.GetParams() );
            response.AddRange( feedForwardLayer2.GetParams() );

            return (response);
        }

        public void Save( Model model )
        {
            layerNorm2.Save( model );
            feedForwardLayer1.Save( model );
            feedForwardLayer2.Save( model );
        }
        public void Load( Model model )
        {
            layerNorm2.Load( model );
            feedForwardLayer1.Load( model );
            feedForwardLayer2.Load( model );
        }
    }
}
