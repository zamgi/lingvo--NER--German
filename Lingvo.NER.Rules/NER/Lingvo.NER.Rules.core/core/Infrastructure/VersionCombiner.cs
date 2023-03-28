using System.Collections.Generic;
using System.Linq;

namespace Lingvo.NER.Rules.core.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class VersionCombiner< T > 
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class EqualityComparer : IEqualityComparer< T[] >
        {
            public bool Equals( T[] x, T[] y ) => x.SequenceEqual( y );
            public int GetHashCode( T[] a ) => a.Length;
        }
        /// <summary>
        /// 
        /// </summary>
        private sealed class EqualityComparer2 : IEqualityComparer< T[] >
        {
            private IEqualityComparer< T > _Comparer;
            public EqualityComparer2( IEqualityComparer< T > comparer ) => _Comparer = comparer;
            public bool Equals( T[] x, T[] y ) => x.SequenceEqual( y, _Comparer );
            #region comm.
            //{
            //    if ( x.Length != y.Length )
            //        return (false);
            //    for ( var i = x.Length - 1; 0 <= i; i-- )
            //    {
            //        if ( x[ i ] != y[ i ] )
            //            return (false);
            //    }
            //    return (true);
            //} 
            #endregion
            public int GetHashCode( T[] a ) => a.Length;
        }

        private HashSet< T[] > _HS;
        private T _CurrentInsertValue;
        public VersionCombiner( IEqualityComparer< T > comparer = null )
        {
            static IEqualityComparer< T[] > getSeqComparer( IEqualityComparer< T > c )
            {
                if ( c != null ) return (new EqualityComparer2( c ));
                return (new EqualityComparer());
            };

            _HS = new HashSet< T[] >( getSeqComparer( comparer ) );
        }

        public IEnumerable< T[] > GetVersions( T[] array, T insertValue, bool returnOriginArray = true )
        {
            _HS.Clear();
            _CurrentInsertValue = insertValue;

            if ( returnOriginArray )
            {
                yield return (array);
            }

            for ( var i = 1; i <= array.Length - 1; i++ )
            {
                foreach ( var a in GetVersion( array, i ) )
                {
                    yield return (a);
                }
            }

            //_HS.Clear();
            //_CurrentInsertStr = null;
        }
        private IEnumerable< T[] > GetVersion( T[] array, int insertPos )
        {
            var new_array = new T[ array.Length + 1 ];
            var j = 0;
            for ( var i = 0; i < array.Length; i++ )
            {
                if ( i == insertPos )
                {
                    new_array[ j++ ] = _CurrentInsertValue;
                }
                new_array[ j++ ] = array[ i ];
            }

            insertPos++;
            if ( insertPos < array.Length )
            {
                foreach ( var a in GetVersion( array, insertPos ) )
                {
                    yield return (a);
                }
            }
            insertPos++;
            if ( insertPos < new_array.Length )
            {
                foreach ( var a in GetVersion( new_array, insertPos ) )
                {
                    yield return (a);
                }
            }

            if ( _HS.Add( new_array ) )
            {
                yield return (new_array);
            }
        }


        public IEnumerable< IList< T > > GetVersions( IList< T > array, T insertValue, bool returnOriginArray = true )
        {
            _HS.Clear();
            _CurrentInsertValue = insertValue;

            if ( returnOriginArray )
            {
                yield return (array);
            }

            for ( var i = 1; i <= array.Count - 1; i++ )
            {
                foreach ( var a in GetVersion( array, i ) )
                {
                    yield return (a);
                }
            }

            //_HS.Clear();
            //_CurrentInsertStr = null;
        }
        private IEnumerable< IList< T > > GetVersion( IList< T > array, int insertPos )
        {
            var new_array = new T[ array.Count + 1 ];
            var j = 0;
            for ( var i = 0; i < array.Count; i++ )
            {
                if ( i == insertPos )
                {
                    new_array[ j++ ] = _CurrentInsertValue;
                }
                new_array[ j++ ] = array[ i ];
            }

            insertPos++;
            if ( insertPos < array.Count )
            {
                foreach ( var a in GetVersion( array, insertPos ) )
                {
                    yield return (a);
                }
            }
            insertPos++;
            if ( insertPos < new_array.Length )
            {
                foreach ( var a in GetVersion( new_array, insertPos ) )
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
