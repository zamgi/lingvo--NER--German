using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.PassportIdCardNumbers;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.tests.PassportIdCardNumbers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class PassportIdCardNumberTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using var np = CreateNerProcessor();
            
            np.AT( "C01X00T47 \r\n T22000129", "C01X00T47" );

            np.AT( "C73Z12345" );
            np.AT( "L6Z3PGVYC" ); np.AT( "L6Z3PGVYC3" );
            np.AT( "LF3ZT4WC0" ); //np.AT( "LF3ZT4WC09" );
            np.AT( "Y0GZC137N" ); np.AT( "Y0GZC137N1" );
            np.AT( "C6X4XR5CL" );
            np.AT( "C6Z1LX1KX" ); np.AT( "C6Z1LX1KX0" );
            np.AT( "E8448784" ); //np.AT( "E84487841" );
            np.AT( "G2011464" ); //np.AT( "G20114648" );            
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text ) => np.AT( text, new[] { text } );
        public static void AT( this NerProcessor np, string text, string s ) => np.AT( text, new[] { s } );
        public static void AT( this NerProcessor np, string text, IList< string > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );

        public static void Check( this IList< word_t > words, IList< string > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NerOutputType.PassportIdCardNumber)
                        select ((PassportIdCardNumberWord) w).PassportIdCardNumbers
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
