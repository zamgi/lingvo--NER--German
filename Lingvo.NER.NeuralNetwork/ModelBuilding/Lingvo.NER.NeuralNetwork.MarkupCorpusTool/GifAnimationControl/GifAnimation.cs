using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace System.Windows.Controls
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class GifAnimation : Viewbox
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class GifFrame : Image
        {
            public int delayTime;
            public int disposalMethod;
            public int left;
            public int top;
            public int width;
            public int height;
        }

        // Gif Animation Fields
        private Canvas _Canvas = null;

        private List< GifFrame > _FrameList = null;

        private int _FrameCounter = 0;
        private int _NumberOfFrames = 0;

        private int _NumberOfLoops = -1;
        private int _CurrentLoop = 0;

        private int _LogicalWidth = 0;
        private int _LogicalHeight = 0;

        private Geometry _TotalTransparentGeometry;
        private GifFrame _CurrentParseGifFrame;
        private WeakContainer _WeakContainer = null;
        private DispatcherTimer _FrameTimer = null;

        private DispatcherTimer frameTimer
        {
            get => _FrameTimer;
            set
            {
                if ( _WeakContainer == null )
                {
                    _WeakContainer = new WeakContainer( this );
                }

                // unsubscribe old
                if ( _FrameTimer != null )
                {
                    _FrameTimer.Tick -= new EventHandler( _WeakContainer.NextFrame );
                }

                // subscribe new
                _FrameTimer = value;
                if ( _FrameTimer != null )
                {
                    _FrameTimer.Tick += new EventHandler( _WeakContainer.NextFrame );
                }
            }
        }

        // Container = GifAnimation
        // Containee = DispatchTimer
        // Handler = NextFrame
        private sealed class WeakContainer : WeakReference
        {
            public WeakContainer( GifAnimation target ) : base( target ) { }
            ~WeakContainer() { }

            public void NextFrame( object sender, EventArgs args )
            {
                var ga = (GifAnimation) this.Target;
                if ( ga != null )
                {
                    ga.NextFrame( sender, args );
                }
                else if ( sender is DispatcherTimer c )
                {
                    c.Tick -= new EventHandler( this.NextFrame );
                }
            }            
        }

        public GifAnimation()
        {
            _Canvas = new Canvas();
            this.Child = _Canvas;
        }

        private void Reset()
        {
            if ( _FrameList != null )
            {
                _FrameList.Clear();
            }
            _FrameList = null;
            _FrameCounter = 0;
            _NumberOfFrames = 0;
            _NumberOfLoops = -1;
            _CurrentLoop = 0;
            _LogicalWidth = 0;
            _LogicalHeight = 0;
            if ( frameTimer != null )
            {
                frameTimer.Stop();
                frameTimer = null;
            }
        }

        private void ParseGif( byte[] gifData )
        {
            _FrameList = new List< GifFrame >();
            _CurrentParseGifFrame = new GifFrame();
            ParseGifDataStream(gifData, 0);
        }

        private int ParseBlock( byte[] gifData, int offset )
        {
            switch ( gifData[ offset ] )
            {
                case 0x21:
                    if ( gifData[ offset + 1 ] == 0xF9 )
                    {
                        return ParseGraphicControlExtension( gifData, offset );
                    }
                    else
                    {
                        return ParseExtensionBlock( gifData, offset );
                    }

                case 0x2C:
                    offset = ParseGraphicBlock( gifData, offset );
                    _FrameList.Add( _CurrentParseGifFrame );
                    _CurrentParseGifFrame = new GifFrame();
                    return offset;

                case 0x3B:
                    return -1;

                default:
                    throw (new Exception( "GIF format incorrect: missing graphic block or special-purpose block. " ));
            }
        }

        private int ParseGraphicControlExtension( byte[] gifData, int offset )
        {
            // Extension Block
            int length = gifData[ offset + 2 ];
            var returnOffset = offset + length + 2 + 1;

            byte packedField = gifData[ offset + 3 ];
            _CurrentParseGifFrame.disposalMethod = (packedField & 0x1C) >> 2;

            // Get DelayTime
            int delay = BitConverter.ToUInt16( gifData, offset + 4 );
            _CurrentParseGifFrame.delayTime = delay;
            while ( gifData[ returnOffset ] != 0x00 )
            {
                returnOffset = returnOffset + gifData[ returnOffset ] + 1;
            }

            returnOffset++;

            return (returnOffset);
        }

        private int ParseLogicalScreen( byte[] gifData, int offset )
        {
            _LogicalWidth = BitConverter.ToUInt16( gifData, offset );
            _LogicalHeight = BitConverter.ToUInt16( gifData, offset + 2 );

            byte packedField = gifData[ offset + 4 ];
            bool hasGlobalColorTable = (int) (packedField & 0x80) > 0 ? true : false;

            int currentIndex = offset + 7;
            if ( hasGlobalColorTable )
            {
                int colorTableLength = packedField & 0x07;
                colorTableLength = (int) Math.Pow( 2, colorTableLength + 1 ) * 3;
                currentIndex = currentIndex + colorTableLength;
            }
            return (currentIndex);
        }

        private int ParseGraphicBlock( byte[] gifData, int offset )
        {
            _CurrentParseGifFrame.left   = BitConverter.ToUInt16( gifData, offset + 1 );
            _CurrentParseGifFrame.top    = BitConverter.ToUInt16( gifData, offset + 3 );
            _CurrentParseGifFrame.width  = BitConverter.ToUInt16( gifData, offset + 5 );
            _CurrentParseGifFrame.height = BitConverter.ToUInt16( gifData, offset + 7 );
            if ( _CurrentParseGifFrame.width > _LogicalWidth )
            {
                _LogicalWidth = _CurrentParseGifFrame.width;
            }
            if ( _CurrentParseGifFrame.height > _LogicalHeight )
            {
                _LogicalHeight = _CurrentParseGifFrame.height;
            }
            byte packedField = gifData[ offset + 9 ];
            var hasLocalColorTable = (packedField & 0x80) > 0 ? true : false;

            int currentIndex = offset + 9;
            if ( hasLocalColorTable )
            {
                int colorTableLength = packedField & 0x07;
                colorTableLength = (int) Math.Pow( 2, colorTableLength + 1 ) * 3;
                currentIndex = currentIndex + colorTableLength;
            }
            currentIndex++; // Skip 0x00

            currentIndex++; // Skip LZW Minimum Code Size;

            while ( gifData[ currentIndex ] != 0x00 )
            {
                currentIndex = currentIndex + gifData[ currentIndex ];
                currentIndex++; // Skip initial size byte
            }
            currentIndex = currentIndex + 1;
            return (currentIndex);
        }

        private int ParseExtensionBlock( byte[] gifData, int offset )
        {
            // Extension Block
            var length = gifData[ offset + 2 ];
            var returnOffset = offset + length + 2 + 1;
            // check if netscape continousLoop extension
            if ( gifData[ offset + 1 ] == 0xFF && length > 10 )
            {
                var netscape = Encoding.ASCII.GetString( gifData, offset + 3, 8 );
                if ( netscape == "NETSCAPE" )
                {
                    _NumberOfLoops = BitConverter.ToUInt16( gifData, offset + 16 );
                    if ( _NumberOfLoops > 0 )
                    {
                        _NumberOfLoops++;
                    }
                }
            }
            while ( gifData[ returnOffset ] != 0x00 )
            {
                returnOffset = returnOffset + gifData[ returnOffset ] + 1;
            }

            returnOffset++;
            return (returnOffset);
        }

        private int ParseHeader( byte[] gifData, int offset )
        {
            var h = Encoding.ASCII.GetString( gifData, offset, 3 );
            if ( h != "GIF" )
            {
                throw (new Exception( "Not a proper GIF file: missing GIF header" ));
            }
            return (6);
        }

        private void ParseGifDataStream( byte[] gifData, int offset )
        {
            offset = ParseHeader(gifData, offset);
            offset = ParseLogicalScreen(gifData, offset);
            while ( offset != -1 )
            {
                offset = ParseBlock(gifData, offset);
            }
        }

        public void CreateGifAnimation( MemoryStream ms )
        {
            Reset();

            byte[] gifData = ms.GetBuffer();  // Use GetBuffer so that there is no memory copy

            var decoder = new GifBitmapDecoder( ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default );

            _NumberOfFrames = decoder.Frames.Count;

            try
            {
                ParseGif( gifData );
            }
            catch
            {
                throw (new FileFormatException( "Unable to parse Gif file format." ));
            }

            for ( int f = 0; f < decoder.Frames.Count; f++ )
            {
                _FrameList[ f ].Source = decoder.Frames[ f ];
                _FrameList[ f ].Visibility = Visibility.Hidden;
                _Canvas.Children.Add( _FrameList[ f ] );
                Canvas.SetLeft( _FrameList[ f ], _FrameList[ f ].left );
                Canvas.SetTop( _FrameList[ f ], _FrameList[ f ].top );
                Canvas.SetZIndex( _FrameList[ f ], f );
            }
            _Canvas.Height = _LogicalHeight;
            _Canvas.Width = _LogicalWidth;

            _FrameList[ 0 ].Visibility = Visibility.Visible;

            for ( int i = 0; i < _FrameList.Count; i++ )
            {
                Console.WriteLine( _FrameList[ i ].disposalMethod.ToString() + " " + _FrameList[ i ].width.ToString() + " " + _FrameList[ i ].delayTime.ToString() );
            }

            if ( _FrameList.Count > 1 )
            {
                if ( _NumberOfLoops == -1 )
                {
                    _NumberOfLoops = 1;
                }

                frameTimer = new DispatcherTimer() { Interval = new TimeSpan( 0, 0, 0, 0, _FrameList[ 0 ].delayTime * 10 ) };
                frameTimer.Start();
            }
        }

        public void NextFrame() => NextFrame( null, null );

        public void NextFrame( object sender, EventArgs e )
        {
            frameTimer.Stop();
            if ( _NumberOfFrames == 0 ) return;
            if ( _FrameList[ _FrameCounter ].disposalMethod == 2 )
            {
                // dispose = background, tricky code to make transparent last frames region
                for ( int fc = 0; fc < _FrameCounter; fc++ )
                {
                    if ( _FrameList[ fc ].Visibility == Visibility.Visible )
                    {
                        GifFrame gf = _FrameList[ _FrameCounter ];
                        var rg2 = new RectangleGeometry( new Rect( gf.left, gf.top, gf.width, gf.height ) );
                        _TotalTransparentGeometry = new CombinedGeometry( GeometryCombineMode.Union, _TotalTransparentGeometry, rg2 );

                        GifFrame gfBack = _FrameList[ fc ];
                        var rgBack = new RectangleGeometry( new Rect( gfBack.left, gfBack.top, gfBack.width, gfBack.height ) );
                        var cg = new CombinedGeometry( GeometryCombineMode.Exclude, rgBack, _TotalTransparentGeometry );
                        var gd = new GeometryDrawing( Brushes.Black, new Pen( Brushes.Black, 0 ), cg );
                        var db = new DrawingBrush( gd );
                        _FrameList[ fc ].OpacityMask = db;
                    }
                }

                _FrameList[ _FrameCounter ].Visibility = Visibility.Hidden;
            }
            if ( _FrameList[ _FrameCounter ].disposalMethod >= 3 )
            {
                _FrameList[ _FrameCounter ].Visibility = Visibility.Hidden;
            }
            _FrameCounter++;

            if ( _FrameCounter < _NumberOfFrames )
            {
                _FrameList[ _FrameCounter ].Visibility = Visibility.Visible;
                frameTimer.Interval = new TimeSpan( 0, 0, 0, 0, _FrameList[ _FrameCounter ].delayTime * 10 );
                frameTimer.Start();
            }
            else
            {
                if ( _NumberOfLoops != 0 )
                {
                    _CurrentLoop++;
                }
                if ( _CurrentLoop < _NumberOfLoops || _NumberOfLoops == 0 )
                {
                    for ( int f = 0; f < _FrameList.Count; f++ )
                    {
                        _FrameList[ f ].Visibility = Visibility.Hidden;
                        _FrameList[ f ].OpacityMask = null;
                    }
                    _TotalTransparentGeometry = null;
                    _FrameCounter = 0;
                    _FrameList[ _FrameCounter ].Visibility = Visibility.Visible;
                    frameTimer.Interval = new TimeSpan( 0, 0, 0, 0, _FrameList[ _FrameCounter ].delayTime * 10 );
                    frameTimer.Start();
                }
            }
        }
    }
}
