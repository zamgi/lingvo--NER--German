using System.Collections.Generic;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;
using Lingvo.NER.Rules.urls;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.NerPostMerging
{
    /// <summary>
    /// 
    /// </summary>
    internal static class UrlAndEmailMerger
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class word_by_startIndex_Comparer : IComparer< word_t >
        {
            public static word_by_startIndex_Comparer Inst { [M(O.AggressiveInlining)] get; } = new word_by_startIndex_Comparer();
            private word_by_startIndex_Comparer() { }
            public int Compare( word_t x, word_t y ) => (x.startIndex - y.startIndex);
        }

        [M(O.AggressiveInlining)] public  static void MergeWithUrls( List< word_t > words, List< url_t > urls, NerProcessor.UsedRecognizerTypeEnum urt )
        {
            if ( !urls.AnyEx() )
            {
                return;
            }

            if ( urt.Has_Emails() )
            {
                if ( urt.Has_Urls() )
                {
                    MergeWithUrlsAndEmails( words, urls );
                }
                else
                {
                    MergeWithEmails( words, urls );
                }
            }
            else if ( urt.Has_Urls() )
            {
                MergeWithUrls( words, urls );
            }
        }
        [M(O.AggressiveInlining)] public  static void RemoveUrlsIfNotNeed4Recognizer( List< word_t > words, NerProcessor.UsedRecognizerTypeEnum urt )
        {
            if ( !urt.Has_Urls() )
            {
                for ( var i = words.Count - 1; 0 <= i; i-- )
                {
                    if ( words[ i ].nerOutputType == NerOutputType.Url )
                    {
                        words.RemoveAt( i );
                    }
                }
            }
        }

        [M(O.AggressiveInlining)] private static void MergeWithUrls( List< word_t > words, List< url_t > urls )
        {
            var wc = words.Count;
            for ( var i = urls.Count - 1; 0 <= i; i-- )
            {
                var u = urls[ i ];
                if ( u.type == UrlTypeEnum.Url )
                {
                    words.Add( new UrlWord( u ) );
                }
            }

            if ( words.Count != wc )
            {
                words.Sort( word_by_startIndex_Comparer.Inst );
            }
        }
        [M(O.AggressiveInlining)] private static void MergeWithEmails( List< word_t > words, List< url_t > urls )
        {
            var wc = words.Count;
            for ( var i = urls.Count - 1; 0 <= i; i-- )
            {
                var u = urls[ i ];
                if ( u.type == UrlTypeEnum.Email )
                {
                    words.Add( new EmailWord( u ) );
                }
            }

            if ( words.Count != wc )
            {
                words.Sort( word_by_startIndex_Comparer.Inst );
            }
        }
        [M(O.AggressiveInlining)] private static void MergeWithUrlsAndEmails( List< word_t > words, List< url_t > urls )
        {
            for ( var i = urls.Count - 1; 0 <= i; i-- )
            {
                words.Add( UrlOrEmailWordBase.Create( urls[ i ] ) );
            }

            words.Sort( word_by_startIndex_Comparer.Inst );
        }

        /*[M(O.AggressiveInlining)] public static void MergeWithUrls( DirectAccessList< (word_t w, int orderNum) > nerWords, List< url_t > urls )
        {
            if ( urls.AnyEx() )
            {
                for ( var i = urls.Count - 1; 0 <= i; i-- )
                {
                    nerWords.AddByRef( (new UrlWord( urls[ i ] ), -1) );
                }

                //---nerWords.Sort( word_by_startIndex_Comparer.Inst );
                var count = nerWords.Count;
                Array.Sort( nerWords._Items, 0, nerWords.Count, word_by_startIndex_Comparer.Inst );

                var prev_orderNum = -1;
                for ( var i = 0; i < count; i++ )
                {
                    ref var t = ref nerWords._Items[ i ];
                    if ( t.orderNum == -1 )
                    {
                        t.orderNum = prev_orderNum + 1;
                    }
                    prev_orderNum = t.orderNum;
                }
            }
        }*/
    }
}