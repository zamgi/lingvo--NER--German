﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Lingvo.NER.NeuralNetwork.SentSplitting;
using Lingvo.NER.NeuralNetwork.Urls;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.NeuralNetwork.Tokenizing
{
    /// <summary>
    /// 
    /// </summary>
    unsafe sealed public class Tokenizer : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public delegate void ProcessSentCallbackDelegate( List< word_t > words );

        /// <summary>
        /// 
        /// </summary>
        [Flags] private enum SpecialCharType : byte
        {
            __UNDEFINE__                = 0x0,

            InterpreteAsWhitespace      = 0x1,
            BetweenLetterOrDigit        = (1 << 1),
            BetweenDigit                = (1 << 2),
            TokenizeDifferentSeparately = (1 << 3),
            DotChar                     = (1 << 4),
        }

        /// <summary>
        /// 
        /// </summary>
        unsafe private sealed class UnsafeConst
        {
            #region [.static & xlat table's.]
            public  static readonly char*  MAX_PTR                          = (char*) (0xffffffffFFFFFFFF);
            private const string           INCLUDE_INTERPRETE_AS_WHITESPACE = "¤¦§¶"; //"¥©¤¦§®¶€™<>";
            private const char             DOT                              = '\u002E'; /* 0x2E, 46, '.' */
            #endregion

            public readonly SpecialCharType* _SPEC_CHARTYPE_MAP;
            private UnsafeConst()
            {
                #region [.xlat table's.]
                #region comm.
                //var BETWEEN_LETTER_OR_DIGIT_RU    = new[]
                //                                    {
                //                                    '\u0026', /* 0x26  , 38  , '&' */
                //                                    '\u0027', /* 0x27  , 39  , ''' */
                //                                    '\u002D', /* 0x2D  , 45  , '-' */
                //                                    '\u005F', /* 0x5F  , 95  , '_' */
                //                                    '\u00AD', /* 0xAD  , 173 , '­' */
                //                                    '\u055A', /* 0x55A , 1370, '՚' */
                //                                    '\u055B', /* 0x55B , 1371, '՛' */
                //                                    '\u055D', /* 0x55D , 1373, '՝' */
                //                                    '\u2012', /* 0x2012, 8210, '‒' */
                //                                    '\u2013', /* 0x2013, 8211, '–' */
                //                                    '\u2014', /* 0x2014, 8212, '—' */
                //                                    '\u2015', /* 0x2015, 8213, '―' */
                //                                    '\u2018', /* 0x2018, 8216, '‘' */
                //                                    '\u2019', /* 0x2019, 8217, '’' */
                //                                    '\u201B', /* 0x201B, 8219, '‛' */
                //                                    };
                #endregion
                var BETWEEN_LETTER_OR_DIGIT_EN    = new[] 
                                                    { 
                                                    '\u0026', /* 0x26  , 38  , '&' */
                                                    //'\u0027', /* 0x27  , 39  , ''' */
                                                    '\u002D', /* 0x2D  , 45  , '-' */
                                                    '\u005F', /* 0x5F  , 95  , '_' */
                                                    '\u00AD', /* 0xAD  , 173 , '­' */
                                                    //'\u055A', /* 0x55A , 1370, '՚' */
                                                    //'\u055B', /* 0x55B , 1371, '՛' */
                                                    //'\u055D', /* 0x55D , 1373, '՝' */
                                                    '\u2012', /* 0x2012, 8210, '‒' */
                                                    '\u2013', /* 0x2013, 8211, '–' */
                                                    '\u2014', /* 0x2014, 8212, '—' */
                                                    '\u2015', /* 0x2015, 8213, '―' */
                                                    '\u2018', /* 0x2018, 8216, '‘' */
                                                    //'\u2019', /* 0x2019, 8217, '’' */
                                                    '\u201B', /* 0x201B, 8219, '‛' */
                                                    };
                var BETWEEN_DIGIT                 = new[] 
                                                    { 
                                                    '\u0022', /* 0x22   , 34   , '"'  */
                                                    '\u002C', /* 0x2C   , 44   , ','  */
                                                    '\u003A', /* 0x3A   , 58   , ':'  */
                                                    '\u3003', /* 0x3003 , 12291, '〃' */
                                                    //-ERROR-!!!-DOT, /* и  0x2E   , 46   , '.' - хотя это и так работает */
                                                    };
                var TOKENIZE_DIFFERENT_SEPARATELY = new[] 
                                                    {             
                                                    '\u2012', /* 0x2012 , 8210 , '‒' */
                                                    '\u2013', /* 0x2013 , 8211 , '–' */
                                                    '\u2014', /* 0x2014 , 8212 , '—' */
                                                    '\u2015', /* 0x2015 , 8213 , '―' */
                                                    '\u2018', /* 0x2018 , 8216 , '‘' */
                                                    '\u2019', /* 0x2019 , 8217 , '’' */
                                                    '\u201B', /* 0x201B , 8219 , '‛' */
                                                    '\u201C', /* 0x201C , 8220 , '“' */
                                                    '\u201D', /* 0x201D , 8221 , '”' */
                                                    '\u201E', /* 0x201E , 8222 , '„' */
                                                    '\u201F', /* 0x201F , 8223 , '‟' */
                                                    '\u2026', /* 0x2026 , 8230 , '…' */
                                                    '\u0021', /* 0x21   , 33   , '!' */
                                                    '\u0022', /* 0x22   , 34   , '"' */
                                                    '\u0026', /* 0x26   , 38   , '&' */
                                                    '\u0027', /* 0x27   , 39   , ''' */
                                                    '\u0028', /* 0x28   , 40   , '(' */
                                                    '\u0029', /* 0x29   , 41   , ')' */
                                                    '\u002C', /* 0x2C   , 44   , ',' */
                                                    '\u002D', /* 0x2D   , 45   , '-' */
                                                    //DOT, //'\u002E', /* 0x2E   , 46   , '.' */
                                                    '\u3003', /* 0x3003 , 12291, '〃' */
                                                    '\u003A', /* 0x3A   , 58   , ':' */
                                                    '\u003B', /* 0x3B   , 59   , ';' */
                                                    '\u003F', /* 0x3F   , 63   , '?' */
                                                    '\u055A', /* 0x55A  , 1370 , '՚' */
                                                    '\u055B', /* 0x55B  , 1371 , '՛'  */
                                                    '\u055D', /* 0x55D  , 1373 , '՝' */
                                                    '\u005B', /* 0x5B   , 91   , '[' */
                                                    '\u005D', /* 0x5D   , 93   , ']' */
                                                    '\u005F', /* 0x5F   , 95   , '_' */
                                                    '\u05F4', /* 0x5F4  , 1524 , '״' */
                                                    '\u007B', /* 0x7B   , 123  , '{' */
                                                    '\u007D', /* 0x7D   , 125  , '}' */
                                                    '\u00A1', /* 0xA1   , 161  , '¡' */
                                                    '\u00AB', /* 0xAB   , 171  , '«' */
                                                    '\u00AD', /* 0xAD   , 173  , '­' */
                                                    '\u00BB', /* 0xBB   , 187  , '»' */
                                                    '\u00BF', /* 0xBF   , 191  , '¿' */
                                                    '/',
                                                    '¥', '©', '®', '€', '™', '°', '№', '$', '%',
                                                    '<', '>',
                                                    };
                #endregion

                //-1-//
                var spec_chartype_map = new byte/*SpecialCharType*/[ char.MaxValue + 1 ];
                fixed ( /*SpecialCharType*/byte* cctm = spec_chartype_map )        
                {
                    for ( var c = char.MinValue; /*c <= char.MaxValue*/; c++ )
                    {
                        if ( /*char.IsWhiteSpace( c ) ||*/ char.IsPunctuation( c ) )
                        {
                            *(cctm + c) = (byte) SpecialCharType.InterpreteAsWhitespace;
                        }

                        if ( c == char.MaxValue )
                        {
                            break;
                        }
                    }

                    foreach ( var c in INCLUDE_INTERPRETE_AS_WHITESPACE )
                    {
                        *(cctm + c) = (byte) SpecialCharType.InterpreteAsWhitespace;
                    }

                    foreach ( var c in TOKENIZE_DIFFERENT_SEPARATELY )
                    {
                        *(cctm + c) = (byte) SpecialCharType.TokenizeDifferentSeparately;
                    }

                    //var between_letter_or_digit = (languageType == LanguageTypeEnum.En) ? BETWEEN_LETTER_OR_DIGIT_EN : BETWEEN_LETTER_OR_DIGIT_RU;
                    foreach ( var c in BETWEEN_LETTER_OR_DIGIT_EN )
                    {
                        *(cctm + c) |= (byte) SpecialCharType.BetweenLetterOrDigit;
                    }

                    foreach ( var c in BETWEEN_DIGIT )
                    {
                        *(cctm + c) |= (byte) SpecialCharType.BetweenDigit;
                    }

                    #region comm.
                    /*
                    foreach ( var c in EXCLUDE_INTERPRETE_AS_WHITESPACE )
                    {
                        var cct = *(cctm + c);
                        if ( (cct & SpecialCharType.BetweenNonWhitespace) == SpecialCharType.BetweenNonWhitespace )
                            *(cctm + c) ^= SpecialCharType.BetweenNonWhitespace;
                        else
                        if ( (cct & SpecialCharType.InterpreteAsWhitespace) == SpecialCharType.InterpreteAsWhitespace )
                            *(cctm + c) ^= SpecialCharType.InterpreteAsWhitespace;
                    }
                    */
                    #endregion

                    //-ERROR-!!!-*(cctm + DOT) |= (byte) SpecialCharType.DotChar;
                    //-ONLY-SO--!!!-
                    *(cctm + DOT) = (byte) SpecialCharType.DotChar;
                }

                var spec_chartype_map_GCHandle = GCHandle.Alloc( spec_chartype_map, GCHandleType.Pinned );
                _SPEC_CHARTYPE_MAP = (SpecialCharType*) spec_chartype_map_GCHandle.AddrOfPinnedObject().ToPointer();
            }
            public static UnsafeConst Inst { get; } = new UnsafeConst();
        }

        public const string NUM_PLACEHOLDER = "[%NUM%]";

        #region [.cctor().]
        private static CharType*        _CTM;
        private static char*            _UIM;
        private static SpecialCharType* _SCTM;
        static Tokenizer()
        {
            _UIM  = xlat_Unsafe.Inst._UPPER_INVARIANT_MAP;
            _CTM  = xlat_Unsafe.Inst._CHARTYPE_MAP;
            _SCTM = UnsafeConst.Inst._SPEC_CHARTYPE_MAP;
        }
        #endregion

        #region [.private field's.]
        private const int DEFAULT_WORDSLIST_CAPACITY = 100;
        private const int DEFAULT_WORDTOUPPERBUFFER  = 100;

        private readonly SentSplitter                 _SentSplitter;
        private readonly UrlDetector                  _UrlDetector;
        private readonly List< word_t >               _Words;
        private readonly INerInputTypeProcessor       _NerInputTypeProcessor;        
        private char*                                 _BASE;
        private char*                                 _Ptr;        
        private int                                   _StartIndex;
        private int                                   _Length;
        private ProcessSentCallbackDelegate           _OuterProcessSentCallback_Delegate;
        private char*                                 _StartPtr;
        private char*                                 _EndPtr;
        private int                                   _WordToUpperBufferSize;
        private GCHandle                              _WordToUpperBufferGCHandle;
        private char*                                 _WordToUpperBufferPtrBase;
        private bool                                  _NotSkipNonLetterAndNonDigitToTheEnd; //need for NER-model-builder
        private SentSplitter.ProcessSentCallbackDelegate _SentSplitterProcessSentCallback_Delegate;
        private UmlautesNormalizer                    _UmlautesNormalizer;
        private readonly sent_t                       _NoSentsAllocateSent;
        private ProcessSentCallbackDelegate           _Dummy_ProcessSentCallbackDelegate;
        private ProcessSentCallbackDelegate           _AccumulateSents_ProcessSentCallbackDelegate;
        private readonly List< List< word_t > >       _AccumulateSents_Words;
        private bool                                  _ReplaceNumsOnPlaceholders;
        private bool                                  _IsPrevWordNumber;
        #endregion

        #region [.ctor().]
        public Tokenizer( TokenizerConfig config, bool replaceNumsOnPlaceholder /*= true*/ )
        {
            _SentSplitter = new SentSplitter( config.SentSplitterConfig );
            _Words        = new List< word_t >( DEFAULT_WORDSLIST_CAPACITY );
            _SentSplitterProcessSentCallback_Delegate = new SentSplitter.ProcessSentCallbackDelegate( SentSplitterProcessSentCallback );

            //--//
            ReAllocWordToUpperBuffer( DEFAULT_WORDTOUPPERBUFFER );
            _NerInputTypeProcessor = config.NerInputTypeProcessor ?? NerInputTypeProcessor_En.Inst;
            _UmlautesNormalizer    = new UmlautesNormalizer();

            _UrlDetector         = _SentSplitter.UrlDetector;
            _NoSentsAllocateSent = sent_t.CreateEmpty();
            _Dummy_ProcessSentCallbackDelegate = new ProcessSentCallbackDelegate( words => { }/*(words, urls) => { }*/ );

            _AccumulateSents_ProcessSentCallbackDelegate = new ProcessSentCallbackDelegate( Accumulate_ProcessSentCallback );
            _AccumulateSents_Words = new List< List< word_t > >( DEFAULT_WORDSLIST_CAPACITY );

            _ReplaceNumsOnPlaceholders = replaceNumsOnPlaceholder;
        }

        private void ReAllocWordToUpperBuffer( int newBufferSize )
        {
            DisposeNativeResources();

            _WordToUpperBufferSize = newBufferSize;
            var wordToUpperBuffer  = new char[ _WordToUpperBufferSize ];
            _WordToUpperBufferGCHandle = GCHandle.Alloc( wordToUpperBuffer, GCHandleType.Pinned );
            _WordToUpperBufferPtrBase  = (char*) _WordToUpperBufferGCHandle.AddrOfPinnedObject().ToPointer();
        }

        ~Tokenizer() => DisposeNativeResources();
        public void Dispose()
        {
            _SentSplitter?.Dispose();
            //_UrlDetector? .Dispose();

            DisposeNativeResources();
            GC.SuppressFinalize( this );
        }
        private void DisposeNativeResources()
        {
            if ( _WordToUpperBufferPtrBase != null )
            {
                _WordToUpperBufferGCHandle.Free();
                _WordToUpperBufferPtrBase = null;
            }
        }
        #endregion

        public INerInputTypeProcessor InputTypeProcessor { [M(O.AggressiveInlining)] get => _NerInputTypeProcessor; }
        public UrlDetector UrlDetector { [M(O.AggressiveInlining)] get => _UrlDetector; }
        public SentSplitter SentSplitter { [M(O.AggressiveInlining)] get => _SentSplitter; }
        public bool ReplaceNumsOnPlaceholders { [M(O.AggressiveInlining)] get => _ReplaceNumsOnPlaceholders; }

        #region [.Merge urls with words.]
        /// <summary>
        /// 
        /// </summary>
        private sealed class word_by_startIndex_Comparer : IComparer< word_t >
        {
            public static word_by_startIndex_Comparer Inst { [M(O.AggressiveInlining)] get; } = new word_by_startIndex_Comparer();
            private word_by_startIndex_Comparer() { }
            public int Compare( word_t x, word_t y ) => (x.startIndex - y.startIndex);
        }

        [M(O.AggressiveInlining)] private static word_t CreateWord( url_t url )
        {
            var w = new word_t()
            {
                nerInputType  = NerInputType.Other,                
                startIndex    = url.startIndex,
                length        = url.length,
                valueOriginal = url.value,
                valueUpper    = url.value,
            };

            switch ( url.type )
            {
                case UrlTypeEnum.Email: w.nerOutputType = NerOutputType.Email; break;
                case UrlTypeEnum.Url  : w.nerOutputType = NerOutputType.Url; break;
                //---default: throw (new ArgumentException( url.ToString() ));
            }

            return (w);
        }
        [M(O.AggressiveInlining)] private static void MergeUrlsToWords( List< word_t > words, List< url_t > urls )
        {
            if ( urls != null )
            {
                for ( var i = urls.Count - 1; 0 <= i; i-- )
                {
                    words.Add( CreateWord( urls[ i ] ) );
                }
                words.Sort( word_by_startIndex_Comparer.Inst );
            }
        }
        #endregion

        #region [.no-sents-allocate, no-urls-allocate.]
        /*private Tokenizer( INerInputTypeProcessor nerInputTypeProcessor )
        {
            _Words = new List< word_t >( DEFAULT_WORDSLIST_CAPACITY );
            _NoSentsAllocateSent = sent_t.CreateEmpty();
            _Dummy_ProcessSentCallbackDelegate = new ProcessSentCallbackDelegate( (words, urls) => { } );

            _UIM  = xlat_Unsafe.Inst._UPPER_INVARIANT_MAP;
            _CTM  = xlat_Unsafe.Inst._CHARTYPE_MAP;
            _SCTM = UnsafeConst.Inst._CRF_CHARTYPE_MAP;

            //--//
            ReAllocWordToUpperBuffer( DEFAULT_WORDTOUPPERBUFFER );

            _NerInputTypeProcessor = nerInputTypeProcessor;
            _UmlautesNormalizer    = new UmlautesNormalizer();
        }
        private Tokenizer( INerInputTypeProcessor nerInputTypeProcessor, UrlDetectorConfig urlDetectorConfig ) : this( nerInputTypeProcessor) => _UrlDetector = new UrlDetector( urlDetectorConfig );

        public static Tokenizer Create4NoSentsNoUrlsAllocate() => new Tokenizer( NerInputTypeProcessor_En.Inst );
        public static Tokenizer Create4NoSentsNoUrlsAllocate( INerInputTypeProcessor nerInputTypeProcessor ) => new Tokenizer( nerInputTypeProcessor );

        public static Tokenizer Create4NoSentsAllocate( UrlDetectorConfig urlDetectorConfig ) => new Tokenizer( NerInputTypeProcessor_En.Inst, urlDetectorConfig );
        public static Tokenizer Create4NoSentsAllocate( string urlDetectorResourcesXmlFilename ) => new Tokenizer( NerInputTypeProcessor_En.Inst, new UrlDetectorConfig( urlDetectorResourcesXmlFilename ) );
        public static Tokenizer Create4NoSentsAllocate( INerInputTypeProcessor nerInputTypeProcessor, UrlDetectorConfig urlDetectorConfig ) => new Tokenizer( nerInputTypeProcessor, urlDetectorConfig );
        public static Tokenizer Create4NoSentsAllocate( INerInputTypeProcessor nerInputTypeProcessor, string urlDetectorResourcesXmlFilename ) => new Tokenizer( nerInputTypeProcessor, new UrlDetectorConfig( urlDetectorResourcesXmlFilename ) );
        */
        #endregion

        #region [.Run.]
        public List< word_t > Run_NoSentsNoUrlsAllocate( string text )
        {
            _OuterProcessSentCallback_Delegate = _Dummy_ProcessSentCallbackDelegate;
            fixed ( char* _base = text )
            {
                _BASE = _base;
                _NoSentsAllocateSent.Set( 0, text.Length, null );
                SentSplitterProcessSentCallback( _NoSentsAllocateSent );
            }
            _OuterProcessSentCallback_Delegate = null;

            return (_Words);
        }
        public List< word_t > Run_NoSentsAllocate( string text )
        {
            _OuterProcessSentCallback_Delegate = _Dummy_ProcessSentCallbackDelegate;
            fixed ( char* _base = text )
            {
                _BASE = _base;

                var urls = _UrlDetector.AllocateUrls( text );
                _NoSentsAllocateSent.Set( 0, text.Length, (0 < urls.Count) ? urls : null );
                SentSplitterProcessSentCallback( _NoSentsAllocateSent );
            }
            _OuterProcessSentCallback_Delegate = null;

            //---MergeUrlsToWords( _Words, _NoSentsAllocateSent.urls );
            return (_Words);
        }

        public void Run( string text, ProcessSentCallbackDelegate processSentCallback )
        {
            _OuterProcessSentCallback_Delegate = processSentCallback;
            fixed ( char* _base = text )
            {
                _BASE = _base;
                _SentSplitter.AllocateSents( text, _SentSplitterProcessSentCallback_Delegate );
            }
            _OuterProcessSentCallback_Delegate = null;
        }
        public void Run_SimpleSentsAllocate( string text, ProcessSentCallbackDelegate processSentCallback )
        {
            _OuterProcessSentCallback_Delegate = processSentCallback;
            fixed ( char* _base = text )
            {
                _BASE = _base;
                _SentSplitter.AllocateSents_Simple( _base, text.Length, _SentSplitterProcessSentCallback_Delegate );
            }
            _OuterProcessSentCallback_Delegate = null;
        }
        
        public List< List< word_t > > Run( string text )
        {
            _AccumulateSents_Words.Clear();
            Run( text, _AccumulateSents_ProcessSentCallbackDelegate );
            return (_AccumulateSents_Words);
        }
        public List< List< word_t > > Run_SimpleSentsAllocate( string text )
        {
            _AccumulateSents_Words.Clear();
            Run_SimpleSentsAllocate( text, _AccumulateSents_ProcessSentCallbackDelegate );
            return (_AccumulateSents_Words);
        }
        private void Accumulate_ProcessSentCallback( List< word_t > words ) => _AccumulateSents_Words.Add( words.ToList( words.Count ) );
        #endregion

        [M(O.AggressiveInlining)] private void SentSplitterProcessSentCallback( sent_t sent )
        {
            _Words.Clear();
            _IsPrevWordNumber = false;
            _StartIndex = sent.startIndex;
            _Length     = 0;
            _StartPtr   = _BASE + _StartIndex;
            _EndPtr     = _StartPtr + sent.length - 1;

            var urls        = sent.urls;
            var urlIndex    = 0;
            var startUrlPtr = (urls != null) ? (_BASE + urls[ 0 ].startIndex) : UnsafeConst.MAX_PTR;

            #region [.main.]
            var realyEndPtr = _EndPtr;
            _EndPtr = SkipNonLetterAndNonDigitToTheEnd();

            for ( _Ptr = _StartPtr; _Ptr <= _EndPtr; _Ptr++ )
            {
                #region [.process allocated url's.]
                if ( startUrlPtr <= _Ptr )
                {
                    #region [.code.]
                    TryCreateWordAndPut2List();

                    var lenu = urls[ urlIndex ].length;
                    #region [.skip-ignore url's.]
                    /*
                    #region [.create word. url.]
                    var lenu = urls[ urlIndex ].length;
                    var vu = new string( startUrlPtr, 0, lenu );
                    var wu = new word_t()
                    {
                        startIndex         = urls[ urlIndex ].startIndex, 
                        length             = lenu, 
                        valueOriginal      = vu,
                        valueUpper         = vu,
                        posTaggerInputType = PosTaggerInputType.Url
                    };
                    _Words.Add( wu );
                    #endregion
                    //*/
                    #endregion

                    _Ptr = startUrlPtr + lenu - 1;
                    urlIndex++;
                    startUrlPtr = (urlIndex < urls.Count) ? (_BASE + urls[ urlIndex ].startIndex) : UnsafeConst.MAX_PTR;

                    _StartIndex = (int) (_Ptr - _BASE + 1);
                    _Length     = 0;
                    continue;

                    #endregion
                }
                #endregion

                var ch = *_Ptr;
                var ct = *(_CTM + ch);
                #region [.whitespace.]
                if ( ct.IsWhiteSpace() )
                {
                    TryCreateWordAndPut2List();

                    _StartIndex++;
                    continue;
                }
                #endregion
                
                var pct = *(_SCTM + ch);
                #region [.dot.]
                if ( ((pct & SpecialCharType.DotChar) == SpecialCharType.DotChar) && IsUpperNextChar() )
                {
                    _Length++;
                    TryCreateWordAndPut2List();
                    continue;
                }
                #endregion

                #region [.between-letter-or-digit.]
                if ( (pct & SpecialCharType.BetweenLetterOrDigit) == SpecialCharType.BetweenLetterOrDigit )
                {
                    if ( !ct.IsHyphen() && IsBetweenLetterOrDigit() ) //always split by Hyphen-Dash
                    {
                        _Length++;
                    }
                    else
                    {
                        TryCreateWordAndPut2List();

                        #region [.merge punctuation (with white-space's).]
                        if ( !MergePunctuation( ch ) )
                            break;
                        #endregion

                        //punctuation word
                        TryCreateWordAndPut2List();
                    }

                    continue;
                }
                //с учетом того, что списки 'BetweenLetterOrDigit' и 'BetweenDigit' не пересекаются
                else if ( (pct & SpecialCharType.BetweenDigit) == SpecialCharType.BetweenDigit )
                {
                    if ( IsBetweenDigit() )
                    {
                        _Length++;
                    }
                    else
                    {
                        TryCreateWordAndPut2List();

                        #region [.merge punctuation (with white-space's).]
                        if ( !MergePunctuation( ch ) )
                            break;
                        #endregion

                        //punctuation word
                        TryCreateWordAndPut2List();
                    }

                    continue;                    
                }
                #endregion

                #region [.tokenize-different-separately.]
                if ( (pct & SpecialCharType.TokenizeDifferentSeparately) == SpecialCharType.TokenizeDifferentSeparately )
                {
                    TryCreateWordAndPut2List();

                    #region [.merge punctuation (with white-space's).]
                    if ( !MergePunctuation( ch ) )
                        break;
                    #region 
                    /*
                    _Length = 1;
                    _Ptr++;
                    for ( ; _Ptr <= _EndPtr; _Ptr++ ) 
                    {
                        var ch_next = *_Ptr;
                        if ( ch_next != ch )
                            break;

                        _Length++;
                    }
                    if ( _EndPtr < _Ptr )
                    {
                        if ( (_Length == 1) && (*_EndPtr == '\0') )
                            _Length = 0;
                        break;
                    }
                    _Ptr--;
                    */
                    #endregion
                    #endregion

                    //punctuation word
                    TryCreateWordAndPut2List();

                    continue;
                }
                #endregion

                #region [.interprete-as-whitespace.]
                if ( (pct & SpecialCharType.InterpreteAsWhitespace) == SpecialCharType.InterpreteAsWhitespace )
                {
                    TryCreateWordAndPut2List();

                    _StartIndex++;
                    continue;
                }
                #endregion

                #region [.increment length.]
                _Length++;
                #endregion
            }
            #endregion

            #region [.last word.]
            TryCreateWordAndPut2List();
            #endregion

            #region [.tail punctuation.]
            for ( _EndPtr = realyEndPtr; _Ptr <= _EndPtr; _Ptr++ )
            {
                var ch = *_Ptr;
                var ct = *(_CTM + ch);
                #region [.whitespace.]
                if ( ct.IsWhiteSpace() )
                {
                    TryCreateWordAndPut2List();

                    _StartIndex++;
                    continue;
                }
                #endregion
                
                var nct = *(_SCTM + ch);
                #region [.tokenize-different-separately.]
                if ( (nct & SpecialCharType.TokenizeDifferentSeparately) == SpecialCharType.TokenizeDifferentSeparately )
                {
                    TryCreateWordAndPut2List();

                    #region [.merge punctuation (with white-space's).]
                    if ( !MergePunctuation( ch ) )
                        break;
                    #endregion

                    //punctuation word
                    TryCreateWordAndPut2List();

                    continue;
                }
                #endregion

                #region [.interprete-as-whitespace.]
                if ( (nct & SpecialCharType.InterpreteAsWhitespace) == SpecialCharType.InterpreteAsWhitespace )
                {
                    TryCreateWordAndPut2List();

                    _StartIndex++;
                    continue;
                }
                #endregion

                #region [.increment length.]
                _Length++;
                #endregion
            }
            #endregion

            #region [.last punctuation.]
            TryCreateWordAndPut2List();
            #endregion

            MergeUrlsToWords( _Words, sent.urls );
            _OuterProcessSentCallback_Delegate( _Words/*, sent.urls*/ );
        }

        [M(O.AggressiveInlining)] private word_t Create_NUM_PLACEHOLDER_Word()
            => new word_t()
            {
                startIndex    = _StartIndex, 
                length        = _Length, 
                valueOriginal = NUM_PLACEHOLDER,
                valueUpper    = NUM_PLACEHOLDER,
                nerInputType  = NerInputType.Num,
                extraWordType = ExtraWordType.IntegerNumber
            };
        private void TryCreateWordAndPut2List()
        {
            if ( _Length != 0 )
            {
                var startPtr = _BASE + _StartIndex;

                #region [.replace or skip second-and-next-num-words.]
                var is_num = _ReplaceNumsOnPlaceholders && IsDigitsWithPunctuations( startPtr, _Length );
                if ( is_num )
                {
                    if ( !_IsPrevWordNumber )
                    {
                        _IsPrevWordNumber = true;
                        _Words.Add( Create_NUM_PLACEHOLDER_Word() );
                    }
                    //#region [.inctement start-index.]
                    //_StartIndex += _Length;
                    //_Length      = 0;
                    //#endregion
                    //return;
                    goto EXIT;
                }
                _IsPrevWordNumber = false;
                #endregion

                #region [.to upper invariant & pos-tagger-list & etc.]
                if ( _WordToUpperBufferSize < _Length )
                {
                    ReAllocWordToUpperBuffer( _Length );
                }                
                for ( int i = 0; i < _Length; i++ )
                {
                    *(_WordToUpperBufferPtrBase + i) = *(_UIM + *(startPtr + i));
                }
                var valueUpper = new string( _WordToUpperBufferPtrBase, 0, _Length );
                #endregion

                #region [.create word.]
                var valueOriginal = new string( _BASE, _StartIndex, _Length );
                var word = new word_t()
                {
                    startIndex    = _StartIndex, 
                    length        = _Length, 
                    valueOriginal = valueOriginal,
                    valueUpper    = valueUpper,
                };
                #endregion

                #region [.nerInputType.]
                (word.nerInputType, word.extraWordType) = _NerInputTypeProcessor.GetNerInputType( _BASE + _StartIndex, _Length );

                if ( ((word.extraWordType & ExtraWordType.HasUmlautes) == ExtraWordType.HasUmlautes) /*&& (_UmlautesNormalizer != null) //---ALWAYE NOT_NULL(?)---// */ )
                {
                    //---word.valueOriginal__UmlautesNormalized = _UmlautesNormalizer.Normalize( _BASE + _StartIndex, _Length );
                    word.valueUpper__UmlautesNormalized = _UmlautesNormalizer.Normalize_ToUpper( _WordToUpperBufferPtrBase, _Length );
                }
                #endregion

                #region [.put-2-list.]
                if ( (word.extraWordType & ExtraWordType.Punctuation) == ExtraWordType.Punctuation )
                {
                    //---Clear_valueOriginal( word );
                    word.valueOriginal = ClearPunctuationValue( word.valueOriginal, word.length );
                    word.valueUpper    = ClearPunctuationValue( word.valueUpper   , word.length );

                    if ( ((word.extraWordType & ExtraWordType.HasUmlautes) == ExtraWordType.HasUmlautes) )
                    {
                        word.valueUpper__UmlautesNormalized = ClearPunctuationValue( word.valueUpper__UmlautesNormalized, word.length );
                    }
                }

                _Words.Add( word );
                #endregion
            EXIT:
                #region [.inctement start-index.]
                _StartIndex += _Length;
                _Length      = 0;
                #endregion
            }
        }

        [M(O.AggressiveInlining)] private char* SkipNonLetterAndNonDigitToTheEnd()
        {
            //need for NER-model-builder
            if ( _NotSkipNonLetterAndNonDigitToTheEnd )
                return (_EndPtr);

            for ( char* ptr = _EndPtr; _StartPtr <= ptr; ptr-- )
            {
                var ct = *(_CTM + *ptr);
                if ( ct.IsLetter() || ct.IsDigit() )
                {
                    #region [.если на конце предложения одиночная буква большая, то точку не отрывать.]
                    if ( ct.IsUpper() )
                    {
                        var p = ptr - 1;
                        if ( (_StartPtr == p) || ((_StartPtr < p) && (*(_CTM + *p)).IsWhiteSpace()) )
                        {
                            p = ptr + 1;
                            if ( (p == _EndPtr) || ((p < _EndPtr) && (*(_CTM + *(p + 1))).IsWhiteSpace()) )
                            {
                                if ( xlat.IsDot( *p ) )
                                return (p);
                            }
                        }
                    }
                    #endregion

                    return (ptr);
                }
            }
            return (_StartPtr - 1);
        }

        [M(O.AggressiveInlining)] private bool IsBetweenLetterOrDigit()
        {
            if ( _Ptr <= _StartPtr )
                return (false);

            var ch = *(_Ptr - 1);
            var ct = *(_CTM + ch);
            if ( !ct.IsLetter() && !ct.IsDigit() )
            {
                return (false);
            }

            var p = _Ptr + 1;
            if ( _EndPtr <= p )
            {
                if ( _EndPtr < p )
                    return (false);
                ch = *p;
                if ( ch == '\0' )
                    return (false);
            }
            else
            {
                ch = *p;
            }
            ct = *(_CTM + ch);
            if ( !ct.IsLetter() && !ct.IsDigit() )
            {
                return (false);
            }

            return (true);
        }
        [M(O.AggressiveInlining)] private bool IsBetweenDigit()
        {
            if ( _Ptr <= _StartPtr )
                return (false);

            var ch = *(_Ptr - 1);
            var ct = *(_CTM + ch);
            if ( !ct.IsDigit() )
            {
                return (false);
            }

            var p = _Ptr + 1;
            if ( _EndPtr <= p )
            {
                if ( _EndPtr < p )
                    return (false);
                ch = *p;
                if ( ch == '\0' )
                    return (false);
            }
            else
            {
                ch = *p;
            }
            ct = *(_CTM + ch);
            if ( !ct.IsDigit() )
            {
                return (false);
            }

            return (true);
        }
        [M(O.AggressiveInlining)] private bool IsUpperNextChar()
        {
            var p = _Ptr + 1;
            var ch = default(char);
            if ( _EndPtr <= p )
            {
                if ( _EndPtr < p )
                    return (false);
                ch = *p;
                if ( ch == '\0' )
                    return (false);
            }
            else
            {
                ch = *p;
            }

            var ct = *(_CTM + ch);
            if ( !ct.IsUpper() )
            {
                return (false);
            }

            return (true);
        }

        [M(O.AggressiveInlining)] private bool MergePunctuation( char begining_ch )
        {
            _Length = 1;
            _Ptr++;
            var whitespace_length = 0;
            for ( ; _Ptr <= _EndPtr; _Ptr++ ) 
            {                
                var ch_next = *_Ptr;
                var ct = *(_CTM  + ch_next);
                if ( ct.IsWhiteSpace() )
                {
                    whitespace_length++;
                    continue;
                }

                var nct = *(_SCTM + ch_next);
                if ( (nct & SpecialCharType.InterpreteAsWhitespace) == SpecialCharType.InterpreteAsWhitespace )
                {
                    whitespace_length++;
                    continue;
                }

                if ( ch_next == begining_ch )
                {
                    _Length += whitespace_length + 1;
                    whitespace_length = 0;
                    continue;
                }

                break;
            }
            if ( _EndPtr < _Ptr )
            {
                if ( (_Length == 1) && (*_EndPtr == '\0') )
                    _Length = 0;
                return (false);
            }
            _Ptr -= whitespace_length + 1;

            return (true);
        }

        [M(O.AggressiveInlining)] public string NormalizeUmlautes( string word ) => _UmlautesNormalizer.Normalize( word );
        [M(O.AggressiveInlining)] public string NormalizeUmlautes_ToUpper( string word ) => _UmlautesNormalizer.Normalize_ToUpper( word );

        [M(O.AggressiveInlining)] unsafe private static bool IsDigitsWithPunctuations( char* ptr, int length )
        {
            var hasDigits = false;
            for ( var i = length - 1; 0 <= i; i-- )
            {
                hasDigits |= ((_CTM[ ptr[ i ] ] & CharType.IsDigit) == CharType.IsDigit);
                if ( !hasDigits && !((_CTM[ ptr[ i ] ] & CharType.IsPunctuation) == CharType.IsPunctuation) )
                {
                    return (false);
                }
            }
            return (hasDigits);
        }      

        //[M(O.AggressiveInlining)] private static void Clear_valueOriginal( word_t w )
        //{            
        //    if ( (w.extraWordType & ExtraWordType.Punctuation) == ExtraWordType.Punctuation )
        //    {
        //        var v = w.valueOriginal;
        //        if ( w.length == 1 )
        //        {
        //            var ch = w.valueOriginal[ 0 ];
        //            switch ( ch )
        //            {
        //                case ':': case '.': case ',': case ';': case '?': case '!': case '(': case ')': case '/': case '%': case '&': case '…': break;
        //                default:
        //                    var ct = xlat.CHARTYPE_MAP[ ch ];
        //                    if ( xlat.IsHyphen( ct ) )
        //                    {
        //                        if ( ch != '-' )
        //                        {
        //                            v = "-";
        //                        }
        //                    }
        //                    else if ( (ct & CharType.IsQuote) == CharType.IsQuote )
        //                    {
        //                        switch ( ch )
        //                        {
        //                            case '\"': case '\'': case '[': case ']': break;
        //                            default:
        //                                v = "\"";
        //                                break;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        switch ( ch )
        //                        {
        //                            case '[': case ']': case '{': case '}': break;
        //                            default:
        //                                int ttttt = 0;
        //                                break;
        //                        }                                            
        //                    }
        //                break;
        //            }
        //        }
        //        else if ( v == "''" )
        //        {
        //            v = "\"";
        //        }
        //        else if ( v == ",," )
        //        {
        //            v = ",";
        //        }
        //        else if ( v == "--" )
        //        {
        //            v = "-";
        //        }
        //        else if ( v != "..." && v != "//" && v != ".+" )
        //        {
        //            int tttttt = 0;
        //        }
        //        w.valueOriginal = v;
        //    }
        //}
        [M(O.AggressiveInlining)] private static string ClearPunctuationValue( string v, int length )
        {
            //Alway  ((extraWordType & ExtraWordType.Punctuation) == ExtraWordType.Punctuation)

            if ( length == 1 )
            {
                var ch = v[ 0 ];
                switch ( ch )
                {
                    case ':': case '.': case ',': case ';': case '?': case '!': case '(': case ')': case '/': case '%': case '&': case '…': break;
                    default:
                        var ct = xlat.CHARTYPE_MAP[ ch ];
                        if ( xlat.IsHyphen( ct ) )
                        {
                            if ( ch != '-' )
                            {
                                v = "-";
                            }
                        }
                        else if ( (ct & CharType.IsQuote) == CharType.IsQuote )
                        {
                            switch ( ch )
                            {
                                case '\"': case '\'': case '[': case ']': break;
                                default:
                                    v = "\"";
                                    break;
                            }
                        }
                        else
                        {
                            switch ( ch )
                            {
                                case '[': case ']': case '{': case '}': break;
                                default:
                                    int ttttt = 0;
                                    break;
                            }                                            
                        }
                    break;
                }
            }
            else if ( v == "''" )
            {
                v = "\"";
            }
            else if ( v == ",," )
            {
                v = ",";
            }
            else if ( v == "--" )
            {
                v = "-";
            }
            else if ( v != "..." && v != "//" && v != ".+" )
            {
                int tttttt = 0;
            }
            return (v);
        }
    
        [M(O.AggressiveInlining)] public static List< string > ToNerInputTokens( List< word_t > input_words, bool upperCase )
        {
            var input_tokens = new List< string >( input_words.Count );
            if ( upperCase )
            {
                foreach ( var w in input_words ) input_tokens.Add( w.valueUpper );
            }
            else
            {
                foreach ( var w in input_words ) input_tokens.Add( w.valueOriginal );
            }
            return (input_tokens);
        }
    }
}
