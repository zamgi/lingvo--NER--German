using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Applications;
using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork.WebService
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Config : Options
    {
        /// <summary>
        /// 
        /// </summary>
        public struct ModelInfo
        {
            public string ModelFilePath;
            //public bool   DelayLoad;
            public bool   LoadImmediate;
            public bool   DontReplaceNumsOnPlaceholders;
            public bool   UpperCase;
        }

        [Arg(nameof(CONCURRENT_FACTORY_INSTANCE_COUNT), nameof(CONCURRENT_FACTORY_INSTANCE_COUNT))] public int? CONCURRENT_FACTORY_INSTANCE_COUNT;
        [Arg(nameof(ModelInfos), nameof(ModelInfos))] public Dictionary< string, ModelInfo > ModelInfos;

        /// <summary>
        /// 
        /// </summary>
        public struct LogToFile
        {
            public bool   Enable;
            public string LogFileName;
        }
        [Arg(nameof(Log), nameof(Log))] public LogToFile Log;
    }
}