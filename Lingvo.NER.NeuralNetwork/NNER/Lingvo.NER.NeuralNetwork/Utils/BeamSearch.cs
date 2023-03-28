using System.Collections.Generic;
using System.Linq;

using Lingvo.NER.NeuralNetwork.Networks;
using Lingvo.NER.NeuralNetwork.Tools;
using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork
{
    /// <summary>
    /// 
    /// </summary>
    public class BeamSearchStatus
    {
        public List<int> OutputIds;
        public float Score;

        public List<WeightTensor> HTs;
        public List<WeightTensor> CTs;

        public BeamSearchStatus()
        {
            OutputIds = new List<int>();
            HTs = new List<WeightTensor>();
            CTs = new List<WeightTensor>();

            Score = 1.0f;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class BeamSearch
    {
        public static List<BeamSearchStatus> GetTopNBSS( List<BeamSearchStatus> bssList, int topN )
        {
            var q = new FixedSizePriorityQueue< ComparableItem< BeamSearchStatus > >( topN, ComparableItemComparer< BeamSearchStatus >.Desc );
            for ( int i = 0; i < bssList.Count; i++ )
            {
                q.Enqueue( new ComparableItem<BeamSearchStatus>( bssList[ i ].Score, bssList[ i ] ) );
            }
            return (q.Select( x => x.Value ).ToList( q.Count ));
        }
    }
}
