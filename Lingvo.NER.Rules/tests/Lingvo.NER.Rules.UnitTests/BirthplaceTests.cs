using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.Birthplaces;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.tests.Birthplaces
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class BirthplaceTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using var np = CreateNerProcessor();
             
            np.AT( "Geburtsort Lüdinghausen", "Lüdinghausen" );
            np.EMPTY( "hier is kein Geburtsort angegeben" );
        }

        [Fact] public void T_2_Dashes()
        {
            using var np = CreateNerProcessor();
         
            np.AT( "Ursprungsort Garmisch-Partenkirchen", "Garmisch-Partenkirchen" );
        }

        [Fact] public void T_3_Spaces()
        {
            using var np = CreateNerProcessor();
         
            np.AT( "Geburtsort Freiburg im Breisgau", "Freiburg im Breisgau" );
        }

        [Fact] public void T_4_Brackets()
        {
            using var np = CreateNerProcessor();
             
            np.AT( "P.O.B. Rotenburg (Wümme)", "Rotenburg (Wümme)" );
            np.AT( "P.O.B. Rotenburg ( Wümme )", "Rotenburg (Wümme)" );
        }

        [Fact] public void T_5_Slashes()
        {
            using var np = CreateNerProcessor();
         
            np.AT( "P.O.B. Oelsnitz / Vogtland", "Oelsnitz/Vogtland" );
            np.AT( "P.O.B. Oelsnitz /Vogtland", "Oelsnitz/Vogtland" );
            np.AT( "P.O.B. Oelsnitz  /  Vogtland", "Oelsnitz/Vogtland" );
        }

        [Fact] public void T_6_First_Of_Many_Words()
        {
            using var np = CreateNerProcessor();

            np.AT( "P.O.B. Oelsnitz", "Oelsnitz" );
            np.AT( "P.O.B. Rotenburg", "Rotenburg" );
            np.AT( "Geburtsort Freiburg im", "Freiburg im" );
        }

        [Fact] public void T_7_Colon()
        {
            using var np = CreateNerProcessor();
             
            np.AT( "Geburtsort: Freiburg im Breisgau", "Freiburg im Breisgau" );
        }

        [Fact] public void T_8_CASE_INSENSITIVE()
        {
            using var np = CreateNerProcessor();
         
            np.AT( "Geburtsort Freiburg IM Breisgau", "Freiburg IM Breisgau" );
            np.AT( "Geburtsort FreiBurg im Breisgau", "FreiBurg im Breisgau" );
        }

        [Fact] public void T_9_Distance_from_preamble()
        {
            using var np = CreateNerProcessor();

            const int    MAX_DISTANCE_FROM_PREAMBLE = 3;
            const string DIST_STR                   = "im ";

            np.AT( "Geburtsort " + DIST_STR + "Freiburg im Breisgau", "Freiburg im Breisgau" );

            var dist_str = string.Concat( Enumerable.Repeat( DIST_STR, 2 ) );
            np.AT( "Geburtsort " + dist_str + "Freiburg im Breisgau", "Freiburg im Breisgau" );

            dist_str = string.Concat( Enumerable.Repeat( DIST_STR, MAX_DISTANCE_FROM_PREAMBLE + 1 ) );
            np.EMPTY( "Geburtsort " + dist_str + "Freiburg im Breisgau" );
        }
        [Fact] public void T_10_NormalizedUmlates()
        {
            using var np = CreateNerProcessor();
             
            np.AT( "Geburtsort Luedinghausen", "Luedinghausen" );
        }

        [Fact] public void T_11_Many_In_One()
        {
            using var np = CreateNerProcessor();
             
            np.AT( @"Geburtsort Lüdinghausen
                     P.O.B. Rotenburg (Wümme)
                     Ursprungsort Garmisch-Partenkirchen
                     Place Of Birth: Freiburg IM Breisgau
                     P.O.B. Oelsnitz / Vogtland",
            new[] { "Lüdinghausen",
                    "Rotenburg (Wümme)",
                    "Garmisch-Partenkirchen",
                    "Freiburg IM Breisgau",
                    "Oelsnitz/Vogtland"
                  } );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text, string p ) => np.AT( text, new[] { p } );
        public static void AT( this NerProcessor np, string text, IList< string > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );
        public static void EMPTY( this NerProcessor np, string text ) => Assert.True( !np.Run_UseSimpleSentsAllocate_v1( text ).Any() );

        public static void Check( this IList< word_t > words, IList< string > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NerOutputType.Birthplace)
                        select ((BirthplaceWord) w).Birthplace
                       ).ToArray();

            var startIndex = 0;
            foreach ( var p in refs )
            {
                startIndex = hyps.IndexOf( p, startIndex );
                if ( startIndex == -1 )
                {
                    Assert.True( false );
                }
                startIndex++;
            }
            Assert.True( 0 < startIndex );
        }
        private static int IndexOf( this IList< string > pairs, string s, int startIndex )
        {
            for ( var len = pairs.Count; startIndex < len; startIndex++ )
            {
                if ( IsEqual( s, pairs[ startIndex ] ) )
                {
                    return (startIndex);
                }
            }
            return (-1);
        }
        private static bool IsEqual( string x, string y ) => (x == y);
    }
}
