using System;
using System.Collections.Generic;
using System.Linq;
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
using Lingvo.NER.Rules.PassportIdCardNumbers;
using Lingvo.NER.Rules.PhoneNumbers;
using Lingvo.NER.Rules.SocialSecurities;
using Lingvo.NER.Rules.TaxIdentifications;
using Lingvo.NER.Rules.urls;
using Lingvo.NER.NeuralNetwork;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Combined.WebService
{
    using NerRuleOutputType = Lingvo.NER.Rules.NerOutputType;
    using NerRuleExtensions = Lingvo.NER.Rules.NerExtensions;
    using _NerRules_word_t  = Lingvo.NER.Rules.tokenizing.word_t;
    using WordInfo          = ResultVM.WordInfo;

    /// <summary>
    /// 
    /// </summary>
    internal static class ModelsExtensions
    {
        [M(O.AggressiveInlining)] public static ErrorVM ToErrorVM( this Exception ex ) => new ErrorVM() { ErrorMessage = ex.Message, FullErrorMessage = ex.ToString(), };

        [M(O.AggressiveInlining)] public static ResultVM ToResultVM( this in (List< _NerRules_word_t > nerWords, Exception error) t, in InitParamsVM p ) => Create( p, t.nerWords );

        [M(O.AggressiveInlining)] private static WordInfo Create( _NerRules_word_t w, StringBuilder buff )
        {
            var wi = new WordInfo()
            {
                startIndex = w.startIndex,
                length     = w.GetNerLength(),
                ner        = NerRuleExtensions.ToText( w.nerOutputType ),
                value      = w.GetNerValue( buff ),
            };

            switch ( w.nerOutputType )
            {
                case NerRuleOutputType.Address:
                    var aw = (AddressWord) w;
                    wi.street   = aw.Street;
                    wi.houseNum = aw.HouseNumber;
                    wi.indexNum = aw.ZipCodeNumber;
                    wi.city     = aw.City;
                break;

                case NerRuleOutputType.AccountNumber: 
                    var ba = (BankAccountWord) w;
                    wi.bankAccountType = ba.BankAccountType.ToString();
                    wi.accountNumber   = ba.AccountNumber;
                    wi.accountOwner    = ba.AccountOwner;
                    wi.bankCode        = ba.BankCode;
                    wi.bankName        = ba.BankName;
                break;

                case NerRuleOutputType.Name:
                    var nw = (NameWord) w;
                    wi.firstName     = nw.Firstname;
                    wi.surName       = nw.Surname;
                    if ( nw.TextPreambleType != TextPreambleTypeEnum.__UNDEFINED__ )
                    {
                        wi.nameType = nw.TextPreambleType.ToString();
                    }                
                break;

                case NerRuleOutputType.PhoneNumber         : wi.city                  = ((PhoneNumberWord)          w).CityAreaName;                              break;
                case NerRuleOutputType.CustomerNumber      : wi.customerNumber        = ((CustomerNumberWord)       w).CustomerNumber;                            break;
                case NerRuleOutputType.Birthday            : wi.birthdayDateTime      = ((BirthdayWord)             w).BirthdayDateTime.ToString( "dd.MM.yyyy" ); break;
                case NerRuleOutputType.Birthplace          : wi.birthplace            = ((BirthplaceWord)           w).Birthplace;                                break;
                case NerRuleOutputType.MaritalStatus       : wi.maritalStatus         = ((MaritalStatusWord)        w).MaritalStatus;                             break;
                case NerRuleOutputType.Nationality         : wi.nationality           = ((NationalityWord)          w).Nationality;                               break;
                case NerRuleOutputType.CreditCard          : wi.creditCardNumber      = ((CreditCardWord)           w).CreditCardNumber;                          break;
                case NerRuleOutputType.PassportIdCardNumber: wi.passportIdCardNumber  = ((PassportIdCardNumberWord) w).PassportIdCardNumbers;                     break;
                case NerRuleOutputType.CarNumber           : wi.carNumber             = ((CarNumberWord)            w).CarNumber;                                 break;
                case NerRuleOutputType.HealthInsurance     : wi.healthInsuranceNumber = ((HealthInsuranceWord)      w).HealthInsuranceNumber;                     break;
                case NerRuleOutputType.DriverLicense       : wi.driverLicense         = ((DriverLicenseWord)        w).DriverLicense;                             break;
                case NerRuleOutputType.SocialSecurity      : wi.socialSecurity        = ((SocialSecurityWord)       w).SocialSecurityNumber;                      break;
                case NerRuleOutputType.TaxIdentification   : wi.taxIdentification     = ((TaxIdentificationWord)    w).TaxIdentificationNumber;                   break;
                case NerRuleOutputType.Company             : wi.companyName           = ((CompanyWord)              w).Name;                                      break;
                //case NerRuleOutputType.Url                 : 
                //case NerRuleOutputType.Email               : wi.urlType = ((UrlOrEmailWordBase) w).UrlType.ToString(); break;
                case NerRuleOutputType.Url                 : 
                case NerRuleOutputType.Email               : wi.urlType = w.nerOutputType.ToString(); break;
            }

            return (wi);
        }
        [M(O.AggressiveInlining)] private static InitParamsVM Copy( this in InitParamsVM p ) => p.ReturnInputText.GetValueOrDefault() ? p : new InitParamsVM( p, null );
        //public static ResultVM Create( in InitParamsVM p ) => new ResultVM() { Params = p.Copy() };
        //public static ResultVM Create( in InitParamsVM p, Exception ex ) => new ResultVM() { Params = p.Copy(), ErrorMessage = ex.Message, FullErrorMessage = ex.ToString() };
        public static ResultVM Create( in InitParamsVM p, IReadOnlyList< _NerRules_word_t > words )
        {
            var lst = new List< WordInfo >( words.Count );
            if ( words.AnyEx() )
            {
                var buf = new StringBuilder();

                var returnWordValue = p.ReturnWordValue.GetValueOrDefault( true );
                foreach ( var w in words )
                {
                    if ( w.HasNerPrevWord ) continue;
                    var wi = Create( w, buf );
                    if ( !returnWordValue )
                    {
                        wi.value = null;
                    }
                    lst.Add( wi );
                }
            }
            return (new ResultVM() { Params = p.Copy(), Words = lst });
        }
#if DEBUG
        public static string ToText( this in InitParamsVM p ) => ((p.Text != null) && (250 < p.Text.Length)) ? (p.Text.Substring( 0, 250 ) + "...") : p.Text;
#endif
    }
}
