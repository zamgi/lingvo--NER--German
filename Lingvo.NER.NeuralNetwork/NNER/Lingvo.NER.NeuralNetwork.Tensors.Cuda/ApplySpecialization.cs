﻿using System;
using System.Collections.Generic;
using System.Linq;

using Lingvo.NER.NeuralNetwork.Tensors.Cuda.RuntimeCompiler;

namespace Lingvo.NER.NeuralNetwork.Tensors.Cuda
{
    // Represents a compile-time specialization of ApplyN.
    // If all tensors are small enough, the kernel will use 32-bit indices
    // The kernels are also specialized for contiguous tensors, tensors with a
    // small number of dimensions, and a totally generic 'specialization'.
    // If TensorDims[i] == -2, then tensor i is entirely contiguous
    // If TensorDims[i] == -1, a totally generic kernel should be generated for that tensor.
    public class ApplySpecialization
    {
        public const string IndexType32 = "unsigned __int32";
        public const string IndexType64 = "unsigned __int64";

        public bool Use32BitIndices { get; private set; }
        public int[] TensorDims { get; private set; }

        public ApplySpecialization( params Tensor[] tensors )
        {
            if ( tensors.All( ApplyUtils.CanUse32BitIndexMath ) )
            {
                Use32BitIndices = true;

                // Specialize each tensor dimenionality independently
                TensorDims = tensors.Select( tensor =>
                {
                     if ( tensor.IsContiguous() )
                     {
                         return -2;
                     }
                     return -1; // tensor.DimensionCount > 3 ? -1 : tensor.DimensionCount;
                })
                .ToArray();
            }
            else
            {
                Use32BitIndices = false;
                // For 64-bit index case (ie. large tensors), only specalize on totally contiguous
                // or totally generic
                if ( tensors.All( x => x.IsContiguous() ) )
                {
                    // All tensors are contiguous
                    TensorDims = Enumerable.Repeat( -2, tensors.Length ).ToArray();
                }
                else
                {
                    // Not all tensors are contiguous - just generate a completely generic kernel
                    TensorDims = Enumerable.Repeat( -1, tensors.Length ).ToArray();
                }
            }
        }

        public ApplySpecialization( bool use32BitIndices, params int[] tensorDims )
        {
            Use32BitIndices = use32BitIndices;
            TensorDims      = tensorDims;
        }

        public KernelConfig GetConfig()
        {
            var cfg = new KernelConfig();
                cfg.Set( "INDEX_TYPE", Use32BitIndices ? IndexType32 : IndexType64 );
            for ( int i = 0; i < TensorDims.Length; ++i )
            {
                var tensorName = (char) ('A' + i);
                cfg.Set( "DIMS" + tensorName, TensorDims[ i ].ToString() );
            }
            return cfg;
        }

        public static IEnumerable<ApplySpecialization> AllSpecializations( int tensorCount )
        {
            yield return new ApplySpecialization( false, Enumerable.Repeat( -2, tensorCount ).ToArray() );
            yield return new ApplySpecialization( false, Enumerable.Repeat( -1, tensorCount ).ToArray() );

            foreach ( int[] combination in CombinationsOf( All32BitTensorDims, tensorCount ) )
            {
                yield return new ApplySpecialization( true, combination );
            }
        }

        private static readonly int[] All32BitTensorDims = new int[] { -2, -1 }; //, 1, 2, 3 };

        private static IEnumerable< T[] > CombinationsOf< T >( T[] possibleValues, int count )
        {
            if ( count < 1 ) throw (new ArgumentOutOfRangeException( "count" ));

            if ( count == 1 )
            {
                foreach ( T item in possibleValues )
                {
                    yield return new T[] { item };
                }
            }
            else
            {
                foreach ( T item in possibleValues )
                {
                    IEnumerable< T[] > restCombinations = CombinationsOf( possibleValues, count - 1 );
                    foreach ( T[] restItems in restCombinations )
                    {
                        var result = new List< T >( count );
                        result.AddRange( restItems );
                        result.Add( item );
                        yield return result.ToArray();
                    }
                }
            }
        }
    }
}
