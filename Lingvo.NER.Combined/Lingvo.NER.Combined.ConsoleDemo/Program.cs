using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Lingvo.NER.NeuralNetwork;
using Lingvo.NER.NeuralNetwork.Tokenizing;
using Lingvo.NER.Rules;

using NerRuleOutputType = Lingvo.NER.Rules.NerOutputType;
using NerRule_word_t    = Lingvo.NER.Rules.tokenizing.word_t;
using NN_word_t         = Lingvo.NER.NeuralNetwork.Tokenizing.word_t;

namespace Lingvo.NER.Combined.ConsoleDemo
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static bool USE_NNER { get; } = (bool.TryParse( ConfigurationManager.AppSettings[ "USE_NNER" ] ?? bool.TrueString, out var b ) && b);

        private static (bool Use, Predictor Predictor, TokenizerConfig TokenizerConfig) CreateNNERIfUse( string[] args, bool use_nner )
        {
            if ( use_nner )
            {
                var opts = OptionsExtensions.ReadInputOptions( args, "ner_de.json" );
                var nnerPredictor       = new Predictor( opts );
                var nnerTokenizerConfig = new TokenizerConfig( opts.SentSplitterResourcesXmlFilename, opts.UrlDetectorResourcesXmlFilename );
                return (true, nnerPredictor, nnerTokenizerConfig);
            }
            return (default);
        }
        private static async Task< NERCombinedConfig > CreateNERCombinedConfig( string[] args, bool use_nner )
        {
            var nner_task                    = Task.Run( () => CreateNNERIfUse( args, use_nner ) );
            var nerRulesProcessorConfig_task = Config.Inst.CreateNerProcessorConfig_AsyncEx();

            await Task.WhenAll( nner_task, nerRulesProcessorConfig_task ).CAX();

            var nner                    = nner_task.Result;
            var nerRulesProcessorConfig = nerRulesProcessorConfig_task.Result;

            var config = new NERCombinedConfig()
            {
                NerRules_ProcessorConfig = nerRulesProcessorConfig,
                NNER_Use                 = nner.Use,
                NNER_Predictor           = nner.Predictor,
                NNER_TokenizerConfig     = nner.TokenizerConfig,
            };
            return (config);
        }

        private static async Task Main( string[] args )
        {
            //---IntervalList___test( args );
            try
            {
                var config = await CreateNERCombinedConfig( args, USE_NNER ).CAX();
                using var nerCombinedProcessor = new NERCombined_Processor( config );

                Run( nerCombinedProcessor );
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
                Console.ResetColor();
            }
        }

        private static void Run( NERCombined_Processor nerCombinedProcessor )
        {
            //var text = "Von der Leyen sprach sich nach längerer Bedenkzeit im Juli 2014 öffentlich für die Beschaffung bewaffnungsfähiger Drohnen für die Bundeswehr aus. denisov72@mail.ru";
            var text = @"das ist fantastisch.
Angela Dorothea Merkel (geb. Kasner; * 17. Juli 1954 in Hamburg) ist eine deutsche Politikerin (CDU). Sie ist seit dem 22. November 2005 Bundeskanzlerin der Bundesrepublik Deutschland. Von April 2000 bis Dezember 2018 war sie Bundesvorsitzende der CDU.
Merkel wuchs in der DDR auf und war dort als Physikerin am Zentralinstitut für Physikalische Chemie tätig. Erstmals politisch aktiv wurde sie während der Wendezeit in der Partei Demokratischer Aufbruch, die sich 1990 der CDU anschloss. In der ersten und letzten demokratisch gewählten Regierung der DDR übte sie das Amt der stellvertretenden Regierungssprecherin aus.
Bei der Bundestagswahl am 2. Dezember 1990 errang sie erstmals ein Bundestagsmandat. Bei den folgenden sieben Bundestagswahlen wurde sie in ihrem Wahlkreis in Vorpommern direkt gewählt. Von 1991 bis 1994 war Merkel Bundesministerin für Frauen und Jugend im Kabinett Kohl IV und von 1994 bis 1998 Bundesministerin für Umwelt, Naturschutz und Reaktorsicherheit im Kabinett Kohl V. Von 1998 bis zu ihrer Wahl zur Bundesvorsitzenden der Partei im Jahr 2000 amtierte sie als Generalsekretärin der CDU.
Leonhard Euler (lateinisch Leonhardus Eulerus; * 15. April 1707 in Basel; † 7. Septemberjul. / 18. September 1783greg. in Sankt Petersburg) war ein Schweizer Mathematiker, Physiker, Astronom, Geograph, Logiker und Ingenieur.
Er machte wichtige und weitreichende Entdeckungen in vielen Zweigen der Mathematik, wie beispielsweise der Infinitesimalrechnung und der Graphentheorie. Gleichzeitig leistete Euler fundamentale Beiträge auf anderen Gebieten wie der Topologie und der analytischen Zahlentheorie. Er prägte grosse Teile der bis heute weltweit gebräuchlichen mathematischen Terminologie und Notation. Beispielsweise führte Euler den Begriff der mathematischen Funktion in die Analysis ein. Er ist zudem für seine Arbeiten in der Mechanik, Strömungsdynamik, Optik, Astronomie und Musiktheorie bekannt.
Euler, der den grössten Teil seines Lebens in Sankt Petersburg und in Berlin verbrachte, war einer der bedeutendsten Mathematiker des 18. Jahrhunderts. Seine herausragenden Leistungen ebbten auch nach seiner Erblindung im Jahre 1771 nicht ab und wurden bereits von seinen Zeitgenossen anerkannt. Er gilt heute als einer der brillantesten und produktivsten Mathematiker aller Zeiten. Seine gesammelten Schriften Opera omnia umfassen bisher 76 Bände – ein mathematisches Werk, dessen Umfang bis heute unerreicht bleibt.
Leonhard Euler zu Ehren erhielten zwei mathematische Konstanten seinen Namen: die Eulersche Zahl (Basis des natürlichen Logarithmus) und die Euler-Mascheroni-Konstante aus der Zahlentheorie, die gelegentlich auch Eulersche Konstante genannt wird.
Leonhard Eulers Arbeiten inspirierten viele Generationen von Mathematikern, darunter Pierre-Simon Laplace, Carl Gustav Jacobi und Carl Friedrich Gauß, nachhaltig. Laplace soll zu seinen Schülern gesagt haben: «Lest Euler, er ist unser aller Meister!».";

            //text = "Leonhard Euler zu Ehren erhielten zwei mathematische Konstanten seinen Namen: die Eulersche Zahl (Basis des natürlichen Logarithmus) und die Euler-Mascheroni-Konstante aus der Zahlentheorie, die gelegentlich auch Eulersche Konstante genannt wird.";
            text = @"Herr 
Maxim Tarassenko 
Schultenberg 54 
45470 Mülheim an der Ruhr 
Staatsangehörigkeit russisch 
Geburtsort Rotenburg (Wümme) 
Beziehungsstatus eingetragene Lebenspartnerschaft 
Rechnung 
1&1 IONOS SE 
Elgendorfer Str. 57 
56410 Montabaur 
Kopie vom 16.08.2020  ";

            var nerWords = nerCombinedProcessor.ProcessText( text );

            nerWords.Print2Console();
        }

        private static void IntervalList___test( string[] args )
        {
            var opts = OptionsExtensions.ReadInputOptions( args, "ner_de.json" );
            var nnerTokenizerConfig = new TokenizerConfig( opts.SentSplitterResourcesXmlFilename, opts.UrlDetectorResourcesXmlFilename );
            using var nnerTokenizer = new Tokenizer( nnerTokenizerConfig, replaceNumsOnPlaceholder: true );

            var sents = nnerTokenizer.Run_SimpleSentsAllocate( "Von der Leyen sprach sich nach längerer Bedenkzeit im Juli 2014 öffentlich für die Beschaffung bewaffnungsfähiger Drohnen für die Bundeswehr aus. denisov72@mail.ru" );

            var lst = new IntervalList< NN_word_t >( sents.Sum( s => s.Count ) );
            foreach ( var w in sents.SelectMany( s => s ) )
            {
                var suc = lst.TryAdd( (w.startIndex, w.length), w );
                Debug.Assert( suc );
            }

            var suc_ = lst.TryGetValue( (1, 5), out var ew );
            foreach ( var w in sents.SelectMany( s => s ) )
            {
                suc_ = lst.TryGetValue( (w.startIndex, w.length), out ew );
                Debug.Assert( suc_ && (w == ew) );
            }
        }

        private static void Print2Console( this IList< NerRule_word_t > nerWords )
        {
            Console.WriteLine( $"-------------------------------------------------\r\n ner-entity-count: {nerWords.Count}, NNER: {nerWords.Count( w => w.IsNNER() )}\r\n" );
            foreach ( var word in nerWords )
            {
                switch ( word.nerOutputType )
                {                    
                    case NerRuleOutputType.ENTR__Crf: Console.ForegroundColor = ConsoleColor.Blue;   break;
                    case NerRuleOutputType.GEO__Crf : Console.ForegroundColor = ConsoleColor.Green;  break;
                    case NerRuleOutputType.NAME__Crf: Console.ForegroundColor = ConsoleColor.Yellow; break;
                    case NerRuleOutputType.ORG__Crf : Console.ForegroundColor = ConsoleColor.Red;    break;
                    case NerRuleOutputType.PROD__Crf: Console.ForegroundColor = ConsoleColor.Cyan;   break;

                    case NerRuleOutputType.PhoneNumber         : Console.ForegroundColor = ConsoleColor.Magenta; break;
                    case NerRuleOutputType.Address             : Console.ForegroundColor = ConsoleColor.Red;     break;
                    case NerRuleOutputType.Url                 : Console.ForegroundColor = ConsoleColor.Green;   break;
                    case NerRuleOutputType.Email               : Console.ForegroundColor = ConsoleColor.Green;   break;
                    case NerRuleOutputType.AccountNumber       : Console.ForegroundColor = ConsoleColor.Yellow;  break;
                    case NerRuleOutputType.Name                : Console.ForegroundColor = ConsoleColor.Red;     break;
                    case NerRuleOutputType.CustomerNumber      : Console.ForegroundColor = ConsoleColor.Green;   break;
                    case NerRuleOutputType.Birthday            : Console.ForegroundColor = ConsoleColor.Yellow;  break;
                    case NerRuleOutputType.Birthplace          : Console.ForegroundColor = ConsoleColor.DarkYellow;  break;
                    case NerRuleOutputType.MaritalStatus       : Console.ForegroundColor = ConsoleColor.DarkMagenta; break;
                    case NerRuleOutputType.Nationality         : Console.ForegroundColor = ConsoleColor.DarkCyan;    break;
                    case NerRuleOutputType.CreditCard          : Console.ForegroundColor = ConsoleColor.Cyan;    break;
                    case NerRuleOutputType.PassportIdCardNumber: Console.ForegroundColor = ConsoleColor.Magenta; break;
                    case NerRuleOutputType.CarNumber           : Console.ForegroundColor = ConsoleColor.Red;     break;
                    case NerRuleOutputType.HealthInsurance     : Console.ForegroundColor = ConsoleColor.Green;   break;
                    case NerRuleOutputType.DriverLicense       : Console.ForegroundColor = ConsoleColor.Blue;    break;
                    case NerRuleOutputType.SocialSecurity      : Console.ForegroundColor = ConsoleColor.Cyan;    break;
                    case NerRuleOutputType.TaxIdentification   : Console.ForegroundColor = ConsoleColor.Magenta; break;
                    case NerRuleOutputType.Company             : Console.ForegroundColor = ConsoleColor.Red;     break;

                    case NerRuleOutputType.PERSON__NNER       : Console.ForegroundColor = ConsoleColor.Red;     break;
                    case NerRuleOutputType.ORGANIZATION__NNER : Console.ForegroundColor = ConsoleColor.Yellow;  break;
                    case NerRuleOutputType.LOCATION__NNER     : Console.ForegroundColor = ConsoleColor.Green;   break;
                    case NerRuleOutputType.MISCELLANEOUS__NNER: Console.ForegroundColor = ConsoleColor.Magenta; break;

                    default: Console.ResetColor(); break;
                }
                Console.WriteLine( word + (word.IsNNER() ? ", (-=NNER=-)" : null) );
            }
            Console.ResetColor();
            Console.WriteLine();
                
            Console.WriteLine( "-------------------------------------------------\r\n" );
        }
        private static bool IsNNER( this NerRule_word_t w ) => w.nerOutputType switch
        {
            NerRuleOutputType.PERSON__NNER        => true,
            NerRuleOutputType.ORGANIZATION__NNER  => true,
            NerRuleOutputType.LOCATION__NNER      => true,
            NerRuleOutputType.MISCELLANEOUS__NNER => true,
            _ => false
        };
    }
}
