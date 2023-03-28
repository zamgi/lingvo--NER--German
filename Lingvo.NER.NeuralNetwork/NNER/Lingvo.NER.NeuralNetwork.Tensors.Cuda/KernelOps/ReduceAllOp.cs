﻿using System;

using ManagedCuda;
using ManagedCuda.VectorTypes;

using Lingvo.NER.NeuralNetwork.Tensors.Core;
using Lingvo.NER.NeuralNetwork.Tensors.Cuda.DeviceCode;
using Lingvo.NER.NeuralNetwork.Tensors.Cuda.RuntimeCompiler;

namespace Lingvo.NER.NeuralNetwork.Tensors.Cuda.KernelOps
{
    /// <summary>
    /// 
    /// </summary>
    public static class ReduceAllOp
    {
        private const long ReduceAllBlockSize = 1024;
        private const long TwoPassReductionSize = 2048;

        public static Tensor Invoke( CudaReduceAllKernels reduceAllKernels, float init, ReduceInitType initType, string kernelName, Tensor result, Tensor src, object extraArg = null )
        {
            int           deviceId    = CudaHelpers.GetDeviceId( src );
            TSCudaContext context     = CudaHelpers.TSContextForTensor( src );
            CudaContext   cudaContext = context.CudaContextForDevice( deviceId );

            cudaContext.SetCurrent();

            if ( src.DimensionCount > TSCudaContext.MAX_DIMS ) throw (new InvalidOperationException( $"Tensors with dimension count > {TSCudaContext.MAX_DIMS} are not supported." ));

            Tensor writeTarget = TensorResultBuilder.GetWriteTarget( result, src, false, 1 );

            if ( src.DimensionCount == 0 )
            {
                return (result);
            }

            long totalElements = src.ElementCount();
            var config = new ApplySpecialization( src );
            object totalElementsTyped = config.Use32BitIndices ? (uint) totalElements : (ulong) totalElements;
            object initValueTyped = ReduceInitConverter.GetInitValue( init, initType, src.ElementType );

            dim3 grid;
            dim3 block;

            byte[] ptx = reduceAllKernels.GetPtx( context.Compiler );
            string fullKernelName = PermutationGenerator.GetMangledName( kernelName, config );

            ManagedCuda.BasicTypes.CUdeviceptr outputDevicePtr = CudaHelpers.GetBufferStart( writeTarget );

            if ( IsTwoPassReductionSize( totalElements ) )
            {
                GetPass1ReduceBlockGrid( context, deviceId, totalElements, out grid, out block );
                uint smemSize = block.x * sizeof(float);

                ManagedCuda.BasicTypes.CUdeviceptr scratchSpace = context.ScratchSpaceForDevice( deviceId ).Buffer;

                if ( extraArg == null )
                {
                    InvokeReduceAll( context, cudaContext, ptx, "twoPassA_" + fullKernelName, grid, block, smemSize, config, src, totalElementsTyped, initValueTyped, scratchSpace );
                }
                else
                {
                    InvokeReduceAll( context, cudaContext, ptx, "twoPassA_" + fullKernelName, grid, block, smemSize, config, src, totalElementsTyped, initValueTyped, scratchSpace, extraArg );
                }

                uint numPass1Blocks = grid.x;
                GetPass2ReduceBlockGrid( context, deviceId, totalElements, out grid, out block );
                smemSize = block.x * sizeof(float);

                InvokeReduceAllPass2( context, cudaContext, ptx, "twoPassB_" + fullKernelName, grid, block, smemSize, config.Use32BitIndices, numPass1Blocks, initValueTyped, scratchSpace, outputDevicePtr );
            }
            else
            {
                GetSinglePassReduceBlockGrid( totalElements, out grid, out block );
                uint smemSize = block.x * sizeof(float);

                if ( extraArg == null )
                {
                    InvokeReduceAll( context, cudaContext, ptx, "onePass_" + fullKernelName, grid, block, smemSize, config, src, totalElementsTyped, initValueTyped, outputDevicePtr );
                }
                else
                {
                    InvokeReduceAll( context, cudaContext, ptx, "onePass_" + fullKernelName, grid, block, smemSize, config, src, totalElementsTyped, initValueTyped, outputDevicePtr, extraArg );
                }
            }

            return (writeTarget);
        }

        public static void InvokeReduceAllPass2( TSCudaContext context, CudaContext cudaContext, byte[] ptx, string kernelName, dim3 grid, dim3 block, uint smemSize, bool index32, params object[] args )
        {
            CudaKernel kernel = context.KernelCache.Get( cudaContext, ptx, kernelName );
            kernel.GridDimensions      = grid;
            kernel.BlockDimensions     = block;
            kernel.DynamicSharedMemory = smemSize;
            kernel.Run( args );
        }

        public static void InvokeReduceAll( TSCudaContext context, CudaContext cudaContext, byte[] ptx, string kernelName, dim3 grid, dim3 block, uint smemSize, ApplySpecialization spec, params object[] args )
        {
            ConvertTensorArgs.Convert( cudaContext, spec.Use32BitIndices, args );

            CudaKernel kernel = context.KernelCache.Get( cudaContext, ptx, kernelName );
            kernel.GridDimensions      = grid;
            kernel.BlockDimensions     = block;
            kernel.DynamicSharedMemory = smemSize;
            kernel.Run( args );
        }

        private static bool IsTwoPassReductionSize( long elements ) => (elements > TwoPassReductionSize);

        private static long GetTwoPassBlocks( TSCudaContext context, int deviceId, long elements )
        {
            long numBlocks = ApplyUtils.CeilDiv( elements, ReduceAllBlockSize );

            // We can only have as many blocks as there is scratch space
            long scratchSpace = context.ScratchSpaceForDevice( deviceId ).Size / sizeof(float);
            if ( scratchSpace <= 0 ) throw (new ApplicationException( $"Device id '{deviceId}' has no scratch space." ));
            if ( scratchSpace < numBlocks )
            {
                numBlocks = scratchSpace;
            }
            return numBlocks;
        }

        private static void GetPass1ReduceBlockGrid( TSCudaContext context, int deviceId, long elements, out dim3 grid, out dim3 block )
        {
            grid  = new dim3( (uint) GetTwoPassBlocks( context, deviceId, elements ) );
            block = new dim3( (uint) ReduceAllBlockSize );
        }

        private static void GetPass2ReduceBlockGrid( TSCudaContext context, int deviceId, long elements, out dim3 grid, out dim3 block )
        {
            grid = new dim3( 1 );
            // We only need as many threads as there were blocks originally
            block = new dim3( (uint) GetTwoPassBlocks( context, deviceId, elements ) );
        }

        private static void GetSinglePassReduceBlockGrid( long elements, out dim3 grid, out dim3 block )
        {
            grid  = new dim3( 1 );
            block = new dim3( (uint) ReduceAllBlockSize );
        }
    }
}
