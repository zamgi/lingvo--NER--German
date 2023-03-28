using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.Birthplaces
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class BirthplaceWord : word_t
    {
        public BirthplaceWord( int _startIndex, string birthplace ) 
        {
            startIndex    = _startIndex;
            nerInputType  = NerInputType.MixCapital;
            nerOutputType = NerOutputType.Birthplace;
            Birthplace    = birthplace;
        }
        public string Birthplace { get; }
#if DEBUG
        public override string ToString() => $"BIRTHPLACE => '{Birthplace}'";
#endif
    }
}