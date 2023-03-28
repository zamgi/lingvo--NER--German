using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using Lingvo.NER.NeuralNetwork.Tokenizing;

namespace Lingvo.NER.NeuralNetwork.TrainCorpusCreator
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static string SentSplitterResourcesXmlFilename => ConfigurationManager.AppSettings[ "SentSplitterResourcesXmlFilename" ];
        private static string UrlDetectorResourcesXmlFilename  => ConfigurationManager.AppSettings[ "UrlDetectorResourcesXmlFilename" ];
        private static string INPUT_FOLER     => ConfigurationManager.AppSettings[ "INPUT_FOLER" ];
        private static string INPUT_FILENAME  => ConfigurationManager.AppSettings[ "INPUT_FILENAME" ];
        private static string OUTPUT_FILENAME => ConfigurationManager.AppSettings[ "OUTPUT_FILENAME" ];

        private static void Main( string[] args )
        {
            try
            {
                var tokenizerConfig = new TokenizerConfig( SentSplitterResourcesXmlFilename, UrlDetectorResourcesXmlFilename );
                using var tokenizer = new Tokenizer( tokenizerConfig, replaceNumsOnPlaceholder: true );

                if ( !INPUT_FILENAME.IsNullOrWhiteSpace() )
                {
                    Run_File( tokenizer, INPUT_FILENAME, OUTPUT_FILENAME );
                }
                else
                {
                    Run_Folder( tokenizer, INPUT_FOLER, OUTPUT_FILENAME );
                }                
            }
            catch ( Exception ex )
            {
                Console_WriteError( ex );
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine( "\r\n\r\n [.....finita.....]" );
            Console.ResetColor();
            //Console.ReadLine();
        }
        private static void Run_File( Tokenizer tokenizer, string inputFileName, string outputFileName )
        {
            inputFileName  = Path.GetFullPath( inputFileName );
            outputFileName = Path.GetFullPath( outputFileName );

            if ( string.Compare( inputFileName, outputFileName, true ) == 0 ) throw (new InvalidOperationException( "[inputFileName == outputFileName]" ));

            Directory.CreateDirectory( Path.GetDirectoryName( outputFileName ) );

            using ( var sw = new StreamWriter( outputFileName, append: false ) )
            {
                Console.Write( $"1.) '{inputFileName}'..." );

                var (xd, sentCount) = CreateXDocumentFromFile( inputFileName );
                var entityCounts = ProcessFile( xd, tokenizer, sw );

                Console.WriteLine( $"end. sent-count={sentCount}, {string.Join( ", ", entityCounts.Select( p => $"{p.Key}={p.Value}" ) )}" );
                Console.WriteLine( $"\r\n OutputFileName: '{outputFileName}'. sent-count={sentCount}" );
            }
            
        }
        private static void Run_Folder( Tokenizer tokenizer, string inputFolder, string outputFileName )
        {
            inputFolder    = Path.GetFullPath( inputFolder );
            outputFileName = Path.GetFullPath( outputFileName );
            Directory.CreateDirectory( Path.GetDirectoryName( outputFileName ) );

            var sentTotalCount = 0;
            using ( var sw = new StreamWriter( outputFileName, append: false ) )
            {
                var n = 0;                
                foreach ( var fn in Directory.EnumerateFiles( inputFolder, "*", SearchOption.AllDirectories ) )
                {
                    if ( string.Compare( fn, outputFileName, true ) == 0 ) continue;

                    Console.Write( $"{++n}.) '{fn}'..." );
                    try
                    {
                        var (xd, sentCount) = CreateXDocumentFromFile( fn );
                        var entityCounts = ProcessFile( xd, tokenizer, sw );

                        Console.WriteLine( $"end. sent-count={sentCount}, {string.Join( ", ", entityCounts.Select( p => $"{p.Key}={p.Value}" ) )}" );

                        sentTotalCount += sentCount;
                    }
                    catch ( Exception ex )
                    {
                        Console_WriteError( ex );
                    }
                }
            }

            Console.WriteLine( $"\r\n OutputFileName: '{outputFileName}'. sent-count={sentTotalCount}" );
        }
        private static (XDocument xd, int sentCount) CreateXDocumentFromFile( string inputFileName )
        {
            using var sr = new StreamReader( inputFileName );

            var sb = new StringBuilder();
            var sentNumber = 0;
            var number     = 0;

            var xd = XDocument.Parse( "<text/>" );
            for ( var sent = sr.ReadLine(); sent != null; sent = sr.ReadLine() )
            {
                var xsent = $"<sent n='{sentNumber}' />".ToXElement();
                try
                {
                    var xt = $"<t>{sent}</t>".ToXElement();

                    foreach ( var xnode in xt.Nodes() )
                    {
                        switch ( xnode.NodeType )
                        {
                            case XmlNodeType.Element:
                                {
                                    var xe = (XElement) xnode;

                                    sb.Trancate();
                                    var words1 = xe.Value.SplitBySpace();
                                    foreach ( var word in words1 )
                                    {
                                        sb.AppendFormat( $"<span n='{number++}'>{word.ToHtmlEncode()}</span> " );
                                    }
                                    sb.RemoveLastChars();
                                
                                    if ( !sb.IsEmptyOrNull() )
                                    {
                                        var cls = xe.Attribute( "class" )?.Value ?? xe.Name;
                                        var master_span = $"<span class='{cls}'>{sb.ToString()}</span> ".ToXElement();
                                        xsent.Add( master_span );
                                        //var i = number;
                                        //foreach ( var span in master_span.Elements( "span" ).Reverse() )
                                        //{
                                        //    sd.Add( --i, span );
                                        //}
                                    }
                                }
                            break;

                            default:
                                {
                                    var words = xnode.ToString().SplitBySpace();
                                    foreach ( var word in words )
                                    {
                                        var span = $"<span n='{number}'>{word.ToHtmlEncode().Replace( "&amp;amp;", "&amp;" )}</span> ".ToXElement();
                                        xsent.Add( span );
                                        //sd.Add( number++, span );
                                    }
                                }
                            break;
                        }
                    }
                }
                catch ( XmlException ex )
                {
                    Debug.WriteLine( ex );

                    var words = sent.SplitBySpace();
                    foreach ( var word in words )
                    {
                        var span = $"<span n='{number}'>{word.ToHtmlEncodeForce()}</span> ".ToXElement();
                        xsent.Add( span );
                        //sd.Add( number++, span );
                    }
                }

                if ( xsent.HasElements )
                {
                    xd.Root.Add( xsent );
                    //sntd.Add( sentNumber, xsent );
                    sentNumber++;
                }
            }

            return (xd, sentNumber);
        }
        private const string O_NER_TYPE = "O";
        private static IReadOnlyDictionary< string, int > ProcessFile( XDocument xd, Tokenizer tokenizer, StreamWriter sw )
        {
#if DEBUG
            var all_ner_types = xd.Descendants().Select( x => (xe: x, cls: x.Attribute( "class" )?.Value) ).Where( t => t.cls != null )
                        .GroupBy( t => t.cls ).ToDictionary( g => g.Key, g => (xe: g.First(), cnt: g.Count()) );
#endif
            var d = new Dictionary< string, int >();

            foreach ( var xsent in xd.XPathSelectElements( "/*/sent" ) )
            {
                var parentSpans = xsent.Elements( "span" ).ToArray();
                for ( int k = 0, parentSpans_len = parentSpans.Length - 1; k <= parentSpans_len; k++ )
                {
                    var parentSpan = parentSpans[ k ];
                    var attr_class = parentSpan.Attribute( "class" )?.Value;
                    if ( attr_class.IsNullOrWhiteSpace() )
                    {
                        var token = parentSpan.Value;
                        #region [.last dot.]
                        var is_dot = (k < parentSpans_len) && xlat.IsDot( token[ ^1 ] );
                        if ( is_dot )
                        {
                            token += "XXX";
                        }
                        #endregion
                        var words = tokenizer.Run_NoSentsAllocate( token );
                        #region [.last dot.]
                        if ( is_dot )
                        {
                            token = token.Substring( 0, token.Length - "XXX".Length );
                            words.RemoveAt( words.Count - 1 );
                        }
                        #endregion
                        foreach ( var w in words )
                        {
                            sw.WriteTokenLine( w.valueOriginal, O_NER_TYPE );
                        }
                        #region comm.
                        //if ( (k < parentSpans_len) && xlat.IsDot( token[ ^1 ] ) )
                        //{
                        //    sw.WriteTokenLine( token, O_NER_TYPE );
                        //}
                        //else
                        //{
                        //    var words = tokenizer.Run_NoSentsAllocate( token );
                        //    foreach ( var w in words )
                        //    {
                        //        sw.WriteTokenLine( w.valueOriginal, O_NER_TYPE );
                        //    }
                        //} 
                        #endregion
                    }
                    else
                    {
                        var (b_nerType, i_nerType) = SpanClassToNERType( attr_class );
                        var spans = parentSpan.Elements( "span" ).ToArray();
                        for ( int i = 0, spans_len = spans.Length - 1; i <= spans_len; i++ )
                        {
                            var span = spans[ i ];
                            var nt   = (i == 0) ? b_nerType : i_nerType;

                            var token = span.Value;
                            #region [.last dot.]
                            var is_dot = (k < parentSpans_len) && (i < spans_len) && xlat.IsDot( token[ ^1 ] );
                            if ( is_dot )
                            {
                                token += "XXX";
                            } 
                            #endregion
                            var words = tokenizer.Run_NoSentsAllocate( token );
                            #region [.last dot.]
                            if ( is_dot )
                            {
                                token = token.Substring( 0, token.Length - "XXX".Length );
                                words.RemoveAt( words.Count - 1 );
                            }
                            #endregion
                            var nt2 = nt;
                            for ( int j = 0, len = words.Count - 1; j <= len; j++ )
                            {
                                var w = words[ j ];
                                if ( (j == len) && (w.extraWordType & ExtraWordType.Punctuation) == ExtraWordType.Punctuation )
                                {
                                    nt2 = O_NER_TYPE;
                                }

                                sw.WriteTokenLine( w.valueOriginal, nt2 );

                                if ( nt2 == b_nerType ) nt2 = i_nerType;
                            }

                            if ( nt != O_NER_TYPE )
                            {
                                d[ nt ] = (d.TryGetValue( nt, out var cnt ) ? cnt + 1 : 1);
                            }
                        }
                    }
                }
                sw.WriteLine();
            }

            return (d);
        }
        private static void WriteTokenLine( this StreamWriter sw, string token, string ner )
        {
            sw.Write( token );
            sw.Write( '\t' );
            sw.WriteLine( ner );
        }
        private static (string b_nerType, string i_nerType) SpanClassToNERType( string attr_class )
        {
            if ( attr_class.IsNullOrWhiteSpace() ) return (O_NER_TYPE, O_NER_TYPE);

            switch ( attr_class )
            {
                case "PER": case "N": return ("B-PER", "I-PER");
                case "ORG": case "J": return ("B-ORG", "I-ORG");
                case "LOC": case "G": return ("B-LOC", "I-LOC");
                default:
                    throw (new ArgumentException( attr_class.ToString() ));
            }
        }

        private static XElement ToXElement( this string value )
        {
            try
            {
                return (XElement.Parse( value, LoadOptions.PreserveWhitespace ));
            }
            catch ( XmlException /*ex*/ )
            {
                return (XElement.Parse( value.Replace( "&", "&amp;" ), LoadOptions.PreserveWhitespace ));
            }
        }
        private static StringBuilder RemoveLastChars( this StringBuilder sb, int count = 1 )
        {
            if ( sb.Length == 0 )
                return (sb);

            if ( count < sb.Length )
                return (sb.Remove( sb.Length - count, count ));

            sb.Length = 0;
            return (sb);
        }
        private static void Trancate( this StringBuilder sb ) => sb.Length = 0;
        private static bool IsEmptyOrNull( this StringBuilder sb ) => (sb == null || sb.Length == 0);
        private static char[] _SplitBySpace_separators = new[] { ' ', '\t' };
        private static string[] SplitBySpace( this string text ) => text.Split( _SplitBySpace_separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );
        private static string ToHtmlEncode( this string s ) => WebUtility.HtmlEncode( s );
        private static string ToHtmlEncodeForce( this string s )
        {
            var sb = new StringBuilder( s.ToHtmlEncode() );
            for ( var i = 0; i < s.Length; i++ )
            {
                var ch = sb[ i ];
                if ( ch < 0x20 )
                {
                    sb[ i ] = (char) 0x20;
                }
            }
            return (sb.ToString());
        }

        public static bool IsEmptyOrNull( this string text ) => string.IsNullOrEmpty( text );
        public static bool IsNullOrWhiteSpace( this string text ) => string.IsNullOrWhiteSpace( text );
        private static void Console_WriteError( Exception ex )
        {
            var fc = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
            //Console.ResetColor();
            Console.ForegroundColor = fc;
        }
    }
}
