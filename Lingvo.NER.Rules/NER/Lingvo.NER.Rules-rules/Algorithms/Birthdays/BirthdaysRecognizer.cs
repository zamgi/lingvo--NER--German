using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;
using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Birthdays
{
    /// <summary>
    ///
    /// </summary>
    internal sealed class BirthdaysRecognizer
    {
        /// <summary>
        /// 
        /// </summary>
        internal sealed class WordsChainDictionary
        {
            private Map< string, WordsChainDictionary > _Slots;
            private bool _IsLeaf;
            private WordsChainDictionary _Epilogue;
            public WordsChainDictionary() => _Slots = new Map< string, WordsChainDictionary >( StringComparer.OrdinalIgnoreCase );

            public static WordsChainDictionary Create4Epilogue( IList< word_t > words )
            {
                var epilogue = new WordsChainDictionary();
                epilogue.Add( words );
                return (epilogue);
            }

            #region [.append words.]
            public void Add( IList< word_t > words, WordsChainDictionary epilogue = null )
            {
                var idx = 0;
                var count = words.Count;
                for ( WordsChainDictionary _this = this, _this_next; ; )
                {
                    #region [.save.]
                    if ( count == idx )
	                {
                        _this._IsLeaf   = true;
                        _this._Epilogue = epilogue;
                        return;
                    }
                    #endregion

                    var v = words[ idx ].valueUpper;
                    if ( !_this._Slots.TryGetValue( v, out _this_next ) )
                    {
                        _this_next = new WordsChainDictionary();
                        _this._Slots.Add( v, _this_next );
                    }                
                    _this = _this_next;
                    idx++;
                }
            }
            /*public void Add( string word )
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
            }*/
            #endregion

            #region [.try get.]
            [M(O.AggressiveInlining)] public bool TryGetFirst( IList< word_t > words, int startIndex, out (WordsChainDictionary epilogue, int length) x )
            {
                x = default;

                var startIndex_saved = startIndex;
                var count = words.Count;
                for ( WordsChainDictionary _this = this, _this_next; ; )
                {
                    #region [.get.]
                    if ( _this._IsLeaf )
                    {
                        x.length   = (startIndex - startIndex_saved);
                        x.epilogue = _this._Epilogue;
                        //--return (true);
                    }
                    #endregion

                    if ( count == startIndex )
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

                return (x.length != 0);
            }
            #endregion
    #if DEBUG
            public override string ToString() => (_IsLeaf ? "true" : $"count: {_Slots.Count}") + ((_Epilogue != null) ? " [HAS EPILOGUE]" : null);
    #endif
        }

        #region [.cctor().]
        private static string[] DATETIME_KNOWN_FORMATS;
        private static WordsChainDictionary _TextPreambles;
        static BirthdaysRecognizer()
        {
            DATETIME_KNOWN_FORMATS = new[]
            {
                "dd.MM.yyyy", "MM.dd.yyyy", "yyyy.dd.MM", "yyyy.MM.dd",
                "dd-MM-yyyy", "MM-dd-yyyy", "yyyy-dd-MM", "yyyy-MM-dd",
                "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/dd/MM", "yyyy/MM/dd",
            };

            Init_TextPreambles();
        }

        private static void Init_TextPreambles()
        {
            using var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate();

            _TextPreambles = new WordsChainDictionary();

            #region [-1-]
            var texts_1 = new[] 
            { 
                "Geburtsdatum", "Geboren am", "Geb. am",

                "Geburtstag",
                "Geburtsdatum",
                "geboren",
                "geboren am",
                "geb.",
                "geb am",
                "Geb",
                "Geb.",
                "Geburt",
                "gebürtig",
                "gebürtig am",
                "birthday",
                "birth day",
                "date of birth",
                "date-of-birth",
                "DOB",
            };
            foreach ( var t in texts_1 )
            {
                var tokens = tokenizer.Run_NoSentsNoUrlsAllocate( t );
                _TextPreambles.Add( tokens );

                if ( tokens.Last().valueUpper.LastChar() == '.' )
                {
                    tokens = tokenizer.Run_NoSentsNoUrlsAllocate( t + " 1234567" );
                    var i = tokens.Count - 1; if ( 0 <= i ) tokens.RemoveAt_Ex( i );
                    _TextPreambles.Add( tokens );
                }
            }
            #endregion

            #region [-2-]
            var puncts = xlat.GetHyphens().Concat( new[] { ':' } );
            foreach ( var p in puncts )
            {
                foreach ( var t in texts_1 )
                {
                    _TextPreambles.Add( tokenizer.Run_NoSentsNoUrlsAllocate( t + p ) );
                }
            }
            #endregion

            #region [-3-]
            _TextPreambles.Add( tokenizer.Run_NoSentsNoUrlsAllocate( "am" ).ToArray(), 
                                WordsChainDictionary.Create4Epilogue( tokenizer.Run_NoSentsNoUrlsAllocate( "geboren" ) ) );
            #endregion
        }
        #endregion

        #region [.ctor().]
        private const char SPACE = ' ';
        private DatesSearcher        _DatesSearcher;
        private DateTimeFormatInfo[] _DateTimeFormats;
        private StringBuilder        _ValueUpperBuff;
        private StringBuilder        _ValueOriginalBuff;
        public BirthdaysRecognizer( params DateTimeFormatInfo[] dateTimeFormats )
        {
            _DateTimeFormats   = dateTimeFormats.ToArray();
            _DatesSearcher     = new DatesSearcher( _DateTimeFormats );
            _ValueUpperBuff    = new StringBuilder( 100 );
            _ValueOriginalBuff = new StringBuilder( 100 );
        }
        #endregion

        [M(O.AggressiveInlining)] private bool TryParse_As_DateTime( string s, out DateTime dt )
        {
            switch ( _DateTimeFormats.Length )
            {
                case 1:
                    if ( DateTime.TryParse( s, _DateTimeFormats[ 0 ], DateTimeStyles.None, out dt ) )
                    {
                        return (true);
                    }
                break;

                case 2:
                    if ( DateTime.TryParse( s, _DateTimeFormats[ 0 ], DateTimeStyles.None, out dt ) )
                    {
                        return (true);
                    }
                    if ( DateTime.TryParse( s, _DateTimeFormats[ 1 ], DateTimeStyles.None, out dt ) )
                    {
                        return (true);
                    }
                break;

                default:
                    foreach ( var dtfi in _DateTimeFormats )
                    {
                        if ( DateTime.TryParse( s, dtfi, DateTimeStyles.None, out dt ) )
                        {
                            return (true);
                        }
                    }
                break;
            }

            if ( DateTime.TryParseExact( s, DATETIME_KNOWN_FORMATS, null, DateTimeStyles.None, out dt ) )
            {
                return (true);
            }

            return (false);
        }
        [M(O.AggressiveInlining)] private static void ReplaceSpaces( StringBuilder buff )
        {
            for ( int i = 0, len = buff.Length - 1; i <= len; i++ )
            {
                if ( buff[ i ].IsWhiteSpace() )
                {
                    if ( (!(0 < i)   || !buff[ i - 1 ].IsLetter()) &&
                         (!(i < len) || !buff[ i + 1 ].IsLetter())
                       )
                    {
                        buff.Remove( i, 1 );
                        len--;
                        i--;
                    }
                }
            }
        }


        private (string valueUpper, string valueOriginal, int length, DateTime dt) _Pdi;
        private bool TryParseDate( List< word_t > words, in SearchResult sr ) //, ref (string valueUpper, string valueOriginal, int length, DateTime dt) x )
        {
            var w1 = words[ sr.StartIndex ];
            if ( (w1.valueUpper == null) || !w1.IsOutputTypeOther() )
            {
                _Pdi.length = 0;
                return (false);
            }

            #region [.core of mean.]
            _ValueUpperBuff   .Append( w1.valueUpper    ).Append( SPACE );
            _ValueOriginalBuff.Append( w1.valueOriginal ).Append( SPACE );

            var t = default(word_t);
            var t_prev = w1;
            for ( int i = sr.StartIndex + 1, j = sr.Length; 1 < j; j--, i++ )
            {
                t = words[ i ];
                if ( (t_prev.endIndex() == t.startIndex) && !t_prev.IsExtraWordTypePunctuation() && !t.IsExtraWordTypePunctuation() )
                {
                    _Pdi.length = 0;
                    goto EXIT;
                }
                _ValueUpperBuff   .Append( t.valueUpper    ).Append( SPACE );
                _ValueOriginalBuff.Append( t.valueOriginal ).Append( SPACE );
                t_prev = t;
            }

            ReplaceSpaces( _ValueOriginalBuff );

            var v = _ValueOriginalBuff.ToString();
            if ( TryParse_As_DateTime( v, out _Pdi.dt ) )
            {
                ReplaceSpaces( _ValueUpperBuff );

                //_Pdi.dt = dt;
                _Pdi.valueOriginal = v;
                _Pdi.valueUpper    = _ValueUpperBuff.ToString();
                _Pdi.length        = (t.startIndex - w1.startIndex) + t.length;

                ////w1.MarkAsDateTime( (dt, DateTimeType.Date, tt.inAlternateFormat) );
                //w1.nerOutputType = NerOutputType.Birthday;
                //w1.valueOriginal = v;
                //w1.valueUpper    = _ValueUpperBuff.ToString();
                //w1.length        = (t.startIndex - w1.startIndex) + t.length;
                ////---w1.ExtraWordType = ExtraWordType.TextDate;

                //for ( int i = sr.StartIndex + 1, j = sr.Length; 1 < j; j--, i++ )
                //{
                //    words[ i ].ClearValuesAndNerChain();
                //}
            }
            else
            {
                _Pdi.length = 0;
            }

        EXIT:
            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();
            #endregion

            //#region [.remove merged words.]
            //words.RemoveWhereValueOriginalIsNull();
            //#endregion

            return (_Pdi.length != 0);
        }
        private void CreateBirthdayWord( List< word_t > words, in SearchResult sr )
        {
            var w1 = words[ sr.StartIndex ];
            w1.ClearValuesAndNerChain();

            var bw = new BirthdayWord( w1.startIndex, in _Pdi.dt )
            {
                valueOriginal = _Pdi.valueOriginal,
                valueUpper    = _Pdi.valueUpper,
                length        = _Pdi.length,
            };
            words[ sr.StartIndex ] = bw;

            for ( int i = sr.StartIndex + 1, j = sr.Length; 1 < j; j--, i++ )
            {
                words[ i ].ClearValuesAndNerChain();
            }
        }

        #region [.public method's.]        
        public void Run( List< word_t > words )
        {
            var has = false;
            for ( int index = 0, len = words.Count; index < len; index++ )
            {
                var w = words[ index ];
                if ( (w.valueUpper == null) || w.IsInputTypeNum() || w.IsExtraWordTypePunctuation() ) //!w.IsOutputTypeOther() )
                {
                    continue;
                }

                if ( !_TextPreambles.TryGetFirst( words, index, out var x ) )
                {
                    continue;
                }

                //var preambleWordIndex = index;
                var startIndex = index + x.length;
                if ( !_DatesSearcher.TryFindFirst( words, startIndex, out var sr ) )
                {
                    continue;
                }

                if ( !TryParseDate( words, in sr ) )
                {
                    continue;
                }

                var startIndex2 = sr.StartIndex + sr.Length;
                if ( x.epilogue != null )
                {
                    if ( !x.epilogue.TryGetFirst( words, startIndex2, out var x2 ) )
                    {
                        continue;
                    }
                    startIndex2 += x2.length;
                }

                CreateBirthdayWord( words, in sr );
                index = startIndex2 - 1;
                has = true;
            }

            #region [.remove merged words.]
            if ( has )
            {
                words.RemoveWhereValueOriginalIsNull();
            }
            #endregion
        }
        #endregion
    }
}
