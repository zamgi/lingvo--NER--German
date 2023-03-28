using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

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
#if (!WITHOUT_CRF)
using Lingvo.NER.Rules.crfsuite;
#endif

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

using _Compression_Stream_ = System.IO.Compression.DeflateStream;
//using _Compression_Stream_ = System.IO.Compression.GZipStream;
//using _Compression_Stream_ = System.IO.Compression.BrotliStream;

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    public interface IConfig
    {
        void ReloadParamsFromConfigFile();
        IConfig GetImpl();

        NerProcessorConfig CreateNerProcessorConfig();
        Task< NerProcessorConfig > CreateNerProcessorConfig_Async();
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class Config : IConfig
    {
        private static volatile IConfig _SelfInst;
        public static IConfig Inst
        {
            get
            {
                if ( _SelfInst == null )
                {
                    Interlocked.Exchange( ref _SelfInst, new Config() );
                    #region comm. prev.
                    //lock ( typeof(IConfig) )
                    //{
                    //    if ( _SelfInst == null )
                    //    {
                    //        _SelfInst = new Config();
                    //    }
                    //} 
                    #endregion
                }
                return (_SelfInst);
            }
        }


        private ReaderWriterLockSlim _RWLS;
        private IConfig __impl__;
        private IConfig _Impl
        {
            get
            {
                _RWLS.EnterReadLock();
                var o = __impl__;
                _RWLS.ExitReadLock();
                return (o);
            }
            set
            {
                Debug.Assert( value != null );

                _RWLS.EnterWriteLock();
                __impl__ = value;
                _RWLS.ExitWriteLock();
            }
        }
        private Config()
        {
            _RWLS = new ReaderWriterLockSlim( LockRecursionPolicy.SupportsRecursion );
            _Impl = Create();
        }
        private static IConfig Create()
        {
            var resourceFileName         = ConfigurationManager.AppSettings[ "RESOURCE_FILENAME" ];
            var resourceFileName_isEmpty = resourceFileName.IsNullOrWhiteSpace();
            var USE_RESOURCE_FILENAME    = (!bool.TryParse( ConfigurationManager.AppSettings[ "NOT_USE_RESOURCE_FILENAME" ], out var b ) || !b) && !resourceFileName_isEmpty;

            var USE_RESOURCE_ASSEMBLY_CONFIG = (!bool.TryParse( ConfigurationManager.AppSettings[ "NOT_USE_RESOURCE_ASSEMBLY" ], out b ) || !b);
            if ( USE_RESOURCE_ASSEMBLY_CONFIG )
            {
                var resourceAssemblyPath = ConfigurationManager.AppSettings[ "RESOURCE_ASSEMBLY_FOLDER" ];
                if ( !resourceAssemblyPath.IsNullOrWhiteSpace() )
                {
                    resourceAssemblyPath = Path.Combine( Path.GetFullPath( resourceAssemblyPath ), ResourceAssemblyConfig.RESOURCE_ASSEMBLY_PATH );
                    return (new ResourceAssemblyConfig( resourceAssemblyPath ));
                }
                else if ( !USE_RESOURCE_FILENAME )
                {
                    resourceAssemblyPath = ResourceAssemblyConfig.RESOURCE_ASSEMBLY_PATH;
                    if ( File.Exists( resourceAssemblyPath ) )
                    {
                        return (new ResourceAssemblyConfig( resourceAssemblyPath ));
                    }
                    resourceAssemblyPath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ), resourceAssemblyPath );
                    if ( File.Exists( resourceAssemblyPath ) )
                    {
                        return (new ResourceAssemblyConfig( resourceAssemblyPath ));
                    }
                }
            }

            if ( USE_RESOURCE_FILENAME )
            {
                if ( File.Exists( resourceFileName ) )
                {
                    return (new ResourceFileConfig( resourceFileName ));
                }
                resourceFileName = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ), resourceFileName );
                if ( File.Exists( resourceFileName ) )
                {
                    return (new ResourceFileConfig( resourceFileName ));
                }
            }

            return (StreamReaderConfig.Inst); //return (TextFilesConfig.Inst);
        }

        public NerProcessorConfig CreateNerProcessorConfig() => _Impl.CreateNerProcessorConfig();
        public Task< NerProcessorConfig > CreateNerProcessorConfig_Async() => _Impl.CreateNerProcessorConfig_Async();
        public void ReloadParamsFromConfigFile()
        {
            ConfigurationManager.RefreshSection( "appSettings" );
            ConfigurationManager.RefreshSection( ConfigSectionHandler.SECTION_NAME );

            var config = Create();
            if ( config.GetType() != _Impl.GetType() )
            {
                _Impl = config;
            }
            _Impl.ReloadParamsFromConfigFile();
        }
        public IConfig GetImpl() => _Impl;

#if (!WITHOUT_CRF)
        public static bool WITHOUT_CRF => false;
#else
        public static bool WITHOUT_CRF => true;
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class ConfigSectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class ConfigurationErrorsException_SectionIsNotFound : ConfigurationErrorsException
        {
            public ConfigurationErrorsException_SectionIsNotFound( string message ) : base( message ) { }
        }

        /*
        <configSections>
          <section name="Lingvo.NER.Rules.Config" type="Lingvo.NER.Rules.ConfigSectionHandler, Lingvo.NER.Rules.config /ASSEMBLY_NAME_WITHOUT_EXT/" requirePermission="false"/>
        </configSections>         
        */
        public const string SECTION_NAME = "Lingvo.NER.Rules.Config";

        object IConfigurationSectionHandler.Create( object parent, object configContext, XmlNode section )
        {
            if ( section == null ) throw (new ConfigurationErrorsException_SectionIsNotFound( @$"Configuration error: '<configuration>\<{SECTION_NAME}>' section is not found." ));

            return (ToXDocument( section ));
        }
        public static XDocument GetSection() => (XDocument) ConfigurationManager.GetSection( SECTION_NAME );

        private static XDocument ToXDocument( XmlNode node )
        {
            using ( var sr  = new StringReader( node.OuterXml ) )
            using ( var xtr = new XmlTextReader( sr ) { Namespaces = false } )
            {
                return (XDocument.Load( xtr ));
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class DefCap
    {
        public const int bankCodes_capacity                   = 4000;
        public const int birthplacePreambles_capacity         = 100;
        public const int birthplaces_capacity                 = 2000;
        public const int carNumbers_capacity                  = 800;
        public const int cities_capacityOneWord               = 11741;
        public const int cities_capacityMultiWord             = 480;
        public const int driverLicenses_capacity              = 700;
        public const int firstnames_capacity                  = 38914;
        public const int maritalStatuses_capacity             = 150;
        public const int maritalStatusPreambles_capacity      = 50;
        public const int nationalities_capacity               = 2000;
        public const int nationalityPreambles_capacity        = 100;
        public const int old_PassportIdCardNumbers_capacity   = 2000;
        public const int old_TaxIdentifications_capacity      = 350;
        public const int phoneNumbers_capacity                = 5500;
        public const int streets_capacityOneWord              = 206064;
        public const int streets_capacityMultiWord            = 693187;
        public const int surnames_capacity                    = 132631;
        public const int zipcodes_capacity                    = 8308;        
        public const int companyVocab_capacity                = 57000;
        public const int companyPrefixes_capacity             = 10;
        public const int companySuffixes_capacity             = 50;
        public const int companyPrefixesPrevSuffixes_capacity = 25;
        public const int companyExpandPreambles_capacity      = 25; 
    }

    #region [.Config-types.]
    /// <summary>
    /// 
    /// </summary>
    public class TextFilesConfig : IConfig
    {
        /// <summary>
        /// 
        /// </summary>
        protected struct NerResourcesConfig
        {
#if (!WITHOUT_CRF)
            public string NER_MODEL;
            public string NER_TEMPLATE;
#endif
            public string URL_DETECTOR_RESOURCES;
            public string SENT_SPLITTER_RESOURCES;
            public string EXCLUDED_NAMES;
            public (string FILENAME, int? CapacityOneWord, int? CapacityMultiWord) CITIES;
            public (string FILENAME, int? CapacityOneWord, int? CapacityMultiWord) STREETS;
            public (string FILENAME, int? Capacity) ZIP_CODES;
            public (string FILENAME, int? Capacity) PHONE_NUMBERS;
            public (string FILENAME, int? Capacity) BANK_NUMBERS;
            public (string FILENAME, int? Capacity) FIRST_NAMES;
            public (string FILENAME, int? Capacity) SUR_NAMES;
            public (string FILENAME, int? Capacity) CAR_NUMBERS;
            public (string FILENAME, int? Capacity) PASSPORT_IDCARD_NUMBERS;
            public (string FILENAME, int? Capacity) DRIVER_LICENSES;
            public (string FILENAME, int? Capacity) TAX_IDENTIFICATIONS;
            public (string FILENAME, int? Capacity) BIRTHPLACES;
            public (string FILENAME, int? Capacity) BIRTHPLACE_PREAMBLES;
            public (string FILENAME, int? Capacity) MARITAL_STATUSES;
            public (string FILENAME, int? Capacity) MARITAL_STATUS_PREAMBLES;
            public (string FILENAME, int? Capacity) NATIONALITIES;
            public (string FILENAME, int? Capacity) NATIONALITY_PREAMBLES;
            public (string FILENAME, int? Capacity) COMPANY_VOCAB;
            public (string FILENAME, int? Capacity) COMPANY_PREFIXES;
            public (string FILENAME, int? Capacity) COMPANY_SUFFIXES;
            public (string FILENAME, int? Capacity) COMPANY_PREFIXES_PREV_SUFFIXES;
            public (string FILENAME, int? Capacity) COMPANY_EXPAND_PREAMBLES; 
        }

        public static TextFilesConfig Inst { get; } = new TextFilesConfig();
        protected TextFilesConfig() => ReadConfigSection();
        private void ReadConfigSection()
        {           
            static int? ToInt32( string s ) => int.TryParse( s, out var i ) ? i : (int?) null;
            static string GetElemAttrValue( XElement xroot, string baseDir, string elemName )
            {
                var fn = xroot.Element( elemName )?.Attribute( "value" )?.Value;
                if ( !fn.IsNullOrWhiteSpace() )
                {
                    return (Path.GetFullPath( Path.Combine( baseDir, fn ) ));
                }
                return (default);
            };
            static (string FILENAME, int? capacity) GetElemAttrValue_T2( XElement xroot, string baseDir, string elemName )
            {
                var xe = xroot.Element( elemName );
                var fn = xe?.Attribute( "value" )?.Value;
                if ( !fn.IsNullOrWhiteSpace() )
                {
                    return (Path.GetFullPath( Path.Combine( baseDir, fn ) ), ToInt32( xe.Attribute( "capacity" )?.Value ));
                }
                return (default);
            };
            static (string FILENAME, int? capacityOneWord, int? capacityMultiWord) GetElemAttrValue_T3( XElement xroot, string baseDir, string elemName )
            {
                var xe = xroot.Element( elemName );
                var fn = xe?.Attribute( "value" )?.Value;
                if ( !fn.IsNullOrWhiteSpace() )
                {
                    return (Path.GetFullPath( Path.Combine( baseDir, fn ) ), ToInt32( xe.Attribute( "capacityOneWord" )?.Value ), ToInt32( xe.Attribute( "capacityMultiWord" )?.Value ));
                }
                return (default);                
            };
            static void ValOrDef( ref int? i, int defval ) => i = i.GetValueOrDefault( defval );
            //------------------------------------------------//

            var xdoc = ConfigSectionHandler.GetSection();
            var xroot = xdoc?.Root?.Element( "RESOURCES" ) ?? throw (new ConfigurationErrorsException( @$"Missing element in config file: '<configuration>\<{ConfigSectionHandler.SECTION_NAME}>\<RESOURCES>'." ));
            var baseDir = xroot.Attribute( "baseDir" )?.Value ?? string.Empty;
            
            _Nrc = new NerResourcesConfig()
            {
#if (!WITHOUT_CRF)
                NER_MODEL                = GetElemAttrValue( xroot, baseDir, "NER_MODEL" ),
                NER_TEMPLATE             = GetElemAttrValue( xroot, baseDir, "NER_TEMPLATE" ),
#endif
                URL_DETECTOR_RESOURCES   = GetElemAttrValue( xroot, baseDir, "URL_DETECTOR_RESOURCES" ),
                SENT_SPLITTER_RESOURCES  = GetElemAttrValue( xroot, baseDir, "SENT_SPLITTER_RESOURCES" ),
                EXCLUDED_NAMES           = GetElemAttrValue( xroot, baseDir, "EXCLUDED_NAMES" ),
                CITIES                   = GetElemAttrValue_T3( xroot, baseDir, "CITIES"  ),
                STREETS                  = GetElemAttrValue_T3( xroot, baseDir, "STREETS" ),
                ZIP_CODES                = GetElemAttrValue_T2( xroot, baseDir, "ZIP_CODES" ),
                PHONE_NUMBERS            = GetElemAttrValue_T2( xroot, baseDir, "PHONE_NUMBERS" ),
                BANK_NUMBERS             = GetElemAttrValue_T2( xroot, baseDir, "BANK_NUMBERS" ),
                FIRST_NAMES              = GetElemAttrValue_T2( xroot, baseDir, "FIRST_NAMES" ),
                SUR_NAMES                = GetElemAttrValue_T2( xroot, baseDir, "SUR_NAMES" ),
                CAR_NUMBERS              = GetElemAttrValue_T2( xroot, baseDir, "CAR_NUMBERS" ),
                PASSPORT_IDCARD_NUMBERS  = GetElemAttrValue_T2( xroot, baseDir, "PASSPORT_IDCARD_NUMBERS" ),
                DRIVER_LICENSES          = GetElemAttrValue_T2( xroot, baseDir, "DRIVER_LICENSES" ),
                TAX_IDENTIFICATIONS      = GetElemAttrValue_T2( xroot, baseDir, "TAX_IDENTIFICATIONS" ),
                BIRTHPLACES              = GetElemAttrValue_T2( xroot, baseDir, "BIRTHPLACES" ),
                BIRTHPLACE_PREAMBLES     = GetElemAttrValue_T2( xroot, baseDir, "BIRTHPLACE_PREAMBLES" ),
                MARITAL_STATUSES         = GetElemAttrValue_T2( xroot, baseDir, "MARITAL_STATUSES" ),
                MARITAL_STATUS_PREAMBLES = GetElemAttrValue_T2( xroot, baseDir, "MARITAL_STATUS_PREAMBLES" ),
                NATIONALITIES            = GetElemAttrValue_T2( xroot, baseDir, "NATIONALITIES" ),
                NATIONALITY_PREAMBLES    = GetElemAttrValue_T2( xroot, baseDir, "NATIONALITY_PREAMBLES" ),
                COMPANY_VOCAB            = GetElemAttrValue_T2( xroot, baseDir, "COMPANY_VOCAB" ),
                COMPANY_PREFIXES         = GetElemAttrValue_T2( xroot, baseDir, "COMPANY_PREFIXES" ),
                COMPANY_SUFFIXES         = GetElemAttrValue_T2( xroot, baseDir, "COMPANY_SUFFIXES" ),
                COMPANY_PREFIXES_PREV_SUFFIXES = GetElemAttrValue_T2( xroot, baseDir, "COMPANY_PREFIXES_PREV_SUFFIXES" ),
                COMPANY_EXPAND_PREAMBLES       = GetElemAttrValue_T2( xroot, baseDir, "COMPANY_EXPAND_PREAMBLES" ),
            };

            ValOrDef( ref _Nrc.CITIES.CapacityOneWord           , DefCap.cities_capacityOneWord    );
            ValOrDef( ref _Nrc.CITIES.CapacityMultiWord         , DefCap.cities_capacityMultiWord  );
            ValOrDef( ref _Nrc.STREETS.CapacityOneWord          , DefCap.streets_capacityOneWord   );
            ValOrDef( ref _Nrc.STREETS.CapacityMultiWord        , DefCap.streets_capacityMultiWord );
            ValOrDef( ref _Nrc.ZIP_CODES.Capacity               , DefCap.zipcodes_capacity         );
            ValOrDef( ref _Nrc.PHONE_NUMBERS.Capacity           , DefCap.phoneNumbers_capacity     );
            ValOrDef( ref _Nrc.BANK_NUMBERS.Capacity            , DefCap.bankCodes_capacity        );
            ValOrDef( ref _Nrc.FIRST_NAMES.Capacity             , DefCap.firstnames_capacity       );
            ValOrDef( ref _Nrc.SUR_NAMES.Capacity               , DefCap.surnames_capacity         );
            ValOrDef( ref _Nrc.CAR_NUMBERS.Capacity             , DefCap.carNumbers_capacity       );
            ValOrDef( ref _Nrc.PASSPORT_IDCARD_NUMBERS.Capacity , DefCap.old_PassportIdCardNumbers_capacity );
            ValOrDef( ref _Nrc.DRIVER_LICENSES.Capacity         , DefCap.driverLicenses_capacity            );
            ValOrDef( ref _Nrc.TAX_IDENTIFICATIONS.Capacity     , DefCap.old_TaxIdentifications_capacity    );
            ValOrDef( ref _Nrc.BIRTHPLACES.Capacity             , DefCap.birthplaces_capacity               );
            ValOrDef( ref _Nrc.BIRTHPLACE_PREAMBLES.Capacity    , DefCap.birthplacePreambles_capacity       );
            ValOrDef( ref _Nrc.MARITAL_STATUSES.Capacity        , DefCap.maritalStatuses_capacity           );
            ValOrDef( ref _Nrc.MARITAL_STATUS_PREAMBLES.Capacity, DefCap.maritalStatusPreambles_capacity    );
            ValOrDef( ref _Nrc.NATIONALITIES.Capacity           , DefCap.nationalities_capacity             );
            ValOrDef( ref _Nrc.NATIONALITY_PREAMBLES.Capacity   , DefCap.nationalityPreambles_capacity      );            
            ValOrDef( ref _Nrc.COMPANY_VOCAB.Capacity           , DefCap.companyVocab_capacity              );
            ValOrDef( ref _Nrc.COMPANY_PREFIXES.Capacity        , DefCap.companyPrefixes_capacity           );
            ValOrDef( ref _Nrc.COMPANY_SUFFIXES.Capacity        , DefCap.companySuffixes_capacity           );
            ValOrDef( ref _Nrc.COMPANY_PREFIXES_PREV_SUFFIXES.Capacity, DefCap.companyPrefixesPrevSuffixes_capacity );
            ValOrDef( ref _Nrc.COMPANY_EXPAND_PREAMBLES      .Capacity, DefCap.companyExpandPreambles_capacity      ); 
            //------------------------------------------------//
        }

        protected NerResourcesConfig _Nrc;

        public PhoneNumbersModel CreatePhoneNumbersModel() => new PhoneNumbersModel( _Nrc.PHONE_NUMBERS );
        public AddressModel CreateAddressModel()
            => new AddressModel( new AddressModel.InputParams()
            {
                ZipCodes = _Nrc.ZIP_CODES,
                Cities   = _Nrc.CITIES,
                Streets  = _Nrc.STREETS,
            });
        public BankAccountsModel CreateBankAccountsModel() => new BankAccountsModel( _Nrc.BANK_NUMBERS );
        public NamesModel CreateNamesModel() 
            => new NamesModel( new NamesModel.InputParams()
            {
                FirstNames            = _Nrc.FIRST_NAMES,
                SurNames              = _Nrc.SUR_NAMES,
                ExcludedNamesFilename = _Nrc.EXCLUDED_NAMES,
            });
        public CarNumbersModel CreateCarNumbersModel() => new CarNumbersModel( _Nrc.CAR_NUMBERS );
        public PassportIdCardNumbersModel CreatePassportIdCardNumbersModel() => new PassportIdCardNumbersModel( _Nrc.PASSPORT_IDCARD_NUMBERS  );
        public DriverLicensesModel CreateDriverLicensesModel() => new DriverLicensesModel( _Nrc.DRIVER_LICENSES );
        public TaxIdentificationsModel CreateTaxIdentificationsModel() => new TaxIdentificationsModel( _Nrc.TAX_IDENTIFICATIONS );
        public BirthplacesModel CreateBirthplacesModel()
            => new BirthplacesModel(new BirthplacesModel.InputParams()
            {
                Birthplaces         = _Nrc.BIRTHPLACES,
                BirthplacePreambles = _Nrc.BIRTHPLACE_PREAMBLES,
            });
        public MaritalStatusesModel CreateMaritalStatusesModel()
            => new MaritalStatusesModel(new MaritalStatusesModel.InputParams()
            {
                MaritalStatuses        = _Nrc.MARITAL_STATUSES,
                MaritalStatusPreambles = _Nrc.MARITAL_STATUS_PREAMBLES,
            });
        public NationalitiesModel CreateNationalitiesModel()
            => new NationalitiesModel(new NationalitiesModel.InputParams()
            {
                Nationalities        = _Nrc.NATIONALITIES,
                NationalityPreambles = _Nrc.NATIONALITY_PREAMBLES,
            });
        public CompaniesModel CreateCompaniesModel()
            => new CompaniesModel( new CompaniesModel.InputParams()
            {
                CompanyVocab         = _Nrc.COMPANY_VOCAB,
                Prefixes             = _Nrc.COMPANY_PREFIXES,
                Suffixes             = _Nrc.COMPANY_SUFFIXES,
                PrefixesPrevSuffixes = _Nrc.COMPANY_PREFIXES_PREV_SUFFIXES,
                ExpandPreambles      = _Nrc.COMPANY_EXPAND_PREAMBLES,
            });

        public virtual NerProcessorConfig CreateNerProcessorConfig()
        {
            var config = new NerProcessorConfig( _Nrc.SENT_SPLITTER_RESOURCES, _Nrc.URL_DETECTOR_RESOURCES )
            {
#if (!WITHOUT_CRF)
                ModelFilename = _Nrc.NER_MODEL,
                TemplateFile  = CRFTemplateFile.Load( _Nrc.NER_TEMPLATE, NerScriber.GetAllowedCrfTemplateFileColumnNames() ),
#endif
                SocialSecuritiesModel = new SocialSecuritiesModel(),
                PhoneNumbersModel          = CreatePhoneNumbersModel(),
                AddressModel               = CreateAddressModel(),
                BankAccountsModel          = CreateBankAccountsModel(),
                BirthplacesModel           = CreateBirthplacesModel(),
                MaritalStatusesModel       = CreateMaritalStatusesModel(),
                NamesModel                 = CreateNamesModel(),
                CarNumbersModel            = CreateCarNumbersModel(),
                NationalitiesModel         = CreateNationalitiesModel(),
                PassportIdCardNumbersModel = CreatePassportIdCardNumbersModel(),
                DriverLicensesModel        = CreateDriverLicensesModel(),
                TaxIdentificationsModel    = CreateTaxIdentificationsModel(),
                CompaniesModel             = CreateCompaniesModel(),
            };
            return (config);
        }
        public virtual async Task< NerProcessorConfig > CreateNerProcessorConfig_Async()
        {
            var phoneNumbersModel_task          = Task.Run( () => CreatePhoneNumbersModel() );
            var addressModel_task               = Task.Run( () => CreateAddressModel() );
            var bankAccountsModel_task          = Task.Run( () => CreateBankAccountsModel() );
            var birthplacesModel_task           = Task.Run( () => CreateBirthplacesModel());
            var maritalStatusesModel_task       = Task.Run( () => CreateMaritalStatusesModel() );
            var namesModel_task                 = Task.Run( () => CreateNamesModel() );
            var nationalitiesModel_task         = Task.Run( () => CreateNationalitiesModel());
            var carNumbersModel_task            = Task.Run( () => CreateCarNumbersModel() );
            var passportIdCardNumbersModel_task = Task.Run( () => CreatePassportIdCardNumbersModel() );
            var driverLicensesModel_task        = Task.Run( () => CreateDriverLicensesModel() );
            var taxIdentificationsModel_task    = Task.Run( () => CreateTaxIdentificationsModel() );
            var companiesModel_task             = Task.Run( () => CreateCompaniesModel() );

            var config_task = Task.Run( () => new NerProcessorConfig( _Nrc.SENT_SPLITTER_RESOURCES, _Nrc.URL_DETECTOR_RESOURCES )
            {
#if (!WITHOUT_CRF)
                ModelFilename = _Nrc.NER_MODEL,
                TemplateFile  = CRFTemplateFile.Load( _Nrc.NER_TEMPLATE, NerScriber.GetAllowedCrfTemplateFileColumnNames() ),
#endif
                SocialSecuritiesModel = new SocialSecuritiesModel(),
            });

            await Task.WhenAll( config_task, phoneNumbersModel_task, addressModel_task, bankAccountsModel_task, namesModel_task, nationalitiesModel_task, 
                                maritalStatusesModel_task, carNumbersModel_task, passportIdCardNumbersModel_task, driverLicensesModel_task, 
                                taxIdentificationsModel_task, companiesModel_task ).CAX();

            var config = config_task.Result;
                config.PhoneNumbersModel          = phoneNumbersModel_task         .Result;
                config.AddressModel               = addressModel_task              .Result;
                config.BankAccountsModel          = bankAccountsModel_task         .Result;
                config.BirthplacesModel           = birthplacesModel_task          .Result;
                config.MaritalStatusesModel       = maritalStatusesModel_task      .Result;
                config.NamesModel                 = namesModel_task                .Result;
                config.CarNumbersModel            = carNumbersModel_task           .Result;
                config.NationalitiesModel         = nationalitiesModel_task        .Result;
                config.PassportIdCardNumbersModel = passportIdCardNumbersModel_task.Result;
                config.DriverLicensesModel        = driverLicensesModel_task       .Result;
                config.TaxIdentificationsModel    = taxIdentificationsModel_task   .Result;
                config.CompaniesModel             = companiesModel_task            .Result;

            return (config);
        }
        public void ReloadParamsFromConfigFile() => ReadConfigSection();
        public IConfig GetImpl() => this;
    }

    /// <summary>
    /// 
    /// </summary>
    public class StreamReaderConfig : TextFilesConfig
    {
        public new static StreamReaderConfig Inst { get; } = new StreamReaderConfig();
        protected StreamReaderConfig() { }

        private PhoneNumbersModel CreatePhoneNumbersModel( StreamReader_Disposer srd ) => new PhoneNumbersModel( srd.Open( _Nrc.PHONE_NUMBERS.FILENAME ), _Nrc.PHONE_NUMBERS.Capacity );
        private AddressModel CreateAddressModel( StreamReader_Disposer srd )
            => new AddressModel( new AddressModel.InputParams_2()
            {
                ZipCodes = (srd.Open( _Nrc.ZIP_CODES.FILENAME ), _Nrc.ZIP_CODES.Capacity),
                Cities   = (srd.Open( _Nrc.CITIES.FILENAME    ), _Nrc.CITIES.CapacityOneWord , _Nrc.CITIES.CapacityMultiWord),
                Streets  = (srd.Open( _Nrc.STREETS.FILENAME   ), _Nrc.STREETS.CapacityOneWord, _Nrc.STREETS.CapacityMultiWord),
            });
        private BankAccountsModel CreateBankAccountsModel( StreamReader_Disposer srd ) => new BankAccountsModel( srd.Open( _Nrc.BANK_NUMBERS.FILENAME ), _Nrc.BANK_NUMBERS.Capacity );
        private BirthplacesModel CreateBirthplacesModel( StreamReader_Disposer srd )
            => new BirthplacesModel( new BirthplacesModel.InputParams_2()
            {
                Birthplaces         = (srd.Open( _Nrc.BIRTHPLACES.FILENAME          ), _Nrc.BIRTHPLACES.Capacity),
                BirthplacePreambles = (srd.Open( _Nrc.BIRTHPLACE_PREAMBLES.FILENAME ), _Nrc.BIRTHPLACE_PREAMBLES.Capacity),
            });
        private MaritalStatusesModel CreateMaritalStatusesModel( StreamReader_Disposer srd )
            => new MaritalStatusesModel( new MaritalStatusesModel.InputParams_2()
            {
                MaritalStatuses        = (srd.Open( _Nrc.MARITAL_STATUSES.FILENAME         ), _Nrc.MARITAL_STATUSES.Capacity),
                MaritalStatusPreambles = (srd.Open( _Nrc.MARITAL_STATUS_PREAMBLES.FILENAME ), _Nrc.MARITAL_STATUS_PREAMBLES.Capacity),
            });
        private NamesModel CreateNamesModel( StreamReader_Disposer srd ) 
            => new NamesModel( new NamesModel.InputParams_2()
            {
                FirstNames                = (srd.Open( _Nrc.FIRST_NAMES.FILENAME ), _Nrc.FIRST_NAMES.Capacity),
                SurNames                  = (srd.Open( _Nrc.SUR_NAMES.FILENAME   ), _Nrc.SUR_NAMES.Capacity),
                ExcludedNamesStreamReader = srd.Open( _Nrc.EXCLUDED_NAMES ),
            });
        private NationalitiesModel CreateNationalitiesModel( StreamReader_Disposer srd )
            => new NationalitiesModel( new NationalitiesModel.InputParams_2()
            {
                Nationalities        = (srd.Open( _Nrc.NATIONALITIES.FILENAME         ), _Nrc.NATIONALITIES.Capacity),
                NationalityPreambles = (srd.Open( _Nrc.NATIONALITY_PREAMBLES.FILENAME ), _Nrc.NATIONALITY_PREAMBLES.Capacity),
            });
        private CarNumbersModel CreateCarNumbersModel( StreamReader_Disposer srd ) => new CarNumbersModel( srd.Open( _Nrc.CAR_NUMBERS.FILENAME ), _Nrc.CAR_NUMBERS.Capacity );
        private PassportIdCardNumbersModel CreatePassportIdCardNumbersModel( StreamReader_Disposer srd ) => new PassportIdCardNumbersModel( srd.Open( _Nrc.PASSPORT_IDCARD_NUMBERS.FILENAME ), _Nrc.PASSPORT_IDCARD_NUMBERS.Capacity );
        private DriverLicensesModel CreateDriverLicensesModel( StreamReader_Disposer srd ) => new DriverLicensesModel( srd.Open( _Nrc.DRIVER_LICENSES.FILENAME ), _Nrc.DRIVER_LICENSES.Capacity );
        private TaxIdentificationsModel CreateTaxIdentificationsModel( StreamReader_Disposer srd ) => new TaxIdentificationsModel( srd.Open( _Nrc.TAX_IDENTIFICATIONS.FILENAME ), _Nrc.TAX_IDENTIFICATIONS.Capacity );
        private CompaniesModel CreateCompaniesModel( StreamReader_Disposer srd )
            => new CompaniesModel( new CompaniesModel.InputParams_2()
            {
                CompanyVocab         = (srd.Open( _Nrc.COMPANY_VOCAB                 .FILENAME ), _Nrc.COMPANY_VOCAB.Capacity),
                Prefixes             = (srd.Open( _Nrc.COMPANY_PREFIXES              .FILENAME ), _Nrc.COMPANY_PREFIXES.Capacity),
                Suffixes             = (srd.Open( _Nrc.COMPANY_SUFFIXES              .FILENAME ), _Nrc.COMPANY_SUFFIXES.Capacity),
                PrefixesPrevSuffixes = (srd.Open( _Nrc.COMPANY_PREFIXES_PREV_SUFFIXES.FILENAME ), _Nrc.COMPANY_PREFIXES_PREV_SUFFIXES.Capacity),
                ExpandPreambles      = (srd.Open( _Nrc.COMPANY_EXPAND_PREAMBLES      .FILENAME ), _Nrc.COMPANY_EXPAND_PREAMBLES.Capacity),
            });

        public override NerProcessorConfig CreateNerProcessorConfig()
        {
            using var srd = new StreamReader_Disposer();

            var config = new NerProcessorConfig( srd.Open( _Nrc.SENT_SPLITTER_RESOURCES ), srd.Open( _Nrc.URL_DETECTOR_RESOURCES ) )
            {
#if (!WITHOUT_CRF)
                ModelFilename              = _Nrc.NER_MODEL,
                TemplateFile               = CRFTemplateFile.Load( _Nrc.NER_TEMPLATE, NerScriber.GetAllowedCrfTemplateFileColumnNames() ),
#endif
                SocialSecuritiesModel      = new SocialSecuritiesModel(),
                PhoneNumbersModel          = CreatePhoneNumbersModel( srd ),
                AddressModel               = CreateAddressModel( srd ),
                BankAccountsModel          = CreateBankAccountsModel( srd ),
                BirthplacesModel           = CreateBirthplacesModel( srd ),
                MaritalStatusesModel       = CreateMaritalStatusesModel( srd ),
                NamesModel                 = CreateNamesModel( srd ),
                NationalitiesModel         = CreateNationalitiesModel( srd ),
                CarNumbersModel            = CreateCarNumbersModel( srd ),
                PassportIdCardNumbersModel = CreatePassportIdCardNumbersModel( srd ),
                DriverLicensesModel        = CreateDriverLicensesModel( srd ),
                TaxIdentificationsModel    = CreateTaxIdentificationsModel( srd ),
                CompaniesModel             = CreateCompaniesModel( srd ),
            };
            return (config);
        }
        public override async Task< NerProcessorConfig > CreateNerProcessorConfig_Async()
        {
            using var srd = new StreamReader_Disposer();

            var phoneNumbersModel_task          = Task.Run( () => CreatePhoneNumbersModel( srd ) );
            var addressModel_task               = Task.Run( () => CreateAddressModel( srd ) );
            var bankAccountsModel_task          = Task.Run( () => CreateBankAccountsModel( srd ) );
            var birthplacesModel_task           = Task.Run( () => CreateBirthplacesModel( srd ) );
            var maritalStatusesModel_task       = Task.Run( () => CreateMaritalStatusesModel( srd ) );
            var namesModel_task                 = Task.Run( () => CreateNamesModel( srd ) );
            var nationalitiesModel_task         = Task.Run( () => CreateNationalitiesModel( srd ) );
            var carNumbersModel_task            = Task.Run( () => CreateCarNumbersModel( srd ) );
            var passportIdCardNumbersModel_task = Task.Run( () => CreatePassportIdCardNumbersModel( srd ) );
            var driverLicensesModel_task        = Task.Run( () => CreateDriverLicensesModel( srd ) );
            var taxIdentificationsModel_task    = Task.Run( () => CreateTaxIdentificationsModel( srd ) );
            var companiesModel_task             = Task.Run( () => CreateCompaniesModel( srd ) );

            var config_task = Task.Run( () => new NerProcessorConfig( srd.Open( _Nrc.SENT_SPLITTER_RESOURCES ), srd.Open( _Nrc.URL_DETECTOR_RESOURCES ) )
            {
#if (!WITHOUT_CRF)
                ModelFilename         = _Nrc.NER_MODEL,
                TemplateFile          = CRFTemplateFile.Load( _Nrc.NER_TEMPLATE, NerScriber.GetAllowedCrfTemplateFileColumnNames() ),
#endif
                SocialSecuritiesModel = new SocialSecuritiesModel(),
            });

            await Task.WhenAll( config_task, phoneNumbersModel_task, addressModel_task, bankAccountsModel_task, namesModel_task, nationalitiesModel_task, 
                                maritalStatusesModel_task, carNumbersModel_task, passportIdCardNumbersModel_task, driverLicensesModel_task, 
                                taxIdentificationsModel_task, companiesModel_task ).CAX();

            var config = config_task.Result;
                config.PhoneNumbersModel          = phoneNumbersModel_task         .Result;
                config.AddressModel               = addressModel_task              .Result;
                config.BankAccountsModel          = bankAccountsModel_task         .Result;
                config.BirthplacesModel           = birthplacesModel_task          .Result;
                config.MaritalStatusesModel       = maritalStatusesModel_task      .Result;
                config.NamesModel                 = namesModel_task                .Result;
                config.CarNumbersModel            = carNumbersModel_task           .Result;
                config.NationalitiesModel         = nationalitiesModel_task        .Result;
                config.PassportIdCardNumbersModel = passportIdCardNumbersModel_task.Result;
                config.DriverLicensesModel        = driverLicensesModel_task       .Result;
                config.TaxIdentificationsModel    = taxIdentificationsModel_task   .Result;
                config.CompaniesModel             = companiesModel_task            .Result;
            return (config);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ResourceFileConfig : TextFilesConfig, IConfig
    {
        /// <summary>
        /// 
        /// </summary>
        private static class NAM
        {
            public const string SENT_SPLITTER_RESOURCES_FILENAME = "sent-splitter-resources.xml";            
            public const string URL_DETECTOR_RESOURCES_FILENAME  = "url-detector-resources.xml";
            public const string NER_TEMPLATE_FILENAME            = "template_ner.txt";

            public const string BANK_NUMBERS_FILENAME                   = "bankCodes.txt";
            public const string BIRTHPLACE_PREAMBLES_FILENAME           = "birthplacePreambles.txt";
            public const string BIRTHPLACES_FILENAME                    = "birthplaces.txt";
            public const string CAR_NUMBERS_FILENAME                    = "carNumbers.txt";
            public const string CITIES_FILENAME                         = "cities.txt";
            public const string DRIVER_LICENSES_FILENAME                = "driverLicenses.txt";
            public const string excluded_names_xml                      = "excluded-names.xml";
            public const string FIRST_NAMES_FILENAME                    = "firstnames.txt";
            public const string MARITAL_STATUSES_FILENAME               = "maritalStatuses.txt";
            public const string MARITAL_STATUS_PREAMBLES_FILENAME       = "maritalStatusPreambles.txt";
            public const string NATIONALITIES_FILENAME                  = "nationalities.txt";
            public const string NATIONALITY_PREAMBLES_FILENAME          = "nationalityPreambles.txt";
            public const string PASSPORT_IDCARD_NUMBERS_FILENAME        = "old_PassportIdCardNumbers.txt";
            public const string TAX_IDENTIFICATIONS_FILENAME            = "old_TaxIdentifications.txt";
            public const string PHONE_NUMBERS_FILENAME                  = "phoneNumbers.txt";
            public const string STREETS_FILENAME                        = "streets.txt";
            public const string SUR_NAMES_FILENAME                      = "surnames.txt";
            public const string ZIP_CODES_FILENAME                      = "zipcodes.txt";
            public const string COMPANY_VOCAB_FILENAME                  = "companies.txt";
            public const string COMPANY_PREFIXES_FILENAME               = "companyPrefixes.txt";
            public const string COMPANY_SUFFIXES_FILENAME               = "companySuffixes.txt";
            public const string COMPANY_PREFIXES_PREV_SUFFIXES_FILENAME = "companyPrefixesPrevSuffixes.txt";
            public const string COMPANY_EXPAND_PREAMBLES_FILENAME       = "companyExpandPreambles.txt";
        }

        private string _ResourceFileName;
        public ResourceFileConfig( string resourceFileName ) => _ResourceFileName = resourceFileName;

        public static string EXCLUDED_NAMES_FILENAME => ConfigurationManager.AppSettings[ "EXCLUDED_NAMES_FILENAME" ];

        private static PhoneNumbersModel CreatePhoneNumbersModel( IReadOnlyDictionary< string, byte[] > rd, StreamReader_Disposer srd ) => new PhoneNumbersModel( srd.Open( rd[ NAM.PHONE_NUMBERS_FILENAME ] ), DefCap.phoneNumbers_capacity );
        private static AddressModel CreateAddressModel( IReadOnlyDictionary< string, byte[] > rd, StreamReader_Disposer srd )
            => new AddressModel( new AddressModel.InputParams_2()
            {
                ZipCodes = (srd.Open( rd[ NAM.ZIP_CODES_FILENAME ] ), DefCap.zipcodes_capacity),
                Cities   = (srd.Open( rd[ NAM.CITIES_FILENAME    ] ), DefCap.cities_capacityOneWord , DefCap.cities_capacityMultiWord),
                Streets  = (srd.Open( rd[ NAM.STREETS_FILENAME   ] ), DefCap.streets_capacityOneWord, DefCap.streets_capacityMultiWord),
            });
        private static BankAccountsModel CreateBankAccountsModel( IReadOnlyDictionary< string, byte[] > rd, StreamReader_Disposer srd ) => new BankAccountsModel( srd.Open( rd[ NAM.BANK_NUMBERS_FILENAME ] ), DefCap.bankCodes_capacity );
        private static BirthplacesModel CreateBirthplacesModel( IReadOnlyDictionary< string, byte[] > rd, StreamReader_Disposer srd )
            => new BirthplacesModel( new BirthplacesModel.InputParams_2()
            {
                Birthplaces         = (srd.Open( rd[ NAM.BIRTHPLACES_FILENAME          ] ), DefCap.birthplaces_capacity),
                BirthplacePreambles = (srd.Open( rd[ NAM.BIRTHPLACE_PREAMBLES_FILENAME ] ), DefCap.birthplacePreambles_capacity),
            });
        private static MaritalStatusesModel CreateMaritalStatusesModel( IReadOnlyDictionary< string, byte[] > rd, StreamReader_Disposer srd )
            => new MaritalStatusesModel( new MaritalStatusesModel.InputParams_2()
            {
                MaritalStatuses        = (srd.Open( rd[ NAM.MARITAL_STATUSES_FILENAME         ] ), DefCap.maritalStatuses_capacity),
                MaritalStatusPreambles = (srd.Open( rd[ NAM.MARITAL_STATUS_PREAMBLES_FILENAME ] ), DefCap.maritalStatusPreambles_capacity),
            });
        private NamesModel CreateNamesModel( IReadOnlyDictionary< string, byte[] > rd, StreamReader_Disposer srd )
        {
            var excluded_names_filename = EXCLUDED_NAMES_FILENAME;
            var exists                  = File.Exists( excluded_names_filename );

            var m = new NamesModel( new NamesModel.InputParams_2()
            {
                FirstNames                = (srd.Open( rd[ NAM.FIRST_NAMES_FILENAME ] ), DefCap.firstnames_capacity),
                SurNames                  = (srd.Open( rd[ NAM.SUR_NAMES_FILENAME   ] ), DefCap.surnames_capacity),
                ExcludedNamesFilename     = exists ? excluded_names_filename : null,
                ExcludedNamesStreamReader = exists ? null : srd.Open( _Nrc.EXCLUDED_NAMES ),
            });
            return (m);
        }
        private static NationalitiesModel CreateNationalitiesModel( IReadOnlyDictionary< string, byte[] > rd, StreamReader_Disposer srd )
            => new NationalitiesModel( new NationalitiesModel.InputParams_2()
            {
                Nationalities        = (srd.Open( rd[ NAM.NATIONALITIES_FILENAME         ] ), DefCap.nationalities_capacity),
                NationalityPreambles = (srd.Open( rd[ NAM.NATIONALITY_PREAMBLES_FILENAME ] ), DefCap.nationalityPreambles_capacity),
            });
        private static CarNumbersModel CreateCarNumbersModel( IReadOnlyDictionary< string, byte[] > rd, StreamReader_Disposer srd ) => new CarNumbersModel( srd.Open( rd[ NAM.CAR_NUMBERS_FILENAME ] ), DefCap.carNumbers_capacity );
        private static PassportIdCardNumbersModel CreatePassportIdCardNumbersModel( IReadOnlyDictionary< string, byte[] > rd, StreamReader_Disposer srd ) => new PassportIdCardNumbersModel( srd.Open( rd[ NAM.PASSPORT_IDCARD_NUMBERS_FILENAME ] ), DefCap.old_PassportIdCardNumbers_capacity );
        private static DriverLicensesModel CreateDriverLicensesModel( IReadOnlyDictionary< string, byte[] > rd, StreamReader_Disposer srd ) => new DriverLicensesModel( srd.Open( rd[ NAM.DRIVER_LICENSES_FILENAME ] ), DefCap.driverLicenses_capacity );
        private static TaxIdentificationsModel CreateTaxIdentificationsModel( IReadOnlyDictionary< string, byte[] > rd, StreamReader_Disposer srd ) => new TaxIdentificationsModel( srd.Open( rd[ NAM.TAX_IDENTIFICATIONS_FILENAME ] ), DefCap.old_TaxIdentifications_capacity );
        private static CompaniesModel CreateCompaniesModel( IReadOnlyDictionary< string, byte[] > rd, StreamReader_Disposer srd )
            => new CompaniesModel( new CompaniesModel.InputParams_2()
            {
                CompanyVocab         = (srd.Open( rd[ NAM.COMPANY_VOCAB_FILENAME                  ] ), DefCap.companyVocab_capacity),
                Prefixes             = (srd.Open( rd[ NAM.COMPANY_PREFIXES_FILENAME               ] ), DefCap.companyPrefixes_capacity),
                Suffixes             = (srd.Open( rd[ NAM.COMPANY_SUFFIXES_FILENAME               ] ), DefCap.companySuffixes_capacity),
                PrefixesPrevSuffixes = (srd.Open( rd[ NAM.COMPANY_PREFIXES_PREV_SUFFIXES_FILENAME ] ), DefCap.companyPrefixesPrevSuffixes_capacity),
                ExpandPreambles      = (srd.Open( rd[ NAM.COMPANY_EXPAND_PREAMBLES_FILENAME       ] ), DefCap.companyExpandPreambles_capacity),
            });

        public override NerProcessorConfig CreateNerProcessorConfig()
        {
            var rd = ZipFileExtensions_Adv.ReadFile( _ResourceFileName );
            using var srd = new StreamReader_Disposer();

            var config = new NerProcessorConfig( srd.Open( rd[ NAM.SENT_SPLITTER_RESOURCES_FILENAME ] ), srd.Open( rd[ NAM.URL_DETECTOR_RESOURCES_FILENAME ] ) )
            {
#if (!WITHOUT_CRF)
                ModelFilename              = _Nrc.NER_MODEL,
                TemplateFile               = CRFTemplateFile.Load( srd.Open( rd[ NAM.NER_TEMPLATE_FILENAME ] ), NerScriber.GetAllowedCrfTemplateFileColumnNames() ),
#endif
                SocialSecuritiesModel      = new SocialSecuritiesModel(),
                PhoneNumbersModel          = CreatePhoneNumbersModel( rd, srd ),
                AddressModel               = CreateAddressModel( rd, srd ),
                BankAccountsModel          = CreateBankAccountsModel( rd, srd ),
                BirthplacesModel           = CreateBirthplacesModel( rd, srd ),
                MaritalStatusesModel       = CreateMaritalStatusesModel( rd, srd ),
                NamesModel                 = CreateNamesModel( rd, srd ),
                NationalitiesModel         = CreateNationalitiesModel( rd, srd ),
                CarNumbersModel            = CreateCarNumbersModel( rd, srd ),
                PassportIdCardNumbersModel = CreatePassportIdCardNumbersModel( rd, srd ),
                DriverLicensesModel        = CreateDriverLicensesModel( rd, srd ),
                TaxIdentificationsModel    = CreateTaxIdentificationsModel( rd, srd ),
                CompaniesModel             = CreateCompaniesModel( rd, srd ),
            };
            return (config);
        }
        public override async Task< NerProcessorConfig > CreateNerProcessorConfig_Async()
        {
            var rd = ZipFileExtensions_Adv.ReadFile( _ResourceFileName );
            using var srd = new StreamReader_Disposer();

            var phoneNumbersModel_task          = Task.Run( () => CreatePhoneNumbersModel( rd, srd ) );
            var addressModel_task               = Task.Run( () => CreateAddressModel( rd, srd ) );
            var bankAccountsModel_task          = Task.Run( () => CreateBankAccountsModel( rd, srd ) );
            var birthplacesModel_task           = Task.Run( () => CreateBirthplacesModel( rd, srd ) );
            var maritalStatusesModel_task       = Task.Run( () => CreateMaritalStatusesModel( rd, srd ) );
            var namesModel_task                 = Task.Run( () => CreateNamesModel( rd, srd ) );
            var nationalitiesModel_task         = Task.Run( () => CreateNationalitiesModel( rd, srd ) );
            var carNumbersModel_task            = Task.Run( () => CreateCarNumbersModel( rd, srd ) );
            var passportIdCardNumbersModel_task = Task.Run( () => CreatePassportIdCardNumbersModel( rd, srd ) );
            var driverLicensesModel_task        = Task.Run( () => CreateDriverLicensesModel( rd, srd ) );
            var taxIdentificationsModel_task    = Task.Run( () => CreateTaxIdentificationsModel( rd, srd ) );
            var companiesModel_task             = Task.Run( () => CreateCompaniesModel( rd, srd ) );

            var config_task = Task.Run( () => new NerProcessorConfig( srd.Open( rd[ NAM.SENT_SPLITTER_RESOURCES_FILENAME ] ), srd.Open( rd[ NAM.URL_DETECTOR_RESOURCES_FILENAME ] ) )
            {
#if (!WITHOUT_CRF)
                ModelFilename         = _Nrc.NER_MODEL,
                TemplateFile          = CRFTemplateFile.Load( srd.Open( rd[ NAM.NER_TEMPLATE_FILENAME ] ), NerScriber.GetAllowedCrfTemplateFileColumnNames() ),
#endif
                SocialSecuritiesModel = new SocialSecuritiesModel(),
            });

            await Task.WhenAll( config_task, phoneNumbersModel_task, addressModel_task, bankAccountsModel_task, namesModel_task, nationalitiesModel_task, 
                                maritalStatusesModel_task, carNumbersModel_task, passportIdCardNumbersModel_task, driverLicensesModel_task, 
                                taxIdentificationsModel_task, companiesModel_task ).CAX();

            var config = config_task.Result;
                config.PhoneNumbersModel          = phoneNumbersModel_task         .Result;
                config.AddressModel               = addressModel_task              .Result;
                config.BankAccountsModel          = bankAccountsModel_task         .Result;
                config.BirthplacesModel           = birthplacesModel_task          .Result;
                config.MaritalStatusesModel       = maritalStatusesModel_task      .Result;
                config.NamesModel                 = namesModel_task                .Result;
                config.CarNumbersModel            = carNumbersModel_task           .Result;
                config.NationalitiesModel         = nationalitiesModel_task        .Result;
                config.PassportIdCardNumbersModel = passportIdCardNumbersModel_task.Result;
                config.DriverLicensesModel        = driverLicensesModel_task       .Result;
                config.TaxIdentificationsModel    = taxIdentificationsModel_task   .Result;
                config.CompaniesModel             = companiesModel_task            .Result;
            return (config);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ResourceAssemblyConfig : IConfig
    {
        /// <summary>
        /// 
        /// </summary>
        private static class NAM
        {
            public const string sent_splitter_resources_xml = "sent_splitter_resources_xml";
            public const string url_detector_resources_xml  = "url_detector_resources_xml";
            public const string templateNER                 = "templateNER";
            public const string excluded_names_xml          = "excluded_names_xml";

            public const string bankCodes                   = "bankCodes";
            public const string birthplacePreambles         = "birthplacePreambles";
            public const string birthplaces                 = "birthplaces";
            public const string carNumbers                  = "carNumbers";
            public const string cities                      = "cities";
            public const string driverLicenses              = "driverLicenses";
            public const string firstnames                  = "firstnames";
            public const string maritalStatuses             = "maritalStatuses";
            public const string maritalStatusPreambles      = "maritalStatusPreambles";
            public const string nationalities               = "nationalities";
            public const string nationalityPreambles        = "nationalityPreambles";
            public const string old_PassportIdCardNumbers   = "old_PassportIdCardNumbers";
            public const string old_TaxIdentifications      = "old_TaxIdentifications";
            public const string phoneNumbers                = "phoneNumbers";
            public const string streets                     = "streets";
            public const string surnames                    = "surnames";
            public const string zipcodes                    = "zipcodes";
            public const string companyVocab                = "companies";
            public const string companyPrefixes             = "companyPrefixes";
            public const string companySuffixes             = "companySuffixes";
            public const string companyPrefixesPrevSuffixes = "companyPrefixesPrevSuffixes";
            public const string companyExpandPreambles      = "companyExpandPreambles"; 
        }
        /// <summary>
        /// 
        /// </summary>
        private readonly struct Ral_Tuple : IDisposable
        {
            private readonly ResourceAssemblyLoader _Ral;
            private readonly StreamReader_Disposer  _Srd;
            public Ral_Tuple( string resourceAssemblyPath, string resourcesClassName )
            {
                _Ral = new ResourceAssemblyLoader( resourceAssemblyPath, resourcesClassName );
                _Srd = new StreamReader_Disposer();
            }
            public void Dispose()
            {
                _Ral.Dispose();
                _Srd.Dispose();
            }

            [M(O.AggressiveInlining)] public StreamReader GetPropertyAsDecompressedStreamReader( string name ) => _Srd.OpenCompression( _Ral.GetProperty< byte[] >( name ) );
            //[M(O.AggressiveInlining)] public int GetPropertyAsInt32( string name, int defval = 0 ) => (_Ral.TryGetProperty< string >( name, out var s ) && int.TryParse( s, out var i )) ? i : defval;
        }

        public const string RESOURCE_ASSEMBLY_PATH = "Lingvo.NER.Rules.Resources.dll";
        public const string RESOURCES_CLASSNAME    = "Lingvo.NER.Rules.Resources.Resources";

        private string _ResourceAssemblyPath;
        private string _ResourcesClassName;
        public ResourceAssemblyConfig( string resourceAssemblyPath = RESOURCE_ASSEMBLY_PATH, string resourcesClassName = RESOURCES_CLASSNAME ) 
        {
            _ResourceAssemblyPath = resourceAssemblyPath;
            _ResourcesClassName   = resourcesClassName;
        }

#if (!WITHOUT_CRF)
        public static string NER_MODEL_FILENAME => ConfigurationManager.AppSettings[ "NER_MODEL_FILENAME" ];
#endif
        public static string EXCLUDED_NAMES_FILENAME => ConfigurationManager.AppSettings[ "EXCLUDED_NAMES_FILENAME" ];

        private static PhoneNumbersModel CreatePhoneNumbersModel( in Ral_Tuple ral ) => new PhoneNumbersModel( ral.GetPropertyAsDecompressedStreamReader( NAM.phoneNumbers ), DefCap.phoneNumbers_capacity );
        private static AddressModel CreateAddressModel( in Ral_Tuple ral )
            => new AddressModel( new AddressModel.InputParams_2()
            {
                ZipCodes = (ral.GetPropertyAsDecompressedStreamReader( NAM.zipcodes ), DefCap.zipcodes_capacity),
                Cities   = (ral.GetPropertyAsDecompressedStreamReader( NAM.cities   ), DefCap.cities_capacityOneWord , DefCap.cities_capacityMultiWord),
                Streets  = (ral.GetPropertyAsDecompressedStreamReader( NAM.streets  ), DefCap.streets_capacityOneWord, DefCap.streets_capacityMultiWord),
            });
        private static BankAccountsModel CreateBankAccountsModel( in Ral_Tuple ral ) => new BankAccountsModel( ral.GetPropertyAsDecompressedStreamReader( NAM.bankCodes ), DefCap.bankCodes_capacity );
        private static BirthplacesModel CreateBirthplacesModel( in Ral_Tuple ral )
            => new BirthplacesModel( new BirthplacesModel.InputParams_2()
            {
                Birthplaces         = (ral.GetPropertyAsDecompressedStreamReader( NAM.birthplaces         ), DefCap.birthplaces_capacity),
                BirthplacePreambles = (ral.GetPropertyAsDecompressedStreamReader( NAM.birthplacePreambles ), DefCap.birthplacePreambles_capacity),
            });
        private static MaritalStatusesModel CreateMaritalStatusesModel( in Ral_Tuple ral )
            => new MaritalStatusesModel( new MaritalStatusesModel.InputParams_2()
            {
                MaritalStatuses        = (ral.GetPropertyAsDecompressedStreamReader( NAM.maritalStatuses        ), DefCap.maritalStatuses_capacity),
                MaritalStatusPreambles = (ral.GetPropertyAsDecompressedStreamReader( NAM.maritalStatusPreambles ), DefCap.maritalStatusPreambles_capacity),
            });
        private static NamesModel CreateNamesModel( in Ral_Tuple ral )
        {
            var excluded_names_filename = EXCLUDED_NAMES_FILENAME;
            var exists                  = File.Exists( excluded_names_filename );

            var m = new NamesModel( new NamesModel.InputParams_2()
            {
                FirstNames                = (ral.GetPropertyAsDecompressedStreamReader( NAM.firstnames ), DefCap.firstnames_capacity),
                SurNames                  = (ral.GetPropertyAsDecompressedStreamReader( NAM.surnames   ), DefCap.surnames_capacity),
                ExcludedNamesFilename     = exists ? excluded_names_filename : null,
                ExcludedNamesStreamReader = exists ? null : ral.GetPropertyAsDecompressedStreamReader( NAM.excluded_names_xml ),
            });
            return (m);
        }
        private static NationalitiesModel CreateNationalitiesModel( in Ral_Tuple ral )
            => new NationalitiesModel( new NationalitiesModel.InputParams_2()
            {
                Nationalities        = (ral.GetPropertyAsDecompressedStreamReader( NAM.nationalities        ), DefCap.nationalities_capacity),
                NationalityPreambles = (ral.GetPropertyAsDecompressedStreamReader( NAM.nationalityPreambles ), DefCap.nationalityPreambles_capacity),
            });
        private static CarNumbersModel CreateCarNumbersModel( in Ral_Tuple ral ) => new CarNumbersModel( ral.GetPropertyAsDecompressedStreamReader( NAM.carNumbers ), DefCap.carNumbers_capacity );
        private static PassportIdCardNumbersModel CreatePassportIdCardNumbersModel( in Ral_Tuple ral ) => new PassportIdCardNumbersModel( ral.GetPropertyAsDecompressedStreamReader( NAM.old_PassportIdCardNumbers ), DefCap.old_PassportIdCardNumbers_capacity );
        private static DriverLicensesModel CreateDriverLicensesModel( in Ral_Tuple ral ) => new DriverLicensesModel( ral.GetPropertyAsDecompressedStreamReader( NAM.driverLicenses ), DefCap.driverLicenses_capacity );
        private static TaxIdentificationsModel CreateTaxIdentificationsModel( in Ral_Tuple ral ) => new TaxIdentificationsModel( ral.GetPropertyAsDecompressedStreamReader( NAM.old_TaxIdentifications ), DefCap.old_TaxIdentifications_capacity );
        private static CompaniesModel CreateCompaniesModel( in Ral_Tuple ral )
            => new CompaniesModel( new CompaniesModel.InputParams_2()
            {
                CompanyVocab         = (ral.GetPropertyAsDecompressedStreamReader( NAM.companyVocab                ), DefCap.companyVocab_capacity),
                Prefixes             = (ral.GetPropertyAsDecompressedStreamReader( NAM.companyPrefixes             ), DefCap.companyPrefixes_capacity),
                Suffixes             = (ral.GetPropertyAsDecompressedStreamReader( NAM.companySuffixes             ), DefCap.companySuffixes_capacity),
                PrefixesPrevSuffixes = (ral.GetPropertyAsDecompressedStreamReader( NAM.companyPrefixesPrevSuffixes ), DefCap.companyPrefixesPrevSuffixes_capacity),
                ExpandPreambles      = (ral.GetPropertyAsDecompressedStreamReader( NAM.companyExpandPreambles      ), DefCap.companyExpandPreambles_capacity),
            });

        public NerProcessorConfig CreateNerProcessorConfig()
        {
            using var ral = new Ral_Tuple( _ResourceAssemblyPath, _ResourcesClassName );

            var config = new NerProcessorConfig( ral.GetPropertyAsDecompressedStreamReader( NAM.sent_splitter_resources_xml ), 
                                                 ral.GetPropertyAsDecompressedStreamReader( NAM.url_detector_resources_xml  ) )
            {
#if (!WITHOUT_CRF)
                ModelFilename              = NER_MODEL_FILENAME,
                TemplateFile               = CRFTemplateFile.Load( ral.GetPropertyAsDecompressedStreamReader( NAM.templateNER ), NerScriber.GetAllowedCrfTemplateFileColumnNames() ),
#endif
                SocialSecuritiesModel      = new SocialSecuritiesModel(),
                PhoneNumbersModel          = CreatePhoneNumbersModel( ral ),
                AddressModel               = CreateAddressModel( ral ),
                BankAccountsModel          = CreateBankAccountsModel( ral ),
                BirthplacesModel           = CreateBirthplacesModel( ral ),
                MaritalStatusesModel       = CreateMaritalStatusesModel( ral ),
                NamesModel                 = CreateNamesModel( ral ),
                NationalitiesModel         = CreateNationalitiesModel( ral ),
                CarNumbersModel            = CreateCarNumbersModel( ral ),
                PassportIdCardNumbersModel = CreatePassportIdCardNumbersModel( ral ),
                DriverLicensesModel        = CreateDriverLicensesModel( ral ),
                TaxIdentificationsModel    = CreateTaxIdentificationsModel( ral ),
                CompaniesModel             = CreateCompaniesModel( ral ),
            };
            return (config);
        }
        public async Task< NerProcessorConfig > CreateNerProcessorConfig_Async()
        {
            using var ral = new Ral_Tuple( _ResourceAssemblyPath, _ResourcesClassName );

            var phoneNumbersModel_task          = Task.Run( () => CreatePhoneNumbersModel( ral ) );
            var addressModel_task               = Task.Run( () => CreateAddressModel( ral ) );
            var bankAccountsModel_task          = Task.Run( () => CreateBankAccountsModel( ral ) );
            var birthplacesModel_task           = Task.Run( () => CreateBirthplacesModel( ral ) );
            var maritalStatusesModel_task       = Task.Run( () => CreateMaritalStatusesModel( ral ) );
            var namesModel_task                 = Task.Run( () => CreateNamesModel( ral ) );
            var nationalitiesModel_task         = Task.Run( () => CreateNationalitiesModel( ral ) );
            var carNumbersModel_task            = Task.Run( () => CreateCarNumbersModel( ral ) );
            var passportIdCardNumbersModel_task = Task.Run( () => CreatePassportIdCardNumbersModel( ral ) );
            var driverLicensesModel_task        = Task.Run( () => CreateDriverLicensesModel( ral ) );
            var taxIdentificationsModel_task    = Task.Run( () => CreateTaxIdentificationsModel( ral ) );
            var companiesModel_task             = Task.Run( () => CreateCompaniesModel( ral ) );

            var config_task = Task.Run( () => new NerProcessorConfig( ral.GetPropertyAsDecompressedStreamReader( NAM.sent_splitter_resources_xml ), 
                                                                      ral.GetPropertyAsDecompressedStreamReader( NAM.url_detector_resources_xml  ) )
            {
#if (!WITHOUT_CRF)
                ModelFilename         = NER_MODEL_FILENAME,
                TemplateFile          = CRFTemplateFile.Load( ral.GetPropertyAsDecompressedStreamReader( NAM.templateNER ), NerScriber.GetAllowedCrfTemplateFileColumnNames() ),
#endif
                SocialSecuritiesModel = new SocialSecuritiesModel(),
            });

            await Task.WhenAll( phoneNumbersModel_task, addressModel_task, bankAccountsModel_task, namesModel_task, config_task, nationalitiesModel_task, 
                                maritalStatusesModel_task, carNumbersModel_task, passportIdCardNumbersModel_task, driverLicensesModel_task, 
                                taxIdentificationsModel_task, companiesModel_task ).CAX();

            var config = config_task.Result;
                config.PhoneNumbersModel          = phoneNumbersModel_task         .Result;
                config.AddressModel               = addressModel_task              .Result;
                config.BankAccountsModel          = bankAccountsModel_task         .Result;
                config.BirthplacesModel           = birthplacesModel_task          .Result;
                config.MaritalStatusesModel       = maritalStatusesModel_task      .Result;
                config.NamesModel                 = namesModel_task                .Result;
                config.CarNumbersModel            = carNumbersModel_task           .Result;
                config.NationalitiesModel         = nationalitiesModel_task        .Result;
                config.PassportIdCardNumbersModel = passportIdCardNumbersModel_task.Result;
                config.DriverLicensesModel        = driverLicensesModel_task       .Result;
                config.TaxIdentificationsModel    = taxIdentificationsModel_task   .Result;
                config.CompaniesModel             = companiesModel_task            .Result;
            return (config);
        }
        public void ReloadParamsFromConfigFile() { }
        public IConfig GetImpl() => this;
    }
    #endregion


    /// <summary>
    /// 
    /// </summary>
    internal sealed class StreamReader_Disposer : IDisposable
    {
        private ConcurrentBag< IDisposable > _Disposables;
        public StreamReader_Disposer() => _Disposables = new ConcurrentBag< IDisposable >();
        public void Dispose()
        {
            //for ( ; _Disposables.TryTake( out var d ); )
            foreach ( var d in _Disposables )
            {
                d.Dispose();
            }
        }

        public StreamReader Hold( StreamReader sr )
        {
            _Disposables.Add( sr );
            return (sr);
        }
        public StreamReader Open( string fn, bool tryOpenCompressionStream = true )
        {
            StreamReader sr;
            if ( tryOpenCompressionStream && IsCompressionFormatByExtension( fn )/*!IsPlainTextFormatByExtension( fn )*/ )
            {
                var fs = File.OpenRead( fn );
                var cs = new _Compression_Stream_( fs, CompressionMode.Decompress );
                sr = new StreamReader( cs );
            }
            else
            {
                sr = new StreamReader( fn );
            }
            _Disposables.Add( sr );
            return (sr);
        }
        public StreamReader Open( byte[] bytes )
        {
            var ms = new MemoryStream( bytes );
            var sr = new StreamReader( ms );
            _Disposables.Add( sr );
            return (sr);
        }
        public StreamReader OpenCompression( byte[] bytes )
        {
            var ms = new MemoryStream( bytes );
            var cs = new _Compression_Stream_( ms, CompressionMode.Decompress );
            var sr = new StreamReader( cs );
            _Disposables.Add( sr );
            return (sr);
        }
        /*[M(O.AggressiveInlining)] private static bool IsPlainTextFormatByExtension( string fn )
        {
            var ext = Path.GetExtension( fn );
            return (string.Compare( ext, ".txt", true ) == 0) || (string.Compare( ext, ".xml", true ) == 0);
        }*/
        [M(O.AggressiveInlining)] private static bool IsCompressionFormatByExtension( string fn ) => (string.Compare( Path.GetExtension( fn ), ".bin", true ) == 0);
    }

    ///// <summary>
    ///// 
    ///// </summary>
    //internal readonly struct LoadConfigConsolePrinter : IDisposable
    //{
    //    private readonly Stopwatch _sw;
    //    public LoadConfigConsolePrinter( bool print2console, string addition_msg = null )
    //    {
    //        if ( print2console )
    //        {
    //            _sw = Stopwatch.StartNew();
    //            Console.Write( $"{{load ner-config{((addition_msg != null) ? $" {addition_msg}" : null)}..." );
    //        }
    //        else
    //        {
    //            _sw = null;
    //        }
    //    }
    //    public void Dispose()
    //    {
    //        if ( _sw != null )
    //        {
    //            _sw.Stop(); Console.WriteLine( $"elapsed: {_sw.Elapsed}}}" );
    //        }
    //    }

    //    public static LoadConfigConsolePrinter Create( bool print2console, string addition_msg = null ) => new LoadConfigConsolePrinter( print2console, addition_msg );
    //    public static LoadConfigConsolePrinter Create_Async( bool print2console, string addition_msg = null ) => new LoadConfigConsolePrinter( print2console, ((addition_msg != null) ? (addition_msg + ' ') : null) + "async" );
    //}

    /// <summary>
    /// 
    /// </summary>
    public static partial class Config_Extensions
    {
        public static ConfiguredTaskAwaitable< T > CAX< T >( this Task< T > task ) => task.ConfigureAwait( false );
        public static ConfiguredTaskAwaitable CAX( this Task task ) => task.ConfigureAwait( false );
#if NETCOREAPP
        public static ConfiguredValueTaskAwaitable< T > CAX< T >( this ValueTask< T > task ) => task.ConfigureAwait( false );
        public static ConfiguredValueTaskAwaitable CAX( this ValueTask task ) => task.ConfigureAwait( false );
#endif
        public static bool IsNullOrEmpty( this string s ) => string.IsNullOrEmpty( s );
        public static bool IsNullOrWhiteSpace( this string s ) => string.IsNullOrWhiteSpace( s );

        private static string get_addition_msg_by_type( this IConfig config )
        {
            if ( config is ResourceAssemblyConfig ) return (" (resource-assembly)");
            if ( config is StreamReaderConfig     ) return (" (files-stream-reader)");
            if ( config is ResourceFileConfig     ) return (" (resource-file)");
            if ( config is TextFilesConfig        ) return (" (text-files)");
            return (" (?)");// (null);
        }
        public static NerProcessorConfig CreateNerProcessorConfigEx( this IConfig config )
        {
            //using var _ = LoadConfigConsolePrinter.Create( print2console: true );
            //return (config.CreateNerProcessorConfig());

            var sw = Stopwatch.StartNew(); Console.Write( $"{{load ner-config{config.get_addition_msg_by_type()}..." );

            var nerConfig = config.CreateNerProcessorConfig();

            sw.Stop(); Console.WriteLine( $"elapsed: {sw.Elapsed}}}" );

            return (nerConfig);
        }
        public static async Task< NerProcessorConfig > CreateNerProcessorConfig_AsyncEx( this IConfig config )
        {
            //using var _ = LoadConfigConsolePrinter.Create_Async( print2console: true );
            //var nerConfig = await config.CreateNerProcessorConfig_Async().CAX();
            //return (nerConfig);

            var sw = Stopwatch.StartNew(); Console.Write( $"{{load ner-config{config.GetImpl().get_addition_msg_by_type()} async..." );

            var nerConfig = await config.CreateNerProcessorConfig_Async().CAX();

            sw.Stop(); Console.WriteLine( $"elapsed: {sw.Elapsed}}}" );

            return (nerConfig);
        }
        public static Task< NerProcessorConfig > CreateNerProcessorConfig_AsyncEx( this IConfig config, bool print2console )
        {
            if ( print2console )
            {
                return (config.CreateNerProcessorConfig_AsyncEx());
            }
            return (config.CreateNerProcessorConfig_Async());
        }
    }
}
