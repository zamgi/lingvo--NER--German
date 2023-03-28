﻿using System;

namespace Lingvo.NER.NeuralNetwork.Tensors.Cpu.LinearAlgebra
{
    public static class LSAME
    {
        /// <summary>
        /// Purpose
        /// =======
        /// 
        /// LSAME returns .TRUE. if CA is the same letter as CB regardless of
        /// case.
        /// 
        ///</summary>
        /// <param name="CA">
        /// (input) CHARACTER*1
        ///</param>
        /// <param name="CB">
        /// (input) CHARACTER*1
        /// CA and CB specify the single characters to be compared.
        ///</param>
        public static bool Run( string CA, string CB )
        {
            CA = CA.Substring( 0, 1 );
            CB = CB.Substring( 0, 1 );

            #region Prolog
            // *
            // *  -- LAPACK auxiliary routine (version 3.1) --
            // *     Univ. of Tennessee, Univ. of California Berkeley and NAG Ltd..
            // *     November 2006
            // *
            // *     .. Scalar Arguments ..
            // *     ..
            // *
            // *  Purpose
            // *  =======
            // *
            // *  LSAME returns .TRUE. if CA is the same letter as CB regardless of
            // *  case.
            // *
            // *  Arguments
            // *  =========
            // *
            // *  CA      (input) CHARACTER*1
            // *  CB      (input) CHARACTER*1
            // *          CA and CB specify the single characters to be compared.
            // *
            // * =====================================================================
            // *
            // *     .. Intrinsic Functions ..
            //      INTRINSIC          ICHAR;
            // *     ..
            // *     .. Local Scalars ..
            // *     ..
            // *     .. Executable Statements ..
            // *
            // *     Test if the characters are equal
            // *
            #endregion

            #region Body
            bool lsame = CA == CB;
            if ( lsame ) return lsame;
            // *
            // *     Now test for equivalence if both characters are alphabetic.
            // *
            int ZCODE = Convert.ToInt32( 'Z' );
            // *
            // *     Use 'Z' rather than 'A' so that ASCII can be detected on Prime
            // *     machines, on which ICHAR returns a value with bit 8 set.
            // *     ICHAR('A') on Prime machines returns 193 which is the same as
            // *     ICHAR('A') on an EBCDIC machine.
            // *
            int INTA = Convert.ToInt32( Convert.ToChar( CA ) );
            int INTB = Convert.ToInt32( Convert.ToChar( CB ) );
            // *
            if ( ZCODE == 90 || ZCODE == 122 )
            {
                // *
                // *        ASCII is assumed - ZCODE is the ASCII code of either lower or
                // *        upper case 'Z'.
                // *
                if ( INTA >= 97 && INTA <= 122 ) INTA -= 32;
                if ( INTB >= 97 && INTB <= 122 ) INTB -= 32;
                // *
            }
            else
            {
                if ( ZCODE == 233 || ZCODE == 169 )
                {
                    // *
                    // *        EBCDIC is assumed - ZCODE is the EBCDIC code of either lower or
                    // *        upper case 'Z'.
                    // *
                    if ( INTA >= 129 && INTA <= 137 || INTA >= 145 && INTA <= 153 || INTA >= 162 && INTA <= 169 ) INTA += 64;
                    if ( INTB >= 129 && INTB <= 137 || INTB >= 145 && INTB <= 153 || INTB >= 162 && INTB <= 169 ) INTB += 64;
                    // *
                }
                else
                {
                    if ( ZCODE == 218 || ZCODE == 250 )
                    {
                        // *
                        // *        ASCII is assumed, on Prime machines - ZCODE is the ASCII code
                        // *        plus 128 of either lower or upper case 'Z'.
                        // *
                        if ( INTA >= 225 && INTA <= 250 ) INTA -= 32;
                        if ( INTB >= 225 && INTB <= 250 ) INTB -= 32;
                    }
                }
            }
            lsame = INTA == INTB;
            return lsame;

            #endregion

        }
    }
}
