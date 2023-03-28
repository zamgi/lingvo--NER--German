using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.CarNumbers;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.tests.CarNumbers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CarNumberTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using ( var np = CreateNerProcessor() )
            {
                np.AT( "MH-MT 2105" );
                np.AT( "MH-MT-2105" );
                np.AT( "MH MT 2105" );
                np.AT( "MH-MT2105" );
                np.AT( "MH MT2105" );
                np.AT( "MH MT-2105" );
                np.AT( "D-KA1234" );
                np.AT( "D-KA-8136" );
                np.AT( "D - KA - 8136" );
                np.AT( "D KA 8136" );
                //np.AT( "DKA8136" );
                //np.AT( "HRB 5919" );
                np.AT( "Found: D-TW-1895 \r\n Found: MH-MT-1909 \r\n Found: HSK-SG-123", new[] { "D-TW-1895", "MH-MT-1909", "HSK-SG-123" } );
                np.AT( "Not Found: M-JK-14 \r\n Not Found: ME-J-1 \r\n Not Found: B-AM-1", new[] { "M-JK-14", "ME-J-1", "B-AM-1" } );
                np.AT( "Not Found: HSK-SG-32E", "HSK-SG-32E" );
                np.AT( "Not Found: HSK-SG-32H", "HSK-SG-32H" );
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text ) => np.AT( text, new[] { text.NoWhitespace() } );
        public static void AT( this NerProcessor np, string text, string s ) => np.AT( text, new[] { s } );
        public static void AT( this NerProcessor np, string text, IList< string > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );

        public static void Check( this IList< word_t > words, IList< string > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NerOutputType.CarNumber)
                        select ((CarNumberWord) w).CarNumber
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
