using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.DriverLicenses
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DriverLicenseWord : word_t
    {
        public DriverLicenseWord( int _startIndex, string driverLicense ) 
        {
            startIndex    = _startIndex;
            nerInputType  = NerInputType.NumCapital;
            nerOutputType = NerOutputType.DriverLicense;
            DriverLicense = driverLicense;
        }
        public string DriverLicense { get; }
#if DEBUG
        public override string ToString() => $"DRIVER-LICENSE: '{DriverLicense}'"; 
#endif
    }
}