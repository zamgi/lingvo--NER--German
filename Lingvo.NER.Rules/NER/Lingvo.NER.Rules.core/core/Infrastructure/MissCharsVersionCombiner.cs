using System.Collections.Generic;
using System.Linq;

namespace Lingvo.NER.Rules.core.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MissCharsVersionCombiner
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class EqualityComparer : IEqualityComparer< string[] >
        {
            public bool Equals( string[] x, string[] y ) => x.SequenceEqual( y );
            public int GetHashCode( string[] a ) => a.Length;
        }

        private Set< char > _MissChars;
        private List< string > _Parts;
        private List< string > _Separs;
        //private string _CurrentInsertValue;
        private HashSet< string[] > _HS;
        //private StringBuilder _Buf;
        public MissCharsVersionCombiner( char[] can_miss_chars )
        {
            //_Buf   = new StringBuilder();
            _Parts  = new List< string >();
            _Separs = new List< string >();
            _HS     = new HashSet< string[] >( new EqualityComparer() );

            _MissChars = Set< char >.CreateWithCloserCapacity( can_miss_chars.Length );
            foreach ( var ch in can_miss_chars ) _MissChars.Add( ch );
        }

        public IEnumerable< IList< string > > GetVersions( string text )
        {
            #region [.1.]
            _Parts.Clear();
            _Separs.Clear();
            int len;
            var start_i = 0;
            for ( var i = 0; i < text.Length; i++ )
            {
                var ch = text[ i ];
                if ( _MissChars.Contains( ch ) )
                {
                    len = i - start_i;
                    //---if ( 0 < len )
                    {
                        var s = text.Substring( start_i, len );
                        _Parts.Add( s );
                        _Separs.Add( ch.ToString() );
                    }
                    start_i = i + 1;
                }
            }
            len = text.Length - start_i;
            //---if ( 0 < len )
            {
                var s = text.Substring( start_i, len );
                _Parts.Add( s );
            }
            #endregion
            //---------------------------------------------//

            #region [.2.]
            _HS.Clear();

            yield return (_Parts);

            for ( var i = 1; i <= _Parts.Count - 1; i++ )
            {
                foreach ( var a in GetVersion( _Parts, i, i - 1 ) )
                {
                    yield return (a);
                }
            }
            #endregion
        }
        private IEnumerable< IList< string > > GetVersion( IList< string > array, int insertPos, int insertCharPos )
        {
            var new_array = new string[ array.Count + 1 ];
            var j = 0;
            for ( var i = 0; i < array.Count; i++ )
            {
                if ( i == insertPos )
                {
                    new_array[ j++ ] = _Separs[ insertCharPos ]; //---_CurrentInsertValue;
                }
                new_array[ j++ ] = array[ i ];
            }

            insertPos++;
            if ( insertPos < array.Count )
            {
                insertCharPos++;
                foreach ( var a in GetVersion( array, insertPos, insertCharPos ) )
                {
                    yield return (a);
                }
            }
            insertPos++;
            if ( insertPos < new_array.Length )
            {
                //insertCharPos++;
                foreach ( var a in GetVersion( new_array, insertPos, insertCharPos ) )
                {
                    yield return (a);
                }
            }

            if ( _HS.Add( new_array ) )
            {
                yield return (new_array);
            }
        }
    }
}
