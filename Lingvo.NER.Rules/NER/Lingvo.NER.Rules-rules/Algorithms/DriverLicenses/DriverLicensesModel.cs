using System.Collections.Generic;
using System.IO;

using Lingvo.NER.Rules.core;

namespace Lingvo.NER.Rules.DriverLicenses
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDriverLicensesModel
    {
        bool IsDriverLicensePreamble( string preamble, out int length );
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class DriverLicensesModel : IDriverLicensesModel
    {
        private SortedStringList_WithValueAndSearchByPart _SSL;
        public DriverLicensesModel( in (string filename, int? capacity) t ) : this( t.filename, t.capacity ) { }
        public DriverLicensesModel( string filename, int? capacity = null ) => _SSL = Init( filename, capacity );
        public DriverLicensesModel( StreamReader sr, int? capacity = null ) => _SSL = Init( sr, capacity );

        #region [.Driver Licenses.]
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
                var num = StringsHelper.ToUpperInvariantInPlace_2( line.Trim() );
                if ( !num.IsNullOrEmpty() )
                {
                    ssl.TryAdd( num, num );
                }
            }

            return (ssl);
        }

        public bool IsDriverLicensePreamble( string preamble, out int length )
        {
            if ( !preamble.IsNullOrEmpty() && _SSL.TryGetValueByPart( preamble, 0, out var existsValue ) )
            {
                length = existsValue.Length;
                return (true);
            }
            length = default;
            return (false);
        }
        #endregion
    }
}
