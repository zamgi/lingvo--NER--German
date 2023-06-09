﻿using System.Runtime.ConstrainedExecution;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;
using R = System.Runtime.ConstrainedExecution.ReliabilityContractAttribute;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    internal static class PrimeHelper
    {
        private static readonly int[] PRIMES = new[]
        {
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861,
            5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437, 187751, 225307, 270371,
            324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369,
        };

        [M(O.AggressiveInlining)] public static int ExpandPrime4Size( int oldSize )
        {
            int newSize = oldSize << 1;
            if ( ((uint) newSize > (uint) 0x7feffffd) && ((int) 0x7feffffd > oldSize) )
            {
                return ((int) 0x7feffffd);
            }
            return (GetPrime( newSize ));
        }

        [M(O.AggressiveInlining), R(Consistency.WillNotCorruptState, Cer.Success)]
        public static int GetPrime( int min )
        {
            if ( min < 0 ) throw (new ArgumentException( nameof(min) ));

            for ( var i = 0; i < PRIMES.Length; i++ )
            {
                var p = PRIMES[ i ];
                if ( min <= p )
                {
                    return (p);
                }
            }
            for ( var j = min | 1; j < int.MaxValue; j += 2 )
            {
                if ( IsPrime( j ) && ((j - 1) % 101 != 0) )
                {
                    return (j);
                }
            }
            return (min);
        }

        [M(O.AggressiveInlining), R(Consistency.WillNotCorruptState, Cer.Success)]
        public static int GetPrimeCloser( int min )
        {
            if ( min < 0 ) throw (new ArgumentException( nameof(min) ));

            for ( var j = (min | 1); j < int.MaxValue; j += 2 )
            {
                if ( IsPrime( j ) && ((j - 1) % 101 != 0) )
                {
                    return (j);
                }
            }
            return (min);
        }

        [M(O.AggressiveInlining), R(Consistency.WillNotCorruptState, Cer.Success)]
        public static bool IsPrime( int candidate )
        {
            if ( (candidate & 1) != 0 )
            {
                var n = (int) Math.Sqrt( candidate );
                for ( int i = 3; i <= n; i += 2 )
                {
                    if ( (candidate % i) == 0 )
                    {
                        return (false);
                    }
                }
                return (true);
            }
            return (candidate == 2);
        }
    }
}
