using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.CreditCards
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CreditCardWord : word_t
    {
        public CreditCardWord( int _startIndex, string creditCardNumber )
        {
            startIndex       = _startIndex;
            nerInputType     = NerInputType.Num;
            nerOutputType    = NerOutputType.CreditCard;
            CreditCardNumber = creditCardNumber;
        }
        public string CreditCardNumber { get; }
#if DEBUG
        public override string ToString() => $"CREDIT-CARD: '{CreditCardNumber}'"; 
#endif
    }
}