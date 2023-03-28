﻿using System;

using ManagedCuda;
using ManagedCuda.BasicTypes;

namespace Lingvo.NER.NeuralNetwork.Tensors.Cuda.ContextState
{
    /// <summary>
    /// This allocator simply forwards all alloc/free requests to CUDA. This will generally be slow
    /// because calling cudaMalloc causes GPU synchronization
    /// </summary>
    public class BasicDeviceAllocator : IDeviceAllocator
    {
        private readonly CudaContext _Context;
        public BasicDeviceAllocator( CudaContext cudaContext ) => _Context = cudaContext;
        public void Dispose() { }

        public IDeviceMemory Allocate( long byteCount )
        {
            CUdeviceptr buffer = _Context.AllocateMemory( byteCount );
            return new BasicDeviceMemory( buffer, () => _Context.FreeMemory( buffer ) );
        }

        public float GetAllocatedMemoryRatio() => 0.0f;
    }
    /// <summary>
    /// 
    /// </summary>
    public class BasicDeviceMemory : IDeviceMemory
    {
        private readonly CUdeviceptr _Pointer;
        private readonly Action _FreeHandler;

        public CUdeviceptr Pointer => _Pointer;
        public BasicDeviceMemory( CUdeviceptr pointer, Action freeHandler )
        {
            _Pointer = pointer;
            _FreeHandler = freeHandler;
        }
        public void Free() => _FreeHandler();
    }
}
