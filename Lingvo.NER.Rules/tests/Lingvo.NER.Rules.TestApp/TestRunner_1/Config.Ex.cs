using System.Configuration;
using System.IO;

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    public static partial class ConfigEx
    {
        public static string TEST_INPUT_FILENAME_1  => Path.GetFullPath( ConfigurationManager.AppSettings[ "TEST_INPUT_FILENAME_1" ] );
        public static string OUTPUT_HTML_FILENAME_1 => Path.GetFullPath( ConfigurationManager.AppSettings[ "OUTPUT_HTML_FILENAME_1" ] ); 
    }
}