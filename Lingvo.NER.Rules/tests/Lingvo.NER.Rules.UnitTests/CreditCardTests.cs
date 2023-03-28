using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.CreditCards;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.tests.CreditCards
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CreditCardTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using ( var np = CreateNerProcessor() )
            {
                np.AT( "5432110051294969 \r\n 5432 1100 5129 4969 ", new[] { "5432110051294969", "5432110051294969" } );
                //---np.AT( "3700 0000 0000 002" );
                //---np.AT( "3700 0000 0100 018" );
                np.AT( "3600 6666 3333 44" );
                np.AT( "3607 0500 0010 20" );
                np.AT( "5101 1800 0000 0007" );
                np.AT( "2222 4000 7000 0005" );
                np.AT( "5100 2900 2900 2909" );
                np.AT( "5555 3412 4444 1115" );
                np.AT( "5577 0000 5577 0004" );
                np.AT( "5136 3333 3333 3335" );
                np.AT( "5585 5585 5585 5583" );
                np.AT( "5555 4444 3333 1111" );
                np.AT( "2222 4107 4036 0010" );
                np.AT( "5555 5555 5555 4444" );
                np.AT( "2222 4107 0000 0002" );
                np.AT( "2222 4000 1000 0008" );
                np.AT( "2223 0000 4841 0010" );
                np.AT( "2222 4000 6000 0007" );
                np.AT( "2223 5204 4356 0010" );
                np.AT( "5500 0000 0000 0004" );
                np.AT( "2222 4000 3000 0004" );
                np.AT( "6771 7980 2500 0004" );
                np.AT( "5100 0600 0000 0002" );
                np.AT( "5100 7050 0000 0002" );
                np.AT( "5103 2219 1119 9245" );
                np.AT( "5424 0000 0000 0015" );
                np.AT( "2222 4000 5000 0009" );
                np.AT( "5106 0400 0000 0008" );
                np.AT( "4111 1111 4555 1142" );
                np.AT( "4988 4388 4388 4305" );
                np.AT( "4166 6766 6766 6746" );
                np.AT( "4646 4646 4646 4644" );
                np.AT( "4000 6200 0000 0007" );
                np.AT( "4000 0600 0000 0006" );
                np.AT( "4293 1891 0000 0008" );
                np.AT( "4988 0800 0000 0000" );
                np.AT( "4111 1111 1111 1111" );
                np.AT( "4444 3333 2222 1111" );
                np.AT( "4001 5900 0000 0001" );
                np.AT( "4000 1800 0000 0002" );
                np.AT( "4000 0200 0000 0000" );
                np.AT( "4000 1600 0000 0004" );
                np.AT( "4002 6900 0000 0008" );
                np.AT( "4400 0000 0000 0008" );
                np.AT( "4484 6000 0000 0004" );
                np.AT( "4607 0000 0000 0009" );
                np.AT( "4977 9494 9494 9497" );
                np.AT( "4000 6400 0000 0005" );
                np.AT( "4003 5500 0000 0003" );
                np.AT( "4000 7600 0000 0001" );
                np.AT( "4017 3400 0000 0003" );
                np.AT( "4005 5190 0000 0006" );
                np.AT( "4131 8400 0000 0003" );
                np.AT( "4035 5010 0000 0008" );
                np.AT( "4151 5000 0000 0008" );
                np.AT( "4571 0000 0000 0001" );
                np.AT( "4199 3500 0000 0002" );
            }
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
                        where (w.nerOutputType == NerOutputType.CreditCard)
                        select ((CreditCardWord) w).CreditCardNumber
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
