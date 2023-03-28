using System.Collections.Generic;
using System.IO;
using System.Linq;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.MaritalStatuses
{
    /// <summary>
    /// 
    /// </summary>
    public interface IWordsChainDictionary
    {
        bool TryGetFirst( IList< word_t > words, int startIndex, out int length );
    }

    public interface IWordsChainYesNoDictionary
    {
        bool TryGetFirst( IList< word_t > words, ref int startIndex, out int length );
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
        [M(O.AggressiveInlining)]
        public bool TryGetFirst( IList< word_t > words, int startIndex, out int length )
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

    unsafe internal sealed class WordsChainYesNoDictionary : IWordsChainYesNoDictionary
    {
        private Map<string, WordsChainYesNoDictionary> _Slots;
        private bool _IsLeaf;
        private bool _IsOptionBase;
        public WordsChainYesNoDictionary( int capacity ) => _Slots = Map<string, WordsChainYesNoDictionary>.CreateWithCloserCapacity( capacity );
        private WordsChainYesNoDictionary() => _Slots = new Map<string, WordsChainYesNoDictionary>();

        #region [.append words.]
        // should be in the form: "preamble ( variant1 / variant2 )", where brackets and slash in only used for variants!
        public void Add( IList< word_t > words )
        {
            var idx = 0;
            var count = words.Count;
            WordsChainYesNoDictionary _this_option_base = this;
            for ( WordsChainYesNoDictionary _this = this, _this_next; ; )
            {
                #region [.save.]
                if ( count == idx )
                {
                    return;
                }
                #endregion

                var v = words[ idx ].valueUpper;
                switch ( v[ 0 ] )
                {
                    case '(':
                        // first option
                        _this._IsOptionBase = true;
                        _this_option_base = _this;
                        break;

                    case '/':
                        //second option
                        _this._IsLeaf = true;
                        _this = _this_option_base;
                        break;

                    case ')':
                        _this._IsLeaf = true;
                        break;

                    default:
                        if ( !_this._Slots.TryGetValue( v, out _this_next ) )
                        {
                            //add next word in chain
                            _this_next = new WordsChainYesNoDictionary();
                            _this._Slots.Add( v, _this_next );
                        }
                        _this = _this_next;
                        break;
                }

                #region comm.
                //var v = words[ idx ].valueUpper;
                //if ( StringsHelper.IsEqual( v, "(" ) )
                //{ // first option
                //    _this._IsOptionBase = true;
                //    _this_option_base = _this;
                //}
                //else if ( StringsHelper.IsEqual( v, "/" ) )
                //{ //second option
                //    _this._IsLeaf = true;
                //    _this = _this_option_base;
                //}
                //else if ( StringsHelper.IsEqual( v, ")" ) )
                //{
                //    _this._IsLeaf = true;
                //}
                //else
                //{
                //    if ( !_this._Slots.TryGetValue( v, out _this_next ) )
                //    {
                //        //add next word in chain
                //        _this_next = new WordsChainYesNoDictionary();
                //        _this._Slots.Add( v, _this_next );
                //    }
                //    _this = _this_next;
                //} 
                #endregion

                idx++;
            }
        }
        #endregion

        #region [.try get.]
        [M(O.AggressiveInlining)] public bool TryGetFirst( IList< word_t > words, ref int startIndex, out int length )
        {
            length = default;

            var startIndex_saved = startIndex;
            var count = words.Count;
            int optionTrials = MaritalStatusesRecognizer.MAX_DISTANCE_FROM_PREAMBLE;
            for ( WordsChainYesNoDictionary _this = this, _this_next; ; )
            {
                #region [.get.]
                if ( _this._IsOptionBase )
                {
                    startIndex_saved = startIndex;
                }
                else if ( _this._IsLeaf )
                {
                    length = (startIndex - startIndex_saved);
                }
                #endregion

                if ( count <= startIndex )
                {
                    break;
                }

                var w = words[ startIndex ];
                if ( !_this._Slots.TryGetValue( w.valueUpper, out _this_next ) )
                {
                    if ( _this._IsOptionBase && 0 < optionTrials )
                        optionTrials--;
                    else
                        break;
                }
                else
                    _this = _this_next;

                startIndex++;
            }

            startIndex--;
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
    public interface IMaritalStatusesModel
    {
        IWordsChainDictionary MaritalStatuses { get; }
        IWordsChainDictionary TextPreambles { get; }
        IWordsChainYesNoDictionary YesNoPreambles { get; }
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class MaritalStatusesModel : IMaritalStatusesModel
    {
        /// <summary>
        /// 
        /// </summary>
        public struct InputParams
        {
            public (string Filename, int? Capacity) MaritalStatuses { get; set; }
            public (string Filename, int? Capacity) MaritalStatusPreambles { get; set; }
        }
        /// <summary>
        /// 
        /// </summary>
        public struct InputParams_2
        {
            public (StreamReader Sr, int? Capacity) MaritalStatuses { get; set; }
            public (StreamReader Sr, int? Capacity) MaritalStatusPreambles { get; set; }
        }

        #region [.ctor().]
        private WordsChainDictionary _MaritalStatuses;
        private WordsChainDictionary _TextPreambles;
        private WordsChainYesNoDictionary _YesNoPreambles;
        private static (word_t[] dashes, Tokenizer tokenizer) create_environment()
        {
            var dashes    = xlat.GetHyphens().Select( h => new word_t() { valueOriginal = h.ToString(), valueUpper = h.ToString() } ).ToArray();
            var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate();
            return (dashes, tokenizer);
        }
        public MaritalStatusesModel( in InputParams p )
        {
            var (dashes, tokenizer) = create_environment();
            using ( tokenizer )
            {
                _MaritalStatuses = Init_MaritalStatuses( p.MaritalStatuses       , tokenizer, dashes );
                _TextPreambles   = Init_TextPreambles  ( p.MaritalStatusPreambles, tokenizer );
                _YesNoPreambles  = Init_YesNoPreambles ( tokenizer );
            }
        }
        public MaritalStatusesModel( in InputParams_2 p )
        {
            var (dashes, tokenizer) = create_environment();
            using ( tokenizer )
            {
                _MaritalStatuses = Init_MaritalStatuses( p.MaritalStatuses       .Sr, p.MaritalStatuses       .Capacity, tokenizer, dashes );
                _TextPreambles   = Init_TextPreambles  ( p.MaritalStatusPreambles.Sr, p.MaritalStatusPreambles.Capacity, tokenizer );
                _YesNoPreambles  = Init_YesNoPreambles ( tokenizer );
            }
        }
        #endregion

        #region [.Init.]
        /// <summary>
        /// 
        /// </summary>
        private static WordsChainDictionary Init_MaritalStatuses( in (string Filename, int? Capacity) p, Tokenizer tokenizer, word_t[] dashes )
        {
            using ( var sr = new StreamReader( p.Filename ) )
            {
                return (Init_MaritalStatuses( sr, p.Capacity, tokenizer, dashes ));
            }
        }
        private static WordsChainDictionary Init_MaritalStatuses( StreamReader sr, int? capacity, Tokenizer tokenizer, word_t[] dashes )
        {
            var three_words = new word_t[ 3 ];
            var wcd = new WordsChainDictionary( capacity.GetValueOrDefault() );

            //local method
            void process( string line )
            {
                StringsHelper.ToUpperInvariantInPlace( line );

                var tokens = tokenizer.Run_NoSentsNoUrlsAllocate( line );

                var hasDashes = tokens.Any( word => word.IsExtraWordTypeDash() );

                if ( hasDashes )
                {
                    foreach ( var dash in dashes )
                    {
                        for ( int i = 0; i < tokens.Count; i++ )
                        {
                            if ( tokens[ i ].IsExtraWordTypeDash() )
                            {
                                tokens[ i ] = dash;
                            }
                        }

                        wcd.Add( tokens );
                    }
                }
                else
                {
                    wcd.Add( tokens );
                }

                if ( tokens.Last().valueUpper.LastChar() == '.' )
                {
                    tokens = tokenizer.Run_NoSentsNoUrlsAllocate( line + " 1234567" );
                    tokens.RemoveAt_Ex( tokens.Count - 1 );
                    wcd.Add( tokens );
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
                var tokens = tokenizer.Run_NoSentsNoUrlsAllocate( line );

                #region [-1-]
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

        private static WordsChainYesNoDictionary Init_YesNoPreambles( Tokenizer tokenizer )
        {
            var words = new[]
            {
                "Verheiratet (ja/nein)",
                "Married (yes/no)",
            };

            var wcd = new WordsChainYesNoDictionary( words.Length );
            foreach ( var w in words )
            {
                var tokens = tokenizer.Run_NoSentsNoUrlsAllocate( w );

                wcd.Add( tokens );
            }
            return (wcd);
        }
        #endregion

        public IWordsChainDictionary      MaritalStatuses { [M(O.AggressiveInlining)] get => _MaritalStatuses; }
        public IWordsChainDictionary      TextPreambles   { [M(O.AggressiveInlining)] get => _TextPreambles; }
        public IWordsChainYesNoDictionary YesNoPreambles  { [M(O.AggressiveInlining)] get => _YesNoPreambles; }
    }
}
