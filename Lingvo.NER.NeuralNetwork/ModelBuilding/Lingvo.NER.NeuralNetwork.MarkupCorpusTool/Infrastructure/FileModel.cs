using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using Lingvo.NER.NeuralNetwork.MarkupCorpusTool.Properties;

namespace Lingvo.NER.NeuralNetwork.MarkupCorpusTool
{
    /// <summary>
    /// 
    /// </summary>
    internal class ObservableProperty< T > //where T : IComparable< T >
    {
        private T                         _Value;
        private readonly string           _PropertyName;
        private readonly Action< string > _FirePropertyChanged;
        private readonly object           _SyncRoot;

        public ObservableProperty( string propertyName, Action< string > firePropertyChanged, bool threadSafety = false )
        {
            if ( propertyName.IsEmptyOrNull() ) throw (new ArgumentNullException( nameof(propertyName) ));
            if ( firePropertyChanged == null  ) throw (new ArgumentNullException( nameof(firePropertyChanged) ));

            _PropertyName        = propertyName;
            _FirePropertyChanged = firePropertyChanged;

            if ( threadSafety )
                _SyncRoot = new object();
        }

        public T Value
        {
            get => _Value;
            set
            {
                if ( _SyncRoot != null )
                {
                    lock ( _SyncRoot )
                    {
                        if ( _Value == null )
                        {
                            if ( value != null )
                            {
                                _Value = value;
                                _FirePropertyChanged( _PropertyName );
                            }
                        }
                        else if ( (value == null) || !_Value.Equals( value ) )
                        {
                            _Value = value;
                            _FirePropertyChanged( _PropertyName );
                        }
                    }
                }
                else
                {
                    if ( _Value == null )
                    {
                        if ( value != null )
                        {
                            _Value = value;
                            _FirePropertyChanged( _PropertyName );
                        }
                    }
                    else if ( (value == null) || !_Value.Equals( value ) )
                    {
                        _Value = value;
                        _FirePropertyChanged( _PropertyName );
                    }
                }
            }
        }

        public static implicit operator T ( ObservableProperty< T > o ) => o.Value;
        /*public implicit operator ObservableProperty< T > ( ObservableProperty< T >, T t )
        {
            this.Value = t;
            return (this);
        }*/
    }

    /// <summary>
    /// Model of processed file
    /// </summary>
    internal sealed class FileModel : INotifyPropertyChanged
    {
        #region [.ctor() & Properties.]
        #region [.Fields.]
        private XDocument _XDoc;
        private ObservableProperty< string > _FileName;
        private ObservableProperty< bool >   _HasChanges;
        private ObservableProperty< int >    _SentsCount;
        private ObservableProperty< int >    _CurrentPageSize;
        private ObservableProperty< int >    _CurrentPageNumber;
        #endregion

        public FileModel()
        {
            _FileName          = new ObservableProperty< string >( "FileName", FirePropertyChanged );
            _HasChanges        = new ObservableProperty< bool >( "HasChanges", FirePropertyChanged, true );
            _SentsCount        = new ObservableProperty< int >( "SentsCount", FirePropertyChanged );
            _CurrentPageSize   = new ObservableProperty< int >( "CurrentPageSize", FirePropertyChanged );
            _CurrentPageNumber = new ObservableProperty< int >( "CurrentPageNumber", FirePropertyChanged );

            _CurrentPageNumber.Value = 0;
            _CurrentPageSize  .Value = Settings.Default.PageSize;
        }

        public string FileName => _FileName;
        public int SentsCount => _SentsCount;
        public XDocument Document
        {
            get => _XDoc; 
            private set
            {
                if ( _XDoc != value )
                {
                    _XDoc = value;

                    FirePropertyChanged( "HasLoadedFile" );
                    _HasChanges.Value = false;
                }
            }
        }
        private Dictionary< int, XElement > SpanDictionary { get; set; }
        private Dictionary< int, XElement > SentDictionary { get; set; }

        public int CurrentPageSize => _CurrentPageSize;
        public int CurrentPageNumber => _CurrentPageNumber;

        public bool HasLoadedFile => (Document != null);
        public bool HasChanges => _HasChanges;
        #endregion

        #region [.Methods.]
        public bool LoadFile( string fileName, Action< double > progressAction = null, CancellationTokenSource cts = null )
        {
            if ( fileName.IsEmptyOrNull() ) throw (new ArgumentNullException( nameof(fileName) ));

            #region [.read file from disk & prepare.]
            Document       = null;
            SpanDictionary = null;
            GC.Collect();

            _FileName.Value = fileName;

            var text = default(string);
            using ( var fs = new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )
            using ( var sr = new StreamReader( fs ) )
            {
                text = sr.ReadToEnd();
            }

            var sents = text.SplitByRN();

            _SentsCount       .Value = 0; //sents.Length;
            _CurrentPageNumber.Value = 0;            
            #endregion

            #region [.create-xdocument.]
            var sd   = new Dictionary< int, XElement >();
            var sntd = new Dictionary< int, XElement >();
            var xd   = XDocument.Parse( "<text/>" );

            var sb = new StringBuilder();
            var sentNumber = 0;
            var number     = 0;
            var sw         = (progressAction != null) ? Stopwatch.StartNew() : null;
            foreach ( var sent in sents )
            {
                #region [.IsCancellationRequested.]
                if ( (cts != null) && cts.IsCancellationRequested )
                {
                    _FileName.Value   = null;
                    _SentsCount.Value = 0;
                    Document          = null;
                    SpanDictionary    = null;
                    return (false);
                }                
                #endregion

                var xsent = $"<sent n='{sentNumber}' />".ToXElement();
                try
                {
                    var xt = $"<t>{sent}</t>".ToXElement();

                    foreach ( var xnode in xt.Nodes() )
                    {
                        switch ( xnode.NodeType )
                        {
                            case XmlNodeType.Element:
                                {
                                    var xe = (XElement) xnode;

                                    sb.Trancate();
                                    var words1 = xe.Value.SplitBySpace();
                                    foreach ( var word in words1 )
                                    {
                                        sb.AppendFormat( $"<span n='{number++}'>{word.ToHtmlEncode()}</span> " );
                                    }
                                    sb.RemoveLastChars();
                                
                                    if ( !sb.IsEmptyOrNull() )
                                    {
                                        var master_span = $"<span class='{xe.Name}'>{sb.ToString()}</span> ".ToXElement();
                                        xsent.Add( master_span );
                                        var i = number;
                                        foreach ( var span in master_span.Elements( "span" ).Reverse() )
                                        {
                                            sd.Add( --i, span );
                                        }  
                                    }
                                }
                            break;

                            default:
                                {
                                    var words = xnode.ToString().SplitBySpace();
                                    foreach ( var word in words )
                                    {
                                        var span = $"<span n='{number}'>{word.ToHtmlEncode().Replace( "&amp;amp;", "&amp;" )}</span> ".ToXElement();
                                        xsent.Add( span );
                                        sd.Add( number++, span );
                                    }
                                }
                            break;
                        }
                    }
                }
                catch ( XmlException )
                {
                    var words = sent.SplitBySpace();
                    foreach ( var word in words )
                    {
                        var span = $"<span n='{number}'>{word.ToHtmlEncodeForce()}</span> ".ToXElement();
                        xsent.Add( span );
                        sd.Add( number++, span );
                    }
                }

                if ( xsent.HasElements )
                {
                    xd.Root.Add( xsent );
                    sntd.Add( sentNumber, xsent );
                    sentNumber++;
                }

                #region [.progressAction.]
                if ( (progressAction != null) && (1500 < sw.ElapsedMilliseconds) )
                {
                    progressAction( (100.0 * sentNumber) / sents.Length /*SentsCount*/ );
                    sw.Reset();
                    sw.Start();
                }
                #endregion
            }
            #endregion

            Document          = xd;
            SpanDictionary    = sd;
            SentDictionary    = sntd;
            _SentsCount.Value = sntd.Count;

            #region [.progressAction.]
            if ( progressAction != null )
            {
                progressAction( (100.0 * sentNumber) / SentsCount );
                sw.Reset();
            }
            #endregion

            return (true);
        }
        
        public IList< Inline > GetInlinesByPages( int pageSize, int pageNumber, out int pageCount )
        {
            if ( !HasLoadedFile ) throw (new InvalidOperationException( "!HasLoadedFile" ));

            _CurrentPageSize.Value   = pageSize;
            _CurrentPageNumber.Value = pageNumber;

            pageCount = (int) ((SentsCount / (pageSize * 1.0)) + 1);

            var startSentNumber = pageNumber * pageSize;
            if ( SentsCount <= startSentNumber ) 
            {
                startSentNumber          = 0;
                _CurrentPageNumber.Value = 0;
            }
            
            var startSent = SentDictionary.TryGetValue( startSentNumber );
            if ( startSent == null )
            {
                if ( (startSentNumber == 0) && (SentDictionary.Count == 0) )
                {
                    throw (new InvalidOperationException( $"Empty file ? (Not found sent with number: {startSentNumber})." ));
                    //return (new List< Inline >());
                }

                throw (new InvalidOperationException( $"Not found sent with number: {startSentNumber}." ));
            }

            var inlines = new List< Inline >();        
            foreach ( var xe in (new[] { startSent }).Concat( startSent.ElementsAfterSelf() ).Take( pageSize ) )
            {
                foreach ( var span in xe.Descendants()
                                        .Select( d => d.HasElements ? d : ((d.Parent.Name == d.Name) ? d.Parent : d) )
                                        .Distinct() )
                {
                    ConvertModelElementToViewElements( inlines, span );

                    #region comm.
                    /*
                    var @class = span.Attribute( "class" );
                    if ( @class != null )
                    {
                        if ( span.HasElements )
                        {
                            var ispan = new Span() { Background = @class.NameToBrush() };
                            var last_child_span = span.Elements().LastOrDefault();
                            foreach ( var child_span in span.Elements() )
                            {
                                ispan.Inlines.Add( child_span.ToRun() );
                                if ( child_span != last_child_span )
                                    ispan.Inlines.AddRunSpace();
                            }
                            inlines.Add( ispan );
                        }
                        else
                        {
                            inlines.Add( span.ToRun( @class.NameToBrush() ) );
                        }
                    }
                    else
                    {
                        inlines.Add( span.ToRun() );
                    }
                    inlines.AddRunSpace();
                    */
                    #endregion
                }
                inlines.AddLineBreak();
            }
            return (inlines);
        }

        public IList< Inline > MarkupCommand( int[] itemsNumbers, string className )
        {            
            var spans = GetSpansByNumbers( itemsNumbers );

            //1.
            UnmarkSpans( spans );

            //2.
            var first_span = spans[ 0 ];
            if ( spans.Length == 1 )
            {
                first_span.SetAttribute( "class", className );
                var result = ConvertModelElementToViewElements( first_span );
                _HasChanges.Value = true;
                return (result);
            }
            else
            {
                var parent_span = $"<span class='{className}' /> ".ToXElement();                
                first_span.ReplaceWith( parent_span );
                parent_span.Add( first_span );
                parent_span.AddXTextSpace();
                foreach ( var span in spans.Skip( 1 ) )
                {
                    span.Remove();
                    parent_span.Add( span );
                    parent_span.AddXTextSpace();
                }
                parent_span.Nodes().Last().Remove();

                var result = ConvertModelElementToViewElements( parent_span );
                _HasChanges.Value = true;
                return (result);
            }
        }
        public IList< Inline > UnmarkCommand( int[] itemsNumbers )
        {
            var spans = GetSpansByNumbers( itemsNumbers );

            UnmarkSpans( spans );

            var result = ConvertModelElementToViewElements( spans );
            _HasChanges.Value = true;
            return (result);
        }
        public void DeleteCommand( int[] itemsNumbers )
        {
            var spans = GetSpansByNumbers( itemsNumbers );

            foreach ( var span in spans )
            {
                var n = span.Attribute( "n" ).Value.ToInt32();
                span.Remove();
                SpanDictionary.Remove( n );
            }

            _HasChanges.Value = true;
        }
        public IList< Inline > ReplaceCommand( int[] itemsNumbers, string replacedText )
        {
            var spans = GetSpansByNumbers( itemsNumbers );
            
            //1.
            var number = SpanDictionary.Any() ? (SpanDictionary.Max( pair => pair.Key ) + 1) : 0;
                
            //2.
            var words = replacedText.SplitBySpace();
            var replacedSpans = new List< XNode >( words.Length );
            foreach ( var word in words )
            {
                var span = $"<span n='{number}'>{word.ToHtmlEncode()}</span> ".ToXElement();
                replacedSpans.Add( span );
                replacedSpans.Add( new XText(" ") );
                SpanDictionary.Add( number++, span );
            }

            //3.
            var arraySpans = replacedSpans.ToArray();
            var firstSpan = spans[ 0 ];
            firstSpan.ReplaceWith( arraySpans );
            var n = firstSpan.Attribute( "n" ).Value.ToInt32();
            SpanDictionary.Remove( n );
            foreach ( var span in spans.Skip( 1 ) )
            {
                n = span.Attribute( "n" ).Value.ToInt32();
                span.Remove();
                SpanDictionary.Remove( n );
            }

            var result = ConvertModelElementToViewElements( arraySpans.OfType< XElement >() );
            _HasChanges.Value = true;
            return (result);
        }

        #region [.save-on-disk command.]
        public void SaveDocumentOnDisk() => SaveDocumentOnDiskRoutine( new object[] { Document, FileName, _HasChanges } );
        public void SaveDocumentOnDiskAsync() => Task.Factory.StartNew( SaveDocumentOnDiskRoutine, new object[] { Document, FileName, _HasChanges } );

        private static readonly object _SaveDocumentOnDiskSyncRoot = new object();
        private static void SaveDocumentOnDiskRoutine( object obj )
        {            
            lock ( _SaveDocumentOnDiskSyncRoot )
            {
                var document   = (XDocument) ((object[])obj)[ 0 ];
                var path       = (string)    ((object[])obj)[ 1 ];
                var hasChanges = (ObservableProperty< bool >) ((object[])obj)[ 2 ];

                //document.Save( path );

                using ( var fs = new FileStream( path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite ) )
                {
                    fs.SetLength( 0 );

                    Config.SaveXslTransform.Transform( document.CreateNavigator(), null, fs );
                }

                hasChanges.Value = false;
            }
        }
        #endregion

        private static IList< Inline > ConvertModelElementToViewElements( XElement span )
        {
            var inlines = new List< Inline >();
            ConvertModelElementToViewElements( inlines, span );
            return (RemoveLastRunSpace( inlines ));
        }
        private static IList< Inline > ConvertModelElementToViewElements( IEnumerable< XElement > spans )
        {
            var inlines = new List< Inline >();
            foreach ( var span in spans )
            {
                ConvertModelElementToViewElements( inlines, span );
            }
            return (RemoveLastRunSpace( inlines ));
        }
        private static IList< Inline > RemoveLastRunSpace( IList< Inline > inlines )
        {
            if ( 0 < inlines.Count )
            {
                if ( inlines[ inlines.Count - 1 ].IsRunSpace() )
                {
                    inlines.RemoveAt( inlines.Count - 1 );
                }
            }
            return (inlines);
        }
        private static void ConvertModelElementToViewElements( IList< Inline > inlines, XElement span )
        {
            var @class = span.Attribute( "class" );
            if ( @class != null )
            {
                if ( span.HasElements )
                {
                    var ispan = new Span() { Background = @class.GetBackgroundBrushByName(), Foreground = @class.GetForegroundBrushByName() };
                    var last_child_span = span.Elements().LastOrDefault();
                    foreach ( var child_span in span.Elements() )
                    {
                        ispan.Inlines.Add( child_span.ToRun() );
                        if ( child_span != last_child_span )
                        {
                            ispan.Inlines.AddRunSpace();
                        }
                    }
                    inlines.Add( ispan );
                }
                else
                {
                    inlines.Add( span.ToRun( @class.GetBackgroundBrushByName(), @class.GetForegroundBrushByName() ) );
                }
            }
            else
            {
                inlines.Add( span.ToRun() );
            }
            inlines.AddRunSpace();
        }

        private static void UnmarkSpans( XElement[] spans )
        {
            //1. delete possible parent-span & attribute-class
            foreach ( var span in spans )
            {
                var parent = span.Parent;
                if ( (parent != null) && (parent.Name == span.Name) )
                {
                    var children = parent.Nodes().ToArray();
                    parent.RemoveNodes();
                    parent.ReplaceWith( children );
                }
                span.RemoveAttribute( "class" );
            }
        }

        private XElement[] GetSpansByNumbers( int[] itemsNumbers )
        {
            if ( !HasLoadedFile )
                throw (new InvalidOperationException("!HasLoadedFile"));
            
            var spans = new List< XElement >();
            foreach ( var number in itemsNumbers )
            {
                var span = default(XElement);
                if ( !SpanDictionary.TryGetValue( number, out span ) )
                    throw (new InvalidOperationException("Span not found, @n = " + number.ToString().InSingleQuotes()));

                spans.Add( span );
            }

            if (spans.Count == 0)
                throw (new InvalidOperationException("spans.Length == 0"));

            if (spans.Count != itemsNumbers.Length)
                throw (new InvalidOperationException("spans.Length != itemsNumbers.Length"));

            return (spans.ToArray());

            #region [.xpath-version.]
            /*
            var xpath = ("//span[ {0} ]").FormatEx( itemsNumbers.Select( n => ("@n='{0}'").FormatEx( n ) ).JoinEx( " or " ) );
            var spans = Document.Root.XPathSelectElements( xpath ).ToArray();

            if (spans.Length == 0)
                throw (new InvalidOperationException("spans.Length == 0"));

            if (spans.Length != itemsNumbers.Length)
                throw (new InvalidOperationException("spans.Length != itemsNumbers.Length"));

            return (spans);
            */
            #endregion
        }
        #endregion

        #region [.INotifyPropertyChanged.]
        public event PropertyChangedEventHandler PropertyChanged;
        private void FirePropertyChanged( string propertyName ) => PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions4ProcessedFile
    {        
        public static void SetFontStyleAndForeground( this IEnumerable< SelectedInline > selectedInlines )
        {
            foreach ( var si in selectedInlines )
            {
                si.SetFontStyleAndForeground();
            }
        }
        public static void ResetFontStyleAndForeground( this IEnumerable< SelectedInline > selectedInlines )
        {
            foreach ( var si in selectedInlines )
            {
                si.ResetFontStyleAndForeground();
            }
        }

        public static void SetBackground( this IEnumerable< SelectedInline > selectedInlines, Brush background = null )
        {
            foreach ( var si in selectedInlines )
            {
                si.Inline.Background = background;
            }
        }

        public static IList< SelectedInline > RemoveFirstAndLastRunSpace( this IList< SelectedInline > selectedInlines )
        {
            if ( 0 < selectedInlines.Count )
            {
                if ( selectedInlines[ 0 ].Inline.IsRunSpace() )
                {
                    selectedInlines.RemoveAt( 0 );
                }
            }
            if ( 0 < selectedInlines.Count )
            {
                if ( selectedInlines[ selectedInlines.Count - 1 ].Inline.IsRunSpace() )
                {
                    selectedInlines.RemoveAt( selectedInlines.Count - 1 );
                }
            }
            return (selectedInlines);
        }
        public static bool EqualOrContainsAsChild( this Inline inline, DependencyObject item )
        {
            if ( inline == item )
                return (true);

            if ( inline is Span span )
                return (span.Inlines.Contains( item ));
            return (false);
        }

        public static string InnerText( this IEnumerable< SelectedInline > selectedInlines )
        {
            var sb = new StringBuilder();
            foreach ( var si in selectedInlines )
            {
                Run  run;
                Span span;
                if ( (run = si.Inline as Run) != null )
                {
                    sb.Append( run.Text );
                }
                else if ( (span = si.Inline as Span) != null )
                {
                    foreach ( var child in span.Inlines )
                    {
                        sb.Append( ((Run) child).Text );
                    }
                }
            }
            return (sb.ToString());
        }

        private static void Remove( this Inline inline )
        {
            if ( inline.Parent is Paragraph paragraph )
            {
                paragraph.Inlines.Remove( inline );
            }
            else if ( inline.Parent is Span span )
            {
                span.Inlines.Remove( inline );
            }
            else
            {
                throw (new InvalidOperationException( "inline.Parent is not Span-element and not Paragraph-element" ));
            }
        }
        public static void Remove( this IEnumerable< SelectedInline > selectedInlines )
        {
            foreach ( var si in selectedInlines )
            {
                si.Inline.Remove();
            }
        }
        private static InlineCollection GetParentInlineCollection( this Inline inline )
        {
            if ( inline.Parent is Paragraph paragraph )
            {
                return (paragraph.Inlines);
            }

            if ( inline.Parent is Span span )
            {
                return (span.Inlines);
            }

            throw (new InvalidOperationException( "inline.Parent is not Span-element and not Paragraph-element" ));
        }
        public static void ReplaceWith( this IEnumerable< SelectedInline > selectedInlines, IList< Inline > replacedInlines )
        {
            if ( (selectedInlines == null) || !selectedInlines.Any() )
            {
                return;
            }

            if ( replacedInlines.AnyEx() )
            {
                var first = selectedInlines.First().Inline;
                var parentInlineCollection = first.GetParentInlineCollection();
                foreach ( var replacedInline in replacedInlines )
                {
                    parentInlineCollection.InsertBefore( first, replacedInline );
                }                
            }

            selectedInlines.Remove();
        }
    }
}
