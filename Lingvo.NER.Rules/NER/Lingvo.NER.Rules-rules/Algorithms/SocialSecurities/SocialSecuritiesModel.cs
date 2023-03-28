using System.Collections.Generic;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.SocialSecurities
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISocialSecuritiesModel
    {
        bool IsSocialSecurityPreamble( string preamble );
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class SocialSecuritiesModel : ISocialSecuritiesModel
    {
        private Set< int > _Set;
        //public SocialSecuritiesModel( string fileName, int? capacity = null ) => Init( fileName, capacity );
        public SocialSecuritiesModel() => Init();

        #region [.Social Securities.]
        private void Init()
        {
            _Set = new Set< int >( 70 );
            _Set.Add( 2 );
            _Set.Add( 3 );
            _Set.Add( 4 );
            _Set.Add( 8 );
            _Set.Add( 9 );
            _Set.Add( 10 );
            _Set.Add( 11 );
            _Set.Add( 12 );
            _Set.Add( 13 );
            _Set.Add( 14 );
            _Set.Add( 15 );
            _Set.Add( 16 );
            _Set.Add( 17 );
            _Set.Add( 18 );
            _Set.Add( 19 );
            _Set.Add( 20 );
            _Set.Add( 21 );
            _Set.Add( 23 );
            _Set.Add( 24 );
            _Set.Add( 25 );
            _Set.Add( 26 );
            _Set.Add( 28 );
            _Set.Add( 29 );
            _Set.Add( 38 );
            _Set.Add( 39 );
            _Set.Add( 40 );
            for ( var i = 42; i <= 79; i++ )
            {
                _Set.Add( i );
            }
            _Set.Add( 80 );
            _Set.Add( 81 );
            _Set.Add( 82 );
            _Set.Add( 89 );
        }
        //private void Init( string fileName, int? capacity )
        //{
        //    _Set = new Set< string >( capacity.GetValueOrDefault() );

        //    using ( var sr = new StreamReader( fileName ) )
        //    {
        //        for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
        //        {
        //            var carNumber = StringsHelper.ToUpperInvariantInPlace_2( line.Trim() );
        //            if ( !carNumber.IsNullOrEmpty() )
        //            {
        //                _Set.Add( carNumber );
        //            }
        //        }
        //    }
        //}
        //public bool IsSocialSecurityPreamble( string preamble ) => (!preamble.IsNullOrEmpty() && _Set.Contains( preamble ));

        [M(O.AggressiveInlining)] private static bool IsDigit( char ch ) => ('0' <= ch) && (ch <= '9');
        public bool IsSocialSecurityPreamble( string preamble )
        {
            if ( (preamble != null) && (2 <= preamble.Length) )
            {
                var ch_0 = preamble[ 0 ];
                var ch_1 = preamble[ 1 ];
                if ( IsDigit( ch_0 ) && IsDigit( ch_1 ) )
                {
                    var num = 10 * (ch_0 - '0') + (ch_1 - '0');
                    return (_Set.Contains( num ));
                }                
            }
            return (false);
        }
        #endregion
    }
}
