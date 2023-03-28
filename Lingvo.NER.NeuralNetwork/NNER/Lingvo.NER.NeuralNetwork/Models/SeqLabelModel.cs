using System.Linq;

using Lingvo.NER.NeuralNetwork.Models;
using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork
{
    /// <summary>
    /// 
    /// </summary>
    public class SeqLabelModel : Model
    {
        public SeqLabelModel() { }
        public SeqLabelModel( int hiddenDim, int embeddingDim, int encoderLayerDepth, int multiHeadNum, EncoderTypeEnums encoderType, Vocab srcVocab, Vocab clsVocab )
            : base( hiddenDim, encoderLayerDepth, encoderType, embeddingDim, multiHeadNum, srcVocab, applyContextEmbeddingsToEntireSequence: false )
        {
            ClsVocab = clsVocab;
        }
        public SeqLabelModel( Model_4_ProtoBufSerializer m )
            : base( m.HiddenDim, m.EncoderLayerDepth, m.EncoderType, m.EncoderEmbeddingDim, m.MultiHeadNum, m.SrcVocab?.ToVocab(), applyContextEmbeddingsToEntireSequence: false )
        {
            ClsVocabs         = m.ClsVocabs?.Select( v => v.ToVocab() ).ToList();
            Name2Weights      = m.Name2Weights;
            BestPrimaryScore  = (m.BestPrimaryScores.AnyEx() ? m.BestPrimaryScores.Values.First() : default);
        }
        public static SeqLabelModel Create( Model_4_ProtoBufSerializer m ) => new SeqLabelModel( m );
    }
}
