using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Models;
using Lingvo.NER.NeuralNetwork.Networks;

namespace Lingvo.NER.NeuralNetwork.Layers
{
    /// <summary>
    /// 
    /// </summary>
    public interface INeuralUnit
    {
        List< WeightTensor > GetParams();
        void Save( Model model );
        void Load( Model model );

        INeuralUnit CloneToDeviceAt( int deviceId );
        int GetDeviceId();
    }
}
