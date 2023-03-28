using System.Diagnostics;

using Lingvo.NER.Rules.urls;

namespace Lingvo.NER.Rules.urls
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class EmailWord : UrlOrEmailWordBase
    {
        public EmailWord( url_t url ) : base( url )
        {
#if DEBUG
            Debug.Assert( url.type == UrlTypeEnum.Email );
#endif
            nerOutputType = NerOutputType.Email;
        }
        public override UrlTypeEnum UrlType => UrlTypeEnum.Email;
    }
}