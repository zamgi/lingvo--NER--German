using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.PhoneNumbers
{
    /// <summary>
    /// 
    /// </summary>
    public enum PhoneNumberTypeEnum
    {
        Telephone,
        Mobile,
        Fax,
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class PhoneNumberWord : word_t
    {
        public PhoneNumberWord( int _startIndex, int _length, string cityAreaName ) 
        {
            startIndex    = _startIndex;
            length        = _length;
            nerInputType  = NerInputType.Num;
            nerOutputType = NerOutputType.PhoneNumber;
            CityAreaName  = cityAreaName;
        }
        public PhoneNumberWord( int _startIndex, int _length, PhoneNumberTypeEnum phoneNumberType ) 
        {
            startIndex      = _startIndex;
            length          = _length;
            nerInputType    = NerInputType.Num;
            nerOutputType   = NerOutputType.PhoneNumber;
            PhoneNumberType = phoneNumberType;
        }

        public string CityAreaName { get; }
        public PhoneNumberTypeEnum PhoneNumberType { get; set; }
#if DEBUG
        public override string ToString() => $"Phone-Number => '{valueOriginal}' ('{valueUpper}'), type: {PhoneNumberType}, city-area: '{CityAreaName}'"; 
#endif
    }
}