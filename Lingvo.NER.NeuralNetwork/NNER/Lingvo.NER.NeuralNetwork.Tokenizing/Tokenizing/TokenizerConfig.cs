using System;
using System.IO;

using Lingvo.NER.NeuralNetwork.SentSplitting;

namespace Lingvo.NER.NeuralNetwork.Tokenizing
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

        public SentSplitterConfig     SentSplitterConfig    { get; }
        public INerInputTypeProcessor NerInputTypeProcessor { get; set; }
    }
}
