using Lingvo.NER.NeuralNetwork.Text;
using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork.Applications
{
    /// <summary>
    /// 
    /// </summary>
    public class Options
    {
        #region [.1.]
        [Arg("The embedding dim", "EmbeddingDim")] public int EmbeddingDim = 128;

        [Arg("Maxmium sentence length in valid and predict/test set", "MaxPredictSentLength")] public int MaxPredictSentLength = 110;
        [Arg("Maxmium sentence length in training corpus"           , "MaxTrainSentLength")] public int MaxTrainSentLength = 110;

        [Arg(nameof(SentSplitterResourcesXmlFilename), nameof(SentSplitterResourcesXmlFilename))] public string SentSplitterResourcesXmlFilename;
        [Arg(nameof(UrlDetectorResourcesXmlFilename), nameof(UrlDetectorResourcesXmlFilename))]   public string UrlDetectorResourcesXmlFilename;

        /// <summary>
        /// 
        /// </summary>
        public struct ExternalValidator_t
        {
            public string FileName;
            public string Arguments;
            public string WorkingDirectory;
        }
        [Arg(nameof(ExternalValidator_t), nameof(ExternalValidator_t))] public ExternalValidator_t ExternalValidator;
        #endregion
        //-------------------------------------------------------------------------//

        #region [.2.]
        [Arg(nameof(CurrentDirectory), nameof(CurrentDirectory))] public string CurrentDirectory;

        [Arg("The file path of config file for parameters", nameof(ConfigFilePath))] public string ConfigFilePath;

        [Arg("The input file for test.", nameof(InputTestFile))] public string InputTestFile;
        [Arg("The test result file.", nameof(OutputFile))] public string OutputFile;

        [Arg("Training corpus folder path", nameof(TrainCorpusPath))] public string TrainCorpusPath;
        [Arg("Valid corpus folder path", nameof(ValidCorpusPath))] public string ValidCorpusPath;
        [Arg(nameof(TryRunValidateParallel), nameof(TryRunValidateParallel))] public bool TryRunValidateParallel;
        [Arg(nameof(ValidationOutputFileName), nameof(ValidationOutputFileName))] public string ValidationOutputFileName;

        [Arg("The batch size", nameof(BatchSize))] public int BatchSize = 1;

        [Arg("The interval hours to run model validation", nameof(ValidIntervalHours))] public float ValidIntervalHours = 1.0f;

        [Arg("The size of vocabulary in source side", nameof(SrcVocabSize))] public int SrcVocabSize = 45000;

        //[Arg("The size of vocabulary in target side", nameof(TgtVocabSize))]
        //public int TgtVocabSize = 45000;

        [Arg("Maxmium epoch number during training. Default is 100", nameof(MaxEpochNum))]
        public int MaxEpochNum = 100;

        [Arg("The trained model file path.", nameof(ModelFilePath))]
        public string ModelFilePath = "model.s2s";
        [Arg(nameof(ModelUpperCase), nameof(ModelUpperCase))] public bool ModelUpperCase;

        [Arg("The vocabulary file path for source side.", nameof(SrcVocab))] public string SrcVocab;
        [Arg("The vocabulary file path for target side.", nameof(TgtVocab))] public string TgtVocab;
        #endregion
        //-------------------------------------------------------------------------//

        #region [.3.]
        [Arg("Processor type: GPU, CPU", nameof(ProcessorType))]
        public ProcessorTypeEnums ProcessorType = ProcessorTypeEnums.GPU;

        [Arg("The options for CUDA NVRTC compiler. Options are split by space. For example: \"--use_fast_math --gpu-architecture=compute_60\"", nameof(CompilerOptions))]
        public string CompilerOptions = "--use_fast_math";

        [Arg("Device ids for training in GPU mode. Default is 0. For multi devices, ids are split by comma, for example: 0,1,2", nameof(DeviceIds))]
        public string DeviceIds = "0";


        [Arg("It indicates if the encoder is trainable", nameof(IsEncoderTrainable))]
        public bool IsEncoderTrainable = true;

        [Arg("Encoder type: LSTM, BiLSTM, Transformer", nameof(EncoderType))]
        public EncoderTypeEnums EncoderType = EncoderTypeEnums.Transformer;

        [Arg("The network depth in encoder.", nameof(EncoderLayerDepth))]
        public int EncoderLayerDepth = 1;

        [Arg("The hidden layer size of encoder and decoder.", nameof(HiddenSize))]
        public int HiddenSize = 128;

        [Arg( "The number of multi-heads in transformer model", nameof( MultiHeadNum ) )]
        public int MultiHeadNum = 8;


        [Arg("The weights optimizer during training. It supports Adam and RMSProp. Adam is default", nameof(Optimizer))]
        public string Optimizer = "Adam";

        [Arg("The beta1 for optimizer", nameof(Beta1))] public float Beta1 = 0.9f;
        [Arg("The beta2 for optimizer", nameof(Beta2))] public float Beta2 = 0.98f;
        [Arg("Clip gradients", nameof(GradClip))] public float GradClip = 3.0f;


        [Arg("Dropout ratio", nameof(DropoutRatio))]
        public float DropoutRatio = 0.0f;

        [Arg("Starting Learning rate factor for encoders", nameof(EncoderStartLearningRateFactor))]
        public float EncoderStartLearningRateFactor = 1.0f;

        [Arg("The ratio of memory usage", nameof(MemoryUsageRatio))]
        public float MemoryUsageRatio = 0.95f;

        [Arg("Starting Learning rate", nameof(StartLearningRate))]
        public float StartLearningRate = 0.0006f;

        [Arg("The shuffle block size", nameof(ShuffleBlockSize))]
        public int ShuffleBlockSize = -1;

        [Arg("Shuffle Type. It could be NoPaddingInSrc, NoPaddingInTgt and Random", nameof(ShuffleType))]
        public ShuffleEnums ShuffleType = ShuffleEnums.Random;

        [Arg("How to deal with too long sequence. It can be Ignore or Truncation", nameof(TooLongSequence))]
        public TooLongSequence TooLongSequence = TooLongSequence.Ignore;

        [Arg("Update parameters every N batches. Default is 1", nameof(UpdateFreq))]
        public int UpdateFreq = 1;

        [Arg("The number of steps for warming up", nameof(WarmUpSteps))]
        public int WarmUpSteps = 8000;

        [Arg("The number of updates for weights", nameof(WeightsUpdateCount))]
        public int WeightsUpdateCount;
        #endregion
    }
}
