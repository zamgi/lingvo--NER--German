namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    public enum NerOutputType : byte
    {
        Other = 0,

        NAME__Crf  = 1,
        ORG__Crf   = 2,
        GEO__Crf   = 3,
        ENTR__Crf  = 4,
        PROD__Crf  = 5,


        PhoneNumber,
        Address,
        Url,
        Email,
        AccountNumber,
        Name,
        MaritalStatus,
        CustomerNumber,
        Birthday,
        Birthplace,
        Nationality,
        CreditCard,
        PassportIdCardNumber,
        CarNumber,
        HealthInsurance,
        DriverLicense,
        SocialSecurity,
        TaxIdentification,
        Company,

        PERSON__NNER,
        ORGANIZATION__NNER,
        LOCATION__NNER,
        MISCELLANEOUS__NNER,
    }

    /// <summary>
    /// 
    /// </summary>
    public static partial class NerExtensions
    {
        public static string ToText( this NerOutputType nerOutputType )
        {
            switch ( nerOutputType )    
            {
                case NerOutputType.NAME__Crf: return "NAME__Crf";
                case NerOutputType.ORG__Crf : return "ORG__Crf";
                case NerOutputType.GEO__Crf : return "GEO__Crf";
                case NerOutputType.ENTR__Crf: return "ENTR__Crf";
                case NerOutputType.PROD__Crf: return "PROD__Crf";

                case NerOutputType.PhoneNumber         : return "PhoneNumber";
                case NerOutputType.Address             : return "Address";
                case NerOutputType.Url                 : return "Url";
                case NerOutputType.Email               : return "Email";
                case NerOutputType.AccountNumber       : return "AccountNumber";
                case NerOutputType.Name                : return "Name";
                case NerOutputType.MaritalStatus       : return "MaritalStatus";
                case NerOutputType.CustomerNumber      : return "CustomerNumber";
                case NerOutputType.Birthday            : return "Birthday";
                case NerOutputType.Birthplace          : return "Birthplace";
                case NerOutputType.Nationality         : return "Nationality";
                case NerOutputType.CreditCard          : return "CreditCard";
                case NerOutputType.PassportIdCardNumber: return "PassportIdCardNumber";
                case NerOutputType.CarNumber           : return "CarNumber";
                case NerOutputType.HealthInsurance     : return "HealthInsurance";
                case NerOutputType.DriverLicense       : return "DriverLicense";
                case NerOutputType.SocialSecurity      : return "SocialSecurity";
                case NerOutputType.TaxIdentification   : return "TaxIdentification";
                case NerOutputType.Company             : return "Company";

                case NerOutputType.PERSON__NNER       : return "PERSON__NNER";
                case NerOutputType.ORGANIZATION__NNER : return "ORGANIZATION__NNER";
                case NerOutputType.LOCATION__NNER     : return "LOCATION__NNER";
                case NerOutputType.MISCELLANEOUS__NNER: return "MISCELLANEOUS__NNER";

                case NerOutputType.Other               : return "Other";
                default                                : return (nerOutputType.ToString());
            }
        }

        public static char ToCrfChar( this NerOutputType nerOutputType )
        {
            switch ( nerOutputType )    
            {
                case NerOutputType.NAME__Crf: return ('N');
                case NerOutputType.ORG__Crf : return ('J');
                case NerOutputType.GEO__Crf : return ('G');
                case NerOutputType.ENTR__Crf: return ('E');
                case NerOutputType.PROD__Crf: return ('P');

                default: //case NerOutputType.O:  
                         return ('O');
            }
        }
        unsafe public static NerOutputType ToNerOutputType( byte* value )
        {
            switch ( ((char) *value++) )
            {
                case 'B': //"B-N", "B-J", "B-G", "B-E", "B-P"
                case 'I': //"I-N", "I-J", "I-G", "I-E", "I-P"
                {
                    var ch = ((char) *value++);
                    if ( ch != '-' ) break;

                    switch ( ((char) *value++) )
                    {
                        case 'N': return (NerOutputType.NAME__Crf);
                        case 'J': return (NerOutputType.ORG__Crf);
                        case 'G': return (NerOutputType.GEO__Crf);
                        case 'E': return (NerOutputType.ENTR__Crf);
                        case 'P': return (NerOutputType.PROD__Crf);
                    }
                }
                break;
            }

            return (NerOutputType.Other);
        }
    }
}
