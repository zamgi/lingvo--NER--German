using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.Address;
using Lingvo.NER.Rules.tokenizing;
using NT = Lingvo.NER.Rules.NerOutputType;

namespace Lingvo.NER.Rules.tests.Address
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class AddressTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using var np = CreateNerProcessor();
             
            np.AT( "und Oberbayern - Max-Joseph-Str. 2 - \r\n 80333 \r\n München - Tel 089/5116-0 - Fax: 089/5116-666 - www.muenchen.ihk.de. Die Eintragung ist überprüfbar unter www.vermittlerregister.info oder beim Deutschen Industrie- und Handelskammertag (DIHK) e.V. - Breite Straße 29 - 10178 Berlin - Telefon: 0-180-500 585-0 (14 Cent/Min aus dem dt. Festnetz, mit abweichenden Preisen aus Mobilfunknetzen).",
                    new[] { (NT.Address, "Max-Joseph-Str.", "2", "80333", "München"), (NT.Address, "Breite Straße", "29", "10178", "Berlin") } );

            np.AT( "und Oberbayern - Max-Joseph-Str. 2 B - \r\n 80333 \r\n München - Tel 089/5116-0 ...",
                    (NT.Address, "Max-Joseph-Str.", "2 B".NoWhitespace(), "80333", "München") );

            np.AT( "und Oberbayern - Max-Joseph-Str. 2B - \r\n 80333 \r\n München - Tel 089/5116-0 ...",
                    (NT.Address, "Max-Joseph-Str.", "2B".NoWhitespace(), "80333", "München") );
        }

        [Fact] public void T_2()
        {
            using var np = CreateNerProcessor();

            np.AT( "45470 Mülheim an der Ruhr, \r\n Schultenberg 54 a  (0,06 Euro pro Anruf aus dem Festnetz)",
                   (NT.Address, "Schultenberg", "54 a".NoWhitespace(), "45470", "Mülheim an der Ruhr") );

            np.AT( "45470 Mülheim an der Ruhr, \r\n Schultenberg 54 A  (0,06 Euro pro Anruf aus dem Festnetz)",
                   (NT.Address, "Schultenberg", "54 A".NoWhitespace(), "45470", "Mülheim an der Ruhr") );

            np.AT( "45470 Mülheim an der Ruhr, \r\n Schultenberg 54C  (0,06 Euro pro Anruf aus dem Festnetz)",
                   (NT.Address, "Schultenberg", "54C", "45470", "Mülheim an der Ruhr") );
        }

        [Fact] public void T_3()
        {
            using var np = CreateNerProcessor();

            np.AT( "und Oberbayern - Max-Joseph-Str. 2 B - \r\n 80333 \r\n Alsbach - xz 089/5116-0 ...",
                   (NT.Address, "Max-Joseph-Str.", "2 B".NoWhitespace(), "80333", "Alsbach") );

            np.AT( "und Oberbayern - Max-Joseph-Str. 2 B - \r\n 80333 \r\n Alsbach-Hähnlein - xz 089/5116-0 ...",
                   (NT.Address, "Max-Joseph-Str.", "2 B".NoWhitespace(), "80333", "Alsbach-Hähnlein") );

            np.AT( "und Oberbayern - Max-Joseph-Str. 2 B - \r\n 80333 \r\n Alsbach - Hähnlein - xz 089/5116-0 ...",
                   (NT.Address, "Max-Joseph-Str.", "2 B".NoWhitespace(), "80333", "Alsbach-Hähnlein") );

            np.AT( "und Oberbayern - Max-Joseph-Str. 2 B - \r\n 80333 \r\n Alsbach Hähnlein - xz 089/5116-0 ...",
                   (NT.Address, "Max-Joseph-Str.", "2 B".NoWhitespace(), "80333", "Alsbach-Hähnlein") );
        }

        [Fact] public void T_4()
        {
            using var np = CreateNerProcessor();

            np.AT( ", geb. 15.02.1969, wohnhaft: 04435 Schkeuditz, Am Rain 5, Am Rain 5 - 04435 Schkeuditz",
                   new[] { (NT.Address, "Am Rain", "5", "04435", "Schkeuditz"), (NT.Address, "Am Rain", "5", "04435", "Schkeuditz") } );

            np.AT( ", geb. 15.02.1969, wohnhaft: 04435 Schkeuditz, Am Rain 5 C, Am Rain 5 C - 04435 Schkeuditz",
                   new[] { (NT.Address, "Am Rain", "5 C".NoWhitespace(), "04435", "Schkeuditz"), (NT.Address, "Am Rain", "5 C".NoWhitespace(), "04435", "Schkeuditz") } );

            np.AT( ", geb. 15.02.1969, wohnhaft: 04435 Schkeuditz, Am Rain 5C, Am Rain 5C - 04435 Schkeuditz",
                   new[] { (NT.Address, "Am Rain", "5C", "04435", "Schkeuditz"), (NT.Address, "Am Rain", "5C", "04435", "Schkeuditz") } );
        }

        [Fact] public void T_5()
        {
            using var np = CreateNerProcessor();

            np.AT_Address( "Leipziger Str. 04435 Schkeuditz", (NT.Address, "Leipziger Str.", null, "04435", "Schkeuditz") );
            np.AT_Address( " Elgendorfer Str. 56410 Montabaur ", (NT.Address, "Elgendorfer Str.", null, "56410", "Montabaur") );
            np.AT_Address( " 56410 Montabaur Elgendorfer Str. ", (NT.Address, "Elgendorfer Str", null, "56410", "Montabaur") );
            np.AT_Address( " Elgendorfer Str. , 56410 Montabaur ", (NT.Address, "Elgendorfer Str.", null, "56410", "Montabaur") );
            np.AT_Address( " 56410 Montabaur, Elgendorfer Str. ", (NT.Address, "Elgendorfer Str", null, "56410", "Montabaur") );

            np.AT_Address( " 56410 Anning bei Sankt Georgen, Monsigniore-Seidinger-Straße ", (NT.Address, "Monsigniore-Seidinger-Straße", null, "56410", "Anning bei Sankt Georgen") );
            np.AT_Address( " Monsigniore-Seidinger-Straße, 56410 Anning bei Sankt Georgen", (NT.Address, "Monsigniore-Seidinger-Straße", null, "56410", "Anning bei Sankt Georgen") );

            np.AT_Address( " 56410 Anning bei Sankt Georgen, Monsigniore Seidinger Straße ", (NT.Address, "Monsigniore-Seidinger-Straße", null, "56410", "Anning bei Sankt Georgen") );
            np.AT_Address( " Monsigniore Seidinger Straße, 56410 Anning bei Sankt Georgen", (NT.Address, "Monsigniore-Seidinger-Straße", null, "56410", "Anning bei Sankt Georgen") );
        }

    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text, (NT nerOutputType, string street, string houseNumber, string zipCodeNumber, string city) p ) => np.AT( text, new[] { p } );
        public static void AT( this NerProcessor np, string text, IList< (NT nerOutputType, string street, string houseNumber, string zipCodeNumber, string city) > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );

        public static void AT_Address( this NerProcessor np, string text, (NT nerOutputType, string street, string houseNumber, string zipCodeNumber, string city) p ) => np.AT_Address( text, new[] { p } );
        public static void AT_Address( this NerProcessor np, string text, IList< (NT nerOutputType, string street, string houseNumber, string zipCodeNumber, string city) > refs )
            => np.Run_UseSimpleSentsAllocate_Address( text ).Check( refs );

        public static void Check( this IList< word_t > words, IList< (NT nerOutputType, string street, string houseNumber, string zipCodeNumber, string city) > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NT.Address)
                        let a = (AddressWord) w
                        select (a.nerOutputType, a.Street, a.HouseNumber, a.ZipCodeNumber, a.City)
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

        private static int IndexOf( this IList< (NT nerOutputType, string street, string houseNumber, string zipCodeNumber, string city) > pairs, 
                                             in (NT nerOutputType, string street, string houseNumber, string zipCodeNumber, string city) p, int startIndex )
        {
            for ( var len = pairs.Count; startIndex < len; startIndex++ )
            {
                if ( IsEqual( p, pairs[ startIndex ] ) )
                {
                    return (startIndex);
                }
            }
            return (-1);
        }

        private static bool IsEqual( in (NT nerOutputType, string street, string houseNumber, string zipCodeNumber, string city) x, 
                                     in (NT nerOutputType, string street, string houseNumber, string zipCodeNumber, string city) y )
            => (x.nerOutputType == y.nerOutputType) && (x.street == y.street) && (x.houseNumber == y.houseNumber) && (x.zipCodeNumber == y.zipCodeNumber) && (x.city == y.city);
    }
}
