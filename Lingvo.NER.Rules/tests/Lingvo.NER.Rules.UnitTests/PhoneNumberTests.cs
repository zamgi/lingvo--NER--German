using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.PhoneNumbers;
using Lingvo.NER.Rules.tokenizing;
using NT = Lingvo.NER.Rules.NerOutputType;
using PT = Lingvo.NER.Rules.PhoneNumbers.PhoneNumberTypeEnum;

namespace Lingvo.NER.Rules.tests.PhoneNumbers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class PhoneNumberTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using ( var np = CreateNerProcessor() )
            {
                np.AT( "offener Betrag	0,00 € Kontaktdaten: Tel.: 030 54 90 87 93 abc.def",
                       (NT.PhoneNumber, PT.Telephone, "030 54 90 87 93".NoWhitespace()) );

                np.AT( "Test-Zahnzusatzversicherung gehört zur Online VersicherungsVergleich GmbH – Rosenheimer Landstr. 35 - D-85521 Ottobrunn - Vertretungsberechtigter Geschäftsführer ist Konrad Dießl - Tel 089/40287399 - Fax 089/40287430 - info@test-zahnzusatzversicherung.de – www.test-zahnzusatzversicherung.de.",
                       new[] { (NT.PhoneNumber, PT.Telephone, "089/40287399"), (NT.PhoneNumber, PT.Fax, "089/40287430") } );

                np.AT( "Tel 01802/550444 (0,06 Euro pro Anruf aus dem Festnetz) Fax 030/20458931 - info@pkv-ombudsmann.de - www.pkv-ombudsmann.de",
                       new[] { (NT.PhoneNumber, PT.Telephone, "01802/550444"), (NT.PhoneNumber, PT.Fax, "030/20458931") } );

                np.AT( "und Oberbayern - Max-Joseph-Str. 2 - 80333 München - Tel 089/5116-0 - Fax: 089/5116-666 - www.muenchen.ihk.de. Die Eintragung ist überprüfbar unter www.vermittlerregister.info oder beim Deutschen Industrie- und Handelskammertag (DIHK) e.V. - Breite Straße 29 - 10178 Berlin - Telefon: 0-180-500 585-0 (14 Cent/Min aus dem dt. Festnetz, mit abweichenden Preisen aus Mobilfunknetzen).",
                       new[] { (NT.PhoneNumber, PT.Telephone, "089/5116-0"), (NT.PhoneNumber, PT.Fax, "089/5116-666"), (NT.PhoneNumber, PT.Telephone, "0-180-500 585-0".NoWhitespace()) } );

                np.AT( "Tel 01802/550444 (0,06 Euro pro Anruf aus dem Festnetz) Fax 030/20458931 - info@pkv-ombudsmann.de - www.pkv-ombudsmann.de",
                       new[] { (NT.PhoneNumber, PT.Telephone, "01802/550444"), (NT.PhoneNumber, PT.Fax, "030/20458931") } );

                np.AT( "(Tel 01802/550444) (0,06 Euro pro Anruf aus dem Festnetz) Fax 030/20458931 - info@pkv-ombudsmann.de - www.pkv-ombudsmann.de",
                       new[] { (NT.PhoneNumber, PT.Telephone, "01802/550444"), (NT.PhoneNumber, PT.Fax, "030/20458931") } );

                np.AT( "(Tel 01802(495)34/550444) (0,06 Euro pro Anruf aus dem Festnetz) Fax 030/20458931 - info@pkv-ombudsmann.de - www.pkv-ombudsmann.de",
                       new[] { (NT.PhoneNumber, PT.Telephone, "01802(495)34/550444"), (NT.PhoneNumber, PT.Fax, "030/20458931") } );

                np.AT( "Breite Straße 29 - 10178 Berlin - Telefon: 0-180-500 585-0 (14 Cent/Min aus dem dt. Festnetz, mit abweichenden Preisen aus Mobilfunknetzen).",
                       (NT.PhoneNumber, PT.Telephone, "0-180-500 585-0".NoWhitespace()) );

                np.AT( "Found: Telefon 0211 229 38 69 \r\n Found: Telefon 0211 / 229 38 69",
                       new[] { (NT.PhoneNumber, PT.Telephone, "0211 229 38 69".NoWhitespace()), (NT.PhoneNumber, PT.Telephone, "0211 / 229 38 69".NoWhitespace()) } );
                np.AT( "Not found: Telefon 0211 2993869  \r\n Not found: Telefon 02112293869",
                       new[] { (NT.PhoneNumber, PT.Telephone, "0211 2993869".NoWhitespace()), (NT.PhoneNumber, PT.Telephone, "02112293869".NoWhitespace()) } );
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text, (NT nerOutputType, PT phoneType, string valueOriginal) p ) => np.AT( text, new[] { p } );
        public static void AT( this NerProcessor np, string text, IList< (NT nerOutputType, PT phoneType, string valueOriginal) > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );

        public static void Check( this IList< word_t > words, IList< (NT nerOutputType, PT phoneType, string valueOriginal) > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NT.PhoneNumber)
                        let p = (PhoneNumberWord) w
                        select (p.nerOutputType, p.PhoneNumberType, p.valueOriginal)
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

        private static int IndexOf( this IList<(NT nerOutputType, PT phoneType, string valueOriginal)> pairs, in (NT nerOutputType, PT phoneType, string valueOriginal) p, int startIndex )
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

        private static bool IsEqual( in (NT nerOutputType, PT phoneType, string valueOriginal) x, in (NT nerOutputType, PT phoneType, string valueOriginal) y )
            => (x.nerOutputType == y.nerOutputType) && (x.phoneType == y.phoneType) && (x.valueOriginal == y.valueOriginal);
    }
}
