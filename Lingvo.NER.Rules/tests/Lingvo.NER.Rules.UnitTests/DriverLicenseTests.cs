using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.DriverLicenses;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.tests.DriverLicenses
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DriverLicenseTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using var np = CreateNerProcessor();

            //np.AT( "B072RRE2I55" );
            np.AT( "J010000SD51" );
            np.AT( "N0704578035" );
            np.AT( "F0100LQUA01" );
            np.AT( "J430A1RZN11" );
            //np.AT( "B020EN83622" );
            //np.AT( "N8968079S12" );
            np.AT( "F01335916X1" );

        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text ) => np.AT( text, new[] { text.NoWhitespace() } );
        //public static void AT( this NerProcessor np, string text, string s ) => np.AT( text, new[] { s } );
        public static void AT( this NerProcessor np, string text, IList< string > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );

        public static void Check( this IList< word_t > words, IList< string > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NerOutputType.DriverLicense)
                        select ((DriverLicenseWord) w).DriverLicense
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
