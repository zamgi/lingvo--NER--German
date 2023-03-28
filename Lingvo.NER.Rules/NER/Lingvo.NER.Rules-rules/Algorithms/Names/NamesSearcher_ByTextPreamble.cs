using System.Collections.Generic;
using System.IO;
using System.Linq;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Names
{
    /// <summary>
    /// 
    /// </summary>
    public enum TextPreambleTypeEnum : byte
    {
        __UNDEFINED__,

        Professor,
        Doctor,
        Engineer,
        Frau,
        Herr,
        Chairman,
    }

    #region not-used. comm.
    /*
    /// <summary>
    /// 
    /// </summary>
    internal readonly struct ByTextPreamble_SearchResult
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Comparer : IComparer< ByTextPreamble_SearchResult >
        {
            public static Comparer Instance { get; } = new Comparer();
            private Comparer() { }
            public int Compare( ByTextPreamble_SearchResult x, ByTextPreamble_SearchResult y ) => (y.StartIndex - x.StartIndex);
        }

        [M(O.AggressiveInlining)] public ByTextPreamble_SearchResult( int startIndex, int length
            , TextPreambleTypeEnum textPreambleType, int preambleWordIndex, string nameValue )
        {
            StartIndex        = startIndex;
            Length            = length;
            TextPreambleType  = textPreambleType;
            PreambleWordIndex = preambleWordIndex;
            NameValue         = nameValue;
        }

        public int                  StartIndex        { [M(O.AggressiveInlining)] get; }        
        public int                  Length            { [M(O.AggressiveInlining)] get; }
        public int                  PreambleWordIndex { [M(O.AggressiveInlining)] get; }
        public TextPreambleTypeEnum TextPreambleType  { [M(O.AggressiveInlining)] get; }
        public string               NameValue         { [M(O.AggressiveInlining)] get; }
        [M(O.AggressiveInlining)] public int EndIndex() => StartIndex + Length;
#if DEBUG
        public override string ToString() => $"[{StartIndex}:{Length}, {TextPreambleType}, '{NameValue}']"; 
#endif
    }
    */
    #endregion

    /// <summary>
    ///
    /// </summary>
    unsafe internal sealed class NamesSearcher_ByTextPreamble
    {
        /// <summary>
        /// 
        /// </summary>
        internal sealed class WordsChainDictionary
        {
            private Map< string, WordsChainDictionary > _Slots;
            private TextPreambleTypeEnum? _TextPreambleType;
            public WordsChainDictionary() => _Slots = new Map< string, WordsChainDictionary >();

            #region [.append words.]
            public void Add( IList< word_t > words, TextPreambleTypeEnum textPreambleType )
            {
                var startIndex = 0;
                for ( WordsChainDictionary _this = this, _this_next; ; )
                {
                    #region [.save.]
                    if ( words.Count == startIndex )
	                {
                        if ( _this._TextPreambleType.HasValue )
                        {
                            if ( _this._TextPreambleType.Value == textPreambleType )
                            {
                                return;
                            }

                            throw (new InvalidDataException());
                        }
                        _this._TextPreambleType = textPreambleType;
                        return;
                    }
                    #endregion

                    var word = words[ startIndex ].valueUpper;
                    if ( !_this._Slots.TryGetValue( word, out _this_next ) )
                    {
                        //add next word in chain
                        _this_next = new WordsChainDictionary();
                        _this._Slots.Add( word, _this_next );
                    }                
                    _this = _this_next;
                    startIndex++;
                }
            }
            public void Add( string word, TextPreambleTypeEnum textPreambleType )
            {
                StringsHelper.ToUpperInvariantInPlace( word );

                if ( !_Slots.TryGetValue( word, out var next ) )
                {
                    //add next word in chain
                    next = new WordsChainDictionary() { _TextPreambleType = textPreambleType };
                    _Slots.Add( word, next );
                }
                else
                {
                    if ( next._TextPreambleType.HasValue ) throw (new InvalidDataException());
                    next._TextPreambleType = textPreambleType;
                }
            }
            #endregion

            #region [.try get.]
            //[M(O.AggressiveInlining)] public bool Contains_AsFirstInChain( word_t word ) => _Slots.Contains( word.valueUpper );

            [M(O.AggressiveInlining)] public bool TryGetFirst( IList< word_t > words, int startIndex, out (TextPreambleTypeEnum textPreambleType, int length) x )
            {
                x = default;

                var startIndex_saved = startIndex;
                for ( WordsChainDictionary _this = this, _this_next; ; )
                {
                    #region [.get.]
                    if ( _this._TextPreambleType.HasValue )
                    {
                        x = (_this._TextPreambleType.Value, startIndex - startIndex_saved);
                        //--return (true);
                    }
                    #endregion

                    if ( words.Count == startIndex )
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

                return (x.length != 0);// && ( x.textPreambleType != TextPreambleTypeEnum.__UNDEFINED__);
            }
            /*[M(O.AggressiveInlining)] public bool TryGet( IList< word_t > words, ICollection< TextPreambleTypeEnum > textPreambleTypes )
            {
                textPreambleTypes.Clear();

                var startIndex = 0;
                for ( WordsChainDictionary _this = this, _this_next; ; )
                {
                    #region [.get.]
                    if ( _this._TextPreambleType.HasValue )
                    {
                        textPreambleTypes.Add( _this._TextPreambleType.Value );
                    }
                    #endregion

                    if ( words.Count == startIndex )
                    {
                        break;
                    }

                    var word = words[ startIndex ].valueUpper;
                    if ( !_this._Slots.TryGetValue( word, out _this_next ) )
                    {
                        break;
                    }
                    _this = _this_next;
                    startIndex++;
                }

                return (textPreambleTypes.Count != 0);
            }*/
            #endregion
#if DEBUG
            public override string ToString() => (_TextPreambleType.HasValue ? _TextPreambleType.Value.ToString() : $"count: {_Slots.Count}");
#endif
        }

        #region [.cctor().]
        private static WordsChainDictionary _TextPreambles;
        private static Set< char >          _AllowedPunctuation_AfterTextPreamble;
        private static CharType*            _CTM;
        static NamesSearcher_ByTextPreamble()
        {
            Init_TextPreambles();

            _AllowedPunctuation_AfterTextPreamble = xlat.GetHyphens().Concat( new[] { ':', '.' } ).ToSet();
            _CTM = xlat_Unsafe.Inst._CHARTYPE_MAP;
        }

        private static void Init_TextPreambles()
        {
            _TextPreambles = new WordsChainDictionary();

            _TextPreambles.Add( "Vorsitzender", TextPreambleTypeEnum.Chairman );

            _TextPreambles.Add( "Professor", TextPreambleTypeEnum.Professor );
            _TextPreambles.Add( "Prof."    , TextPreambleTypeEnum.Professor );

            _TextPreambles.Add( "Frau", TextPreambleTypeEnum.Frau );
            _TextPreambles.Add( "Fr." , TextPreambleTypeEnum.Frau );

            _TextPreambles.Add( "Herr" , TextPreambleTypeEnum.Herr );
            _TextPreambles.Add( "Herrn", TextPreambleTypeEnum.Herr );
            _TextPreambles.Add( "Hr."  , TextPreambleTypeEnum.Herr );

            _TextPreambles.Add( "Doktor", TextPreambleTypeEnum.Doctor );
            _TextPreambles.Add( "Dr."   , TextPreambleTypeEnum.Doctor );
            using ( var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate() )
            {
                _TextPreambles.Add_WithLastWordAddDot( tokenizer.Run_NoSentsNoUrlsAllocate( "Dr.-Ing."      ), TextPreambleTypeEnum.Doctor );
                _TextPreambles.Add_WithLastWordAddDot( tokenizer.Run_NoSentsNoUrlsAllocate( "Dr. rer. pol." ), TextPreambleTypeEnum.Doctor );
                _TextPreambles.Add_WithLastWordAddDot( tokenizer.Run_NoSentsNoUrlsAllocate( "Dr. med."      ), TextPreambleTypeEnum.Doctor );

                _TextPreambles.Add_WithLastWordAddDot( tokenizer.Run_NoSentsNoUrlsAllocate( "Prof. Dr."     ), TextPreambleTypeEnum.Professor );

                _TextPreambles.Add_WithLastWordAddDot( tokenizer.Run_NoSentsNoUrlsAllocate( "Dipl.-Ing."       ), TextPreambleTypeEnum.Engineer );
                _TextPreambles.Add_WithLastWordAddDot( tokenizer.Run_NoSentsNoUrlsAllocate( "Dipl.-Volksw."    ), TextPreambleTypeEnum.Engineer );
                _TextPreambles.Add_WithLastWordAddDot( tokenizer.Run_NoSentsNoUrlsAllocate( "Dipl.-Verw.Wiss." ), TextPreambleTypeEnum.Engineer );
            }
        }
        #endregion

        #region [.ctor().]
        private NamesRecognizer _NamesRecognizer;
        public NamesSearcher_ByTextPreamble( NamesRecognizer namesRecognizer ) => _NamesRecognizer = namesRecognizer;
        #endregion

        [M(O.AggressiveInlining)] private bool IsAllowedPunctuation_AfterTextPreamble( word_t w, out bool isFirstNameAbbreviation )
        {
            switch ( w.length )
            {
                case 1 :
                    isFirstNameAbbreviation = false;
                    return (_AllowedPunctuation_AfterTextPreamble.Contains( w.valueUpper[ 0 ] ));

                case 2 :
                    if ( _CTM[ w.valueUpper[ 0 ] ].IsUpperLetter() && xlat.IsDot( w.valueUpper[ 1 ] ) )
                    {
                        isFirstNameAbbreviation = true;
                        return (true);
                    }
                break;
            }

            isFirstNameAbbreviation = false;
            return (false);
        }
        
        public bool TryRecognizeAll( List< word_t > words )
        {
            var success = false;
            for ( var index = 0; index < words.Count; index++ )
            {
                var w = words[ index ];
                if ( w.IsInputTypeNum() || w.IsExtraWordTypePunctuation() ) //!w.IsOutputTypeOther() )
                {
                    continue;
                }

                if ( !_TextPreambles.TryGetFirst( words, index, out var x ) )
                {
                    continue;
                }
                var preambleWordIndex = index;

                var startIndex = index + x.length;
                var isFirstNameAbbreviation = false;
                if ( (startIndex < words.Count) && IsAllowedPunctuation_AfterTextPreamble( words[ startIndex ], out isFirstNameAbbreviation ) )
                {
                    startIndex++;
                }

                int endIndex;
                var r = isFirstNameAbbreviation ? _NamesRecognizer.TryRecognize_FirstNameAbbreviation_And_SurName_AfterTextPreamble( words, startIndex, x.textPreambleType, new SearchResult( startIndex - 1, 1 ), out endIndex )
                                                : _NamesRecognizer.TryRecognize_FullName_AfterTextPreamble( words, startIndex, x.textPreambleType, out endIndex );
                if ( !r )
                {
                    r = _NamesRecognizer.TryRecognize_SurNameOnly_AfterTextPreamble( words, startIndex, x.textPreambleType, out endIndex );
                }
                if ( r )
                {
                    success = true;
                    index   = endIndex;
                    words[ preambleWordIndex ].ClearOutputTypeAndNerChain();
                }
            }

            return (success);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class ByTextPreambleExtensions
    {
        [M(O.AggressiveInlining)] public static void Add_WithLastWordAddDot( this NamesSearcher_ByTextPreamble.WordsChainDictionary wd, IList< word_t > words, TextPreambleTypeEnum tpt )
        {
            if ( words.Any() )
            {
                var w = words.Last();
                if ( (w.length == 1) && xlat.IsDot( w.valueOriginal[ 0 ] ) )
                {
                    words.RemoveAt( words.Count - 1 );
                    w = words.Last();
                    w.valueUpper    += '.';
                    w.valueOriginal += '.';
                }
                wd.Add( words, tpt );
            }
        }
    }
}