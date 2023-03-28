using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Models;
using Lingvo.NER.NeuralNetwork.Networks;

namespace Lingvo.NER.NeuralNetwork.Layers
{
    /// <summary>
    /// 
    /// </summary>
    public class LSTMCell
    {
        //WARNING!!! - DON'T RENAME ANY FIELD'S!!!

        private readonly WeightTensor m_Wxh;
        private readonly WeightTensor m_b;
        private WeightTensor m_hidden;
        private WeightTensor m_cell;
        private readonly int m_hdim;
        private readonly int m_dim;
        private readonly int m_deviceId;
        private readonly string m_name;
        private readonly LayerNormalization m_layerNorm1;
        private readonly LayerNormalization m_layerNorm2;

        public WeightTensor Hidden => m_hidden;

        public LSTMCell( string name, int hdim, int dim, int deviceId, bool isTrainable )
        {
            m_name = name;

            m_Wxh = new WeightTensor( new long[ 2 ] { dim + hdim, hdim * 4 }, deviceId, normType: NormType.Uniform, name: $"{name}.{nameof(m_Wxh )}", isTrainable: isTrainable );
            m_b   = new WeightTensor( new long[ 2 ] { 1, hdim * 4 }, 0, deviceId, name: $"{name}.{nameof(m_b )}", isTrainable: isTrainable );

            m_hdim     = hdim;
            m_dim      = dim;
            m_deviceId = deviceId;

            m_layerNorm1 = new LayerNormalization( $"{name}.{nameof(m_layerNorm1 )}", hdim * 4, deviceId, isTrainable: isTrainable );
            m_layerNorm2 = new LayerNormalization( $"{name}.{nameof(m_layerNorm2 )}", hdim, deviceId, isTrainable: isTrainable );
        }

        public WeightTensor Step( WeightTensor input, ComputeGraphTensor g )
        {
            using ( ComputeGraphTensor innerGraph = g.CreateSubGraph( m_name ) )
            {
                WeightTensor hidden_prev = m_hidden;
                WeightTensor cell_prev = m_cell;

                WeightTensor inputs = innerGraph.Concate( 1, input, hidden_prev );
                WeightTensor hhSum = innerGraph.Affine( inputs, m_Wxh, m_b );
                WeightTensor hhSum2 = m_layerNorm1.Norm( hhSum, innerGraph );

                (WeightTensor gates_raw, WeightTensor cell_write_raw) = innerGraph.SplitColumns( hhSum2, m_hdim * 3, m_hdim );
                WeightTensor gates = innerGraph.Sigmoid( gates_raw );
                WeightTensor cell_write = innerGraph.Tanh( cell_write_raw );

                (WeightTensor input_gate, WeightTensor forget_gate, WeightTensor output_gate) = innerGraph.SplitColumns( gates, m_hdim, m_hdim, m_hdim );

                // compute new cell activation: ct = forget_gate * cell_prev + input_gate * cell_write
                m_cell = g.EltMulMulAdd( forget_gate, cell_prev, input_gate, cell_write );
                WeightTensor ct2 = m_layerNorm2.Norm( m_cell, innerGraph );

                // compute hidden state as gated, saturated cell activations
                m_hidden = g.EltMul( output_gate, innerGraph.Tanh( ct2 ) );

                return (m_hidden);
            }
        }

        public virtual List<WeightTensor> GetParams()
        {
            var response = new List<WeightTensor> { m_Wxh, m_b };

            response.AddRange( m_layerNorm1.GetParams() );
            response.AddRange( m_layerNorm2.GetParams() );

            return (response);
        }

        public void Reset( WeightTensorFactory weightFactory, int batchSize )
        {
            if ( m_hidden != null )
            {
                m_hidden.Dispose();
                m_hidden = null;
            }
            if ( m_cell != null )
            {
                m_cell.Dispose();
                m_cell = null;
            }

            m_hidden = weightFactory.CreateWeightTensor( batchSize, m_hdim, m_deviceId, true, name: $"{m_name}.{nameof(m_hidden )}", isTrainable: true );
            m_cell   = weightFactory.CreateWeightTensor( batchSize, m_hdim, m_deviceId, true, name: $"{m_name}.{nameof(m_cell )}", isTrainable: true );
        }

        public void Save( Model model )
        {
            m_Wxh.Save( model );
            m_b.Save( model );

            m_layerNorm1.Save( model );
            m_layerNorm2.Save( model );
        }
        public void Load( Model model )
        {
            m_Wxh.Load( model );
            m_b.Load( model );

            m_layerNorm1.Load( model );
            m_layerNorm2.Load( model );
        }
    }
}
