using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    public enum BuildModelNerInputType : byte
    {
        __UNDEFINED__ = 0,

        Other,
        B_NAME, I_NAME,
        B_ORG,  I_ORG,
        B_GEO,  I_GEO,
        B_ENTR, I_ENTR,
        B_PROD, I_PROD,

        __UNKNOWN__
    }

    /// <summary>
    /// 
    /// </summary>
    public struct buildmodel_word_t
    {
        public word_t                 word;
        public BuildModelNerInputType buildModelNerInputType;
#if DEBUG
        public override string ToString() => ('\'' + word.valueOriginal + "'  [" + word.startIndex + ":" + word.length + "]  " +
                                              '\'' + word.nerInputType.ToString() + "'  " +
                                              '\'' + ((buildModelNerInputType == BuildModelNerInputType.Other) ? "-" : buildModelNerInputType.ToString()) + '\''
                                             );
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public static partial class NerExtensions
    {
        public static string ToText( this BuildModelNerInputType buildModelNerInputType )
        {
            switch ( buildModelNerInputType )
            {
                case BuildModelNerInputType.B_NAME: return ("B-N");
                case BuildModelNerInputType.I_NAME: return ("I-N");

                case BuildModelNerInputType.B_ORG:  return ("B-J");  
                case BuildModelNerInputType.I_ORG:  return ("I-J");

                case BuildModelNerInputType.B_GEO:  return ("B-G");  
                case BuildModelNerInputType.I_GEO:  return ("I-G");

                case BuildModelNerInputType.B_ENTR: return ("B-E");
                case BuildModelNerInputType.I_ENTR: return ("I-E");

                case BuildModelNerInputType.B_PROD: return ("B-P");
                case BuildModelNerInputType.I_PROD: return ("I-P");

                default: //BuildModelNerInputType.O: 
                                                    return ("O");
            }
        }
        public static BuildModelNerInputType ToBuildModelNerInputTypeB( this NerOutputType nerOutputType )
        {
            switch ( nerOutputType )
            {
                case NerOutputType.NAME__Crf: return (BuildModelNerInputType.B_NAME);
                case NerOutputType.ORG__Crf:  return (BuildModelNerInputType.B_ORG);
                case NerOutputType.GEO__Crf:  return (BuildModelNerInputType.B_GEO);
                case NerOutputType.ENTR__Crf: return (BuildModelNerInputType.B_ENTR);
                case NerOutputType.PROD__Crf: return (BuildModelNerInputType.B_PROD);
                default: //case NerOutputType.O: 
                                         return (BuildModelNerInputType.Other);
            }
        }
        public static BuildModelNerInputType ToBuildModelNerInputTypeI( this NerOutputType nerOutputType )
        {
            switch ( nerOutputType )
            {
                case NerOutputType.NAME__Crf: return (BuildModelNerInputType.I_NAME);
                case NerOutputType.ORG__Crf:  return (BuildModelNerInputType.I_ORG);
                case NerOutputType.GEO__Crf:  return (BuildModelNerInputType.I_GEO);
                case NerOutputType.ENTR__Crf: return (BuildModelNerInputType.I_ENTR);
                case NerOutputType.PROD__Crf: return (BuildModelNerInputType.I_PROD);
                default: //case NerOutputType.O: 
                                         return (BuildModelNerInputType.Other);
            }
        }        
    }
}
