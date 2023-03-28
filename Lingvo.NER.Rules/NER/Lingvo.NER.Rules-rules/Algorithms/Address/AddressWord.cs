using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.Address
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class AddressWord : word_t
    {
        public AddressWord( int _startIndex ) 
        {
            startIndex    = _startIndex;
            nerInputType  = NerInputType.MixCapital;
            nerOutputType = NerOutputType.Address;
        }

        public string Street;
        public string HouseNumber;
        public string ZipCodeNumber;
        public string City;
#if DEBUG
        public override string ToString() => $"ADDRESS => street:'{Street}', house:'{HouseNumber}', zip:'{ZipCodeNumber}', city:'{City}'"; 
#endif
    }
}