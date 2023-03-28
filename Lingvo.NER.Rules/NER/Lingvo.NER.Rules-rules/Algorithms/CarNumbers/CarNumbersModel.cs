using System.Collections.Generic;
using System.IO;

using Lingvo.NER.Rules.core;

namespace Lingvo.NER.Rules.CarNumbers
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICarNumbersModel
    {
        bool IsCarNumberPreamble( string preamble );
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class CarNumbersModel : ICarNumbersModel
    {
        private Set< string > _Set;
        public CarNumbersModel( in (string carNumbersFilename, int? capacity) t ) : this( t.carNumbersFilename, t.capacity ) { }
        public CarNumbersModel( string carNumbersFilename, int? capacity = null ) => _Set = Init_CarNumbers( carNumbersFilename, capacity );
        public CarNumbersModel( StreamReader sr, int? capacity = null ) => _Set = Init_CarNumbers( sr, capacity );

        #region [.Car Numbers.]
        private static Set< string > Init_CarNumbers( string carNumbersFilename, int? capacity )
        {
            using ( var sr = new StreamReader( carNumbersFilename ) )
            {
                return (Init_CarNumbers( sr, capacity ));
            }
        }
        private static Set< string > Init_CarNumbers( StreamReader sr, int? capacity )
        {
            var set = new Set< string >( capacity.GetValueOrDefault() );

            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                var carNumber = StringsHelper.ToUpperInvariantInPlace_2( line.Trim() );
                if ( !carNumber.IsNullOrEmpty() )
                {
                    set.Add( carNumber );
                }
            }

            return (set);
        }

        public bool IsCarNumberPreamble( string preamble ) => (!preamble.IsNullOrEmpty() && _Set.Contains( preamble ));
        #endregion
    }
}
