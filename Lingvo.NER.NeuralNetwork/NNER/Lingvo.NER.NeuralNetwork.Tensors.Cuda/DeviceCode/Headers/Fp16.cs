﻿using Lingvo.NER.NeuralNetwork.Tensors.Cuda.RuntimeCompiler;

namespace Lingvo.NER.NeuralNetwork.Tensors.Cuda.DeviceCode.Headers
{
    /// <summary>
    /// 
    /// </summary>
    [CudaInclude("Code", "Fp16")]
    public static class Fp16
    {
        public static readonly string Code = @"
typedef struct __align__(2) {
   unsigned short x;
} __half;
typedef __half half;
#define FP16_FUNC static __device__ __inline__
FP16_FUNC __half __float2half(const float a);
FP16_FUNC float __half2float(const __half a);

";

    }
}
