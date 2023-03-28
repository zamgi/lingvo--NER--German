using System.Collections.Generic;

using Lingvo.NER.NeuralNetwork.Applications;
using Lingvo.NER.NeuralNetwork.Networks;

namespace Lingvo.NER.NeuralNetwork
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Predictor
    {
        private SeqLabel _SL;
        private bool     _ModelUpperCase;
        public Predictor( string modelFilePath, bool modelUpperCase, int maxPredictSentLength, ProcessorTypeEnums processorType, string deviceIds = null/*[== "0"]*/ )
        {
            var opts = new Options()
            {
                ModelFilePath        = modelFilePath,
                ModelUpperCase       = modelUpperCase,
                MaxPredictSentLength = maxPredictSentLength,
                ProcessorType        = processorType,
                DeviceIds            = deviceIds,                
            };
            _SL = SeqLabel.Create4Predict( opts );
            _ModelUpperCase = modelUpperCase;
        }
        public Predictor( Options opts ) : this( opts, opts.ModelUpperCase ) { }
        public Predictor( Options opts, bool modelUpperCase )
        {
            _SL = SeqLabel.Create4Predict( opts );
            _ModelUpperCase = modelUpperCase;
        }
        public Predictor( SeqLabel sl, bool modelUpperCase )
        {
            _SL = sl;
            _ModelUpperCase = modelUpperCase;
        }

        public bool ModelUpperCase => _ModelUpperCase;

        public List< string > Predict( List< string > inputTokens, int? maxPredictSentLength = null, float cutDropout = 0.1f ) => _SL.Predict_Full( inputTokens, maxPredictSentLength, cutDropout ).labelTokens;
        public (List< string > labelTokens, NetworkResult.ClassesInfo classesInfos) Predict_2( List< string > inputTokens, int? maxPredictSentLength = null, float cutDropout = 0.1f ) => _SL.Predict_Full( inputTokens, maxPredictSentLength, cutDropout, returnWordClassInfos: true );
        public NetworkResult.ClassesInfo Predict_ClassesInfo( List< string > inputTokens, int? maxPredictSentLength = null, float cutDropout = 0.1f ) => _SL.Predict_Full( inputTokens, maxPredictSentLength, cutDropout, returnWordClassInfos: true ).classesInfos;
    }
}

