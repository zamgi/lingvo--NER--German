using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Xunit;

using Lingvo.NER.Rules.Address;
using Lingvo.NER.Rules.BankAccounts;
using Lingvo.NER.Rules.Birthplaces;
using Lingvo.NER.Rules.CarNumbers;
using Lingvo.NER.Rules.Companies;
using Lingvo.NER.Rules.DriverLicenses;
using Lingvo.NER.Rules.MaritalStatuses;
using Lingvo.NER.Rules.Names;
using Lingvo.NER.Rules.Nationalities;
using Lingvo.NER.Rules.PassportIdCardNumbers;
using Lingvo.NER.Rules.PhoneNumbers;
using Lingvo.NER.Rules.SocialSecurities;
using Lingvo.NER.Rules.TaxIdentifications;
using Lingvo.NER.Rules.tokenizing;

using NT = Lingvo.NER.Rules.NerOutputType;

namespace Lingvo.NER.Rules.tests
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class NerTestsBase
    {
        /// <summary>
        /// 
        /// </summary>
        protected sealed class Config
        {
            private IConfigurationRoot _Configuration;
            private string             _RESOURCES_BASE_DIR;
            public Config( IConfigurationRoot configuration )
            {
                _Configuration = configuration ?? throw (new ArgumentNullException( nameof(configuration) ));
                _RESOURCES_BASE_DIR = _Configuration[ "RESOURCES_BASE_DIR" ]?.Trim();
            }
            private string GetFilePath( string relativeFilePathConfigKey )
            {
                var relativeFilePath = _Configuration[ relativeFilePathConfigKey ]?.Trim();
                return (_RESOURCES_BASE_DIR.IsNullOrEmpty() ? relativeFilePath : Path.Combine( _RESOURCES_BASE_DIR, relativeFilePath ));
            }

            //public string NER_MODEL_FILENAME                => GetFilePath( "NER_MODEL_FILENAME" );
            //public string NER_TEMPLATE_FILENAME             => GetFilePath( "NER_TEMPLATE_FILENAME" );
            public string URL_DETECTOR_RESOURCES_FILENAME   => GetFilePath( "URL_DETECTOR_RESOURCES_FILENAME" );
            public string SENT_SPLITTER_RESOURCES_FILENAME  => GetFilePath( "SENT_SPLITTER_RESOURCES_FILENAME" );
            public string PHONE_NUMBERS_FILENAME            => GetFilePath( "PHONE_NUMBERS_FILENAME" );
            public string ZIP_CODES_FILENAME                => GetFilePath( "ZIP_CODES_FILENAME" );
            public string CITIES_FILENAME                   => GetFilePath( "CITIES_FILENAME" );
            public string STREETS_FILENAME                  => GetFilePath( "STREETS_FILENAME" );
            public string BANK_NUMBERS_FILENAME             => GetFilePath( "BANK_NUMBERS_FILENAME" );
            public string BIRTHPLACES_FILENAME              => GetFilePath( "BIRTHPLACES_FILENAME" );
            public string BIRTHPLACE_PREAMBLES_FILENAME     => GetFilePath( "BIRTHPLACE_PREAMBLES_FILENAME" );
            public string FIRST_NAMES_FILENAME              => GetFilePath( "FIRST_NAMES_FILENAME" );
            public string SUR_NAMES_FILENAME                => GetFilePath( "SUR_NAMES_FILENAME" );
            public string EXCLUDED_NAMES_FILENAME           => GetFilePath( "EXCLUDED_NAMES_FILENAME" );
            public string CAR_NUMBERS_FILENAME              => GetFilePath( "CAR_NUMBERS_FILENAME" );
            public string PASSPORT_IDCARD_NUMBERS_FILENAME  => GetFilePath( "PASSPORT_IDCARD_NUMBERS_FILENAME" );
            public string MARITAL_STATUSES_FILENAME         => GetFilePath( "MARITAL_STATUSES_FILENAME" );
            public string MARITAL_STATUS_PREAMBLES_FILENAME => GetFilePath( "MARITAL_STATUS_PREAMBLES_FILENAME" );
            public string NATIONALITIES_FILENAME            => GetFilePath( "NATIONALITIES_FILENAME" );
            public string NATIONALITY_PREAMBLES_FILENAME    => GetFilePath( "NATIONALITY_PREAMBLES_FILENAME" );
            public string DRIVER_LICENSES_FILENAME          => GetFilePath( "DRIVER_LICENSES_FILENAME" );
            public string TAX_IDENTIFICATIONS_FILENAME      => GetFilePath( "TAX_IDENTIFICATIONS_FILENAME" );
            public string COMPANY_VOCAB_FILENAME            => GetFilePath( "COMPANY_VOCAB_FILENAME" );
            public string COMPANY_PREFIXES_FILENAME         => GetFilePath( "COMPANY_PREFIXES_FILENAME" );
            public string COMPANY_SUFFIXES_FILENAME         => GetFilePath( "COMPANY_SUFFIXES_FILENAME" );
            public string COMPANY_PREFIXES_PREV_SUFFIXES_FILENAME => GetFilePath( "COMPANY_PREFIXES_PREV_SUFFIXES_FILENAME" );
            public string COMPANY_EXPAND_PREAMBLES_FILENAME       => GetFilePath( "COMPANY_EXPAND_PREAMBLES_FILENAME" );

            public PhoneNumbersModel CreatePhoneNumbersModel() => new PhoneNumbersModel( PHONE_NUMBERS_FILENAME, capacity: 5500 );
            public AddressModel CreateAddressModel()
                => new AddressModel( new AddressModel.InputParams()
                {
                    ZipCodes = (ZIP_CODES_FILENAME, Capacity: 8308),
                    Cities   = (CITIES_FILENAME   , CapacityOneWord: 11741 , CapacityMultiWord: 480),
                    Streets  = (STREETS_FILENAME  , CapacityOneWord: 206064, CapacityMultiWord: 693187),
                });
            public BankAccountsModel CreateBankAccountsModel() => new BankAccountsModel( BANK_NUMBERS_FILENAME, capacity: 4000 );
            public BirthplacesModel CreateBirthplacesModel()
                => new BirthplacesModel(new BirthplacesModel.InputParams()
                {
                    Birthplaces         = (BIRTHPLACES_FILENAME         , Capacity: 2000),
                    BirthplacePreambles = (BIRTHPLACE_PREAMBLES_FILENAME, Capacity: 100),
                });
            public MaritalStatusesModel CreateMaritalStatusesModel()
                => new MaritalStatusesModel( new MaritalStatusesModel.InputParams()
                {
                    MaritalStatuses        = (MARITAL_STATUSES_FILENAME        , Capacity: 150),
                    MaritalStatusPreambles = (MARITAL_STATUS_PREAMBLES_FILENAME, Capacity: 50),
                });
            public NamesModel CreateNamesModel()
                => new NamesModel( new NamesModel.InputParams()
                {
                    FirstNames            = (FIRST_NAMES_FILENAME, Capacity: 38914),
                    SurNames              = (SUR_NAMES_FILENAME  , Capacity: 132631),
                    ExcludedNamesFilename = EXCLUDED_NAMES_FILENAME,
                });
            public NationalitiesModel CreateNationalitiesModel()
                => new NationalitiesModel(new NationalitiesModel.InputParams()
                {
                    Nationalities        = (NATIONALITIES_FILENAME        , Capacity: 2000),
                    NationalityPreambles = (NATIONALITY_PREAMBLES_FILENAME, Capacity: 100),
                });
            public CarNumbersModel CreateCarNumbersModel() => new CarNumbersModel( CAR_NUMBERS_FILENAME, capacity: 800 );
            public PassportIdCardNumbersModel CreatePassportIdCardNumbersModel() => new PassportIdCardNumbersModel( PASSPORT_IDCARD_NUMBERS_FILENAME, capacity: 2000 );
            public DriverLicensesModel CreateDriverLicensesModel() => new DriverLicensesModel( DRIVER_LICENSES_FILENAME, capacity: 700 );
            public TaxIdentificationsModel CreateTaxIdentificationsModel() => new TaxIdentificationsModel( TAX_IDENTIFICATIONS_FILENAME, capacity: 350 );
            public CompaniesModel CreateCompaniesModel()
                => new CompaniesModel( new CompaniesModel.InputParams()
                {
                    CompanyVocab         = (COMPANY_VOCAB_FILENAME                 , Capacity: 57000),
                    Prefixes             = (COMPANY_PREFIXES_FILENAME              , Capacity: 10),
                    Suffixes             = (COMPANY_SUFFIXES_FILENAME              , Capacity: 100),
                    PrefixesPrevSuffixes = (COMPANY_PREFIXES_PREV_SUFFIXES_FILENAME, Capacity: 25),
                    ExpandPreambles      = (COMPANY_EXPAND_PREAMBLES_FILENAME      , Capacity: 25),
                });

            public NerProcessorConfig CreateNerProcessorConfig()
                => new NerProcessorConfig( SENT_SPLITTER_RESOURCES_FILENAME, URL_DETECTOR_RESOURCES_FILENAME )
                {
                    //ModelFilename              = NER_MODEL_FILENAME,
                    //TemplateFile               = CRFTemplateFile.Load( NER_TEMPLATE_FILENAME, NerScriber.GetAllowedCrfTemplateFileColumnNames() ),
                    SocialSecuritiesModel      = new SocialSecuritiesModel(),
                    PhoneNumbersModel          = CreatePhoneNumbersModel(),
                    AddressModel               = CreateAddressModel(),
                    BankAccountsModel          = CreateBankAccountsModel(),
                    BirthplacesModel           = CreateBirthplacesModel(),
                    MaritalStatusesModel       = CreateMaritalStatusesModel(),
                    NamesModel                 = CreateNamesModel(),
                    NationalitiesModel         = CreateNationalitiesModel(),
                    CarNumbersModel            = CreateCarNumbersModel(),
                    PassportIdCardNumbersModel = CreatePassportIdCardNumbersModel(),
                    DriverLicensesModel        = CreateDriverLicensesModel(),
                    TaxIdentificationsModel    = CreateTaxIdentificationsModel(),
                    CompaniesModel             = CreateCompaniesModel(),
                };
        }

        #region [.cctor().]
        protected static Config             _Config { get; private set; }
        private   static NerProcessorConfig _NPConfig;
        static NerTestsBase()
        {
            const string SETTINGS_JSON_FILE_NAME = "settings.json";

            IConfigurationRoot configuration;
            try
            {
                configuration = new ConfigurationBuilder().AddJsonFile( SETTINGS_JSON_FILE_NAME ).Build();
            }
            catch ( FileNotFoundException )
            {
                Debug.WriteLine( $"'{SETTINGS_JSON_FILE_NAME}' not found in project folder." );
                return;
            }

            _Config   = new Config( configuration );
            _NPConfig = _Config.CreateNerProcessorConfig();
        }
        protected static NerProcessor CreateNerProcessor( NerProcessor.UsedRecognizerTypeEnum urt = NerProcessor.UsedRecognizerTypeEnum.All_Without_Crf ) => new NerProcessor( _NPConfig, urt );
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static bool IsNullOrEmpty( this string s ) => string.IsNullOrEmpty( s );
        public static bool IsNullOrWhiteSpace( this string s ) => string.IsNullOrWhiteSpace( s );
        public static string NoWhitespace( this string s ) => s.Replace( " ", string.Empty );

        public static void AT( this NerProcessor np, string text, (NT nerOutputType, string valueOriginal) p ) => np.AT( text, new[] { p } );
        public static void AT( this NerProcessor np, string text, IList< (NT nerOutputType, string valueOriginal) > pairs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( pairs );

        public static void Check( this IList< word_t > words, IList< (NT nerOutputType, string valueOriginal) > refs )
        {
            var pairs_2 = (from w in words select (w.nerOutputType, w.valueOriginal)).ToArray( words.Count );

            var startIndex = 0;
            foreach ( var p in refs )
            {
                startIndex = pairs_2.IndexOf( p, startIndex );
                if ( startIndex == -1 )
                {
                    Assert.True( false );
                }
                startIndex++;
            }
            Assert.True( 0 < startIndex );
        }

        private static int IndexOf( this IList< (NT nerOutputType, string valueOriginal) > pairs, in (NT nerOutputType, string valueOriginal) p, int startIndex )
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
        private static T[] ToArray< T >( this IEnumerable< T > seq, int count )
        {
            var array = new T[ count ];
            count = 0;
            foreach ( var t in seq )
            {
                array[ count++ ] = t;
            }
            return (array);
        }

        private static bool IsEqual( in (NT nerOutputType, string valueOriginal) x, in (NT nerOutputType, string valueOriginal) y ) => (x.nerOutputType == y.nerOutputType) && (x.valueOriginal == y.valueOriginal);
    }
}
