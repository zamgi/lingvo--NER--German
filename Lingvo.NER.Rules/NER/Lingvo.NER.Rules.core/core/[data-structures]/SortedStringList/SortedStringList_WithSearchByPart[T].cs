using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class SortedStringList_WithValueAndSearchByPart : IEnumerable< (string key, string value) >
	{
        private const int MAX_CAPACITY_THRESHOLD = 0x7FFFFFFF /*int.MaxValue*/ - 0x400 * 0x400 /*1MB*/; /* => 2146435071 == 0x7fefffff*/
#if DEBUG
        public override string ToString() => Count.ToString();
#endif
        private static readonly (string key, string value)[] EMPTY_ARRAY = new (string key, string value)[ 0 ];

        private (string key, string value)[] _Array;
		private int _Size;

		public int Capacity
		{
			get => _Array.Length;
			private set
			{
                if ( value != _Array.Length )
				{
                    if ( 0 < value )
					{
                        var destinationArray = new (string key, string value)[ value ];
                        if ( 0 < _Size )
						{
                            Array.Copy( _Array, 0, destinationArray, 0, _Size );
						}
                        _Array = destinationArray;
					}
                    else
                    {
                        _Array = EMPTY_ARRAY;
                    }
				}
			}
		}
		public int Count { [M(O.AggressiveInlining)] get => _Size; }
        
        public SortedStringList_WithValueAndSearchByPart( int capacity = 0 )
		{
            _Array = (capacity == 0) ? EMPTY_ARRAY : new (string key, string value)[ capacity ];
            //_Size  = 0;
		}
	
        public bool TryAdd( string key, string value )
        {
            var index = InternalBinarySearch( key );
            if ( 0 <= index )
            {
                return (false);
            }
            Insert( ~index, key, value );
            return (true);
        }
        public bool TryAdd( string key )
        {
            var index = InternalBinarySearch( key );
            if ( 0 <= index )
            {
                return (false);
            }
            Insert( ~index, key, null );
            return (true);
        }

        public bool TryGetValueByPart( string key, int startIndex, out string existsValue ) => InternalBinarySearchByPart( key, startIndex, out existsValue );
        public bool TryGetValueByPart( string key, int startIndex ) => InternalBinarySearchByPart_2( key, startIndex );
        public bool TryGetValueByPart( string key ) => InternalBinarySearchByPart_2( key, 0 );

        private void Insert( int index, string key, string value )
		{
            if ( _Size == _Array.Length )
            {
                EnsureCapacity( _Size + 1 );
            }
            if ( index < _Size )
            {
                Array.Copy( _Array, index, _Array, index + 1, _Size - index );
            }
            _Array[ index ] = (key, value);
			_Size++;
		}
        private void EnsureCapacity( int min )
		{
            int capacity;
            switch ( _Array.Length )
            {
                case 0:  capacity = 1; break;
                case 1:  capacity = 16; break;
                default: capacity = _Array.Length * 2; break;
            }
            if ( MAX_CAPACITY_THRESHOLD < capacity )
            {
                capacity = MAX_CAPACITY_THRESHOLD;
            }
            if ( capacity < min )
            {
                capacity = min;
            }
			Capacity = capacity;
		}

        #region comm.
        /*public void AddStrict( string key )
		{
            var index = InternalBinarySearch( key ); 
            if ( 0 <= index )
			{
                throw (new ArgumentException( index.ToString(), nameof(index) ));
			}
            Insert( ~index, key );
		}*/
        /*public void Clear()
		{
            System.Array.Clear( _Array, 0, _Size );
			_Size = 0;
		}*/
        //public bool ContainsKey( string key ) => (0 <= InternalBinarySearch( key ));
        //public bool ContainsKeyByPart( string key, int startIndex ) => (0 <= InternalBinarySearchByPart( key, startIndex ));
        /*public bool ContainsKeyByPart( string key )
        {
            for ( var startIndex = 0; ; )
            {
                var index = InternalBinarySearchByPart( key, startIndex );
                if ( 0 <= index )
                {
                    ref var t = ref _Array[ index ];
                    startIndex += t.key.Length;

                    if ( key.Length <= startIndex )
                    {
                        return (true);
                    }
                }
                return (false);
            }
        }*/

        /*public IEnumerable< string > StartsWith( string key )
        {
            var index = InternalBinarySearch( key );
            if ( index < 0 )
            {
                yield break;
            }

            var startIndex = index - 1;
            for ( ; 0 <= startIndex; startIndex-- )
            {
                var existsKey = _Array[ startIndex ];
                int d;
                if ( key.Length <= existsKey.Length )
                {
                    d = string.CompareOrdinal( existsKey, 0, key, 0, key.Length );
                }
                else
                {
                    d = string.CompareOrdinal( existsKey, key );
                }
                if ( d != 0 )
                {
                    startIndex++;
                    break;
                }
            }

            for ( ; startIndex <= index; startIndex++ )
            {
                yield return (_Array[ startIndex ]);
            }

            for ( ; startIndex < _Size; startIndex++ )
            {
                var existsKey = _Array[ startIndex ];
                int d;
                if ( key.Length <= existsKey.Length )
                {
                    d = string.CompareOrdinal( existsKey, 0, key, 0, key.Length );
                }
                else
                {
                    d = string.CompareOrdinal( existsKey, key );
                }
                if ( d != 0 )
                {
                    break;
                }

                yield return (existsKey);
            }
        }*/

        /*public int IndexOfKey( string key )
		{
            int index = InternalBinarySearch( key );
            if ( index < 0 )
            {
                return (-1);
            }
			return (index);
		}
        public int IndexOfKeyByPart( string key, int startIndex )
        {
            int index = InternalBinarySearchByPart( key, startIndex );
            if ( index < 0 )
            {
                return (-1);
            }
            return (index);
        }*/
        /*public int IndexOfKeyCore( string key )
        {
            int index = InternalBinarySearch( key );
            return (index);
        }
        public int IndexOfKeyCoreByPart( string key, int startIndex )
        {
            int index = InternalBinarySearchByPart( key, startIndex );
            return (index);
        }*/

        /*public bool TryGetValue( string key, out string existsValue )
		{
            var index = InternalBinarySearch( key );
            if ( 0 <= index )
			{
                ref var t = ref _Array[ index ];
                existsValue = t.value;
				return (true);
			}
			existsValue = default;
			return (false);
		}*/

        /*public void RemoveAt( int index )
		{
            if ( index < 0 || _Size <= index )
			{
				throw (new ArgumentOutOfRangeException( nameof(index) ));
			}
			_Size--;
            if ( index < _Size )
			{
                System.Array.Copy( _Array, index + 1, _Array, index, _Size - index );
			}
			_Array[ _Size ] = default(string);
		}
		public bool Remove( string key )
		{
            int index = InternalBinarySearch( key );
            if ( 0 <= index )
            {
                RemoveAt( index );
            }
            return (0 <= index);
		}*/
        /*public void TrimExcess()
		{
            int size = (int) ((double) _Array.Length * 0.9);
            if ( _Size < size )
            {
                Capacity = _Size;
            }
		}
        public void Trim()
        {
            Capacity = _Size;
        }*/
        #endregion

        [M(O.AggressiveInlining)] private int InternalBinarySearch( string searchKey )
        {
            var i = 0;
            for ( var endIndex = _Size - 1; i <= endIndex; )
            {
                var middleIndex = i + ((endIndex - i) >> 1);
                ref readonly var t = ref _Array[ middleIndex ];

                var d = string.CompareOrdinal( t.key, searchKey );
                if ( d == 0 )
                {
                    return (middleIndex);
                }

                if ( d < 0 )
                {
                    i = middleIndex + 1;
                }
                else
                {
                    endIndex = middleIndex - 1;
                }
            }
            return (~i);
        }
        [M(O.AggressiveInlining)] private int InternalBinarySearchByPart( string searchKey, int startIndex )
        {
            var i = 0;
            for ( var endIndex = _Size - 1; i <= endIndex; )
            {
                var middleIndex = i + ((endIndex - i) >> 1);
                ref readonly var t = ref _Array[ middleIndex ];

                var d = string.CompareOrdinal( t.key, 0, searchKey, startIndex, t.key.Length );
                
                #region [.only for suffix this.]
                /*int d;
                if ( value.Length <= key.Length )
                {
                    d = string.CompareOrdinal( key, 0, value, startIndex, value.Length );
                }
                else
                {
                    d = string.CompareOrdinal( key, 0, value, startIndex, key.Length );
                }*/
                #endregion

                if ( d == 0 )
                {
                    return (middleIndex);
                }

                if ( d < 0 )
                {
                    i = middleIndex + 1;
                }
                else
                {
                    endIndex = middleIndex - 1;
                }
            }
            return (~i);
        }
        [M(O.AggressiveInlining)] private bool InternalBinarySearchByPart( string searchKey, int startIndex, out string existsValue )
        {
            var i = 0;
            for ( var endIndex = _Size - 1; i <= endIndex; )
            {
                var middleIndex = i + ((endIndex - i) >> 1);
                ref readonly var t = ref _Array[ middleIndex ];

                var d = string.CompareOrdinal( t.key, 0, searchKey, startIndex, t.key.Length );
                if ( d == 0 )
                {
                    var t_value    = t.value;
                    var key_length = t.key.Length;
                    while ( key_length < searchKey.Length )
                    {
                        if ( endIndex < ++middleIndex )
                        {
                            break;
                        }
                        ref readonly var t0 = ref _Array[ middleIndex ];
                        d = string.CompareOrdinal( t0.key, 0, searchKey, startIndex, t0.key.Length );
                        if ( d == 0 )
                        {
                            t_value    = t0.value;
                            key_length = t0.key.Length;
                        }
                        else
                        {
                            break;
                        }
                    }

                    existsValue = t_value;
                    return (true);
                }

                if ( d < 0 )
                {
                    i = middleIndex + 1;
                }
                else
                {
                    endIndex = middleIndex - 1;
                }
            }
            existsValue = default;
            return (false);
        }
        [M(O.AggressiveInlining)] private bool InternalBinarySearchByPart_2( string searchKey, int startIndex ) //, out string existsValue )
        {
            var i = 0;
            for ( var endIndex = _Size - 1; i <= endIndex; )
            {
                var middleIndex = i + ((endIndex - i) >> 1);
                ref readonly var t = ref _Array[ middleIndex ];

                var d = string.CompareOrdinal( t.key, 0, searchKey, startIndex, t.key.Length );
                if ( d == 0 )
                {
                    //---var t_value    = t.value;
                    var key_length = t.key.Length;
                    while ( key_length < searchKey.Length )
                    {
                        if ( endIndex < ++middleIndex )
                        {
                            break;
                        }
                        ref readonly var t0 = ref _Array[ middleIndex ];
                        d = string.CompareOrdinal( t0.key, 0, searchKey, startIndex, t0.key.Length );
                        if ( d == 0 )
                        {
                            //---t_value    = t0.value;
                            key_length = t0.key.Length;
                        }
                        else
                        {
                            break;
                        }
                    }

                    //---existsValue = t_value;
                    return (true);
                }

                if ( d < 0 )
                {
                    i = middleIndex + 1;
                }
                else
                {
                    endIndex = middleIndex - 1;
                }
            }
            //---existsValue = default;
            return (false);
        }

        #region [.IEnumerable< string >.]
        public IEnumerator< (string key, string value) > GetEnumerator()
        {
            for ( int i = 0; i < _Size; i++ )
            {
                yield return (_Array[ i ]);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}
