using System;
using System.Collections.Generic;

using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.core
{
    /// <summary>
    /// 
    /// </summary>
    unsafe internal static class Extensions
    {
        private static CharType* _CTM; // [need use 'xlat.CHARTYPE_MAP' for debug]
        static Extensions() => _CTM = xlat_Unsafe.Inst._CHARTYPE_MAP;

        public static void ThrowIfNull( this object obj, string paramName )
        {
            if ( obj == null ) throw (new ArgumentNullException( paramName ));
        }
        public static void ThrowIfNullOrWhiteSpace( this string text, string paramName )
        {
            if ( text.IsNullOrWhiteSpace() ) throw (new ArgumentNullException( paramName ));
        }
        //public static void ThrowIfNullOrWhiteSpaceAnyElement( this ICollection< string > collection, string paramName )
        //{
        //    if ( collection == null ) throw (new ArgumentNullException( paramName ));
        //    foreach ( var c in collection )
        //    {
        //        if ( string.IsNullOrWhiteSpace( c ) ) throw (new ArgumentNullException( paramName + " => some collection element is NULL-or-WhiteSpace" ));
        //    }
        //}

        //public static Set< T > ToSet< T >( this IList< T > lst, IEqualityComparer< T > comparer = null ) => new Set< T >( lst, comparer );
        public static Set< T > ToSet< T >( this IEnumerable< T > seq, IEqualityComparer< T > comparer = null ) => new Set< T >( seq, comparer: comparer );
        public static T[] ToArray< T >( this IEnumerable< T > seq, int count )
        {
            var array = new T[ count ];
            count = 0;
            foreach ( var t in seq )
            {
                array[ count++ ] = t;
            }
            return (array);
        }
        public static List< T > ToList< T >( this IEnumerable< T > seq, int capacity )
        {
            var lst = new List< T >( capacity );
            foreach ( var t in seq )
            {
                lst.Add( t );
            }
            return (lst);
        }
        public static List< T > SortEx< T >( this List< T > lst )
        {
            lst.Sort();
            return (lst);
        }

        [M(O.AggressiveInlining)] public static bool ContainsAny( this string s, char[] anyOf ) => (s.IndexOfAny( anyOf ) != -1);
        [M(O.AggressiveInlining)] public static bool IsNullOrWhiteSpace( this string text ) => string.IsNullOrWhiteSpace( text );
        [M(O.AggressiveInlining)] public static bool IsNullOrEmpty( this string text ) => string.IsNullOrEmpty( text );
        //public static int GetItemMinLength( this ICollection< string > coll ) => ((coll.Count != 0) ? coll.Min( d => d.Length ) : 0);
        //public static int GetItemMaxLength( this ICollection< string > coll ) => ((coll.Count != 0) ? coll.Max( d => d.Length ) : 0);
        [M(O.AggressiveInlining)] public static bool AnyEx< T >( this IList< T > lst ) => (lst != null) && (0 < lst.Count);
        //[M(O.AggressiveInlining)] public static bool AnyEx< T >( this IEnumerable< T > seq ) => (seq != null) && seq.Any();
        
        public static void RemoveWhereValueOriginalIsNull( this List< word_t > words )
        {
            for ( int i = words.Count - 1; 0 <= i; i-- )
            {
                if ( words[ i ].valueOriginal == null )
                {
                    words.RemoveAt_Ex( i );
                }
            }
        }
        public static void RemoveWhereValueOriginalIsNull( this List< word_t > words, out int removeCount )
        {
            removeCount = 0;
            for ( int i = words.Count - 1; 0 <= i; i-- )
            {
                if ( words[ i ].valueOriginal == null )
                {
                    words.RemoveAt_Ex( i );
                    removeCount++;
                }
            }
        }
        [M(O.AggressiveInlining)] public static void RemoveAt_Ex( this List< word_t > words, int removeIndex )
        {
            //-1-//
            //var w = words[ removeIndex ];
            //---w.removeFromChain();

            //-2-//
            words.RemoveAt( removeIndex );
        }

        [M(O.AggressiveInlining)] public static char FirstChar( this string s ) => s[ 0 ];
        [M(O.AggressiveInlining)] public static char LastChar( this string s ) => s[ s.Length - 1 ];

        [M(O.AggressiveInlining)] public static void ClearValuesAndNerChain( this word_t w )
        {
            w.valueUpper = w.valueOriginal = null;
            w.ResetNextPrev();
        }
        [M(O.AggressiveInlining)] public static void ClearOutputTypeAndNerChain( this word_t w )
        {
            w.nerOutputType = NerOutputType.Other;
            w.ResetNextPrev();
        }

        //[M(O.AggressiveInlining)] public static bool IsHyphen( this CharType ct ) => ((ct & CharType.IsHyphen) == CharType.IsHyphen);
        [M(O.AggressiveInlining)] public static bool IsHyphen( this char ch ) => ((_CTM[ ch ] & CharType.IsHyphen) == CharType.IsHyphen);
        [M(O.AggressiveInlining)] public static bool IsDigit( this CharType ct ) => ((ct & CharType.IsDigit) == CharType.IsDigit);
        [M(O.AggressiveInlining)] public static bool IsDigit( this char ch ) => ((_CTM[ ch ] & CharType.IsDigit) == CharType.IsDigit);
        [M(O.AggressiveInlining)] public static bool IsUpper( this CharType ct ) => ((ct & CharType.IsUpper) == CharType.IsUpper);        
        [M(O.AggressiveInlining)] public static bool IsUpperLetter( this CharType ct ) => ((ct & CharType.IsLetter) == CharType.IsLetter) && ((ct & CharType.IsUpper) == CharType.IsUpper);
        [M(O.AggressiveInlining)] public static bool IsUpperLetter( this char ch )
        {
            var ct = _CTM[ ch ];
            return ((ct & CharType.IsLetter) == CharType.IsLetter) && ((ct & CharType.IsUpper) == CharType.IsUpper);
        }
        [M(O.AggressiveInlining)] public static bool IsLetter( this CharType ct ) => ((ct & CharType.IsLetter) == CharType.IsLetter);

        [M(O.AggressiveInlining)] public static bool IsWhiteSpace( this char ch ) => ((_CTM[ ch ] & CharType.IsWhiteSpace) == CharType.IsWhiteSpace);
        [M(O.AggressiveInlining)] public static bool IsWhiteSpace( this CharType ct ) => ((ct & CharType.IsWhiteSpace) == CharType.IsWhiteSpace);
        [M(O.AggressiveInlining)] public static bool IsLetter( this char ch ) => ((_CTM[ ch ] & CharType.IsLetter) == CharType.IsLetter);
        [M(O.AggressiveInlining)] public static bool IsLetterOrDigit( this char ch )
        {
            var ct = _CTM[ ch ];
            return ((ct & CharType.IsLetter) == CharType.IsLetter) || ((ct & CharType.IsDigit) == CharType.IsDigit);
        }
        [M(O.AggressiveInlining)] public static bool IsPunctuation( this char ch ) => ((_CTM[ ch ] & CharType.IsPunctuation) == CharType.IsPunctuation);
        [M(O.AggressiveInlining)] public static bool IsPunctuation( this CharType ct ) => ((ct & CharType.IsPunctuation) == CharType.IsPunctuation);

        [M(O.AggressiveInlining)] public static bool IsOutputTypeOther( this word_t w ) => (w.nerOutputType == NerOutputType.Other);
        [M(O.AggressiveInlining)] public static bool IsOutputTypeName( this word_t w ) => (w.nerOutputType == NerOutputType.Name);

        [M(O.AggressiveInlining)] public static bool IsInputTypeNum( this word_t w ) => (w.nerInputType == NerInputType.Num);
        [M(O.AggressiveInlining)] public static bool IsExtraWordTypeIntegerNumber( this word_t w ) => (w.extraWordType == ExtraWordType.IntegerNumber);
        [M(O.AggressiveInlining)] public static bool IsExtraWordTypeColon( this word_t w ) => ((w.extraWordType & ExtraWordType.Colon) == ExtraWordType.Colon);
        [M(O.AggressiveInlining)] public static bool IsExtraWordTypeComma( this word_t w ) => ((w.extraWordType & ExtraWordType.Comma) == ExtraWordType.Comma);
        [M(O.AggressiveInlining)] public static bool IsExtraWordTypeDash( this word_t w ) => ((w.extraWordType & ExtraWordType.Dash) == ExtraWordType.Dash);
        [M(O.AggressiveInlining)] public static bool IsExtraWordTypePunctuation( this word_t w ) => ((w.extraWordType & ExtraWordType.Punctuation) == ExtraWordType.Punctuation);
        [M(O.AggressiveInlining)] public static bool IsExtraWordTypeHasUmlautes( this word_t w ) => ((w.extraWordType & ExtraWordType.HasUmlautes) == ExtraWordType.HasUmlautes);
    }
}
