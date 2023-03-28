﻿using System;
using System.Numerics;

namespace Lingvo.NER.NeuralNetwork.Tensors.Cpu.LinearAlgebra
{
    public static class SGEMM
    {
        private const float ONE = 1.0f;
        private const float ZERO = 0.0f;
    
        /// <summary>
        /// Purpose
        /// =======
        /// 
        /// SGEMM  performs one of the matrix-matrix operations
        /// 
        /// C := alpha*op( A )*op( B ) + beta*C,
        /// 
        /// where  op( X ) is one of
        /// 
        /// op( X ) = X   or   op( X ) = X',
        /// 
        /// alpha and beta are scalars, and A, B and C are matrices, with op( A )
        /// an m by k matrix,  op( B )  a  k by n matrix and  C an m by n matrix.
        /// 
        ///</summary>
        /// <param name="TRANSA">
        /// - CHARACTER*1.
        /// On entry, TRANSA specifies the form of op( A ) to be used in
        /// the matrix multiplication as follows:
        /// 
        /// TRANSA = 'N' or 'n',  op( A ) = A.
        /// 
        /// TRANSA = 'T' or 't',  op( A ) = A'.
        /// 
        /// TRANSA = 'C' or 'c',  op( A ) = A'.
        /// 
        /// Unchanged on exit.
        ///</param>
        /// <param name="TRANSB">
        /// - CHARACTER*1.
        /// On entry, TRANSB specifies the form of op( B ) to be used in
        /// the matrix multiplication as follows:
        /// 
        /// TRANSB = 'N' or 'n',  op( B ) = B.
        /// 
        /// TRANSB = 'T' or 't',  op( B ) = B'.
        /// 
        /// TRANSB = 'C' or 'c',  op( B ) = B'.
        /// 
        /// Unchanged on exit.
        ///</param>
        /// <param name="M">
        /// - INTEGER.
        /// On entry,  M  specifies  the number  of rows  of the  matrix
        /// op( A )  and of the  matrix  C.  M  must  be at least  zero.
        /// Unchanged on exit.
        ///</param>
        /// <param name="N">
        /// - INTEGER.
        /// On entry,  N  specifies the number  of columns of the matrix
        /// op( B ) and the number of columns of the matrix C. N must be
        /// at least zero.
        /// Unchanged on exit.
        ///</param>
        /// <param name="K">
        /// - INTEGER.
        /// On entry,  K  specifies  the number of columns of the matrix
        /// op( A ) and the number of rows of the matrix op( B ). K must
        /// be at least  zero.
        /// Unchanged on exit.
        ///</param>
        /// <param name="ALPHA">
        /// and beta are scalars, and A, B and C are matrices, with op( A )
        ///</param>
        /// <param name="A">
        /// - DOUBLE PRECISION array of DIMENSION ( LDA, ka ), where ka is
        /// k  when  TRANSA = 'N' or 'n',  and is  m  otherwise.
        /// Before entry with  TRANSA = 'N' or 'n',  the leading  m by k
        /// part of the array  A  must contain the matrix  A,  otherwise
        /// the leading  k by m  part of the array  A  must contain  the
        /// matrix A.
        /// Unchanged on exit.
        ///</param>
        /// <param name="LDA">
        /// - INTEGER.
        /// On entry, LDA specifies the first dimension of A as declared
        /// in the calling (sub) program. When  TRANSA = 'N' or 'n' then
        /// LDA must be at least  max( 1, m ), otherwise  LDA must be at
        /// least  max( 1, k ).
        /// Unchanged on exit.
        ///</param>
        /// <param name="B">
        /// - DOUBLE PRECISION array of DIMENSION ( LDB, kb ), where kb is
        /// n  when  TRANSB = 'N' or 'n',  and is  k  otherwise.
        /// Before entry with  TRANSB = 'N' or 'n',  the leading  k by n
        /// part of the array  B  must contain the matrix  B,  otherwise
        /// the leading  n by k  part of the array  B  must contain  the
        /// matrix B.
        /// Unchanged on exit.
        ///</param>
        /// <param name="LDB">
        /// - INTEGER.
        /// On entry, LDB specifies the first dimension of B as declared
        /// in the calling (sub) program. When  TRANSB = 'N' or 'n' then
        /// LDB must be at least  max( 1, k ), otherwise  LDB must be at
        /// least  max( 1, n ).
        /// Unchanged on exit.
        ///</param>
        /// <param name="BETA">
        /// - DOUBLE PRECISION.
        /// On entry,  BETA  specifies the scalar  beta.  When  BETA  is
        /// supplied as zero then C need not be set on input.
        /// Unchanged on exit.
        ///</param>
        /// <param name="C">
        /// := alpha*op( A )*op( B ) + beta*C,
        ///</param>
        /// <param name="LDC">
        /// - INTEGER.
        /// On entry, LDC specifies the first dimension of C as declared
        /// in  the  calling  (sub)  program.   LDC  must  be  at  least
        /// max( 1, m ).
        /// Unchanged on exit.
        /// 
        ///</param>
        unsafe public static void Run( string TRANSA, string TRANSB, int M, int N, int K, float ALPHA, float* A, int LDA, float* B, int LDB, float BETA, float* C, int LDC )
        {
            float TEMP = 0; int I = 0; int INFO = 0; int J = 0; int L = 0; int NCOLA = 0; int NROWA = 0; int NROWB = 0;
            bool NOTA = false; bool NOTB = false;
            int C_J = 0; int A_L = 0; int A_I = 0; int B_J = 0;
            int o_a = -1 - LDA; int o_b = -1 - LDB; int o_c = -1 - LDC;

            TRANSA = TRANSA.Substring( 0, 1 ); 
            TRANSB = TRANSB.Substring( 0, 1 );

            #region Prolog
            // *     .. Scalar Arguments ..
            // *     ..
            // *     .. Array Arguments ..
            // *     ..
            // *
            // *  Purpose
            // *  =======
            // *
            // *  SGEMM  performs one of the matrix-matrix operations
            // *
            // *     C := alpha*op( A )*op( B ) + beta*C,
            // *
            // *  where  op( X ) is one of
            // *
            // *     op( X ) = X   or   op( X ) = X',
            // *
            // *  alpha and beta are scalars, and A, B and C are matrices, with op( A )
            // *  an m by k matrix,  op( B )  a  k by n matrix and  C an m by n matrix.
            // *
            // *  Arguments
            // *  ==========
            // *
            // *  TRANSA - CHARACTER*1.
            // *           On entry, TRANSA specifies the form of op( A ) to be used in
            // *           the matrix multiplication as follows:
            // *
            // *              TRANSA = 'N' or 'n',  op( A ) = A.
            // *
            // *              TRANSA = 'T' or 't',  op( A ) = A'.
            // *
            // *              TRANSA = 'C' or 'c',  op( A ) = A'.
            // *
            // *           Unchanged on exit.
            // *
            // *  TRANSB - CHARACTER*1.
            // *           On entry, TRANSB specifies the form of op( B ) to be used in
            // *           the matrix multiplication as follows:
            // *
            // *              TRANSB = 'N' or 'n',  op( B ) = B.
            // *
            // *              TRANSB = 'T' or 't',  op( B ) = B'.
            // *
            // *              TRANSB = 'C' or 'c',  op( B ) = B'.
            // *
            // *           Unchanged on exit.
            // *
            // *  M      - INTEGER.
            // *           On entry,  M  specifies  the number  of rows  of the  matrix
            // *           op( A )  and of the  matrix  C.  M  must  be at least  zero.
            // *           Unchanged on exit.
            // *
            // *  N      - INTEGER.
            // *           On entry,  N  specifies the number  of columns of the matrix
            // *           op( B ) and the number of columns of the matrix C. N must be
            // *           at least zero.
            // *           Unchanged on exit.
            // *
            // *  K      - INTEGER.
            // *           On entry,  K  specifies  the number of columns of the matrix
            // *           op( A ) and the number of rows of the matrix op( B ). K must
            // *           be at least  zero.
            // *           Unchanged on exit.
            // *
            // *  ALPHA  - DOUBLE PRECISION.
            // *           On entry, ALPHA specifies the scalar alpha.
            // *           Unchanged on exit.
            // *
            // *  A      - DOUBLE PRECISION array of DIMENSION ( LDA, ka ), where ka is
            // *           k  when  TRANSA = 'N' or 'n',  and is  m  otherwise.
            // *           Before entry with  TRANSA = 'N' or 'n',  the leading  m by k
            // *           part of the array  A  must contain the matrix  A,  otherwise
            // *           the leading  k by m  part of the array  A  must contain  the
            // *           matrix A.
            // *           Unchanged on exit.
            // *
            // *  LDA    - INTEGER.
            // *           On entry, LDA specifies the first dimension of A as declared
            // *           in the calling (sub) program. When  TRANSA = 'N' or 'n' then
            // *           LDA must be at least  max( 1, m ), otherwise  LDA must be at
            // *           least  max( 1, k ).
            // *           Unchanged on exit.
            // *
            // *  B      - DOUBLE PRECISION array of DIMENSION ( LDB, kb ), where kb is
            // *           n  when  TRANSB = 'N' or 'n',  and is  k  otherwise.
            // *           Before entry with  TRANSB = 'N' or 'n',  the leading  k by n
            // *           part of the array  B  must contain the matrix  B,  otherwise
            // *           the leading  n by k  part of the array  B  must contain  the
            // *           matrix B.
            // *           Unchanged on exit.
            // *
            // *  LDB    - INTEGER.
            // *           On entry, LDB specifies the first dimension of B as declared
            // *           in the calling (sub) program. When  TRANSB = 'N' or 'n' then
            // *           LDB must be at least  max( 1, k ), otherwise  LDB must be at
            // *           least  max( 1, n ).
            // *           Unchanged on exit.
            // *
            // *  BETA   - DOUBLE PRECISION.
            // *           On entry,  BETA  specifies the scalar  beta.  When  BETA  is
            // *           supplied as zero then C need not be set on input.
            // *           Unchanged on exit.
            // *
            // *  C      - DOUBLE PRECISION array of DIMENSION ( LDC, n ).
            // *           Before entry, the leading  m by n  part of the array  C must
            // *           contain the matrix  C,  except when  beta  is zero, in which
            // *           case C need not be set on entry.
            // *           On exit, the array  C  is overwritten by the  m by n  matrix
            // *           ( alpha*op( A )*op( B ) + beta*C ).
            // *
            // *  LDC    - INTEGER.
            // *           On entry, LDC specifies the first dimension of C as declared
            // *           in  the  calling  (sub)  program.   LDC  must  be  at  least
            // *           max( 1, m ).
            // *           Unchanged on exit.
            // *
            // *
            // *  Level 3 Blas routine.
            // *
            // *  -- Written on 8-February-1989.
            // *     Jack Dongarra, Argonne National Laboratory.
            // *     Iain Duff, AERE Harwell.
            // *     Jeremy Du Croz, Numerical Algorithms Group Ltd.
            // *     Sven Hammarling, Numerical Algorithms Group Ltd.
            // *
            // *
            // *     .. External Functions ..
            // *     ..
            // *     .. External Subroutines ..
            // *     ..
            // *     .. Intrinsic Functions ..
            //      INTRINSIC MAX;
            // *     ..
            // *     .. Local Scalars ..
            // *     ..
            // *     .. Parameters ..
            // *     ..
            // *
            // *     Set  NOTA  and  NOTB  as  true if  A  and  B  respectively are not
            // *     transposed and set  NROWA, NCOLA and  NROWB  as the number of rows
            // *     and  columns of  A  and the  number of  rows  of  B  respectively.
            // *
            #endregion

            #region Body
            NOTA = LSAME.Run( TRANSA, "N" );
            NOTB = LSAME.Run( TRANSB, "N" );
            if ( NOTA )
            {
                NROWA = M;
                NCOLA = K;
            }
            else
            {
                NROWA = K;
                NCOLA = M;
            }
            if ( NOTB )
            {
                NROWB = K;
            }
            else
            {
                NROWB = N;
            }
            // *
            // * Test the input parameters.
            // *
            INFO = 0;
            if ( (!NOTA) && (!LSAME.Run( TRANSA, "C" )) && (!LSAME.Run( TRANSA, "T" )) )
            {
                INFO = 1;
            }
            else
            {
                if ( (!NOTB) && (!LSAME.Run( TRANSB, "C" )) && (!LSAME.Run( TRANSB, "T" )) )
                {
                    INFO = 2;
                }
                else
                {
                    if ( M < 0 )
                    {
                        INFO = 3;
                    }
                    else
                    {
                        if ( N < 0 )
                        {
                            INFO = 4;
                        }
                        else
                        {
                            if ( K < 0 )
                            {
                                INFO = 5;
                            }
                            else
                            {
                                if ( LDA < Math.Max( 1, NROWA ) )
                                {
                                    INFO = 8;
                                }
                                else
                                {
                                    if ( LDB < Math.Max( 1, NROWB ) )
                                    {
                                        INFO = 10;
                                    }
                                    else
                                    {
                                        if ( LDC < Math.Max( 1, M ) )
                                        {
                                            INFO = 13;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if ( INFO != 0 )
            {
                return;
            }
            // *
            // * Quick return if possible.
            // *
            if ( (M == 0) || (N == 0) || (((ALPHA == ZERO) || (K == 0)) && (BETA == ONE)) ) return;
            // *
            // * And if  alpha.eq.zero.
            // *
            if ( ALPHA == ZERO )
            {
                if ( BETA == ZERO )
                {
                    for ( J = 1; J <= N; J++ )
                    {
                        C_J = J * LDC + o_c;
                        for ( I = 1; I <= M; I++ )
                        {
                            C[ I + C_J ] = ZERO;
                        }
                    }
                }
                else
                {
                    for ( J = 1; J <= N; J++ )
                    {
                        C_J = J * LDC + o_c;
                        for ( I = 1; I <= M; I++ )
                        {
                            C[ I + C_J ] *= BETA;
                        }
                    }
                }
                return;
            }
            // *
            // * Start the operations.
            // *
            if ( NOTB )
            {
                if ( NOTA )
                {
                    // *
                    // * Form  C := alpha*A*B + beta*C.
                    // *
                    for ( J = 1; J <= N; J++ )
                    {
                        if ( BETA == ZERO )
                        {
                            C_J = J * LDC + o_c;

                            var spanC = new Span<float>( C + C_J + 1, M );
                            spanC.Fill( 0.0f );
                        }
                        else
                        {
                            if ( BETA != ONE )
                            {
                                C_J = J * LDC + o_c;

                                var spanC      = new Span<float>( C + C_J, M );
                                int vectorSize = Vector<float>.Count;
                                for ( I = 1; I <= M - vectorSize; I += vectorSize )
                                {
                                    var vecC = new Vector<float>( spanC.Slice( I ) );
                                    vecC *= BETA;
                                    vecC.CopyTo( spanC.Slice( I ) );
                                }
                                for ( ; I <= M; I++ )
                                {
                                    C[ I + C_J ] *= BETA;
                                }
                            }
                        }
                        for ( L = 1; L <= K; L++ )
                        {
                            if ( B[ L + J * LDB + o_b ] != ZERO )
                            {
                                TEMP = ALPHA * B[ L + J * LDB + o_b ];
                                C_J = J * LDC + o_c;
                                A_L = L * LDA + o_a;

                                var spanC = new Span<float>( C + C_J, M );
                                var spanA = new Span<float>( A + A_L, M );

                                int vectorSize = Vector<float>.Count;
                                for ( I = 1; I <= M - vectorSize; I += vectorSize )
                                {
                                    var vecC = new Vector<float>( spanC.Slice( I ) );
                                    var vecA = new Vector<float>( spanA.Slice( I ) );
                                    vecC += TEMP * vecA;
                                    vecC.CopyTo( spanC.Slice( I ) );
                                }

                                for ( ; I <= M; I++ )
                                {
                                    C[ I + C_J ] += TEMP * A[ I + A_L ];
                                }

                            }
                        }
                    }
                }
                else
                {
                    // *
                    // * Form  C := alpha*A'*B + beta*C
                    // *
                    for ( J = 1; J <= N; J++ )
                    {
                        for ( I = 1; I <= M; I++ )
                        {
                            TEMP = ZERO;
                            A_I = I * LDA + o_a;
                            B_J = J * LDB + o_b;

                            var spanA = new Span<float>( A + A_I, K );
                            var spanB = new Span<float>( B + B_J, K );

                            int vectorSize = Vector<float>.Count;
                            for ( L = 1; L <= K - vectorSize; L += vectorSize )
                            {
                                var vecA = new Vector<float>( spanA.Slice( L ) );
                                var vecB = new Vector<float>( spanB.Slice( L ) );
                                TEMP += Vector.Dot( vecA, vecB );
                            }
                            for ( ; L <= K; L++ )
                            {
                                TEMP += A[ L + A_I ] * B[ L + B_J ];
                            }

                            if ( BETA == ZERO )
                            {
                                C[ I + J * LDC + o_c ] = ALPHA * TEMP;
                            }
                            else
                            {
                                C[ I + J * LDC + o_c ] = ALPHA * TEMP + BETA * C[ I + J * LDC + o_c ];
                            }
                        }
                    }
                }
            }
            else
            {
                if ( NOTA )
                {
                    // *
                    // * Form  C := alpha*A*B' + beta*C
                    // *
                    for ( J = 1; J <= N; J++ )
                    {
                        if ( BETA == ZERO )
                        {
                            C_J = J * LDC + o_c;

                            var spanC = new Span<float>( C + C_J + 1, M );
                            spanC.Fill( 0.0f );
                        }
                        else
                        {
                            if ( BETA != ONE )
                            {
                                C_J = J * LDC + o_c;

                                var spanC = new Span<float>( C + C_J, M );
                                int vectorSize = Vector<float>.Count;
                                for ( I = 1; I <= M - vectorSize; I += vectorSize )
                                {
                                    var vecC = new Vector<float>( spanC.Slice( I ) );
                                    vecC *= BETA;
                                    vecC.CopyTo( spanC.Slice( I ) );
                                }
                                for ( ; I <= M; I++ )
                                {
                                    C[ I + C_J ] *= BETA;
                                }
                            }
                        }
                        for ( L = 1; L <= K; L++ )
                        {
                            if ( B[ J + L * LDB + o_b ] != ZERO )
                            {
                                TEMP = ALPHA * B[ J + L * LDB + o_b ];
                                C_J = J * LDC + o_c;
                                A_L = L * LDA + o_a;

                                var spanC = new Span<float>( C + C_J, M );
                                var spanA = new Span<float>( A + A_L, M );

                                int vectorSize = Vector<float>.Count;
                                for ( I = 1; I <= M - vectorSize; I += vectorSize )
                                {
                                    var vecC = new Vector<float>( spanC.Slice( I ) );
                                    var vecA = new Vector<float>( spanA.Slice( I ) );
                                    vecC += TEMP * vecA;
                                    vecC.CopyTo( spanC.Slice( I ) );
                                }

                                for ( ; I <= M; I++ )
                                {
                                    C[ I + C_J ] += TEMP * A[ I + A_L ];
                                }
                            }
                        }
                    }
                }
                else
                {
                    // *
                    // * Form  C := alpha*A'*B' + beta*C
                    // *
                    for ( J = 1; J <= N; J++ )
                    {
                        for ( I = 1; I <= M; I++ )
                        {
                            TEMP = ZERO;
                            A_I = I * LDA + o_a;
                            for ( L = 1; L <= K; L++ )
                            {
                                TEMP += A[ L + A_I ] * B[ J + L * LDB + o_b ];
                            }
                            if ( BETA == ZERO )
                            {
                                C[ I + J * LDC + o_c ] = ALPHA * TEMP;
                            }
                            else
                            {
                                C[ I + J * LDC + o_c ] = ALPHA * TEMP + BETA * C[ I + J * LDC + o_c ];
                            }
                        }
                    }
                }
            }
            #endregion
        }
    }
}
