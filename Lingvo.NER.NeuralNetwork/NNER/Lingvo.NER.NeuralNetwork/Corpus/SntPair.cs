using System;
using System.Collections.Generic;
using System.Linq;

using Lingvo.NER.NeuralNetwork.Utils;
using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.NeuralNetwork.Text
{
    /// <summary>
    /// 
    /// </summary>
    public class RawSntPair
    {
        private string _SrcSnt;
        private string _TgtSnt;

        private long _SrcGroupLenId;
        private long _TgtGroupLenId;
        private long _GroupLenId;

        private int _SrcLength;
        private int _TgtLength;

        private long _MaxSeqLength;
        public RawSntPair( string srcSnt, string tgtSnt, int maxSrcSeqLength, int maxTgtSeqLength, bool truncateTooLongSeq )
        {
            _MaxSeqLength = Math.Max( maxSrcSeqLength, maxTgtSeqLength );

            if ( truncateTooLongSeq )
            {
                srcSnt = TruncateSeq( srcSnt, maxSrcSeqLength );
                tgtSnt = TruncateSeq( tgtSnt, maxTgtSeqLength );
            }

            _SrcLength = CountWhiteSpace( srcSnt );
            _TgtLength = CountWhiteSpace( tgtSnt );

            _SrcGroupLenId = GenerateGroupLenId( srcSnt, _MaxSeqLength );
            _TgtGroupLenId = GenerateGroupLenId( tgtSnt, _MaxSeqLength );
            _GroupLenId    = GenerateGroupLenId( srcSnt + '\t' + tgtSnt, _MaxSeqLength );

            _SrcSnt = srcSnt;
            _TgtSnt = tgtSnt;
        }

        public bool IsEmptyPair() => _SrcSnt.IsNullOrEmpty() && _TgtSnt.IsNullOrEmpty();

        public string SrcSnt => _SrcSnt;
        public string TgtSnt => _TgtSnt;
        public long SrcGroupLenId => _SrcGroupLenId;
        public long TgtGroupLenId => _TgtGroupLenId;
        public long GroupLenId => _GroupLenId;
        public int SrcLength => _SrcLength;
        public int TgtLength => _TgtLength;

        private static long GenerateGroupLenId( string s, long maxSeqLength )
        {
            long r = 0;
            var array = s.Split( '\t' );
            foreach ( var a in array )
            {
                r *= maxSeqLength;
                r += CountWhiteSpace( a ); //---r += = a.Split( ' ' ).Length;
            }
            return (r);
        }
        [M(O.AggressiveInlining)] private static int CountWhiteSpace( string s )
        {
            var cnt = 0;
            for ( var i = s.Length - 1; 0 <= i; i-- )
            {
                if ( s[ i ] == ' ' )
                {
                    cnt++;
                }
            }
            return (cnt);
        }
        private static string TruncateSeq( string str, int maxSeqLength )
        {
            var array   = str.Split( '\t' );
            var results = new List< string >( array.Length );

            foreach ( var a in array )
            {
                var tokens = a.Split( ' ' );
                if ( tokens.Length <= maxSeqLength )
                {
                    results.Add( a );
                }
                else
                {
                    results.Add( string.Join( ' ', tokens, 0, maxSeqLength ) );
                }
            }

            return (string.Join( "\t", results ));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SntPair
    {
        private List<List<string>> _SrcTokenGroups; //shape: (group_size, sequence_length)
        private List<List<string>> _TgtTokenGroups; //shape: (group_size, sequence_length)

        public SntPair( string srcLine, string tgtLine )
        {
            _SrcTokenGroups = CreateGroup( srcLine );
            _TgtTokenGroups = CreateGroup( tgtLine );
        }

        public List<List<string>> SrcTokenGroups => _SrcTokenGroups;
        public List<List<string>> TgtTokenGroups => _TgtTokenGroups;

        public string PrintSrcTokens()
        {
            var rst = new List<string>( SrcTokenGroups.Count );
            int gIdx = 0;
            foreach ( var g in SrcTokenGroups )
            {
                rst.Add( $"GroupId '{gIdx}': " + string.Join( " ", g ) );
                gIdx++;
            }
            return (string.Join( "\n", rst ));
        }
        public string PrintTgtTokens()
        {
            var rst = new List<string>( TgtTokenGroups.Count );
            int gIdx = 0;
            foreach ( var g in TgtTokenGroups )
            {
                rst.Add( $"GroupId '{gIdx}': " + string.Join( " ", g ) );
                gIdx++;
            }
            return (string.Join( "\n", rst ));
        }

        private static List<List<string>> CreateGroup( string line )
        {
            var groups = line.Split( '\t' );
            var res = new List<List<string>>( groups.Length );
            foreach ( var group in groups )
            {
                var array = group.Split( ' ' );
                res.Add( array.ToList( array.Length ) );
            }
            return (res);
        }
    }
}
