using System.IO;
using System.Net;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        //public static bool IsNullOrEmpty( this string s ) => string.IsNullOrEmpty( s );
        //public static bool IsNullOrWhiteSpace( this string s ) => string.IsNullOrWhiteSpace( s );
        public static string Cut( this string text, bool straighten = true, int max_len = 2048 )
        {
            if ( text != null )
            {
                if ( straighten ) text = text.Replace( "\r\n", " " ).Replace( "\r", string.Empty ).Replace( '\n', ' ' );
                return ((max_len < text.Length) ? text.Substring( 0, max_len ) + "..." : text);
            }
            return (text);            
        }

        [M(O.AggressiveInlining)] public static TextWriter WR( this TextWriter tw, string s )
        {
            tw.Write( s );
            return (tw);
        }
        [M(O.AggressiveInlining)] public static TextWriter WR( this TextWriter tw, int i )
        {
            tw.Write( i );
            return (tw);
        }
        [M(O.AggressiveInlining)] public static TextWriter WR_Escape( this TextWriter tw, string s )
        {
            WebUtility.HtmlEncode( s, tw );
            return (tw);
        }
        [M(O.AggressiveInlining)] public static string Escape( this string s ) => WebUtility.HtmlEncode( s );
    }
}