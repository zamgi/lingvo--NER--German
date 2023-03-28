using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Lingvo.NER.Rules.crfsuite
{
    /// <summary>
    /// Загрузчик шаблонов
    /// </summary>
    internal sealed class CRFTemplateFileLoader
    {
        #region [.private field's.]
        // Названия групп внутри регулярных выражений
        private const string TEMPLATES = "Templates";
        private const string TEMPLATE  = "Template";
        private const string FIELDS    = "Fields";

        /// Регулярное выражение для выделения шаблонов
        private Regex _TemplatesRegex;

        /// регулярное выражение для выделения одного шаблона
        private Regex _TemplateRegex;

        /// Регулярное выражение для выделения названий столбцов
        private Regex _FieldsRegex;
        #endregion

        private CRFTemplateFileLoader()
        {
            _TemplatesRegex = new Regex( "templates\\s*=\\s*\\((?<" + TEMPLATES + ">(\\s*.+)*)\\s*\\)", RegexOptions.IgnoreCase );
            _TemplateRegex  = new Regex( "\\((?<" + TEMPLATE + ">[^(]*)\\),", RegexOptions.IgnoreCase );
            _FieldsRegex    = new Regex( "fields\\s*=\\s*'(?<" + FIELDS + ">[^']*)'", RegexOptions.IgnoreCase );
        }

        public static CRFTemplateFile _Load_( string filePath ) => (new CRFTemplateFileLoader()).Load( filePath );
        public static CRFTemplateFile _Load_( StreamReader sr ) => (new CRFTemplateFileLoader()).Load( sr );
        public static CRFTemplateFile _Load_( string filePath, char[] allowedColumnNames ) => (new CRFTemplateFileLoader()).Load( filePath, allowedColumnNames );
        public static CRFTemplateFile _Load_( StreamReader sr, char[] allowedColumnNames ) => (new CRFTemplateFileLoader()).Load( sr, allowedColumnNames );
        ///Загрузить файл шаблона
        ///@param filePath Путь к файлу шаблона
        ///@return файл шаблона
        private CRFTemplateFile Load( string filePath )
		{
			using ( var sr = new StreamReader( filePath ) )
            {
			    return (Load( sr ));
            }
		}
        private CRFTemplateFile Load( StreamReader sr )
        {
            var text = sr.ReadToEnd();

            var columnNames           = ExtractColumnNames( text );
            var columnIndexDictionary = CreateColumnIndexDictionary( columnNames );
            var attributeTemplates    = ExtractAttributeTemplates( text, columnIndexDictionary );

            return (new CRFTemplateFile( columnNames, attributeTemplates ));
        }
        private CRFTemplateFile Load( string filePath, char[] allowedColumnNames ) => Load( Load( filePath ), allowedColumnNames );
        private CRFTemplateFile Load( StreamReader sr, char[] allowedColumnNames ) => Load( Load( sr ), allowedColumnNames );
        private CRFTemplateFile Load( CRFTemplateFile templateFile, char[] allowedColumnNames )
        {
            if ( (allowedColumnNames != null) && (allowedColumnNames.Length != 0) )
            {
                var hs = new HashSet< char >( allowedColumnNames );
                foreach ( var columnName in templateFile.ColumnNames )
                {
                    if ( !hs.Contains( columnName ) )
                    {
                        throw (new InvalidDataException( "Invalid column-name: '" + columnName + "', allowed only '" + string.Join( ",", allowedColumnNames ) + "'" ));
                    }
                }
            }
            return (templateFile);
        }

        // Извлечь шаблоны аттрибутов
        // @param fileString - Содержимое файла-шаблона
        // @return - Шаблоны аттрибутов
        private CRFNgram[] ExtractAttributeTemplates( string text, Dictionary< char, int > columnIndexDictionary )
        {
            var attributeTemplateStrings = ExtractAttributeTemplateStrings( text );
            var split_chars = new[] { ',' };
            var attributeTemplates = new List< CRFNgram >( attributeTemplateStrings.Length );
            foreach ( var str in attributeTemplateStrings )
            {
                MatchCollection matchCollection = _TemplateRegex.Matches( str );
                if ( matchCollection.Count == 0 )
                    continue;

                var attributeTemplate = new List< CRFAttribute >( matchCollection.Count );
                foreach ( Match currentMatch in matchCollection )
                {
                    var oneTemplate = currentMatch.Groups[ TEMPLATE ].Value;
                    var pair = oneTemplate.Split( split_chars );

                    var attributeName = ParseAttributeName( pair[ 0 ] );
                    if ( attributeName.Length != 1 )
                    {
                        throw (new InvalidDataException( $"Attribute-name is not valid, must be one-char: '{attributeName }'" ));
                    }
                    var attributeNameChar = attributeName[ 0 ]; //char.ToUpperInvariant( attributeName[ 0 ] );
                    var position      = int.Parse( pair[ 1 ] );
                    var columnIndex   = columnIndexDictionary[ attributeNameChar ];                    

                    attributeTemplate.Add( new CRFAttribute( attributeNameChar, position, columnIndex ) );
                }
                attributeTemplates.Add( new CRFNgram( attributeTemplate.ToArray() ) );
            }
            return (attributeTemplates.ToArray());
        }

        // Извлечь название аттрибута
        // @param attrStr - Строка, содержащая название аттрибута
        // @return - Название аттрибута
        private static string ParseAttributeName( string attrStr )
        {
            int startIndex = attrStr.IndexOf( '\'' ) + 1;
            int endIndex   = attrStr.IndexOf( '\'', startIndex);
            return (attrStr.Substring( startIndex, endIndex - startIndex ));
        }

        // Извлечь названия столбцов
        // @param fileString - Содержимое файла-шаблона
        // @return - Названия столбцов
        private char[] ExtractColumnNames( string text )
        {
            Match match = _FieldsRegex.Match( text );
            var columnNames = match.Groups[ FIELDS ].Value.Split( ' ', '\t', '\n' );
            var columnNameChars = new char[ columnNames.Length ];
            for ( int i = 0; i < columnNames.Length; i++ )
            {
                var columnName = columnNames[ i ];
                if ( columnName.Length != 1 )
                {
                    throw (new InvalidDataException( $"Column-name is not valid, must be one-char: '{columnName}'" ));
                }
                columnNameChars[ i ] = columnName[ 0 ];
            }
            return (columnNameChars);
        }

        // Извлечь строки, соответствующие шаблонам аттрибутов
        // @param fileString - Содержимое файла-шаблона
        // @return - Строки, соответствующие шаблонам аттрибутов
        private string[] ExtractAttributeTemplateStrings( string fileString )
        {
            Match templatesMatch = _TemplatesRegex.Match( fileString );
            string templates = templatesMatch.Groups[ TEMPLATES ].Value;

            templates = Regex.Replace( templates, "\\s*\\(\\s*\\(\\s*", "(" );
            templates = Regex.Replace( templates, ",\\s*\\)\\s*,", ",\n" );

            return (templates.Split( new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries ));
        }

		/// <summary>
        /// Проинициализировать словарь индексов аттрибутов
		/// </summary>
        private static Dictionary< char, int > CreateColumnIndexDictionary( char[] columnNames )
		{
            var dict = new Dictionary< char, int >( columnNames.Length );
			var index = 0;
            foreach ( var columnName in columnNames )
			{
				dict.Add( columnName, index );
				index++;
			}
            return (dict);
		}
    }
}
