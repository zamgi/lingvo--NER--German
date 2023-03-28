using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.SocialSecurities
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class SocialSecurityWord : word_t
    {
        public SocialSecurityWord( int _startIndex, string socialSecurityNumber ) 
        {
            startIndex           = _startIndex;
            nerInputType         = NerInputType.NumCapital;
            nerOutputType        = NerOutputType.SocialSecurity;
            SocialSecurityNumber = socialSecurityNumber;
        }
        public string SocialSecurityNumber { get; }
#if DEBUG
        public override string ToString() => $"SOCIAL-SECURITY: '{SocialSecurityNumber}'"; 
#endif
    }
}