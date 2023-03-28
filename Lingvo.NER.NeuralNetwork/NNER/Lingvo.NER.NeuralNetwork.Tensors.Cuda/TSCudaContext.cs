using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using ManagedCuda;
using ManagedCuda.BasicTypes;
using ManagedCuda.CudaBlas;

using Lingvo.NER.NeuralNetwork.Tensors.Cuda.ContextState;
using Lingvo.NER.NeuralNetwork.Tensors.Cuda.Util;
using Lingvo.NER.NeuralNetwork.Tensors.Cuda.RuntimeCompiler;

namespace Lingvo.NER.NeuralNetwork.Tensors.Cuda
{
    /// <summary>
    /// 
    /// </summary>
    public struct ScratchSpace
    {
        public int Size;
        public CUdeviceptr Buffer;
    }

    /// <summary>
    /// 
    /// </summary>

    public class TSCudaContext : IDisposable
    {
        public const int MAX_DIMS = 25;
        private const string CACHE_DIR = @"cuda_cache\general";

        private readonly DeviceState[]   _DeviceStates;
        private readonly bool[,]         _P2PAccess;
        //private readonly int[]           _DeviceIds;
        private readonly Dictionary< int, int > _IndexByDeviceId;
        private readonly KernelDiskCache _DiskCache;
        private readonly CudaCompiler    _Compiler;
        private readonly CudaKernelCache _KernelCache;

        public TSCudaContext( Dictionary<int, int> indexByDeviceId, float memoryUsageRatio = 0.9f, string[] compilerOptions = null )
        {
            _IndexByDeviceId = indexByDeviceId;
            _KernelCache     = new CudaKernelCache();
            _DeviceStates    = new DeviceState[ _IndexByDeviceId.Count ];
            foreach ( var p in _IndexByDeviceId )
            {
                var deviceId = p.Key;
                var i        = p.Value;
                _DeviceStates[ i ] = new DeviceState( deviceId, memoryUsageRatio );
            }
            _P2PAccess = EnablePeerAccess( _DeviceStates.Select( x => x.CudaContext ).ToArray(), _DeviceStates[ 0 ].CudaContext );

            _DiskCache = new KernelDiskCache( Path.Combine( Environment.CurrentDirectory, CACHE_DIR ) );
            _Compiler  = new CudaCompiler( _DiskCache, compilerOptions );

            OpRegistry.RegisterAssembly( Assembly.GetExecutingAssembly() );
        }
        //public TSCudaContext( int[] deviceIds, float memoryUsageRatio = 0.9f, string[] compilerOptions = null )
        //{
        //    _KernelCache  = new CudaKernelCache();
        //    _DeviceIds    = deviceIds;
        //    _DeviceStates = new DeviceState[ deviceIds.Length ];
        //    for ( int i = 0; i < deviceIds.Length; i++ )
        //    {
        //        _DeviceStates[ i ] = new DeviceState( deviceIds[ i ], memoryUsageRatio );
        //    }
        //    _P2PAccess = EnablePeerAccess( _DeviceStates.Select( x => x.CudaContext ).ToArray(), _DeviceStates[ 0 ].CudaContext );

        //    _DiskCache = new KernelDiskCache( Path.Combine( Environment.CurrentDirectory, CACHE_DIR ) );
        //    _Compiler  = new CudaCompiler( _DiskCache, compilerOptions );

        //    OpRegistry.RegisterAssembly( Assembly.GetExecutingAssembly() );
        //}
        public void Dispose()
        {
            _KernelCache.Dispose();
            foreach ( DeviceState deviceState in _DeviceStates )
            {
                deviceState.Dispose();
            }
        }

        private int GetDeviceIdIndex( int deviceId ) => (_IndexByDeviceId.TryGetValue( deviceId, out var i ) ? i : -1);
        //private int GetDeviceIdIndex( int deviceId )
        //{
        //    for ( int i = 0; i < _DeviceIds.Length; i++ )
        //    {
        //        if ( _DeviceIds[ i ] == deviceId )
        //        {
        //            return (i);
        //        }
        //    }
        //    return (-1);
        //}

        public CudaCompiler Compiler => _Compiler;
        public CudaKernelCache KernelCache => _KernelCache;

        public void Synchronize( int deviceId )
        {
            var idx = GetDeviceIdIndex( deviceId );
            _DeviceStates[ idx ].CudaContext.Synchronize();
        }
        public void SynchronizeAll()
        {
            foreach ( DeviceState device in _DeviceStates )
            {
                device.CudaContext.Synchronize();
            }
        }

        public CudaContext CudaContextForDevice( int deviceId )
        {
            var idx = GetDeviceIdIndex( deviceId );
            return _DeviceStates[ idx ].CudaContext;
        }

        public IDeviceAllocator AllocatorForDevice( int deviceId )
        {
            var idx = GetDeviceIdIndex( deviceId );
            return _DeviceStates[ idx ].MemoryAllocator;
        }

        public CudaContext CudaContextForTensor( Tensor tensor ) => CudaContextForDevice( CudaHelpers.GetDeviceId( tensor ) );
        public ScratchSpace ScratchSpaceForDevice( int deviceId )
        {
            var idx = GetDeviceIdIndex( deviceId );
            return _DeviceStates[ idx ].ScratchSpace;
        }

        public PooledObject<CudaBlas> BlasForDevice( int deviceId )
        {
            var idx = GetDeviceIdIndex( deviceId );
            return _DeviceStates[ idx ].BlasHandles.Get();
        }

        public PooledObject<CudaBlas> BlasForTensor( Tensor tensor ) => BlasForDevice( CudaHelpers.GetDeviceId( tensor ) );

        public bool CanAccessPeer( int srcDevice, int peerDevice )
        {
            int srcDeviceIdx = GetDeviceIdIndex( srcDevice );
            int peerDeviceIdx = GetDeviceIdIndex( peerDevice );
            return _P2PAccess[ srcDeviceIdx, peerDeviceIdx ];
        }

        public CudaDeviceProperties DeviceInfoForContext( CudaContext cudaContext )
        {
            var idx = GetDeviceIdIndex( cudaContext.DeviceId );
            return _DeviceStates[ idx ].DeviceInfo;
        }

        // Returns a matrix of [i, j] values where [i, j] is true iff device i can access device j
        private static bool[,] EnablePeerAccess( CudaContext[] cudaContexts, CudaContext restoreCurrent )
        {
            var result = new bool[ cudaContexts.Length, cudaContexts.Length ];

            for ( int i = 0; i < cudaContexts.Length; ++i )
            {
                for ( int j = 0; j < cudaContexts.Length; ++j )
                {
                    if ( i == j )
                    {
                        result[ i, j ] = true;
                    }
                    else
                    {
                        result[ i, j ] = EnablePeers( cudaContexts[ i ], cudaContexts[ j ] );
                    }
                }
            }

            restoreCurrent.SetCurrent();
            return (result);
        }

        private static bool EnablePeers( CudaContext src, CudaContext target )
        {
            if ( !src.DeviceCanAccessPeer( target ) )
            {
                return (false);
            }

            src.SetCurrent();

            try
            {
                CudaContext.EnablePeerAccess( target );
                return (true);
            }
            catch
            {
                return (false);
            }
        }

        public void Precompile( Action<string> precompileProgressWriter )
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach ( var t in assembly.TypesWithAttribute< PrecompileAttribute >( true ).Where( x => !x.type.IsAbstract ) )
            {
                precompileProgressWriter( $"Precompiling '{t.type.Name}'\n" );

                var instance = (IPrecompilable) Activator.CreateInstance( t.type );
                instance.Precompile( Compiler );
            }
        }
        public void CleanUnusedPTX() => _DiskCache.CleanUnused();
    }
}
