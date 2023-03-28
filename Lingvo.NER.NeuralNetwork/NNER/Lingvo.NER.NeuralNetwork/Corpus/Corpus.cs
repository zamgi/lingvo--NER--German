using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork.Text
{
    /// <summary>
    /// 
    /// </summary>
    public enum TooLongSequence
    {
        Ignore,
        Truncation
    }

    /// <summary>
    /// 
    /// </summary>
    public class Corpus : IDisposable
    {
        private readonly Random   _Rnd             = new Random( DateTime.Now.Millisecond );
        private TooLongSequence   _TooLongSequence = TooLongSequence.Ignore;
        private bool              _ShowTokenDist   = true;
        private CancellationToken _Ct;

        protected int _MaxSrcSentLength = 110;
        protected int _MaxTgtSentLength = 110;
        protected int _BlockSize        = 1_000_000;
        protected int _BatchSize        = 1;
        protected List< string > _SrcFileList;
        protected List< string > _TgtFileList;
        protected ShuffleEnums   _ShuffleEnums;

        public Corpus( string corpusFilePath, int batchSize
                     , int               shuffleBlockSize  = -1
                     , int               maxSentLength     = 128
                     , ShuffleEnums      shuffleEnums      = ShuffleEnums.Random
                     , TooLongSequence   tooLongSequence   = TooLongSequence.Ignore
                     , CancellationToken cancellationToken = default )
        {
            Logger.WriteLine( $"Loading sequence labeling corpus from '{corpusFilePath}' MaxSentLength = '{maxSentLength}'" );
            _TooLongSequence  = tooLongSequence;
            _BatchSize        = batchSize;
            _BlockSize        = shuffleBlockSize;
            _MaxSrcSentLength = maxSentLength;
            _MaxTgtSentLength = maxSentLength;
            _ShuffleEnums     = shuffleEnums;
            _Ct               = cancellationToken;

            _SrcFileList = new List< string >();
            _TgtFileList = new List< string >();

            var (srcFilePath, tgtFilePath) = ConvertSequenceLabelingFormatToParallel( corpusFilePath );

            _SrcFileList.Add( srcFilePath );
            _TgtFileList.Add( tgtFilePath );
        }
        public void Dispose()
        {
            foreach ( var fn in _SrcFileList )
            {
                File_Delete_NoThrow( fn );
            }
            _SrcFileList.Clear();

            foreach ( var fn in _TgtFileList )
            {
                File_Delete_NoThrow( fn );
            }
            _TgtFileList.Clear();
        }

        public int BatchSize => _BatchSize;

        /// <summary>
        /// Build vocabulary from training corpus
        /// </summary>
        public (Vocab srcVocab, Vocab tgtVocab) BuildVocabs( bool vocabIgnoreCase, int vocabSize = 45000 )
        {
            var cbb = new CorpusBatchBuilder();
            foreach ( var sntPairBatch in this.GetSntPairBatchs() )
            {
                cbb.CountSntPairTokens( sntPairBatch.SntPairs );
            }
            cbb.ReduceSrcTokensToSingleGroup();

            (List<Vocab> srcVocabs, List<Vocab> tgtVocabs) = cbb.GenerateVocabs( vocabIgnoreCase, vocabSize );
            return (srcVocabs[ 0 ], tgtVocabs[ 0 ]);
        }
        public Vocab BuildTargetVocab( bool vocabIgnoreCase, int vocabSize = 45000 )
        {
            var cbb = new CorpusBatchBuilder();
            foreach ( var sntPairBatch in this.GetSntPairBatchs() )
            {
                cbb.CountTargetTokens( sntPairBatch.SntPairs );
            }

            var tgtVocabs = cbb.GenerateTargetVocab( vocabIgnoreCase, vocabSize );
            return (tgtVocabs[ 0 ]);
        }

        private void Shuffle( List< RawSntPair > rawSntPairs )
        {
            Logger.WriteLine( $"Starting shuffle {rawSntPairs.Count} sentence pairs." );

            if ( _ShuffleEnums == ShuffleEnums.Random )
            {
                for ( int i = 0; i < rawSntPairs.Count; i++ )
                {
                    int idx = _Rnd.Next( 0, rawSntPairs.Count );
                    RawSntPair t = rawSntPairs[ i ];
                    rawSntPairs[ i ] = rawSntPairs[ idx ];
                    rawSntPairs[ idx ] = t;
                }

                return;
            }

            //Put sentence pair with same source length into the bucket
            var dict = new Dictionary<long, List<RawSntPair>>(); //<source sentence length, sentence pair set>
            foreach ( RawSntPair p in rawSntPairs )
            {
                long length = 0;

                if ( _ShuffleEnums == ShuffleEnums.NoPaddingInSrc )
                {
                    length = p.SrcGroupLenId;
                }
                else if ( _ShuffleEnums == ShuffleEnums.NoPadding )
                {
                    length = p.GroupLenId;
                }
                else
                {
                    length = p.TgtGroupLenId;
                }

                if ( !dict.ContainsKey( length ) )
                {
                    dict.Add( length, new List<RawSntPair>() );
                }

                dict[ length ].Add( p );
            }

            //Randomized the order of sentence pairs with same length in source side
            Parallel.ForEach( dict, pair =>
            {
                var rnd = new Random( DateTime.Now.Millisecond + (int) pair.Key );

                List<RawSntPair> sntPairList = pair.Value;
                for ( int i = 0; i < sntPairList.Count; i++ )
                {
                    int idx = rnd.Next( 0, sntPairList.Count );
                    RawSntPair t = sntPairList[ i ];
                    sntPairList[ i ] = sntPairList[ idx ];
                    sntPairList[ idx ] = t;
                }
            });

            //Split large bucket to smaller buckets
            var dictSB = new Dictionary<long, List<RawSntPair>>();

            foreach ( var p in dict )
            {
                if ( p.Value.Count <= _BatchSize )
                {
                    if ( dictSB.TryGetValue( p.Key, out var lst ) )
                    {
                        lst.AddRange( p.Value );                        
                    }
                    else
                    {
                        dictSB.Add( p.Key, p.Value );
                    }
                }
                else
                {
                    int N = p.Value.Count / _BatchSize;
                    for ( int i = 0; i < N; i++ )
                    {
                        var pairs = p.Value.GetRange( i * _BatchSize, _BatchSize );
                        dictSB.Add( p.Key + 100000000000000 * _MaxSrcSentLength * i, pairs );
                    }

                    if ( p.Value.Count % _BatchSize != 0 )
                    {
                        dictSB.Add( p.Key + 100000000000000 * _MaxSrcSentLength * N, p.Value.GetRange( _BatchSize * N, p.Value.Count % _BatchSize ) );
                    }
                }
            }

            rawSntPairs.Clear();

            long[] keys = dictSB.Keys.ToArray();
            for ( int i = 0; i < keys.Length; i++ )
            {
                int idx = _Rnd.Next( 0, keys.Length );
                long t = keys[ i ];
                keys[ i ] = keys[ idx ];
                keys[ idx ] = t;
            }

            foreach ( long key in keys )
            {
                rawSntPairs.AddRange( dictSB[ key ] );
            }
        }
        private (string srcShuffledFilePath, string tgtShuffledFilePath) ShuffleAll()
        {
            var locker           = new object();
            var lockerForShuffle = new object();

            var dictSrcLenDist = new SortedDictionary< int, int >();
            var dictTgtLenDist = new SortedDictionary< int, int >();

            var tmp_dir = GetTempWorkDirPath();
            var srcShuffledFilePath = Path.Combine( tmp_dir, Path.GetRandomFileName() + ".tmp" );
            var tgtShuffledFilePath = Path.Combine( tmp_dir, Path.GetRandomFileName() + ".tmp" );
            if ( !Directory.Exists( tmp_dir ) ) Directory.CreateDirectory( tmp_dir );

            Logger.WriteLine( $"Loading and shuffling corpus from '{_SrcFileList.Count}' files." );

            var corpusSize       = 0;
            var tooLongSrcSntCnt = 0;
            var tooLongTgtSntCnt = 0;
            var totalSntCnt      = 0;

            using ( var src_sw = new StreamWriter( srcShuffledFilePath, append: false ) )
            using ( var tgt_sw = new StreamWriter( tgtShuffledFilePath, append: false ) )
            {
                Parallel.For( 0, _SrcFileList.Count, i =>
                //for (int i = 0; i < _SrcFileList.Count; i++)
                {
                    if ( _ShowTokenDist )
                    {
                        Logger.WriteLine( $"Process file '{_SrcFileList[ i ]}' and '{_TgtFileList[ i ]}'" );
                    }

                    var sntPairs = new List< RawSntPair >();

                    using ( var src_sr = new StreamReader( _SrcFileList[ i ] ) )
                    using ( var tgt_sr = new StreamReader( _TgtFileList[ i ] ) )
                    {
                        while ( !src_sr.EndOfStream || !tgt_sr.EndOfStream )
                        {
                            var rawSntPair = new RawSntPair( src_sr.ReadLine(), tgt_sr.ReadLine(), _MaxSrcSentLength, _MaxTgtSentLength, (_TooLongSequence == TooLongSequence.Truncation) );
                            if ( rawSntPair.IsEmptyPair() )
                            {
                                continue; //---break;
                            }

                            if ( _ShowTokenDist )
                            {
                                lock ( locker )
                                {
                                    var si = rawSntPair.SrcLength / 100;
                                    if ( !dictSrcLenDist.TryGetValue( si, out var cnt ) )
                                    {
                                        dictSrcLenDist.Add( si, 1 );
                                    }
                                    else
                                    {
                                        dictSrcLenDist[ si ] = cnt + 1;
                                    }                                    

                                    var ti = rawSntPair.TgtLength / 100;
                                    if ( !dictTgtLenDist.TryGetValue( ti, out cnt ) )
                                    {
                                        dictTgtLenDist.Add( ti, 1 );
                                    }
                                    else
                                    {
                                        dictTgtLenDist[ ti ] = cnt + 1;
                                    }                                    
                                }
                            }

                            var isTooLongSent = false;
                            if ( _MaxSrcSentLength < rawSntPair.SrcLength )
                            {
                                Interlocked.Increment( ref tooLongSrcSntCnt );
                                isTooLongSent = true;
                            }
                            if ( _MaxTgtSentLength < rawSntPair.TgtLength )
                            {
                                Interlocked.Increment( ref tooLongTgtSntCnt );
                                isTooLongSent = true;
                            }
                            Interlocked.Increment( ref totalSntCnt ); 

                            if ( isTooLongSent )
                            {
                                continue;
                            }

                            sntPairs.Add( rawSntPair );
                            Interlocked.Increment( ref corpusSize );

                            if ( (0 < _BlockSize) && (_BlockSize <= sntPairs.Count) )
                            {
                                Shuffle( sntPairs );
                                lock ( lockerForShuffle )
                                {
                                    foreach ( RawSntPair p in sntPairs )
                                    {
                                        src_sw.WriteLine( p.SrcSnt );
                                        tgt_sw.WriteLine( p.TgtSnt );
                                    }
                                }
                                sntPairs.Clear();
                            }
                        }
                    }

                    if ( 0 < sntPairs.Count )
                    {
                        Shuffle( sntPairs );
                        lock ( lockerForShuffle )
                        {
                            foreach ( RawSntPair p in sntPairs )
                            {
                                src_sw.WriteLine( p.SrcSnt );
                                tgt_sw.WriteLine( p.TgtSnt );
                            }
                        }
                        sntPairs.Clear();
                    }
                });
            }

            Logger.WriteLine( $"Shuffled '{corpusSize}' sentence pairs to file '{srcShuffledFilePath}' and '{tgtShuffledFilePath}', total-sent-count: {totalSntCnt}." );

            if ( 0 < tooLongSrcSntCnt )
            {
                Logger.WriteWarnLine( $"Found {tooLongSrcSntCnt} source sentences are longer than '{_MaxSrcSentLength}' tokens, ignore them." );
            }
            if ( 0 < tooLongTgtSntCnt )
            {
                Logger.WriteWarnLine( $"Found {tooLongTgtSntCnt} target sentences are longer than '{_MaxTgtSentLength}' tokens, ignore them." );
            }

            if ( _ShowTokenDist )
            {
                Logger.WriteLine( $"AggregateSrcLength = '{_ShuffleEnums}'" );
                Logger.WriteLine( $"Src token length distribution" );

                var srcTotalNum = dictSrcLenDist.Values.Sum();
                var srcAccNum = 0;
                foreach ( var p in dictSrcLenDist )
                {
                    srcAccNum += p.Value;
                    Logger.WriteLine( $"{p.Key * 100} ~ {(p.Key + 1) * 100}: {p.Value} (acc: {100.0f * (float) srcAccNum / (float) srcTotalNum:F}%)" );
                }

                Logger.WriteLine( $"Tgt token length distribution" );

                var tgtTotalNum = dictTgtLenDist.Values.Sum();
                var tgtAccNum = 0;
                foreach ( var pair in dictTgtLenDist )
                {
                    tgtAccNum += pair.Value;
                    Logger.WriteLine( $"{pair.Key * 100} ~ {(pair.Key + 1) * 100}: {pair.Value}  (acc: {100.0f * (float) tgtAccNum / (float) tgtTotalNum:F}%)" );
                }

                _ShowTokenDist = false;
            }

            return (srcShuffledFilePath, tgtShuffledFilePath);
        }

        private static bool IsSameSntLen( List< List< string > > groups, int[] lens )
        {
            for ( int i = 0; i < lens.Length; i++ )
            {
                if ( lens[ i ] != groups[ i ].Count )
                {
                    return (false);
                }
            }
            return (true);
        }
        private static void UpdateSntLen( List< List< string > > groups, int[] lens )
        {
            for ( int i = 0; i < lens.Length; i++ )
            {
                lens[ i ] = groups[ i ].Count;
            }
        }

        public IEnumerable< CorpusBatch > GetSntPairBatchs()
        {
            (var srcShuffledFilePath, var tgtShuffledFilePath) = ShuffleAll();
            try
            {
                using ( var src_sr = new StreamReader( srcShuffledFilePath ) )
                using ( var tgt_sr = new StreamReader( tgtShuffledFilePath ) )
                {
                    int[] lastSrcSntLen = null;
                    int[] lastTgtSntLen = null;
                    var maxOutputsSize = _BatchSize * 10000;
                    var outputs = new List<SntPair>();

                    while ( !_Ct.IsCancellationRequested )
                    {
                        var line = src_sr.ReadLine();
                        if ( line == null )
                        {
                            break;
                        }

                        var src_line = line.Trim();
                        var tgt_line = tgt_sr.ReadLine().Trim();
                        var sntPair = new SntPair( src_line, tgt_line );

                        if ( lastSrcSntLen == null )
                        {
                            lastSrcSntLen = new int[ sntPair.SrcTokenGroups.Count ];
                            lastTgtSntLen = new int[ sntPair.TgtTokenGroups.Count ];

                            for ( int i = 0; i < lastSrcSntLen.Length; i++ ) lastSrcSntLen[ i ] = -1;
                            for ( int i = 0; i < lastTgtSntLen.Length; i++ ) lastTgtSntLen[ i ] = -1;
                        }

                        if ( ((lastTgtSntLen[ 0 ] > 0) && (_ShuffleEnums == ShuffleEnums.NoPaddingInTgt) && !IsSameSntLen( sntPair.TgtTokenGroups, lastTgtSntLen )) ||
                             ((lastSrcSntLen[ 0 ] > 0) && (_ShuffleEnums == ShuffleEnums.NoPaddingInSrc) && !IsSameSntLen( sntPair.SrcTokenGroups, lastSrcSntLen )) ||
                             ((lastSrcSntLen[ 0 ] > 0) && (lastTgtSntLen[ 0 ] > 0) && (_ShuffleEnums == ShuffleEnums.NoPadding) && (!IsSameSntLen( sntPair.TgtTokenGroups, lastTgtSntLen ) || !IsSameSntLen( sntPair.SrcTokenGroups, lastSrcSntLen ))) ||
                             (outputs.Count > maxOutputsSize )
                           )
                        {
                            for ( int i = 0; (i < outputs.Count) && !_Ct.IsCancellationRequested; i += _BatchSize )
                            {
                                int size = Math.Min( _BatchSize, outputs.Count - i );
                                var batch = new CorpusBatch( outputs.GetRange( i, size ) );
                                yield return (batch);
                            }

                            outputs.Clear();
                        }

                        outputs.Add( sntPair );

                        if ( lastSrcSntLen != null )
                        {
                            UpdateSntLen( sntPair.SrcTokenGroups, lastSrcSntLen );
                            UpdateSntLen( sntPair.TgtTokenGroups, lastTgtSntLen );
                        }
                    }

                    for ( int i = 0; (i < outputs.Count) && !_Ct.IsCancellationRequested; i += _BatchSize )
                    {
                        int size = Math.Min( _BatchSize, outputs.Count - i );
                        var batch = new CorpusBatch( outputs.GetRange( i, size ) );
                        yield return (batch);
                    }
                }
            }
            finally
            {
                File_Delete_NoThrow( srcShuffledFilePath );
                File_Delete_NoThrow( tgtShuffledFilePath );
            }
        }

        protected static void File_Delete_NoThrow( string fn )
        {
            try
            {
                File.Delete( fn );
            }
            catch
            {
                ;
            }
        }

        private static string GetTempWorkDirPath() => Path.Combine( Directory.GetCurrentDirectory(), ".tmp" );

        private static (string srcFilePath, string tgtFilePath) ConvertSequenceLabelingFormatToParallel( string filePath )
        {
            var separator = new char[] { ' ', '\t' };

            var sents    = new List< IList< (string src, string tgt) > >();
            var currSent = new List< (string src, string tgt) >();

            using var sr = new StreamReader( filePath );
            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                if ( line.IsNullOrEmpty() )
                {
                    if ( 0 < currSent.Count )
                    {
                        sents.Add( currSent.ToArray() );
                        currSent.Clear();
                    }
                }
                else
                {
                    var array = line.Split( separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );
                    if ( array.Length < 2 ) continue;
                    var src = array[ 0 ];
                    var tgt = array[ 1 ];

                    currSent.Add( (src, tgt) );
                }
            }
            if ( 0 < currSent.Count )
            {
                sents.Add( currSent.ToArray() );
            }

            var tmp_dir = GetTempWorkDirPath();
            var srcFilePath = Path.Combine( tmp_dir, Path.GetRandomFileName() + "_src.tmp" );
            var tgtFilePath = Path.Combine( tmp_dir, Path.GetRandomFileName() + "_tgt.tmp" );
            if ( !Directory.Exists( tmp_dir ) ) Directory.CreateDirectory( tmp_dir );

            using var src_sw = new StreamWriter( srcFilePath, append: false );
            using var tgt_sw = new StreamWriter( tgtFilePath, append: false );
            foreach ( var sent in sents )
            {
                for ( int i = 0, len = sent.Count - 1; i <= len; i++ )
                {
                    var (src, tgt) = sent[ i ];
                    src_sw.Write( src );
                    tgt_sw.Write( tgt );
                    if ( i != len )
                    {
                        src_sw.Write( ' ' );
                        tgt_sw.Write( ' ' );
                    }
                }
                src_sw.WriteLine();
                tgt_sw.WriteLine();
            }

            Logger.WriteLine( $"Convert sequence labeling corpus file '{filePath}' to parallel corpus files '{srcFilePath}' and '{tgtFilePath}'." );

            return (srcFilePath, tgtFilePath);
        }
    }
}
