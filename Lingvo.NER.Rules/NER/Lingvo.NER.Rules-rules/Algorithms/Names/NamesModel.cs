using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.core.Infrastructure;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Names
{
    /// <summary>
    /// 
    /// </summary>
    public interface IWordsChainDictionary
    {
        bool TryGetFirst( IList< word_t > words, int startIndex, out int length );
        bool TryGetFirst( word_t w );
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
        [M(O.AggressiveInlining)] public static bool IsFirstLetterUpper( word_t w )
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
                if ( !w.IsExtraWordTypePunctuation() && !IsFirstLetterUpper( w ) ) //we need always first letter must be UPPER
                {
#if DEBUG
                    //---Debug.Assert( (w.nerOutputType == NerOutputType.Url) || (w.nerInputType == NerInputType.NumCapital) || !w.valueOriginal[ 0 ].IsUpperLetter() );                    
#endif
                    break;
                }
                if ( !_this._Slots.TryGetValue( w.valueUpper, out _this_next ) )
                {
                    if ( !w.IsExtraWordTypeHasUmlautes() || !_this._Slots.TryGetValue( w.valueUpper__UmlautesNormalized, out _this_next ) )
                    {
                        break;
                    }
                }                
                _this = _this_next;
                startIndex++;
            }

            return (length != 0);
        }
        [M(O.AggressiveInlining)] public bool TryGetFirst( word_t w )
        {
            //if ( !_IsLeaf )
            //{
            //    return (false);
            //}
            if ( !w.IsExtraWordTypePunctuation() && !IsFirstLetterUpper( w ) ) //we need always first letter must be UPPER
            {
#if DEBUG
                Debug.Assert( (w.nerInputType == NerInputType.NumCapital) || !w.valueOriginal[ 0 ].IsUpperLetter() );
#endif
                return (false);
            }
            if ( !_Slots.ContainsKey( w.valueUpper ) )
            {
                if ( !w.IsExtraWordTypeHasUmlautes() || !_Slots.ContainsKey( w.valueUpper__UmlautesNormalized ) )
                {
                    return (false);
                }
            }

            return (true);
        }
        #endregion
#if DEBUG
        public override string ToString() => (_IsLeaf ? "true" : $"count: {_Slots.Count}");
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public interface INamesModel
    {
        IWordsChainDictionary FirstNames { get; }
        IWordsChainDictionary SurNames   { get; }

        bool IsExcludedName( string fn, string sn );
        bool IsExcludedName_IgnoreCase( string fn, string sn );
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class NamesModel : INamesModel
    {
        /// <summary>
        /// 
        /// </summary>
        public struct InputParams
        {
            public (string Filename, int? Capacity) FirstNames { get; set; }
            public (string Filename, int? Capacity) SurNames   { get; set; }
            public string ExcludedNamesFilename { get; set; }
        }
        /// <summary>
        /// 
        /// </summary>
        public struct InputParams_2
        {
            public (StreamReader Sr, int? Capacity) FirstNames { get; set; }
            public (StreamReader Sr, int? Capacity) SurNames   { get; set; }
            public string ExcludedNamesFilename { get; set; }
            public StreamReader ExcludedNamesStreamReader { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        private readonly struct ExcludedName
        {
            /// <summary>
            /// 
            /// </summary>
            public sealed class EqualityComparer : IEqualityComparerByRef< ExcludedName >
            {
                public bool Equals( in ExcludedName x, in ExcludedName y ) => (StringsHelper.IsEqual( x.Firstname, y.Firstname ) && StringsHelper.IsEqual( x.Surname, y.Surname ));
                public int GetHashCode( in ExcludedName obj ) => obj.Firstname.GetHashCode() ^ obj.Surname.GetHashCode();
            }

            [M(O.AggressiveInlining)] public ExcludedName( string fn, string sn )
            {
                Firstname = fn;
                Surname   = sn;
            }
            public string Firstname { [M(O.AggressiveInlining)] get; }
            public string Surname   { [M(O.AggressiveInlining)] get; }
#if DEBUG
            public override string ToString() => $"first-name: '{Firstname}', sur-name: '{Surname}'";
#endif
        }

        #region [.ctor().]
        private WordsChainDictionary _FirstNames;
        private WordsChainDictionary _SurNames;
        private SetByRef< ExcludedName > _ExcludedNames;
        private static (word_t[] dashes, char[] dashes_chars, VersionCombiner< word_t > vc, Tokenizer tokenizer) create_environment()
        {
            var dashes       = xlat.GetHyphens().Select( h => new word_t() { valueUpper = h.ToString() } ).ToArray();
            var dashes_chars = xlat.GetHyphens().ToArray();
            var vc           = new VersionCombiner< word_t >( new word_EqualityComparer() );
            var tokenizer    = Tokenizer.Create4NoSentsNoUrlsAllocate();
            return (dashes, dashes_chars, vc, tokenizer);
        }
        public NamesModel( in InputParams p )
        {
            var (dashes, dashes_chars, vc, tokenizer) = create_environment();
            using ( tokenizer )
            {
                _FirstNames = Init( p.FirstNames, tokenizer, dashes, dashes_chars, vc );
                _SurNames   = Init( p.SurNames  , tokenizer, dashes, dashes_chars, vc );
            }

            _ExcludedNames = Init_ExcludedNames( p.ExcludedNamesFilename );
        }
        public NamesModel( in InputParams_2 p )
        {
            var (dashes, dashes_chars, vc, tokenizer) = create_environment();
            using ( tokenizer )
            {
                _FirstNames = Init( p.FirstNames.Sr, p.FirstNames.Capacity, tokenizer, dashes, dashes_chars, vc );
                _SurNames   = Init( p.SurNames  .Sr, p.SurNames  .Capacity, tokenizer, dashes, dashes_chars, vc );
            }

            _ExcludedNames = (p.ExcludedNamesStreamReader != null) ? Init_ExcludedNames( p.ExcludedNamesStreamReader )
                                                                   : Init_ExcludedNames( p.ExcludedNamesFilename );
        }
        #endregion

        #region [.Init.]
        /// <summary>
        /// 
        /// </summary>
        private sealed class word_EqualityComparer : IEqualityComparer< word_t >
        {
            public bool Equals( word_t x, word_t y ) => StringsHelper.IsEqual( x.valueUpper, y.valueUpper );
            public int GetHashCode( word_t obj ) => obj.valueUpper.GetHashCode();
        }

        private static WordsChainDictionary Init( in (string Filename, int? Capacity) p, Tokenizer tokenizer, word_t[] dashes, char[] dashes_chars, VersionCombiner< word_t > vc )
        {
            using ( var sr = new StreamReader( p.Filename ) )
            {
                return (Init( sr, p.Capacity, tokenizer, dashes, dashes_chars, vc ));
            }            
        }
        private static WordsChainDictionary Init( StreamReader sr, int? capacity, Tokenizer tokenizer, word_t[] dashes, char[] dashes_chars, VersionCombiner< word_t > vc )
        {
            const char DASH  = '-';
            const char SPACE = ' ';
            var three_words = new word_t[ 3 ];

            var wcd = new WordsChainDictionary( capacity.GetValueOrDefault() );

            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                if ( (line.Length == 0) || (line[ 0 ] == '#') ) continue;

                StringsHelper.ToUpperInvariantInPlace( line );
                #region comm.
                /*
                #if DEBUG
                if ( line.Contains( "MERKEL" ) )
                {
                    Debugger.Break();
                }
                #endif
                //*/
                #endregion
                wcd.Add( line );

                if ( !StringsHelper.ContainsOnlyLetters( line ) )
                {
                    line = line.Replace( DASH, SPACE );

                    var words = tokenizer.Run_NoSentsNoUrlsAllocate( line );
                    if ( 1 < words.Count )
                    {
                        wcd.Add( words );

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
                                    continue; //"D'SOUZA" => skip for insert dash
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
                    foreach ( var new_char in dashes_chars )
                    {
                        line = line.Replace( old_char, new_char );
                        old_char = new_char;

                        wcd.Add( line );
                    }
                }
            }
            
            return (wcd);
        }

        private static SetByRef< ExcludedName > Init_ExcludedNames( string fileName )
        {
            using ( var sr = new StreamReader( fileName ) )
            {
                return (Init_ExcludedNames( sr ));
            }
            #region comm. prev.
            /*
            var xes = XDocument.Load( fileName ).Root.Descendants( "_" );

            var set = SetByRef< ExcludedName >.CreateWithCloserCapacity( new ExcludedName.EqualityComparer(), xes.Count() );
            foreach ( var xe in xes )
            {
                var fn = xe.Attribute( "n1" )?.Value;
                var sn = xe.Attribute( "n2" )?.Value;
                if ( !fn.IsNullOrEmpty() && !sn.IsNullOrEmpty() )
                {
                    StringsHelper.ToUpperInvariantInPlace( fn );
                    StringsHelper.ToUpperInvariantInPlace( sn );

                    set.Add( new ExcludedName( fn, sn ) );

                    //var flip = xe.Attribute( "flip" )?.Value;
                    set.Add( new ExcludedName( sn, fn ) );
                }
            }
            return (set);
            //*/
            #endregion
        }
        private static SetByRef< ExcludedName > Init_ExcludedNames( StreamReader sr )
        {
            var xes = XDocument.Load( sr ).Root.Descendants( "_" );

            var set = SetByRef< ExcludedName >.CreateWithCloserCapacity( new ExcludedName.EqualityComparer(), xes.Count() );
            foreach ( var xe in xes )
            {
                var fn = xe.Attribute( "n1" )?.Value;
                var sn = xe.Attribute( "n2" )?.Value;
                if ( !fn.IsNullOrEmpty() && !sn.IsNullOrEmpty() )
                {
                    StringsHelper.ToUpperInvariantInPlace( fn );
                    StringsHelper.ToUpperInvariantInPlace( sn );

                    set.Add( new ExcludedName( fn, sn ) );

                    //var flip = xe.Attribute( "flip" )?.Value;
                    set.Add( new ExcludedName( sn, fn ) );
                }
            }
            return (set);
        }
        #endregion

        public IWordsChainDictionary FirstNames { [M(O.AggressiveInlining)] get => _FirstNames; }
        public IWordsChainDictionary SurNames   { [M(O.AggressiveInlining)] get => _SurNames;   }
        public bool IsExcludedName( string fn, string sn ) => _ExcludedNames.Contains( new ExcludedName( fn, sn ) );
        public bool IsExcludedName_IgnoreCase( string fn, string sn ) => throw new NotImplementedException(); //---_ExcludedNames.Contains( new ExcludedName( StringsHelper.ToUpperInvariant( fn ), StringsHelper.ToUpperInvariant( sn ) ) );
    }
}
