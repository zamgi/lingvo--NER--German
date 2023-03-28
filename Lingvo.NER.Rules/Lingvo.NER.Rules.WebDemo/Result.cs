using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Web;

using Newtonsoft.Json;

using Lingvo.NER.Rules.Address;
using Lingvo.NER.Rules.BankAccounts;
using Lingvo.NER.Rules.Birthdays;
using Lingvo.NER.Rules.Birthplaces;
using Lingvo.NER.Rules.CarNumbers;
using Lingvo.NER.Rules.Companies;
using Lingvo.NER.Rules.CustomerNumbers;
using Lingvo.NER.Rules.DriverLicenses;
using Lingvo.NER.Rules.HealthInsurances;
using Lingvo.NER.Rules.MaritalStatuses;
using Lingvo.NER.Rules.Names;
using Lingvo.NER.Rules.Nationalities;
using Lingvo.NER.Rules.NerPostMerging;
using Lingvo.NER.Rules.PassportIdCardNumbers;
using Lingvo.NER.Rules.CreditCards;
using Lingvo.NER.Rules.PhoneNumbers;
using Lingvo.NER.Rules.SocialSecurities;
using Lingvo.NER.Rules.TaxIdentifications;
using Lingvo.NER.Rules.urls;
using Lingvo.NER.Rules.tokenizing;

using JC  = Newtonsoft.Json.JsonConverterAttribute;
using JP  = Newtonsoft.Json.JsonPropertyAttribute;
using SEC = Newtonsoft.Json.Converters.StringEnumConverter;
using D   = Newtonsoft.Json.DefaultValueHandling;
using N   = Newtonsoft.Json.NullValueHandling;

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class ResultJson
    {
        /// <summary>
        /// 
        /// </summary>
        public struct WordInfo
        {
            [JP("i")]   public int    startIndex { get; set; }
            [JP("l")]   public int    length     { get; set; }
            [JP("ner")] public string ner        { get; set; }
            [JP("v")]   public string value      { get; set; }

            [JP(DefaultValueHandling=D.Ignore)] public string street   { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string houseNum { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string indexNum { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string city     { get; set; }

            [JP(NullValueHandling=N.Ignore), JC(typeof(SEC))] public UrlTypeEnum? urlType { get; set; }

            [JP(DefaultValueHandling=D.Ignore)] public string accountNumber { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string accountOwner  { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string bankCode      { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string bankName      { get; set; }
            [JP(NullValueHandling=N.Ignore), JC(typeof(SEC))] public BankAccountTypeEnum? bankAccountType { get; set; }
            [JP(DefaultValueHandling = D.Ignore)] public string customerNumber { get; set; }

            [JP(DefaultValueHandling=D.Ignore)] public string firstName { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string surName   { get; set; }
            [JP(NullValueHandling=N.Ignore), JC(typeof(SEC))] public TextPreambleTypeEnum? nameType { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string maritalStatus { get; set; }

            [JP(DefaultValueHandling=D.Ignore)] public string birthdayDateTime      { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string birthplace            { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string nationality           { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string creditCardNumber      { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string passportIdCardNumber  { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string carNumber             { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string healthInsuranceNumber { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string driverLicense         { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string socialSecurity        { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string taxIdentification     { get; set; }
            [JP(DefaultValueHandling=D.Ignore)] public string companyName           { get; set; }            
        } 
        /// <summary>
        /// 
        /// </summary>
        public sealed class UnitedEntityInfo
        {
            public UnitedEntityInfo( NerUnitedEntity nue, StringBuilder buff )
            {
                if ( TryCreate( nue.Word_1, buff, out var wi ) ) w1 = wi; 
                if ( TryCreate( nue.Word_2, buff, out     wi ) ) w2 = wi;
                if ( TryCreate( nue.Word_3, buff, out     wi ) ) w3 = wi;
                if ( TryCreate( nue.Word_4, buff, out     wi ) ) w4 = wi;
                if ( TryCreate( nue.Word_5, buff, out     wi ) ) w5 = wi;

                startIndex = int.MaxValue;
                x( nue.Word_1 );
                x( nue.Word_2 );
                x( nue.Word_3 );
                x( nue.Word_4 );
                x( nue.Word_5 );
                if ( nue.Word_6_and_more != null )
                {
                    foreach ( var w in nue.Word_6_and_more )
                    {
                        x( w );
                    }
                }
                length = _endIndex - startIndex;
            }
            private void x( word_t w )
            {
                if ( w != null )
                {
                    startIndex = Math.Min( w.startIndex, startIndex );
                    _endIndex  = Math.Max( w.startIndex + w.length, _endIndex );
                }
            }

            [JP(NullValueHandling=N.Ignore)] public WordInfo? w1 { get; }
            [JP(NullValueHandling=N.Ignore)] public WordInfo? w2 { get; }
            [JP(NullValueHandling=N.Ignore)] public WordInfo? w3 { get; }
            [JP(NullValueHandling=N.Ignore)] public WordInfo? w4 { get; }
            [JP(NullValueHandling=N.Ignore)] public WordInfo? w5 { get; }
            [JP("i")] public int startIndex { get; private set; }
            [JP("l")] public int length     { get; private set; }
            [JsonIgnore] private int _endIndex;
        }

        [JP("words", DefaultValueHandling=D.Ignore)] public IList< WordInfo > Words { get; }
        [JP("unitedEntities", DefaultValueHandling=D.Ignore)] public IList< UnitedEntityInfo > UnitedEntities { get; }
        [JP("relevanceRanking")] public int RelevanceRanking { get; }
        [JP("errorMessage", DefaultValueHandling=D.Ignore)] public string ExceptionMessage { get; }

        private static void Create( word_t w, StringBuilder buff, out WordInfo wi )
        {
            wi = new WordInfo()
            {
                startIndex = w.startIndex,
                length     = w.GetNerLength(),
                ner        = w.nerOutputType.ToText(),
                value      = w.GetNerValue( buff ),
            };

            switch ( w.nerOutputType )
            {
                case NerOutputType.Address:
                    var aw = (AddressWord) w;
                    wi.street   = aw.Street;
                    wi.houseNum = aw.HouseNumber;
                    wi.indexNum = aw.ZipCodeNumber;
                    wi.city     = aw.City;
                break;

                case NerOutputType.AccountNumber: 
                    var ba = (BankAccountWord) w;
                    wi.bankAccountType = ba.BankAccountType;
                    wi.accountNumber   = ba.AccountNumber;
                    wi.accountOwner    = ba.AccountOwner;
                    wi.bankCode        = ba.BankCode;
                    wi.bankName        = ba.BankName;
                break;

                case NerOutputType.Name:
                    var nw = (NameWord) w;
                    wi.firstName = nw.Firstname;
                    wi.surName   = nw.Surname;
                    if ( nw.TextPreambleType != TextPreambleTypeEnum.__UNDEFINED__ )
                    {
                        wi.nameType = nw.TextPreambleType;
                    }
                break;

                case NerOutputType.PhoneNumber         : wi.city                  = ((PhoneNumberWord)          w).CityAreaName;                              break;
                case NerOutputType.CustomerNumber      : wi.customerNumber        = ((CustomerNumberWord)       w).CustomerNumber;                            break;
                case NerOutputType.Birthday            : wi.birthdayDateTime      = ((BirthdayWord)             w).BirthdayDateTime.ToString( "dd.MM.yyyy" ); break;
                case NerOutputType.Birthplace          : wi.birthplace            = ((BirthplaceWord)           w).Birthplace;                                break;
                case NerOutputType.MaritalStatus       : wi.maritalStatus         = ((MaritalStatusWord)        w).MaritalStatus;                             break;
                case NerOutputType.Nationality         : wi.nationality           = ((NationalityWord)          w).Nationality;                               break;
                case NerOutputType.CreditCard          : wi.creditCardNumber      = ((CreditCardWord)           w).CreditCardNumber;                          break;
                case NerOutputType.PassportIdCardNumber: wi.passportIdCardNumber  = ((PassportIdCardNumberWord) w).PassportIdCardNumbers;                     break;
                case NerOutputType.CarNumber           : wi.carNumber             = ((CarNumberWord)            w).CarNumber;                                 break;
                case NerOutputType.HealthInsurance     : wi.healthInsuranceNumber = ((HealthInsuranceWord)      w).HealthInsuranceNumber;                     break;
                case NerOutputType.DriverLicense       : wi.driverLicense         = ((DriverLicenseWord)        w).DriverLicense;                             break;
                case NerOutputType.SocialSecurity      : wi.socialSecurity        = ((SocialSecurityWord)       w).SocialSecurityNumber;                      break;
                case NerOutputType.TaxIdentification   : wi.taxIdentification     = ((TaxIdentificationWord)    w).TaxIdentificationNumber;                   break;
                case NerOutputType.Url                 : 
                case NerOutputType.Email               : wi.urlType               = ((UrlOrEmailWordBase)       w).UrlType;                                   break;
                case NerOutputType.Company             : wi.companyName           = ((CompanyWord)              w).Name;                                      break;
            }
        }
        private static bool TryCreate( word_t w, StringBuilder buff, out WordInfo wi )
        {
            if ( w != null )
            {
                Create( w, buff, out wi );
                return (true);
            }
            wi = default;
            return (false);
        }

        public ResultJson( Exception ex ) => ExceptionMessage = ex.ToString();
        public ResultJson( IList< word_t > words )
        {
            var buff = new StringBuilder();

            var lst = new List< WordInfo >( words.Count );
            foreach ( var w in words )
            {
                if ( w.HasNerPrevWord ) continue;
                Create( w, buff, out var wi );
                lst.Add( wi );
            }
            Words = lst;
        }
        public ResultJson( IList< word_t > words, IList< NerUnitedEntity > nerUnitedEntities, int relevanceRanking ) : this( words )
        {
            var buff = new StringBuilder();

            var lst = new List< UnitedEntityInfo >( nerUnitedEntities.Count );
            foreach ( var nue in nerUnitedEntities )
            {
                lst.Add( new UnitedEntityInfo( nue, buff ) );
            }
            UnitedEntities   = lst;
            RelevanceRanking = relevanceRanking;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class ConcurrentFactoryHelper
    {
        /// <summary>
        /// 
        /// </summary>
        private struct InterlockedLock
        {
            private const int OCCUPIED = 1;
            private const int FREE     = 0;

            private int __Lock__;

            public bool TryEnter() => (Interlocked.CompareExchange( ref __Lock__, OCCUPIED, FREE ) == FREE);
            public void Enter_v1()
            {
                if ( Interlocked.CompareExchange( ref __Lock__, OCCUPIED, FREE ) != FREE )
                {
                    var spinWait = default(SpinWait);
                    while ( Interlocked.CompareExchange( ref __Lock__, OCCUPIED, FREE ) != FREE )
                    {
                        spinWait.SpinOnce();
                    }
                }
            }
            public void Enter_v2()
            {
                if ( Interlocked.CompareExchange( ref __Lock__, OCCUPIED, FREE ) != FREE )
                {
                    while ( Interlocked.CompareExchange( ref __Lock__, OCCUPIED, FREE ) != FREE )
                    {
                        Thread.Sleep( 1 );
                    }
                }
            }
            public void Exit() => Interlocked.Exchange( ref __Lock__, FREE ); //Volatile.Write( ref _Locker, FREE );
        }

        private static InterlockedLock _Lock;
        private static ConcurrentFactory _ConcurrentFactory;
        public static async Task< ConcurrentFactory > GetConcurrentFactory_Async( bool reloadModel )
        {
            var f = _ConcurrentFactory;
            if ( f == null )
            {
                _Lock.Enter_v2();
                try
                {
                    f = _ConcurrentFactory;
                    if ( f == null )
                    {
                        var config = await Config.Inst.CreateNerProcessorConfig_Async().ConfigureAwait( false );
                        f = new ConcurrentFactory( config, ConfigEx.CONCURRENT_FACTORY_INSTANCE_COUNT );
                        _ConcurrentFactory = f;
                    }
                }
                finally
                {
                    _Lock.Exit();
                }
            }
            else if ( reloadModel )
            {
                _ConcurrentFactory?.Dispose_NoThrow();
                _ConcurrentFactory = null;
                return (await GetConcurrentFactory_Async( false ).ConfigureAwait( false ));
            }
            return (f);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class ResultExtensions
    {
        public static void SendJsonResponse( this HttpResponse response, IList< word_t > words ) => response.SendJsonResponse( new ResultJson( words ) );
        public static void SendJsonResponse( this HttpResponse response, IList< word_t > nerWords, IList< NerUnitedEntity > nerUnitedEntities, int relevanceRanking ) => response.SendJsonResponse( new ResultJson( nerWords, nerUnitedEntities, relevanceRanking ) );
        public static void SendJsonResponse( this HttpResponse response, Exception ex ) => response.SendJsonResponse( new ResultJson( ex ) );
        private static void SendJsonResponse( this HttpResponse response, ResultJson result )
        {
            response.ContentType = "application/json";
            //---response.Headers.Add( "Access-Control-Allow-Origin", "*" );

            var json = JsonConvert.SerializeObject( result );
            response.Write( json );
        }
    }
}