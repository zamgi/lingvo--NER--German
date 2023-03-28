using System.Diagnostics;

namespace System.Collections.Generic
{
    /// <summary>
    /// Useful in number of places that return an empty byte array to avoid unnecessary memory allocation.
    /// </summary>
    internal static class EmptyArray< T >
    {
        public static readonly T[] Value = new T[ 0 ];
    }

    /// <summary>
    /// Helper type for avoiding allocations while building arrays.
    /// </summary>
    internal struct ArrayBuilder< T >
    {
        private const int DEFAULT_CAPACITY = 4;
        private const int MAX_CORECLR_ARRAYLENGTH = 0x7fefffff; // For byte arrays the limit is slightly larger

        private T[] _Array; // Starts out null, initialized on first Add.
        private int _Count; // Number of items into _array we're using.

        /// <summary>
        /// Initializes the <see cref="ArrayBuilder{T}"/> with a specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity of the array to allocate.</param>
        public ArrayBuilder( int capacity ) : this()
        {
#if DEBUG
            Debug.Assert( 0 <= capacity ); 
#endif
            if ( 0 < capacity )
            {
                _Array = new T[ capacity ];
            }
        }

        /// <summary>
        /// Gets the number of items this instance can store without re-allocating,
        /// or 0 if the backing array is <c>null</c>.
        /// </summary>
        public int Capacity => ((_Array != null) ? _Array.Length : 0);

        /// <summary>
        /// Gets the number of items in the array currently in use.
        /// </summary>
        public int Count => _Count;

        /// <summary>
        /// Gets or sets the item at a certain index in the array.
        /// </summary>
        /// <param name="index">The index into the array.</param>
        public T this[ int index ]
        {
            get
            {
#if DEBUG
                Debug.Assert( index >= 0 && index < _Count ); 
#endif
                return _Array[ index ];
            }
            set
            {
#if DEBUG
                Debug.Assert( index >= 0 && index < _Count ); 
#endif
                _Array[ index ] = value;
            }
        }

        /// <summary>
        /// Adds an item to the backing array, resizing it if necessary.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add( T item )
        {
            if ( _Count == Capacity )
            {
                EnsureCapacity( _Count + 1 );
            }

            UncheckedAdd( item );
        }

        /// <summary>
        /// Returns an array with equivalent contents as this builder.
        /// </summary>
        /// <remarks>
        /// Do not call this method twice on the same builder.
        /// </remarks>
        public T[] ToArray()
        {
            if ( _Count == 0 )
            {
                return (EmptyArray< T >.Value);
            }
#if DEBUG
            Debug.Assert( _Array != null ); // Nonzero _count should imply this  
#endif
            T[] result = _Array;
            if ( _Count < result.Length )
            {
                // Avoid a bit of overhead (method call, some branches, extra codegen)
                // which would be incurred by using Array.Resize
                result = new T[ _Count ];
                Array.Copy( _Array, 0, result, 0, _Count );
            }
#if DEBUG
            // Try to prevent callers from using the ArrayBuilder after ToArray, if _count != 0.
            _Count = -1;
            _Array = null;
#endif
            return (result);
        }

        /// <summary>
        /// Adds an item to the backing array, without checking if there is room.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <remarks>
        /// Use this method if you know there is enough space in the <see cref="ArrayBuilder{T}"/>
        /// for another item, and you are writing performance-sensitive code.
        /// </remarks>
        public void UncheckedAdd( T item )
        {
#if DEBUG
            Debug.Assert( _Count < Capacity ); 
#endif
            _Array[ _Count++ ] = item;
        }

        private void EnsureCapacity( int minimum )
        {
#if DEBUG
            Debug.Assert( Capacity < minimum ); 
#endif
            int capacity = Capacity;
            int nextCapacity = (capacity == 0) ? DEFAULT_CAPACITY : 2 * capacity;

            if ( (uint) MAX_CORECLR_ARRAYLENGTH < (uint) nextCapacity )
            {
                nextCapacity = Math.Max( capacity + 1, MAX_CORECLR_ARRAYLENGTH );
            }

            nextCapacity = Math.Max( nextCapacity, minimum );

            T[] next = new T[ nextCapacity ];
            if ( 0 < _Count )
            {
                Array.Copy( _Array, 0, next, 0, _Count );
            }
            _Array = next;
        }
    }
}