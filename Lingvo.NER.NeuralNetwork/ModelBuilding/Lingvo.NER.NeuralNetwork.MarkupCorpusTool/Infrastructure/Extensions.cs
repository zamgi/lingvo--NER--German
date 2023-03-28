using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace Lingvo.NER.NeuralNetwork.MarkupCorpusTool
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        //public static readonly NumberFormatInfo NF = new NumberFormatInfo() { NumberDecimalSeparator = "." };

        public static bool IsEmptyOrNull( this string text ) => string.IsNullOrEmpty( text );
        public static bool IsNullOrWhiteSpace( this string text ) => string.IsNullOrWhiteSpace( text );
        public static bool IsEmptyOrNull( this StringBuilder sb ) => (sb == null || sb.Length == 0);    
        public static bool AnyEx< T >( this IList< T > list ) => ((list != null) && (0 < list.Count));

        //public static string InBrackets( this string text ) => (text.IsEmptyOrNull() ? text : ('[' + text + ']'));
        public static string InSingleQuotes( this string text ) => (string.IsNullOrEmpty( text ) ? text : ('\'' + text + '\''));
        public static StringBuilder RemoveLastChars( this StringBuilder sb, int count = 1 )
        {
            if ( sb.Length == 0 )
                return (sb);

            if ( count < sb.Length )
                return (sb.Remove( sb.Length - count, count ));

            sb.Length = 0;
            return (sb);
        }
        public static void Trancate( this StringBuilder sb ) => sb.Length = 0;

        public static string[] SplitBy( this string text, params char[] chars ) => text.Split( chars, StringSplitOptions.RemoveEmptyEntries );
        public static string[] SplitByRN( this string text ) => text.Split( new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries );
        public static string[] SplitBySpace( this string text ) => text.Split( new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries );

        public static T     ToEnum< T >( this string value ) where T : struct => ((T)Enum.Parse(typeof(T), value));
        public static int   ToInt32( this string value ) => int.Parse( value );
        //public static float ToFloat( this string value ) => float.Parse( value, NF );
        public static bool  ToBool( this string value ) => bool.Parse( value );
        //public static int?  TryToInt32( this string value ) => (int.TryParse( value, out var i ) ? i : (int?)null);

        public static XElement ToXElement( this string value )
        {
            try
            {
                return (XElement.Parse( value, LoadOptions.PreserveWhitespace ));
            }
            catch ( XmlException /*ex*/ )
            {
                return (XElement.Parse( value.Replace( "&", "&amp;" ), LoadOptions.PreserveWhitespace ));
            }
        }
        //public static string InnerHTML( this XElement element, string childrenSeparator = " " ) => string.Join( childrenSeparator, (element.Nodes().Select( e => e.ToString( SaveOptions.DisableFormatting ) )).ToArray() );

        public static void SetAttribute( this XElement element, string attributeName, string attributeValue )
        {
            var a = element.Attribute( attributeName );
            if ( a == null )
            {
                element.Add( new XAttribute( attributeName, attributeValue ) );
            }
            else
            {
                a.Value = attributeValue;
            }
        }
        public static void RemoveAttribute( this XElement element, string attributeName )
        {
            var a = element.Attribute( attributeName );
            if ( a != null )
            {
                a.Remove();
            }
        }
        public static void AddXTextSpace( this XElement element ) => element.Add( " " );

        public static void AddRunSpace( this ICollection< Inline > inlines ) => inlines.Add( new Run( " " ) );
        public static bool IsRunSpace( this Run run ) => ((run.Text == " ") && (run.Tag == null));
        public static bool IsRunSpace( this Inline inline ) => ((inline is Run run) ? run.IsRunSpace() : false);
        public static void AddLineBreak( this ICollection< Inline > inlines ) => inlines.Add( new LineBreak() );
        public static Run ToRun( this XElement element )
        {
            var run = new Run( element.Value ) ;
            var n = element.Attribute( "n" );
            if ( n != null )
            {
                run.Tag = n.Value.ToInt32();
            }
            return (run);
        }
        public static Run ToRun( this XElement element, Brush background )
        {
            var run = element.ToRun();
                run.Background = background;
            return (run);
        }
        public static Run ToRun( this XElement element, Brush background, Brush foreground )
        {
            var run = element.ToRun();
                run.Background = background;
                run.Foreground = foreground;
            return (run);
        }

        //public static string CombinePath( this string path1, string path2 ) => Path.Combine( path1, path2 );

        //public static string ToHtmlAttributeEncode( this string s ) => HttpUtility.HtmlAttributeEncode( s );
        public static string ToHtmlEncode( this string s ) => HttpUtility.HtmlEncode( s );
        public static string ToHtmlEncodeForce( this string s )
        {
            var sb = new StringBuilder( s.ToHtmlEncode() );
            for ( var i = 0; i < s.Length; i++ )
            {
                var ch = sb[ i ];
                if ( ch < 0x20 )
                {
                    sb[ i ] = (char) 0x20;
                }
            }
            return (sb.ToString());
        }

        public static bool FileExists(this string fileName) => (!fileName.IsEmptyOrNull() &&  File.Exists( fileName ));
        public static string MinimizePath( this string path, int charsCountWithoutEllipsis )
        {           
            if ( charsCountWithoutEllipsis < path.Length )
            {
                var minimizedPath = path;
                var array = minimizedPath.Split('\\');
                for ( int j = array.Length - 2; charsCountWithoutEllipsis < minimizedPath.Length && 0 < j; j-- )
                {
                    minimizedPath = string.Join( "\\", array, 0, j );
                    for ( int i = j; i < array.Length - 1; i++ )
                    {
                        minimizedPath += "\\...";
                    }
                    minimizedPath += '\\' + array[ array.Length - 1 ];
                }
                return (minimizedPath);
            }
            return (path);
        }

        public static void TrySetSelectedValue( this ComboBox comboBox, string value )
        {
            foreach ( ComboBoxItem item in comboBox.Items )
            {
                if ( value.CompareTo( item.Content ) == 0 )
                {
                    item.IsSelected = true;
                    break;
                }
            }
        }

        public static T TryGetValue< TKey, T >( this Dictionary< TKey, T > dict, TKey key, T defaultT = default ) => (dict.TryGetValue( key, out var t ) ? t : defaultT);
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class AssemblyInfoHelper
    {
        public static string AssemblyTitle
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof(AssemblyTitleAttribute), false );
                if ( 0 < attributes.Length )
                {
                    var titleAttribute = (AssemblyTitleAttribute) attributes[ 0 ];
                    if ( !string.IsNullOrEmpty( titleAttribute.Title ) )
                    {
                        return (titleAttribute.Title);
                    }
                }
                return (Path.GetFileNameWithoutExtension( Assembly.GetExecutingAssembly().CodeBase )); 
            }
        }
        public static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static string AssemblyCopyright
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyCopyrightAttribute ), false );
                if ( 0 < attributes.Length )
                {
                    return ((AssemblyCopyrightAttribute) attributes[ 0 ]).Copyright;
                }
                return (string.Empty);
            }
        }
        public static string AssemblyLastWriteTime => File.GetLastWriteTime( Assembly.GetExecutingAssembly().Location ).ToString( "dd.MM.yyyy HH:mm" );
        //public static string AssemblyDescription
        //{
        //    get
        //    {
        //        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof(AssemblyDescriptionAttribute), false );
        //        if ( 0 < attributes.Length )
        //        {
        //            return ((AssemblyDescriptionAttribute) attributes[ 0 ]).Description; 
        //        }
        //        return (string.Empty);
        //    }
        //}
        //public static string AssemblyProduct
        //{
        //    get
        //    {
        //        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof(AssemblyProductAttribute), false );
        //        if ( 0 < attributes.Length )
        //        {
        //            return ((AssemblyProductAttribute) attributes[ 0 ]).Product;
        //        }
        //        return (string.Empty); 
        //    }
        //}
        //public static string AssemblyCompany
        //{
        //    get
        //    {
        //        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof(AssemblyCompanyAttribute), false );
        //        if ( 0 < attributes.Length )
        //        {
        //            return ((AssemblyCompanyAttribute) attributes[ 0 ]).Company;
        //        }
        //        return (string.Empty); 
        //    }
        //}        
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class RgbColorParser
    {
        public static bool TryParse( string rgb_color, out (byte r, byte g, byte b) rgb )
        {
            var regex = new Regex( @"rgb\((?<r>\d{1,3}),(?<g>\d{1,3}),(?<b>\d{1,3})\)" );
            var match = regex.Match( rgb_color?.Replace( " ", string.Empty ).ToLowerInvariant() ?? string.Empty );
            if ( match.Success )
            {
                if ( byte.TryParse( match.Groups[ "r" ].Value, out var r ) &&
                     byte.TryParse( match.Groups[ "g" ].Value, out var g ) &&
                     byte.TryParse( match.Groups[ "b" ].Value, out var b ) )
                {
                    rgb = (r, g, b);
                    return (true);
                }
            }
            rgb = default;
            return (false);
        }
    }
}
