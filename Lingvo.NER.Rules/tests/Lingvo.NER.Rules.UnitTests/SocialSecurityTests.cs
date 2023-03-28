using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.SocialSecurities;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.tests.SocialSecurities
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class SocialSecurityTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using var np = CreateNerProcessor();
            
            np.AT( "15070649C103" );
            np.AT( "13 020281 W 025" );
            np.AT( "12/190367/K/001" );
            np.AT( "04-150872-P-084" );
            np.AT( "44 091052 K 004" );
            np.AT( "65 070260 Z 999" );

            np.AT( "53 270139 W 032" );
            np.AT( "53 /270139/W/032" );
            np.AT( "53-270139-W-032" );
            np.AT( "53270139W032" );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text ) => np.AT( text, new[] { text.NoWhitespace().Replace( "/", string.Empty ).Replace( "-", string.Empty ) } );
        public static void AT( this NerProcessor np, string text, IList< string > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );

        public static void Check( this IList< word_t > words, IList< string > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NerOutputType.SocialSecurity)
                        select ((SocialSecurityWord) w).SocialSecurityNumber
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
