using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.CustomerNumbers;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.tests.CustomerNumbers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CustomerNumberTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using var np = CreateNerProcessor();
            
            np.AT( "Aktenzeichen Ist 8-F-48-03  xxx Aktenzeichen 9-Ad-48-03 \r\n Aktenzeichen 5.3247.431218.8  \r\n  Vertragsnummer AID073453343 Erich",
                    new[]
                    {
                        "8-F-48-03",
                        "9-Ad-48-03",
                        "5.3247.431218.8",
                        "AID073453343",
                    });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text, string customerNumber ) => np.AT( text, new[] { customerNumber } );
        public static void AT( this NerProcessor np, string text, IList< string > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );

        public static void Check( this IList< word_t > words, IList< string > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NerOutputType.CustomerNumber)
                        select ((CustomerNumberWord) w).CustomerNumber
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
        private static int IndexOf( this IList< string > customerNumbers, string customerNumber, int startIndex )
        {
            for ( var len = customerNumbers.Count; startIndex < len; startIndex++ )
            {
                if ( IsEqual( customerNumber, customerNumbers[ startIndex ] ) )
                {
                    return (startIndex);
                }
            }
            return (-1);
        }
        private static bool IsEqual( string x, string y ) => (x == y);
    }
}
