using System;
using System.Collections.Generic;
using System.Linq;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.NeuralNetwork.WebService
{
    /// <summary>
    /// 
    /// </summary>
    internal static class ModelsExtensions
    {
        [M(O.AggressiveInlining)] public static ResultVM ToResultVM( this IReadOnlyCollection< (IList< ResultVM.TupleVM > nerResult, Exception error) > seq ) 
            => new ResultVM() { NerResults = seq.Select( t => new ResultVM.NerResultVM() { Tuples = t.nerResult, Error = new ErrorVM( t.error ) } ) .ToList() };
        [M(O.AggressiveInlining)] public static ResultVM ToResultVM( this in (IList< ResultVM.TupleVM > nerResult, Exception error) t ) 
            => new ResultVM() { NerResults = new[] { new ResultVM.NerResultVM() { Tuples = t.nerResult, Error = new ErrorVM( t.error ) } } };
        [M(O.AggressiveInlining)] public static ResultVM.TupleVM ToTupleVM( this (string word, string ner) t ) => new ResultVM.TupleVM() { Word = t.word, Ner = t.ner };
        [M(O.AggressiveInlining)] public static ErrorVM ToErrorVM( this Exception ex ) => new ErrorVM() { ErrorMessage = ex.Message, FullErrorMessage = ex.ToString(), };

#if DEBUG
        public static string ToText( this in ParamsVM p )
        {
            if ( (p.Text != null) && (250 < p.Text.Length) )
            {
                return (p.Text.Substring( 0, 250 ) + "...");
            }
            return (p.Text);
        }
#endif
    }
}
