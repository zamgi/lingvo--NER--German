using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Models;
using Lingvo.NER.NeuralNetwork.Networks;

namespace Lingvo.NER.NeuralNetwork.Layers
{
    /// <summary>
    /// 
    /// </summary>
    public class AttentionPreProcessResult
    {
        public WeightTensor Uhs;
        public WeightTensor encOutput;
    }
    /// <summary>
    /// 
    /// </summary>

    public class AttentionUnit : INeuralUnit
    {
        //WARNING!!! - DON'T RENAME ANY FIELD'S!!!

        private readonly WeightTensor m_V;
        private readonly WeightTensor m_Ua;
        private readonly WeightTensor m_bUa;
        private readonly WeightTensor m_Wa;
        private readonly WeightTensor m_bWa;

        private readonly string m_name;
        private readonly int m_hiddenDim;
        private readonly int m_contextDim;
        private readonly int m_deviceId;
        private readonly bool m_isTrainable;

        private bool m_enableCoverageModel;
        private readonly WeightTensor m_Wc;
        private readonly WeightTensor m_bWc;
        private readonly LSTMCell m_coverage;

        private readonly int k_coverageModelDim = 16;

        public AttentionUnit( string name, int hiddenDim, int contextDim, int deviceId, bool enableCoverageModel, bool isTrainable )
        {
            m_name                = name;
            m_hiddenDim           = hiddenDim;
            m_contextDim          = contextDim;
            m_deviceId            = deviceId;
            m_enableCoverageModel = enableCoverageModel;
            m_isTrainable         = isTrainable;

            //---Logger.WriteLine( $"Creating attention unit '{name}' HiddenDim = '{hiddenDim}', ContextDim = '{contextDim}', DeviceId = '{deviceId}', EnableCoverageModel = '{enableCoverageModel}'" );

            m_Ua  = new WeightTensor( new long[ 2 ] { contextDim, hiddenDim }, deviceId, normType: NormType.Uniform, name: $"{name}.{nameof(m_Ua )}", isTrainable: isTrainable );
            m_Wa  = new WeightTensor( new long[ 2 ] { hiddenDim, hiddenDim }, deviceId, normType: NormType.Uniform, name: $"{name}.{nameof(m_Wa )}", isTrainable: isTrainable );
            m_bUa = new WeightTensor( new long[ 2 ] { 1, hiddenDim }, 0, deviceId, name: $"{name}.{nameof(m_bUa )}", isTrainable: isTrainable );
            m_bWa = new WeightTensor( new long[ 2 ] { 1, hiddenDim }, 0, deviceId, name: $"{name}.{nameof(m_bWa )}", isTrainable: isTrainable );
            m_V   = new WeightTensor( new long[ 2 ] { hiddenDim, 1 }, deviceId, normType: NormType.Uniform, name: $"{name}.{nameof(m_V )}", isTrainable: isTrainable );

            if ( m_enableCoverageModel )
            {
                m_Wc       = new WeightTensor( new long[ 2 ] { k_coverageModelDim, hiddenDim }, deviceId, normType: NormType.Uniform, name: $"{name}.{nameof(m_Wc )}", isTrainable: isTrainable );
                m_bWc      = new WeightTensor( new long[ 2 ] { 1, hiddenDim }, 0, deviceId, name: $"{name}.{nameof(m_bWc )}", isTrainable: isTrainable );
                m_coverage = new LSTMCell( name: $"{name}.{nameof(m_coverage )}", hdim: k_coverageModelDim, dim: 1 + contextDim + hiddenDim, deviceId: deviceId, isTrainable: isTrainable );
            }
        }

        public int GetDeviceId() => m_deviceId;

        public AttentionPreProcessResult PreProcess( WeightTensor encOutput, int batchSize, ComputeGraphTensor g )
        {
            int srcSeqLen = encOutput.Rows / batchSize;

            var r = new AttentionPreProcessResult() { encOutput = encOutput };

            r.Uhs = g.Affine( r.encOutput, m_Ua, m_bUa );
            r.Uhs = g.View( r.Uhs, dims: new long[] { batchSize, srcSeqLen, -1 } );

            if ( m_enableCoverageModel )
            {
                m_coverage.Reset( g.GetWeightFactory(), r.encOutput.Rows );
            }

            return (r);
        }

        public WeightTensor Perform( WeightTensor state, AttentionPreProcessResult attnPre, int batchSize, ComputeGraphTensor graph )
        {
            int srcSeqLen = attnPre.encOutput.Rows / batchSize;

            using ( ComputeGraphTensor g = graph.CreateSubGraph( m_name ) )
            {
                // Affine decoder state
                WeightTensor wc = g.Affine( state, m_Wa, m_bWa );

                // Expand dims from [batchSize x decoder_dim] to [batchSize x srcSeqLen x decoder_dim]
                WeightTensor wc1 = g.View( wc, dims: new long[] { batchSize, 1, wc.Columns } );
                WeightTensor wcExp = g.Expand( wc1, dims: new long[] { batchSize, srcSeqLen, wc.Columns } );

                WeightTensor ggs = null;
                if ( m_enableCoverageModel )
                {
                    // Get coverage model status at {t-1}
                    WeightTensor wCoverage = g.Affine( m_coverage.Hidden, m_Wc, m_bWc );
                    WeightTensor wCoverage1 = g.View( wCoverage, dims: new long[] { batchSize, srcSeqLen, -1 } );

                    ggs = g.AddTanh( attnPre.Uhs, wcExp, wCoverage1 );
                }
                else
                {
                    ggs = g.AddTanh( attnPre.Uhs, wcExp );
                }

                WeightTensor ggss = g.View( ggs, dims: new long[] { batchSize * srcSeqLen, -1 } );
                WeightTensor atten = g.Mul( ggss, m_V );

                WeightTensor attenT = g.Transpose( atten );
                WeightTensor attenT2 = g.View( attenT, dims: new long[] { batchSize, srcSeqLen } );

                WeightTensor attenSoftmax1 = g.Softmax( attenT2, inPlace: true );

                WeightTensor attenSoftmax = g.View( attenSoftmax1, dims: new long[] { batchSize, 1, srcSeqLen } );
                WeightTensor inputs2 = g.View( attnPre.encOutput, dims: new long[] { batchSize, srcSeqLen, attnPre.encOutput.Columns } );

                WeightTensor contexts = graph.MulBatch( attenSoftmax, inputs2 );

                contexts = graph.View( contexts, dims: new long[] { batchSize, attnPre.encOutput.Columns } );

                if ( m_enableCoverageModel )
                {
                    // Concatenate tensor as input for coverage model
                    WeightTensor aCoverage = g.View( attenSoftmax1, dims: new long[] { attnPre.encOutput.Rows, 1 } );

                    WeightTensor state2 = g.View( state, dims: new long[] { batchSize, 1, state.Columns } );
                    WeightTensor state3 = g.Expand( state2, dims: new long[] { batchSize, srcSeqLen, state.Columns } );
                    WeightTensor state4 = g.View( state3, dims: new long[] { batchSize * srcSeqLen, -1 } );

                    WeightTensor concate = g.Concate( 1, aCoverage, attnPre.encOutput, state4 );
                    m_coverage.Step( concate, graph );
                }

                return (contexts);
            }
        }

        public virtual List<WeightTensor> GetParams()
        {
            var response = new List<WeightTensor>
            {
                m_Ua,
                m_Wa,
                m_bUa,
                m_bWa,
                m_V
            };

            if ( m_enableCoverageModel )
            {
                response.Add( m_Wc );
                response.Add( m_bWc );
                response.AddRange( m_coverage.GetParams() );
            }

            return (response);
        }

        public void Save( Model model )
        {
            m_Ua.Save( model );
            m_Wa.Save( model );
            m_bUa.Save( model );
            m_bWa.Save( model );
            m_V.Save( model );

            if ( m_enableCoverageModel )
            {
                m_Wc.Save( model );
                m_bWc.Save( model );
                m_coverage.Save( model );
            }
        }
        public void Load( Model model )
        {
            m_Ua.Load( model );
            m_Wa.Load( model );
            m_bUa.Load( model );
            m_bWa.Load( model );
            m_V.Load( model );

            if ( m_enableCoverageModel )
            {
                m_Wc.Load( model );
                m_bWc.Load( model );
                m_coverage.Load( model );
            }
        }

        public INeuralUnit CloneToDeviceAt( int deviceId ) => new AttentionUnit( m_name, m_hiddenDim, m_contextDim, deviceId, m_enableCoverageModel, m_isTrainable );
    }
}



