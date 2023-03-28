using System;
using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Networks;
using Lingvo.NER.NeuralNetwork.Tensors;
using Lingvo.NER.NeuralNetwork.Text;

namespace Lingvo.NER.NeuralNetwork.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public static class TensorUtils
    {
        public static void ScatterFill( WeightTensor res, float val, WeightTensor indices, int dim ) => Ops.ScatterFill( res.TWeight, val, dim, indices.TWeight );

        /// <summary>
        /// Create input embedding from token embeddings, segment embeddings
        /// </summary>
        /// <returns>The embedding tensor. shape: (batchsize * seqLen, embedding_dim) </returns>
        public static WeightTensor CreateTokensEmbeddings( List<List<int>> seqs, ComputeGraphTensor g, WeightTensor embeddingsTensor,
            WeightTensor segmentEmbedding, WeightTensor contextEmbeddings, Vocab vocab, bool applyContextEmbeddingsToEntireSequence = true, float scaleFactor = 1.0f )
        {
            if ( seqs             == null ) throw (new ArgumentNullException( nameof(seqs) ));
            if ( g                == null ) throw (new ArgumentNullException( nameof(g) ));
            if ( embeddingsTensor == null ) throw (new ArgumentNullException( nameof(embeddingsTensor) ));
            if ( vocab            == null ) throw (new ArgumentNullException( nameof(vocab) ));

            int batchSize = seqs.Count;
            int seqLen    = seqs[ 0 ].Count;

            var idxs    = new float[ batchSize * seqLen ];
            var segIdxs = new float[ batchSize * seqLen ];

            var segment0Length = new List<int>( batchSize );

            for ( int i = 0; i < batchSize; i++ )
            {
                int segIdx = 0;
                for ( int j = 0; j < seqLen; j++ )
                {
                    idxs[ i * seqLen + j ] = seqs[ i ][ j ];
                    segIdxs[ i * seqLen + j ] = segIdx;

                    var token = vocab.GetString( seqs[ i ][ j ] );
                    if ( token == BuildInTokens.SEP )
                    {
                        //A new segment
                        if ( segIdx == 0 )
                        {
                            segment0Length.Add( j );
                        }
                        segIdx++;
                    }
                }

                if ( segIdx == 0 )
                {
                    segment0Length.Add( seqLen );
                }
            }

            WeightTensor embeddingRst = g.IndexSelect( embeddingsTensor, idxs );
            if ( scaleFactor != 1.0f )
            {
                embeddingRst = g.Mul( embeddingRst, scaleFactor, inPlace: true );
            }

            // Apply segment embeddings to the input sequence embeddings
            if ( segmentEmbedding != null )
            {
                embeddingRst = g.Add( embeddingRst, g.IndexSelect( segmentEmbedding, segIdxs ) );
            }

            // Apply contextual feature embeddings to the input sequence embeddings
            if ( contextEmbeddings != null )
            {
                int dim = contextEmbeddings.Columns;
                contextEmbeddings = g.View( contextEmbeddings, dims: new long[] { batchSize, 1, dim } );
                contextEmbeddings = g.Expand( contextEmbeddings, dims: new long[] { batchSize, seqLen, dim } );

                if ( !applyContextEmbeddingsToEntireSequence )
                {
                    //Only apply contexual feature embeddings to the first segment of the input sequence
                    WeightTensor featureMaskTensor = g.BuildFeatureMask( seqLen, segment0Length, embeddingsTensor.Columns ); //shape: (batch_size, seqLen, dim)
                    contextEmbeddings = g.EltMul( contextEmbeddings, featureMaskTensor );
                }

                embeddingRst = g.Add( embeddingRst, contextEmbeddings );
            }

            return (embeddingRst);
        }
    }
}
