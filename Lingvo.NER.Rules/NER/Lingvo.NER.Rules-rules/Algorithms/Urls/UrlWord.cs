using System.Diagnostics;

namespace Lingvo.NER.Rules.urls
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class UrlWord : UrlOrEmailWordBase
    {
        public UrlWord( url_t url ) : base( url )
        {
#if DEBUG
            Debug.Assert( url.type == UrlTypeEnum.Url );
#endif
            nerOutputType = NerOutputType.Url;
        }
        public override UrlTypeEnum UrlType => UrlTypeEnum.Url;
    }
}