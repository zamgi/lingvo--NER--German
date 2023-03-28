using System;
using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Models;
using Lingvo.NER.NeuralNetwork.Networks;

namespace Lingvo.NER.NeuralNetwork.Layers
{
    /// <summary>
    /// 
    /// </summary>
    internal class MultiHeadAttention
    {
        //WARNING!!! - DON'T RENAME ANY FIELD'S!!!

        private readonly WeightTensor W0;
        private readonly WeightTensor b0;

        private readonly WeightTensor Q;
        private readonly WeightTensor K;
        private readonly WeightTensor V;

        private readonly WeightTensor Qb;
        private readonly WeightTensor Kb;
        private readonly WeightTensor Vb;

        private readonly WeightTensor QKV;
        private readonly WeightTensor QKVb;

        private readonly LayerNormalization layerNormQ;

        private readonly int m_hiddenDim;
        private readonly int m_d;
        private readonly int m_multiHeadNum;
        private readonly string m_name;
        private readonly float m_dropoutRatio;

        private readonly bool m_sharedQKV;

        public MultiHeadAttention( string name, int multiHeadNum, int hiddenDim, int inputDim, float dropoutRatio, int deviceId, bool isTrainable, bool sharedQKV = false, float learningRateFactor = 1.0f )
        {
            m_name         = name;
            m_hiddenDim    = hiddenDim;
            m_multiHeadNum = multiHeadNum;
            m_d            = m_hiddenDim / m_multiHeadNum;
            m_dropoutRatio = dropoutRatio;
            m_sharedQKV    = sharedQKV;

            W0 = new WeightTensor( new long[ 2 ] { hiddenDim, hiddenDim }, deviceId, name: $"{name}.{nameof(W0 )}", isTrainable: isTrainable, normType: NormType.Uniform, learningRateFactor: learningRateFactor );
            b0 = new WeightTensor( new long[ 2 ] { 1, hiddenDim }, 0, deviceId, name: $"{name}.{nameof(b0 )}", isTrainable: isTrainable );

            if ( !m_sharedQKV )
            {
                Q  = new WeightTensor( new long[ 2 ] { inputDim, hiddenDim }, deviceId, name: $"{name}.{nameof(Q )}", isTrainable: isTrainable, normType: NormType.Uniform, learningRateFactor: learningRateFactor );
                Qb = new WeightTensor( new long[ 2 ] { 1, hiddenDim }, 0, deviceId, name: $"{name}.{nameof(Qb )}", isTrainable: isTrainable, learningRateFactor: learningRateFactor );

                K  = new WeightTensor( new long[ 2 ] { inputDim, hiddenDim }, deviceId, name: $"{name}.{nameof(K )}", isTrainable: isTrainable, normType: NormType.Uniform, learningRateFactor: learningRateFactor );
                Kb = new WeightTensor( new long[ 2 ] { 1, hiddenDim }, 0, deviceId, name: $"{name}.{nameof(Kb )}", isTrainable: isTrainable, learningRateFactor: learningRateFactor );

                V  = new WeightTensor( new long[ 2 ] { inputDim, hiddenDim }, deviceId, name: $"{name}.{nameof(V )}", isTrainable: isTrainable, normType: NormType.Uniform, learningRateFactor: learningRateFactor );
                Vb = new WeightTensor( new long[ 2 ] { 1, hiddenDim }, 0, deviceId, name: $"{name}.{nameof(Vb )}", isTrainable: isTrainable, learningRateFactor: learningRateFactor );
            }
            else
            {
                QKV  = new WeightTensor( new long[ 2 ] { inputDim, hiddenDim * 3 }, deviceId, name: $"{name}.{nameof(Q )}", isTrainable: isTrainable, normType: NormType.Uniform, learningRateFactor: learningRateFactor );
                QKVb = new WeightTensor( new long[ 2 ] { 1, hiddenDim * 3 }, 0, deviceId, name: $"{name}.{nameof(Qb )}", isTrainable: isTrainable, learningRateFactor: learningRateFactor );
            }

            layerNormQ = new LayerNormalization( $"{name}.{nameof(layerNormQ )}", m_hiddenDim, deviceId, isTrainable, learningRateFactor: learningRateFactor );
        }

        /// <summary>
        /// Scaled multi-heads attention component with skip connectioned feed forward layers
        /// </summary>
        /// <param name="inputQ">The input Q tensor</param>
        /// <param name="keyMask">The mask for softmax</param>
        /// <param name="batchSize">Batch size of input data set</param>
        /// <param name="graph">The instance of computing graph</param>
        /// <returns>Transformered output tensor</returns>
        public (WeightTensor, WeightTensor) Perform( WeightTensor inputQ, WeightTensor keyMask, int batchSize, ComputeGraphTensor graph, bool outputAttenWeights = false )
        {
            using ComputeGraphTensor g = graph.CreateSubGraph( $"{m_name}_MultiHeadAttention" );
            int seqLenQ = inputQ.Rows / batchSize;

            WeightTensor inputQNorm = layerNormQ.Norm( inputQ, g );

            //Input projections
            var weightedQKV = g.View( g.Affine( inputQNorm, QKV, QKVb ), dims: new long[] { batchSize, seqLenQ, 3, m_multiHeadNum, m_d } );
            var allQ = g.Select( weightedQKV, 2, 0 );
            var allK = g.Select( weightedQKV, 2, 1 );
            var allV = g.Select( weightedQKV, 2, 2 );


            //Multi-head attentions
            WeightTensor Qs = g.View( g.AsContiguous( g.Transpose( allQ, 1, 2 ) ), dims: new long[] { batchSize * m_multiHeadNum, seqLenQ, m_d } );
            WeightTensor Ks = g.View( g.AsContiguous( g.Transpose( g.Transpose( allK, 1, 2 ), 2, 3 ) ), dims: new long[] { batchSize * m_multiHeadNum, m_d, seqLenQ } );
            WeightTensor Vs = g.View( g.AsContiguous( g.Transpose( allV, 1, 2 ) ), dims: new long[] { batchSize * m_multiHeadNum, seqLenQ, m_d } );

            // Scaled softmax
            float scale = 1.0f / (float) (Math.Sqrt( m_d ));
            var   attn  = g.MulBatch( Qs, Ks, scale );
            attn = g.View( attn, dims: new long[] { batchSize, m_multiHeadNum, seqLenQ, seqLenQ } );

            if ( keyMask != null )
            {
                attn = g.Add( attn, keyMask, inPlace: true );
            }

            var attnProbs = g.Softmax( attn, inPlace: true );

            WeightTensor sumAttnWeights = null;
            if ( outputAttenWeights )
            {
                //Merge all attention probs over multi-heads
                sumAttnWeights = graph.Sum( attnProbs, 1 );
                sumAttnWeights = graph.Div( sumAttnWeights, (float) m_multiHeadNum );
                sumAttnWeights = graph.View( sumAttnWeights, new long[] { batchSize * seqLenQ, seqLenQ } );
            }

            attnProbs = g.View( attnProbs, dims: new long[] { batchSize * m_multiHeadNum, seqLenQ, seqLenQ } );

            WeightTensor o = g.View( g.MulBatch( attnProbs, Vs ), dims: new long[] { batchSize, m_multiHeadNum, seqLenQ, m_d } );
            WeightTensor W = g.View( g.AsContiguous( g.Transpose( o, 1, 2 ) ), dims: new long[] { batchSize * seqLenQ, m_multiHeadNum * m_d } );

            // Output projection
            WeightTensor finalAttResults = g.Dropout( g.Affine( W, W0, b0 ), batchSize, m_dropoutRatio, inPlace: true );
            WeightTensor result = graph.Add( finalAttResults, inputQ, inPlace: true );

            return (result, sumAttnWeights);
        }

        /// <summary>
        /// Scaled multi-heads attention component with skip connectioned feed forward layers
        /// </summary>
        /// <param name="inputQ">The input Q tensor</param>
        /// <param name="inputK">The input K tensor</param>
        /// <param name="inputV">The input V tensor</param>
        /// <param name="keyMask">The mask for softmax</param>
        /// <param name="batchSize">Batch size of input data set</param>
        /// <param name="graph">The instance of computing graph</param>
        /// <returns>Transformered output tensor</returns>
        public (WeightTensor, WeightTensor) Perform( WeightTensor inputQ, WeightTensor inputK, WeightTensor inputV, WeightTensor keyMask, int batchSize, ComputeGraphTensor graph, bool outputAttenWeights = false, Dictionary<string, WeightTensor> cachedTensors = null )
        {
            var keyName = $"{m_name}_MultiHeadAttention";
            using ComputeGraphTensor g = graph.CreateSubGraph( keyName );
            int seqLenQ = inputQ.Rows / batchSize;

            // SeqLenK must be euqal to SeqLenV
            int seqLenK = inputK.Rows / batchSize;
            int seqLenV = inputV.Rows / batchSize;

            WeightTensor inputQNorm = layerNormQ.Norm( inputQ, g );

            //Input projections
            WeightTensor allQ = g.View( g.Affine( inputQNorm, Q, Qb ), dims: new long[] { batchSize, seqLenQ, m_multiHeadNum, m_d } );
            WeightTensor allK = g.View( g.Affine( inputK,     K, Kb ), dims: new long[] { batchSize, seqLenK, m_multiHeadNum, m_d } );
            WeightTensor allV = g.View( g.Affine( inputV,     V, Vb ), dims: new long[] { batchSize, seqLenV, m_multiHeadNum, m_d } );

            //Multi-head attentions
            WeightTensor Qs = g.View( g.AsContiguous( g.Transpose( allQ, 1, 2 ) ), dims: new long[] { batchSize * m_multiHeadNum, seqLenQ, m_d } );


            WeightTensor Ks = null;
            WeightTensor Vs = null;

            if ( cachedTensors == null ) // We don't use any cached tensors
            {
                Ks = g.View( g.AsContiguous( g.Transpose( g.Transpose( allK, 1, 2 ), 2, 3 ) ), dims: new long[] { batchSize * m_multiHeadNum, m_d, seqLenK } );
                Vs = g.View( g.AsContiguous( g.Transpose( allV, 1, 2 ) ), dims: new long[] { batchSize * m_multiHeadNum, seqLenV, m_d } );
            }
            else
            {
                var KsCacheName = keyName + '_' + nameof(Ks);
                var VsCacheName = keyName + '_' + nameof(Vs);

                if ( !cachedTensors.TryGetValue( KsCacheName, out Ks ) )
                {
                    Ks = g.View( g.AsContiguous( g.Transpose( g.Transpose( allK, 1, 2 ), 2, 3 ) ), dims: new long[] { batchSize * m_multiHeadNum, m_d, seqLenK } );
                    cachedTensors.Add( KsCacheName, Ks.CopyWeightsRef( KsCacheName, Ks.NeedGradient ) );
                }
               
                if ( !cachedTensors.TryGetValue( VsCacheName, out Vs ) )
                {
                    Vs = g.View( g.AsContiguous( g.Transpose( allV, 1, 2 ) ), dims: new long[] { batchSize * m_multiHeadNum, seqLenV, m_d } );
                    cachedTensors.Add( VsCacheName, Vs.CopyWeightsRef( VsCacheName, Vs.NeedGradient ) );
                }
            }

            // Scaled softmax
            float scale = 1.0f / (float) (Math.Sqrt( m_d ));
            var attn = g.MulBatch( Qs, Ks, scale );
                attn = g.View( attn, dims: new long[] { batchSize, m_multiHeadNum, seqLenQ, seqLenK } );
            if ( keyMask != null )
            {
                attn = g.Add( attn, keyMask, inPlace: true );
            }

            var attnProbs = g.Softmax( attn, inPlace: true );

            WeightTensor sumAttnWeights = null;
            if ( outputAttenWeights )
            {
                sumAttnWeights = g.Select( attnProbs, 1, 0 );
                for ( int i = 1; i < m_multiHeadNum; i++ )
                {
                    var tmp = g.Select( attnProbs, 1, i );
                    sumAttnWeights = g.Add( sumAttnWeights, tmp );
                }

                sumAttnWeights = graph.Div( sumAttnWeights, (float) m_multiHeadNum );
                sumAttnWeights = graph.View( sumAttnWeights, new long[] { batchSize * seqLenQ, seqLenK } );
            }

            attnProbs = g.View( attnProbs, dims: new long[] { batchSize * m_multiHeadNum, seqLenQ, seqLenK } );

            WeightTensor o = g.View( g.MulBatch( attnProbs, Vs ), dims: new long[] { batchSize, m_multiHeadNum, seqLenQ, m_d } );
            WeightTensor W = g.View( g.AsContiguous( g.Transpose( o, 1, 2 ) ), dims: new long[] { batchSize * seqLenQ, m_multiHeadNum * m_d } );

            // Output projection
            WeightTensor finalAttResults = g.Dropout( g.Affine( W, W0, b0 ), batchSize, m_dropoutRatio, inPlace: true );
            WeightTensor result = graph.Add( finalAttResults, inputQ, inPlace: true );

            return (result, sumAttnWeights);
        }

        public virtual List<WeightTensor> GetParams()
        {
            var response = new List<WeightTensor> { W0, b0 };

            if ( !m_sharedQKV )
            {
                response.Add( Q );
                response.Add( Qb );

                response.Add( K );
                response.Add( Kb );

                response.Add( V );
                response.Add( Vb );
            }
            else
            {
                response.Add( QKV );
                response.Add( QKVb );
            }

            response.AddRange( layerNormQ.GetParams() );

            return (response);
        }
        public void Save( Model model )
        {
            if ( !m_sharedQKV )
            {
                Q.Save( model );
                Qb.Save( model );

                K.Save( model );
                Kb.Save( model );

                V.Save( model );
                Vb.Save( model );
            }
            else
            {
                QKV.Save( model );
                QKVb.Save( model );
            }

            W0.Save( model );
            b0.Save( model );

            layerNormQ.Save( model );
        }

        public void Load( Model model )
        {
            if ( !m_sharedQKV )
            {
                Q.Load( model );
                Qb.Load( model );

                K.Load( model );
                Kb.Load( model );

                V.Load( model );
                Vb.Load( model );
            }
            else
            {
                QKV.Load( model );
                QKVb.Load( model );
            }

            W0.Load( model );
            b0.Load( model );

            layerNormQ.Load( model );
        }
    }
}
