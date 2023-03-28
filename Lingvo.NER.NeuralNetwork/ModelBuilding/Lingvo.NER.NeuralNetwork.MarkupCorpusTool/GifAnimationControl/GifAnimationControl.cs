using System.IO;
using System.Net;
using System.Security;
using System.Windows.Media;
using System.Windows.Resources;
using System.Windows.Threading;

namespace System.Windows.Controls
{
    /// <summary>
    /// 
    /// </summary>
    public class GifAnimationControlExceptionRoutedEventArgs : RoutedEventArgs
    {
        public Exception ErrorException;
        public GifAnimationControlExceptionRoutedEventArgs( RoutedEvent routedEvent, object obj ) : base( routedEvent, obj ) { }
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class WebReadState
    {
        public WebRequest WebRequest;
        public MemoryStream MemoryStream;
        public Stream ReadStream;
        public byte[] Buffer;
    }

    /// <summary>
    /// 
    /// </summary>
    public class GifAnimationControl : UserControl
    {
        // Only one of the following (_gifAnimation or _image) should be non null at any given time
        private GifAnimation _GifAnimation = null;
        private Image _Image = null;

        public GifAnimationControl() { }

        public static readonly DependencyProperty ForceGifAnimProperty = DependencyProperty.Register( "ForceGifAnim", typeof( bool ), typeof( GifAnimationControl ), new FrameworkPropertyMetadata( false ) );
        public bool ForceGifAnim
        {
            get => (bool) this.GetValue( ForceGifAnimProperty ); 
            set => this.SetValue( ForceGifAnimProperty, value ); 
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register( "Source", typeof( string ), typeof( GifAnimationControl ), new FrameworkPropertyMetadata( "", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback( OnSourceChanged ) ) );
        private static void OnSourceChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            var ga = (GifAnimationControl) d;
            var s  = (string) e.NewValue;
            ga.CreateFromSourceString( s );
        }
        public string Source
        {
            get => (string) this.GetValue( SourceProperty ); 
            set => this.SetValue( SourceProperty, value ); 
        }


        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register( "Stretch", typeof( Stretch ), typeof( GifAnimationControl ), new FrameworkPropertyMetadata( Stretch.Fill, FrameworkPropertyMetadataOptions.AffectsMeasure, new PropertyChangedCallback( OnStretchChanged ) ) );
        private static void OnStretchChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            var obj = (GifAnimationControl) d;
            var s   = (Stretch) e.NewValue;
            if ( obj._GifAnimation != null )
            {
                obj._GifAnimation.Stretch = s;
            }
            else if ( obj._Image != null )
            {
                obj._Image.Stretch = s;
            }
        }
        public Stretch Stretch
        {
            get => (Stretch) this.GetValue( StretchProperty ); 
            set => this.SetValue( StretchProperty, value ); 
        }

        public static readonly DependencyProperty StretchDirectionProperty = DependencyProperty.Register( "StretchDirection", typeof( StretchDirection ), typeof( GifAnimationControl ), new FrameworkPropertyMetadata( StretchDirection.Both, FrameworkPropertyMetadataOptions.AffectsMeasure, new PropertyChangedCallback( OnStretchDirectionChanged ) ) );
        private static void OnStretchDirectionChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            var obj = (GifAnimationControl) d;
            var s   = (StretchDirection) e.NewValue;
            if ( obj._GifAnimation != null )
            {
                obj._GifAnimation.StretchDirection = s;
            }
            else if ( obj._Image != null )
            {
                obj._Image.StretchDirection = s;
            }
        }
        public StretchDirection StretchDirection
        {
            get => (StretchDirection) this.GetValue( StretchDirectionProperty ); 
            set => this.SetValue( StretchDirectionProperty, value ); 
        }

        public delegate void ExceptionRoutedEventHandler( object sender, GifAnimationControlExceptionRoutedEventArgs args );

        public static readonly RoutedEvent ImageFailedEvent = EventManager.RegisterRoutedEvent( "ImageFailed", RoutingStrategy.Bubble, typeof( ExceptionRoutedEventHandler ), typeof( GifAnimationControl ) );

        public event ExceptionRoutedEventHandler ImageFailed
        {
            add => AddHandler( ImageFailedEvent, value ); 
            remove => RemoveHandler( ImageFailedEvent, value ); 
        }

        private void _Image_ImageFailed( object sender, ExceptionRoutedEventArgs e ) => RaiseImageFailedEvent( e.ErrorException );

        private void RaiseImageFailedEvent( Exception exp )
        {
            var newArgs = new GifAnimationControlExceptionRoutedEventArgs( ImageFailedEvent, this );
            newArgs.ErrorException = exp;
            RaiseEvent( newArgs );
        }

        private void DeletePreviousImage()
        {
            if ( _Image != null )
            {
                this.RemoveLogicalChild( _Image );
                this.Content = null;
                _Image = null;
            }
            if ( _GifAnimation != null )
            {
                this.RemoveLogicalChild( _GifAnimation );
                this.Content = null;
                _GifAnimation = null;
            }
        }

        private void CreateNonGifAnimationImage()
        {
            if ( Source == string.Empty ) return;
            _Image = new Image();
            _Image.ImageFailed += new EventHandler< ExceptionRoutedEventArgs >( _Image_ImageFailed );
            var src = (ImageSource) (new ImageSourceConverter().ConvertFromString( Source ));
            _Image.Source = src;
            _Image.Stretch = Stretch;
            _Image.StretchDirection = StretchDirection;
            this.AddChild( _Image );
        }

        private void CreateGifAnimation( MemoryStream ms )
        {
            _GifAnimation = new GifAnimation();
            _GifAnimation.CreateGifAnimation( ms );
            _GifAnimation.Stretch = Stretch;
            _GifAnimation.StretchDirection = StretchDirection;
            this.AddChild( _GifAnimation );
        }

        private void CreateFromSourceString( string source )
        {
            DeletePreviousImage();
            Uri uri;
            try
            {
                uri = new Uri( source, UriKind.RelativeOrAbsolute );
            }
            catch ( Exception ex )
            {
                RaiseImageFailedEvent( ex );
                return;
            }
            if ( source.Trim().EndsWith( ".GIF", StringComparison.InvariantCultureIgnoreCase ) || ForceGifAnim )
            {
                if ( !uri.IsAbsoluteUri )
                {
                    GetGifStreamFromPack( uri );
                }
                else
                {

                    string leftPart = uri.GetLeftPart( UriPartial.Scheme );

                    if ( leftPart == "http://" || leftPart == "ftp://" || leftPart == "file://" )
                    {
                        GetGifStreamFromHttp( uri );
                    }
                    else if ( leftPart == "pack://" )
                    {
                        GetGifStreamFromPack( uri );
                    }
                    else
                    {
                        CreateNonGifAnimationImage();
                    }
                }
            }
            else
            {
                CreateNonGifAnimationImage();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private delegate void WebRequestFinishedDelegate( MemoryStream memoryStream );

        private void WebRequestFinished( MemoryStream memoryStream ) => CreateGifAnimation( memoryStream );

        /// <summary>
        /// 
        /// </summary>
        private delegate void WebRequestErrorDelegate( Exception exp );

        private void WebRequestError( Exception exp ) => RaiseImageFailedEvent( exp );

        private void WebResponseCallback( IAsyncResult asyncResult )
        {
            var webReadState = (WebReadState) asyncResult.AsyncState;
            WebResponse webResponse;
            try
            {
                webResponse = webReadState.WebRequest.EndGetResponse( asyncResult );
                webReadState.ReadStream = webResponse.GetResponseStream();
                webReadState.Buffer = new byte[ 100000 ];
                webReadState.ReadStream.BeginRead( webReadState.Buffer, 0, webReadState.Buffer.Length, new AsyncCallback( WebReadCallback ), webReadState );
            }
            catch ( WebException exp )
            {
                this.Dispatcher.Invoke( DispatcherPriority.Render, new WebRequestErrorDelegate( WebRequestError ), exp );
            }
        }

        private void WebReadCallback( IAsyncResult asyncResult )
        {
            var webReadState = (WebReadState) asyncResult.AsyncState;
            var count = webReadState.ReadStream.EndRead( asyncResult );
            if ( count > 0 )
            {
                webReadState.MemoryStream.Write( webReadState.Buffer, 0, count );
                try
                {
                    webReadState.ReadStream.BeginRead( webReadState.Buffer, 0, webReadState.Buffer.Length, new AsyncCallback( WebReadCallback ), webReadState );
                }
                catch ( WebException exp )
                {
                    this.Dispatcher.Invoke( DispatcherPriority.Render, new WebRequestErrorDelegate( WebRequestError ), exp );
                }
            }
            else
            {
                this.Dispatcher.Invoke( DispatcherPriority.Render, new WebRequestFinishedDelegate( WebRequestFinished ), webReadState.MemoryStream );
            }
        }

        private void GetGifStreamFromHttp( Uri uri )
        {
            try
            {
                var webReadState = new WebReadState()
                {
                    MemoryStream = new MemoryStream(),
                    WebRequest   = WebRequest.Create( uri ),                    
                };
                webReadState.WebRequest.Timeout = 10000;
                webReadState.WebRequest.BeginGetResponse( new AsyncCallback( WebResponseCallback ), webReadState );
            }
            catch ( SecurityException )
            {
                // Try image load, The Image class can display images from other web sites
                CreateNonGifAnimationImage();
            }
        }

        private void ReadGifStreamSynch( Stream s )
        {
            byte[] gifData;
            MemoryStream ms;
            using ( s )
            {
                ms = new MemoryStream( (int) s.Length );
                var br = new BinaryReader( s );
                gifData = br.ReadBytes( (int) s.Length );
                ms.Write( gifData, 0, (int) s.Length );
                ms.Flush();
            }
            CreateGifAnimation( ms );
        }

        private void GetGifStreamFromPack( Uri uri )
        {
            try
            {
                StreamResourceInfo streamInfo;

                if ( !uri.IsAbsoluteUri )
                {
                    streamInfo = Application.GetContentStream( uri );
                    if ( streamInfo == null )
                    {
                        streamInfo = Application.GetResourceStream( uri );
                    }
                }
                else
                {
                    if ( uri.GetLeftPart( UriPartial.Authority ).Contains( "siteoforigin" ) )
                    {
                        streamInfo = Application.GetRemoteStream( uri );
                    }
                    else
                    {
                        streamInfo = Application.GetContentStream( uri );
                        if ( streamInfo == null )
                        {
                            streamInfo = Application.GetResourceStream( uri );
                        }
                    }
                }
                if ( streamInfo == null )
                {
                    throw new FileNotFoundException( "Resource not found.", uri.ToString() );
                }
                ReadGifStreamSynch( streamInfo.Stream );
            }
            catch ( Exception ex )
            {
                RaiseImageFailedEvent( ex );
            }
        }
    }
}
