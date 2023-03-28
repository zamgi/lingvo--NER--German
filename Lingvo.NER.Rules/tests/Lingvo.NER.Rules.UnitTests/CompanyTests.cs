using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Lingvo.NER.Rules.Companies;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.tests.Companies
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CompanyTests : NerTestsBase
    {
        #region [.descr.]
        /*1. Название фирмы заканчивается на (большие и маленькие буквы не играют значения) (SUFFIX):
            GmbH
            gGmbH
            mbH
            AG
            e.V. („eV“, „e V“, „e. V.“)
            AöR
            GmbH & Co KG
            GmbH & Co. KG
            GbR
            Ltd
            Ltd & Co KG
            Ltd & Co. KG
            SE
            Inc.
            LLP
            B.V.
            B.V. & Co. KG
            B.V. & Co KG
                  Предшедствующее слово при этом есть часть названия, примеры:
            Bayer AG
            zamgi GmbH
            databyte GmbH
            TUI AG
            Booqua Ltd. & Co KG
            innogy SE
            Aspera Inc.
        */
        #endregion
        [Fact] public void T_Suffix_1()
        {
            using var np = CreateNerProcessor();

            np.AT( @"Bayer AG
                    zamgi GmbH
                    databyte GmbH
                    TUI AG
                    Booqua Ltd. & Co KG
                    innogy SE
                    Aspera Inc.", 
                new[] 
                {
                   "Bayer AG",
                    "zamgi GmbH",
                    "databyte GmbH",
                    "TUI AG",
                    "Booqua Ltd. & Co KG",
                    "innogy SE",
                    "Aspera Inc.",
                } );
        }

        #region [.descr.]
        /*2. Если перед окончанием „mbH“ идет слово, содержащее слово „gesellschaft“, то и предыдущее слово тоже является частью названия:
            Frankenförder Forschungsgesellschaft mbH
        */
        #endregion
        [Fact] public void T_Suffix_2()
        {
            using var np = CreateNerProcessor();

            np.AT( "Frankenförder Forschungsgesellschaft mbH", "Frankenförder Forschungsgesellschaft mbH" );
        }

        #region [.descr.]
        /*3. Если на удалении 1-4 слов слева от окончания находится одно из слов следующего списка:
            Deutsche
            Deutsches
            Mitteldeutsche
            Hallenbad
            Theater
            Internationale
            Kulturregion
            Tourismus
            Stadtwerke
            Kommunale
            Hotel
            (список будет расширяться)
                и все слова пишутся с заглавной буквы (исключения – «für», «zur», «zum», «am», «an»), то все эти слова являются частью названия фирмы. Примеры:
            Mitteldeutsche Medienförderung GmbH
            Westerwald Gästeservice e.V.
            Hallenbad Diez-Limburg GmbH
            Internationale Bodensee -  Tourismus GmbH
            Kulturregion Frankfurt RheinMain gGmbH
            Theater Lübeck gGmbH
            Deutsches Zentrum für Altersfragen e.V.
            Deutsches Institut für Entwicklungspolitik gGmbH
            Deutsche Bahn AG
            Stadtwerke Duisburg AG
            Hotel am Badersee ABG GmbH
        */
        #endregion
        [Fact] public void T_Suffix_3()
        {
            using var np = CreateNerProcessor();

            np.AT( @"Mitteldeutsche Medienförderung GmbH
                    Westerwald Gästeservice e.V.
                    Hallenbad Diez-Limburg GmbH
                    Internationale Bodensee -  Tourismus GmbH
                    Kulturregion Frankfurt RheinMain gGmbH
                    Theater Lübeck gGmbH
                    Deutsches Zentrum für Altersfragen e.V.
                    Deutsches Institut für Entwicklungspolitik gGmbH
                    Deutsche Bahn AG
                    Stadtwerke Duisburg AG
                    Hotel am Badersee ABG GmbH", 
                new[] 
                {
                    "Mitteldeutsche Medienförderung GmbH",
                    "Gästeservice e. V.",
                    "Hallenbad Diez-Limburg GmbH",
                    "Internationale Bodensee-Tourismus GmbH",
                    "Kulturregion Frankfurt RheinMain gGmbH",
                    "Theater Lübeck gGmbH",
                    "Deutsches Zentrum für Altersfragen e. V.",
                    "Deutsches Institut für Entwicklungspolitik gGmbH",
                    "Deutsche Bahn AG",
                    "Stadtwerke Duisburg AG",
                    "Hotel am Badersee ABG GmbH",
                } );
        }

        #region [.descr.]
        /*4. Если на удалении 1-4 слов слева от окончания находится сокращение (то есть все буквы заглавные) и все слова пишутся с заглавной буквы, то все эти слова являются частью названия фирмы. Примеры:
            VBB Verkehrsverbund Berlin-Brandenburg GmbH
        */
        #endregion
        [Fact] public void T_Suffix_4()
        {
            using var np = CreateNerProcessor();

            np.AT( "VBB Verkehrsverbund Berlin-Brandenburg GmbH", "VBB Verkehrsverbund Berlin-Brandenburg GmbH" );
        }

        #region [.descr.]
        /*5. Если перед найденным названием идет сокращение (то есть все буквы заглавные), то это сокращение тоже является частью названия:
            KBE Kommunale Beteiligungsgesellschaft mbH
            HSH Finanzfonds AöR
            VEBEG Gesellschaft mbH
            DB Netz AG
            TVO  Schuster GmbH & Co. KG
            A+B Pertler GmbH
        */
        #endregion
        [Fact] public void T_Suffix_5()
        {
            using var np = CreateNerProcessor();

            np.AT( @"KBE Kommunale Beteiligungsgesellschaft mbH
                    HSH Finanzfonds AöR
                    VEBEG Gesellschaft mbH
                    DB Netz AG
                    TVO  Schuster GmbH & Co. KG
                    A+B Pertler GmbH", 
                new[] 
                {
                    "KBE Kommunale Beteiligungsgesellschaft mbH",
                    "HSH Finanzfonds AöR",
                    "VEBEG Gesellschaft mbH",
                    "DB Netz AG",
                    "TVO Schuster GmbH & Co. KG",
                    "A+B Pertler GmbH",
                } );
        }

        #region [.descr.]
        /*6. Если после слов (PREFIX)
            Stiftung
            Stiftung für
            Stiftung zur

            Идет слово с заглавной буквы, то это является частью названия:
            Stiftung Bundeskanzler-Adenauer-Haus
        */
        #endregion
        [Fact] public void T_Prefixes_6()
        {
            using var np = CreateNerProcessor();

            np.AT( "Stiftung Bundeskanzler-Adenauer-Haus", "Stiftung Bundeskanzler-Adenauer-Haus" );
            np.AT( "Stiftung Bundeskanzler - Adenauer - Haus", "Stiftung Bundeskanzler-Adenauer-Haus" );
            np.AT( "Stiftung Bundeskanzler - Adenauer - 123", "Stiftung Bundeskanzler-Adenauer" );
            np.AT( "Stiftung Bundeskanzler - Adenauer - asd", "Stiftung Bundeskanzler-Adenauer" );
            np.AT( "Stiftung für Bundeskanzler-Adenauer-Haus", "Stiftung für Bundeskanzler-Adenauer-Haus" );
            np.AT( "Stiftung zur Bundeskanzler-Adenauer-Haus", "Stiftung zur Bundeskanzler-Adenauer-Haus" );
            np.AT( @"Stiftung Bundeskanzler-Adenauer-Haus
                     Stiftung Bundeskanzler - Adenauer - Haus
                     Stiftung Bundeskanzler - Adenauer - 123
                     Stiftung Bundeskanzler - Adenauer - asd
                     Stiftung für Bundeskanzler-Adenauer-Haus
                     Stiftung zur Bundeskanzler-Adenauer-Haus", 
                   new[] 
                   {
                    "Stiftung Bundeskanzler-Adenauer-Haus",
                    "Stiftung Bundeskanzler-Adenauer-Haus",
                    "Stiftung Bundeskanzler-Adenauer",
                    "Stiftung Bundeskanzler-Adenauer",
                    "Stiftung für Bundeskanzler-Adenauer-Haus",
                    "Stiftung zur Bundeskanzler-Adenauer-Haus",
                   } );
            np.EMPTY( "hier is kein Geburtsort angegeben" );
        }

        #region [.descr.]
        /*7. Если перед окончанием идет одно из следующих слов и их сочитаний:
            Deutschland
            Holding
            Deutschland Holding
            Real Estate
            Asset
            Management
            Service
            Media
            Digital
            (Deutschland)
            (список будет расширяться)
                    то и предыдущее тоже является частью названия. Примеры:
            Heraeus Holding GmbH
            Roche Deutschland Holding GmbH
            Aurelis Real Estate GmbH
            Aurelis Management GmbH
            Aurelis Real Estate GmbH & Co KG
            Aurelis Real Estate Service GmbH
            ProSiebenSat.1 Media SE
            TaurusMedia Digital GmbH
            Etengo (Deutschland) AG
            Oracle Deutschland B.V. & Co. KG
        */
        #endregion
        [Fact] public void T_Suffix_7()
        {
            using var np = CreateNerProcessor();

            np.AT( @"Heraeus Holding GmbH
                    Roche Deutschland Holding GmbH
                    Aurelis Real Estate GmbH
                    Aurelis Management GmbH
                    Aurelis Real Estate GmbH & Co KG
                    Aurelis Real Estate Service GmbH
                    ProSiebenSat.1 Media SE
                    TaurusMedia Digital GmbH
                    Etengo (Deutschland) AG
                    Oracle Deutschland B.V. & Co. KG", 
                new[] 
                {
                    "Heraeus Holding GmbH",
                    "Roche Deutschland Holding GmbH",
                    "Aurelis Real Estate GmbH",
                    "Aurelis Management GmbH",
                    "Aurelis Real Estate GmbH & Co KG",
                    "Aurelis Real Estate Service GmbH",
                    "ProSiebenSat. 1 Media SE",
                    "TaurusMedia Digital GmbH",
                    "Etengo (Deutschland) AG",
                    "Oracle Deutschland B.V. & Co. KG",
                } );
        }

        #region [.descr.]
        /*8. Будет словарь с названиями компаний, их адресов и телефонов*/
        #endregion
        [Fact] public void T_CompanyVocab_1()
        {
            var comps = new[] 
            {
                "\"\"\"Ast-Rein\"\" Holte GmbH\"",
                "Ast-Rein Holte GmbH",
                "Ast - Rein Holte GmbH",
                "Ast Rein Holte GmbH",

                "Rainer Gindal Bustouristik GmbH & Co.KG",
                "Rainer Gindal Bustouristik GmbH&Co.KG",
                "Rainer Gindal Bustouristik GmbH&CoKG",
                "Rainer Gindal Bustouristik GmbHCoKG",
                "Rainer Gindal Bustouristik GmbH Co KG",
            };

            using var np = CreateNerProcessor();

            foreach ( var comp in comps )
            {
                var (nerWords, _, _) = np.Run_UseSimpleSentsAllocate_v2( comp );

                Assert.True( nerWords.Count == 1 );
                Assert.True( nerWords[ 0 ].nerOutputType == NerOutputType.Company );
                Assert.True( nerWords[ 0 ].startIndex == 0 );
            }
        }
        [Fact] public void T_CompanyVocab_2()
        {
            var lines = File.ReadLines( _Config.COMPANY_VOCAB_FILENAME );

            Parallel.ForEach( lines, new ParallelOptions() { MaxDegreeOfParallelism = (Environment.ProcessorCount << 1) },
            () => CreateNerProcessor(),
            (line, loopState, np) =>
            {
                if ( !line.IsNullOrWhiteSpace() && (line[ 0 ] != '#') )
                {
                    //var words = np.Run_UseSimpleSentsAllocate_v1( line );
                    var (nerWords, _, _) = np.Run_UseSimpleSentsAllocate_v2( line );

                    Assert.True( nerWords.Count == 1 ); //---Assert.True( 0 < nerWords.Count );
                    Assert.True( nerWords[ 0 ].nerOutputType == NerOutputType.Company );
                    Assert.True( nerWords[ 0 ].startIndex == 0 );
                    //---Assert.True( nerWords[ 0 ].valueOriginal == line );
                }
                return (np);
            },
            (np) => np.Dispose()
            );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text, string @ref ) => np.AT( text, new[] { @ref } );
        public static void AT( this NerProcessor np, string text, IList< string > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );
        public static void EMPTY( this NerProcessor np, string text ) => Assert.True( !np.Run_UseSimpleSentsAllocate_v1( text ).Any() );

        public static void Check( this IList< word_t > words, IList< string > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NerOutputType.Company)
                        select ((CompanyWord) w).Name
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
