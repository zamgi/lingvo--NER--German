using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.crfsuite
{
    /// <summary>
    /// Обработчик именованных сущностей. Обработка с использованием библиотеки CRFSuit 
    /// </summary>
    public sealed class NerModelBuilder : IDisposable
    {
        #region [.const's.]
        /*
        private const string NAME_TAG = "NAME";
        private const string ORG_TAG  = "ORG";
        private const string GEO_TAG  = "GEO";
        private const string ENTR_TAG = "ENTR";
        private const string PROD_TAG = "PROD";
        */

        private const string NAME_TAG = "N";
        private const string ORG_TAG  = "J";
        private const string GEO_TAG  = "G";
        private const string ENTR_TAG = "E";
        private const string PROD_TAG = "P";
        #endregion

        #region [.private field's.]
        private readonly Tokenizer                 _Tokenizer;
        private readonly NerScriber                _NerScriber;
        private readonly StringBuilder             _Sb;
        private readonly List< buildmodel_word_t > _Words;
        private readonly bool                      _IgnoreXmlError;
        #endregion

        #region [.ctor().]
        public NerModelBuilder( NerModelBuilderConfig config )
		{
			CheckConfig( config );

            _NerScriber     = NerScriber.Create4ModelBuilder( config.TemplateFile );
            #region [.learner. not used.]
            /*_NerCRFSuiteModelBuilderAdapter = new NerCRFSuiteModelBuilderAdapter( 
                config.ModelFilename, 
                config.TemplateFilename, 
                config.Algorithm 
            );*/
            #endregion
            _Tokenizer      = Tokenizer.Create4NerModelBuilder( config.TokenizerConfig4NerModelBuilder );
            _Words          = new List< buildmodel_word_t >();
            _Sb             = new StringBuilder();
            _IgnoreXmlError = config.IgnoreXmlError;
		}

        public void Dispose() => _NerScriber.Dispose();
        #endregion

        public delegate void ProcessSentCallbackDelegate( int sentNumber );
        public delegate void ProcessXmlErrorSentCallbackDelegate( string line, int sentNumber );
        public delegate void StartBuildCallbackDelegate();

        public int Build( TextReader                          textReader, 
                          ProcessSentCallbackDelegate         processSentCallback, 
                          int                                 sentNumberCallbackStep,
                          ProcessXmlErrorSentCallbackDelegate processXmlErrorSentCallback,
                          StartBuildCallbackDelegate          startBuildCallback )
        {
            throw (new NotImplementedException( "learner. not used." ));    
        }
        #region [.learner. not used.]
        /*
        public int Build( TextReader                          textReader, 
                          ProcessSentCallbackDelegate         processSentCallback, 
                          int                                 sentNumberCallbackStep,
                          ProcessXmlErrorSentCallbackDelegate processXmlErrorSentCallback,
                          StartBuildCallbackDelegate          startBuildCallback )
        {
            var sentNumber = 1;
            for ( var line = textReader.ReadLine(); line != null; line = textReader.ReadLine() )
            {
                var xe = ToXElement( line, sentNumber );
                if ( xe != null )
                {
                    var words = from n in xe.Nodes()
                                from word in CreateWords( n )
                                select word;

                    _Words.Clear();
                    _Words.AddRange( words );

                    _NerCRFSuiteModelBuilderAdapter.AppendWords( _Words );
                }
                else
                {
                    processXmlErrorSentCallback( line, sentNumber );
                }


                if ( (sentNumber % sentNumberCallbackStep) == 0 )
                {
                    processSentCallback( sentNumber );
                }
                sentNumber++;
            }
            if ( (sentNumber % sentNumberCallbackStep) != 0 )
            {
                processSentCallback( sentNumber );
            }

            startBuildCallback();

            _NerCRFSuiteModelBuilderAdapter.Build();

            return (sentNumber);
        }
        */
        #endregion

        public int CreateCrfInputFormatFile( TextWriter                          textWriter,
                                             TextReader                          textReader, 
                                             ProcessSentCallbackDelegate         processSentCallback, 
                                             int                                 sentNumberCallbackStep,
                                             ProcessXmlErrorSentCallbackDelegate processXmlErrorSentCallback )
        {
            var sentNumber = 1;
            for ( var line = textReader.ReadLine(); line != null; line = textReader.ReadLine() )
            {
                var xe = ToXElement( line, sentNumber );
                if ( xe != null )
                {
                    var words = from n in xe.Nodes()
                                from word in CreateWords( n )
                                select word;

                    _Words.Clear();
                    _Words.AddRange( words );

                    if ( 0 < _Words.Count )
                    {
                        _NerScriber.WriteCrfAttributesWords4ModelBuilder( textWriter, _Words );
                    }
                }
                else
                {
                    processXmlErrorSentCallback( line, sentNumber );
                }

                if ( (sentNumber % sentNumberCallbackStep) == 0 )
                {
                    processSentCallback( sentNumber );
                }
                sentNumber++;
            }
            if ( (sentNumber % sentNumberCallbackStep) != 0 )
            {
                processSentCallback( sentNumber );
            }

            return (sentNumber);
        }

        private XElement ToXElement( string sent, int lineNumber )
        {
            try
            {
                var xml = _Sb.Clear().Append( "<r>" ).Append( sent ).Append( "</r>" ).ToString();
                var xe = XElement.Parse( xml, LoadOptions.None );
                return (xe);
            }
            catch ( XmlException /*ex*/ )
            {
                try
                {
                    var xml = _Sb.Replace( "&", "&amp;" ).ToString();
                    var xe = XElement.Parse( xml, LoadOptions.None );
                    return (xe);
                }
                catch ( Exception ex )
                {
                    if ( _IgnoreXmlError )
                        return (null);

                    throw (new InvalidDataException( $"APPROXIMITE-LINE-NUMBER: {lineNumber}, SENT-TEXT: '{sent}'", ex ));
                }
            }
        }
        private IEnumerable< buildmodel_word_t > CreateWords( XNode xnode )
        {
            switch ( xnode.NodeType )
            {
                case XmlNodeType.Element:
                    var xe = (XElement) xnode;
                    return (CreateByMarkupText( xe ));

                case XmlNodeType.Text:
                    return (CreateByPlainText( xnode ));
            }

            return (Enumerable.Empty< buildmodel_word_t >());
        }
        private IEnumerable< buildmodel_word_t > CreateByMarkupText( XElement xe )
        {
            var nerOutputType = ToNerOutputType( xe );

            var prev_xe_same_this_type = false;
            var xnode_prev = xe.PreviousNode;
            if ( (xnode_prev != null) && (xnode_prev.NodeType == XmlNodeType.Element) )
            {
                var xe_prev = (XElement) xnode_prev;
                var nerOutputType_prev = ToNerOutputType( xe_prev );
                if ( nerOutputType == nerOutputType_prev )
                {
                    prev_xe_same_this_type = true;
                }
            }

            var words = _Tokenizer.Run4ModelBuilder( xe.Value, (xe.NextNode == null), nerOutputType, prev_xe_same_this_type );
            return (words);
        }
        private IEnumerable< buildmodel_word_t > CreateByPlainText( XNode xnode ) => _Tokenizer.Run4ModelBuilder( xnode.ToString(), (xnode.NextNode == null), NerOutputType.Other, false );

        private NerOutputType ToNerOutputType( XElement xe )
        {
            switch ( xe.Name.LocalName )
            {
                case NAME_TAG: return (NerOutputType.NAME__Crf);
                case ORG_TAG : return (NerOutputType.ORG__Crf);
                case GEO_TAG : return (NerOutputType.GEO__Crf);
                case ENTR_TAG: return (NerOutputType.ENTR__Crf);
                case PROD_TAG: return (NerOutputType.PROD__Crf);
                default:
                    var xli = ((IXmlLineInfo) xe);
                    var li  = (xli != null) ? ($" ({xli.LineNumber}:{xli.LinePosition})") : null;
                    throw (new ArgumentException( $"Wrong markup: '{xe.Name.LocalName}'{li}" ));
            }
        }

        private static void CheckConfig( NerModelBuilderConfig config )
		{
			config.ThrowIfNull( nameof(config) );
            #region [.learner. not used.]
            /*
            config.ModelFilename    .ThrowIfNullOrWhiteSpace( "ModelFilename" );
            */
            #endregion
			config.TemplateFile.ThrowIfNull( nameof(config.TemplateFile) );
            config.TokenizerConfig4NerModelBuilder.ThrowIfNull( nameof(config.TokenizerConfig4NerModelBuilder) );
		}
    }
}
