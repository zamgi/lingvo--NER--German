using System;

using ManagedCuda;
using ManagedCuda.CudaBlas;

using Lingvo.NER.NeuralNetwork.Tensors.Cuda.ContextState;
using Lingvo.NER.NeuralNetwork.Tensors.Cuda.Util;
using ManagedCuda.BasicTypes;

namespace Lingvo.NER.NeuralNetwork.Tensors.Cuda
{
    /// <summary>
    /// Used by TSCudaContext to maintain per-device state
    /// </summary>
    public class DeviceState : IDisposable
    {
        private const int SCRATCH_SPACE_PER_SMS_TREAM = 4 * sizeof(float);

        public readonly CudaContext CudaContext;
        public readonly CudaDeviceProperties DeviceInfo;
        public readonly ObjectPool<CudaBlas> BlasHandles;
        public readonly IDeviceAllocator MemoryAllocator;
        public readonly ScratchSpace ScratchSpace;

        public DeviceState( int deviceId, float memoryUsageRatio = 0.9f )
        {
            CudaContext = new CudaContext( deviceId );
            DeviceInfo = CudaContext.GetDeviceInfo();

            BlasHandles = new ObjectPool< CudaBlas >( 1, () =>
            {
                CudaContext.SetCurrent();
                return (new CudaBlas());
            },
            blas => blas.Dispose() );

            MemoryAllocator = new PoolingDeviceAllocator( CudaContext, memoryUsageRatio );
            ScratchSpace    = AllocScratchSpace( CudaContext, DeviceInfo );
        }

        public void Dispose()
        {
            BlasHandles.Dispose();
            CudaContext.Dispose();
            MemoryAllocator.Dispose();
        }

        private static ScratchSpace AllocScratchSpace( CudaContext context, CudaDeviceProperties deviceProps )
        {
            var size = SCRATCH_SPACE_PER_SMS_TREAM * deviceProps.MultiProcessorCount;
            CUdeviceptr buffer = context.AllocateMemory( size );
            return (new ScratchSpace() { Size = size, Buffer = buffer });
        }
    }
}
