using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lingvo.NER.Rules.crfsuite
{
    /// <summary>
    /// Внутреннее представление шаблона для построения входных данных SRFSuitNER
    /// </summary>
    public sealed class CRFTemplateFile
	{
        /// <summary>
        /// 
        /// </summary>
        private struct index_count_t : IEqualityComparer< index_count_t >
        {
            public static readonly index_count_t Instance = new index_count_t();

            public index_count_t( int index, int count )
            {
                Index = index;
                Count = count;
            }

            public int Index;
            public int Count;

            public bool Equals( index_count_t x, index_count_t y ) => ((x.Index == y.Index) && (x.Count == y.Count));
            public int GetHashCode( index_count_t obj ) => (Index.GetHashCode() ^ Count.GetHashCode());
        }

        private readonly int _MinAttributePosition;
        private readonly int _MaxAttributePosition;
        private readonly Dictionary< index_count_t, CRFNgram[] > _Dictionary;

        /// <summary>
        /// Конструктор шаблона для построения входных данных SRFSuitNER
        /// </summary>
        /// <param name="columnNames">Наименования столбцов преобразованного входного файла</param>
        /// <param name="ngrams">шаблоны N-грамм</param>
		public CRFTemplateFile( char[] columnNames, CRFNgram[] ngrams )
		{
            CheckTemplate( columnNames, ngrams );

			_ColumnNames = columnNames;
			_Ngrams      = ngrams;

            var positions = from ngram in _Ngrams
                            from attr in ngram.Attributes
                            select attr.Position;
            _MinAttributePosition = positions.Min();
            _MaxAttributePosition = positions.Max();

            _Dictionary = new Dictionary< index_count_t, CRFNgram[] >( index_count_t.Instance );
		}

        public static CRFTemplateFile Load( string filePath ) => CRFTemplateFileLoader._Load_( filePath );
        public static CRFTemplateFile Load( StreamReader sr ) => CRFTemplateFileLoader._Load_( sr );
        public static CRFTemplateFile Load( string filePath, char[] allowedColumnNames ) => CRFTemplateFileLoader._Load_( filePath, allowedColumnNames );
        public static CRFTemplateFile Load( StreamReader sr, char[] allowedColumnNames ) => CRFTemplateFileLoader._Load_( sr, allowedColumnNames );

        /// <summary>
        /// // Наименования столбцов преобразованного входного файла
        /// </summary>
        public IReadOnlyList< char > ColumnNames => _ColumnNames;
        private char[] _ColumnNames;

        /// <summary>
        /// // шаблоны N-грамм
        /// </summary>
        public IReadOnlyList< CRFNgram > Ngrams => _Ngrams;
        private CRFNgram[] _Ngrams;

        public IReadOnlyList< CRFNgram > GetNgramsWhichCanTemplateBeApplied( int wordIndex, int wordsCount )
        {
            var i1 = wordIndex + _MinAttributePosition;                    if ( 0 < i1 ) i1 = 0;
            var i2 = wordsCount - (wordIndex + _MaxAttributePosition) - 1; if ( 0 < i2 ) i2 = 0;
            var wordIndexAndCountTuple = new index_count_t( i1, i2 );

            if ( !_Dictionary.TryGetValue( wordIndexAndCountTuple, out var ngrams ) )
            {
                var lst = new List< CRFNgram >();
                foreach ( var ngram in _Ngrams )
                {
                    if ( ngram.CanTemplateBeApplied( wordIndex, wordsCount ) )
                    {
                        lst.Add( ngram );
                    }
                }
                ngrams = lst.ToArray();

                _Dictionary.Add( wordIndexAndCountTuple, ngrams );
            }
            return (ngrams);
        }


        private static void CheckTemplate( char[] columnNames, CRFNgram[] ngrams )
        {
            var hs = new HashSet< char >( columnNames );

            foreach ( var ngram in ngrams )
            {
                foreach ( var attr in ngram.Attributes )
                {
                    if ( !hs.Contains( attr.AttributeName ) )
                    {
                        throw (new InvalidDataException( $"Attribute '{attr.AttributeName}' not contained in the column names of the CRF template file: '{string.Join( "', '", columnNames )}'" ));
                    }
                }
            }
        }
	};
}
