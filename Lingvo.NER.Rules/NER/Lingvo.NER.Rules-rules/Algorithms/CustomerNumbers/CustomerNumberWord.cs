using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.CustomerNumbers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CustomerNumberWord : word_t
    {
        public CustomerNumberWord( int _startIndex )
        {
            startIndex    = _startIndex;
            nerInputType  = NerInputType.NumCapital;
            nerOutputType = NerOutputType.CustomerNumber;
        }
        public string CustomerNumber;
#if DEBUG
        public override string ToString() => $"CUSTOMER-NUMBER: '{CustomerNumber}'";
#endif
    }
}