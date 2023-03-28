using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Lingvo.NER.Rules.Names;
using Lingvo.NER.Rules.TestApp.Properties;
using Lingvo.NER.Rules.tokenizing;
using Lingvo.NER.Rules.urls;

namespace Lingvo.NER.Rules.TestApp
{
    /// <summary>
    /// 
    /// </summary>
    internal static class TestRunner_1
    {
        /// <summary>
        /// 
        /// </summary>
        private struct TestTuple
        {
            public string Text        { get; set; }
            public string Description { get; set; }
            public IReadOnlyList< SuperWord > NerWords { get; set; }
        }
        /// <summary>
        /// 
        /// </summary>
        private struct TestTupleResult
        {
            public TestTuple t;
            public IReadOnlyList< (SuperWord expected, word_t real) > discrepancies;

            public static implicit operator TestTupleResult( in (TestTuple t, IReadOnlyList< (SuperWord expected, word_t real) > discrepancies) x ) => new TestTupleResult() { t = x.t, discrepancies = x.discrepancies };
        }

        private static async Task< IReadOnlyCollection< TestTuple > > ReadTuplesFromFiles( params string[] fileNames )
        {
            var tuples = new List< TestTuple >( fileNames.Length * 100 );
            var hs     = new HashSet< string >( fileNames.Length * 100 );

            var jso = new JsonSerializerOptions( JsonSerializerDefaults.Web ) { AllowTrailingCommas = true, IncludeFields = true, ReadCommentHandling = JsonCommentHandling.Skip };
            jso.Converters.Add( new JsonStringEnumConverter() );

            foreach ( var fileName in fileNames )
            {
                using var fs = File.OpenRead( fileName );

                var raw_tuples = await JsonSerializer.DeserializeAsync< List< TestTuple > >( fs, jso ).CAX();
            
                foreach ( var t in raw_tuples )
                {
                    if ( t.Text.IsNullOrEmpty() ) continue;
                    if ( hs.Add( t.Text ) )
                    {
                        tuples.Add( t );
                    }
                }
            }

            return (tuples);
        }

        //public static Task Run( NerProcessorConfig config, string inputFileName, string outputHtmlFileName ) => Run( config, new[] { inputFileName }, outputHtmlFileName );
        public static async Task Run( NerProcessorConfig config/*, string[] inputFileNames, string outputHtmlFileName*/ )
        {
            var tuples = await ReadTuplesFromFiles( ConfigEx.TEST_INPUT_FILENAME_1 ).CAX();

            var results = ProcessNer( tuples, config );
            WriteToOutputFile( results, ConfigEx.OUTPUT_HTML_FILENAME_1 );

            #region comm. open output file
            //if ( true ) //openOutputHtmlFileName )
            //{
            //    using var p = Process.Start( new ProcessStartInfo( Config.OUTPUT_HTML_FILENAME ) { UseShellExecute = true } );
            //} 
            #endregion
        }

        private static IReadOnlyList< TestTupleResult > ProcessNer( IReadOnlyCollection< TestTuple > tuples, NerProcessorConfig config )
        {
            var results = new List< TestTupleResult >( tuples.Count );

            var buf = new List< (SuperWord expected, word_t real) >();
            using ( var nerProcessor = new NerProcessor( config ) )
            {
                var n = 0;
                foreach ( var t in tuples )
                {
                    Console.WriteLine( $"\r\n-------------------------------------------------" );
                    Console.WriteLine( $"{++n} of {tuples.Count}). " + (t.Description.IsNullOrEmpty() ? null : $"[{t.Description}], ") + $"text: '{t.Text.Cut()}'" );

                    var (nerWords, _, _/*nerUnitedEntities, relevanceRanking*/) = nerProcessor.Run_UseSimpleSentsAllocate_v2( t.Text );

                    if ( TryGetDiscrepancy( t.NerWords, nerWords, buf ) )
                    {
                        var discrepancies = buf.ToArray();
                        results.Add( (t, discrepancies) );

                        Console.WriteLine( " =>" );
                        discrepancies.Print2Console();                        
                    }
                    else
                    {
                        results.Add( (t, null) );

                        Console.WriteLine( " => ok." );
                    }
                }
            }

            return (results);
        }
        private static void WriteToOutputFile( IReadOnlyList< TestTupleResult > results, string outputHtmlFileName )
        {
            Console.WriteLine( $"\r\n-------------------------------------------------" );
            Console.Write( $"start write output file '{outputHtmlFileName}'..." );

            var buf = new StringBuilder();

            if ( !Directory.Exists( Path.GetDirectoryName( outputHtmlFileName ) ) ) Directory.CreateDirectory( Path.GetDirectoryName( outputHtmlFileName ) );
            using var sw = new StreamWriter( outputHtmlFileName );
           
            var dt = $"Lingvo.NER.Rules.Tests; {DateTime.Now:dd.MM.yyyy, HH:mm}";
            sw.WR( Resources.begin_of_html_1.Replace( "<title></title>", $"<title>{dt.Escape()}</title>" ) )
                .WR( "<h4>" ).WR_Escape( dt ).WR( "</h4>" )
                .WR( "<table>" )
                .WR( "<tr>" )
                .WR( "<th> # </th>" )
                .WR( "<th> Text </th>" )
                .WR( "<th> Expectation </th>" )
                .WR( "<th> Result </th>" )
                .WR( "</tr>" );

            var n = 0;
            foreach ( var x in results )
            {
                sw.WR( "<tr>" )
                    .WR( "<td>" ).WR( ++n ).WR( "</td>" )
                    .WR( "<td>" );
                if ( !x.t.Description.IsNullOrEmpty() )
                    sw.WR( "<div class='descr'>" ).WR_Escape( x.t.Description ).WR( "</div>" );
                sw.WR( "<div class='text'>" ).WR_Escape( x.t.Text ).WR( "</div>" ).WR( "</td>" );
                sw.WR( "<td>" ).WR( x.t.NerWords.ToHtml( buf ) ).WR( "</td>" );

                if ( x.discrepancies != null )
                {
                    sw.WR( "<td>" ).WR( x.discrepancies.ToHtml( buf ) ).WR( "</td>" );
                }
                else
                {
                    sw.Write( "<td class='ok'> Ok </td>" );
                }

                sw.Write( "</tr>" );
            }
            sw.WR( "</table>" ).WR( Resources.end_of_html );

            Console.WriteLine( "end." );
        }

        private static bool TryGetDiscrepancy( IReadOnlyList< SuperWord > nerWords, IReadOnlyList< word_t > realNerAllocatedWords, List< (SuperWord expected, word_t real) > buf )
        {
            buf.Clear();
            if ( nerWords == null ) nerWords = new SuperWord[ 0 ];

            using var e1 = nerWords.GetEnumerator();
            using var e2 = realNerAllocatedWords.GetEnumerator();
            for ( ; e1.MoveNext() & e2.MoveNext(); )
            {
                var x = e1.Current;
                var w = e2.Current;

                if ( !IsEquals( x, w ) )
                {
                    buf.Add( (x, w) );
                }
            }
            if ( nerWords.Count < realNerAllocatedWords.Count )
            {
                var w = e2.Current;
                buf.Add( (null, w) );

                for ( ; e2.MoveNext(); )
                {
                    w = e2.Current;
                    buf.Add( (null, w) );
                }
            }
            else if ( realNerAllocatedWords.Count < nerWords.Count )
            {
                var x = e1.Current;
                buf.Add( (x, null) );

                for ( ; e1.MoveNext(); )
                {
                    x = e1.Current;
                    buf.Add( (x, null) );
                }
            }

            return (buf.Count != 0);
        }
        private static bool IsEquals( SuperWord x, word_t w )
        {
            if ( x.nerOutputType == w.nerOutputType )
            {
                switch ( x.nerOutputType )
                {
                    case NerOutputType.Name:
                        var nw = (NameWord) w;
                        return ((x.Firstname == nw.Firstname) && (x.Surname == nw.Surname) && (x.TextPreambleType == nw.TextPreambleType));

                    case NerOutputType.Email:
                    case NerOutputType.Url:
                        var ew = (UrlOrEmailWordBase) w;
                        return (/*(x.UrlType == ew.UrlType) &&*/ (x.valueOriginal == ew.valueOriginal));
                }
            }
            return (false);
        }

        #region [.SuperWord => html.]
        private static string ToHtml( this IReadOnlyList< SuperWord > sws, StringBuilder buf )
        {
            buf.Clear().Append( "<table class='inner'>" );
            var n = 0;
            foreach ( var sw in sws )
            {
                buf.Append( "<tr>" )
                   .Append( "<td>" ).Append( ++n ).Append( "</td>" );
                buf.Write( sw );
                buf.Append( "</tr>" );
            }
            buf.Append( "</table>" );
            var html = buf.ToString();
            return (html);
        }
        private static void WriteAsTableTo( this SuperWord sw, StringBuilder buf )
        {
            buf.Append( "<table class='inner3'>" );
            buf.Append( "<tr>" );
            buf.Write( sw );
            buf.Append( "</tr>" );
            buf.Append( "</table>" );
        }
        private static void Write( this StringBuilder buf, SuperWord sw )
        {
            var nt = sw.nerOutputType.ToString().Escape();
            buf.Append( $"<td><span class='{nt}'>" ).Append( nt ).Append( "</span></td>" );
            switch ( sw.nerOutputType )
            {
                case NerOutputType.Name:
                    buf.Append( "<td>" )
                       .Append( "<div> first-name: </div>" )
                       .Append( "<div> sur-name: </div>" )
                       .Append( "</td>" );
                    buf.Append( "<td>" )
                       .Append( $"<span class='{nt}'>" ).Append( sw.Firstname.Escape() ).Append( "</span>" )
                       .Append( $"<span class='{nt}'>" ).Append( sw.Surname  .Escape() ).Append( "</span>" )
                       .Append( "</td>" );
                    break;

                case NerOutputType.Url:
                case NerOutputType.Email:
                    buf.Append( "<td>" )
                       .Append( "<div> type: </div>" )
                       .Append( "<div> val: </div>" )
                       .Append( "</td>" );
                    buf.Append( "<td>" )
                       .Append( $"<span class='{nt}'>" ).Append( sw.UrlType ).Append( "</span>" )
                       .Append( $"<span class='{nt}'>" ).Append( sw.valueOriginal.Escape() ).Append( "</span>" )
                       .Append( "</td>" );
                    break;

                default:
                    //throw new NotImplementedException();
                    buf.Append( "<td colspan='2'><h2 style='color: red'>[NOT-IMPLEMENTED]</h2></td>" );
                    break;
            }
        }
        #endregion

        #region [.word_t => html.]
        private static void WriteAsTableTo( this word_t w, StringBuilder buf )
        {
            buf.Append( "<table class='inner3'>" );
            buf.Append( "<tr>" )
               .Append( $"<td><span class='{w.nerOutputType}'>" ).Append( w.nerOutputType ).Append( "</span></td>" );
            switch ( w.nerOutputType )
            {
                case NerOutputType.Name : buf.Write( (NameWord) w ); break;
                case NerOutputType.Url  : buf.Write( (UrlWord) w ); break;
                case NerOutputType.Email: buf.Write( (EmailWord) w ); break;

                default:
                    //throw new NotImplementedException(); 
                    buf.Append( "<td colspan='2'><h2 style='color: red'>[NOT-IMPLEMENTED]</h2></td>" );
                    break;
            }
            buf.Append( "</tr>" );
            buf.Append( "</table>" );
        }
        private static void Write( this StringBuilder buf, NameWord w )
        {
            buf.Append( "<td>" )
                .Append( "<div> first-name: </div>" )
                .Append( "<div> sur-name: </div>" )
                .Append( "</td>" );
            buf.Append( "<td>" )
                .Append( $"<span class='{w.nerOutputType}'>" ).Append( w.Firstname.Escape() ).Append( "</span>" )
                .Append( $"<span class='{w.nerOutputType}'>" ).Append( w.Surname.Escape() ).Append( "</span>" )
                .Append( "</td>" );
        }
        private static void Write( this StringBuilder buf, UrlOrEmailWordBase w )
        {
            buf.Append( "<td>" )
               .Append( "<div> type: </div>" )
               .Append( "<div> val: </div>" )
               .Append( "</td>" );
            buf.Append( "<td>" )
               .Append( $"<span class='{w.nerOutputType}'>" ).Append( w.UrlType ).Append( "</span>" )
               .Append( $"<span class='{w.nerOutputType}'>" ).Append( w.valueOriginal.Escape() ).Append( "</span>" )
               .Append( "</td>" );
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        private enum discrepancy_type_enum
        {
            Extra,
            Missing,
            Discrepancy
        }
        private static string ToHtml( this IReadOnlyList< (SuperWord expected, word_t real) > discrepancies, StringBuilder buf )
        {
            static string get_error_prefix( discrepancy_type_enum dt )
            {
                switch ( dt )
                {
                    case discrepancy_type_enum.Extra: return ("Extra");
                    case discrepancy_type_enum.Missing: return ("Missing");
                    case discrepancy_type_enum.Discrepancy: return ("Discrepancy");
                    default: throw (new ArgumentException( dt.ToString() ));
                }
            };
            static discrepancy_type_enum get_discrepancy_type( in (SuperWord expected, word_t real) d )
            {
                if ( d.expected == null )
                {
                    return (discrepancy_type_enum.Extra);
                }
                else if ( d.real == null )
                {
                    return (discrepancy_type_enum.Missing);
                }
                else
                {
                    return (discrepancy_type_enum.Discrepancy);
                }
            };

            buf.Clear().Append( "<table class='inner2'>" )
                       .Append( "<tr>" )
                       .Append( "<th/>" )
                       .Append( "<th> error type </th>" )
                       .Append( "<th> expected </th>" )
                       .Append( "<th> real </th>" )
                       .Append( "</tr>" );
            var n = 0;
            foreach ( var d in discrepancies )
            {
                var dt = get_discrepancy_type( in d );
                var error_prefix = get_error_prefix( dt ).Escape();
                buf.Append( "<tr>" )
                   .Append( "<td>" ).Append( ++n ).Append( "</td>" )
                   .Append( $"<td><span class='{error_prefix}'>" ).Append( error_prefix ).Append( "</span></td>" );

                switch ( dt )
                {
                    case discrepancy_type_enum.Extra: //if ( d.expected == null )
                        buf.Append( "<td></td>" ).Append( "<td>" );
                        d.real.WriteAsTableTo( buf );
                        buf.Append( "</td>" );
                        break;

                    case discrepancy_type_enum.Missing: //if ( d.real == null )
                        buf.Append( "<td>" );
                        d.expected.WriteAsTableTo( buf );
                        buf.Append( "</td>" ).Append( "<td></td>" );
                        break;

                    case discrepancy_type_enum.Discrepancy: //if ( d.expected != d.real )
                        buf.Append( "<td>" );
                        d.expected.WriteAsTableTo( buf );
                        buf.Append( "</td>" );
                        buf.Append( "<td>" );
                        d.real.WriteAsTableTo( buf );
                        buf.Append( "</td>" );
                        break;

                    default: throw (new ArgumentException( dt.ToString() ));
                }

                buf.Append( "</tr>" );
            }
            buf.Append( "</table>" );
            var html = buf.ToString();
            return (html);
        }
        //private static string ToHtml( this in (SuperWord expected, word_t real) d )
        //{
        //    if ( d.expected == null )
        //    {
        //        return ($"extra: {{{d.real}}}".Escape());
        //    }
        //    else if ( d.real == null )
        //    {
        //        return ($"missing: {{{d.expected}}}".Escape());
        //    }
        //    else
        //    {
        //        return ($"discrepancy: expected={{{d.expected}}} => real={{{d.real}}}".Escape());
        //    }
        //}


        private static void Print2Console( this IList< (SuperWord expected, word_t real) > discrepancies )
        {
            foreach ( var d in discrepancies )
            {
                d.Print2Console();
            }
        }        
        private static void Print2Console( this in (SuperWord expected, word_t real) d )
        {
            var fc = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red/*DarkRed*/;
            if ( d.expected == null )
            {
                Console.WriteLine( $"extra: {{{d.real}}}" );
            }
            else if ( d.real == null )
            {
                Console.WriteLine( $"missing: {{{d.expected}}}" );
            }
            else
            {
                Console.WriteLine( $"discrepancy: expected={{{d.expected}}} => real={{{d.real}}}" );
            }
            Console.ForegroundColor = fc;
        }
        private static void Print2Console( this IList< word_t > nerWords, int relevanceRanking )
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

                    case NerOutputType.PhoneNumber         : Console.ForegroundColor = ConsoleColor.Magenta; break;
                    case NerOutputType.Address             : Console.ForegroundColor = ConsoleColor.Red;     break;
                    case NerOutputType.Url                 : Console.ForegroundColor = ConsoleColor.Green;   break;
                    case NerOutputType.Email               : Console.ForegroundColor = ConsoleColor.Green;   break;
                    case NerOutputType.AccountNumber       : Console.ForegroundColor = ConsoleColor.Yellow;  break;
                    case NerOutputType.Name                : Console.ForegroundColor = ConsoleColor.Red;     break;
                    case NerOutputType.CustomerNumber      : Console.ForegroundColor = ConsoleColor.Green;   break;
                    case NerOutputType.Birthday            : Console.ForegroundColor = ConsoleColor.Yellow;  break;
                    case NerOutputType.Birthplace          : Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                    case NerOutputType.MaritalStatus       : Console.ForegroundColor = ConsoleColor.DarkMagenta; break;
                    case NerOutputType.Nationality         : Console.ForegroundColor = ConsoleColor.DarkCyan;  break;
                    case NerOutputType.CreditCard          : Console.ForegroundColor = ConsoleColor.Cyan;    break;
                    case NerOutputType.PassportIdCardNumber: Console.ForegroundColor = ConsoleColor.Magenta; break;
                    case NerOutputType.CarNumber           : Console.ForegroundColor = ConsoleColor.Red;     break;
                    case NerOutputType.HealthInsurance     : Console.ForegroundColor = ConsoleColor.Green;   break;
                    case NerOutputType.DriverLicense       : Console.ForegroundColor = ConsoleColor.Blue;    break;
                    case NerOutputType.SocialSecurity      : Console.ForegroundColor = ConsoleColor.Cyan;    break;
                    case NerOutputType.TaxIdentification   : Console.ForegroundColor = ConsoleColor.Magenta; break;
                    default: Console.ResetColor(); break;
                }
                Console.WriteLine( word );
            }
            Console.ResetColor();
            Console.WriteLine();
                
            Console.WriteLine( "-------------------------------------------------\r\n" );
        }
    }
}