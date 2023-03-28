using System.Collections.Generic;
using System.IO;

using Lingvo.NER.Rules.core;

namespace Lingvo.NER.Rules.TaxIdentifications
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITaxIdentificationsModel
    {
        bool IsOldTaxIdentifications( string value, out int length );
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class TaxIdentificationsModel : ITaxIdentificationsModel
    {
        private SortedStringList_WithValueAndSearchByPart _SSL;
        public TaxIdentificationsModel( in (string filename, int? capacity) t ) : this( t.filename, t.capacity ) { }
        public TaxIdentificationsModel( string filename, int? capacity = null ) => _SSL = Init( filename, capacity );
        public TaxIdentificationsModel( StreamReader sr, int? capacity = null ) => _SSL = Init( sr, capacity );

        private static SortedStringList_WithValueAndSearchByPart Init( string filename, int? capacity )
        {
            using ( var sr = new StreamReader( filename ) )
            {
                return (Init( sr, capacity ));
            }
        }
        private static SortedStringList_WithValueAndSearchByPart Init( StreamReader sr, int? capacity )
        {
            var ssl = new SortedStringList_WithValueAndSearchByPart( capacity.GetValueOrDefault() );

            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                var num = line.Trim();
                if ( !num.IsNullOrEmpty() )
                {
                    ssl.TryAdd( num, num );
                }
            }

            return (ssl);
        }

        public bool IsOldTaxIdentifications( string value, out int length )
        {
            if ( !value.IsNullOrEmpty() && _SSL.TryGetValueByPart( value, 0, out var existsValue ) )
            {
                length = existsValue.Length;
                return (true);
            }
            length = default;
            return (false);
        }
    }
}
