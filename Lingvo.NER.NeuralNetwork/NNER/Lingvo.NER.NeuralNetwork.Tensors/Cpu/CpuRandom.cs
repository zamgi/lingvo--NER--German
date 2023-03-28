﻿using System;
using System.Reflection;

namespace Lingvo.NER.NeuralNetwork.Tensors.Cpu
{
    /// <summary>
    /// 
    /// </summary>
    [OpsClass]
    public class CpuRandom
    {
        private static readonly Random _SeedGen = new Random();
        public CpuRandom() { }

        // allArgs should start with a null placeholder for the RNG object
        private static void InvokeWithRng( int? seed, MethodInfo method, params object[] allArgs )
        {
            if ( !seed.HasValue )
            {
                seed = _SeedGen.Next();
            }

            NativeWrapper.CheckResult( CpuOpsNative.TS_NewRNG( out IntPtr rng ) );
            NativeWrapper.CheckResult( CpuOpsNative.TS_SetRNGSeed( rng, seed.Value ) );
            allArgs[ 0 ] = rng;
            NativeWrapper.InvokeTypeMatch( method, allArgs );
            NativeWrapper.CheckResult( CpuOpsNative.TS_DeleteRNG( rng ) );
        }

        private readonly MethodInfo uniform_func = NativeWrapper.GetMethod( "TS_RandomUniform" );
        [RegisterOpStorageType("random_uniform", typeof(CpuStorage))] public void Uniform( Tensor result, int? seed, float min, float max ) => InvokeWithRng( seed, uniform_func, null, result, min, max ); 

        private readonly MethodInfo normal_func = NativeWrapper.GetMethod( "TS_RandomNormal" );
        [RegisterOpStorageType("random_normal", typeof(CpuStorage))] public void Normal( Tensor result, int? seed, float mean, float stdv ) => InvokeWithRng( seed, normal_func, null, result, mean, stdv ); 

        private readonly MethodInfo exponential_func = NativeWrapper.GetMethod( "TS_RandomExponential" );
        [RegisterOpStorageType("random_exponential", typeof(CpuStorage))] public void Exponential( Tensor result, int? seed, float lambda ) => InvokeWithRng( seed, exponential_func, null, result, lambda ); 

        private readonly MethodInfo cauchy_func = NativeWrapper.GetMethod( "TS_RandomCauchy" );
        [RegisterOpStorageType("random_cauchy", typeof(CpuStorage))] public void Cauchy( Tensor result, int? seed, float median, float sigma ) => InvokeWithRng( seed, cauchy_func, null, result, median, sigma ); 

        private readonly MethodInfo log_normal_func = NativeWrapper.GetMethod( "TS_RandomLogNormal" );
        [RegisterOpStorageType("random_lognormal", typeof(CpuStorage))] public void LogNormal( Tensor result, int? seed, float mean, float stdv ) => InvokeWithRng( seed, log_normal_func, null, result, mean, stdv ); 

        private readonly MethodInfo geometric_func = NativeWrapper.GetMethod( "TS_RandomGeometric" );
        [RegisterOpStorageType("random_geometric", typeof(CpuStorage))] public void Geometric( Tensor result, int? seed, float p ) => InvokeWithRng( seed, geometric_func, null, result, p ); 

        private readonly MethodInfo bernoulli_func = NativeWrapper.GetMethod( "TS_RandomBernoulli" );
        [RegisterOpStorageType("random_bernoulli", typeof(CpuStorage))] public void Bernoulli( Tensor result, int? seed, float p ) => InvokeWithRng( seed, bernoulli_func, null, result, p ); 
    }
}
