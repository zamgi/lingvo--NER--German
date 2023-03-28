using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.tokenizing;
using Lingvo.NER.Rules.urls;
using NT = Lingvo.NER.Rules.NerOutputType;
using UT = Lingvo.NER.Rules.urls.UrlTypeEnum;

namespace Lingvo.NER.Rules.tests.Urls
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class UrlTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using ( var np = CreateNerProcessor( NerProcessor.UsedRecognizerTypeEnum.All_Without_Crf | NerProcessor.UsedRecognizerTypeEnum.Urls ) )
            {
                np.AT( "Wir laden Sie ein, unsere Webseite www.x-kom.de zu besuchen. Xyz file:///www.x-kom.de/xz.txt?a=1&b=xyz. Abc ftp://www.x-kom.de/x/y/?abc&def /qwerty/47567.",
                       new[] { (NT.Url, UT.Url, "www.x-kom.de"),
                               (NT.Url, UT.Url, "file:///www.x-kom.de/xz.txt?a=1&b=xyz"),
                               (NT.Url, UT.Url, "ftp://www.x-kom.de/x/y/?abc&def") } );

                np.AT( "Vertretungsberechtigter google.com Geschäftsführer ist Konrad Dießl - Tel 089/40287399 - Fax 089/40287430 - http://vasia@test-zahnzusatzversicherung.de:1234/?x@z, test-zahnzusatzversicherung.de.",
                       new[] { (NT.Url, UT.Url, "google.com"),
                               (NT.Url, UT.Url, "http://vasia@test-zahnzusatzversicherung.de:1234/?x@z"),
                               (NT.Url, UT.Url, "test-zahnzusatzversicherung.de") } );

            }
        }

        [Fact] public void T_2()
        {
            using ( var np = CreateNerProcessor( NerProcessor.UsedRecognizerTypeEnum.All_Without_Crf | NerProcessor.UsedRecognizerTypeEnum.Urls ) )
            {
                np.AT( "Vertretungsberechtigter Geschäftsführer ist Konrad Dießl - Tel 089/40287399 - Fax 089/40287430 - info@test-zahnzusatzversicherung.de – www.test-zahnzusatzversicherung.de.",
                       new[] { (NT.Email, UT.Email, "info@test-zahnzusatzversicherung.de"), (NT.Url, UT.Url, "www.test-zahnzusatzversicherung.de") } );
            }
        }

        [Fact] public void T_3()
        {
            using ( var np = CreateNerProcessor( NerProcessor.UsedRecognizerTypeEnum.All_Without_Crf | NerProcessor.UsedRecognizerTypeEnum.Urls ) )
            {
                np.AT( "thorsten.wiese@rocketta.de"   , (NT.Email, UT.Email, "thorsten.wiese@rocketta.de") );
                np.AT( "thorsten.wiese[at]rocketta.de", (NT.Email, UT.Email, "thorsten.wiese[at]rocketta.de") );
                np.AT( "thorsten.wiese(at)rocketta.de", (NT.Email, UT.Email, "thorsten.wiese(at)rocketta.de") );
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text, (NT nerOutputType, UT urlType, string valueOriginal) p ) => np.AT( text, new[] { p } );
        public static void AT( this NerProcessor np, string text, IList< (NT nerOutputType, UT urlType, string valueOriginal) > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );

        public static void Check( this IList< word_t > words, IList< (NT nerOutputType, UT urlType, string valueOriginal) > refs )
        {
            static (bool success, (NT nerOutputType, UT urlType, string valueOriginal) x) try_get( word_t w )
            {
                switch ( w.nerOutputType )
                {
                    case NT.Url:
                        var u = (UrlWord) w;
                        return (true, (u.nerOutputType, u.UrlType, u.valueOriginal));

                    case NT.Email:
                        var e = (EmailWord) w;
                        return (true, (e.nerOutputType, e.UrlType, e.valueOriginal));

                    default:
                        return (false, default);
                }
            };

            var hyps = (from w in words
                        let t = try_get( w )
                        where (t.success)
                        select (t.x)
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

        private static int IndexOf( this IList<(NT nerOutputType, UT urlType, string valueOriginal)> pairs, in (NT nerOutputType, UT urlType, string valueOriginal) p, int startIndex )
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

        private static bool IsEqual( in (NT nerOutputType, UT urlType, string valueOriginal) x, in (NT nerOutputType, UT urlType, string valueOriginal) y )
            => (x.nerOutputType == y.nerOutputType) && (x.urlType == y.urlType) && (x.valueOriginal == y.valueOriginal);
    }
}
