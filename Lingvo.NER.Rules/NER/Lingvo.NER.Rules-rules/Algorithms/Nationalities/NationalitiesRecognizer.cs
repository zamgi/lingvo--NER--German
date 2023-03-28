using System.Collections.Generic;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Nationalities
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class NationalitiesRecognizer
    {
        const int MAX_DISTANCE_FROM_PREAMBLE = 3;

        #region [.ctor().]
        private const char SPACE = ' ';
        private INationalitiesModel _Model;
        private IWordsChainDictionary _TextPreambles;
        private NationalitiesSearcher _NationalitiesSearcher;
        private StringBuilder _ValueUpperBuff;
        private StringBuilder _ValueOriginalBuff;

        public NationalitiesRecognizer( INationalitiesModel model )
        {
            _Model = model;

            _TextPreambles = _Model.TextPreambles;

            _NationalitiesSearcher = new NationalitiesSearcher( _Model.Nationalities );

            _ValueUpperBuff = new StringBuilder( 100 );
            _ValueOriginalBuff = new StringBuilder( 100 );
        }
        #endregion

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

                if ( !_TextPreambles.TryGetFirst( words, index, out var length ) )
                {
                    continue;
                }

                //var preambleWordIndex = index;
                var startIndex = index + length;
                if ( !_NationalitiesSearcher.TryFindFirst2Rigth( words, startIndex, MAX_DISTANCE_FROM_PREAMBLE, out var sr ) )
                {
                    continue;
                }

                var startIndex2 = sr.StartIndex + sr.Length;

                CreateNationalityWord( words, in sr );
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

        [M(O.AggressiveInlining)] private void CreateNationalityWord( List< word_t > words, in SearchResult sr )
        {

            var w1 = words[ sr.StartIndex ];

            var nw = new NationalityWord( w1.startIndex, GetNationalityValue( words, in sr ) );

            var t = default(word_t);
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                t = words[ i ];
                _ValueUpperBuff.Append( t.valueUpper ).Append( SPACE );
                _ValueOriginalBuff.Append( t.valueOriginal ).Append( SPACE );
                t.ClearValuesAndNerChain();
            }

            nw.valueOriginal = _ValueOriginalBuff.ToString( 0, _ValueOriginalBuff.Length - 1 );
            nw.valueUpper    = _ValueUpperBuff.ToString( 0, _ValueUpperBuff.Length - 1 );
            nw.length        = (t.startIndex - w1.startIndex) + t.length;
            words[ sr.StartIndex ] = nw;

            _ValueUpperBuff   .Clear();
            _ValueOriginalBuff.Clear();
        }

        [M(O.AggressiveInlining)] private string GetNationalityValue( List< word_t > words, in SearchResult sr )
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
    }
}
