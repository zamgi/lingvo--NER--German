using System;
using System.Collections.Generic;
using System.Text;

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
using Lingvo.NER.Rules.NerPostMerging;
using Lingvo.NER.Rules.PassportIdCardNumbers;
using Lingvo.NER.Rules.PhoneNumbers;
using Lingvo.NER.Rules.SocialSecurities;
using Lingvo.NER.Rules.TaxIdentifications;
using Lingvo.NER.Rules.tokenizing;
using Lingvo.NER.Rules.urls;

using JP = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Lingvo.NER.Rules.WebService
{
    /// <summary>
    /// 
    /// </summary>
    public struct ResultVM
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

            public string street   { get; set; }
            public string houseNum { get; set; }
            public string indexNum { get; set; }
            public string city     { get; set; }

            public UrlTypeEnum? urlType { get; set; }

            public string accountNumber { get; set; }
            public string accountOwner  { get; set; }
            public string bankCode      { get; set; }
            public string bankName      { get; set; }
            public BankAccountTypeEnum? bankAccountType { get; set; }

            public string firstName { get; set; }
            public string surName   { get; set; }
            public TextPreambleTypeEnum? nameType { get; set; }

            public string customerNumber        { get; set; }
            public string maritalStatus         { get; set; }
            public string birthdayDateTime      { get; set; }
            public string birthplace            { get; set; }
            public string nationality           { get; set; }
            public string creditCardNumber      { get; set; }
            public string passportIdCardNumber  { get; set; }
            public string carNumber             { get; set; }
            public string healthInsuranceNumber { get; set; }
            public string driverLicense         { get; set; }
            public string socialSecurity        { get; set; }
            public string taxIdentification     { get; set; }
            public string companyName           { get; set; }            
        }
        /// <summary>
        /// 
        /// </summary>
        public sealed class UnitedEntityInfo
        {
            public UnitedEntityInfo() { }
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

            public WordInfo? w1 { get; }
            public WordInfo? w2 { get; }
            public WordInfo? w3 { get; }
            public WordInfo? w4 { get; }
            public WordInfo? w5 { get; }
            [JP("i")] public int startIndex { get; set; }
            [JP("l")] public int length     { get; set; }
            [JsonIgnore] private int _endIndex;
        }

        [JP("words")           ] public IList< WordInfo > Words { get; init; }
        [JP("unitedEntities")  ] public IList< UnitedEntityInfo > UnitedEntities { get; init; }
        [JP("relevanceRanking")] public int RelevanceRanking { get; init; }
        [JP("errorMessage")    ] public string ErrorMessage     { get; init; }
        [JP("fullErrorMessage")] public string FullErrorMessage { get; init; }
        public InitParamsVM InitParams { get; set; }

        private static WordInfo Create( word_t w, StringBuilder buff )
        {
            var wi = new WordInfo()
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
                    wi.firstName     = nw.Firstname;
                    wi.surName       = nw.Surname;
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

            return (wi);
        }
        private static bool TryCreate( word_t w, StringBuilder buff, out WordInfo wi )
        {
            if ( w != null )
            {
                wi = Create( w, buff );
                return (true);
            }
            wi = default;
            return (false);
        }

        public ResultVM( in InitParamsVM p ) : this()
        {            
            if ( p.ReturnInputText.GetValueOrDefault() )
            {
                InitParams = p;                
            }
            else
            {
                var t = p;
                t.Text = null;
                InitParams = t;
            }
        }
        public ResultVM( in InitParamsVM p, Exception ex ) : this( in p )
        {
            ErrorMessage     = ex.Message;
            FullErrorMessage = ex.ToString();
        }
        public ResultVM( in InitParamsVM p, IReadOnlyList< word_t > words ) : this( in p )
        {
            var lst = new List< WordInfo >( words.Count );
            if ( words.AnyEx() )
            {
                var buff = new StringBuilder();

                var returnWordValue = p.ReturnWordValue.GetValueOrDefault( true );
                foreach ( var w in words )
                {
                    if ( w.HasNerPrevWord ) continue;
                    var wi = Create( w, buff );
                    if ( !returnWordValue )
                    {
                        wi.value = null;
                    }
                    lst.Add( wi );
                }
            }
            Words = lst;
        }
        public ResultVM( in InitParamsVM p, IReadOnlyList< word_t > words, IReadOnlyList< NerUnitedEntity > nerUnitedEntities, int relevanceRanking ) : this( in p, words )
        {
            if ( p.ReturnUnitedEntities.GetValueOrDefault() && nerUnitedEntities.AnyEx() )
            {
                var buff = new StringBuilder();

                var lst = new List< UnitedEntityInfo >( nerUnitedEntities.Count );
                foreach ( var nue in nerUnitedEntities )
                {
                    lst.Add( new UnitedEntityInfo( nue, buff ) );
                }
                UnitedEntities = lst;
            }
            RelevanceRanking = relevanceRanking;
        }
#if DEBUG
        public override string ToString() => $"count: {(Words?.Count).GetValueOrDefault()}";
#endif
    }
}
