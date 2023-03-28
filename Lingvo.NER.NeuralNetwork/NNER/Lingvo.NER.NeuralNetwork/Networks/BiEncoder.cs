﻿using System.Collections.Generic;
using System.Linq;

using Lingvo.NER.NeuralNetwork.Layers;
using Lingvo.NER.NeuralNetwork.Models;
using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork.Networks
{
    /// <summary>
    /// 
    /// </summary>

    public class BiEncoder : IEncoder
    {
        private readonly List<LSTMCell> m_forwardEncoders;
        private readonly List<LSTMCell> m_backwardEncoders;
        private readonly string m_name;
        private readonly int m_hiddenDim;
        private readonly int m_inputDim;
        private readonly int m_depth;
        private readonly int m_deviceId;
        private readonly bool m_isTrainable;

        public BiEncoder( string name, int hiddenDim, int inputDim, int depth, int deviceId, bool isTrainable )
        {
            Logger.WriteLine( $"Creating BiLSTM encoder at device '{deviceId}'. HiddenDim = '{hiddenDim}', InputDim = '{inputDim}', Depth = '{depth}', IsTrainable = '{isTrainable}'" );

            m_forwardEncoders  = new List<LSTMCell>();
            m_backwardEncoders = new List<LSTMCell>();

            m_forwardEncoders.Add( new LSTMCell( $"{name}.Forward_LSTM_0", hiddenDim, inputDim, deviceId, isTrainable: isTrainable ) );
            m_backwardEncoders.Add( new LSTMCell( $"{name}.Backward_LSTM_0", hiddenDim, inputDim, deviceId, isTrainable: isTrainable ) );

            for ( int i = 1; i < depth; i++ )
            {
                m_forwardEncoders.Add( new LSTMCell( $"{name}.Forward_LSTM_{i}", hiddenDim, hiddenDim * 2, deviceId, isTrainable: isTrainable ) );
                m_backwardEncoders.Add( new LSTMCell( $"{name}.Backward_LSTM_{i}", hiddenDim, hiddenDim * 2, deviceId, isTrainable: isTrainable ) );
            }

            m_name        = name;
            m_hiddenDim   = hiddenDim;
            m_inputDim    = inputDim;
            m_depth       = depth;
            m_deviceId    = deviceId;
            m_isTrainable = isTrainable;
        }

        public int GetDeviceId() => m_deviceId;
        public INeuralUnit CloneToDeviceAt( int deviceId ) => new BiEncoder( m_name, m_hiddenDim, m_inputDim, m_depth, deviceId, m_isTrainable );

        public void Reset( WeightTensorFactory weightFactory, int batchSize )
        {
            foreach ( var c in m_forwardEncoders )
            {
                c.Reset( weightFactory, batchSize );
            }
            foreach ( var c in m_backwardEncoders )
            {
                c.Reset( weightFactory, batchSize );
            }
        }

        public WeightTensor Encode( WeightTensor rawInputs, int batchSize, ComputeGraphTensor g, WeightTensor srcSelfMask )
        {
            int seqLen = rawInputs.Rows / batchSize;

            rawInputs = g.TransposeBatch( rawInputs, seqLen );

            var inputs = new List<WeightTensor>( seqLen );
            for ( int i = 0; i < seqLen; i++ )
            {
                WeightTensor emb_i = g.Peek( rawInputs, 0, i * batchSize, batchSize );
                inputs.Add( emb_i );
            }

            var forwardOutputs  = new List<WeightTensor>( m_depth * seqLen );
            var backwardOutputs = new List<WeightTensor>( m_depth * seqLen );

            List<WeightTensor> layerOutputs = inputs.ToList();
            for ( int i = 0; i < m_depth; i++ )
            {
                for ( int j = 0; j < seqLen; j++ )
                {
                    WeightTensor forwardOutput = m_forwardEncoders[ i ].Step( layerOutputs[ j ], g );
                    forwardOutputs.Add( forwardOutput );

                    WeightTensor backwardOutput = m_backwardEncoders[ i ].Step( layerOutputs[ inputs.Count - j - 1 ], g );
                    backwardOutputs.Add( backwardOutput );
                }

                backwardOutputs.Reverse();
                layerOutputs.Clear();
                for ( int j = 0; j < seqLen; j++ )
                {
                    WeightTensor concatW = g.Concate( 1, forwardOutputs[ j ], backwardOutputs[ j ] );
                    layerOutputs.Add( concatW );
                }

            }

            var result = g.Concate( layerOutputs, 0 );

            return g.TransposeBatch( result, batchSize );
        }

        public List<WeightTensor> GetParams()
        {
            var response = new List<WeightTensor>( m_forwardEncoders.Count * 10 + m_backwardEncoders.Count * 10 );
            foreach ( var c in m_forwardEncoders )
            {
                response.AddRange( c.GetParams() );
            }
            foreach ( var c in m_backwardEncoders )
            {
                response.AddRange( c.GetParams() );
            }
            return (response);
        }
        public void Save( Model model )
        {
            foreach ( var c in m_forwardEncoders )
            {
                c.Save( model );
            }
            foreach ( var c in m_backwardEncoders )
            {
                c.Save( model );
            }
        }
        public void Load( Model model )
        {
            foreach ( var c in m_forwardEncoders )
            {
                c.Load( model );
            }
            foreach ( var c in m_backwardEncoders )
            {
                c.Load( model );
            }
        }
    }
}
