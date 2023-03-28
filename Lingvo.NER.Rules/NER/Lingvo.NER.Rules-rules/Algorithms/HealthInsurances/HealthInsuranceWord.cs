using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.HealthInsurances
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class HealthInsuranceWord : word_t
    {
        public HealthInsuranceWord( int _startIndex, string healthInsuranceNumber ) 
        {
            startIndex            = _startIndex;
            nerInputType          = NerInputType.NumCapital;
            nerOutputType         = NerOutputType.HealthInsurance;
            HealthInsuranceNumber = healthInsuranceNumber;
        }
        public string HealthInsuranceNumber { get; }
#if DEBUG
        public override string ToString() => $"HEALTH-INSURANCE: '{HealthInsuranceNumber}'"; 
#endif
    }
}