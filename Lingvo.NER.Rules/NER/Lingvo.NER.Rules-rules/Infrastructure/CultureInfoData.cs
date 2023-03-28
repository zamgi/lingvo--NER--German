using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Lingvo.NER.Rules.core;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules
{

    /// <summary>
    /// 
    /// </summary>
    internal struct CultureInfoData
    {
        private Set< char > _Separators;
        private Set< char > _DecimalSeparators;
        private Set< char > _GroupSeparators;
        private string      _CurrencySymbolUpper;

        private static void tryAdd( Set< char > hs, char c ) 
        {
            if ( !c.IsWhiteSpace() )
            {
                hs.Add( c );
            }
        }

        public CultureInfoData( CultureInfo ci )
        {
            Nfi  = ci.NumberFormat;
            Dtfi = ci.DateTimeFormat;
            TimeSeparator      = Dtfi.TimeSeparator.FirstOrDefault();
            TimeSeparatorArray = new[] { TimeSeparator };
            DateSeparator      = Dtfi.DateSeparator.FirstOrDefault();
            DateSeparatorArray = new[] { DateSeparator };

            _Separators        = new Set< char >( 11 );
            _DecimalSeparators = new Set< char >();
            _GroupSeparators   = new Set< char >();
            

            tryAdd( _Separators, TimeSeparator );
            tryAdd( _Separators, DateSeparator ); AllowWhiteSpaceAsGroupSeparator = false;
            var ch = Nfi.CurrencyGroupSeparator.FirstOrDefault(); tryAdd( _Separators, ch ); tryAdd( _GroupSeparators, ch ); AllowWhiteSpaceAsGroupSeparator |= ch.IsWhiteSpace();
                ch = Nfi.NumberGroupSeparator  .FirstOrDefault(); tryAdd( _Separators, ch ); tryAdd( _GroupSeparators, ch ); AllowWhiteSpaceAsGroupSeparator |= ch.IsWhiteSpace();
                ch = Nfi.PercentGroupSeparator .FirstOrDefault(); tryAdd( _Separators, ch ); tryAdd( _GroupSeparators, ch ); AllowWhiteSpaceAsGroupSeparator |= ch.IsWhiteSpace();

                ch = Nfi.CurrencyDecimalSeparator.FirstOrDefault(); tryAdd( _Separators, ch ); tryAdd( _DecimalSeparators, ch );
                ch = Nfi.NumberDecimalSeparator  .FirstOrDefault(); tryAdd( _Separators, ch ); tryAdd( _DecimalSeparators, ch );
                ch = Nfi.PercentDecimalSeparator .FirstOrDefault(); tryAdd( _Separators, ch ); tryAdd( _DecimalSeparators, ch );

            _CurrencySymbolUpper = Nfi.CurrencySymbol?.ToUpper();
        }
    
        public NumberFormatInfo   Nfi  { [M(O.AggressiveInlining)] get; }
        public DateTimeFormatInfo Dtfi { [M(O.AggressiveInlining)] get; }
        public bool AllowWhiteSpaceAsGroupSeparator { [M(O.AggressiveInlining)] get; }
        public char               TimeSeparator      { [M(O.AggressiveInlining)] get; }
        public char[]             TimeSeparatorArray { [M(O.AggressiveInlining)] get; }
        public char               DateSeparator      { [M(O.AggressiveInlining)] get; }
        public char[]             DateSeparatorArray { [M(O.AggressiveInlining)] get; }

        [M(O.AggressiveInlining)] public bool IsFormatSeparator ( char c ) => _Separators.Contains( c );
        [M(O.AggressiveInlining)] public bool IsDecimalSeparator( char c ) => _DecimalSeparators.Contains( c );
        [M(O.AggressiveInlining)] public bool IsGroupSeparator  ( char c ) => _GroupSeparators.Contains( c );
        public bool HasGroupSeparators { [M(O.AggressiveInlining)] get => (_GroupSeparators.Count != 0); }
        [M(O.AggressiveInlining)] public string[] SplitByTimeSeparator( string s ) => s.Split( TimeSeparatorArray, StringSplitOptions.RemoveEmptyEntries );
        [M(O.AggressiveInlining)] public string[] SplitByDateSeparator( string s ) => s.Split( DateSeparatorArray, StringSplitOptions.RemoveEmptyEntries );
        [M(O.AggressiveInlining)] public bool IsCurrencySymbol( string s ) => (s == _CurrencySymbolUpper);
    }
}
