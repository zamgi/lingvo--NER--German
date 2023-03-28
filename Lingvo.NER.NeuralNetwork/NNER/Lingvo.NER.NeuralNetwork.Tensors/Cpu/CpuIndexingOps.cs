﻿using System;

using Lingvo.NER.NeuralNetwork.Tensors.Core;

namespace Lingvo.NER.NeuralNetwork.Tensors.Cpu
{
    /// <summary>
    /// 
    /// </summary>
    [OpsClass]
    public class CpuIndexingOps
    {
        public CpuIndexingOps() { }

        [RegisterOpStorageType("gather", typeof(CpuStorage))]
        public Tensor Gather( Tensor result, Tensor src, int dim, Tensor indices )
        {
            if ( result != null && result.DimensionCount != src.DimensionCount )
            {
                throw new InvalidOperationException( "result and src must have same number of dimensions" );
            }
            if ( result != null && dim < 0 && dim >= result.DimensionCount )
            {
                throw new ArgumentOutOfRangeException( nameof(dim ) );
            }
            if ( indices.DimensionCount != src.DimensionCount )
            {
                throw new InvalidOperationException( "src and indices must have same number of dimensions" );
            }
            if ( result != null && !result.IsSameSizeAs( indices ) )
            {
                throw new InvalidOperationException( "result and indices must be the same size" );
            }
            if ( result != null && !TensorResultBuilder.ArrayEqualExcept( src.Sizes, result.Sizes, dim ) )
            {
                throw new InvalidOperationException( "result and src must be the same size except in dimension dim" );
            }

            Tensor writeTarget = TensorResultBuilder.GetWriteTarget( result, indices.Allocator, src.ElementType, false, indices.Sizes );
            TensorApplyCPU.Gather( writeTarget, src, dim, indices );

            return (writeTarget);
        }

        [RegisterOpStorageType("scatter", typeof(CpuStorage))]
        public Tensor Scatter( Tensor result, Tensor src, int dim, Tensor indices )
        {
            if ( result == null )
            {
                throw new ArgumentNullException( nameof(result ) );
            }
            if ( result.DimensionCount != src.DimensionCount )
            {
                throw new InvalidOperationException( "result and src must have same number of dimensions" );
            }
            if ( dim < 0 && dim >= result.DimensionCount )
            {
                throw new ArgumentOutOfRangeException( nameof(dim ) );
            }
            if ( indices.DimensionCount != src.DimensionCount )
            {
                throw new InvalidOperationException( "src and indices must have same number of dimensions" );
            }
            if ( !src.IsSameSizeAs( indices ) )
            {
                throw new InvalidOperationException( "src and indices must be the same size" );
            }
            if ( !TensorResultBuilder.ArrayEqualExcept( src.Sizes, result.Sizes, dim ) )
            {
                throw new InvalidOperationException( "result and src must be the same size except in dimension dim" );
            }

            Tensor writeTarget = result;
            TensorApplyCPU.Scatter( writeTarget, src, dim, indices );

            return (writeTarget);
        }


        [RegisterOpStorageType("scatter_add", typeof(CpuStorage))]
        public Tensor ScatterAdd( Tensor result, Tensor src, int dim, Tensor indices )
        {
            if ( result == null )
            {
                throw new ArgumentNullException( nameof(result ) );
            }
            if ( result.DimensionCount != src.DimensionCount )
            {
                throw new InvalidOperationException( "result and src must have same number of dimensions" );
            }
            if ( dim < 0 && dim >= result.DimensionCount )
            {
                throw new ArgumentOutOfRangeException( nameof(dim ) );
            }
            if ( indices.DimensionCount != src.DimensionCount )
            {
                throw new InvalidOperationException( "src and indices must have same number of dimensions" );
            }
            if ( !src.IsSameSizeAs( indices ) )
            {
                throw new InvalidOperationException( "src and indices must be the same size" );
            }
            if ( !TensorResultBuilder.ArrayEqualExcept( src.Sizes, result.Sizes, dim ) )
            {
                throw new InvalidOperationException( "result and src must be the same size except in dimension dim" );
            }

            Tensor writeTarget = result;
            TensorApplyCPU.ScatterAdd( writeTarget, src, dim, indices );

            return (writeTarget);
        }

        [RegisterOpStorageType("scatter_fill", typeof(CpuStorage))]
        public Tensor ScatterFill( Tensor result, float value, int dim, Tensor indices )
        {
            if ( result == null )
            {
                throw new ArgumentNullException( nameof(result ) );
            }
            if ( dim < 0 && dim >= result.DimensionCount )
            {
                throw new ArgumentOutOfRangeException( nameof(dim ) );
            }
            if ( indices.DimensionCount != result.DimensionCount )
            {
                throw new InvalidOperationException( "result and indices must have same number of dimensions" );
            }
            if ( !TensorResultBuilder.ArrayEqualExcept( indices.Sizes, result.Sizes, dim ) )
            {
                throw new InvalidOperationException( "result and indices must be the same size except in dimension dim" );
            }

            Tensor writeTarget = result;
            TensorApplyCPU.ScatterFill( writeTarget, value, dim, indices );
            return (writeTarget);
        }
    }
}
