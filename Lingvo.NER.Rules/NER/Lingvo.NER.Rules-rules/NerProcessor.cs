using System;
using System.Collections.Generic;
using System.Globalization;

using Lingvo.NER.Rules.Address;
using Lingvo.NER.Rules.BankAccounts;
using Lingvo.NER.Rules.Birthdays;
using Lingvo.NER.Rules.Birthplaces;
using Lingvo.NER.Rules.CarNumbers;
using Lingvo.NER.Rules.Companies;
using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.CreditCards;
using Lingvo.NER.Rules.CustomerNumbers;
using Lingvo.NER.Rules.DriverLicenses;
using Lingvo.NER.Rules.HealthInsurances;
using Lingvo.NER.Rules.MaritalStatuses;
using Lingvo.NER.Rules.Names;
using Lingvo.NER.Rules.Nationalities;
using Lingvo.NER.Rules.NerPostMerging;
#if (!WITHOUT_CRF)
using Lingvo.NER.Rules.NerPostMerging_4_Crf;
#endif
using Lingvo.NER.Rules.PassportIdCardNumbers;
using Lingvo.NER.Rules.PhoneNumbers;
using Lingvo.NER.Rules.SocialSecurities;
using Lingvo.NER.Rules.TaxIdentifications;
using Lingvo.NER.Rules.tokenizing;
using Lingvo.NER.Rules.urls;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;
using URT = Lingvo.NER.Rules.NerProcessor.UsedRecognizerTypeEnum;
using _WordsOrderList_ = System.Collections.Generic.DirectAccessList< (Lingvo.NER.Rules.tokenizing.word_t w, int orderNum) >;

namespace Lingvo.NER.Rules
{
    /// <summary>
    ///
    /// </summary>
    public sealed class NerProcessor : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        [Flags] public enum UsedRecognizerTypeEnum : int
        {
            NO = 0,

            PhoneNumbers          = 1,
            Address               = (1 << 1),
            BankAccounts          = (1 << 2),
            Names                 = (1 << 3),
            Urls                  = (1 << 4),
            Emails                = (1 << 5),
            CustomerNumbers       = (1 << 6),
            Birthdays             = (1 << 7),
            CreditCards           = (1 << 8),
            PassportIdCardNumbers = (1 << 9),
            CarNumbers            = (1 << 10),
            HealthInsurances      = (1 << 11),
            DriverLicenses        = (1 << 12),
            SocialSecurities      = (1 << 13),
            TaxIdentifications    = (1 << 14),
            Nationalities         = (1 << 15),
            Birthplaces           = (1 << 16),
            MaritalStatuses       = (1 << 17),
            Companies             = (1 << 18),
#if (!WITHOUT_CRF)
            Crf                   = (1 << 18),
#endif
            All_Without_Crf = PhoneNumbers | Address | BankAccounts | Names | Emails /*| Urls*/ | CustomerNumbers | Birthdays | 
                              CreditCards | PassportIdCardNumbers | Nationalities | Birthplaces | MaritalStatuses |
                              CarNumbers | HealthInsurances | DriverLicenses | SocialSecurities | TaxIdentifications | Companies,
#if (!WITHOUT_CRF)
            All             = All_Without_Crf | Crf,
#else
            All             = All_Without_Crf,
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public delegate void OuterProcessSentCallbackDelegate( List< word_t > words );

        #region [.private field's.]
        private const int DEFAULT_WORDSLIST_CAPACITY = 1000;
#if (!WITHOUT_CRF)
        private NerScriber _CrfNerScriber;
#endif
        private Tokenizer                             _Tokenizer;
        private List< word_t >                        _NerWords;
        private List< NerUnitedEntity >               _NerUnitedEntities;
        private _WordsOrderList_                      _NerWordsWithOrderNum_Buff;
        private Tokenizer.ProcessSentCallbackDelegate _ProcessSentCallback_Delegate;
        private Tokenizer.ProcessSentCallbackDelegate _ProcessSentCallback_UseSimpleSentsAllocate_Address_Delegate;
        private PhoneNumbersRecognizer                _PhoneNumbersRecognizer;
        private NerProcessorHelper                    _NerProcessorHelper;
        private AddressRecognizer                     _AddressRecognizer;
        private BankAccountsRecognizer                _BankAccountsRecognizer;
        private CustomerNumbersRecognizer             _CustomerNumbersRecognizer;
        private NamesRecognizer                       _NamesRecognizer;
        private MaritalStatusesRecognizer             _MaritalStatusesRecognizer;
        private BirthdaysRecognizer                   _BirthdaysRecognizer;
        private BirthplacesRecognizer                 _BirthplacesRecognizer;
        private NationalitiesRecognizer               _NationalitiesRecognizer;
        private CreditCardsRecognizer                 _CreditCardsRecognizer;
        private PassportIdCardNumbersRecognizer       _PassportIdCardNumbersRecognizer;
        private CarNumbersRecognizer                  _CarNumbersRecognizer;
        private HealthInsurancesRecognizer            _HealthInsurancesRecognizer;
        private DriverLicensesRecognizer              _DriverLicensesRecognizer;
        private SocialSecuritiesRecognizer            _SocialSecuritiesRecognizer;
        private TaxIdentificationsRecognizer          _TaxIdentificationsRecognizer;
        private CompaniesRecognizer                   _CompaniesRecognizer;
        #endregion

        #region [.ctor().]
        public NerProcessor( NerProcessorConfig config, URT urt = URT.All_Without_Crf )
		{
			CheckConfig( config, urt );

            #region [.Crf.]
#if (!WITHOUT_CRF)
            if ( urt.Has_Crf() )
            {
                _CrfNerScriber = NerScriber.Create( config.ModelFilename, config.TemplateFile );
            }
#endif
            #endregion

            _Tokenizer                    = new Tokenizer( config.TokenizerConfig );
            _NerWords                     = new List< word_t >( DEFAULT_WORDSLIST_CAPACITY );            
            _NerUnitedEntities            = new List< NerUnitedEntity >( DEFAULT_WORDSLIST_CAPACITY );
            _NerWordsWithOrderNum_Buff    = new _WordsOrderList_( DEFAULT_WORDSLIST_CAPACITY );
            _NerProcessorHelper           = new NerProcessorHelper( _Tokenizer.InputTypeProcessor );
            _ProcessSentCallback_Delegate = new Tokenizer.ProcessSentCallbackDelegate( ProcessSentCallback );
            _ProcessSentCallback_UseSimpleSentsAllocate_Address_Delegate = new Tokenizer.ProcessSentCallbackDelegate( ProcessSentCallback_UseSimpleSentsAllocate_Address );

            #region [.PhoneNumbers.]
            if ( urt.Has_PhoneNumbers() )
            {
                _PhoneNumbersRecognizer = new PhoneNumbersRecognizer( config.PhoneNumbersModel );
            }
            #endregion

            #region [.Address.]
            if ( urt.Has_Address() )
            {
                _AddressRecognizer = new AddressRecognizer( config.AddressModel );
            }
            #endregion

            #region [.BankAccounts.]
            if ( urt.Has_BankAccounts() )
            {
                _BankAccountsRecognizer = new BankAccountsRecognizer( config.BankAccountsModel );
            }
            #endregion

            #region [.CustomerNumbers.]
            if ( urt.Has_CustomerNumbers() )
            {
                _CustomerNumbersRecognizer = new CustomerNumbersRecognizer();
            }
            #endregion

            #region [.Names.]
            if ( urt.Has_Names() )
            {
                _NamesRecognizer = new NamesRecognizer( config.NamesModel, GetCurrentOriginalText );
            }
            #endregion

            #region [.MaritalStatuses.]
            if ( urt.Has_MaritalStatuses() )
            {
                _MaritalStatusesRecognizer = new MaritalStatusesRecognizer( config.MaritalStatusesModel );
            }
            #endregion

            #region [.Birthdays.]
            if ( urt.Has_Birthdays() )
            {
                _BirthdaysRecognizer = new BirthdaysRecognizer( _NerProcessorHelper.CultureInfoData.Dtfi,
                                                                _NerProcessorHelper.Alt_CultureInfoData.Dtfi );
            }
            #endregion

            #region [.Birthplaces.]
            if ( urt.Has_Birthplaces() )
            {
                _BirthplacesRecognizer = new BirthplacesRecognizer( config.BirthplacesModel );
            }
            #endregion

            #region [.Nationalities.]
            if ( urt.Has_Nationalities() )
            {
                _NationalitiesRecognizer = new NationalitiesRecognizer( config.NationalitiesModel );
            }
            #endregion

            #region [.CreditCards.]
            if ( urt.Has_CreditCards() )
            {
                _CreditCardsRecognizer = new CreditCardsRecognizer();
            }
            #endregion

            #region [.PassportIdCardNumbers.]
            if ( urt.Has_PassportIdCardNumbers() )
            {
                _PassportIdCardNumbersRecognizer = new PassportIdCardNumbersRecognizer( config.PassportIdCardNumbersModel );
            }
            #endregion

            #region [.CarNumbers.]
            if ( urt.Has_CarNumbers() )
            {
                _CarNumbersRecognizer = new CarNumbersRecognizer( config.CarNumbersModel );
            }
            #endregion

            #region [.HealthInsurances.]
            if ( urt.Has_HealthInsurances() )
            {
                _HealthInsurancesRecognizer = new HealthInsurancesRecognizer();
            }
            #endregion

            #region [.DriverLicenses.]
            if ( urt.Has_DriverLicenses() )
            {
                _DriverLicensesRecognizer = new DriverLicensesRecognizer( config.DriverLicensesModel );
            }
            #endregion

            #region [.SocialSecurities.]
            if ( urt.Has_SocialSecurities() )
            {
                _SocialSecuritiesRecognizer = new SocialSecuritiesRecognizer( config.SocialSecuritiesModel );
            }
            #endregion

            #region [.TaxIdentifications.]
            if ( urt.Has_TaxIdentifications() )
            {
                _TaxIdentificationsRecognizer = new TaxIdentificationsRecognizer( config.TaxIdentificationsModel );
            }
            #endregion

            #region [.Companies.]
            if ( urt.Has_Companies() )
            {
                _CompaniesRecognizer = new CompaniesRecognizer( config.CompaniesModel );
            }
            #endregion

            #region [.UsedRecognizerType.]
            UsedRecognizerType = urt;
            #endregion
        }
        private static void CheckConfig( NerProcessorConfig config, URT urt )
		{
			config.ThrowIfNull( nameof(config) );
            config.TokenizerConfig.ThrowIfNull( nameof(config.TokenizerConfig) );

            #region [.Crf.]
#if (!WITHOUT_CRF)
            if ( urt.Has_Crf() )
            {
                config.ModelFilename.ThrowIfNullOrWhiteSpace( nameof(config.ModelFilename) );
                config.TemplateFile.ThrowIfNull( nameof(config.TemplateFile) );
            }
#endif
            #endregion

            #region [.PhoneNumbers.]
            if ( urt.Has_PhoneNumbers() )
            {
                config.PhoneNumbersModel.ThrowIfNull( nameof(config.PhoneNumbersModel) );
            }
            #endregion

            #region [.Address.]
            if ( urt.Has_Address() )
            {
                config.AddressModel.ThrowIfNull( nameof(config.AddressModel) );
            }
            #endregion

            #region [.BankAccounts.]
            if ( urt.Has_BankAccounts() )
            {
                config.BankAccountsModel.ThrowIfNull( nameof(config.BankAccountsModel) );
            }
            #endregion

            #region [.Birthplaces.]
            if ( urt.Has_Birthplaces() )
            {
                config.BirthplacesModel.ThrowIfNull( nameof(config.BirthplacesModel) );
            }
            #endregion

            #region [.Names.]
            if ( urt.Has_Names() )
            {
                config.NamesModel.ThrowIfNull( nameof(config.NamesModel) );
            }
            #endregion

            #region [.MaritalStatuses.]
            if ( urt.Has_MaritalStatuses() )
            {
                config.MaritalStatusesModel.ThrowIfNull( nameof(config.MaritalStatusesModel) );
            }
            #endregion

            #region [.Nationalities.]
            if ( urt.Has_Nationalities() )
            {
                config.NationalitiesModel.ThrowIfNull( nameof(config.NationalitiesModel) );
            }
            #endregion

            #region [.Companies.]
            if ( urt.Has_Companies() )
            {
                config.CompaniesModel.ThrowIfNull( nameof(config.CompaniesModel) );
            }
            #endregion

            #region [.check UsedRecognizerType.]
            if ( urt == URT.NO ) throw (new ArgumentException( nameof(UsedRecognizerTypeEnum) ));
            #endregion
        }

        public void Dispose()
        {
#if (!WITHOUT_CRF)
            _CrfNerScriber?.Dispose();
#endif
            _Tokenizer.Dispose();
        }

        public URT UsedRecognizerType { [M(O.AggressiveInlining)] get; }
#if DEBUG
        public override string ToString() => $"Used recognizers: {UsedRecognizerType}";
#endif
        #endregion

        #region [.for access to Current-Original-Text from 'NamesRecognizer'.]
        private string _CurrentOriginalText;
        private string GetCurrentOriginalText() => _CurrentOriginalText;
        #endregion

        [M(O.AggressiveInlining)] private void Fill_NerWords( List< word_t > words )
        {
            for ( int i = 0, len = words.Count; i < len; i++ )
            {
                var word = words[ i ];
                if ( !word.IsOutputTypeOther() )
                {
                    _NerWords.Add( word );
                }
            }
        }
        [M(O.AggressiveInlining)] private void Fill_NerWordsWithOrderNum_Buff( List< word_t > words )
        {
            //---_NerWordsWithOrderNum_Buff.Clear();

            var prev_word = default(word_t);
            for ( int i = 0, n = 0, len = words.Count; i < len; i++ )
            {
                var word = words[ i ];

                if ( (word.length == 1) && word.IsExtraWordTypePunctuation() )
                {
                    prev_word = word;
                    continue;
                }
                
                if ( (prev_word != null) && (prev_word.endIndex() != word.startIndex) )
                {
                    n++;
                }

                if ( !word.IsOutputTypeOther() )
                {
                    _NerWordsWithOrderNum_Buff.AddByRef( (word, n) );
                }

                prev_word = word;
            }
        }
        [M(O.AggressiveInlining)] private void Fill_NerWords_From_NerWordsWithOrderNum_Buff()
        {
            for ( int i = 0, len = _NerWordsWithOrderNum_Buff.Count; i < len; i++ )
            {
                ref readonly var t = ref _NerWordsWithOrderNum_Buff._Items[ i ];
                _NerWords.Add( t.w );
            }

            _NerWordsWithOrderNum_Buff.Clear();
        }
        [M(O.AggressiveInlining)] private void PostProcess( List< word_t > words )
        {
            #region [.Address #1.]
            if ( UsedRecognizerType.Has_Address() )
            {
                _AddressRecognizer.Recognize_FullAddress( words );
                //_AddressRecognizer.Recognize_CityZipCodeStreetOnly    ( words );
                //_AddressRecognizer.Recognize_CityOnlyStreetHouseNumber( words );
                //_AddressRecognizer.Recognize_CityZipCode              ( words );
            }
            #endregion

            //---_NerProcessorHelper.UnstuckNumbersFromOthers__OLD( words );
            //---Task.WaitAny( Task.Run( () => _NerProcessorHelper.UnstuckNumbersFromOthers__OLD( words ) ), Task.Delay( 30_000 ) );
            _NerProcessorHelper.UnstuckNumbersFromOthers__NEW( words );

            #region [.Address #2.] 
            if ( UsedRecognizerType.Has_Address() )
            {
                // "Ramsbachstr.3 88069 Tettnang"
                _AddressRecognizer.Recognize_FullAddress( words );
                _AddressRecognizer.Recognize_CityZipCodeStreetOnly( words );
                _AddressRecognizer.Recognize_CityOnlyStreetHouseNumber( words );
                _AddressRecognizer.Recognize_CityZipCode( words );
            }
            #endregion

            #region [.PhoneNumbers.]
            if ( UsedRecognizerType.Has_PhoneNumbers() )
            {
                //---_NerProcessorHelper.UnstuckNumbersFromOthers( words );
                _PhoneNumbersRecognizer.Run( words );
            }
            #endregion

            #region [.BankAccounts.]
            if ( UsedRecognizerType.Has_BankAccounts() )
            {
                _BankAccountsRecognizer.Run( words );
            }
            #endregion

            #region [.CustomerNumbers.]
            if ( UsedRecognizerType.Has_CustomerNumbers() )
            {
                _CustomerNumbersRecognizer.Run( words );
            }
            #endregion

            #region [.Birthdays.]
            if ( UsedRecognizerType.Has_Birthdays() )
            {
                _BirthdaysRecognizer.Run( words );
            }
            #endregion

            #region [.Birthplaces.]
            if ( UsedRecognizerType.Has_Birthplaces() )
            {
                _BirthplacesRecognizer.Run( words );
            }
            #endregion

            #region [.MaritalStatuses.]
            if ( UsedRecognizerType.Has_MaritalStatuses() )
            {
                _MaritalStatusesRecognizer.Run( words );
            }
            #endregion

            #region [.Nationalities.]
            if ( UsedRecognizerType.Has_Nationalities() )
            {
                _NationalitiesRecognizer.Run( words );
            }
            #endregion

            #region [.CreditCards.]
            if ( UsedRecognizerType.Has_CreditCards() )
            {
                _CreditCardsRecognizer.Run( words );
            }
            #endregion

            #region [.PassportIdCardNumbers.]
            if ( UsedRecognizerType.Has_PassportIdCardNumbers() )
            {
                _PassportIdCardNumbersRecognizer.Run( words );
            }
            #endregion

            #region [.CarNumbers.]
            if ( UsedRecognizerType.Has_CarNumbers() )
            {
                _CarNumbersRecognizer.Run( words );
            }
            #endregion

            #region [.HealthInsurances.]
            if ( UsedRecognizerType.Has_HealthInsurances() )
            {
                _HealthInsurancesRecognizer.Run( words );
            }
            #endregion

            #region [.DriverLicenses.]
            if ( UsedRecognizerType.Has_DriverLicenses() )
            {
                _DriverLicensesRecognizer.Run( words );
            }
            #endregion

            #region [.SocialSecurities.]
            if ( UsedRecognizerType.Has_SocialSecurities() )
            {
                _SocialSecuritiesRecognizer.Run( words );
            }
            #endregion

            #region [.TaxIdentifications.]
            if ( UsedRecognizerType.Has_TaxIdentifications() )
            {
                _TaxIdentificationsRecognizer.Run( words );
            }
            #endregion

            #region [.Companies.]
            if ( UsedRecognizerType.Has_Companies() )
            {
                _CompaniesRecognizer.Run( words );
            }
            #endregion

            #region [.Names.]
            if ( UsedRecognizerType.Has_Names() )
            {
                _NamesRecognizer.Recognize( words );

                if ( _NerProcessorHelper.TrimEndsPunctuationsFromOthers( words ) )
                {
                    _NamesRecognizer.Recognize( words );
                }
            }
            #endregion
        }

        /*public List< word_t > Run_v1( string text )
        {
            _NerWords.Clear();

            _Tokenizer.Run( text, _ProcessSentCallback_Delegate );

            return (_NerWords);
        }*/
        public List< word_t > Run_UseSimpleSentsAllocate_v1( string text )
        {
            _NerWords.Clear();
            _CurrentOriginalText = text;

            _Tokenizer.Run_UseSimpleSentsAllocate( text, _ProcessSentCallback_Delegate );
            
            _CurrentOriginalText = null;
            return (_NerWords);
        }

        /*public (List< word_t > nerWords, List< NerUnitedEntity_v2 > nerUnitedEntities) Run_v2( string text )
        {
            _NerWords.Clear(); _NerUnitedEntities.Clear();

            _Tokenizer.Run( text, _ProcessSentCallback_Delegate );

            return (_NerWords, _NerUnitedEntities);
        }*/
        public (List< word_t > nerWords, List< NerUnitedEntity > nerUnitedEntities, int relevanceRanking) Run_UseSimpleSentsAllocate_v2( string text )
        {
            _NerWords.Clear(); _NerUnitedEntities.Clear();
            _CurrentOriginalText = text;

            _Tokenizer.Run_UseSimpleSentsAllocate( text, _ProcessSentCallback_Delegate );

            var relevanceRanking = CalcRelevanceRanking( _NerWords );
            _CurrentOriginalText = null;
            return (_NerWords, _NerUnitedEntities, relevanceRanking);
        }

        private void ProcessSentCallback( List< word_t > words, List< url_t > urls )
        {
            if ( 0 < words.Count )
            {
                #region [.Crf.]
#if (!WITHOUT_CRF)
                if ( UsedRecognizerType.Has_Crf() )
                {
                    _CrfNerScriber.Run( words );
                    NerPostMerger_4_Crf.Run( words );
                }
#endif
                #endregion

                //---if ( UsedRecognizerType.Has_UrlsOrEmails() ) UrlAndEmailMerger.MergeWithUrls( words, urls, UsedRecognizerType );
                UrlAndEmailMerger.MergeWithUrls( words, urls, UsedRecognizerType | URT.Urls /*url can be part of company-names*/ );

                PostProcess( words );
                Fill_NerWordsWithOrderNum_Buff( words ); //---Fill_NerWords( words );

                UrlAndEmailMerger.RemoveUrlsIfNotNeed4Recognizer( words, UsedRecognizerType ); /*url can be part of company-names*/

                if ( 0 < _NerWordsWithOrderNum_Buff.Count )
                {
                    NerPostMerger.Run( _NerWordsWithOrderNum_Buff, _NerUnitedEntities );
                    Fill_NerWords_From_NerWordsWithOrderNum_Buff();
                }
            }
            else if ( UsedRecognizerType.Has_UrlsOrEmails() && urls.AnyEx() )
            {
                UrlAndEmailMerger.MergeWithUrls( words, urls, UsedRecognizerType );
                Fill_NerWords( words );
            }
        }

        [M(O.AggressiveInlining)] private static int GetDifferentValues4PersonalDataCategories( NerOutputType nerOutputType )
        {
            switch ( nerOutputType )
            {
                case NerOutputType.Name                : return (0);
                case NerOutputType.Address             : return (1);
                case NerOutputType.PhoneNumber         : return (1);
                case NerOutputType.Email               : return (1);
                case NerOutputType.AccountNumber       : return (30);
                case NerOutputType.CreditCard          : return (30);
                case NerOutputType.Birthday            : return (0);
                case NerOutputType.Birthplace          : return (1);
                case NerOutputType.MaritalStatus       : return (1);
                case NerOutputType.Nationality         : return (1);
                case NerOutputType.PassportIdCardNumber: return (20);
                case NerOutputType.CarNumber           : return (20);
                case NerOutputType.HealthInsurance     : return (30);
                case NerOutputType.DriverLicense       : return (20);
                case NerOutputType.SocialSecurity      : return (20);
                case NerOutputType.TaxIdentification   : return (20);
                default                                : return (0);
            }
        }
        [M(O.AggressiveInlining)] private static int GetFocusOnOneCategory( NerOutputType nerOutputType, int percentageOfTypes ) =>
            nerOutputType switch
            {
                NerOutputType.Birthday =>
                    percentageOfTypes switch
                    {
                        (<= 50) => 0,
                        (> 50) and (<= 74) => -2,
                        (>= 75) and (<= 89) => -5,
                        (>= 90) => -10,
                    },
                var x when
                    x == NerOutputType.Name ||
                    x == NerOutputType.MaritalStatus ||
                    x == NerOutputType.Address ||
                    x == NerOutputType.Birthplace ||
                    x == NerOutputType.PhoneNumber ||
                    x == NerOutputType.Nationality ||
                    x == NerOutputType.Email =>
                        percentageOfTypes switch
                        {
                            (<= 50) => 0,
                            (> 50) and (<= 74) => -1,
                            (>= 75) and (<= 89) => -2,
                            (>= 90) => -3,
                        },
                var y when
                    y == NerOutputType.AccountNumber ||
                    y == NerOutputType.CreditCard ||
                    y == NerOutputType.PassportIdCardNumber ||
                    y == NerOutputType.CarNumber ||
                    y == NerOutputType.HealthInsurance ||
                    y == NerOutputType.DriverLicense ||
                    y == NerOutputType.SocialSecurity ||
                    y == NerOutputType.TaxIdentification =>
                        percentageOfTypes switch
                        {
                            (<= 50) => 1,
                            (> 50) and (<= 74) => 2,
                            (>= 75) and (<= 89) => 3,
                            (>= 90) => 5,
                        },
                _ => 0,
            };

        private Dictionary< NerOutputType, int > _NerOutputTypeCountDict = new Dictionary< NerOutputType, int >();
        private int CalcRelevanceRanking( List< word_t > nerWords )
        {
            if ( nerWords.AnyEx() )
            {
                //1. Number of Personal Data Categories: No. of different Personal Data categories found Points: +10 for each category (max. 130 points per document = 13 categories x 10 points)
                var numberOfPersonalDataCategories = 0;

                //2. Amount of Personal Data: No. of Personal Data found, no matter which type and percentage Points: +2 for each Personal Data
                var amountOfPersonalData = (nerWords.Count << 1);

                //3. Different Values for Personal Data Categories: Each type should have a certain value of points awarded independently from the number of categories, each type receives an additional value as follows:
                var differentValues4PersonalDataCategories = 0;

                var nerWordsCount = nerWords.Count;
                for ( var i = 0; i < nerWordsCount; i++ )
                {
                    var word = nerWords[ i ];

                    differentValues4PersonalDataCategories += GetDifferentValues4PersonalDataCategories( word.nerOutputType );

                    if ( !_NerOutputTypeCountDict.TryGetValue( word.nerOutputType, out var count ) )
                    {
                        _NerOutputTypeCountDict.Add( word.nerOutputType, 1 );
                        numberOfPersonalDataCategories += 10;
                    }
                    else
                    {
                        _NerOutputTypeCountDict[ word.nerOutputType ] = count + 1;
                    }
                }

                //4. Focus on One Category: if a document contains more than a certain percentage of a specific type/category, relevance should be adapted accordingly:
                var focusOnCategories = 0;
                foreach ( var p in _NerOutputTypeCountDict )
                {
                    var nerOutputType = p.Key;
                    var count         = p.Value;
                    var percentageOfTypes = (100 * count) / nerWordsCount;
                    focusOnCategories += count * GetFocusOnOneCategory( nerOutputType, percentageOfTypes );
                }
                _NerOutputTypeCountDict.Clear();

                var relevanceRanking = numberOfPersonalDataCategories + amountOfPersonalData + differentValues4PersonalDataCategories + focusOnCategories;
                return (relevanceRanking);
            }
            return (0);
        }


        public List< word_t[] > Run_Debug( string text )
        {
            var wordsBySents = new List< word_t[] >();
            _CurrentOriginalText = text;

            _Tokenizer.Run( text, (words, urls) =>
            {
                #region [.Crf.]
#if (!WITHOUT_CRF)
                if ( UsedRecognizerType.Has_Crf() )
                {
                    _CrfNerScriber.Run( words );
                    //NerPostMerger_4_Crf.Run( words );
                }
#endif
                #endregion

                UrlAndEmailMerger.MergeWithUrls( words, urls, UsedRecognizerType | URT.Urls /*url can be part of company-names*/ );

                PostProcess( words );

                UrlAndEmailMerger.RemoveUrlsIfNotNeed4Recognizer( words, UsedRecognizerType ); /*url can be part of company-names*/

                ////if ( UsedRecognizerType.Has_UrlsOrEmails() ) UrlAndEmailMerger.MergeWithUrls( words, urls, UsedRecognizerType );

                wordsBySents.Add( words.ToArray() );
            });

            _CurrentOriginalText = null;
            return (wordsBySents);
        }
        public List< word_t[] > Run_Debug_UseSimpleSentsAllocate_Raw( string text )
        {
            var wordsBySents = new List< word_t[] >();
            _CurrentOriginalText = text;

            _Tokenizer.Run_UseSimpleSentsAllocate( text, (words, urls) =>
            {
                //if ( UsedRecognizerType.Has_Crf() )
                //{
                //    _NerScriber.Run( words );                    
                //}
                //PostProcess( words );

                UrlAndEmailMerger.MergeWithUrls( words, urls, UsedRecognizerType );

                wordsBySents.Add( words.ToArray() );
            });

            _CurrentOriginalText = null;
            return (wordsBySents);
        }


        public List< word_t > Run_UseSimpleSentsAllocate_Address( string text )
        {
            _NerWords.Clear();
            _CurrentOriginalText = text;

            _Tokenizer.Run_UseSimpleSentsAllocate( text, _ProcessSentCallback_UseSimpleSentsAllocate_Address_Delegate );

            _CurrentOriginalText = null;
            return (_NerWords);
        }
        private void ProcessSentCallback_UseSimpleSentsAllocate_Address( List< word_t > words, List< url_t > urls )
        {
            if ( 0 < words.Count )
            {
                //if ( UsedRecognizerType.Has_Crf() )
                //{
                //    _NerScriber .Run( words );
                //    NerPostMerge.Run( words );
                //}

                #region [.Address #1.]
                //PostProcess( words );
                _AddressRecognizer.Recognize_FullAddress( words );
                //_AddressRecognizer.Recognize_CityZipCodeStreetOnly    ( words );
                //_AddressRecognizer.Recognize_CityOnlyStreetHouseNumber( words );
                //_AddressRecognizer.Recognize_CityZipCode              ( words );
                //_AddressRecognizer.Recognize_StreetHouseNumber        ( words );
                //_AddressRecognizer.Recognize_CityOnly                 ( words );
                //_AddressRecognizer.Recognize_StreetOnly               ( words );
                #endregion

                _NerProcessorHelper.UnstuckNumbersFromOthers__NEW( words );

                #region [.Address #2.]
                //PostProcess( words );
                _AddressRecognizer.Recognize_FullAddress( words );
                _AddressRecognizer.Recognize_CityZipCodeStreetOnly( words );
                _AddressRecognizer.Recognize_CityOnlyStreetHouseNumber( words );
                _AddressRecognizer.Recognize_CityZipCode( words );
                _AddressRecognizer.Recognize_StreetHouseNumber( words );
                _AddressRecognizer.Recognize_CityOnly( words );
                _AddressRecognizer.Recognize_StreetOnly( words );
                #endregion

                Fill_NerWords( words );
            }

            UrlAndEmailMerger.MergeWithUrls( _NerWords, urls, UsedRecognizerType );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal sealed class NerProcessorHelper
    {
        private static CharType* _CTM;
        static NerProcessorHelper() => _CTM = xlat_Unsafe.Inst._CHARTYPE_MAP;

        private INerInputTypeProcessor _InputTypeProcessor;
        private CultureInfoData        _CultureInfoData;
        private CultureInfoData        _Alt_CultureInfoData;
        private List<word_t> _UnstuckNumbersWordsBuff;
        public NerProcessorHelper( INerInputTypeProcessor inputTypeProcessor )
        {
            _InputTypeProcessor = inputTypeProcessor;

            _CultureInfoData     = new CultureInfoData( CultureInfo.GetCultureInfo( "de" ) );
            _Alt_CultureInfoData = new CultureInfoData( CultureInfo.GetCultureInfo( "en" ) );

            _UnstuckNumbersWordsBuff = new List< word_t >();
        }

        public ref readonly CultureInfoData CultureInfoData => ref _CultureInfoData;
        public ref readonly CultureInfoData Alt_CultureInfoData => ref _Alt_CultureInfoData;

        /*public void UnstuckNumbersFromOthers__OLD( List< word_t > words, bool force = true )
        {
            const int MAX_WORDS_COUNT = 500_000; //250_000; // 
            if ( MAX_WORDS_COUNT < words.Count ) return;

            for ( var i = words.Count - 1; 0 <= i; i-- )
            {
                var w = words[ i ];

                #region [.skip not 'Ner-Other'.]
                if ( !w.IsOutputTypeOther() )
                {
                    continue;
                }
                #endregion

                #region [.skip not 'Num' & not-contains-punctuation.]
                if ( !w.IsInputTypeNum() && !StringsHelper.ContainsPunctuation( w.valueOriginal ) )
                {
                    continue;
                }
                #endregion

                var w_i = i;
                var w_len = w.valueOriginal.Length;
                var is_modified = false;
                for ( var startIndex = 0; startIndex < w_len; startIndex++ )
                {                    
                    var ct = _CTM[ w.valueOriginal[ startIndex ] ];

                    #region [.find word border.]
                    var length = 1;
                    var startsFromDigit = ct.IsDigit();
                    if ( startsFromDigit )
                    {//begin num-word
                        if ( force )
                        {
                            for ( var j = startIndex + 1; j < w_len; j++ )
                            {
                                if ( !_CTM[ w.valueOriginal[ j ] ].IsDigit() )
                                {
                                    break;
                                }
                                length++;
                            }
                        }
                        else
                        {
                            var format_separator_count = 0;
                            for ( var j = startIndex + 1; j < w_len; j++ )
                            {
                                var c = w.valueOriginal[ j ];
                                if ( !_CTM[ c ].IsDigit() )
                                {
                                    if ( _CultureInfoData.IsFormatSeparator( c ) || _Alt_CultureInfoData.IsFormatSeparator( c ) )
                                    //---if ( xlat.IsDot( c ) || xlat.IsComma( c ) )
                                    {
                                        format_separator_count++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                length++;
                            }
                            if ( (1 < format_separator_count) && !is_modified )
                            {
                                break;
                            }
                        }
                    }
                    else
                    {//begin not-num-word
                        for ( var j = startIndex + 1; j < w_len; j++ )
                        {
                            if ( _CTM[ w.valueOriginal[ j ] ].IsDigit() )
                            {
                                break;
                            }
                            length++;
                        }
                    }
                    #endregion

                    #region [.alloc separate word.]
                    var nw = new word_t()
                    {
                        valueOriginal = w.valueOriginal.Substring( startIndex, length ),
                        valueUpper    = w.valueUpper   .Substring( startIndex, length ),
                        startIndex    = w.startIndex + startIndex,
                        length        = length,
                    };
                    #region [.InputTypeProcessor.]
                    if ( startsFromDigit && force )
                    {
                        (nw.nerInputType, nw.extraWordType) = (NerInputType.Num, ExtraWordType.IntegerNumber);
                    }
                    else
                    {
                        (nw.nerInputType, nw.extraWordType) = _InputTypeProcessor.GetNerInputType( nw );
                    }
                    #endregion
                    #region [.add word & move to next part of word.]
                    if ( w_i == i )
                    {
                        words[ i ] = nw; //replace 
                    }
                    else
                    {
                        words.Insert( w_i, nw ); //insert
                    }
                    w_i++;
                    startIndex += length - 1;
                    is_modified = true;
                    #endregion
                    #endregion
                }
            }
        }*/
        
        public void UnstuckNumbersFromOthers__NEW( List< word_t > words, bool force = true )
        {
            for ( int i = 0, len = words.Count; i < len; i++ )
            {
                var w = words[ i ];

                #region [.skip not 'Ner-Other'.]
                if ( !w.IsOutputTypeOther() )
                {
                    _UnstuckNumbersWordsBuff.Add( w );
                    continue;
                }
                #endregion

                #region [.skip not 'Num' & not-contains-punctuation.]
                if ( !w.IsInputTypeNum() && !StringsHelper.ContainsPunctuation( w.valueOriginal ) )
                {
                    _UnstuckNumbersWordsBuff.Add( w );
                    continue;
                }
                #endregion

                //var w_i = i;
                var w_len = w.valueOriginal.Length;
                var is_modified = false;
                for ( var startIndex = 0; startIndex < w_len; startIndex++ )
                {
                    var ct = _CTM[ w.valueOriginal[ startIndex ] ];

                    #region [.find word border.]
                    var length = 1;
                    var startsFromDigit = ct.IsDigit();
                    if ( startsFromDigit )
                    {//begin num-word
                        if ( force )
                        {
                            for ( var j = startIndex + 1; j < w_len; j++ )
                            {
                                if ( !_CTM[ w.valueOriginal[ j ] ].IsDigit() )
                                {
                                    break;
                                }
                                length++;
                            }
                        }
                        else
                        {
                            var format_separator_count = 0;
                            for ( var j = startIndex + 1; j < w_len; j++ )
                            {
                                var c = w.valueOriginal[ j ];
                                if ( !_CTM[ c ].IsDigit() )
                                {
                                    if ( _CultureInfoData.IsFormatSeparator( c ) || _Alt_CultureInfoData.IsFormatSeparator( c ) )
                                    //---if ( xlat.IsDot( c ) || xlat.IsComma( c ) )
                                    {
                                        format_separator_count++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                length++;
                            }
                            if ( (1 < format_separator_count) && !is_modified )
                            {
                                break;
                            }
                        }
                    }
                    else
                    {//begin not-num-word
                        for ( var j = startIndex + 1; j < w_len; j++ )
                        {
                            if ( _CTM[ w.valueOriginal[ j ] ].IsDigit() )
                            {
                                break;
                            }
                            length++;
                        }
                    }
                    #endregion

                    #region [.alloc separate word.]
                    var nw = new word_t()
                    {
                        valueOriginal = w.valueOriginal.Substring( startIndex, length ),
                        valueUpper = w.valueUpper.Substring( startIndex, length ),
                        startIndex = w.startIndex + startIndex,
                        length = length,
                    };
                    #region [.InputTypeProcessor.]
                    if ( startsFromDigit && force )
                    {
                        (nw.nerInputType, nw.extraWordType) = (NerInputType.Num, ExtraWordType.IntegerNumber);
                    }
                    else
                    {
                        (nw.nerInputType, nw.extraWordType) = _InputTypeProcessor.GetNerInputType( nw );
                    }
                    #endregion
                    #region [.add word & move to next part of word.]
                    _UnstuckNumbersWordsBuff.Add( nw );
                    //if ( w_i == i )
                    //{
                    //    _UnstuckNumbersWordsBuff.Add( nw ); //replace 
                    //    //---words[ i ] = nw; //replace 
                    //}
                    //else
                    //{
                    //    _UnstuckNumbersWordsBuff.Insert( w_i, nw ); //insert
                    //    //---words.Insert( w_i, nw ); //insert
                    //}
                    //w_i++;
                    startIndex += length - 1;
                    is_modified = true;
                    #endregion
                    #endregion
                }
            }

            words.Clear();
            words.AddRange( _UnstuckNumbersWordsBuff );
            _UnstuckNumbersWordsBuff.Clear();
        }

        [M(O.AggressiveInlining)] private static bool TrimEndsPunctuations( string s, out string new_s )
        {
            if ( s != null )
            {
                var idx = s.Length - 1;
                if ( (1 < idx) && _CTM[ s[ idx ] ].IsPunctuation() )
                {
                    for ( idx--; 1 < idx; idx-- )
                    {
                        if ( !_CTM[ s[ idx ] ].IsPunctuation() )
                        {
                            new_s = s.Substring( 0, idx + 1 );
                            return (true);
                        }
                    }
                }
            }
            new_s = default;
            return (false);
        }
        public bool TrimEndsPunctuationsFromOthers( List< word_t > words )
        {
            var is_modified = false;
            for ( int i = 0, len = words.Count; i < len; i++ )
            {
                var w = words[ i ];

                #region [.skip not 'Ner-Other'.]
                if ( !w.IsOutputTypeOther() )
                {
                    continue;
                }
                #endregion

                if ( TrimEndsPunctuations( w.valueOriginal, out var s ) )
                {
                    is_modified = true;
                    w.valueOriginal = s;
                    w.length        = s.Length;

                    if ( TrimEndsPunctuations( w.valueUpper, out s ) )
                    {
                        w.valueUpper = s;
                    }

                    //if ( TrimEndsPunctuations( w.valueOriginal__UmlautesNormalized, out s ) )
                    //{
                    //    w.valueOriginal__UmlautesNormalized = s;
                    //}
                    if ( TrimEndsPunctuations( w.valueUpper__UmlautesNormalized, out s ) )
                    {
                        w.valueUpper__UmlautesNormalized = s;
                    }
                }
            }
            return (is_modified);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class NerProcessorExtensions
    {
        [M(O.AggressiveInlining)] public static bool Has_PhoneNumbers( this URT urt ) => ((urt & URT.PhoneNumbers) == URT.PhoneNumbers);
        [M(O.AggressiveInlining)] public static bool Has_Address( this URT urt ) => ((urt & URT.Address) == URT.Address);
        [M(O.AggressiveInlining)] public static bool Has_BankAccounts( this URT urt ) => ((urt & URT.BankAccounts) == URT.BankAccounts);
        [M(O.AggressiveInlining)] public static bool Has_CustomerNumbers(this URT urt ) => ((urt & URT.CustomerNumbers) == URT.CustomerNumbers);
        [M(O.AggressiveInlining)] public static bool Has_Birthdays( this URT urt ) => ((urt & URT.Birthdays) == URT.Birthdays);
        [M(O.AggressiveInlining)] public static bool Has_Birthplaces(this URT urt) => ((urt & URT.Birthplaces) == URT.Birthplaces);
        [M(O.AggressiveInlining)] public static bool Has_CreditCards( this URT urt ) => ((urt & URT.CreditCards) == URT.CreditCards);
        [M(O.AggressiveInlining)] public static bool Has_PassportIdCardNumbers( this URT urt ) => ((urt & URT.PassportIdCardNumbers) == URT.PassportIdCardNumbers);
        [M(O.AggressiveInlining)] public static bool Has_CarNumbers( this URT urt ) => ((urt & URT.CarNumbers) == URT.CarNumbers);
        [M(O.AggressiveInlining)] public static bool Has_HealthInsurances( this URT urt ) => ((urt & URT.HealthInsurances) == URT.HealthInsurances);
        [M(O.AggressiveInlining)] public static bool Has_DriverLicenses( this URT urt ) => ((urt & URT.DriverLicenses) == URT.DriverLicenses);
        [M(O.AggressiveInlining)] public static bool Has_SocialSecurities( this URT urt ) => ((urt & URT.SocialSecurities) == URT.SocialSecurities);
        [M(O.AggressiveInlining)] public static bool Has_TaxIdentifications( this URT urt ) => ((urt & URT.TaxIdentifications) == URT.TaxIdentifications);        
        [M(O.AggressiveInlining)] public static bool Has_Names( this URT urt ) => ((urt & URT.Names) == URT.Names);
        [M(O.AggressiveInlining)] public static bool Has_MaritalStatuses( this URT urt ) => ((urt & URT.MaritalStatuses) == URT.MaritalStatuses);
        [M(O.AggressiveInlining)] public static bool Has_Nationalities( this URT urt ) => ((urt & URT.Nationalities) == URT.Nationalities);
        [M(O.AggressiveInlining)] public static bool Has_UrlsOrEmails( this URT urt ) => ((urt & URT.Urls) == URT.Urls) || ((urt & URT.Emails) == URT.Emails);
        [M(O.AggressiveInlining)] public static bool Has_Urls( this URT urt ) => ((urt & URT.Urls) == URT.Urls);
        [M(O.AggressiveInlining)] public static bool Has_Emails( this URT urt ) => ((urt & URT.Emails) == URT.Emails);
        [M(O.AggressiveInlining)] public static bool Has_Companies( this URT urt ) => ((urt & URT.Companies) == URT.Companies);
#if (!WITHOUT_CRF)
        [M(O.AggressiveInlining)] public static bool Has_Crf( this URT urt ) => ((urt & URT.Crf) == URT.Crf);
#endif
    }
}
