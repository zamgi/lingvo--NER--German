using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.NeuralNetwork.Tokenizing
{
    /// <summary>
    /// 
    /// </summary>
    public static partial class NNerExtensions
    {
        public static void SetNNerOutputType( this IList< word_t > words, IList< string > output_words )
        {
            //---Debug.Assert( words.Count == output_words.Count );

            for ( var i = Math.Min( words.Count, output_words.Count ) - 1; 0 <= i; i-- )
            {
                words[ i ].nnerOutputType = output_words[ i ].ToNNerOutputType();
            }
        }

        [M(O.AggressiveInlining)] public static bool TryTokenizeBySents( this Tokenizer tokenizer, string text, out IList< List< word_t > > input_sents )
        {
            var sents = tokenizer.Run_SimpleSentsAllocate( text );
            if ( 0 < sents.Count )
            {
                input_sents = sents.Where( s => 0 < s.Count ).ToList( sents.Count );
                return (0 < input_sents.Count);
            }

            input_sents = default;
            return (false);
        }
        [M(O.AggressiveInlining)] public static bool TryTokenizeBySentsWithLock( this Tokenizer tokenizer, string text, out IList< List< word_t > > input_sents )
        {
            lock ( tokenizer )
            {
                return (tokenizer.TryTokenizeBySents( text, out input_sents ));
            }
        }
        [M(O.AggressiveInlining)] internal static List< T > ToList< T >( this IEnumerable< T > seq, int capatity )
        {
            var lst = new List< T >( capatity );
            lst.AddRange( seq );
            return (lst);
        }
    }
}