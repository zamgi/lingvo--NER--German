using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.MaritalStatuses
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MaritalStatusWord : word_t
    {
        public MaritalStatusWord( int _startIndex, string maritalStatus ) 
        {
            startIndex    = _startIndex;
            nerInputType  = NerInputType.MixCapital;
            nerOutputType = NerOutputType.MaritalStatus;
            MaritalStatus = maritalStatus;
        }
        public string MaritalStatus { get; }

#if DEBUG
        public override string ToString() => $"MARITAL STATUS => '{MaritalStatus}'";
#endif
    }
}