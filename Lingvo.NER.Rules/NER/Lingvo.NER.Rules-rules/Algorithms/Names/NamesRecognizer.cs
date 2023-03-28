using System;
using System.Collections.Generic;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Names
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class NamesRecognizer : IDisposable
    {
        #region [.cctor().]
        private static Set< string > _BeforeCannotWords;
        static NamesRecognizer()
        {
            var beforeCannotWords = new[] 
            {
                "der",
                "die",
                "das",
                "eine",
                "ein",
                "zu",
                "zurm",
                "zum",
                "von",
                "vom",
                "in",
                "im",
                "auf",
                "aufm",            
            };
            _BeforeCannotWords = Set< string >.CreateWithCloserCapacity( beforeCannotWords.Length << 1 );

            for ( var i = beforeCannotWords.Length - 1; 0 <= i; i-- )
            {
                var w = beforeCannotWords[ i ];
                _BeforeCannotWords.Add( w );

                w = xlat.UPPER_INVARIANT_MAP[ w[ 0 ] ] + w.Substring( 1 );
                _BeforeCannotWords.Add( w );
            }            
        }
        #endregion

        #region [.ctor().]
        private const char SPACE = ' ';
        private NameSearcher                 _FirstNamesSearcher;
        private SurNameSearcher              _SurNamesSearcher;
        private NamesSearcher_ByTextPreamble _Searcher_ByTextPreamble;
        private INamesModel                  _Model;
        private StringBuilder                _ValueUpperBuff;
        private StringBuilder                _ValueOriginalBuff;
        private Func< string >               _GetOriginalTextFunc;
        private string                       _CurrentOriginalText;

        public NamesRecognizer( INamesModel model, Func< string > getOriginalTextFunc )
        {
            _Model = model;
            _GetOriginalTextFunc = getOriginalTextFunc;

            _FirstNamesSearcher      = new NameSearcher( model.FirstNames );
            _SurNamesSearcher        = new SurNameSearcher( model.SurNames   );
            _Searcher_ByTextPreamble = new NamesSearcher_ByTextPreamble( this );

            _ValueUpperBuff    = new StringBuilder( 100 );
            _ValueOriginalBuff = new StringBuilder( 100 );
        }
        public void Dispose() => _SurNamesSearcher.Dispose();
        #endregion

        public void Recognize( List< word_t > words )
        {            
            //#1
            _Searcher_ByTextPreamble.TryRecognizeAll( words );

            //#2
            Recognize_FullNames( words );

            //end.
            _CurrentOriginalText = null;
        }

        private void Recognize_FullNames( List< word_t > words )
        {
            //#1 => (First-name) + (Sur-names)
            if ( _FirstNamesSearcher.TryFindAll( words, out var firstNames ) )
            {
                var wasCreateWord = false;
                foreach ( var firstName in firstNames )
                {
                    var w1 = words[ firstName.StartIndex ];
                    if ( !CanProcess( w1 ) || !CanProcess_Use_BeforeCannotWords( words, firstName.StartIndex ) )
                        continue;

                    var firstName_EndIndex = firstName.EndIndex();
                    if ( !_SurNamesSearcher.TryFindFirst2Rigth( words, firstName_EndIndex, out var surName ) )
                    {
                        //allowed comma between fn & sn => "Petra, Freudenberger-Lötz und Anita, Müller-Friese"
                        if ( (words.Count <= firstName_EndIndex) ||
                             !words[ firstName_EndIndex ].IsExtraWordTypeComma() ||
                             !_SurNamesSearcher.TryFindFirst2Rigth( words, firstName_EndIndex + 1, out surName )
                           )
                        {
                            continue;
                        }
                    }

                    wasCreateWord |= CreateNameWord( words, w1, in firstName, in surName );
                }

                #region [.remove merged words.]
                if ( wasCreateWord )
                {
                    words.RemoveWhereValueOriginalIsNull();
                }
                #endregion

                //#2 => (Sur-names) + (First-name)
                if ( _SurNamesSearcher.TryFindAll( words, out var surNames ) )
                {
                    wasCreateWord = false;
                    foreach ( var surName in surNames )
                    {
                        /*
                        //Фамилия не может идти первым словом в предложении (Не уверен, что это всегда так - но пока не смог подобрать обратных примеров, так что пока исходим из этой предпосылки)
                        if ( surName.StartIndex == 0 )
                            continue;
                        */

                        var w1 = words[ surName.StartIndex ];
                        if ( !CanProcess ( w1 ) || !CanProcess_Use_BeforeCannotWords( words, surName.StartIndex ) )
                            continue;

                        var surName_EndIndex = surName.EndIndex();
                        if ( !_FirstNamesSearcher.TryFindFirst2Rigth( words, surName_EndIndex, out var firstName ) )
                        {
                            //allowed comma between sn & fn => "Freudenberger-Lötz, Petra und Müller-Friese, Anita"
                            if ( (words.Count <= surName_EndIndex) ||
                                 !words[ surName_EndIndex ].IsExtraWordTypeComma() ||
                                 !_FirstNamesSearcher.TryFindFirst2Rigth( words, surName_EndIndex + 1, out firstName )
                               )
                            {
                                continue;
                            }
                        }

                        wasCreateWord |= CreateNameWord( words, w1, in firstName, in surName );
                    }

                    #region [.remove merged words.]
                    if ( wasCreateWord )
                    {
                        words.RemoveWhereValueOriginalIsNull();
                    }
                    #endregion
                }
            }
        }

        internal bool TryRecognize_FullName_AfterTextPreamble( List< word_t > words, int startIndex, TextPreambleTypeEnum tpt, out int endIndex )
        {
            //#1 => (First-name) + (Sur-names)
            SearchResult surName;
            if ( _FirstNamesSearcher.TryFindFirst2Rigth( words, startIndex, out var firstName ) )
            {
                var w1 = words[ firstName.StartIndex ];
                if ( CanProcess( w1 ) && _SurNamesSearcher.TryFindFirst2Rigth( words, firstName.EndIndex(), out surName ) )
                {
                    if ( CreateNameWord( words, w1, in firstName, in surName, tpt ) )
                    {
                        words.RemoveWhereValueOriginalIsNull( out var removeCount );

                        endIndex = surName.EndIndex() - removeCount - 1;
                        return (true);
                    }
                }
            }

            //#2 => (Sur-names) + (First-name)
            if ( _SurNamesSearcher.TryFindFirst2Rigth( words, startIndex, out surName ) )
            {
                var w1 = words[ surName.StartIndex ];
                if ( CanProcess( w1 ) && _FirstNamesSearcher.TryFindFirst2Rigth( words, surName.EndIndex(), out firstName ) )
                {
                    if ( CreateNameWord( words, w1, in firstName, in surName, tpt ) )
                    {
                        words.RemoveWhereValueOriginalIsNull( out var removeCount );

                        endIndex = firstName.EndIndex() - removeCount - 1;
                        return (true);
                    }
                }
            }

            endIndex = default;
            return (false);
        }
        internal bool TryRecognize_FirstNameAbbreviation_And_SurName_AfterTextPreamble( List< word_t > words, int startIndex, TextPreambleTypeEnum tpt, in SearchResult firstName, out int endIndex )
        {      
            //#1 => (Sur-names)
            if ( _SurNamesSearcher.TryFindFirst2Rigth( words, startIndex, out var surName ) )
            {
                var w1 = words[ surName.StartIndex ];
                if ( CanProcess( w1 ) )
                {
                    if ( firstName.StartIndex < surName.StartIndex ) w1 = words[ firstName.StartIndex ];
                    if ( CreateNameWord( words, w1, in firstName, in surName, tpt ) )
                    {
                        words.RemoveWhereValueOriginalIsNull( out var removeCount );

                        endIndex = surName.EndIndex() - removeCount - 1;
                        return (true);
                    }
                }
            }

            endIndex = default;
            return (false);
        }
        internal bool TryRecognize_SurNameOnly_AfterTextPreamble( List< word_t > words, int startIndex, TextPreambleTypeEnum tpt, out int endIndex )
        {      
            //#1 => (Sur-names)
            if ( _SurNamesSearcher.TryFindFirst2Rigth( words, startIndex, out var surName ) )
            {
                var w1 = words[ surName.StartIndex ];
                if ( CanProcess( w1 ) )
                {
                    CreateNameWord_SurName( words, w1, in surName, tpt );
                    words.RemoveWhereValueOriginalIsNull( out var removeCount );

                    endIndex = surName.EndIndex() - removeCount - 1;
                    return (true);
                }
            }

            endIndex = default;
            return (false);
        }

        public void Recognize_FirstNamesOnly( List< word_t > words )
        {
            //#1 => (First-names) 
            if ( _FirstNamesSearcher.TryFindAll( words, out var firstNames ) )
            {
                var wasCreateWord = false;
                foreach ( var sr in firstNames )
                {
                    var w1 = words[ sr.StartIndex ];
                    if ( CanProcess( w1 ) )
                    {
                        CreateNameWord_FirstName( words, w1, in sr );

                        wasCreateWord = true;
                    }
                }

                #region [.remove merged words.]
                if ( wasCreateWord )
                {
                    words.RemoveWhereValueOriginalIsNull();
                }
                #endregion
            }
        }
        public void Recognize_SurNamesOnly( List< word_t > words )
        {
            //#1 => (Sur-names)
            if ( _SurNamesSearcher.TryFindAll( words, out var surNames ) )
            {
                var wasCreateWord = false;
                foreach ( var sr in surNames )
                {
                    var w1 = words[ sr.StartIndex ];
                    if ( CanProcess( w1 ) )
                    {
                        CreateNameWord_SurName( words, w1, in sr );

                        wasCreateWord = true;
                    }
                }

                #region [.remove merged words.]
                if ( wasCreateWord )
                {
                    words.RemoveWhereValueOriginalIsNull();
                }
                #endregion
            }
        }

        [M(O.AggressiveInlining)] private static bool CanProcess_Use_BeforeCannotWords( List< word_t > words, int index )
        {
            if ( 0 < index )
            {
                return (!_BeforeCannotWords.Contains( words[ index - 1 ].valueOriginal ));
            }
            return (true);
        }
        [M(O.AggressiveInlining)] private static bool CanProcess( word_t w ) => ((w.valueUpper != null) && w.IsOutputTypeOther()); //!w.IsName());
        [M(O.AggressiveInlining)] private bool CreateNameWord( List< word_t > words, word_t w1, in SearchResult firstName_sr, in SearchResult surName_sr
            , TextPreambleTypeEnum tpt = TextPreambleTypeEnum.__UNDEFINED__ )
        {
            var (fn_original, fn_upper) = GetNameValueOriginalAndUpper( words, in firstName_sr );
            var (sn_original, sn_upper) = GetNameValueOriginalAndUpper( words, in surName_sr   );

            if ( _Model.IsExcludedName( fn_upper, sn_upper ) )
            {
                return (false);
            }
            if ( HasDotBetween_In_CurrentOriginalText( words, in firstName_sr, in surName_sr ) )
            {
                return (false);
            }
            //-------------------------------------------------//

            var nw = new NameWord( w1.startIndex ) { TextPreambleType = tpt, Firstname = fn_original, Surname = sn_original };

            var last = default(word_t);
            for ( int i = firstName_sr.StartIndex, j = firstName_sr.Length; 0 < j; j--, i++ )
            {
                var t = words[ i ];                
                _ValueUpperBuff   .Append( t.valueUpper    ).Append( SPACE );
                _ValueOriginalBuff.Append( t.valueOriginal ).Append( SPACE );
                t.ClearValuesAndNerChain();

                if ( (last == null) || (last.startIndex < t.startIndex) ) last = t;
            }
            for ( int i = surName_sr.StartIndex, j = surName_sr.Length; 0 < j; j--, i++ )
            {
                var t = words[ i ];
                _ValueUpperBuff   .Append( t.valueUpper    ).Append( SPACE );
                _ValueOriginalBuff.Append( t.valueOriginal ).Append( SPACE );
                t.ClearValuesAndNerChain();

                if ( (last == null) || (last.startIndex < t.startIndex) ) last = t;
            }

            nw.valueOriginal = _ValueOriginalBuff.ToString( 0, _ValueOriginalBuff.Length - 1 );
            nw.valueUpper    = _ValueUpperBuff   .ToString( 0, _ValueUpperBuff   .Length - 1 );
            nw.length        = (last.startIndex - w1.startIndex) + last.length;
            words[ firstName_sr.StartIndex ] = nw;

            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();

            return (true);
        }

        [M(O.AggressiveInlining)] private void CreateNameWord_FirstName( List< word_t > words, word_t w1, in SearchResult sr )
        {
            var nw = new NameWord( w1.startIndex );

            nw.Firstname = GetNameValueOriginal( words, in sr );

            var t = default(word_t);
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                t = words[ i ];
                _ValueUpperBuff   .Append( t.valueUpper    ).Append( SPACE );
                _ValueOriginalBuff.Append( t.valueOriginal ).Append( SPACE );
                t.ClearValuesAndNerChain();
            }

            nw.valueOriginal = _ValueOriginalBuff.ToString( 0, _ValueOriginalBuff.Length - 1 );
            nw.valueUpper    = _ValueUpperBuff   .ToString( 0, _ValueUpperBuff   .Length - 1 );
            nw.length        = (t.startIndex - w1.startIndex) + t.length;
            words[ sr.StartIndex ] = nw;

            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();
        }
        [M(O.AggressiveInlining)] private void CreateNameWord_SurName( List< word_t > words, word_t w1, in SearchResult sr
            , TextPreambleTypeEnum tpt = TextPreambleTypeEnum.__UNDEFINED__ )
        {
            var nw = new NameWord( w1.startIndex ) { TextPreambleType = tpt };

            nw.Surname = GetNameValueOriginal( words, in sr );

            var t = default(word_t);
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                t = words[ i ];
                _ValueUpperBuff   .Append( t.valueUpper    ).Append( SPACE );
                _ValueOriginalBuff.Append( t.valueOriginal ).Append( SPACE );
                t.ClearValuesAndNerChain();
            }

            nw.valueOriginal = _ValueOriginalBuff.ToString( 0, _ValueOriginalBuff.Length - 1 );
            nw.valueUpper    = _ValueUpperBuff   .ToString( 0, _ValueUpperBuff   .Length - 1 );
            nw.length        = (t.startIndex - w1.startIndex) + t.length;
            words[ sr.StartIndex ] = nw;

            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();
        }

        [M(O.AggressiveInlining)] private string GetNameValueOriginal( List< word_t > words, in SearchResult sr )
        {
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                var w = words[ i ];
                if ( _ValueOriginalBuff.IsLastCharIsLetter() && !w.IsExtraWordTypePunctuation() )
                {
                    _ValueOriginalBuff.Append( SPACE );
                }
                _ValueOriginalBuff.Append( w.valueOriginal ); //---.Append( SPACE );
            }
            var nameValue = _ValueOriginalBuff.ToString();
            _ValueOriginalBuff.Clear();
            return (nameValue);
        }
        [M(O.AggressiveInlining)] private (string original, string upper) GetNameValueOriginalAndUpper( List< word_t > words, in SearchResult sr )
        {
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                var w = words[ i ];
                if ( _ValueOriginalBuff.IsLastCharIsLetter() && !w.IsExtraWordTypePunctuation() )
                {
                    _ValueOriginalBuff.Append( SPACE );
                    _ValueUpperBuff   .Append( SPACE );
                }
                _ValueOriginalBuff.Append( w.valueOriginal ); //---.Append( SPACE );
                _ValueUpperBuff   .Append( w.valueUpper    );
            }
            var original = _ValueOriginalBuff.ToString();
            var upper    = _ValueUpperBuff   .ToString();
            _ValueOriginalBuff.Clear();
            _ValueUpperBuff   .Clear();
            return (original, upper);
        }

        [M(O.AggressiveInlining)] private bool HasDotBetween_In_CurrentOriginalText( List< word_t > words, in SearchResult firstName_sr, in SearchResult surName_sr )
        {
            if ( _CurrentOriginalText == null ) _CurrentOriginalText = _GetOriginalTextFunc();

            word_t w1;
            word_t w2;
            if ( firstName_sr.StartIndex < surName_sr.StartIndex )
            {
                w1 = words[ firstName_sr.EndIndex() - 1 ];
                w2 = words[ surName_sr.StartIndex ];
            }
            else
            {
                w1 = words[ surName_sr.EndIndex() - 1 ];
                w2 = words[ firstName_sr.StartIndex ];
            }
            
            for ( int i = w1.endIndex(), end = w2.startIndex; i < end; i++ )
            {
                if ( xlat.IsDot( _CurrentOriginalText[ i ] ) )
                {
                    return (true);
                }
            }
            return (false);
        }
    }
}
