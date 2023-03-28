using System;
using System.Collections.Generic;

using JP = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Lingvo.NER.Combined.WebService
{
    /// <summary>
    /// 
    /// </summary>
    public readonly struct InitParamsVM
    {
        public InitParamsVM( string text ) : this() => Text = text;
        public InitParamsVM( in InitParamsVM o, string text )
        {
            ModelType            = o.ModelType;
            MaxPredictSentLength = o.MaxPredictSentLength;
            ReturnInputText      = o.ReturnInputText;
            ReturnUnitedEntities = o.ReturnUnitedEntities;
            ReturnWordValue      = o.ReturnWordValue;
            Text                 = text;
        }
        public string Text                 { get; init; }
        public string ModelType            { get; init; }
        public int?   MaxPredictSentLength { get; init; }

        public bool? ReturnInputText      { get; init; }
        public bool? ReturnUnitedEntities { get; init; }
        public bool? ReturnWordValue      { get; init; }
    }

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

            public /*UrlTypeEnum?*/string urlType { get; set; }

            public string accountNumber { get; set; }
            public string accountOwner  { get; set; }
            public string bankCode      { get; set; }
            public string bankName      { get; set; }
            public /*BankAccountTypeEnum?*/string bankAccountType { get; set; }
            public string customerNumber { get; set; }

            public string firstName { get; set; }
            public string surName   { get; set; }
            public /*TextPreambleTypeEnum?*/string nameType { get; set; }

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

        [JP("initParams")      ] public InitParamsVM      Params           { get; init; }
        [JP("errorMessage")    ] public string            ErrorMessage     { get; init; }
        [JP("fullErrorMessage")] public string            FullErrorMessage { get; init; }
        [JP("words")           ] public IList< WordInfo > Words            { get; init; }
#if DEBUG
        public override string ToString() => $"count: {(Words?.Count).GetValueOrDefault()}";
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public readonly struct ErrorVM
    {
        public ErrorVM( Exception ex )
        {
            ErrorMessage     = ex?.Message;
            FullErrorMessage = ex?.ToString();
        }
        public string ErrorMessage     { get; init; }
        public string FullErrorMessage { get; init; }
        public override string ToString() => ErrorMessage;
    }
}
