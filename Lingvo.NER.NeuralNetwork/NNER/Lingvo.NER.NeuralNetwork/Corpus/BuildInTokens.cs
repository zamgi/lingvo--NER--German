using System.Collections.Generic;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.NeuralNetwork.Text
{
    /// <summary>
    /// 
    /// </summary>
    public static class BuildInTokens
    {
        public const string EOS = "</s>";
        public const string BOS = "<s>";
        public const string UNK = "<unk>";
        public const string CLS = "[CLS]";
        public const string SEP = "[SEP]";

        [M(O.AggressiveInlining)] public static bool IsPreDefinedToken( string str )
        {
            switch ( str )
            {
                case EOS: case BOS: case UNK: case CLS: return (true);
                default: return (false);
            }
            //return (str == EOS || str == BOS || str == UNK || str == CLS);
        }

        /// <summary>
        /// Pad given sentences to the same length and return their original length
        /// </summary>
        [M(O.AggressiveInlining)] public static float[] PadSentences( List< List< string > > s, int maxLen = -1 )
        {
            var originalLengths = new float[ s.Count ];
            if ( maxLen <= 0 )
            {
                foreach ( var lst in s )
                {
                    if ( maxLen < lst.Count )
                    {
                        maxLen = lst.Count;
                    }
                }
            }

            for ( int i = 0; i < s.Count; i++ )
            {
                int count = s[ i ].Count;
                originalLengths[ i ] = count;

                var s_i = s[ i ];
                for ( int j = 0, len = maxLen - count; j < len; j++ )
                {
                    s_i.Add( EOS );
                }
            }
            return (originalLengths);
        }
        [M(O.AggressiveInlining)] public static void PadSentences_2( List< List< string > > s, int maxLen = -1 )
        {
            if ( maxLen <= 0 )
            {
                foreach ( var lst in s )
                {
                    if ( maxLen < lst.Count )
                    {
                        maxLen = lst.Count;
                    }
                }
            }

            for ( int i = 0; i < s.Count; i++ )
            {
                int count = s[ i ].Count;

                var s_i = s[ i ];
                for ( int j = 0, len = maxLen - count; j < len; j++ )
                {
                    s_i.Add( EOS );
                }
            }
        }
    }
}
