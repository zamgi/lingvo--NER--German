using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.TaxIdentifications;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.tests.TaxIdentifications
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class TaxIdentificationTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using ( var np = CreateNerProcessor() )
            {
                np.AT( "Steueridentifikationsnr.       81872495633", "81872495633" );
                np.AT( "Bundesweite Identifikationsnr. 67 624 305 982", "67 624 305 982" );
                np.AT( "Tax-ID                         86 095 742 719", "86 095 742 719" );
                np.AT( "Steuer-IdNr.                   47/036/892/816", "47/036/892/816" );
                np.AT( "Steuer-ID Nr.                  65-929-970-489", "65-929-970-489" );
                np.AT( "Tax Identification No.         57549285017", "57549285017" );
                np.AT( "Steuer IdNr.                   25 768 131 411", "25 768 131 411" );

                np.AT( "Steuernummer  4151081508156", "4151081508156" );
                np.AT( "Steuer Nummer 013 815 08153", "013 815 08153" );
                np.AT( "Steuernr.     151/815/08156", "151/815/08156" );
                np.AT( "Steuernr      93815/08152", "93815/08152" );
                np.AT( "StNr          289381508152", "289381508152" );
                np.AT( "St.Nr.        2893081508152", "2893081508152" );
                np.AT( "StNr.         181/815/08155", "181/815/08155" );
                np.AT( "St.Nr         918181508155", "918181508155" );
                np.AT( "St-Nr         9181081508155", "9181081508155" );
                np.AT( "St-Nr.        21/815/08150", "21/815/08150" );
                np.AT( "St.-Nr.       112181508150", "112181508150" );
                np.AT( "St.-Nr        1121081508150", "1121081508150" );
                np.AT( "Tax Number    048/815/08155", "048/815/08155" );
                np.AT( "Tax No.       304881508155", "304881508155" );
                np.AT( "Tax No        3048081508155", "3048081508155" );

                np.AT( "Found: Tax ID: 94 172 863 657", "94 172 863 657" );
                np.AT( "Not Found Tax No. 105/2638/1949", "105/2638/1949" );
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text, string s ) => np.AT( text, new[] { s.NoWhitespace().Replace( "/", string.Empty ).Replace( "-", string.Empty ) } );
        public static void AT( this NerProcessor np, string text, IList< string > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );

        public static void Check( this IList< word_t > words, IList< string > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NerOutputType.TaxIdentification)
                        select ((TaxIdentificationWord) w).TaxIdentificationNumber
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
