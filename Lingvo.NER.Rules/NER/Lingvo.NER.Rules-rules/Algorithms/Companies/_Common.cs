using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Companies
{
    /// <summary>
    /// 
    /// </summary>
    internal readonly struct SearchResult
    {
        [M(O.AggressiveInlining)] public SearchResult( int startIndex, int length )
        {
            StartIndex = startIndex;
            Length     = length;
        }
        public int StartIndex { [M(O.AggressiveInlining)] get; }
        public int Length     { [M(O.AggressiveInlining)] get; }
        [M(O.AggressiveInlining)] public int EndIndex() => StartIndex + Length;
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}]";
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal static class CompaniesExtensions
    {
        private static CharType* _CTM;
        static CompaniesExtensions() => _CTM = xlat_Unsafe.Inst._CHARTYPE_MAP;

        [M(O.AggressiveInlining)] public static bool IsFirstLetterUpper( this word_t w )
        {
            switch ( w.nerInputType )
            {
                #region [.common.]
                case NerInputType.AllCapital:         // Все заглавные буквы (больше одной) [МТС]
                case NerInputType.LatinCapital:       // Только первая заглавная на латинице [Fox]
                case NerInputType.MixCapital:         // Смешенные заглавные и прописные буквы; 
                                                      //русский   : {латиница + кириллица [СевКавГПУ]}, 
                                                      //английский: {заглавные и строчные, первая буква - заглавная, между буквами может быть тире, точка: St.-Petersburg , FireFox, Google.Maps}
                case NerInputType.MixCapitalWithDot:  // Все заглавные буквы (больше одной) подряд с точкой (точками) [V.IV.I.PA]
                ////case NerInputType.NumCapital:         // Начинается с заглавной буквы и содержит хотябы одну цифру [МИГ-21]
                case NerInputType.OneCapital:         // Одна заглавная буква без точки [F]
                case NerInputType.OneCapitalWithDot:  // одна заглавная буква с точкой [F.]        
                #endregion

                //#region [.russian-language.]
                //case NerInputType.AllLatinCapital: // все буквы заглавные и все на латинице [POP]
                //case NerInputType.FirstCapital:    // Только первая заглавная на кириллице [Вася]            
                //#endregion

                #region [.english-language.]
                case NerInputType.AllCapitalWithDot: // все заглавные буквы (больше одной) с точкой (точками), без тире: [U.N.]
                case NerInputType.LatinFirstCapital: // только первая заглавная:  [Thatcher]
                #endregion

                    return (true);
                default:
                    return (false);
            }
        }
        [M(O.AggressiveInlining)] public static bool IsUpperAbbreviation( this word_t w, int minLength )
        {
            if ( minLength <= w.length )
            {
                switch ( w.nerInputType )
                {
                    case NerInputType.AllCapital: // Все заглавные буквы (больше одной) [МТС]
                        return (true);

                    case NerInputType.MixCapital:
                        fixed ( char* ptr = w.valueOriginal )
                        {
                            for ( var i = w.length - 1; 0 <= i; i-- )
                            {
                                var ch = ptr[ i ];
                                var ct = _CTM[ ch ];
                                if ( !ct.IsUpperLetter() && !ct.IsPunctuation() && (ch != '+') )
                                    return (false);
                            }
                            return (true);
                        }
                }
            }
            return (false);
        }
        [M(O.AggressiveInlining)] public static bool IsContainsLetters( this word_t w )
        {
#if DEBUG
            [M(O.AggressiveInlining)] static bool is_contains_letters( word_t w )
            {
                fixed ( char* ptr = w.valueOriginal )
                {
                    for ( var i = w.length - 1; 0 <= i; i-- )
                    {
                        if ( _CTM[ ptr[ i ] ].IsLetter() )
                            return (true);
                    }
                    return (false);
                }
            };
#endif
            switch ( w.nerInputType )
            {
                case NerInputType.LatinNum:
                case NerInputType.Quote:
                case NerInputType.Num:
                case NerInputType.Comma:
#if DEBUG
                    Debug.Assert( !is_contains_letters( w ) );
#endif
                    return (false);

                default:
#if DEBUG
                    var has = (w.extraWordType == ExtraWordType.HasUmlautes) || (w.extraWordType == ExtraWordType.Other);
                    Debug.Assert( (has && is_contains_letters( w )) || !is_contains_letters( w ) );
                    return (has);
#else
                    return ((w.extraWordType == ExtraWordType.HasUmlautes) || (w.extraWordType == ExtraWordType.Other));
#endif                    
            }
        }

        [M(O.AggressiveInlining)] public static bool IsOutputTypeOtherOrUrl( this word_t w ) => (w.nerOutputType == NerOutputType.Other) || (w.nerOutputType == NerOutputType.Url);

        [M(O.AggressiveInlining)] public static bool Contains( this IWcd_Find2Right wcd, IList< word_t > words, int startIndex ) => wcd.TryFind2Right( words, startIndex, out var _ );
        [M(O.AggressiveInlining)] public static string JoinWithSep( this IList< string > array, StringBuilder buf, char sep = ' ' )
        {
            buf.Clear();
            foreach ( var a in array )
            {
                //if ( buf.Length != 0 ) buf.Append( sep );
                buf.Append( a ).Append( sep );
            }
            return (buf.ToString());
        }
        [M(O.AggressiveInlining)] public static string Join( this IList< string > array, StringBuilder buf )
        {
            buf.Clear();
            foreach ( var a in array )
            {
                buf.Append( a );
            }
            return (buf.ToString());
        }
        [M(O.AggressiveInlining)] public static string JoinWithTrimWhitespaces( this IList< string > array, StringBuilder buf )
        {
            buf.Clear();
            foreach ( var s in array )
            {
                var i = 0;
                var end = s.Length - 1;
                if ( end < 0 ) continue;
                for ( ; i <= end; i++ )
                {
                    if ( !s[ i ].IsWhiteSpace() )
                        break;
                }
                for ( ; 0 <= end; end-- )
                {
                    if ( !s[ end ].IsWhiteSpace() )
                        break;
                }

                var len = end + 1 - i;
                if ( 0 < len )
                {
                    buf.Append( s, i, len );
                }
            }
            return (buf.ToString());
        }
        [M(O.AggressiveInlining)] public static string RemoveWhitespaces( this string s, StringBuilder buf )
        {
            buf.Clear();
            foreach ( var ch in s )
            {
                if ( !_CTM[ ch ].IsWhiteSpace() )
                {
                    buf.Append( ch );
                }
            }
            return (buf.ToString());
        }

        [M(O.AggressiveInlining)] public static List< word_t > RemoveLast( this List< word_t > words )
        {
            var i = words.Count - 1;
            if ( 0 <= i )
            {
                words.RemoveAt_Ex( i );
            }
            return (words);
        }
        [M(O.AggressiveInlining)] public static bool ContainsInBetween( this string s, char ch )
        {
            var i = s.IndexOf( ch );
            return (i != -1) && (i != 0) && (i != s.Length - 1);
        }
    }
}
