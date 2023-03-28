using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

using Lingvo.NER.NeuralNetwork.MarkupCorpusTool.Properties;

namespace Lingvo.NER.NeuralNetwork.MarkupCorpusTool
{
    /// <summary>
    /// 
    /// </summary>
    internal struct Mark
    {
        public Mark( string configName, string tagName, string backgroundColorName, string foregroundColorName, string title ) : this()
        {
            ConfigName          = configName;
            TagName             = tagName;
            BackgroundColorName = backgroundColorName;
            ForegroundColorName = foregroundColorName;
            Title               = title.IsEmptyOrNull() ? tagName : title;

            BackgroundBrush = CreateBackgroundBrush( BackgroundColorName );
            ForegroundBrush = new SolidColorBrush( GetByName( ForegroundColorName ) );
            Pen             = new Pen( this.ForegroundBrush, 1.0 );
        }

        public string ConfigName          { get; }
        public string TagName             { get; }
        public string BackgroundColorName { get; }
        public string ForegroundColorName { get; }
        public string Title               { get; }

        public Brush BackgroundBrush { get; }
        public Brush ForegroundBrush { get; }
        public Pen   Pen             { get; }

        private static Color GetByName( string colorName )
        {
            Color color;
            if ( RgbColorParser.TryParse( colorName, out var rgb ) )
            {
                color = Color.FromRgb( rgb.r, rgb.g, rgb.b );
            }
            else
            {
                color = (Color) ColorConverter.ConvertFromString( colorName );
            }
            return (color);
        }
        private static Brush CreateBackgroundBrush( string colorName )
        {
            var color = GetByName( colorName );
            var backgroundBrush = new SolidColorBrush( color );

            return (backgroundBrush);

            #region [.trying make border around consolidate-many-runs-span.]
            /*
            var drawing = new DrawingGroup();
            var gd = new GeometryDrawing()
            {
                Geometry = new RectangleGeometry(new System.Windows.Rect(0, 0, 1, 1)),
                Brush    = new SolidColorBrush( (Color) ColorConverter.ConvertFromString( BackgroundcolorName ) ),
            };
            drawing.Children.Add( gd );
            var id = new ImageDrawing()
            {
                Rect = new System.Windows.Rect(0, 0, 1, 1),
                ImageSource = new BitmapImage( new Uri( "pack://application:,,,/Lingvo.NER.NeuralNetwork.MarkupCorpusTool.app;Component/Images/border.png" ) )
            };
            drawing.Children.Add( id );

            _Brush = new DrawingBrush( drawing );
            */
            #endregion
        }        
     

        public override int GetHashCode() => TagName.GetHashCode();
        public override string ToString() => (TagName + ", " + Title + ", " + BackgroundColorName + ", " + ForegroundColorName);
        public override bool Equals( object obj )
        {            
            if ( obj is Mark mark )
            {
                return (mark.TagName == this.TagName);
            }
            return base.Equals( obj );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Config
    {
        /// <summary>
        /// 
        /// </summary>
        [Flags] public enum ChosenWordsSelectionMethodEnum
        {
            None                 = 0,

            ItalicFontStyle      = 1,
            OverAndUnderlineLine = 2,
            All                  = ItalicFontStyle | OverAndUnderlineLine,
        }

        static Config()
        {
            #region [.MARK's.]
            for ( var i = 1; ; i++ )
            {
                var configName = "MARK-" + i;
                var markText = ConfigurationManager.AppSettings[ configName ];
                if (markText == null)
                    break;

                var dict = new Dictionary< string, string >();
                foreach ( var s in markText.SplitBy( ';' ) )
                {
                    var n = s.SplitBy( ':' );

                    dict.Add( n[ 0 ].Trim(), n[ 1 ].Trim() );
                }                

                var mark = new Mark( configName, 
                                     dict[ "tag-name" ], 
                                     dict[ "background-color" ],
                                     dict.TryGetValue2( "color", "White" ),
                                     dict.TryGetValue2( "title" ) 
                                   );

                _MarksDictionary.Add( mark.TagName, mark );
            }            
            #endregion

            #region [.save-xsl-transform.]
            SaveXslTransform = new XslCompiledTransform( false );
            using ( var sr = new StringReader( Resources.save ) )
            using ( var xr = new XmlTextReader( sr ) )
            {
                SaveXslTransform.Load( xr );
            }
            #endregion
        }

        private static readonly Dictionary< string, Mark > _MarksDictionary = new Dictionary< string, Mark >();

        public static IEnumerable< Mark > Marks => _MarksDictionary.Values;

        private static string TryGetValue2( this Dictionary< string, string > dict, string key, string defaultValue = null ) => dict.TryGetValue( key, out var value ) ? value : defaultValue;
        public static Brush GetBackgroundBrushByName( this XAttribute attribute ) => _MarksDictionary.TryGetValue( attribute.Value, out var mark ) ? mark.BackgroundBrush : Brushes.Azure;
        public static Brush GetForegroundBrushByName( this XAttribute attribute ) => _MarksDictionary.TryGetValue( attribute.Value, out var mark ) ? mark.ForegroundBrush : Brushes.Black;

        public static readonly XslCompiledTransform SaveXslTransform;
         
        public static readonly bool ShowSplashOnStartup         = ConfigurationManager.AppSettings[ "ShowSplashOnStartup"         ].ToBool();
        public static readonly bool ShowOpenFileDialogOnStarted = ConfigurationManager.AppSettings[ "ShowOpenFileDialogOnStarted" ].ToBool();
        public static readonly bool AutoOpenLastOpenedFile      = ConfigurationManager.AppSettings[ "AutoOpenLastOpenedFile"      ].ToBool();        
        public static readonly int  MaxChosenWordsInlinesCount  = ConfigurationManager.AppSettings[ "MaxChosenWordsInlinesCount"  ].ToInt32();
        public static readonly ChosenWordsSelectionMethodEnum ChosenWordsSelectionMethod = ConfigurationManager.AppSettings[ "ChosenWordsSelectionMethod" ].ToEnum< ChosenWordsSelectionMethodEnum >();
    }
}
