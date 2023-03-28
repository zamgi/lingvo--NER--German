using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.Names;
using Lingvo.NER.Rules.tokenizing;
using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.MaritalStatuses_bak
{
    /// <summary>
    ///
    /// </summary>
    unsafe internal sealed class MaritalStatusesSearcher_ByTextPreamble
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class WordsChainDictionary
        {
            private Map< string, WordsChainDictionary > _Slots;
            private bool _IsLeaf;
            public WordsChainDictionary( int capacity ) => _Slots = Map< string, WordsChainDictionary >.CreateWithCloserCapacity( capacity );
            public WordsChainDictionary() => _Slots = new Map< string, WordsChainDictionary >();

            #region [.append words.]
            public void Add( IList< word_t > words )
            {
                var idx = 0;
                var count = words.Count;
                for ( WordsChainDictionary _this = this, _this_next; ; )
                {
                    #region [.save.]
                    if ( count == idx )
	                {
                        _this._IsLeaf = true;
                        return;
                    }
                    #endregion

                    var v = words[ idx ].valueUpper;
                    if ( !_this._Slots.TryGetValue( v, out _this_next ) )
                    {
                        //add next word in chain
                        _this_next = new WordsChainDictionary();
                        _this._Slots.Add( v, _this_next );
                    }                
                    _this = _this_next;
                    idx++;
                }
            }
            #endregion

            #region [.try get.]
            [M(O.AggressiveInlining)] public bool TryGetFirst( IList< word_t > words, int startIndex, out int length )
            {
                length = default;

                var startIndex_saved = startIndex;
                var count = words.Count;
                for ( WordsChainDictionary _this = this, _this_next; ; )
                {
                    #region [.get.]
                    if ( _this._IsLeaf )
                    {
                        length = (startIndex - startIndex_saved);
                        //--return (true);
                    }
                    #endregion

                    if ( count <= startIndex )
                    {
                        break;
                    }

                    if ( !_this._Slots.TryGetValue( words[ startIndex ].valueUpper, out _this_next ) )
                    {
                        break;
                    }
                    _this = _this_next;
                    startIndex++;
                }

                return (length != 0);
            }
            #endregion
    #if DEBUG
            public override string ToString() => (_IsLeaf ? "true" : $"count: {_Slots.Count}");
    #endif
        }

        #region [.cctor().]
        private static WordsChainDictionary _TextPreambles;
        private static WordsChainDictionary _MaritalStatuses;
        private static Set< char >          _AllowedPunctuation_AfterTextPreamble;
        static MaritalStatusesSearcher_ByTextPreamble()
        {
            Init();

            _AllowedPunctuation_AfterTextPreamble = xlat.GetHyphens().Concat( new[] { ':' } ).ToSet();
        }

        private static void Init()
        {
            using var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate();

            #region [.TextPreambles.]
            var words = new[] 
            {
                "Familienstand",
                "Beziehung",
                "Beziehungsstatus",
                "Beziehungsstand",
                "Beziehungsverhältnis",
                "Verhältnis",
                "Partnerbeziehung",
                "Lebensverhältnis",
                "Lebensverhältnisse",
                "Verheiratet (ja/nein)",
                "Personenstand",
                "Status",
                "Marital status",
                "Relationship",
                "Relationship status",
                "Partner relationship",
                "Life relationship",
                "Living relationship",
                "Married (yes/no)",
                "Civil status",
            };

            _TextPreambles = CreateWordsChainDictionary( tokenizer, words );
            #endregion

            #region [.MaritalStatuses.]
            words = new[]
            {
                "ledig",
                "verheiratet",
                "verlobt",
                "geschieden",
                "getrennt",
                "verwitwet",
                "in einer Lebensgemeinschaft",
                "eingetragene Lebenspartnerschaft",
                "eingetragene Lebenspartnerin/eingetragener Lebenspartner verstorben",
                "eingetragene Lebenspartnerin / eingetragener Lebenspartner verstorben",
                "eingetragene Lebenspartnerin verstorben",
                "eingetragener Lebenspartner verstorben",
                "eingetragene Lebenspartnerschaft aufgehoben",
                "solo",
                "liiert",
                "Single",
                "Mingle",
                "Ehe",
                "Ehegemeinschaft",
                "in einer Beziehung",
                "in einer Beziehung lebend",
                "in fester Beziehung",
                "in fester Beziehung lebend",
                "in einer Wohngemeinschaft",
                "zusammen lebend",
                "zusammenlebend",
                "nicht zusammen lebend",
                "nicht zusammenlebend",
                "allein lebend",
                "alleinlebend",
                "keine Angabe",
                "es ist kompliziert",
                "in einer offenen Beziehung",
                "Wohngemeinschaft",
                "in fester Partnerschaft",
                "mit dem Lebenspartner zusammen lebend",
                "mit dem Lebenspartner zusammenlebend",
                "mit der Lebenspartnerin zusammen lebend",
                "mit der Lebenspartnerin zusammenlebend",
                "durch Tod aufgelöste Lebenspartnerschaft",
                "aufgehobene Lebenspartnerschaft",
                "durch Todeserklärung aufgelöste Lebenspartnerschaft",
                "nicht bekannt",
                "unbekannt",
                "LD",
                "VH",
                "VW",
                "GS",
                "EA",
                "LP",
                "LV",
                "LA",
                "LE",
                "NB",
                "Witwer",
                "Witwe",
                "Lebens- und Einstandsgemeinschaft",
                "Lebensgemeinschaft",
                "Einstandsgemeinschaft",
                "Bedarfsgemeinschaft",
                "verpartnert",
                "single",
                "married",
                "engaged",
                "divorced",
                "separated",
                "widowed",
                "in a cohabitation",
                "registered life partnership",
                "registered life partner deceased",
                "registered civil partnership annulled",
                "solo",
                "mingle",
                "in a relationship",
                "living in a relationship",
                "living in a flat share",
                "living together",
                "not living together",
                "not specified",
                "it is complicated",
                "in an open relationship",
                "in a steady partnership",
                "living together with the life partner",
                "living together with the partner",
                "civil partnership dissolved by death",
                "dissolved civil partnership",
                "civil partnership dissolved by declaration of death",
                "not known",
                "unknown",
            };

            _MaritalStatuses = CreateWordsChainDictionary( tokenizer, words );
            #endregion
        }
        private static WordsChainDictionary CreateWordsChainDictionary( Tokenizer tokenizer, string[] words )
        {
            var wcd = new WordsChainDictionary( words.Length );

            foreach ( var w in words )
            {
                var tokens = tokenizer.Run_NoSentsNoUrlsAllocate( w );
                wcd.Add( tokens ); //.Add_WithDot( tokens );

                if ( tokens.Last().valueUpper.LastChar() == '.' )
                {
                    tokens = tokenizer.Run_NoSentsNoUrlsAllocate( w + " 1234567" );
                    tokens.RemoveAt_Ex( tokens.Count - 1 );
                    wcd.Add( tokens ); //.Add_WithDot( tokenizer.Run_NoSentsNoUrlsAllocate( w + " ." ) );
                }
            }

            return (wcd);
        }
        #endregion

        #region [.ctor().]
        private StringBuilder _Buffer;
        public MaritalStatusesSearcher_ByTextPreamble() => _Buffer = new StringBuilder( 100 );
        #endregion

        [M(O.AggressiveInlining)] private static bool IsAllowedPunctuation_AfterTextPreamble( word_t w ) => ((w.length == 1) && _AllowedPunctuation_AfterTextPreamble.Contains( w.valueUpper[ 0 ] ));

        public void Run( List< word_t > words )
        {
            const int MAX_BETWEEN_WORDS_COUNT           = 25;
            const int MAX_TO_MARITAL_STATUS_WORDS_COUNT = 5;

            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                var w = words[ index ];
                if ( !w.IsOutputTypeName() )
                {
                    continue;
                }                
                var nameWordIndex = index;
                //-----------------------------------------------------------//

                for ( index++; index < len; index++ )
                {
                    if ( MAX_BETWEEN_WORDS_COUNT < (index - nameWordIndex) )
                    {
                        break;
                    }

                    w = words[ index ];
                    if ( w.IsOutputTypeName() )
                    {
                        nameWordIndex = index;
                        continue;
                    }
                    if ( !w.IsOutputTypeOther() )
                    {
                        continue;
                    }
                    if ( !_TextPreambles.TryGetFirst( words, index, out var length ) )
                    {
                        continue;
                    }

                    var startIndex = index + length;
                    if ( (startIndex < len) && IsAllowedPunctuation_AfterTextPreamble( words[ startIndex ] ) )
                    {
                        startIndex++;
                    }

                    var count = 0;
                AGAIN:                    
                    if ( !_MaritalStatuses.TryGetFirst( words, startIndex, out var length_2 ) )
                    {
                        if ( ++count <= MAX_TO_MARITAL_STATUS_WORDS_COUNT )
                        {
                            startIndex++;
                            goto AGAIN;
                        }
                        continue;
                    }
                    if ( (len <= startIndex) || !words[ startIndex ].IsOutputTypeOther() )
                    {
                        break;
                    }

                    for ( int k = startIndex, end = startIndex + length_2; k < end; k++ )
                    {
                        _Buffer.Append( words[ k ].valueOriginal ).Append( ' ' );
                    }
                    if ( _Buffer.Length != 0 )
                    {
                        var nw = (NameWord) words[ nameWordIndex ];
                            nw.MaritalStatus = _Buffer.ToString( 0, _Buffer.Length - 1 );
                        _Buffer.Clear();
                    }
                }
            }
        }
    }
}