using System.Collections.Generic;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Birthplaces
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class BirthplacesRecognizer
    {
        const int MAX_DISTANCE_FROM_PREAMBLE = 3;

        #region [.ctor().]
        private const char SPACE = ' ';
        private IWordsChainDictionary _TextPreambles;
        private BirthplacesSearcher   _BirthplacesSearcher;
        private StringBuilder _ValueUpperBuff;
        private StringBuilder _ValueOriginalBuff;

        public BirthplacesRecognizer( IBirthplacesModel model )
        {
            _TextPreambles       = model.TextPreambles;
            _BirthplacesSearcher = new BirthplacesSearcher( model.Birthplaces );

            _ValueUpperBuff    = new StringBuilder( 100 );
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
                if ( !_BirthplacesSearcher.TryFindFirst2Rigth( words, startIndex, MAX_DISTANCE_FROM_PREAMBLE, out var sr ) )
                {
                    continue;
                }

                var startIndex2 = sr.StartIndex + sr.Length;

                CreateBirthplaceWord( words, in sr );
                index = startIndex2 - 1;
                has   = true;
            }

            #region [.remove merged words.]
            if ( has )
            {
                words.RemoveWhereValueOriginalIsNull();
            }
            #endregion
        }
        #endregion

        [M(O.AggressiveInlining)] private void CreateBirthplaceWord( List< word_t > words, in SearchResult sr )
        {
            var w1 = words[ sr.StartIndex ];

            var birthplace = CreateBirthplaceValue( words, in sr );
            var nw = new BirthplaceWord( w1.startIndex, birthplace );

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
        [M(O.AggressiveInlining)] private string CreateBirthplaceValue( List< word_t > words, in SearchResult sr )
        {
            for ( int i = sr.StartIndex, j = sr.Length; 0 < j; j--, i++ )
            {
                var w = words[ i ];

                if ( _ValueOriginalBuff.IsLastCharIsLetter() && (!w.IsExtraWordTypePunctuation() || w.valueOriginal[ 0 ] == '(') )
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
