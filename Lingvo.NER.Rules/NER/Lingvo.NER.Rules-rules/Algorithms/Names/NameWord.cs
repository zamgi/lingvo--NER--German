using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.Names
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class NameWord : word_t
    {
        public NameWord( int _startIndex ) 
        {
            startIndex    = _startIndex;
            nerInputType  = NerInputType.MixCapital;
            nerOutputType = NerOutputType.Name;
        }

        public string Firstname;
        public string Surname;
        public TextPreambleTypeEnum TextPreambleType;
        public string MaritalStatus;
#if DEBUG
        public override string ToString() => $"NAME => first-name: '{Firstname}', sur-name: '{Surname}'" 
                                           + ((TextPreambleType != TextPreambleTypeEnum.__UNDEFINED__) ? $" ({TextPreambleType})" : null)
                                           + ((MaritalStatus != null) ? $" (marital-status: '{MaritalStatus}')" : null); 
#endif
    }
}