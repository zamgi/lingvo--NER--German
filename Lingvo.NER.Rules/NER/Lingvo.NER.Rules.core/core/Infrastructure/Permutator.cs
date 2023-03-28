using System.Collections.Generic;
using System.Linq;

namespace Lingvo.NER.Rules.core.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    public static class Permutator
    {
        public static List< T[] > GetPermutations< T >( T[] values )
        {
            switch ( values.Length )
            {
                case 1:
                {
                    var s1 = values[ 0 ];

                    var lst = new List< T[] >( 1 );
                    lst.Add( new[] { s1 } );
                    return (lst);
                }
                case 2:
                {
                    var str1 = values[ 0 ];
                    var str2 = values[ 1 ];

                    var lst = new List< T[] >( 2 );
                    lst.Add( new[] { str1,  str2 } );
                    lst.Add( new[] { str2,  str1 } );
                    return (lst);
                }
                default:
                {
                    var result = values.GetPermutationsRecurrent( 0 );

                    var lst = (from r in result
                               select r.ToArray()
                              ).ToList();
                    return (lst);
                }
            }
        }
        public static List< T[] > GetPermutations< T >( IList< T > values )
        {
            switch ( values.Count )
            {
                case 1:
                {
                    var s1 = values[ 0 ];

                    var lst = new List< T[] >( 1 );
                    lst.Add( new[] { s1 } );
                    return (lst);
                }
                case 2:
                {
                    var str1 = values[ 0 ];
                    var str2 = values[ 1 ];

                    var lst = new List< T[] >( 2 );
                    lst.Add( new[] { str1,  str2 } );
                    lst.Add( new[] { str2,  str1 } );
                    return (lst);
                }
                default:
                {
                    var result = values.GetPermutationsRecurrent( 0 );

                    var lst = (from r in result
                               select r.ToArray()
                              ).ToList();
                    return (lst);
                }
            }
        }
        private static IEnumerable< IEnumerable< T > > GetPermutationsRecurrent< T >( this IList< T > values, int startIndex )
        {
            switch ( values.Count - startIndex )
            {
                case 2:
                {
                    var s1 = values[ startIndex     ];
                    var s2 = values[ startIndex + 1 ];

                    yield return (Enumerable.Repeat( s1, 1 ).Concat( Enumerable.Repeat( s2, 1 ) ));
                    yield return (Enumerable.Repeat( s2, 1 ).Concat( Enumerable.Repeat( s1, 1 ) ));
                }
                break;

                case 3:
                {
                    var s1 = values[ startIndex     ];
                    var s2 = values[ startIndex + 1 ];
                    var s3 = values[ startIndex + 2 ];
                    
                    yield return (Enumerable.Repeat( s1, 1 ).Concat( Enumerable.Repeat( s2, 1 ) ).Concat( Enumerable.Repeat( s3, 1 ) ));
                    yield return (Enumerable.Repeat( s1, 1 ).Concat( Enumerable.Repeat( s3, 1 ) ).Concat( Enumerable.Repeat( s2, 1 ) ));

                    yield return (Enumerable.Repeat( s2, 1 ).Concat( Enumerable.Repeat( s1, 1 ) ).Concat( Enumerable.Repeat( s3, 1 ) ));
                    yield return (Enumerable.Repeat( s2, 1 ).Concat( Enumerable.Repeat( s3, 1 ) ).Concat( Enumerable.Repeat( s1, 1 ) ));

                    yield return (Enumerable.Repeat( s3, 1 ).Concat( Enumerable.Repeat( s1, 1 ) ).Concat( Enumerable.Repeat( s2, 1 ) ));
                    yield return (Enumerable.Repeat( s3, 1 ).Concat( Enumerable.Repeat( s2, 1 ) ).Concat( Enumerable.Repeat( s1, 1 ) ));
                }
                break;

                default:
                {
                    var temp = new T[ values.Count ];
                    values.CopyTo( temp, 0 );

                    for ( var i = startIndex; i < temp.Length; i++ )
                    {
                        var t = temp[ startIndex ];
                        var s = temp[ startIndex ] = temp[ i ];
                        temp[ i ] = t;                       

                        var result = temp.GetPermutationsRecurrent( startIndex + 1 );
                        foreach ( var r in result )
                        {
                            yield return (Enumerable.Repeat( s, 1 ).Concat( r ));
                        }
                    }
                }
                break;
            }
        }
    }
}
