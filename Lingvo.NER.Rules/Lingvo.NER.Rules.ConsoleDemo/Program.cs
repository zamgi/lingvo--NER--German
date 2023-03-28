using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lingvo.NER.Rules.Address;
using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        unsafe private static void miss_chars()
        {
            var can_miss_chars = new[] { '.', '&', '"', '\'', '!' };

            var cvc = new core.Infrastructure.MissCharsVersionCombiner( can_miss_chars );
            var vers = cvc.GetVersions( "W.P.A. GmbH" ).Select( a => string.Join( " ", a ) ).ToArray();
                vers = cvc.GetVersions( "W..P..A..GmbH" ).Select( a => string.Join( " ", a ) ).ToArray();
                vers = cvc.GetVersions( "Gaul's Catering GmbH & Co. KG" ).Select( a => string.Join( " ", a ) ).ToArray();
                vers = cvc.GetVersions( " \"\"\"Ast-Rein\"\" Holte GmbH\" " ).Select( a => string.Join( " ", a ) ).ToArray();

            cvc = new core.Infrastructure.MissCharsVersionCombiner( new[] { '!', '&', '-', '.' } );
            vers = cvc.GetVersions( "a . b - c & d"/*"a . b - c & d ! e"*/ ).Select( a => string.Join( " ", a ) ).ToArray();



            using var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate();            
            
            var t  = "Gaul's Catering GmbH & Co. KG";
            var ws = tokenizer.Run_NoSentsNoUrlsAllocate( t );
                
            t  = "YEP! TV Betriebs GmbH & Co. KG";
            ws = tokenizer.Run_NoSentsNoUrlsAllocate( t );
            
            t  = "mediaplus media 2 gmbh&co.kg";
            ws = tokenizer.Run_NoSentsNoUrlsAllocate( t );

            
            //var vc = new core.Infrastructure.VersionCombiner< string >();
            //var ___ = vc.GetVersions( new[] { "a", "b", "c" },  );
        }

        private static async Task Main()
        {
            //miss_chars();

            try
            {
                using var config = await Config.Inst.CreateNerProcessorConfig_AsyncEx().CAX();

                //Run_File( config );
                //Run_Files( config );
                //Run_Address( config );
                //Run_Banks( config );
                //Run_Names( config );
                //Run_Names_Double( config );
                //Run_PhoneNumbers( config );
                //Run_Urls( config );
                //Run_CustomerNumbers( config );
                //Run_Birthdays( config );
                //Run_Birthplaces( config );
                //Run_Nationalities( config );
                //Run_CreditCards( config );
                //Run_PassportIdCardNumbers( config );
                //Run_CarNumbers( config );
                //Run_DriverLicenses( config );
                //Run_HealthInsurances( config );
                //Run_SocialSecurities( config );
                //Run_TaxIdentifications_New( config );
                //Run_TaxIdentifications_Old( config );
                //Run_TaxIdentifications( config );
                //Run_MaritalStatuses( config );
                //---Run_Companies( config );
                Run_CompaniesVocab( config );
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
                Console.ResetColor();
            }

            #region [.GC.Collect.]
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.GetTotalMemory( forceFullCollection: true );
            GC.Collect();
            #endregion

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine( "  [.......finita.......]" );
            Console.ReadLine();
        }

        private static void Run_File( NerProcessorConfig config )
        {
            var fn   = @"../../[docs-examples]/1/Angebot Zahnversicherung 2.pdf.txt";
            var text = File.ReadAllText( fn ); //"Ein Fließtext zum Verstecken von persönlichen Informationen. Thorsten Wiese. Da kann einfach mal was versteckt sein, ohne, dass man es merkt. Hier vielleicht oder hier Teststraße 12, 99998 Mülheim an Ruhr. "; // 

            ProcessText( text, config );
        }
        private static void Run_Address( NerProcessorConfig config )
        {
            //---Test__AddressModel();
            //---Test__PhoneNumbersModel();
            //--Process_Raw_Streets();
            //Filter_Streets();
            //Filter_Streets_Full();
            //streets_without_StreetPostfix();
            //Test__Streets();

            //var text = File.ReadAllText( "../../[docs-examples]/1/Protokoll Vereinsunterlagen.txt" );
            var text = //"Elgendorfer Str. 57 56410 Montabaur";
                       //"Albrecht-Dürer-Allee  или  Paul-von-Hindenburg-Platz. Bernd Straube Eduard-Künneke-Str. 4 06124 Halle \r\n Bernd Straube Eduard - Künneke - Str. 312"; // " +49 (0) 6196 908 1009"; // "+49 2036044902088"; // "XXX +8(495)123 -45-67, 8-(495)-345-67-89 AbcDef 7-512-987-21-45; Qwerty +4915140513399 "; //"2,4, 10000, 2.456.542, 8:45"; //
                       //" Sulzbach/Saar Sulzbach-Saar"; //                           
                       //"Herrn Maxim Tarassenko, wohnhaft in Schultenberg 54, 45470, Mülheim an der Ruhr";
                       //"Rechnungsadresse: zamgi Gmbh Maxim Tarasenko Schultenberg 54 45470 Mülheim An Der Ruhr Rechnung";
                       //"Glashütter Str. 104, 01277 Dresden";
                       //"xxxxxxx Eduard-Künneke-Str. 4, 06124, Anning bei Sankt Georgen "; // "xxxxxxx Eduard-Künneke-Str. 4 06124 Halle "; //"Bernd Straube Eduard-Künneke-Str. 4 06124 Halle ";
                       //@"LG Chem Europe GmbH Otto-Volger Str. 7C 65843 Sulzbach (Taunus)";
                       //"Kontaktdaten: Tel.: 030 54 90 87 93 info@x-kom.de www.x-kom.de";
                       //"80333 München - Tel 089/5116-0 - Fax: 089/5116-666 - www.muenchen.ihk.de. Die Eintragung ";

                        //"Tel 01802/550444 (0,06 Euro pro Anruf aus dem Festnetz) Fax 030/20458931 - info@pkv-ombudsmann.de - www.pkv-ombudsmann.de";
                        //"(Tel 01802/550444) (0,06 Euro pro Anruf aus dem Festnetz) Fax 030/20458931 - info@pkv-ombudsmann.de - www.pkv-ombudsmann.de";
                        //"(Tel 01802(495)34/550444) (0,06 Euro pro Anruf aus dem Festnetz) Fax 030/20458931 - info@pkv-ombudsmann.de - www.pkv-ombudsmann.de";
                        //"Breite Straße 29 - 10178 Berlin - Telefon: 0-180-500 585-0 (14 Cent/Min aus dem dt. Festnetz, mit abweichenden Preisen aus Mobilfunknetzen).";

                        //"und Oberbayern - Max-Joseph-Str. 2 B - \r\n 80333 \r\n Alsbach - Hähnlein - xz 089/5116-0 ..."; // "und Oberbayern - Max-Joseph-Str. 2 B - \r\n 80333 \r\n Alsbach-Hähnlein - xz 089/5116-0 ...";
                        //"45470 Mülheim an der Ruhr, \r\n Schultenberg 54 a  (0,06 Euro pro Anruf aus dem Festnetz)"; //"Schultenberg 54 \r\n 45470 Mülheim an der Ruhr";
                        //", geb. 15.02.1969, wohnhaft: 04435 Schkeuditz, Am Rain 5 C, Am Rain 5 C - 04435 Schkeuditz"; //", geb. 15.02.1969, wohnhaft: 04435 Schkeuditz, Am Rain 5, Am Rain 5 - 04435 Schkeuditz"; //"Christina Mitteldorf, geb. 15.02.1969, wohnhaft: 04435 Schkeuditz, Am Rain 5,";

                        //"Postfach 102872 44728 Bochum";
                        //"Äußere Leipziger Str. 04435 Schkeuditz";
                        //"1. Kanal 14 04435 Schkeuditz";
                        //"Breite Straße 29 - 10178 Berlin";
                        //"Zweiter Teich-Privatweg 29 10178 Berlin";
                        //"W.-von-Siemens-Straße 29 10178 Berlin";
                        //"Bergstraße 29 10178 Berlin";
                        //"45470 Mülheim an der Ruhr, \r\n Schultenberg 54 A  (0,06 Euro pro Anruf aus dem Festnetz)";
                        //"Erzbergerstr. 9-15, 68165 Mannheim"; //" 56410 Anning bei Sankt Georgen, Monsigniore Seidinger Straße ";
                        //"von Zyllnhardt Straße 29 10178 Berlin"; //"von-Zyllnhardt-Straße 29 10178 Berlin";
                        //"A.-Puschkin-Straße 123 04435 Schkeuditz";
                        //"A 10 BAB Seeberg Ost 123 04435 Schkeuditz";
                        //"Personalabteilung 48356 Nordwalde";
                        //"Ramsbachstr.3\n88069 Tettnang";
                        //@"Äußere Leipziger Str.\r\n04435 Schkeuditz\r\nVorsitzender\r\nHans-Peter Burk ";
                        //"Gröper Gärten 5f 16909 Wittstock";
                        //"Ruhr-University Bochum\r\n44780 Bochum, Germany";
                        "Am Siebertsweiher 3/5\r\n57290 Neunkirchen";

            ProcessText( text, config );
        }
        private static void Run_Banks( NerProcessorConfig config )
        {
            var text = //"IBAN: DE84 5747 0047 0167 1544 00";
                       //"IBAN: DE84574700470167154400";
                       //File.ReadAllText( "/Lingvo-NER/[docs-examples]/2/Bank Einzugsermächtigung Yaris.txt" ); Bank Mietvertrag WE 1 Birk.txt
                       //File.ReadAllText( "/Lingvo-NER/[docs-examples]/2/Bank Mietvertrag WE 1 Birk.txt" );
                       @"Kontoinhaber: Thorsten Fehr Kontonummer: 9161183273
Geldinstitut: Sparkasse KölnBonn Bankleitzahl: 37050198
IBAN: DE91 3705 0198 0061 1832 73 BIC: COLSDE33XXX";
            text = "Postbank Stuttgart (BLZ 60010070), Kto.-Nr. 385550708"; //text + "\n" + text;
            text = " UniCredit Bank · IBAN DE55 3702 0090 0003 7512 10";

            ProcessText( text, config );
        }
        private static void Run_Names( NerProcessorConfig config )
        {
            var text = //"Angela Merkel-Koch \r\n Erich Maria Remarque";
                       //". Wenn Sie hierzu oder auch allgemein noch ";
                       //" Dipl.-Ing. Müller Thomas";
                       //"Frank Von Der Höhe";
                       //"Sehr geehrte Frau Sabrina Barthel, vielen Dank für Ihre Anfrage und das uns entgegengebrachte Vertrauen.";
                       //"Vorsitzender B.Böhm";
                       //"Angela Merkel - Koch \r\n Erich Maria Remarque";
                       //". Wenn Sie hierzu  Maxim  Tarasenko oder auch allgemein noch ";
                       //File.ReadAllText( @"E:\Lingvo-NER\[docs-examples]\2\_names_list.txt" );
                       //"Uwe Rösler   Uwe Roesler";
                       //@"M 4     Hat Gott auch einen Namen? Erzählvorschlag \r\n Im Islam stehen 99 Namen für Allah für Gottes Eigenschaften.";
                       //                       @"Man kann gar nicht genug Namen verwenden, Gabi. Schmidt Spiele ist ein sehr alter Hersteller von Gesellschaftsspielen.
                       //Max Goldt aber auch. Goldt ist dazu noch ein guter Autor. Sten Laurel und Oliver Hardy waren Ikonen";
                       //"Entschuldigt Steffi Schäflein - Thompson";
                       @"Verwendete Literatur:
Name in title JULIA BRUMM is now recognized, 
Name in title julia BRUMM is not recognized,
Name in title Julia BRUMM is now recognized,
Name in title Julia Brumm is now recognized,
Freudenberger-Lötz, Petra und Müller-Friese, Anita: Schatztruhe Religion. Materialien für den fächerverbindenden Unterricht in der Grundschule. Teil 1. Calwer. Stuttgart. 2005
Wuckelt, Agnes und Seifert, Viola: Ich bin Naomi und wer bist du? Interreligiöses Lernen in der Grundschule";

            ProcessText( text, config );
        }
        private static void Run_Names_Double( NerProcessorConfig config )
        {
            var text = //"Angela Merkel-Koch \r\n Zuzana Merkel - Koch \r\n Erich Merkelbach-Merkhoffer \r\n Zora Merkelbach - Merkhoffer";
                       //" Zuzana Merkel - Koch ";
                       //"Angela Merkel-Koch";
                       //"Angela Müller-Schröder, Zuzana Schröder-Müller";
                       //"Angela Müller - Schröder, Zuzana Schröder - Müller";
                       "Angela Müller-Schröder, Zuzana Schröder-Müller, \r\n Angela Müller - Schröder, Zuzana Schröder - Müller";

            ProcessText( text, config );
        }
        private static void Run_PhoneNumbers( NerProcessorConfig config )
        {
            var text = //"0123 / 45 67 89 0  \r\n  2045 / 45 67 89 0";
                       //"Found: Telefon 0211 229 38 69 \r\n Found: Telefon 0211 / 229 38 69";
                       "Not found: Telefon 0211 2993869  \r\n Not found: Telefon 02112293869" + " Mobile Phone No. 0211 2993869";

            ProcessText( text, config );
        }
        private static void Run_Urls( NerProcessorConfig config )
        {
            /*Also, we different writing styles for @
                [at]
                (at)
                It should also be allowed that 1 space is between the @ and the rest of the email address.*/
            var text = //"thorsten.wiese@rocketta.de  \r\n thorsten.wiese[at]rocketta.de  \r\n thorsten.wiese(at)rocketta.de \r\n";
                       "thorsten.wiese @ rocketta.de  \r\n thorsten.wiese [at] rocketta.de  \r\n thorsten.wiese (at) rocketta.de  ";

            ProcessText( text, config );
        }
        private static void Run_CustomerNumbers( NerProcessorConfig config )
        {
            var text = "Aktenzeichen Ist 8-F-48-03  xxx Aktenzeichen 9-Ad-48-03 \r\n Aktenzeichen 5.3247.431218.8  \r\n  Vertragsnummer AID073453343 Erich";

            ProcessText( text, config );
        }
        private static void Run_Birthdays( NerProcessorConfig config )
        {
            var text = "Geboren: 1.1.1980 / Köln"; //"Geb.	02.02.2021";
/*
@"Geburtsdatum 11.11.1971
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
Geb. am 25/1/85";
            //*/
            //"Geboren am 25 Januar 1990";
            //"Geb. am 25. Januar 80"; 
            //"am 25.1.78 geboren \r\n Geburtsdatum 25. Januar 1979";
            //"xxx 0176 96266349 zzz";
            ProcessText( text, config );
        }
        private static void Run_Birthplaces( NerProcessorConfig config )
        {
            var text = "Ursprungsort Garmisch-Partenkirchen\r\n Geburtsort Freiburg im Breisgau\r\n  P.O.B. Rotenburg (Wümme)";

            ProcessText( text, config );
        }        
        private static void Run_Nationalities( NerProcessorConfig config )
        {
            var text = "Staatsangehörigkeit äthiopisch \r\n Political Home: democratic republic of the congo";

            ProcessText( text, config );
        }
        private static void Run_CreditCards( NerProcessorConfig config )
        {
            var text = "5432110051294969 \r\n 5432 1100 5129 4969 ";

            ProcessText( text, config );
        }
        private static void Run_PassportIdCardNumbers( NerProcessorConfig config )
        {
            var text = //"C01X00T47 \r\n T22000129 \r\n L01X00T47 \r\n T22000129";
@"
C73Z12345

L6Z3PGVYC  L6Z3PGVYC3
LF3ZT4WC0  LF3ZT4WC09
Y0GZC137N  Y0GZC137N1
C6X4XR5CL
C6Z1LX1KX  C6Z1LX1KX0
E8448784  E84487841
G2011464  G20114648
";

            ProcessText( text, config );
        }
        private static void Run_CarNumbers( NerProcessorConfig config )
        {
            var text = //"D-KA1234"; // "D-KA1234 D-KA-8136 \r\n T22000129 \r\n D - KA - 8136 \r\n DKA8136 \r\n D KA 8136 \r\n ";
                       //"HRB 5919"; // "D KA 8136";
                       //" SSKNDE77IBAN         DE    16    760    501    0100    116    249  ";
                       //" DIN VDE 0100 ";
                       /*@"Datenblatt des Moduls HIP-215NKHE5 des
genannte Unterlagen - DIN VDE 0100-705; VDE 0100-705:2007-10, 
Niederspannungs-Schaltgerätekombinationen - DIN VDE 0100-520; VDE 0100-520:2013-06,
Oktober 2002 - DIN VDE 0105-100 VDE 0105-100:2009-10, Betrieb von elektrischen Anlagen, Teil 100: Allgemeine Festlegungen GUTACHTEN
- DIN VDE 0100-410 VDE 0100-410:2007-06 
Erdungsanlagen und Schutzleiter - DIN VDE 0100-600 (VDE 0100-600:2008-06)
 vom Typ HIP-215NKHE5 des Herstellers 
Wechselrichters SB 3300TL HC bezieht
IN 40A/IFN 0,3A.
";*/
                       /*@"Found: D-TW-1895 \r\n Found: MH-MT-1909 \r\n Found: HSK-SG-123";*/
                       /*"Not Found: M-JK-14 \r\n Not Found: ME-J-1 \r\n Not Found: B-AM-1";*/
                       "Not Found: HSK-SG-32E \r\n" +
                       "Not Found: HSK-SG-32H";
            text = "Car numbers: \"D-KA1234, D-KA-8136\"";

            ProcessText( text, config );
        }
        private static void Run_DriverLicenses( NerProcessorConfig config )
        {
            var text = @"
B072RRE2I55
J010000SD51
N0704578035
F0100LQUA01
J430A1RZN11
B020EN83622
N8968079S12
F01335916X1";

            ProcessText( text, config );
        }
        private static void Run_HealthInsurances( NerProcessorConfig config )
        {
            var text = @"I526064554
A123456780
K734027627
Z610573490
Q327812091";

            ProcessText( text, config );
        }
        private static void Run_SocialSecurities( NerProcessorConfig config )
        {
            var text = //*
                @"15070649C103
53 270139 W 032
13 020281 W 025
  12/190367/K/006
12/190367/K/001
04-150872-P-084
  04-150872-P-080
44 091052 K 004
  44 091052 K 001
65 070260 Z 999

53 270139 W 032
·Common: /x
53/270139/W/032
·Uncommon: - or no space:
53-270139-W-032
53270139W032";
            //*/
                //"04-150872-P-080";
                //"13 020281 W 025";
                //"12/190367/K/001";
                //"04-150872-P-084 \r\n 44 091052 K 004";
            text = "Social securities: \"53 270139 W 032, 13 020281 W 025, 04-150872-P-084\"";
            //---text = "Social securities: \" 53 270139 W 032 , 13 020281 W 025 , 04-150872-P-084 \"";

            ProcessText( text, config );
        }
        private static void Run_TaxIdentifications_New( NerProcessorConfig config )
        {
            var text = @"
Steueridentifikationsnr.       81872495633
Bundesweite Identifikationsnr. 67 624 305 982
Tax-ID                         86 095 742 719
Steuer-IdNr.                   47/036/892/816
Steuer-ID Nr.                  65-929-970-489
Tax Identification No.         57549285017
Steuer IdNr.                   25 768 131 411";

            ProcessText( text, config );
        }
        private static void Run_TaxIdentifications_Old( NerProcessorConfig config )
        {
            var text = @"
Steuernummer  4151081508156
Steuer Nummer 013 815 08153
Steuernr.     151/815/08156
Steuernr      93815/08152
StNr          289381508152
St.Nr.        2893081508152
StNr.         181/815/08155
St.Nr         918181508155
St-Nr         9181081508155
St-Nr.        21/815/08150
St.-Nr.       112181508150
St.-Nr        1121081508150
Tax Number    048/815/08155
Tax No.       304881508155
Tax No        3048081508155";

            ProcessText( text, config );
        }
        private static void Run_TaxIdentifications( NerProcessorConfig config )
        {
            var text = @"
Found: Tax ID: 94 172 863 657
Not Found Tax No. 105/2638/1949";

            ProcessText( text, config );
        }
        private static void Run_MaritalStatuses( NerProcessorConfig config )
        {
            var text = @"Name:					Max Mustermann
Geburtsdatum:			01.01.1970
Wohnort:				12345 Musterstadt
Straße:				Musterstraße 1
Telefon:				12345 67890
E-Mail:					tableb@blocomo.com
Relationship status:	ledig
Familienstand:			ledig
";

            ProcessText( text, config );
        }
        private static void Run_Companies( NerProcessorConfig config )
        {
            string text;

            //text = @"Bayer AG
            //        zamgi GmbH
            //        databyte GmbH
            //        TUI AG
            //        Booqua Ltd. & Co KG
            //        innogy SE
            //        Aspera Inc.";

            //text = "Frankenförder Forschungsgesellschaft mbH";

            //text = @"Mitteldeutsche Medienförderung GmbH
            //        Westerwald Gästeservice e.V.
            //        Hallenbad Diez-Limburg GmbH
            //        Internationale Bodensee -  Tourismus GmbH
            //        Kulturregion Frankfurt RheinMain gGmbH
            //        Theater Lübeck gGmbH
            //        Deutsches Zentrum für Altersfragen e.V.
            //        Deutsches Institut für Entwicklungspolitik gGmbH
            //        Deutsche Bahn AG
            //        Stadtwerke Duisburg AG
            //        Hotel am Badersee ABG GmbH";

            //text = "VBB Verkehrsverbund Berlin-Brandenburg GmbH";

            text = @"KBE Kommunale Beteiligungsgesellschaft mbH
                    HSH Finanzfonds AöR
                    VEBEG Gesellschaft mbH
                    DB Netz AG
                    TVO  Schuster GmbH & Co. KG
                    A+B Pertler GmbH";

            //text = @"Stiftung Bundeskanzler-Adenauer-Haus
            //         Stiftung Bundeskanzler - Adenauer - Haus
            //         Stiftung Bundeskanzler - Adenauer - 123
            //         Stiftung Bundeskanzler - Adenauer - asd
            //         Stiftung für Bundeskanzler-Adenauer-Haus
            //         Stiftung zur Bundeskanzler-Adenauer-Haus";

            text = @"Heraeus Holding GmbH
                    Roche Deutschland Holding GmbH
                    Aurelis Real Estate GmbH
                    Aurelis Management GmbH
                    Aurelis Real Estate GmbH & Co KG
                    Aurelis Real Estate Service GmbH
                    ProSiebenSat.1 Media SE
                    TaurusMedia Digital GmbH
                    Etengo (Deutschland) AG
                    Oracle Deutschland B.V. & Co. KG";

            text = "ProSiebenSat.1 Media SE";

            ProcessText( text, config );
        }
        private static void Run_CompaniesVocab( NerProcessorConfig config )
        {
            using var nerProcessor = new NerProcessor( config );

            //var fn   = @"../../[docs-examples]/1/Angebot Zahnversicherung 2.pdf.txt";
            //var text = File.ReadAllText( fn );

            var t = "1.FC Köln GmbH & Co. KGaA;DE;Köln;50937;Franz-Kremer-Allee 1-3;0221 26011221;0221 71616319";
                t = "Zxczxczxc gmbh&co.kg";
                t = "Boxen.de xxxxxxxxxxxxx GmbH";
            var (nerWords_, _, _) = nerProcessor.Run_UseSimpleSentsAllocate_v2( t );


            var fn = @"../../[resources]/Companies/companies.txt";
            var n = 0;
            var wrongs = new List< (string line, string companyName, word_t[] nerWords) > ();
            foreach ( var text in File.ReadLines( fn ).Skip( 1 ) )
            {
                n++;
                var (nerWords, _, _) = nerProcessor.Run_UseSimpleSentsAllocate_v2( text );

                if ( nerWords.Count == 0 ) { wrongs.Add( (text, null, null) ); continue; }
                if ( nerWords[ 0 ].startIndex != 0 ) { wrongs.Add( (text, nerWords[ 0 ].valueOriginal, nerWords.ToArray()) ); continue; }
                if ( nerWords[ 0 ].valueOriginal != text ) { wrongs.Add( (text, nerWords[ 0 ].valueOriginal, nerWords.ToArray()) ); continue; }

                Debug.Assert( 0 < nerWords.Count );
                Debug.Assert( nerWords[ 0 ].startIndex == 0 );
                Debug.Assert( nerWords[ 0 ].valueOriginal == text );

                //---ProcessText( text, config );
            }

            Console.WriteLine( wrongs.Count );
        }

        private static void ProcessText( string text, NerProcessorConfig config )
        {
            using ( var nerProcessor = new NerProcessor( config ) )
            {
                Console.WriteLine( $"\r\n-------------------------------------------------\r\n text: '{text.Cut()}'" );

                var (nerWords, nerUnitedEntities, relevanceRanking) = nerProcessor.Run_UseSimpleSentsAllocate_v2( text );

                nerWords.Print2Console( relevanceRanking );
            }
        }
        private static void Print2Console( this List< word_t > nerWords, int relevanceRanking )
        {
            Console.WriteLine( $"-------------------------------------------------\r\n relevance-ranking: {relevanceRanking}\r\n ner-entity-count: {nerWords.Count}{Environment.NewLine}" );
            foreach ( var word in nerWords )
            {
                switch ( word.nerOutputType )
                {                    
                    case NerOutputType.ENTR__Crf: Console.ForegroundColor = ConsoleColor.Blue;   break;
                    case NerOutputType.GEO__Crf : Console.ForegroundColor = ConsoleColor.Green;  break;
                    case NerOutputType.NAME__Crf: Console.ForegroundColor = ConsoleColor.Yellow; break;
                    case NerOutputType.ORG__Crf : Console.ForegroundColor = ConsoleColor.Red;    break;
                    case NerOutputType.PROD__Crf: Console.ForegroundColor = ConsoleColor.Cyan;   break;

                    case NerOutputType.PhoneNumber         : Console.ForegroundColor = ConsoleColor.Magenta;     break;
                    case NerOutputType.Address             : Console.ForegroundColor = ConsoleColor.Red;         break;
                    case NerOutputType.Url                 : Console.ForegroundColor = ConsoleColor.Green;       break;
                    case NerOutputType.Email               : Console.ForegroundColor = ConsoleColor.Green;       break;
                    case NerOutputType.AccountNumber       : Console.ForegroundColor = ConsoleColor.Yellow;      break;
                    case NerOutputType.Name                : Console.ForegroundColor = ConsoleColor.Red;         break;
                    case NerOutputType.CustomerNumber      : Console.ForegroundColor = ConsoleColor.Green;       break;
                    case NerOutputType.Birthday            : Console.ForegroundColor = ConsoleColor.Yellow;      break;
                    case NerOutputType.Birthplace          : Console.ForegroundColor = ConsoleColor.DarkYellow;  break;
                    case NerOutputType.MaritalStatus       : Console.ForegroundColor = ConsoleColor.DarkMagenta; break;
                    case NerOutputType.Nationality         : Console.ForegroundColor = ConsoleColor.DarkCyan;    break;
                    case NerOutputType.CreditCard          : Console.ForegroundColor = ConsoleColor.Cyan;        break;
                    case NerOutputType.PassportIdCardNumber: Console.ForegroundColor = ConsoleColor.Magenta;     break;
                    case NerOutputType.CarNumber           : Console.ForegroundColor = ConsoleColor.Red;         break;
                    case NerOutputType.HealthInsurance     : Console.ForegroundColor = ConsoleColor.Green;       break;
                    case NerOutputType.DriverLicense       : Console.ForegroundColor = ConsoleColor.Blue;        break;
                    case NerOutputType.SocialSecurity      : Console.ForegroundColor = ConsoleColor.Cyan;        break;
                    case NerOutputType.TaxIdentification   : Console.ForegroundColor = ConsoleColor.Magenta;     break;
                    case NerOutputType.Company             : Console.ForegroundColor = ConsoleColor.Red;         break;
                    default: Console.ResetColor(); break;
                }
                Console.WriteLine( word );
            }
            Console.ResetColor();
            Console.WriteLine();
                
            Console.WriteLine( "-------------------------------------------------\r\n" );
        }
        private static void Run_Files( NerProcessorConfig config )
        {
            using ( var nerProcessor = new NerProcessor( config ) )
            {
                var path = @"E:\Lingvo-NER\[docs-examples]\3";
                foreach ( var fn in Directory.EnumerateFiles( path, "*.txt", SearchOption.AllDirectories ) )
                {                    
                    var text = File.ReadAllText( fn );

                    Console.WriteLine( $" '{fn}':" );

                    var (nerWords, nerUnitedEntities, relevanceRanking) = nerProcessor.Run_UseSimpleSentsAllocate_v2( text );
                    
                    nerWords.Print2Console( relevanceRanking );
                }
            }
        }

        private static string Cut( this string text, int max_len = 2048 ) => (text != null) && (max_len < text.Length) ? text.Substring( 0, max_len ) + "..." : text;

        #region [.other.]
        private static void Test__PhoneNumbersModel()
        {
            var phoneNumbersModel = TextFilesConfig.Inst.CreatePhoneNumbersModel();
            //var _ = phoneNumbersModel.IsValid( "2129", out var _cityAreaName );
            //var _ = phoneNumbersModel.IsValid( "2010", out var _cityAreaName );
            var _ = phoneNumbersModel.IsValid( "99780", out var _cityAreaName );
#if DEBUG
            phoneNumbersModel.SelfTest(); 
#endif
            for ( int i = 0, n = 1; i < 100_000_000; i++ )
            {
                var cityAreaCode = i.ToString();
                var isValid = phoneNumbersModel.IsValid( cityAreaCode, out var cityAreaName );
                if ( isValid && (i % 50_000) == 0 )
                {
                    Console.WriteLine( $"{(n++)}). {cityAreaCode}: '{cityAreaName}'" );
                }
            }
        }
        private static void Test__AddressModel()
        {            
            var addressModel = TextFilesConfig.Inst.CreateAddressModel();

            Debug.Assert( addressModel.IsStreetWordType( "Albrecht-Dürer-Allee" ) == StreetWordType.StreetPostfix );
            Debug.Assert( addressModel.IsStreetWordType( "Albrecht-Dürerallee" ) == StreetWordType.StreetPostfix );
            Debug.Assert( addressModel.IsStreetWordType( "Bachstr" ) == StreetWordType.StreetPostfix );
            Debug.Assert( addressModel.IsStreetWordType( "Bachstraße" ) == StreetWordType.StreetPostfix );
            Debug.Assert( addressModel.IsStreetWordType( "Bachstrasse" ) == StreetWordType.StreetPostfix );
            Debug.Assert( addressModel.IsStreetWordType( "Allee" ) == StreetWordType.StreetEndKeyWord );
            Debug.Assert( addressModel.IsStreetWordType( "Römerplatz" ) == StreetWordType.StreetPostfix );
            Debug.Assert( addressModel.IsStreetWordType( "Drusweilerweg" ) == StreetWordType.StreetPostfix );
            Debug.Assert( addressModel.IsStreetWordType( "Bismarckring" ) == StreetWordType.StreetPostfix );
            Debug.Assert( addressModel.IsStreetWordType( "Neumarkt" ) == StreetWordType.StreetPostfix );
            Debug.Assert( addressModel.IsStreetWordType( "Schultenberg" ) == StreetWordType.StreetPostfix );
            

            Debug.Assert( addressModel.IsStreetWordType( "Albrecht-DürerAllee" ) == StreetWordType.__UNDEFINED__ );
            Debug.Assert( addressModel.IsStreetWordType( "SchultenBerg" ) == StreetWordType.__UNDEFINED__ );
            Debug.Assert( addressModel.IsStreetWordType( "erg" ) == StreetWordType.__UNDEFINED__ );
        }
        #endregion

        #region [.streets.]
        private static bool IsBeginOfStreet( this string s ) => (s.First() == '"') && (s.Last() != '"');
        private static bool IsMiddleOfStreet( this string s ) => (s.First() != '"') && (s.Last() != '"');
        private static bool IsEndOfStreet( this string s ) => (s.First() != '"') && (s.Last() == '"');
        private static void Process_Raw_Streets()
        {
            var ss_streets_all  = new SortedSet< string >();
            var ss_streets_spec = new SortedSet< string >();
            var ss_zipCodes_all = new SortedSet< string >();

            //#1
            {
                var seps = new[] { ',' };
                string zipCode, street;
                using ( var sr = new StreamReader( "/4396_geodatendeutschland_1050_20200901.csv" ) )
                {
                    var line = sr.ReadLine();
                    for ( line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                    {
                        var array = line.Split( seps ); //, StringSplitOptions.RemoveEmptyEntries );
                        //Debug.Assert( array.Length == 20 );

                        if ( array.Length == 20 )
                        {
                            zipCode = array[ 18 ];
                            street  = array[ 19 ].Trim( '"' );
                        }
                        else
                        {
                            zipCode = street = null;

                            var idx = array.Length - 1;
                            if ( array[ idx ].IsEndOfStreet() )
                            {
                                for ( idx--; 0 <= idx; idx-- )
                                {
                                    var a = array[ idx ];
                                    if ( a.IsMiddleOfStreet() ) continue;
                                    if ( a.IsBeginOfStreet() )
                                    {
                                        Debug.Assert( 0 < idx );
                                        zipCode = array[ idx - 1 ];
                                        street  = a + ',';
                                        for ( idx++; idx < array.Length; idx++ )
                                        {
                                            street += array[ idx ] + ',';
                                        }
                                        street = street.Substring( 0, street.Length - 1 ).Trim( '"' );
                                        ss_streets_spec.Add( street );
                                        break;
                                    }
                                }

                                if ( idx <= 0 )
                                {
                                    throw (new InvalidDataException());
                                }
                            }
                            else
                            {
                                throw (new InvalidDataException());
                            }
                        }

                        if ( (street == null) || zipCode == null )
                        {
                            throw (new InvalidDataException());
                        }                        

                        ss_zipCodes_all.Add( zipCode );
                        ss_streets_all .Add( street  );
                    }
                }
            }

            //#2
            {
                using ( var sw_streets_all = new StreamWriter( "/streets_all.txt" ) )
                {
                    foreach ( var street in ss_streets_all )
                    {
                        sw_streets_all.WriteLine( street );
                    }
                }
                using ( var sw_streets_spec = new StreamWriter( "/streets_spec.txt" ) )
                {
                    foreach ( var street in ss_streets_spec )
                    {
                        sw_streets_spec.WriteLine( street );
                    }
                }

                using ( var sw_zipCodes_all = new StreamWriter( "/zipCodes_all.txt" ) )
                {
                    foreach ( var zipCode in ss_zipCodes_all )
                    {
                        sw_zipCodes_all.WriteLine( zipCode );
                    }
                }
            }
        }

        private static void Filter_Streets()
        {
            var addressModel = TextFilesConfig.Inst.CreateAddressModel();

            using ( var sr = new StreamReader( "/streets_all.txt" ) )
            using ( var sw_streets_filtered = new StreamWriter( "/streets_filtered.txt" ) )
            {
                for ( var street = sr.ReadLine(); street != null; street = sr.ReadLine() )
                {
                    if ( addressModel.IsStreetWordType( street ) == StreetWordType.__UNDEFINED__ )
                    {
                        sw_streets_filtered.WriteLine( street );
                    }
                }
            }
        }
        private static void Filter_Streets_Full()
        {
            using var config = Config.Inst.CreateNerProcessorConfig();
            var addressModel = config.AddressModel;

            using ( var nerProcessor = new NerProcessor( config ) )
            {
                using ( var sr = new StreamReader( "/streets_all.txt" ) )
                using ( var sw_streets_filtered      = new StreamWriter( "/streets_filtered_full.txt" ) )
                using ( var sw_streets_filtered_spec = new StreamWriter( "/streets_filtered_full_spec.txt" ) )
                {
                    for ( var street = sr.ReadLine(); street != null; street = sr.ReadLine() )
                    {
                        var sents = nerProcessor.Run_Debug_UseSimpleSentsAllocate_Raw( street );
                        Debug.Assert( sents.Count == 1 );
                        var words = sents[ 0 ];

                        switch ( words.Length )
                        {
                            case 0: break;
                            case 1:
                                if ( addressModel.IsStreetWordType( words[ 0 ].valueOriginal ) == StreetWordType.__UNDEFINED__ )
                                {
                                    sw_streets_filtered.WriteLine( street );
                                }
                            break;

                            case 2:
                                var swt = addressModel.IsStreetWordType( words[ 1 ].valueOriginal );
                                if ( swt == StreetWordType.__UNDEFINED__ )
                                {
                                    sw_streets_filtered.WriteLine( street );
                                }
                            break;

                            default:
                                sw_streets_filtered.WriteLine( street );
                            break;
                        }

                        static bool has_spec( word_t[] ws )
                        {
                            for ( var i = 0; i < ws.Length; i++ )
                            {
                                var w = ws[ i ];
                                if ( w.nerInputType == NerInputType.Num )
                                {
                                    return (true);
                                }
                                if ( w.length == 1 )
                                {
                                    var ch = w.valueOriginal[ 0 ];
                                    if ( (ch != '-') && (ch != '\'') )
                                    {
                                        var ct = xlat.CHARTYPE_MAP[ ch ];
                                        if ( (ct & CharType.IsLower) != CharType.IsLower &&
                                             (ct & CharType.IsPunctuation) != CharType.IsPunctuation
                                           )
                                        {
                                            return (true);
                                        }                                        
                                    }
                                }
                            }
                            return (false);
                        };

                        if ( has_spec( words ) )
                        {
                            sw_streets_filtered_spec.WriteLine( street );
                        }
                    }
                }
            }
        }

        private static string NormalizeStreet( this string s, StringBuilder buff )
        {
            if ( s != null )
            {
                buff.Clear().Append( s ).Replace( " ", string.Empty )
                                        .Replace( "-", string.Empty );
                return (buff.ToString());
                //for ( var i = 0; i < s.Length; i++ )
                //{
                //    var ch = s[ i ];
                //    if ( char.IsPunctuation( ch ) || char.IsWhiteSpace( ch ) )
                //    {
                //        if ( ch == '.' ) buff.Append( ch );
                //        else 
                //    }
                //}
            }
            return (s);
        }
        private static void Test__Streets()
        {
            using var config = Config.Inst.CreateNerProcessorConfig();
            using var nerProcessor = new NerProcessor( config );

            const char   SPACE = ' ';
            const char   TAB   = '\t';
            const string HOUSE = "29";
            const string ZIP   = "10178";
            const string CITY  = "Berlin";

            var sb = new StringBuilder( 0x100 );
            using ( var sr = new StreamReader( "/streets_all.txt" ) )
            using ( var sw = new StreamWriter( "/streets_all--finding_errors.txt" ) )
            {
                for ( var street = sr.ReadLine(); street != null; street = sr.ReadLine() )
                {
                    sb.Clear().Append( street )
                              .Append( SPACE ).Append( HOUSE )
                              .Append( SPACE ).Append( ZIP   )
                              .Append( SPACE ).Append( CITY  );
                    var address = sb.ToString();

                    var (nerWords, _, _) = nerProcessor.Run_UseSimpleSentsAllocate_v2( address );
                    if ( (nerWords.Count == 1) && (nerWords[ 0 ] is AddressWord aw) )
                    {
                        if ( aw.City          == CITY  &&
                             aw.ZipCodeNumber == ZIP   &&
                             aw.HouseNumber   == HOUSE &&
                             ((aw.Street == street) || (aw.Street.NormalizeStreet( sb ) == street.NormalizeStreet( sb )))
                           )
                        {
                            continue;
                        }

                        sw.Write( street );
                        sw.Write( TAB );
                        sw.Write( '"' ); sw.Write( address ); sw.Write( '"' );

                        sw.Write( TAB );
                        sw.Write( $"city: \"{aw.City}\", " );
                        sw.Write( $"zip: \"{aw.ZipCodeNumber}\", " );
                        sw.Write( $"street: \"{aw.Street}\", " );
                        sw.Write( $"house: \"{aw.HouseNumber}\"" );
                    }
                    else
                    {
                        sw.Write( street );
                        sw.Write( TAB );
                        sw.Write( '"' ); sw.Write( address ); sw.Write( '"' );
                    }
                    sw.WriteLine();
                }
            }
        }
        private static void streets_without_StreetPostfix()
        {
            var config = Config.Inst.CreateNerProcessorConfig();
            var addressModel = config.AddressModel;

            using ( var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate() )
            {
                using ( var sr = new StreamReader( "/streets_all.txt" ) )
                using ( var sw = new StreamWriter( "/streets_without_StreetPostfix.txt" ) )
                {
                    for ( var street = sr.ReadLine(); street != null; street = sr.ReadLine() )
                    {
                        var words = tokenizer.Run_NoSentsNoUrlsAllocate( street );
                        switch ( words.Count )
                        {
                            case 1:
                            {
                                var v = words[ 0 ].valueOriginal;
                                if ( v == "von-Zyllnhardt-Straße" )
                                {
                                    Debugger.Break();
                                }
                                var swt = addressModel.GetStreetPostfixWordType( v );
                                if ( swt == StreetWordType.__UNDEFINED__ )
                                {
                                    sw.WriteLine( street );
                                }
                                else if ( swt == StreetWordType.StreetPostfix )
                                {
                                    var arr = v.Split( '-' );
                                    if ( 2 < arr.Length )
                                    {
                                        sw.WriteLine( street );
                                    }
                                }
                            }
                            break;

                            default:
                                sw.WriteLine( street );
                            break;

                            //case 2:
                            //{
                            //    var swt = addressModel.GetStreetPostfixWordType( words[ 1 ].valueOriginal );
                            //    if ( swt == StreetWordType.__UNDEFINED__ )
                            //    {
                            //        sw.WriteLine( street );
                            //    }
                            //}
                            //break;
                        }
                    }
                }
            }
        }
        #endregion

        #region [.banks.]
        private static void Process_Raw_BankCodes()
        {
            var ss  = new SortedSet< string >();

            using ( var sr = new StreamReader( "/BLZ.txt" ) )
            {
                for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                {
                    if ( line.Length < 8 ) continue;
                    var bankCode = line.Substring( 0, 8 );
                    ss.Add( bankCode );
                }
            }

            using ( var sw = new StreamWriter( "/bankCodes.txt" ) )
            {
                foreach ( var _ in ss )
                {
                    sw.WriteLine( _ );
                }
            }
        }
        #endregion

        #region [.names.]
        private static bool Is_DE( this string word )
        {
            for ( var i = word.Length - 1; 0 <= i; i-- )
            {
                var ch = word[ i ];
                if ( 'a' <= ch && ch <= 'z' ) continue;
                if ( 'A' <= ch && ch <= 'Z' ) continue;

                switch ( ch )
                {
                    case 'ü': case 'ö': case 'ä': case 'ß':
                    case 'Ü': case 'Ö': case 'Ä':
                        break;

                    default:
                        return (false);
                }
            }
            return (true);
        }
        private static async Task xxxxx()
        {
            using var config = await Config.Inst.CreateNerProcessorConfig_Async().CAX();
            var namesModel = config.NamesModel;

            var ss = new SortedSet< string >();
            var lst = new List< string >( 500_000 );
            var line_count = 0;
            using ( var sr = new StreamReader( @"E:\[Maxim Tarasenko]\language-models-txt__(IsFirstUpperOtherLower)\de--wiki-(ngram_1-cut_1)_(IsFirstUpperOtherLower).txt" ) )
            {
                var sep = new[] { '\t' };
                var one_word = new word_t() { valueOriginal = "xz" };
                var words = new[] { one_word };
                for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                {
                    line_count++;

                    var a = line.Split( sep, StringSplitOptions.RemoveEmptyEntries );
                    if ( a.Length != 2 ) continue;
                    if ( !double.TryParse( a[ 1 ], out var _ ) ) continue;

                    var word = a[ 0 ];

                    if ( !word.Is_DE() ||
                         word.Contains( 'Č' ) ||
                         word.Contains( 'Š' ) ||
                         word.Contains( 'ě' ) ||
                         word.Contains( 'Ż' ) ||
                         word.Contains( 'Ž' ) ||
                         word.Contains( 'Ș' ) ||
                         word.Contains( 'ă' ) ||
                         word.EndsWith( "straße" ) ) continue;

                    one_word.valueOriginal = word;
                    if ( namesModel.FirstNames.TryGetFirst( words, 0, out var length ) ) continue;
                    if ( namesModel.SurNames  .TryGetFirst( words, 0, out     length ) ) continue;                    

                    if ( ss.Add( word ) )
                    {
                        lst.Add( word );
                    }
                }
            }

            using ( var sw = new StreamWriter( @"E:\[Maxim Tarasenko]\language-models-txt__(IsFirstUpperOtherLower)\de--wiki-(ngram_1-cut_1)_(IsFirstUpperOtherLower)__cleaned-1.txt" ) )
            {
                foreach ( var word in ss )
                {
                    sw.WriteLine( word );
                }
            }
            using ( var sw = new StreamWriter( @"E:\[Maxim Tarasenko]\language-models-txt__(IsFirstUpperOtherLower)\de--wiki-(ngram_1-cut_1)_(IsFirstUpperOtherLower)__cleaned-2.txt" ) )
            {
                foreach ( var word in lst )
                {
                    sw.WriteLine( word );
                }
            }
        }
        #endregion
    }
}
