using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.BankAccounts
{
    /// <summary>
    /// 
    /// </summary>
    public enum BankAccountTypeEnum : byte
    {
        IBAN,
        BankCode_AccountNumber,
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class BankAccountWord : word_t
    {
        public BankAccountWord( int _startIndex, BankAccountTypeEnum bankAccountType ) 
        {
            startIndex      = _startIndex;
            nerInputType    = NerInputType.NumCapital;
            nerOutputType   = NerOutputType.AccountNumber;
            BankAccountType = bankAccountType;
        }

        public BankAccountTypeEnum BankAccountType { get; }

        public string IBAN;

        public string BankCode;
        public string AccountNumber;

        public string BankName;
        public string AccountOwner;
#if DEBUG
        public override string ToString() => (BankAccountType == BankAccountTypeEnum.IBAN) ? $"BANK-ACCOUNT => IBAN:'{IBAN}'"
                                            : $"BANK-ACCOUNT => {BankAccountType} => "
                                            + ((BankCode      != null) ? $", bank-code: '{BankCode}'" : null)
                                            + ((AccountNumber != null) ? $", account-number: '{AccountNumber}'" : null)
                                            + ((BankName      != null) ? $", bank-name: '{BankName}'" : null)
                                            + ((AccountOwner  != null) ? $", account-owner: '{AccountOwner}'" : null); 
#endif
    }
}