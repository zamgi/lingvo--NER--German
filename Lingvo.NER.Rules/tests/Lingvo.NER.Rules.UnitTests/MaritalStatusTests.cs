using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.MaritalStatuses;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.tests.MaritalStatuses
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MaritalStatusTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using var np = CreateNerProcessor();

            np.AT( "Familienstand verheiratet", "verheiratet" );
            np.EMPTY( "hier is kein Familienstand angegeben" );
        }
        
        [Fact] public void T_2_Spaces()
        {
            using var np = CreateNerProcessor();

            np.AT( "Beziehungsstatus eingetragene Lebenspartnerschaft", "eingetragene Lebenspartnerschaft" );
        }

        [Fact] public void T_3_Punktuation()
        {
            using var np = CreateNerProcessor();

            np.AT( "Beziehungsverhältnis Lebens- und Einstandsgemeinschaft", "Lebens-und Einstandsgemeinschaft" );
        }

        [Fact] public void T_4_Colon()
        {
            using var np = CreateNerProcessor();

            np.AT( "Civil status: living together with the life partner", "living together with the life partner" );
        }

        [Fact] public void T_5_CASE_INSENSITIVE()
        {
            using var np = CreateNerProcessor();

            np.AT( "FamilienStand VerHeiratet", "VerHeiratet" );
            np.AT( "FAMILIENSTAND VERHEIRATET", "VERHEIRATET" );
        }

        [Fact] public void T_6_Distance_from_preamble()
        {
            using var np = CreateNerProcessor();

            const int MAX_DISTANCE_FROM_PREAMBLE = 3;
            const string DIST_STR = "ist ";
            np.AT( "Familienstand " + DIST_STR + "verheiratet", "verheiratet" );

            var dist_str = string.Concat( Enumerable.Repeat( DIST_STR, 2 ) );
            np.AT( "Familienstand " + dist_str + "verheiratet", "verheiratet" );

            dist_str = string.Concat( Enumerable.Repeat( DIST_STR, MAX_DISTANCE_FROM_PREAMBLE + 1 ) );
            np.EMPTY( "Familienstand " + dist_str + "verheiratet" );
        }
        [Fact] public void T_7_YesNoCases()
        {
            using var np = CreateNerProcessor();

            np.AT( "Verheiratet ja", "ja" );
            np.AT( "Verheiratet nein", "nein" );
            // Colon
            np.AT( "Verheiratet: ja", "ja" );
            np.AT( "Verheiratet: nein", "nein" );
            // Distance
            np.AT( "Verheiratet man ja", "ja" );
            np.AT( "Verheiratet man nein", "nein" );

            np.AT( "Married no", "no" );
        }

        [Fact] public void T_8_NormalizedUmlates()
        {
            using var np = CreateNerProcessor();

            np.AT( "Beziehungsverhaeltnis durch Tod aufgeloeste Lebenspartnerschaft", "durch Tod aufgeloeste Lebenspartnerschaft" );
        }

        [Fact] public void T_9_Many_in_one()
        {
            using var np = CreateNerProcessor();
             
            np.AT(@"Familienstand verheiratet
                    Beziehungsstatus eingetragene Lebenspartnerschaft
                    Verheiratet ja
                    Civil status: living together with the life partner",
            new[] { "verheiratet",
                    "eingetragene Lebenspartnerschaft",
                    "ja",
                    "living together with the life partner"
                    });
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
                        where (w.nerOutputType == NerOutputType.MaritalStatus)
                        select ((MaritalStatusWord)w).MaritalStatus
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
