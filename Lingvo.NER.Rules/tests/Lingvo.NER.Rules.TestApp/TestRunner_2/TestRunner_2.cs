using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Lingvo.NER.Rules.Address;
using Lingvo.NER.Rules.BankAccounts;
using Lingvo.NER.Rules.Birthdays;
using Lingvo.NER.Rules.Birthplaces;
using Lingvo.NER.Rules.CarNumbers;
using Lingvo.NER.Rules.Companies;
using Lingvo.NER.Rules.CreditCards;
using Lingvo.NER.Rules.CustomerNumbers;
using Lingvo.NER.Rules.DriverLicenses;
using Lingvo.NER.Rules.HealthInsurances;
using Lingvo.NER.Rules.MaritalStatuses;
using Lingvo.NER.Rules.Names;
using Lingvo.NER.Rules.Nationalities;
using Lingvo.NER.Rules.PassportIdCardNumbers;
using Lingvo.NER.Rules.PhoneNumbers;
using Lingvo.NER.Rules.SocialSecurities;
using Lingvo.NER.Rules.TaxIdentifications;
using Lingvo.NER.Rules.tokenizing;
using Lingvo.NER.Rules.urls;

using Lingvo.NER.Rules.TestApp.Properties;

namespace Lingvo.NER.Rules.TestApp
{
    /// <summary>
    /// 
    /// </summary>
    internal static class TestRunner_2
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly struct TestTuple
        {
            public string                  Text        { get; init; }
            public string                  Description { get; init; }
            public IReadOnlyList< word_t > NerWords    { get; init; }
            public string                  Html        { get; init; }
        }
        /// <summary>
        /// 
        /// </summary>
        private struct TestTupleResult
        {
            public TestTuple t;
            public IReadOnlyList< word_t > RealAllocatedNerWords         { get; init; }
            public string                  RealHtml                      { get; init; }
            public bool                    IsExpectationAndRealAreEquals { get; init; }

            public static implicit operator TestTupleResult( in (TestTuple t, IReadOnlyList< word_t > realAllocatedNerWords, string realHtml, bool isExpectationAndRealAreEquals) x ) 
                => new TestTupleResult() { t = x.t, RealAllocatedNerWords = x.realAllocatedNerWords, RealHtml = x.realHtml, IsExpectationAndRealAreEquals = x.isExpectationAndRealAreEquals };
        }
        /// <summary>
        /// 
        /// </summary>
        private sealed class words_EqualityComparer : IEqualityComparer< word_t >
        {
            private string _OriginalText;
            public words_EqualityComparer( string originalText ) => _OriginalText = originalText;
            public bool Equals( word_t x, word_t y )
            {
                if ( x.nerOutputType == y.nerOutputType )
                {
                    var x_v = (x.length != 0) ? _OriginalText.Substring( x.startIndex, x.length ) : x.valueOriginal;
                    var y_v = (y.length != 0) ? _OriginalText.Substring( y.startIndex, y.length ) : y.valueOriginal;
                    return (x_v == y_v);
                }
                return (false);
            }
            public int GetHashCode( [DisallowNull] word_t w ) => throw new NotImplementedException(); // w.nerOutputType.GetHashCode() ^ w.valueOriginal.GetHashCode();
        }

        private static IReadOnlyList< word_t > ParseNerWords( XElement xtext )
        {
            var nerWords = new List< word_t >( xtext.Elements().Count() );
            foreach ( var x in xtext.Elements() )
            {
                var nerOutputType_text = (x.Name.LocalName == "span") ? x.Attribute( "class" )?.Value : x.Name.LocalName;
                if ( Enum.TryParse< NerOutputType >( nerOutputType_text, true, out var nerOutputType ) )
                {
                    var w = new word_t()
                    {
                        nerOutputType = nerOutputType,
                        valueOriginal = x.Value, //x.ToString( SaveOptions.DisableFormatting ),
                        //valueUpper    = x.Value.ToUpperInvariant(),
                    };
                    nerWords.Add( w );
                }
                else
                {
                    Console_WriteLine( $"Unknown NER-type: '{nerOutputType_text}' => '{x.ToString( SaveOptions.DisableFormatting )}'", ConsoleColor.DarkYellow );
                }
            }
            return (nerWords);
        }
        private static async Task< IReadOnlyCollection< TestTuple > > ReadTuplesFromFiles( XsltTransformer xsltTransformer, params string[] fileNames )
        {
            var tuples = new List< TestTuple >( fileNames.Length * 100 );
            var hs     = new HashSet< string >( fileNames.Length * 100 );
            
            foreach ( var fileName in fileNames )
            {
                using var fs = File.OpenRead( fileName );
                var xdoc = await XDocument.LoadAsync( fs, LoadOptions.PreserveWhitespace, default ).CAX();

                foreach ( var xtest in xdoc.Root.ElementsByLocalName( "test" ) )
                {                    
                    //var xtext = xtest.ElementsByLocalName( "text" ).FirstOrDefault();
                    //if ( xtext == null ) continue;
                    var text = xtest.Value; //xtest.ToString( SaveOptions.DisableFormatting );

                    if ( text.IsNullOrWhiteSpace() ) continue;
                    if ( hs.Add( text ) )
                    {
                        var description = xtest.AttributesByLocalName( "Description" ).FirstOrDefault()?.Value;
                        var html = xsltTransformer.MakeXsltTransform_Ex( xtest );
                        var nerWords = ParseNerWords( xtest );

                        var t = new TestTuple()
                        {
                            Description = description,
                            //XText       = xtest,
                            Text        = text,
                            NerWords    = nerWords,
                            Html        = html,
                        };
                        tuples.Add( t );
                    }
                }
            }

            return (tuples);
        }

        public static async Task Run( NerProcessorConfig config )
        {
            var xsltTransformer = new XsltTransformer( Resources.ner_xml_2_html__piece );
            var tuples = await ReadTuplesFromFiles( xsltTransformer, ConfigEx.TEST_INPUT_FILENAME_2 ).CAX();

            var results = ProcessNer( tuples, xsltTransformer, config );
            WriteToOutputFile( results, ConfigEx.OUTPUT_HTML_FILENAME_2 );

            #region comm. open output file
            //if ( true ) //openOutputHtmlFileName )
            //{
            //    using var p = Process.Start( new ProcessStartInfo( Config.OUTPUT_HTML_FILENAME ) { UseShellExecute = true } );
            //} 
            #endregion
        }

        private static IReadOnlyList< TestTupleResult > ProcessNer( IReadOnlyCollection< TestTuple > tuples, XsltTransformer xsltTransformer, NerProcessorConfig config )
        {
            static void Console_Write_Hr() => Console.WriteLine( $"\r\n-------------------------------------------------" );
            static void Console_Write_Result( bool isExpectationAndRealAreEquals )
            {
                if ( isExpectationAndRealAreEquals )
                {
                    Console.WriteLine( " => ok." );                    
                }
                else
                {
                    Console.Write( " => " );
                    Console_Write( "MISMATCH", ConsoleColor.DarkRed );
                    Console.WriteLine( "." );
                }
            };

            var results = new List< TestTupleResult >( tuples.Count );
            
            using ( var nerProcessor = new NerProcessor( config ) )
            {
                var n = 0;
                var mismatchCount = 0;
                foreach ( var t in tuples )
                {
                    Console_Write_Hr();
                    Console.WriteLine( $"{++n} of {tuples.Count}). " + (t.Description.IsNullOrEmpty() ? null : $"[{t.Description}], ") + $"text: '{t.Text.Cut()}'" );

                    var (nerWords, _, _/*nerUnitedEntities, relevanceRanking*/) = nerProcessor.Run_UseSimpleSentsAllocate_v2( t.Text );
                    var xe = CreateNerXElement( in t, nerWords );
                    var html = xsltTransformer.MakeXsltTransform_Ex( xe );
                    var isExpectationAndRealAreEquals = t.NerWords.SequenceEqual( nerWords, new words_EqualityComparer( t.Text ) );

                    results.Add( (t, nerWords, html, isExpectationAndRealAreEquals) );

                    if ( !isExpectationAndRealAreEquals ) mismatchCount++;

                    Console_Write_Result( isExpectationAndRealAreEquals );
                }

                Console_Write_Hr();
                Console.WriteLine( $"total: {tuples.Count}, success: {tuples.Count - mismatchCount}" + ((mismatchCount != 0) ? $", mismatch: {mismatchCount}" : null) + '.' );
            }

            return (results);
        }
        private static XElement CreateNerXElement( in TestTuple t, IReadOnlyList< word_t > realNerAllocatedWords )
        {
            //<test Description="Names #1">
            var xner = new XElement( "test" );
            if ( !t.Description.IsNullOrEmpty() ) xner.Add( new XAttribute( "Description", t.Description ) );

            var startIndex = 0;
            int len;
            foreach ( var w in realNerAllocatedWords )
            {
                len = w.startIndex - startIndex;
                if ( 0 < len )
                {
                    xner.Add( new XText( t.Text.Substring( startIndex, len ) ) );
                    startIndex = w.endIndex();
                }

                var xe = new XElement( w.nerOutputType.ToString() );
                switch ( w.nerOutputType )
                {
                    case NerOutputType.Name                : xe.Add( CreateXAttributes( (NameWord) w ) ); break;
                    case NerOutputType.Email               : 
                    case NerOutputType.Url                 : xe.Add( CreateXAttributes( (UrlOrEmailWordBase)       w ) ); break;
                    case NerOutputType.PhoneNumber         : xe.Add( CreateXAttributes( (PhoneNumberWord)          w ) ); break;
                    case NerOutputType.Address             : xe.Add( CreateXAttributes( (AddressWord)              w ) ); break;
                    case NerOutputType.AccountNumber       : xe.Add( CreateXAttributes( (BankAccountWord)          w ) ); break;
                    case NerOutputType.MaritalStatus       : xe.Add( CreateXAttributes( (MaritalStatusWord)        w ) ); break;
                    case NerOutputType.CustomerNumber      : xe.Add( CreateXAttributes( (CustomerNumberWord)       w ) ); break;
                    case NerOutputType.Birthday            : xe.Add( CreateXAttributes( (BirthdayWord)             w ) ); break;
                    case NerOutputType.Birthplace          : xe.Add( CreateXAttributes( (BirthplaceWord)           w ) ); break;
                    case NerOutputType.Nationality         : xe.Add( CreateXAttributes( (NationalityWord)          w ) ); break;
                    case NerOutputType.CreditCard          : xe.Add( CreateXAttributes( (CreditCardWord)           w ) ); break;
                    case NerOutputType.PassportIdCardNumber: xe.Add( CreateXAttributes( (PassportIdCardNumberWord) w ) ); break;
                    case NerOutputType.CarNumber           : xe.Add( CreateXAttributes( (CarNumberWord)            w ) ); break;
                    case NerOutputType.HealthInsurance     : xe.Add( CreateXAttributes( (HealthInsuranceWord)      w ) ); break;
                    case NerOutputType.DriverLicense       : xe.Add( CreateXAttributes( (DriverLicenseWord)        w ) ); break;
                    case NerOutputType.SocialSecurity      : xe.Add( CreateXAttributes( (SocialSecurityWord)       w ) ); break;
                    case NerOutputType.TaxIdentification   : xe.Add( CreateXAttributes( (TaxIdentificationWord)    w ) ); break;
                    case NerOutputType.Company             : xe.Add( CreateXAttributes( (CompanyWord)              w ) ); break;

                    default:
                        Console.WriteLine( $"{nameof(CreateNerXElement)} => {w.nerOutputType} => {nameof(NotImplementedException)}" );
                        break;
                }
                xe.Add( new XText( t.Text.Substring( w.startIndex, w.length ) /*w.valueOriginal*/ ) );
                xner.Add( xe );
            }
            len = t.Text.Length - startIndex;
            if ( 0 < len )
            {
                xner.Add( new XText( t.Text.Substring( startIndex, len ) ) );
            }
            return (xner);
        }

        private static void WriteToOutputFile( IReadOnlyList< TestTupleResult > results, string outputHtmlFileName )
        {
            Console.WriteLine( $"\r\n-------------------------------------------------" );
            Console.Write( $"start write output file '{outputHtmlFileName}'..." );

            if ( !Directory.Exists( Path.GetDirectoryName( outputHtmlFileName ) ) ) Directory.CreateDirectory( Path.GetDirectoryName( outputHtmlFileName ) );
            using var sw = new StreamWriter( outputHtmlFileName );
           
            var title = $"Lingvo.NER.Rules.Tests; {DateTime.Now:dd.MM.yyyy, HH:mm}";
            sw.WR( Resources.begin_of_html_2.Replace( "<title></title>", $"<title>{title.Escape()}</title>" ) )
                .WR( "<h4>" ).WR_Escape( title ).WR( "</h4>" )
                .WR( "<table>" )
                .WR( "<tr>" )
                .WR( "<th> # </th>" )
                .WR( "<th> Pattern (Expectation) </th>" )
                .WR( "<th> Processed (Real) </th>" )
                .WR( "<th> Summary </th>" )
                .WR( "</tr>" );

            var n = 0;
            var mismatchCount = 0;
            foreach ( var x in results )
            {
                sw.WR( "<tr>" )
                  .WR( "<td>" ).WR( ++n ).WR( "</td>" )
                  .WR( "<td>" );
                //if ( !x.t.Description.IsNullOrEmpty() ) sw.WR( "<div class='descr'>" ).WR_Escape( x.t.Description ).WR( "</div>" );
                sw.WR( "<div class='text'>" ).WR/*WR_Escape*/( x.t.Html ).WR( "</div>" )
                  .WR( "</td>" );

                sw.WR( "<td>" )
                  .WR( "<div class='text'>" ).WR/*WR_Escape*/( x.RealHtml ).WR( "</div>" )
                  .WR( "</td>" );

                if ( x.IsExpectationAndRealAreEquals )
                {
                    sw.WR( "<td class='ok'> <span>Ok</span> </td>" );
                }
                else
                {
                    mismatchCount++;
                    sw.WR( "<td class='mismatch'> <span>MISMATCH</span> </td>" );
                }

                sw.WR( "</tr>" );
            }
            sw.WR( "<tr/><td colspan='4'/>" )
              .WR( "<tr><td/>" )
              .WR( "<td class='summary'>" )
                .WR( $"<div>Total: {results.Count}</div>" )
                .WR( $"<div class='ok'>Success (Ok): {results.Count - mismatchCount}</div>" )
                .WR( (mismatchCount != 0) ? $"<div class='mismatch'>Mismatch: {mismatchCount}</div>" : null )
              .WR( "</td>" )
              .WR( "<td/><td/><tr/>" );
            sw.WR( "</table>" ).WR( Resources.end_of_html );

            Console.WriteLine( "end." );
        }

        private static IEnumerable< XElement > ElementsByLocalName( this XContainer x, string localName )
            => x.Elements().Where( e => e.Name.LocalName.Equals( localName, StringComparison.InvariantCultureIgnoreCase ) );
        private static IEnumerable< XAttribute > AttributesByLocalName( this XElement x, string localName )
            => x.Attributes().Where( a => a.Name.LocalName.Equals( localName, StringComparison.InvariantCultureIgnoreCase ) );
        private static string MakeXsltTransform_Ex( this XsltTransformer xsltTransformer, XElement xtest, bool trimInner = true, bool replaceCRLF_2_BR = true )
        {
            static void trim_start( XNode n )
            {
                if ( n is XElement xe )
                {
                    trim_start( xe.FirstNode );
                }
                else if ( n is XText xt )
                {
                    xt.Value = xt.Value.TrimStart();
                }
            };
            static void trim_end( XNode n )
            {
                if ( n is XElement xe )
                {
                    trim_end( xe.LastNode );
                }
                else if ( n is XText xt )
                {
                    xt.Value = xt.Value.TrimEnd();
                }
            };

            if ( trimInner )
            {
                trim_start( xtest.FirstNode );
                trim_end  ( xtest.LastNode  );
            }
            return (xsltTransformer.MakeXsltTransform_v1( xtest, replaceCRLF_2_BR ));
        }

        private static void Console_Write( string msg, ConsoleColor color )
        {
            var fc = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write( msg );
            Console.ForegroundColor = fc;
        }
        private static void Console_WriteLine( string msg, ConsoleColor color )
        {
            var fc = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine( msg );
            Console.ForegroundColor = fc;
        }

        #region [.Create XAttributes by NER-type.]
        private static IEnumerable< XAttribute > CreateXAttributes( NameWord w )
        {
            if ( !w.Firstname.IsNullOrEmpty() ) yield return (new XAttribute( "firstname", w.Firstname ));
            if ( !w.Surname  .IsNullOrEmpty() ) yield return (new XAttribute( "surname"  , w.Surname   ));
            if ( w.TextPreambleType != TextPreambleTypeEnum.__UNDEFINED__ ) yield return (new XAttribute( "nameType", w.TextPreambleType ));
            if ( !w.MaritalStatus.IsNullOrEmpty() ) yield return (new XAttribute( "maritalStatus", w.MaritalStatus ));
            
        }
        private static IEnumerable< XAttribute > CreateXAttributes( UrlOrEmailWordBase w )
        {
            yield return (new XAttribute( "urltype", w.UrlType ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( PhoneNumberWord w )
        {            
            if ( !w.CityAreaName.IsNullOrEmpty() ) yield return (new XAttribute( "cityAreaName"   , w.CityAreaName ));
            yield return (new XAttribute( "phoneNumberType", w.PhoneNumberType ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( CustomerNumberWord w )
        {
            if ( !w.CustomerNumber.IsNullOrEmpty() ) yield return (new XAttribute( "customerNumber", w.CustomerNumber ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( BirthdayWord w )
        {
            yield return (new XAttribute( "birthdayDateTime", w.BirthdayDateTime.ToString( "dd.MM.yyyy" ) ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( BirthplaceWord w )
        {
            if ( !w.Birthplace.IsNullOrEmpty() ) yield return (new XAttribute( "birthplace", w.Birthplace ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( MaritalStatusWord w )
        {
            if ( !w.MaritalStatus.IsNullOrEmpty() ) yield return (new XAttribute( "maritalStatus", w.MaritalStatus ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( NationalityWord w )
        {
            if ( !w.Nationality.IsNullOrEmpty() ) yield return (new XAttribute( "nationality", w.Nationality ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( CreditCardWord w )
        {
            if ( !w.CreditCardNumber.IsNullOrEmpty() ) yield return (new XAttribute( "creditCardNumber", w.CreditCardNumber ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( PassportIdCardNumberWord w )
        {
            if ( !w.PassportIdCardNumbers.IsNullOrEmpty() ) yield return (new XAttribute( "passportIdCardNumber", w.PassportIdCardNumbers ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( CarNumberWord w )
        {
            if ( !w.CarNumber.IsNullOrEmpty() ) yield return (new XAttribute( "carNumber", w.CarNumber ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( HealthInsuranceWord w )
        {
            if ( !w.HealthInsuranceNumber.IsNullOrEmpty() ) yield return (new XAttribute( "healthInsuranceNumber", w.HealthInsuranceNumber ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( DriverLicenseWord w )
        {
            if ( !w.DriverLicense.IsNullOrEmpty() ) yield return (new XAttribute( "driverLicense", w.DriverLicense ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( SocialSecurityWord w )
        {
            if ( !w.SocialSecurityNumber.IsNullOrEmpty() ) yield return (new XAttribute( "socialSecurity", w.SocialSecurityNumber ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( TaxIdentificationWord w )
        {
            if ( !w.TaxIdentificationNumber.IsNullOrEmpty() ) yield return (new XAttribute( "taxIdentification", w.TaxIdentificationNumber ));
            if ( w.TaxIdentificationType != TaxIdentificationTypeEnum.Default ) yield return (new XAttribute( "taxIdentificationType", w.TaxIdentificationType ));
        }
        private static IEnumerable<XAttribute> CreateXAttributes( CompanyWord w )
        {
            if ( !w.Name.IsNullOrEmpty() ) yield return (new XAttribute( "companyName", w.Name ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( AddressWord w )
        {
            if ( !w.Street       .IsNullOrEmpty() ) yield return (new XAttribute( "street"  , w.Street ));
            if ( !w.HouseNumber  .IsNullOrEmpty() ) yield return (new XAttribute( "houseNum", w.HouseNumber ));
            if ( !w.ZipCodeNumber.IsNullOrEmpty() ) yield return (new XAttribute( "indexNum", w.ZipCodeNumber ));
            if ( !w.City         .IsNullOrEmpty() ) yield return (new XAttribute( "city"    , w.City ));
        }
        private static IEnumerable< XAttribute > CreateXAttributes( BankAccountWord w )
        {
            yield return (new XAttribute( "bankAccountType", w.BankAccountType ));
            if ( !w.AccountNumber.IsNullOrEmpty() ) yield return (new XAttribute( "accountNumber", w.AccountNumber ));
            if ( !w.AccountOwner .IsNullOrEmpty() ) yield return (new XAttribute( "accountOwner" , w.AccountOwner ));
            if ( !w.BankCode     .IsNullOrEmpty() ) yield return (new XAttribute( "bankCode"     , w.BankCode ));
            if ( !w.BankName     .IsNullOrEmpty() ) yield return (new XAttribute( "bankName"     , w.BankName ));
            if ( w.BankAccountType == BankAccountTypeEnum.IBAN )
            {
                if ( !w.IBAN.IsNullOrEmpty() ) yield return (new XAttribute( "IBAN", w.IBAN ));
            }
        }
        #endregion
    }
}