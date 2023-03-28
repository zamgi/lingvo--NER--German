using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.HealthInsurances;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.tests.HealthInsurances
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class HealthInsuranceTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using var np = CreateNerProcessor();

            np.AT( "I526064554" );
            np.AT( "A123456780" );
            np.AT( "K734027627" );
            np.AT( "Z610573490" );
            np.AT( "Q327812091" );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text ) => np.AT( text, new[] { text.NoWhitespace() } );
        public static void AT( this NerProcessor np, string text, IList< string > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );

        public static void Check( this IList< word_t > words, IList< string > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NerOutputType.HealthInsurance)
                        select ((HealthInsuranceWord) w).HealthInsuranceNumber
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
