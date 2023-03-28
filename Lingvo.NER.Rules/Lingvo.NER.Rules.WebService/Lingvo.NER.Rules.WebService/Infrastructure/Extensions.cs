using System;
using System.Collections.Generic;
using System.Linq;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        [M(O.AggressiveInlining)] public static string Cut( this string s, int maxlen = 250 ) => string.IsNullOrEmpty( s ) ? s : ((s.Length <= maxlen) ? s : s.Substring( 0, maxlen - 3 ) + "...");
        [M(O.AggressiveInlining)] public static bool AnyEx< T >( this IEnumerable< T > seq ) => (seq != null && seq.Any());
        [M(O.AggressiveInlining)] public static void Dispose_NoThrow( this IDisposable disposable )
        {
            try
            {
                disposable?.Dispose();
            }
            catch
            {
                ;
            }
        }
        [M(O.AggressiveInlining)] public static string AsText( in this TimeSpan ts ) => ts.ToString( (1.0 < Math.Abs( ts.TotalDays )) ? "dd\\,hh\\:mm\\:ss" : "hh\\:mm\\:ss" );
        [M(O.AggressiveInlining)] public static string AsText( in this DateTime dt ) => dt.ToString( "dd.MM.yyyy HH:mm:ss" );
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class CONSOLE
    {
        public static void WriteLine( string text, ConsoleColor? foregroundColor = null )
        {
            lock ( typeof(CONSOLE) )
            {
                if ( foregroundColor.HasValue )
                {
                    var fc = Console.ForegroundColor;
                    Console.ForegroundColor = foregroundColor.Value;
                    Console.WriteLine( text );
                    Console.ForegroundColor = fc;
                }
                else
                {
                    Console.WriteLine( text );
                }
            }
        }
        public static void Write( string text, ConsoleColor? foregroundColor = null )
        {
            lock ( typeof(CONSOLE) )
            {
                if ( foregroundColor.HasValue )
                {
                    var fc = Console.ForegroundColor;
                    Console.ForegroundColor = foregroundColor.Value;
                    Console.Write( text );
                    Console.ForegroundColor = fc;
                }
                else
                {
                    Console.Write( text );
                }
            }
        }
        public static void WriteLineError( string text )
        {
            lock ( typeof(CONSOLE) )
            {
                var fc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + text );
                Console.ForegroundColor = fc;
            }
        }
        public static void WriteError( string text )
        {
            lock ( typeof(CONSOLE) )
            {
                var fc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write( text );
                Console.ForegroundColor = fc;
            }
        }

        public static string ReadLine() => Console.ReadLine();
    }
}