﻿using System;
using System.Linq;

using Lingvo.NER.NeuralNetwork.Tensors.Core;

namespace Lingvo.NER.NeuralNetwork.Tensors
{
    /// <summary>
    /// 
    /// </summary>
    public static class Ops
    {
        public static Tensor NewContiguous( Tensor src )
        {
            Tensor result = new Tensor( src.Allocator, src.ElementType, src.Sizes.ToArray() );
            Copy( result, src );
            return result;
        }

        public static Tensor AsContiguous( Tensor src )
        {
            if ( src.IsContiguous() )
            {
                return src.CopyRef();
            }
            else
            {
                return NewContiguous( src );
            }
        }

        public static Tensor Concat( Tensor result, int dimension, params Tensor[] inputs ) => TensorConcatenation.Concat( result, dimension, inputs );

        public static void FillOneHot( Tensor result, int labelCount, int[] labels )
        {
            if ( result.Storage is Cpu.CpuStorage )
            {
                DoFillOneHot( result, labelCount, labels );
            }
            else
            {
                //If the result is not on the CPU, it is much faster to build the tensor on the CPU and then copy
                //An alternative to this would be building a specific GPU kernel for this operation
                Cpu.CpuAllocator cpuAlloc = new Cpu.CpuAllocator();
                using ( Tensor cpuResult = new Tensor( cpuAlloc, result.ElementType, result.Sizes ) )
                {
                    DoFillOneHot( cpuResult, labelCount, labels );
                    Ops.Copy( result, cpuResult );
                }
            }
        }

        private static void DoFillOneHot( Tensor result, int labelCount, int[] labels )
        {
            if ( result.DimensionCount != 2 ) throw (new InvalidOperationException( "result must be a 2D tensor" ));
            if ( result.Sizes[ 0 ] != labels.Length ) throw (new InvalidOperationException( "first dimension of result must equal the number of samples" ));
            if ( result.Sizes[ 1 ] > labelCount ) throw (new InvalidOperationException( "second dimension of result must be at least as large as labelCount" ));

            Ops.Fill( result, 0 );
            for ( int i = 0; i < labels.Length; ++i )
            {
                if ( labels[ i ] < 0 || labels[ i ] >= labelCount )
                {
                    throw (new InvalidOperationException( "label at index " + i + " is out of range 0 <= x < labelCount" ));
                }

                result.SetElementAsFloat( 1.0f, i, labels[ i ] );
            }
        }

        public static void Copy( Tensor result, Tensor src ) => OpRegistry.Invoke( "copy", result, src ); 
        public static void Fill( Tensor result, float value ) => OpRegistry.Invoke( "fill", result, value ); 

        public static Tensor Dot( Tensor result, Tensor lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "dot", result, lhs, rhs ); 
        public static Tensor Addmm( Tensor result, float beta, Tensor src, float alpha, Tensor m1, Tensor m2 ) => (Tensor) OpRegistry.Invoke( "addmm", result, beta, src, alpha, m1, m2 ); 

        public static Tensor AddmmBatch( Tensor result, float beta, Tensor src, float alpha, Tensor m1, Tensor m2 ) => (Tensor) OpRegistry.Invoke( "addmmbatch", result, beta, src, alpha, m1, m2 ); 

        public static Tensor Abs( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "abs", result, src ); 
        public static Tensor Neg( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "neg", result, src ); 
        public static Tensor Sign( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "sign", result, src ); 

        public static Tensor Relu( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "relu", result, src ); 

        public static Tensor ReluD( Tensor result, Tensor w, Tensor g ) => (Tensor) OpRegistry.Invoke( "relud", result, w, g ); 

        public static Tensor AddReluD( Tensor result, Tensor t, Tensor w, Tensor g ) => (Tensor) OpRegistry.Invoke( "addrelud", result, t, w, g ); 

        public static Tensor Sqrt( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "sqrt", result, src ); 

        public static Tensor Rsqrt( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "rsqrt", result, src ); 

        public static Tensor Exp( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "exp", result, src ); 
        public static Tensor Log( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "log", result, src ); 
        public static Tensor Log1p( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "log1p", result, src ); 
        public static Tensor Floor( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "floor", result, src ); 
        public static Tensor Ceil( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "ceil", result, src ); 
        public static Tensor Round( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "round", result, src ); 
        public static Tensor Trunc( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "trunc", result, src ); 
        public static Tensor Frac( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "frac", result, src ); 

        public static Tensor Sin( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "sin", result, src ); 
        public static Tensor Cos( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "cos", result, src ); 
        public static Tensor Tan( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "tan", result, src ); 

        public static Tensor Asin( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "asin", result, src ); 
        public static Tensor Acos( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "acos", result, src ); 
        public static Tensor Atan( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "atan", result, src ); 

        public static Tensor Sinh( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "sinh", result, src ); 
        public static Tensor Cosh( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "cosh", result, src ); 
        public static Tensor Tanh( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "tanh", result, src ); 

        public static Tensor Sigmoid( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "sigmoid", result, src ); 

        public static Tensor AddSigmoidD( Tensor result, Tensor t, Tensor resW, Tensor resG ) => (Tensor) OpRegistry.Invoke( "addsigmoidD", result, t, resW, resG ); 

        public static Tensor AddTanhD( Tensor result, Tensor t, Tensor resW, Tensor resG ) => (Tensor) OpRegistry.Invoke( "addtanhD", result, t, resW, resG ); 

        public static Tensor SigmoidD( Tensor result, Tensor resW, Tensor resG ) => (Tensor) OpRegistry.Invoke( "sigmoidD", result, resW, resG ); 

        public static Tensor TanhD( Tensor result, Tensor resW, Tensor resG ) => (Tensor) OpRegistry.Invoke( "tanhD", result, resW, resG ); 

        public static Tensor AddTanh( Tensor result, Tensor x, Tensor y ) => (Tensor) OpRegistry.Invoke( "addtanh", result, x, y ); 
        public static Tensor AddTanh3( Tensor result, Tensor x, Tensor y, Tensor z ) => (Tensor) OpRegistry.Invoke( "addtanh3", result, x, y, z ); 

        public static Tensor MulMulAdd( Tensor result, Tensor x, Tensor y, Tensor z, Tensor w ) => (Tensor) OpRegistry.Invoke( "mulmuladd", result, x, y, z, w ); 

        public static Tensor AddMul( Tensor result, Tensor x, Tensor y, Tensor z ) => (Tensor) OpRegistry.Invoke( "addmul", result, x, y, z ); 
        public static Tensor AddMulV( Tensor result, Tensor x, Tensor y, float z ) => (Tensor) OpRegistry.Invoke( "addmulv", result, x, y, z ); 

        public static Tensor AddDiv( Tensor result, Tensor x, Tensor y, Tensor z ) => (Tensor) OpRegistry.Invoke( "adddiv", result, x, y, z ); 

        public static Tensor MaskFill( Tensor result, Tensor t, Tensor mask, float defValue ) => (Tensor) OpRegistry.Invoke( "maskfill", result, t, mask, defValue ); 

        public static Tensor Atan2( Tensor result, Tensor srcY, Tensor srcX ) => (Tensor) OpRegistry.Invoke( "atan2", result, srcY, srcX ); 
        public static Tensor Pow( Tensor result, Tensor src, float value ) => (Tensor) OpRegistry.Invoke( "pow", result, src, value ); 
        public static Tensor Tpow( Tensor result, float value, Tensor src ) => (Tensor) OpRegistry.Invoke( "tpow", result, value, src ); 
        public static Tensor Lerp( Tensor result, Tensor srcA, Tensor srcB, float weight ) => (Tensor) OpRegistry.Invoke( "lerp", result, srcA, srcB ); 
        public static Tensor Clamp( Tensor result, Tensor src, float min, float max ) => (Tensor) OpRegistry.Invoke( "clamp", result, src, min, max ); 

        public static Tensor Add( Tensor result, Tensor lhs, float rhs ) => (Tensor) OpRegistry.Invoke( "addv", result, lhs, rhs ); 
        public static Tensor Sub( Tensor result, Tensor lhs, float rhs ) => (Tensor) OpRegistry.Invoke( "subv", result, lhs, rhs ); 
        public static Tensor Sub( Tensor result, float lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "rsubv", result, lhs, rhs ); 
        public static Tensor Mul( Tensor result, Tensor lhs, float rhs ) => (Tensor) OpRegistry.Invoke( "mulv", result, lhs, rhs ); 
        public static Tensor Div( Tensor result, Tensor lhs, float rhs ) => (Tensor) OpRegistry.Invoke( "divv", result, lhs, rhs ); 
        public static Tensor Div( Tensor result, float lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "rdivv", result, lhs, rhs ); 
        public static Tensor Mod( Tensor result, Tensor lhs, float rhs ) => (Tensor) OpRegistry.Invoke( "modv", result, lhs, rhs ); 

        public static Tensor GreaterThan( Tensor result, Tensor lhs, float rhs ) => (Tensor) OpRegistry.Invoke( "gtValue", result, lhs, rhs ); 
        public static Tensor LessThan( Tensor result, Tensor lhs, float rhs ) => (Tensor) OpRegistry.Invoke( "ltValue", result, lhs, rhs ); 
        public static Tensor GreaterOrEqual( Tensor result, Tensor lhs, float rhs ) => (Tensor) OpRegistry.Invoke( "geValue", result, lhs, rhs ); 
        public static Tensor LessOrEqual( Tensor result, Tensor lhs, float rhs ) => (Tensor) OpRegistry.Invoke( "leValue", result, lhs, rhs ); 
        public static Tensor EqualTo( Tensor result, Tensor lhs, float rhs ) => (Tensor) OpRegistry.Invoke( "eqValue", result, lhs, rhs ); 
        public static Tensor NotEqual( Tensor result, Tensor lhs, float rhs ) => (Tensor) OpRegistry.Invoke( "neValue", result, lhs, rhs ); 

        public static Tensor Add( Tensor result, Tensor lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "addt", result, lhs, rhs ); 
        public static Tensor Sub( Tensor result, Tensor lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "subt", result, lhs, rhs ); 
        public static Tensor Mul( Tensor result, Tensor lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "mult", result, lhs, rhs );
        public static Tensor Div( Tensor result, Tensor lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "divt", result, lhs, rhs ); 
        public static Tensor Mod( Tensor result, Tensor lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "modt", result, lhs, rhs ); 

        public static Tensor AtomicAdd( Tensor result, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "atomicadd", result, rhs ); 

        public static Tensor GreaterThan( Tensor result, Tensor lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "gtTensor", result, lhs, rhs ); 
        public static Tensor LessThan( Tensor result, Tensor lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "ltTensor", result, lhs, rhs ); 
        public static Tensor GreaterOrEqual( Tensor result, Tensor lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "geTensor", result, lhs, rhs ); 
        public static Tensor LessOrEqual( Tensor result, Tensor lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "leTensor", result, lhs, rhs ); 
        public static Tensor EqualTo( Tensor result, Tensor lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "eqTensor", result, lhs, rhs ); 
        public static Tensor NotEqual( Tensor result, Tensor lhs, Tensor rhs ) => (Tensor) OpRegistry.Invoke( "neTensor", result, lhs, rhs ); 

        public static Tensor Sum( Tensor result, Tensor src, int dimension ) => (Tensor) OpRegistry.Invoke( "sum", result, src, dimension ); 
        public static Tensor Prod( Tensor result, Tensor src, int dimension ) => (Tensor) OpRegistry.Invoke( "prod", result, src, dimension ); 
        public static Tensor Min( Tensor result, Tensor src, int dimension ) => (Tensor) OpRegistry.Invoke( "min", result, src, dimension ); 
        public static Tensor Max( Tensor result, Tensor src, int dimension ) => (Tensor) OpRegistry.Invoke( "max", result, src, dimension ); 
        public static Tensor Argmin( Tensor result, Tensor src, int dimension ) => (Tensor) OpRegistry.Invoke( "argmin", result, src, dimension ); 
        public static Tensor Argmax( Tensor result, Tensor src, int dimension ) => (Tensor) OpRegistry.Invoke( "argmax", result, src, dimension ); 

        public static Tensor Mean( Tensor result, Tensor src, int dimension ) => (Tensor) OpRegistry.Invoke( "mean", result, src, dimension ); 
        public static Tensor Norm( Tensor result, Tensor src, int dimension, float value ) => (Tensor) OpRegistry.Invoke( "norm", result, src, dimension, value ); 
        public static Tensor Std( Tensor result, Tensor src, int dimension, bool normByN ) => (Tensor) OpRegistry.Invoke( "std", result, src, dimension, normByN ); 
        public static Tensor Var( Tensor result, Tensor src, int dimension, bool normByN ) => (Tensor) OpRegistry.Invoke( "var", result, src, dimension, normByN ); 

        public static Tensor Softmax( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "softmax", result, src ); 
        public static Tensor SoftmaxGrad( Tensor grad, Tensor adj, Tensor val, bool addGrad = true ) => (Tensor) OpRegistry.Invoke( "softmaxgrad", grad, adj, val, addGrad ); 


        public static Tensor IndexSelect( Tensor result, Tensor src, Tensor indice ) => (Tensor) OpRegistry.Invoke( "indexselect", result, src, indice ); 
        public static Tensor IndexSelectGrad( Tensor grad, Tensor adj, Tensor indice ) => (Tensor) OpRegistry.Invoke( "indexselectgrad", grad, adj, indice ); 


        public static Tensor BuildSrcTgtMask( Tensor result, Tensor srcOriginalLengths, Tensor tgtOriginalLengths, int srcPaddedSeqLength, int tgtPaddedSeqLength, float value, float maskedValue )
           => (Tensor) OpRegistry.Invoke( "buildsrctgtmask", result, srcOriginalLengths, tgtOriginalLengths, srcPaddedSeqLength, tgtPaddedSeqLength, value, maskedValue );

        public static Tensor BuildSelfMask( Tensor result, Tensor originalLengths, int paddedSeqLength, float value, float maskedValue )
            => (Tensor) OpRegistry.Invoke( "buildselfmask", result, originalLengths, paddedSeqLength, value, maskedValue );

        public static Tensor BuildSelfTriMask( Tensor result, Tensor originalLengths, int paddedSeqLength, float value, float maskedValue )
            => (Tensor) OpRegistry.Invoke( "buildselftrimask", result, originalLengths, paddedSeqLength, value, maskedValue );

        public static Tensor BuildTriMask( Tensor result, float value, float maskedValue ) => (Tensor) OpRegistry.Invoke( "buildtrimask", result, value, maskedValue );

        public static Tensor LayerNorm( Tensor result, Tensor src, Tensor alpha, Tensor beta, float eps = 1e-09f ) => (Tensor) OpRegistry.Invoke( "layernorm", result, src, alpha, beta, eps ); 
        public static Tensor LayerNormGrad( Tensor outGrad, Tensor alphaGrad, Tensor betaGrad, Tensor inGrad, Tensor y, Tensor x, Tensor alpha, Tensor beta, float eps = 1e-09f )
            => (Tensor) OpRegistry.Invoke( "layernormgrad", outGrad, alphaGrad, betaGrad, inGrad, y, x, alpha, beta, eps );

        public static Tensor AddLayerNorm( Tensor result, Tensor src1, Tensor src2, Tensor alpha, Tensor beta, float eps = 1e-09f ) => (Tensor) OpRegistry.Invoke( "addlayernorm", result, src1, src2, alpha, beta, eps ); 
        public static Tensor AddLayerNormGrad( Tensor out1Grad, Tensor out2Grad, Tensor alphaGrad, Tensor betaGrad, Tensor inGrad, Tensor y, Tensor x1, Tensor x2, Tensor alpha, Tensor beta, float eps = 1e-09f ) => (Tensor) OpRegistry.Invoke( "addlayernormgrad", out1Grad, out2Grad, alphaGrad, betaGrad, inGrad, y, x1, x2, alpha, beta, eps ); 

        public static Tensor Adam( Tensor weight, Tensor gradient, Tensor v, Tensor m, int batchSize, float step_size, float clipval, float regc, float decay_rate_v, float decay_rate_m, int iter, float eps )
            => (Tensor) OpRegistry.Invoke( "adam", weight, gradient, v, m, batchSize, step_size, clipval, regc, decay_rate_v, decay_rate_m, iter, eps );

        public static Tensor RMSProp( Tensor weight, Tensor gradient, Tensor cache, int batchSize, float step_size, float clipval, float regc, float decay_rate, float eps )
            => (Tensor) OpRegistry.Invoke( "rmsprop", weight, gradient, cache, batchSize, step_size, clipval, regc, decay_rate, eps );

        public static Tensor SumAll( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "sumall", result, src ); 
        public static Tensor ProdAll( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "prodall", result, src ); 
        public static Tensor MinAll( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "minall", result, src ); 
        public static Tensor MaxAll( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "maxall", result, src ); 

        public static Tensor MeanAll( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "meanall", result, src ); 
        public static Tensor NormAll( Tensor result, Tensor src, float value ) => (Tensor) OpRegistry.Invoke( "normall", result, src, value ); 
        public static Tensor StdAll( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "stdall", result, src ); 
        public static Tensor VarAll( Tensor result, Tensor src ) => (Tensor) OpRegistry.Invoke( "varall", result, src ); 

        public static float SumAll( Tensor src ) { using ( Tensor resultTensor = SumAll( null, src ) ) { return resultTensor.GetElementAsFloat( 0 ); } }
        public static float ProdAll( Tensor src ) { using ( Tensor resultTensor = ProdAll( null, src ) ) { return resultTensor.GetElementAsFloat( 0 ); } }
        public static float MinAll( Tensor src ) { using ( Tensor resultTensor = MinAll( null, src ) ) { return resultTensor.GetElementAsFloat( 0 ); } }
        public static float MaxAll( Tensor src ) { using ( Tensor resultTensor = MaxAll( null, src ) ) { return resultTensor.GetElementAsFloat( 0 ); } }

        public static float MeanAll( Tensor src ) { using ( Tensor resultTensor = MeanAll( null, src ) ) { return resultTensor.GetElementAsFloat( 0 ); } }
        public static float VarAll( Tensor src ) { using ( Tensor resultTensor = VarAll( null, src ) ) { return resultTensor.GetElementAsFloat( 0 ); } }
        public static float StdAll( Tensor src ) { using ( Tensor resultTensor = StdAll( null, src ) ) { return resultTensor.GetElementAsFloat( 0 ); } }
        public static float NormAll( Tensor src, float value ) { using ( Tensor resultTensor = NormAll( null, src, value ) ) { return resultTensor.GetElementAsFloat( 0 ); } }

        public static Tensor Gather( Tensor result, Tensor src, int dim, Tensor indices ) => (Tensor) OpRegistry.Invoke( "gather", result, src, dim, indices ); 
        public static Tensor Scatter( Tensor result, Tensor src, int dim, Tensor indices ) => (Tensor) OpRegistry.Invoke( "scatter", result, src, dim, indices ); 

        public static Tensor ScatterAdd( Tensor result, Tensor src, int dim, Tensor indices ) => (Tensor) OpRegistry.Invoke( "scatter_add", result, src, dim, indices ); 
        public static Tensor ScatterFill( Tensor result, float value, int dim, Tensor indices ) => (Tensor) OpRegistry.Invoke( "scatter_fill", result, value, dim, indices ); 
    }
}
