using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Models;
using Lingvo.NER.NeuralNetwork.Networks;

namespace Lingvo.NER.NeuralNetwork.Layers
{
    /// <summary>
    /// 
    /// </summary>
    internal class FeedForwardLayer : IFeedForwardLayer
    {
        //WARNING!!! - DON'T RENAME ANY FIELD'S!!!

        private readonly WeightTensor m_Whd;
        private readonly WeightTensor m_Bd;
        private readonly string m_name;
        private readonly float m_dropoutRatio;
        private readonly int m_inputDim;
        private readonly int m_outputDim;
        private readonly int m_deviceId;
        private readonly bool m_isTrainable;

        public FeedForwardLayer( string name, int inputDim, int outputDim, float dropoutRatio, int deviceId, bool isTrainable, float learningRateFactor = 1.0f )
        {
            //---Logger.WriteLine( $"Create feed forward layer '{name}' InputDim = '{inputDim}', OutputDim = '{outputDim}', DropoutRatio = '{dropoutRatio}', DeviceId = '{deviceId}'" );

            m_name         = name;
            m_inputDim     = inputDim;
            m_outputDim    = outputDim;
            m_dropoutRatio = dropoutRatio;
            m_deviceId     = deviceId;
            m_isTrainable  = isTrainable;

            m_Whd = new WeightTensor( new long[ 2 ] { inputDim, outputDim },    deviceId, name: $"{name}.{nameof(m_Whd )}", normType: NormType.Uniform, isTrainable: isTrainable, learningRateFactor: learningRateFactor );
            m_Bd  = new WeightTensor( new long[ 2 ] {        1, outputDim }, 0, deviceId, name: $"{name}.{nameof(m_Bd )}" ,                             isTrainable: isTrainable, learningRateFactor: learningRateFactor );
        }

        public int GetDeviceId() => m_deviceId;
        public WeightTensor Process( WeightTensor input, int batchSize, ComputeGraphTensor g, float alpha = 1.0f )
        {
            WeightTensor res = g.Affine( input, m_Whd, m_Bd, alpha );
            var output = g.Dropout( res, batchSize, m_dropoutRatio, inPlace: true );
            return (output);
        }

        public virtual List<WeightTensor> GetParams() => new List<WeightTensor> { m_Whd, m_Bd };
        public void Save( Model model )
        {
            m_Whd.Save( model );
            m_Bd.Save( model );
        }
        public void Load( Model model )
        {
            m_Whd.Load( model );
            m_Bd.Load( model );
        }

        public INeuralUnit CloneToDeviceAt( int deviceId ) => new FeedForwardLayer( m_name, m_inputDim, m_outputDim, m_dropoutRatio, deviceId, m_isTrainable );
    }
}
