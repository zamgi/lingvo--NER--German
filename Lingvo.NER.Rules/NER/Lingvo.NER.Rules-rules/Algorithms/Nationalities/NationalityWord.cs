using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.Nationalities
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class NationalityWord : word_t
    {
        public NationalityWord( int _startIndex, string nationality ) 
        {
            startIndex    = _startIndex;
            nerInputType  = NerInputType.MixCapital;
            nerOutputType = NerOutputType.Nationality;
            Nationality   = nationality;
        }
        public string Nationality { get; }
#if DEBUG
        public override string ToString() => $"NATIONALITY => '{Nationality}'";
#endif
    }
}