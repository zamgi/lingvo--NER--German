namespace Lingvo.NER.Rules.crfsuite
{
    /// <summary>
    /// Аттрибут линейного CRF алгоритма
    /// </summary>
    public sealed class CRFAttribute
	{
        public CRFAttribute( char attributeName, int position, int columnIndex )
        {
            AttributeName = attributeName;
            Position      = position; //PositionPlus1 = Position + 1;
            ColumnIndex   = columnIndex;
        }
	
        /// <summary>
        /// название аттрибута
        /// </summary>
        public readonly char AttributeName;

        /// <summary>
        /// индекс позиции аттрибута
        /// </summary>
        public readonly int Position;
        /// <summary>
        /// Position + 1 => comfortable for MorphoAmbiguityResolver
        /// </summary>
        //public readonly int PositionPlus1;

        public readonly int ColumnIndex;
#if DEBUG
        public override string ToString() => $"[{AttributeName}:{Position}], position: {Position}, column-index: {ColumnIndex}"; 
#endif
    };
}