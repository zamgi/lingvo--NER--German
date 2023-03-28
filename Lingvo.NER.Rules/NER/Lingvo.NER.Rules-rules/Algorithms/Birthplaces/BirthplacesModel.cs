using System.Collections.Generic;
using System.IO;
using System.Linq;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.core.Infrastructure;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Birthplaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IWordsChainDictionary
    {
        bool TryGetFirst( IList< word_t > words, int startIndex, out int length );
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal sealed class WordsChainDictionary : IWordsChainDictionary
    {
        private Map< string, WordsChainDictionary > _Slots;
        private bool _IsLeaf;
        public WordsChainDictionary( int capacity ) => _Slots = Map< string, WordsChainDictionary >.CreateWithCloserCapacity( capacity );
        private WordsChainDictionary() => _Slots = new Map< string, WordsChainDictionary >();

        #region [.append words.]
        public void Add( IList< word_t > words, bool each_is_leaf = false )
        {
            var idx = 0;
            var count = words.Count;
            for ( WordsChainDictionary _this = this, _this_next; ; )
            {
                #region [.save.]
                // every word may end the sentence
                if ( each_is_leaf )
                {
                    _this._IsLeaf = true;
                }
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
        public void Add( string word )
        {
            if ( !_Slots.TryGetValue( word, out var next ) )
            {
                //add next word in chain
                next = new WordsChainDictionary() { _IsLeaf = true };
                _Slots.Add( word, next );
            }
            else
            {
                next._IsLeaf = true;
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
                }
                #endregion

                if ( count == startIndex )
                {
                    break;
                }

                var w = words[ startIndex ];
                if ( !_this._Slots.TryGetValue( w.valueUpper, out _this_next ) )
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

    /// <summary>
    /// 
    /// </summary>
    public interface IBirthplacesModel
    {
        IWordsChainDictionary Birthplaces   { get; }
        IWordsChainDictionary TextPreambles { get; }
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class BirthplacesModel : IBirthplacesModel
    {
        /// <summary>
        /// 
        /// </summary>
        public struct InputParams
        {
            public (string Filename, int? Capacity) Birthplaces         { get; set; }
            public (string Filename, int? Capacity) BirthplacePreambles { get; set; }
        }
        /// <summary>
        /// 
        /// </summary>
        public struct InputParams_2
        {
            public (StreamReader Sr, int? Capacity) Birthplaces         { get; set; }
            public (StreamReader Sr, int? Capacity) BirthplacePreambles { get; set; }
        }

        #region [.ctor().]
        private WordsChainDictionary _Birthplaces;
        private WordsChainDictionary _TextPreambles;
        private static (word_t[] dashes, VersionCombiner< word_t > vc, Tokenizer tokenizer) create_environment()
        {
            var dashes    = xlat.GetHyphens().Select( h => new word_t() { valueOriginal = h.ToString(), valueUpper = h.ToString() } ).ToArray();
            var vc        = new VersionCombiner< word_t >( new word_EqualityComparer() );
            var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate();
            return (dashes, vc, tokenizer);
        }
        public BirthplacesModel( in InputParams p )
        {
            var (dashes, vc, tokenizer) = create_environment();
            using ( tokenizer )
            {
                _Birthplaces   = Init_Birthplaces  ( p.Birthplaces        , tokenizer, dashes, vc );
                _TextPreambles = Init_TextPreambles( p.BirthplacePreambles, tokenizer );
            }
        }
        public BirthplacesModel( in InputParams_2 p )
        {
            var (dashes, vc, tokenizer) = create_environment();
            using ( tokenizer )
            {
                _Birthplaces   = Init_Birthplaces  ( p.Birthplaces        .Sr, p.Birthplaces        .Capacity, tokenizer, dashes, vc );
                _TextPreambles = Init_TextPreambles( p.BirthplacePreambles.Sr, p.BirthplacePreambles.Capacity, tokenizer );
            }
        }
        #endregion

        #region [.Init.]
        /// <summary>
        /// 
        /// </summary>
        private sealed class word_EqualityComparer : IEqualityComparer< word_t >
        {
            public bool Equals( word_t x, word_t y ) => (StringsHelper.IsEqual( x.valueOriginal, y.valueOriginal ));
            public int GetHashCode( word_t obj ) => obj.valueOriginal.GetHashCode();
        }

        private static WordsChainDictionary Init_Birthplaces( in (string Filename, int? Capacity) p, Tokenizer tokenizer, word_t[] dashes, VersionCombiner< word_t > vc )
        {
            using ( var sr = new StreamReader( p.Filename ) )
            {
                return (Init_Birthplaces( sr, p.Capacity, tokenizer, dashes, vc ));
            }
        }
        private static WordsChainDictionary Init_Birthplaces( StreamReader sr, int? capacity, Tokenizer tokenizer, word_t[] dashes, VersionCombiner< word_t > vc )
        {
            const char DASH  = '-';
            const char SPACE = ' ';
            var three_words = new word_t[ 3 ];

            var wcd = new WordsChainDictionary( capacity.GetValueOrDefault() );

            //local method
            void process( string line )
            {
                StringsHelper.ToUpperInvariantInPlace( line );

                if ( StringsHelper.ContainsOnlyLetters( line ) )
                {
                    wcd.Add( line );
                }
                else
                {
                    line = line.Replace( DASH, SPACE );

                    var words = tokenizer.Run_NoSentsNoUrlsAllocate( line );
                    if ( 1 < words.Count )
                    {
                        wcd.Add( words, true );

                        switch ( words.Count )
                        {
                            case 2:
                                three_words[ 0 ] = words[ 0 ];
                                three_words[ 2 ] = words[ 1 ];
                                foreach ( var dash in dashes )
                                {
                                    three_words[ 1 ] = dash;
                                    wcd.Add( three_words );
                                }
                                break;

                            case 3:
                                var w = words[ 1 ];
                                if ( (w.length == 1) && (w.nerInputType == NerInputType.Quote) )
                                {
                                    return; //"D'SOUZA" => skip for insert dash
                                }
                                goto default;

                            default:
                                foreach ( var dash in dashes )
                                {
                                    foreach ( var words_2 in vc.GetVersions( words, dash, returnOriginArray: false ) )
                                    {
                                        wcd.Add( words_2 );
                                    }
                                }
                                break;
                        }
                    }

                    var old_char = SPACE;
                    foreach ( var dash in dashes )
                    {
                        var new_char = dash.valueOriginal[ 0 ];
                        line = line.Replace( old_char, new_char );
                        old_char = new_char;

                        wcd.Add( line );
                    }
                }
            };

            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                if ( line.Length == 0 ) continue;

                process( line );
                process( tokenizer.NormalizeUmlautes( line ) );
            }

            return (wcd);
        }

        private static WordsChainDictionary Init_TextPreambles( in (string Filename, int? Capacity) p, Tokenizer tokenizer )
        {
            using ( var sr = new StreamReader( p.Filename ) )
            {
                return (Init_TextPreambles( sr, p.Capacity, tokenizer ));
            }
        }
        private static WordsChainDictionary Init_TextPreambles( StreamReader sr, int? capacity, Tokenizer tokenizer )
        {
            var colons = xlat.GetHyphens().Concat( new[] { ':' } ).ToArray();
            var wcd = new WordsChainDictionary( capacity.GetValueOrDefault() );

            //local method
            void process( string line )
            {
                #region [-1-]
                var tokens = tokenizer.Run_NoSentsNoUrlsAllocate( line );
                wcd.Add( tokens );

                if ( tokens.Last().valueUpper.LastChar() == '.' )
                {   // copy-pasted from BirthdayRecognizer
                    tokens = tokenizer.Run_NoSentsNoUrlsAllocate( line + " 1234567" );
                    var i = tokens.Count - 1; if ( 0 <= i ) tokens.RemoveAt_Ex( i );
                    wcd.Add( tokens );
                }
                #endregion

                #region [-2-]
                foreach ( var c in colons )
                {
                    tokens = tokenizer.Run_NoSentsNoUrlsAllocate( line + c );
                    wcd.Add( tokens );
                }
                #endregion
            };

            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                if ( line.Length == 0 ) continue;

                process( line );
                process( tokenizer.NormalizeUmlautes( line ) );
            }

            return (wcd);
        }
        #endregion

        public IWordsChainDictionary Birthplaces   { [M(O.AggressiveInlining)] get => _Birthplaces;   }
        public IWordsChainDictionary TextPreambles { [M(O.AggressiveInlining)] get => _TextPreambles; }
    }
}
