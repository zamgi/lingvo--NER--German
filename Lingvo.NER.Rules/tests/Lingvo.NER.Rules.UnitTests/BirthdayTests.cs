using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.Birthdays;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.tests.Birthdays
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class BirthdayTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using ( var np = CreateNerProcessor() )
            {
                np.AT( "Geburtsdatum 11.11.1971", "11.11.1971".ToDT() );
                np.AT( "am 11.11.1972 geboren ", "11.11.1972".ToDT() );
                np.AT( "Geboren am 11.11.1973 ", "11.11.1973".ToDT() );
                np.AT( "Geb. am 11.11.1974", "11.11.1974".ToDT() );
                np.AT( "Geburtsdatum 25.01.1975", "25.01.1975".ToDT() );
                np.AT( "Geboren am 25.1.1976", "25.01.1976".ToDT() );
                np.AT( "Geb. am 25.01.77", "25.01.1977".ToDT() );
                np.AT( "am 25.1.78 geboren ", "25.01.1978".ToDT() );
                np.AT( "Geburtsdatum 25. Januar 1979", "25.01.1979".ToDT() );
                np.AT( "Geboren am 25 Januar 1980", "25.01.1980".ToDT() );
                np.AT( "Geb. am 25. Januar 81", "25.01.1981".ToDT() );
                np.AT( "am 25/01/1982 geboren ", "25.01.1982".ToDT() );
                np.AT( "Geburtsdatum 25/1/1983", "25.01.1983".ToDT() );
                np.AT( "Geboren am 25/01/84", "25.01.1984".ToDT() );
                np.AT( "Geb. am 25/1/85", "25.01.1985".ToDT() );
            }
        }

        [Fact] public void T_2()
        {
            using ( var np = CreateNerProcessor() )
            {
                np.AT( @"Geburtsdatum 11.11.1971
                        am 11.11.1972 geboren 
                        Geboren am 11.11.1973 
                        Geb. am 11.11.1974

                        Geburtsdatum 25.01.1975
                        Geboren am 25.1.1976
                        Geb. am 25.01.77
                        am 25.1.78 geboren 

                        Geburtsdatum 25. Januar 1979
                        Geboren am 25 Januar 1980
                        Geb. am 25. Januar 81
                        am 25/01/1982 geboren 

                        Geburtsdatum 25/1/1983
                        Geboren am 25/01/84
                        Geb. am 25/1/85",
                new[] { "11.11.1971".ToDT(),
                        "11.11.1972".ToDT(),
                        "11.11.1973".ToDT(),
                        "11.11.1974".ToDT(),
                        "25.01.1975".ToDT(),
                        "25.01.1976".ToDT(),
                        "25.01.1977".ToDT(),
                        "25.01.1978".ToDT(),
                        "25.01.1979".ToDT(),
                        "25.01.1980".ToDT(),
                        "25.01.1981".ToDT(),
                        "25.01.1982".ToDT(),
                        "25.01.1983".ToDT(),
                        "25.01.1984".ToDT(),
                        "25.01.1985".ToDT(), } );
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static DateTime ToDT( this string s ) => DateTime.ParseExact( s, "dd.MM.yyyy", null );
        public static void AT( this NerProcessor np, string text, in DateTime dt ) => np.AT( text, new[] { dt } );
        public static void AT( this NerProcessor np, string text, IList< DateTime > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );

        public static void Check( this IList< word_t > words, IList< DateTime > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NerOutputType.Birthday)
                        let b = (BirthdayWord) w
                        select b.BirthdayDateTime
                       ).ToArray();

            var startIndex = 0;
            foreach ( var p in refs )
            {
                startIndex = hyps.IndexOf( in p, startIndex );
                if ( startIndex == -1 )
                {
                    Assert.True( false );
                }
                startIndex++;
            }
            Assert.True( 0 < startIndex );
        }
        private static int IndexOf( this IList< DateTime > pairs, in DateTime dt, int startIndex )
        {
            for ( var len = pairs.Count; startIndex < len; startIndex++ )
            {
                if ( IsEqual( dt, pairs[ startIndex ] ) )
                {
                    return (startIndex);
                }
            }
            return (-1);
        }
        private static bool IsEqual( in DateTime x, in DateTime y ) => (x == y);
    }
}
