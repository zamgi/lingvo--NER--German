namespace Lingvo.NER.NeuralNetwork.Tokenizing
{
    /// <summary>
    /// 
    /// </summary>
    public enum NerOutputType : byte
    {
        Other = 0,

        PERSON        = 1,
        ORGANIZATION  = 2,
        LOCATION      = 3,
        MISCELLANEOUS = 4,

        Email,
        Url
    }

    /// <summary>
    /// 
    /// </summary>
    public static partial class NerExtensions
    {
        public static string ToText( this NerOutputType nerOutputType )
        {
            switch ( nerOutputType )    
            {
                case NerOutputType.PERSON       : return "PERSON";
                case NerOutputType.ORGANIZATION : return "ORGANIZATION";
                case NerOutputType.LOCATION     : return "LOCATION";
                case NerOutputType.MISCELLANEOUS: return "MISCELLANEOUS";
                case NerOutputType.Email        : return "Email";
                case NerOutputType.Url          : return "Url";
                case NerOutputType.Other        : return "Other";
                default                         : return (nerOutputType.ToString());
            }
        }
    }
}
