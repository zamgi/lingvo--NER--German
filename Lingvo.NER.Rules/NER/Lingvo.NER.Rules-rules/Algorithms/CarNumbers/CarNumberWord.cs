using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.CarNumbers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CarNumberWord : word_t
    {
        public CarNumberWord( int _startIndex, string carNumber ) 
        {
            startIndex    = _startIndex;
            nerInputType  = NerInputType.NumCapital;
            nerOutputType = NerOutputType.CarNumber;
            CarNumber     = carNumber;
        }
        public string CarNumber { get; }
#if DEBUG
        public override string ToString() => $"CAR-NUMBER: '{CarNumber}'"; 
#endif
    }
}