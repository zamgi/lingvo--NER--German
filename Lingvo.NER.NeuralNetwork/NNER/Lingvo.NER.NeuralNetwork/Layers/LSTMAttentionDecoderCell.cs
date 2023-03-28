using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Models;
using Lingvo.NER.NeuralNetwork.Networks;

namespace Lingvo.NER.NeuralNetwork.Layers
{
    /// <summary>
    /// 
    /// </summary>
    public class LSTMAttentionDecoderCell
    {
        //WARNING!!! - DON'T RENAME ANY FIELD'S!!!

        public WeightTensor Hidden { get; set; }
        public WeightTensor Cell { get; set; }

        private readonly int m_hiddenDim;
        private readonly int m_inputDim;
        private readonly int m_deviceId;
        private readonly string m_name;
        private readonly WeightTensor m_Wxhc;
        private readonly WeightTensor m_b;
        private readonly LayerNormalization m_layerNorm1;
        private readonly LayerNormalization m_layerNorm2;

        public LSTMAttentionDecoderCell( string name, int hiddenDim, int inputDim, int contextDim, int deviceId, bool isTrainable )
        {
            m_name      = name;
            m_hiddenDim = hiddenDim;
            m_inputDim  = inputDim;
            m_deviceId  = deviceId;

            //---Logger.WriteLine( $"Create LSTM attention decoder cell '{name}' HiddemDim = '{hiddenDim}', InputDim = '{inputDim}', ContextDim = '{contextDim}', DeviceId = '{deviceId}'" );

            m_Wxhc = new WeightTensor( new long[ 2 ] { inputDim + hiddenDim + contextDim, hiddenDim * 4 }, deviceId, normType: NormType.Uniform, name: $"{name}.{nameof(m_Wxhc)}", isTrainable: isTrainable );
            m_b    = new WeightTensor( new long[ 2 ] { 1, hiddenDim * 4 }, 0, deviceId, name: $"{name}.{nameof(m_b)}", isTrainable: isTrainable );

            m_layerNorm1 = new LayerNormalization( $"{name}.{nameof(m_layerNorm1)}", hiddenDim * 4, deviceId, isTrainable );
            m_layerNorm2 = new LayerNormalization( $"{name}.{nameof(m_layerNorm2)}", hiddenDim, deviceId, isTrainable );
        }

        /// <summary>
        /// Update LSTM-Attention cells according to given weights
        /// </summary>
        /// <param name="context">The context weights for attention</param>
        /// <param name="input">The input weights</param>
        /// <param name="computeGraph">The compute graph to build workflow</param>
        /// <returns>Update hidden weights</returns>
        public WeightTensor Step( WeightTensor context, WeightTensor input, ComputeGraphTensor g )
        {
            using ( ComputeGraphTensor computeGraph = g.CreateSubGraph( m_name ) )
            {
                WeightTensor cell_prev = Cell;
                WeightTensor hidden_prev = Hidden;

                WeightTensor hxhc = computeGraph.Concate( 1, input, hidden_prev, context );
                WeightTensor hhSum = computeGraph.Affine( hxhc, m_Wxhc, m_b );
                WeightTensor hhSum2 = m_layerNorm1.Norm( hhSum, computeGraph );

                (WeightTensor gates_raw, WeightTensor cell_write_raw) = computeGraph.SplitColumns( hhSum2, m_hiddenDim * 3, m_hiddenDim );
                WeightTensor gates = computeGraph.Sigmoid( gates_raw );
                WeightTensor cell_write = computeGraph.Tanh( cell_write_raw );

                (WeightTensor input_gate, WeightTensor forget_gate, WeightTensor output_gate) = computeGraph.SplitColumns( gates, m_hiddenDim, m_hiddenDim, m_hiddenDim );

                // compute new cell activation: ct = forget_gate * cell_prev + input_gate * cell_write
                Cell = g.EltMulMulAdd( forget_gate, cell_prev, input_gate, cell_write );
                WeightTensor ct2 = m_layerNorm2.Norm( Cell, computeGraph );

                Hidden = g.EltMul( output_gate, computeGraph.Tanh( ct2 ) );

                return Hidden;
            }
        }

        public List<WeightTensor> GetParams()
        {
            var response = new List<WeightTensor> { m_Wxhc, m_b };

            response.AddRange( m_layerNorm1.GetParams() );
            response.AddRange( m_layerNorm2.GetParams() );

            return (response);
        }

        public void Reset( WeightTensorFactory weightFactory, int batchSize )
        {
            Hidden = weightFactory.CreateWeightTensor( batchSize, m_hiddenDim, m_deviceId, true, name: $"{m_name}.{nameof(Hidden )}", isTrainable: true );
            Cell = weightFactory.CreateWeightTensor( batchSize, m_hiddenDim, m_deviceId, true, name: $"{m_name}.{nameof(Cell )}", isTrainable: true );
        }

        public void Save( Model model )
        {
            m_Wxhc.Save( model );
            m_b.Save( model );

            m_layerNorm1.Save( model );
            m_layerNorm2.Save( model );
        }
        public void Load( Model model )
        {
            m_Wxhc.Load( model );
            m_b.Load( model );

            m_layerNorm1.Load( model );
            m_layerNorm2.Load( model );
        }
    }
}


