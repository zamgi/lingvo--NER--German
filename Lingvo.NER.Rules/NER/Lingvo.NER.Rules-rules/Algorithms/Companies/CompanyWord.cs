using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.Companies
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CompanyWord : word_t
    {
        public CompanyWord( int _startIndex ) 
        {
            startIndex    = _startIndex;
            nerInputType  = NerInputType.MixCapital;
            nerOutputType = NerOutputType.Company;
        }
        public string Name => valueOriginal;
#if DEBUG
        public override string ToString() => $"COMPANY => '{Name}'";
#endif
    }
}