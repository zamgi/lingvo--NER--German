using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace Lingvo.NER.Rules.TestApp
{
    /*/// <summary>
    /// 
    /// </summary>
    internal static class Xslt
    {
        public static string MakeXsltTransformXml2Html( this Stream stream, string xsltAsString ) => XElement.Load( stream ).MakeXsltTransformXml2Html( xsltAsString );
        public static string MakeXsltTransformXml2Html( this string xml, string xsltAsString ) => XElement.Parse( xml ).MakeXsltTransformXml2Html( xsltAsString );
        public static string MakeXsltTransformXml2Html( this XElement xe, string xsltAsString ) => (new XsltTransformer( xsltAsString )).MakeXsltTransform_v1( xe );
    }
    //*/

    /// <summary>
    /// 
    /// </summary>
    internal sealed class XsltTransformer
    {
        private XslCompiledTransform _Xslt;
        private int _UTF8_preambleLength;
        public XsltTransformer( string xsltAsString )
        {
            _Xslt = new XslCompiledTransform( false );

            using ( var sr = new StringReader( xsltAsString ) )
            using ( var xr = XmlReader.Create( sr ) )
            {
                _Xslt.Load( xr, XsltSettings.TrustedXslt, null );
            }

            _UTF8_preambleLength = Encoding.UTF8.GetPreamble().Length;
        }

        public string MakeXsltTransform_v1( XElement xe, bool replaceCRLF_2_BR = true )
        {
            using ( var ms = new MemoryStream() )
            {
                using ( var xr = xe.CreateReader() )
                {
                    _Xslt.Transform( xr, null, ms );
                }

                var buffer = ms.GetBuffer();
                var xml = Encoding.UTF8.GetString( buffer, _UTF8_preambleLength, (int) ms.Length - _UTF8_preambleLength );
                if ( replaceCRLF_2_BR )
                {
                    xml = xml.Replace( "\r\n", "<br/>" ).Replace( "\n", "<br/>" );
                }
                return (xml);
            }
        }
        public XDocument MakeXsltTransform_v2( XElement xe )
        {
            var output_xdoc = new XDocument();
            using ( var xr = xe.CreateReader() )
            using ( var xw = output_xdoc.CreateWriter() )
            {
                _Xslt.Transform( xr, xw );
            }
            return (output_xdoc/*.ToString( SaveOptions.DisableFormatting )*/);            
        }
    }
}
