using Lingvo.NER.Rules.Names;
using Lingvo.NER.Rules.tokenizing;
using Lingvo.NER.Rules.urls;

namespace Lingvo.NER.Rules.TestApp
{
#pragma warning disable CS0649
    /// <summary>
    /// 
    /// </summary>
    internal sealed class SuperWord : word_t
    {
        #region [.NameWord.]
        public string               Firstname;
        public string               Surname;
        public TextPreambleTypeEnum TextPreambleType;
        public string               MaritalStatus;
        #endregion

        #region [.UrlOrEmailWordBase.]
        public UrlTypeEnum UrlType;
        //public string valueOriginal;
        #endregion

        public override string ToString() //=> nerOutputType.ToString();
        {
            switch ( nerOutputType )
            {
                case NerOutputType.Name:
                    return $"NAME => first-name: '{Firstname}', sur-name: '{Surname}'"
                                           + ((TextPreambleType != TextPreambleTypeEnum.__UNDEFINED__) ? $" ({TextPreambleType})" : null)
                                           + ((MaritalStatus != null) ? $" (marital-status: '{MaritalStatus}')" : null);

                default:
                    return (nerOutputType.ToString());
            }
        }
    }
#pragma warning restore CS0649
}