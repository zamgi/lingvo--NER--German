using System;
using System.IO;

using Lingvo.NER.Rules.sentSplitting;
using Lingvo.NER.Rules.urls;

namespace Lingvo.NER.Rules.tokenizing
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class TokenizerConfig : IDisposable
    {
        public TokenizerConfig( string sentSplitterResourcesXmlFilename, string urlDetectorResourcesXmlFilename )
            => SentSplitterConfig = new SentSplitterConfig( sentSplitterResourcesXmlFilename, urlDetectorResourcesXmlFilename );
        public TokenizerConfig( StreamReader sentSplitterResourcesXmlStreamReader, StreamReader urlDetectorResourcesXmlStreamReader )
            => SentSplitterConfig = new SentSplitterConfig( sentSplitterResourcesXmlStreamReader, urlDetectorResourcesXmlStreamReader );
        public void Dispose() => SentSplitterConfig.Dispose();

        public SentSplitterConfig            SentSplitterConfig           { get; /*set;*/ }
        public INerInputTypeProcessorFactory NerInputTypeProcessorFactory { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class TokenizerConfig4NerModelBuilder
    {
        public TokenizerConfig4NerModelBuilder() { }
        public UrlDetectorConfig             UrlDetectorConfig            { get; set; }
        public INerInputTypeProcessorFactory NerInputTypeProcessorFactory { get; set; }        
    }
}
