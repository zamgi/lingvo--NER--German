using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Layers;
using Lingvo.NER.NeuralNetwork.Models;

namespace Lingvo.NER.NeuralNetwork.Networks
{
    /// <summary>
    /// 
    /// </summary>

    public class LSTMEncoder
    {
        public List<LSTMCell> Encoders = new List<LSTMCell>();
        public int Hdim { get; set; }
        public int Dim { get; set; }
        public int Depth { get; set; }

        public LSTMEncoder( string name, int hdim, int dim, int depth, int deviceId, bool isTrainable )
        {
            Encoders.Add( new LSTMCell( $"{name}.LSTM_0", hdim, dim, deviceId, isTrainable ) );

            for ( int i = 1; i < depth; i++ )
            {
                Encoders.Add( new LSTMCell( $"{name}.LSTM_{i}", hdim, hdim, deviceId, isTrainable ) );
            }
            this.Hdim = hdim;
            this.Dim = dim;
            this.Depth = depth;
        }

        public void Reset( WeightTensorFactory weightFactory, int batchSize )
        {
            foreach ( LSTMCell e in Encoders )
            {
                e.Reset( weightFactory, batchSize );
            }
        }

        public WeightTensor Encode( WeightTensor V, ComputeGraphTensor g )
        {
            foreach ( LSTMCell encoder in Encoders )
            {
                WeightTensor e = encoder.Step( V, g );
                V = e;
            }
            return V;
        }

        public List<WeightTensor> GetParams()
        {
            var response = new List<WeightTensor>( Encoders.Count );
            foreach ( LSTMCell e in Encoders )
            {
                response.AddRange( e.GetParams() );
            }
            return (response);
        }
        public void Save( Model model )
        {
            foreach ( LSTMCell e in Encoders )
            {
                e.Save( model );
            }
        }
        public void Load( Model model )
        {
            foreach ( LSTMCell e in Encoders )
            {
                e.Load( model );
            }
        }
    }
}
