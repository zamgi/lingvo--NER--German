using System;
using System.Collections.Generic;
using System.Linq;

namespace Lingvo.NER.NeuralNetwork.WebService
{
    /// <summary>
    /// 
    /// </summary>
    public readonly struct ParamsVM
    {
        public ParamsVM( string text ) : this() => Text = text;
        public string Text                 { get; init; }
        public string ModelType            { get; init; }
        public bool   MakePostMerge        { get; init; }
        public int?   MaxPredictSentLength { get; init; }
    }

    /// <summary>
    /// 
    /// </summary>
    public readonly struct ResultVM
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly struct TupleVM
        {
            public string Word { get; init; }
            public string Ner  { get; init; }
            public override string ToString() => $"{Word} | {Ner}";
        }
        /// <summary>
        /// 
        /// </summary>
        public readonly struct NerResultVM
        {
            public IList< TupleVM > Tuples { get; init; }
            public /*Exception*/ErrorVM Error { get; init; }
            public override string ToString() => (Error.ErrorMessage != null) ? Error.ErrorMessage : string.Join( " ", Tuples.Select( t => $"{t.Word}|{t.Ner}" ) );
        }

        public IReadOnlyCollection< NerResultVM > NerResults { get; init; }
        public override string ToString() => string.Join( "\r\n", NerResults );
    }

    /// <summary>
    /// 
    /// </summary>
    public readonly struct ErrorVM
    {
        public ErrorVM( Exception ex )
        {
            ErrorMessage     = ex?.Message;
            FullErrorMessage = ex?.ToString();
        }
        public string ErrorMessage     { get; init; }
        public string FullErrorMessage { get; init; }
        public override string ToString() => ErrorMessage;
    }
}
