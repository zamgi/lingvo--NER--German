using System.Text;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.NeuralNetwork.Tokenizing
{
    /// <summary>
    /// 
    /// </summary>
    public class word_t
    {
        public string valueOriginal;
        //---public string valueOriginal__UmlautesNormalized;
        public string valueUpper;
        public string valueUpper__UmlautesNormalized;
        public int    startIndex;
        public int    length;
        public ExtraWordType extraWordType;
//#if DEBUG
//        [M(O.AggressiveInlining)] public word_t() { }
//#endif
        [M(O.AggressiveInlining)] public int endIndex() => startIndex + length;

        #region [.ner.]
        public NerInputType   nerInputType;
        public NerOutputType  nerOutputType;
        public NNerOutputType nnerOutputType;
        public NNerBaseOutputType nnerBaseOutputType
        { 
            [M(O.AggressiveInlining)] get
            {
                switch ( nnerOutputType )
                {
                    case NNerOutputType.B_PER : case NNerOutputType.I_PER : return (NNerBaseOutputType.PER);
                    case NNerOutputType.B_ORG : case NNerOutputType.I_ORG : return (NNerBaseOutputType.ORG);
                    case NNerOutputType.B_LOC : case NNerOutputType.I_LOC : return (NNerBaseOutputType.LOC);
                    case NNerOutputType.B_MISC: case NNerOutputType.I_MISC: return (NNerBaseOutputType.MISC);
                    default: return (NNerBaseOutputType.Other);
                }                
            }
        }
        public NNerPrefixOutputType nnerPrefixOutputType
        { 
            [M(O.AggressiveInlining)] get
            {
                switch ( nnerOutputType )
                {
                    case NNerOutputType.B_PER: case NNerOutputType.B_ORG: case NNerOutputType.B_LOC: case NNerOutputType.B_MISC: return (NNerPrefixOutputType.B);
                    case NNerOutputType.I_PER: case NNerOutputType.I_ORG: case NNerOutputType.I_LOC: case NNerOutputType.I_MISC: return (NNerPrefixOutputType.I);
                    default: return (NNerPrefixOutputType.Other);
                }                
            }
        }

        //next ner-word in chain
        public word_t nerNext { [M(O.AggressiveInlining)] get; private set; }
        //previous ner-word in chain
        public word_t nerPrev { [M(O.AggressiveInlining)] get; private set; }

        [M(O.AggressiveInlining)] public void SetNextPrev( word_t next, NerOutputType nerOutputType )
        {
            nerNext = next;
            next.nerPrev = this;

            this.nerOutputType = next.nerOutputType = nerOutputType;
        }
        [M(O.AggressiveInlining)] public void ResetNextPrev()
        {
            if ( nerPrev != null )
            {
                nerPrev.nerNext = nerNext;
                if ( nerNext != null )
                {
                    nerNext.nerPrev = nerPrev;
                    nerNext = null;
                }
                nerPrev = null;
            }
            else if ( nerNext != null )
            {
                nerNext.nerPrev = null;
                nerNext = null;
            }
        }
        public bool IsFirstWordInNerChain { [M(O.AggressiveInlining)] get => (nerNext != null && nerPrev == null); }
        public bool IsWordInNerChain      { [M(O.AggressiveInlining)] get => (nerNext != null || nerPrev != null); }
        public bool HasNerPrevWord        { [M(O.AggressiveInlining)] get => (nerPrev != null); }

        //public string GetNerValue() => GetNerValue( new StringBuilder() );
        public string GetNerValue( StringBuilder sb )
        {
            if ( nerNext != null )
            {
                sb.Clear();
                for ( var w = this; w != null; w = w.nerNext )
                {
                    sb.Append( w.valueOriginal ).Append( ' ' );
                }
                return (sb.Remove( sb.Length - 1, 1 ).ToString());
            }
            return (valueOriginal);
        }
        public int    GetNerLength()
        {
            if ( nerNext != null )
            {
                for ( var w = this; ; w = w.nerNext )
                {
                    if ( w.nerNext == null )
                    {
                        var len = ((w.startIndex - this.startIndex) + w.length);
                        return (len);
                    }
                }
            }
            return (length);
        }
        public int    GetNerChainLength()
        {
            if ( nerNext != null )
            {
                var len = 1;
                for ( var w = this; ; w = w.nerNext )
                {
                    if ( w.nerNext == null )
                    {
                        return (len);
                    }
                    len++;
                }
            }
            return (1);
        }
        #endregion

        #region [.to-string's.]
        public override string ToString()
        {
            var t = (nerOutputType != NerOutputType.Other) ? nerOutputType.ToText() : nnerOutputType.ToText();
            return ($"{t},  '{valueUpper}' ({valueOriginal}), [{startIndex}:{length}]" +
                    (IsFirstWordInNerChain ? $",  chain-len: {GetNerChainLength()}" : null));
        }
        #endregion
    }
}