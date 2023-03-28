using System.Configuration;

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    public static class ConfigEx
    {
        public static int MAX_INPUTTEXT_LENGTH              => int.Parse( ConfigurationManager.AppSettings[ "MAX_INPUTTEXT_LENGTH" ] );
        public static int CONCURRENT_FACTORY_INSTANCE_COUNT => int.Parse( ConfigurationManager.AppSettings[ "CONCURRENT_FACTORY_INSTANCE_COUNT" ] );
    }
}