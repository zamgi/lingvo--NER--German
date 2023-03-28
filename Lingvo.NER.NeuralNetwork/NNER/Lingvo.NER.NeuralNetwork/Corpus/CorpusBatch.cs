using System;
using System.Collections.Generic;
using System.Linq;

using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork.Text
{
    /// <summary>
    /// 
    /// </summary>
    public class CorpusBatch
    {
        protected List<List<List<string>>> _SrcGroups; // shape (group_size, batch_size, seq_size)
        protected List<List<List<string>>> _TgtGroups;
        protected List<SntPair> _SntPairs;
        private int _SrcTokenCount;
        private int _TgtTokenCount;

        private CorpusBatch() { }
        public CorpusBatch( List< string > inputTokens )
        {
            _SrcGroups = new List< List< List< string > > > { new List< List< string > > { inputTokens } };
            _TgtGroups = new List<List<List<string>>> { InitializeHypTokens( string.Empty ) };
        }
        public CorpusBatch( List<SntPair> sntPairs ) => CreateBatch( sntPairs );

        private void CreateBatch( List< SntPair > sntPairs )
        {
            _SrcTokenCount = 0;
            _TgtTokenCount = 0;

            _SntPairs = sntPairs;
                        
            var srcGroupNum = sntPairs[ 0 ].SrcTokenGroups.Count;
            var src_capacity_2  = sntPairs.Count * srcGroupNum;
            _SrcGroups = new List<List<List<string>>>( srcGroupNum );            
            for ( var i = 0; i < srcGroupNum; i++ )
            {
                _SrcGroups.Add( new List<List<string>>( src_capacity_2 ) );
            }

            var tgtGroupNum = sntPairs[ 0 ].TgtTokenGroups.Count;
            var tgt_capacity_2  = sntPairs.Count * tgtGroupNum;
            _TgtGroups = new List<List<List<string>>>( tgtGroupNum );
            for ( var i = 0; i < tgtGroupNum; i++ )
            {
                _TgtGroups.Add( new List<List<string>>( tgt_capacity_2 ) );
            }
            
            for ( int i = 0, len = sntPairs.Count; i < len; i++ )
            {
                var sntPairs_i = sntPairs[ i ];

                if ( sntPairs_i.SrcTokenGroups.Count != srcGroupNum )
                {
                    throw (new DataMisalignedException( $"Source data '{i}' group size is mismatch. It's {sntPairs_i.SrcTokenGroups.Count}, but it should be {srcGroupNum}. Tokens: {sntPairs_i.PrintSrcTokens()}" ));
                }
                for ( var j = 0; j < srcGroupNum; j++ )
                {
                    var sntPairs_i__SrcTokenGroups_j = sntPairs_i.SrcTokenGroups[ j ];
                    _SrcGroups[ j ].Add( sntPairs_i__SrcTokenGroups_j );
                    _SrcTokenCount += sntPairs_i__SrcTokenGroups_j.Count;
                }

                if ( sntPairs_i.TgtTokenGroups.Count != tgtGroupNum )
                {
                    throw (new DataMisalignedException( $"Target data '{i}' group size is mismatch. It's {sntPairs_i.TgtTokenGroups.Count}, but it should be {tgtGroupNum}. Tokens: {sntPairs_i.PrintTgtTokens()}" ));
                }
                for ( var j = 0; j < tgtGroupNum; j++ )
                {
                    var sntPairs_i__TgtTokenGroups_j = sntPairs_i.TgtTokenGroups[ j ];
                    _TgtGroups[ j ].Add( sntPairs_i__TgtTokenGroups_j );
                    _TgtTokenCount += sntPairs_i__TgtTokenGroups_j.Count;
                }
            }
        }        

        public CorpusBatch CloneSrcTokens()
        {
            var spb = new CorpusBatch()
            {
                _SrcGroups = _SrcGroups,
                _TgtGroups = new List<List<List<string>>>()
            };
            spb._TgtGroups.Add( InitializeHypTokens( string.Empty ) );
            return (spb);
        }

        public IReadOnlyList< SntPair > SntPairs => _SntPairs;
        public int SrcTokenCount => _SrcTokenCount;
        public int TgtTokenCount => _TgtTokenCount;

        public int GetBatchSize() => _SrcGroups[ 0 ].Count;
        public int GetSrcGroupSize() => _SrcGroups.Count;


        public CorpusBatch GetRange( int idx, int count )
        {
            var batch = new CorpusBatch() { _SrcGroups = new List<List<List<string>>>( _SrcGroups.Count ) };
            for ( int i = 0; i < _SrcGroups.Count; i++ )
            {
                var lst = new List<List<string>>( count );                
                lst.AddRange( _SrcGroups[ i ].GetRange( idx, count ) );
                batch._SrcGroups.Add( lst );
            }

            if ( _TgtGroups != null )
            {
                batch._TgtGroups = new List<List<List<string>>>( _TgtGroups.Count );
                for ( int i = 0; i < _TgtGroups.Count; i++ )
                {
                    var lst = new List< List< string > >( count );
                    batch._TgtGroups.Add( lst );

                    var tgtTknsGroups_i = _TgtGroups[ i ];
                    if ( 0 < tgtTknsGroups_i.Count )
                    {
                        lst.AddRange( tgtTknsGroups_i.GetRange( idx, count ) );
                    }
                }
            }
            else
            {
                batch._TgtGroups = _TgtGroups;
            }
            return (batch);
        }

        public List<List<string>> GetSrcTokens( int group ) => _SrcGroups[ group ];
        public List<List<string>> GetTgtTokens( int group ) => _TgtGroups[ group ];
        public List<List<string>> InitializeHypTokens( string prefix )
        {
            var batchSize = this.GetBatchSize();
            var hypTkns = new List<List<string>>( batchSize );
            for ( int i = 0; i < batchSize; i++ )
            {
                if ( !prefix.IsNullOrEmpty() )
                {
                    hypTkns.Add( new List<string>() { prefix } );
                }
                else
                {
                    hypTkns.Add( new List<string>() );
                }
            }
            return (hypTkns);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class CorpusBatchBuilder
    {
        private List<Dictionary<string, int>> _Src_dicts;
        private List<Dictionary<string, int>> _Tgt_dicts;
        public CorpusBatchBuilder()
        {
            _Src_dicts = new List<Dictionary<string, int>>();
            _Tgt_dicts = new List<Dictionary<string, int>>();
        }

        public void ReduceSrcTokensToSingleGroup()
        {
            Logger.WriteLine( $"Reduce source vocabs group from '{_Src_dicts.Count}' to 1" );
            var rst = new Dictionary<string, int>( _Src_dicts.Sum( d => d.Count ) );

            foreach ( var dict in _Src_dicts )
            {
                foreach ( var p in dict )
                {
                    if ( rst.TryGetValue( p.Key, out var cnt ) )
                    {
                        rst[ p.Key ] = cnt + p.Value;
                    }
                    else
                    {
                        rst.Add( p.Key, p.Value );
                    }
                }
            }

            _Src_dicts.Clear();
            _Src_dicts.Add( rst );
        }

        /// <summary>
        /// Build vocabulary from training corpus
        /// </summary>
        /// side and the second group in target side are shared vocabulary
        public void CountSntPairTokens( IEnumerable< SntPair > sntPairs )
        {
            foreach ( var sntPair in sntPairs )
            {
                var src_len = sntPair.SrcTokenGroups.Count;
                while ( _Src_dicts.Count < src_len )
                {
                    _Src_dicts.Add( new Dictionary< string, int >() );
                }
                var tgt_len = sntPair.TgtTokenGroups.Count;
                while ( _Tgt_dicts.Count < tgt_len )
                {
                    _Tgt_dicts.Add( new Dictionary< string, int >() );
                }

                for ( var g = 0; g < src_len; g++ )
                {
                    var d      = _Src_dicts[ g ];
                    var tokens = sntPair.SrcTokenGroups[ g ];
                    for ( int i = 0, len = tokens.Count; i < len; i++ )
                    {
                        var token = tokens[ i ];
                        if ( d.TryGetValue( token, out var cnt ) )
                        {
                            d[ token ] = cnt + 1;
                        }
                        else
                        {
                            d.Add( token, 1 );
                        }
                    }
                }

                for ( var g = 0; g < tgt_len; g++ )
                {
                    var d      = _Tgt_dicts[ g ];
                    var tokens = sntPair.TgtTokenGroups[ g ];
                    for ( int i = 0, len = tokens.Count; i < len; i++ )
                    {
                        var token = tokens[ i ];                        
                        if ( d.TryGetValue( token, out var cnt ) )
                        {
                            d[ token ] = cnt + 1;
                        }
                        else
                        {
                            d.Add( token, 1 );
                        }
                    }
                }
            }
        }
        public void CountTargetTokens( IEnumerable< SntPair > sntPairs )
        {
            foreach ( var sntPair in sntPairs )
            {
                var tgt_len = sntPair.TgtTokenGroups.Count;
                while ( _Tgt_dicts.Count < tgt_len )
                {
                    _Tgt_dicts.Add( new Dictionary<string, int>() );
                }

                for ( var g = 0; g < tgt_len; g++ )
                {
                    var d      = _Tgt_dicts[ g ];
                    var tokens = sntPair.TgtTokenGroups[ g ];
                    for ( int i = 0; i < tokens.Count; i++ )
                    {
                        var token = tokens[ i ];
                        if ( d.TryGetValue( token, out var cnt ) )
                        {
                            d[ token ] = cnt + 1;
                        }
                        else
                        {
                            d.Add( token, 1 );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Build vocabulary from training corpus
        /// </summary>
        /// <param name="sharedSrcTgtVocabGroupMapping">The mappings for shared vocabularies between source side and target side. The values in the mappings are group ids. For example: sharedSrcTgtVocabGroupMapping[0] = 1 means the first group in source
        /// side and the second group in target side are shared vocabulary</param>
        public (List<Vocab>, List<Vocab>) GenerateVocabs( bool vocabIgnoreCase, int vocabSize = 45000 ) => GenerateVocabs( vocabIgnoreCase, vocabIgnoreCase, vocabSize, vocabSize );
        public (List<Vocab>, List<Vocab>) GenerateVocabs( bool srcVocabIgnoreCase, bool tgtVocabIgnoreCase, int srcVocabSize = 45000, int tgtVocabSize = 45000 )
        {
            Logger.WriteLine( $"Building vocabulary from corpus." );

            List<Vocab> srcVocabs = InnerBuildVocab( srcVocabIgnoreCase, srcVocabSize, _Src_dicts, "Source" );
            List<Vocab> tgtVocabs = InnerBuildVocab( tgtVocabIgnoreCase, tgtVocabSize, _Tgt_dicts, "Target" );

            return (srcVocabs, tgtVocabs);
        }
        public List<Vocab> GenerateTargetVocab( bool vocabIgnoreCase, int vocabSize = 45000 )
        {
            Logger.WriteLine( $"Building target vocabulary from corpus." );

            List<Vocab> tgtVocabs = InnerBuildVocab( vocabIgnoreCase, vocabSize, _Tgt_dicts, "Target" );

            return (tgtVocabs);
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class Int32_Desc_Comparer : IComparer< int >
        {
            public int Compare( int x, int y ) => y.CompareTo( x );
        }
        private static List< Vocab > InnerBuildVocab( bool ignoreCase, int vocabSize, List< Dictionary< string, int > > ds, string tag )
        {
            var vocabs   = new List< Vocab >( ds.Count );
            var comparer = new Int32_Desc_Comparer();

            for ( int i = 0; i < ds.Count; i++ )
            {
                var sd = new SortedDictionary< int, List< string > >( comparer );

                var s_d = ds[ i ];
                foreach ( var p in s_d )
                {
                    if ( !sd.TryGetValue( p.Value, out var lst ) )
                    {
                        lst = new List< string >();
                        sd.Add( p.Value, lst );
                    }
                    lst.Add( p.Key );
                }

                var v = Vocab.CreateDicts( ignoreCase );
                var wordToIndex = v.wordToIndex;
                var indexToWord = v.indexToWord;
                var q = Vocab.START_MEANING_INDEX;
                foreach ( var p in sd )
                {
                    foreach ( var token in p.Value )
                    {
                        if ( !BuildInTokens.IsPreDefinedToken( token ) )
                        {
                            // add word to vocab
                            wordToIndex[ token ] = q;
                            indexToWord[ q     ] = token;
                            q++;

                            if ( vocabSize <= q )
                            {
                                break;
                            }
                        }
                    }

                    if ( vocabSize <= q )
                    {
                        break;
                    }
                }

                vocabs.Add( new Vocab( v ) );
                Logger.WriteLine( $"{tag} Vocab Group '{i}': Original vocabulary size = '{s_d.Count}', Truncated vocabulary size = '{q}'" );
            }
            return (vocabs);
        }
    }
}
