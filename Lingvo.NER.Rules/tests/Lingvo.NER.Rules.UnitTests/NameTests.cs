using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.Names;
using Lingvo.NER.Rules.tokenizing;
using NT = Lingvo.NER.Rules.NerOutputType;
using TPT = Lingvo.NER.Rules.Names.TextPreambleTypeEnum;

namespace Lingvo.NER.Rules.tests.Names
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class NameTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using var np = CreateNerProcessor();

            np.AT( "Angela Merkel-Koch \r\n Erich Maria Remarque", new[] { ("Angela", "Merkel-Koch"), ("Erich Maria", "Remarque") } );
            np.AT( "Angela Merkel - Koch \r\n Erich Maria Remarque", new[] { ("Angela", "Merkel-Koch"), ("Erich Maria", "Remarque") } );

            np.EMPTY( ". Wenn Sie hierzu oder auch allgemein noch " );

            np.AT( " Dipl.-Ing. Müller Thomas", ("Thomas", "Müller", TPT.Engineer) );
            np.AT( "Frank Von Der Höhe", ("Frank", "Von Der Höhe") );
            np.AT( "Sehr geehrte Frau Sabrina Barthel, vielen Dank für Ihre Anfrage und das uns entgegengebrachte Vertrauen.", ("Sabrina", "Barthel", TPT.Frau) );

            np.AT( "Vorsitzender B.Böhm", ("B.", "Böhm", TPT.Chairman) );
        }

        [Fact] public void T_2()
        {
            using var np = CreateNerProcessor();

            np.AT( "Anwesende Vera Grünberg, Birgit Schütz, Andreas Uiker, Julia Wiese\r\nEntschuldigt Steffi Schäflein - Thompson",
                   new[] { ("Vera", "Grünberg"), ("Birgit", "Schütz"), ("Andreas", "Uiker"), ("Julia", "Wiese"), ("Steffi", "Schäflein-Thompson") } );

            np.EMPTY( @"Die wirtschaftlichen Verhältnisse des Mieters/der Mieter sind geordnet und mittels einer Bonitätsauskunft (z.B. Infoscore, Schufa) geprüft
Ja Nein
Hinweis: Bei neuen Mietverhältnissen entfällt die Wartezeit" );
        }

        [Fact] public void T_3()
        {
            using var np = CreateNerProcessor();

            np.AT( "Uwe Rösler"   , ("Uwe", "Rösler") );
            np.AT( "Andrea Hübner", ("Andrea", "Hübner") );
            np.AT( "Thomas Häßler", ("Thomas", "Häßler") );

            np.EMPTY( "M 4 Hat Gott auch einen Namen? Erzählvorschlag \r\n Im Islam stehen 99 Namen für Allah für Gottes Eigenschaften." );

            //dot between fn & sn => "Gabi. Schmidt"
            np.AT( @"Man kann gar nicht genug Namen verwenden, Gabi. Schmidt Spiele ist ein sehr alter Hersteller von Gesellschaftsspielen.
Max Goldt aber auch. Goldt ist dazu noch ein guter Autor. Sten Laurel und Oliver Hardy waren Ikonen", 
                   new[] { ("Max", "Goldt"), ("Sten", "Laurel"), ("Oliver", "Hardy") } );

            //comma between sn & fn => "Freudenberger-Lötz, Petra"
            np.AT( @"Verwendete Literatur:
Freudenberger-Lötz, Petra und Müller-Friese, Anita: Schatztruhe Religion. Materialien für den fächerverbindenden Unterricht in der Grundschule. Teil 1. Calwer. Stuttgart. 2005
Wuckelt, Agnes und Seifert, Viola: Ich bin Naomi und wer bist du? Interreligiöses Lernen in der Grundschule",
                   new[] { ("Petra", "Freudenberger-Lötz"), ("Anita", "Müller-Friese"), ("Agnes", "Wuckelt"), ("Viola", "Seifert") } );
        }

        [Fact] public void T_4()
        {
            using var np = CreateNerProcessor();

            //dot between fn & sn => "Gabi. Schmidt"
            np.AT( @"Verwendete Literatur:
Name in title JULIA BRUMM is now recognized, 
Name in title julia BRUMM is not recognized,
Name in title Julia BRUMM is now recognized,
Name in title Julia Brumm is now recognized,....", 
                   new[] { ("JULIA", "BRUMM"), ("Julia", "BRUMM"), ("Julia", "Brumm") } );
        }

        [Fact] public void T_5()
        {
            using var np = CreateNerProcessor();

            //It is necessary that any two surnames can be combined. Type Müller-Spüller, Spüller-Müller etc.
            np.AT( @"Angela Müller-Schröder, Zuzana Schröder-Müller, \r\n Angela Müller - Schröder, Zuzana Schröder - Müller", 
                   new[] { ("Angela", "Müller-Schröder"), ("Zuzana", "Schröder-Müller"), ("Angela", "Müller-Schröder"), ("Zuzana", "Schröder-Müller") } );

            np.AT( @"Angela Panghy-Lee , Zuzana Freudenberger-Lötz , Angela Freudenberger-Lotz , Zuzana Müller-Friese , Angela Muller-Friese",
                   new[] { ("Angela", "Panghy-Lee"), ("Zuzana", "Freudenberger-Lötz"), ("Angela", "Freudenberger-Lotz"), ("Zuzana", "Müller-Friese"), ("Angela", "Muller-Friese") } );

            np.AT( @"Angela Panghy - Lee , Zuzana Freudenberger - Lötz , Angela Freudenberger - Lotz , Zuzana Müller - Friese , Angela Muller - Friese",
                   new[] { ("Angela", "Panghy-Lee"), ("Zuzana", "Freudenberger-Lötz"), ("Angela", "Freudenberger-Lotz"), ("Zuzana", "Müller-Friese"), ("Angela", "Muller-Friese") } );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text, (string firstName, string surName, TPT tpt) p ) => np.AT( text, new[] { p } );
        public static void AT( this NerProcessor np, string text, IList< (string firstName, string surName, TPT tpt) > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );

        public static void AT( this NerProcessor np, string text, (string firstName, string surName) p ) => np.AT( text, new[] { p } );
        public static void AT( this NerProcessor np, string text, IList< (string firstName, string surName) > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );
        public static void EMPTY( this NerProcessor np, string text ) => Assert.True( !np.Run_UseSimpleSentsAllocate_v1( text ).Any() );

        private static void Check( this IList< word_t > words, IList< (string firstName, string surName) > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NT.Name)
                        let n = (NameWord) w
                        select (NT.Name, n.Firstname, n.Surname)
                       ).ToArray();

            var startIndex = 0;
            foreach ( var p in refs )
            {
                startIndex = hyps.IndexOf( (NT.Name, p.firstName, p.surName), startIndex );
                if ( startIndex == -1 )
                {
                    Assert.True( false );
                }
                startIndex++;
            }
            Assert.True( 0 < startIndex );
        }
        private static int IndexOf( this IList< (NT nerOutputType, string firstName, string surName) > pairs, in (NT nerOutputType, string firstName, string surName) p, int startIndex )
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
        private static bool IsEqual( in (NT nerOutputType, string firstName, string surName) x, in (NT nerOutputType, string firstName, string surName) y )
            => (x.nerOutputType == y.nerOutputType) && (x.firstName == y.firstName) && (x.surName == y.surName);

        private static void Check( this IList< word_t > words, IList< (string firstName, string surName, TPT tpt) > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NT.Name)
                        let n = (NameWord) w
                        select (NT.Name, n.Firstname, n.Surname, n.TextPreambleType)
                       ).ToArray();

            var startIndex = 0;
            foreach ( var p in refs )
            {
                startIndex = hyps.IndexOf( (NT.Name, p.firstName, p.surName, p.tpt), startIndex );
                if ( startIndex == -1 )
                {
                    Assert.True( false );
                }
                startIndex++;
            }
            Assert.True( 0 < startIndex );
        }
        private static int IndexOf( this IList< (NT nerOutputType, string firstName, string surName, TPT tpt) > pairs, in (NT nerOutputType, string firstName, string surName, TPT tpt) p, int startIndex )
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
        private static bool IsEqual( in (NT nerOutputType, string firstName, string surName, TPT tpt) x, in (NT nerOutputType, string firstName, string surName, TPT tpt) y )
            => (x.nerOutputType == y.nerOutputType) && (x.firstName == y.firstName) && (x.surName == y.surName) && (x.tpt == y.tpt);
    }
}
