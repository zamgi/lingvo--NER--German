using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.TaxIdentifications
{
    /// <summary>
    /// 
    /// </summary>
    public enum TaxIdentificationTypeEnum : byte
    {
        Default,
        New = Default,
        Old,
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class TaxIdentificationWord : word_t
    {
        public TaxIdentificationWord( int _startIndex, string taxIdentificationNumber, TaxIdentificationTypeEnum taxIdentificationType ) 
        {
            startIndex              = _startIndex;
            nerInputType            = NerInputType.NumCapital;
            nerOutputType           = NerOutputType.TaxIdentification;
            TaxIdentificationNumber = taxIdentificationNumber;
            TaxIdentificationType   = taxIdentificationType;
        }
        public string TaxIdentificationNumber { get; }
        public TaxIdentificationTypeEnum TaxIdentificationType { get; }
#if DEBUG
        public override string ToString() => $"TAX-IDENTIFICATION: '{TaxIdentificationNumber}' ({TaxIdentificationType})"; 
#endif
    }
}