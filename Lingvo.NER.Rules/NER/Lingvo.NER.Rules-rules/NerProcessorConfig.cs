using System;
using System.IO;

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
using Lingvo.NER.Rules.urls;
#if (!WITHOUT_CRF)
using Lingvo.NER.Rules.crfsuite;
#endif

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class NerProcessorConfigBase
    {
#if (!WITHOUT_CRF)
        public CRFTemplateFile TemplateFile { get; set; }
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class NerProcessorConfig : NerProcessorConfigBase, IDisposable
    {
        public NerProcessorConfig( string sentSplitterResourcesXmlFilename, string urlDetectorResourcesXmlFilename )
        {
            TokenizerConfig = new TokenizerConfig( sentSplitterResourcesXmlFilename, urlDetectorResourcesXmlFilename )
            {
                NerInputTypeProcessorFactory = NerInputTypeProcessorFactory_En.Inst,
            };
        }
        public NerProcessorConfig( StreamReader sentSplitterResourcesXmlStreamReader, StreamReader urlDetectorResourcesXmlStreamReader )
        {
            TokenizerConfig = new TokenizerConfig( sentSplitterResourcesXmlStreamReader, urlDetectorResourcesXmlStreamReader )
            {
                NerInputTypeProcessorFactory = NerInputTypeProcessorFactory_En.Inst,
            };
        }
        public void Dispose() => TokenizerConfig.Dispose();

        public TokenizerConfig TokenizerConfig { get; }
#if (!WITHOUT_CRF)
        public string ModelFilename { get; set; }
#endif
        public IPhoneNumbersModel          PhoneNumbersModel          { get; set; }
        public IAddressModel               AddressModel               { get; set; }
        public IBankAccountsModel          BankAccountsModel          { get; set; }
        public IBirthplacesModel           BirthplacesModel           { get; set; }
        public INamesModel                 NamesModel                 { get; set; }
        public IMaritalStatusesModel       MaritalStatusesModel       { get; set; }
        public INationalitiesModel         NationalitiesModel         { get; set; }
        public ICarNumbersModel            CarNumbersModel            { get; set; }
        public IPassportIdCardNumbersModel PassportIdCardNumbersModel { get; set; }
        public IDriverLicensesModel        DriverLicensesModel        { get; set; }
        public ISocialSecuritiesModel      SocialSecuritiesModel      { get; set; }
        public ITaxIdentificationsModel    TaxIdentificationsModel    { get; set; }
        public ICompaniesModel             CompaniesModel             { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class NerModelBuilderConfig : NerProcessorConfigBase
    {
        public NerModelBuilderConfig( UrlDetectorConfig urlDetectorConfig )
        {
            TokenizerConfig4NerModelBuilder = new TokenizerConfig4NerModelBuilder()
            {
                UrlDetectorConfig            = urlDetectorConfig,
                NerInputTypeProcessorFactory = NerInputTypeProcessorFactory_En.Inst,
            };
        }

        public TokenizerConfig4NerModelBuilder TokenizerConfig4NerModelBuilder { get; }
        public bool                            IgnoreXmlError                  { get; set; }
    }
}
