using System;
using System.Collections.Generic;
using System.Linq;

using Lingvo.NER.NeuralNetwork.Tensors;
using Lingvo.NER.NeuralNetwork.Tensors.Cpu;
using Lingvo.NER.NeuralNetwork.Tensors.Cuda;
using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork
{
    /// <summary>
    /// 
    /// </summary>
    public static class TensorAllocator
    {
        private static IAllocator[] _Allocator;
        private static TSCudaContext _CudaContext;
        //---private static int[] _DeviceIds;
        private static Dictionary< int, int > _IndexByDeviceId;
        private static ProcessorTypeEnums _ArchType;
        
        public static void InitDevices( ProcessorTypeEnums archType, int[] deviceIds, float memoryUsageRatio = 0.9f, string[] compilerOptions = null )
        {
            _ArchType        = archType;
            _IndexByDeviceId = deviceIds.Distinct().ToDictionary( deviceId => deviceId, i => i );
            _Allocator       = new IAllocator[ _IndexByDeviceId.Count ];

            if ( _ArchType == ProcessorTypeEnums.GPU )
            {
                Logger.WriteLine( $"Initialize device's: '{string.Join( "', '", _IndexByDeviceId.Keys )}'." );

                _CudaContext = new TSCudaContext( _IndexByDeviceId, memoryUsageRatio, compilerOptions );
                _CudaContext.Precompile( Console.Write );
                _CudaContext.CleanUnusedPTX();
            }
        }
        //public static void InitDevices( ProcessorTypeEnums archType, int[] deviceIds, float memoryUsageRatio = 0.9f, string[] compilerOptions = null )
        //{
        //    _ArchType  = archType;
        //    _DeviceIds = deviceIds;            
        //    _Allocator = new IAllocator[ _DeviceIds.Length ];

        //    if ( _ArchType == ProcessorTypeEnums.GPU )
        //    {
        //        foreach ( var id in _DeviceIds )
        //        {
        //            Logger.WriteLine( $"Initialize device '{id}'" );
        //        }

        //        _CudaContext = new TSCudaContext( _DeviceIds, memoryUsageRatio, compilerOptions );
        //        _CudaContext.Precompile( Console.Write );
        //        _CudaContext.CleanUnusedPTX();
        //    }
        //}

        public static IAllocator Allocator( int deviceId )
        {
            var idx = GetDeviceIdIndex( deviceId );
            var allocator = _Allocator[ idx ];
            if ( allocator == null )
            {
                if ( _ArchType == ProcessorTypeEnums.GPU )
                {
                    allocator = _Allocator[ idx ] = new CudaAllocator( _CudaContext, deviceId );
                }
                else
                {
                    allocator = _Allocator[ idx ] = new CpuAllocator();
                }                
            }            
            return (allocator);
        }

        private static int GetDeviceIdIndex( int deviceId )
        {
            if ( _IndexByDeviceId.TryGetValue( deviceId, out var i ) )
            {
                return (i);
            }

            throw (new ArgumentException( $"Failed to get deviceId '{deviceId}', deviceId List = '{string.Join( "', '", _IndexByDeviceId.Keys )}'." ));
        }
        //private static int GetDeviceIdIndex( int id )
        //{
        //    for ( var i = _DeviceIds.Length - 1; 0 <= i ; i-- )
        //    {
        //        if ( _DeviceIds[ i ] == id )
        //        {
        //            return (i);
        //        }
        //    }

        //    throw (new ArgumentException( $"Failed to get deviceId '{id}', deviceId List = '{string.Join( ", ", _DeviceIds )}'" ));
        //}
    }
}
