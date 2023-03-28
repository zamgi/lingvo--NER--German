using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.NeuralNetwork.Tokenizing
{
    /// <summary>
    /// 
    /// </summary>
    public enum NNerOutputType : byte
    {
        Other = 0,

        B_PER,
        I_PER,
        B_ORG,
        I_ORG,
        B_LOC,
        I_LOC,
        B_MISC,
        I_MISC,
    }
    /// <summary>
    /// 
    /// </summary>
    public enum NNerBaseOutputType : byte
    {
        Other = 0,

        PER,
        ORG,
        LOC,
        MISC,
    }
    /// <summary>
    /// 
    /// </summary>
    public enum NNerPrefixOutputType : byte
    {
        Other = 0,

        B,
        I,
    }

    /// <summary>
    /// 
    /// </summary>
    public static partial class NNerExtensions
    {
        public static string ToText( this NNerOutputType nnerOutputType )
        {
            switch ( nnerOutputType )    
            {
                case NNerOutputType.B_PER  : return "B-PER";
                case NNerOutputType.I_PER  : return "I-PER";
                case NNerOutputType.B_ORG  : return "B-ORG";
                case NNerOutputType.I_ORG  : return "I-ORG";
                case NNerOutputType.B_LOC  : return "B-LOC";
                case NNerOutputType.I_LOC  : return "I-LOC";
                case NNerOutputType.B_MISC : return "B-MISC";
                case NNerOutputType.I_MISC : return "I-MISC";
                case NNerOutputType.Other  : return "O";
                default                    : return (nnerOutputType.ToString());
            }
        }
        [M(O.AggressiveInlining)] public static NNerOutputType ToNNerOutputType( this string nnerOutputType )
        {
            if ( nnerOutputType != null )
            {
                switch ( nnerOutputType )    
                {
                    case "B-PER" : return NNerOutputType.B_PER;
                    case "I-PER" : return NNerOutputType.I_PER;
                    case "B-ORG" : return NNerOutputType.B_ORG;
                    case "I-ORG" : return NNerOutputType.I_ORG;
                    case "B-LOC" : return NNerOutputType.B_LOC;
                    case "I-LOC" : return NNerOutputType.I_LOC;
                    case "B-MISC": return NNerOutputType.B_MISC;
                    case "I-MISC": return NNerOutputType.I_MISC;
                    case "O"     : return NNerOutputType.Other;
                }
            }
            return (NNerOutputType.Other);
        }
        [M(O.AggressiveInlining)] public static NerOutputType ToNerOutputType( this NNerOutputType nerOutputType )
        {
            switch ( nerOutputType )    
            {
                case NNerOutputType.B_PER : case NNerOutputType.I_PER : return NerOutputType.PERSON;
                case NNerOutputType.B_ORG : case NNerOutputType.I_ORG : return NerOutputType.ORGANIZATION;
                case NNerOutputType.B_LOC : case NNerOutputType.I_LOC : return NerOutputType.LOCATION;
                case NNerOutputType.B_MISC: case NNerOutputType.I_MISC: return NerOutputType.MISCELLANEOUS;
                case NNerOutputType.Other : default: return NerOutputType.Other;
            }
        }
    }
}
