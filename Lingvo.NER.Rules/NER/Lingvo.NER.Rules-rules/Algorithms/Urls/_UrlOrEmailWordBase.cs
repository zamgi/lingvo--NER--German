using System;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.urls
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class UrlOrEmailWordBase : word_t
    {
        protected UrlOrEmailWordBase( url_t url ) 
        {
            nerInputType  = NerInputType.Other;
            startIndex    = url.startIndex;
            length        = url.length;
            valueOriginal = url.value;
            valueUpper    = StringsHelper.ToUpperInvariant( url.value );
        }
        public abstract UrlTypeEnum UrlType { get; }
        public static UrlOrEmailWordBase Create( url_t url )
        {
            switch ( url.type )
            {
                case UrlTypeEnum.Email: return (new EmailWord( url ));
                case UrlTypeEnum.Url  : return (new UrlWord  ( url ));
                default: throw (new ArgumentException( url.ToString() ));
            }
        }
#if DEBUG
        public override string ToString() => $"{UrlType} => '{valueOriginal}'";
#endif
    }
}