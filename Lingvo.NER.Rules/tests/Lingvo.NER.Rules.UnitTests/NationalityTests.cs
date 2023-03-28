using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.Nationalities;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.tests.Nationalities
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class NationalityTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using var np = CreateNerProcessor();
             
            np.AT( "Staatsangehörigkeit äthiopisch", "äthiopisch" );
            np.EMPTY( "hier is keine Staatsangehörigkeit angegeben" );
        }

        [Fact] public void T_2_Dashes()
        {
            using var np = CreateNerProcessor();
             
            np.AT( "Ethnische Gruppe papua-neuguineisch", "papua-neuguineisch" );
        }

        [Fact] public void T_3_Spaces()
        {
            using var np = CreateNerProcessor();
             
            np.AT( "Political Home democratic republic of the congo", "democratic republic of the congo" );
        }

        [Fact] public void T_4_Colon()
        {
            using var np = CreateNerProcessor();
             
            np.AT( "Political Home: democratic republic of the congo", "democratic republic of the congo" );
        }

        [Fact] public void T_5_CASE_INSENSITIVE()
        {
            using var np = CreateNerProcessor();
             
            np.AT( "Staatsangehörigkeit Äthiopisch", "Äthiopisch" );
            np.AT( "Staatsangehörigkeit ÄTHioPisch", "ÄTHioPisch" );
        }

        [Fact] public void T_6_Distance_from_preamble()
        {
            using var np = CreateNerProcessor();

            const int MAX_DISTANCE_FROM_PREAMBLE = 3;
            var dist_str = "ist ";
            np.AT( "Staatsangehörigkeit " + dist_str + "äthiopisch", "äthiopisch" );

            dist_str = string.Concat( Enumerable.Repeat( "ist ", 2 ) );
            np.AT( "Staatsangehörigkeit " + dist_str + "äthiopisch", "äthiopisch" );

            dist_str = string.Concat( Enumerable.Repeat( "ist ", MAX_DISTANCE_FROM_PREAMBLE + 1 ) );
            np.EMPTY( "Staatsangehörigkeit " + dist_str + "äthiopisch" );
        }

        [Fact] public void T_7_NormalizedUmlates()
        {
            using var np = CreateNerProcessor();
            
            np.AT( "Staatsangehoerigkeit aethiopisch", "aethiopisch" );
            np.AT( "Staatsangehoerigkeit aEthiopisch", "aEthiopisch" );
        }

        [Fact] public void T_8_Many_in_one()
        {
            using var np = CreateNerProcessor();
            
            np.AT( @"Staatsangehörigkeit äthiopisch
                    Ethnische Gruppe papua-neuguineisch
                    Political Home democratic republic of the congo
                    Political Home: democratic republic of the congo",
            new[] { "äthiopisch", "papua-neuguineisch", "democratic republic of the congo", "democratic republic of the congo" } );
            
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
                        where (w.nerOutputType == NerOutputType.Nationality)
                        select ((NationalityWord) w).Nationality
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
