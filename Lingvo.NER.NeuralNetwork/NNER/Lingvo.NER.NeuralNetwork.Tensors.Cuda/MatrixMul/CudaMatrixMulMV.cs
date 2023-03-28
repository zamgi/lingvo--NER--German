﻿using System;

using ManagedCuda.CudaBlas;

using Lingvo.NER.NeuralNetwork.Tensors.Core;

namespace Lingvo.NER.NeuralNetwork.Tensors.Cuda.MatrixMul
{
    /// <summary>
    /// 
    /// </summary>
    public static class CudaMatrixMulMV
    {
        public static Tensor Mul_M_V( TSCudaContext context, Tensor result, Tensor lhs, Tensor rhs )
        {
            if ( lhs.ElementType != rhs.ElementType || (result != null && result.ElementType != lhs.ElementType) )
            {
                throw new InvalidOperationException( "All tensors must have the same element type" );
            }

            CudaHelpers.ThrowIfDifferentDevices( result, lhs, rhs );
            if ( result != null && !(result.Storage is CudaStorage) )
            {
                throw new ArgumentException( "result must be a CUDA tensor", "result" );
            }
            if ( !(lhs.Storage is CudaStorage) )
            {
                throw new ArgumentException( "lhs must be a CUDA tensor", "lhs" );
            }
            if ( !(rhs.Storage is CudaStorage) )
            {
                throw new ArgumentException( "rhs must be a CUDA tensor", "rhs" );
            }
            if ( lhs.DimensionCount != 2 )
            {
                throw new ArgumentException( "lhs must have 2 dimensions", "lhs" );
            }
            if ( rhs.DimensionCount != 1 )
            {
                throw new ArgumentException( "rhs must have 1 dimension (ie. be a vector)", "rhs" );
            }

            Tensor lhsClone;
            if ( lhs.Strides[ 1 ] == 1 ) // If lhs is already row-major, do nothing
            {
                lhsClone = lhs.CopyRef();
            }
            else if ( lhs.Strides[ 0 ] == 1 ) // If lhs is column-major, transpose it
            {
                lhsClone = lhs.Transpose();
            }
            else // If lhs is not contiguous in either dimension, make a temporary contiguous copy
            {
                lhsClone = Ops.NewContiguous( lhs );
            }

            Tensor writeTarget = TensorResultBuilder.GetWriteTarget( result, rhs, false, lhs.Sizes[ 0 ] );
            try
            {
                if ( writeTarget.ElementType == DType.Float32 )
                {
                    Run_M_V_float( context, writeTarget, lhsClone, rhs );
                }
                else if ( writeTarget.ElementType == DType.Float64 )
                {
                    Run_M_V_double( context, writeTarget, lhsClone, rhs );
                }
                else
                {
                    throw new NotSupportedException( "CUDA Matrix-Vector multiplication with element type " + result.ElementType + " not supported" );
                }
            }
            finally
            {
                lhsClone.Dispose();
            }

            return (writeTarget);
        }

        private static void Run_M_V_float( TSCudaContext context, Tensor result, Tensor mat, Tensor vec )
        {
            // Require lhs to be row-major. This means we must tell BLAS to transpose it (BLAS expects column-major matrices)
            if ( mat.Strides[ 1 ] != 1 )
            {
                throw new ArgumentException( "lhs must be contiguous in the last dimension" );
            }

            using ( Util.PooledObject<CudaBlas> blas = context.BlasForTensor( mat ) )
            {
                ManagedCuda.BasicTypes.CUdeviceptr yPtr = CudaHelpers.GetBufferStart( result );
                ManagedCuda.BasicTypes.CUdeviceptr aPtr = CudaHelpers.GetBufferStart( mat );
                ManagedCuda.BasicTypes.CUdeviceptr xPtr = CudaHelpers.GetBufferStart( vec );

                var trans = Operation.Transpose;
                int m = (int) mat.Sizes[ 1 ];
                int n = (int) mat.Sizes[ 0 ];
                int incx = (int) vec.Strides[ 0 ];
                int lda = (int) mat.Strides[ 0 ];
                int incy = (int) result.Strides[ 0 ];
                float alpha = 1;
                float beta = 0;

                CudaBlasNativeMethods.cublasSgemv_v2( blas.Value.CublasHandle, trans, m, n, ref alpha, aPtr, lda, xPtr, incx, ref beta, yPtr, incy );
            }
        }

        private static void Run_M_V_double( TSCudaContext context, Tensor result, Tensor mat, Tensor vec )
        {
            // Require lhs to be row-major. This means we must tell BLAS to transpose it (BLAS expects column-major matrices)
            if ( mat.Strides[ 1 ] != 1 )
            {
                throw new ArgumentException( "lhs must be contiguous in the last dimension" );
            }

            using ( Util.PooledObject<CudaBlas> blas = context.BlasForTensor( mat ) )
            {
                ManagedCuda.BasicTypes.CUdeviceptr yPtr = CudaHelpers.GetBufferStart( result );
                ManagedCuda.BasicTypes.CUdeviceptr aPtr = CudaHelpers.GetBufferStart( mat );
                ManagedCuda.BasicTypes.CUdeviceptr xPtr = CudaHelpers.GetBufferStart( vec );

                Operation trans = Operation.Transpose;
                int m = (int) mat.Sizes[ 1 ];
                int n = (int) mat.Sizes[ 0 ];
                int incx = (int) vec.Strides[ 0 ];
                int lda = (int) mat.Strides[ 0 ];
                int incy = (int) result.Strides[ 0 ];
                double alpha = 1;
                double beta = 0;

                CudaBlasNativeMethods.cublasDgemv_v2( blas.Value.CublasHandle, trans, m, n, ref alpha, aPtr, lda, xPtr, incx, ref beta, yPtr, incy );
            }
        }
    }
}
