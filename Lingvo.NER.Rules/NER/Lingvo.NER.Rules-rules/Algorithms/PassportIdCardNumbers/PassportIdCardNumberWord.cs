using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.PassportIdCardNumbers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class PassportIdCardNumberWord : word_t
    {
        public PassportIdCardNumberWord( int _startIndex, string passportIdCardNumber ) 
        {
            startIndex            = _startIndex;
            nerInputType          = NerInputType.NumCapital;
            nerOutputType         = NerOutputType.PassportIdCardNumber;
            PassportIdCardNumbers = passportIdCardNumber;
        }
        public string PassportIdCardNumbers { get; }
#if DEBUG
        public override string ToString() => $"PASSPORT-ID_CARD-NUMBER: '{PassportIdCardNumbers}'";
#endif
    }
}