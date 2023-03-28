﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Lingvo.NER.NeuralNetwork
{
    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static bool IsNullOrEmpty( this string s ) => string.IsNullOrEmpty( s );
        public static bool IsNullOrWhiteSpace( this string s ) => string.IsNullOrWhiteSpace( s );
        public static TimeSpan StopElapsed( this Stopwatch sw )
        {
            sw.Stop();
            return (sw.Elapsed);
        }
        public static ConfiguredTaskAwaitable< T > CAX< T >( this Task< T > task ) => task.ConfigureAwait( false );
        public static ConfiguredTaskAwaitable CAX( this Task task ) => task.ConfigureAwait( false );
        public static void Cancel_NoThrow( this CancellationTokenSource cts )
        {
            try
            {
                cts.Cancel();
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( ex );//suppress
            }
        }
        //public static void AddWithLock< K, T >( this IDictionary< K, T > d, K k, T t )
        //{
        //    lock ( d )
        //    {
        //        d.Add( k, t );
        //    }
        //}
        public static bool AnyEx< T >( this IList< T > lst ) => (lst != null) && (0 < lst.Count);
        public static bool AnyEx< T >( this IReadOnlyCollection< T > lst ) => (lst != null) && (0 < lst.Count);
        public static bool AnyEx< T >( this IReadOnlyList< T > lst ) => (lst != null) && (0 < lst.Count);
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

