using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Helper type for building dynamically-sized arrays while minimizing allocations and copying.
    /// </summary>
    internal struct LargeArrayBuilder< T >
    {
        private const int STARTING_CAPACITY = 4;
        private const int RESIZE_LIMIT = 32;

        private readonly int _MaxCapacity;    // The maximum capacity this builder can have.
        private T[] _First;                   // The first buffer we store items in. Resized until ResizeLimit.
        private ArrayBuilder< T[] > _Buffers; // After ResizeLimit * 2, we store previous buffers we've filled out here.
        private T[] _Current;                 // Current buffer we're reading into. If _count <= ResizeLimit, this is _first.
        private int _Index;                   // Index into the current buffer.
        private int _Count;                   // Count of all of the items in this builder.

        /// <summary>
        /// Constructs a new builder.
        /// </summary>
        /// <param name="initialize">Pass <c>true</c>.</param>
        public LargeArrayBuilder( bool initialize ) : this( maxCapacity: int.MaxValue )
        {
            // This is a workaround for C# not having parameterless struct constructors yet.
            // Once it gets them, replace this with a parameterless constructor.
#if DEBUG
            Debug.Assert( initialize ); 
#endif
        }

        /// <summary>
        /// Constructs a new builder with the specified maximum capacity.
        /// </summary>
        /// <param name="maxCapacity">The maximum capacity this builder can have.</param>
        /// <remarks>
        /// Do not add more than <paramref name="maxCapacity"/> items to this builder.
        /// </remarks>
        public LargeArrayBuilder( int maxCapacity )
        {
#if DEBUG
            Debug.Assert( 0 <= maxCapacity ); 
#endif
            _First = _Current = EmptyArray< T >.Value;
            _Buffers = default;
            _Index = 0;
            _Count = 0;
            _MaxCapacity = maxCapacity;
        }

        /// <summary>
        /// Gets the number of items added to the builder.
        /// </summary>
        public int Count => _Count;

        /// <summary>
        /// Adds an item to this builder.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <remarks>
        /// Use <see cref="Add"/> if adding to the builder is a bottleneck for your use case.
        /// Otherwise, use <see cref="SlowAdd"/>.
        /// </remarks>
        //---[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add( T item )
        {
#if DEBUG
            Debug.Assert( _Count < _MaxCapacity ); 
#endif
            if ( _Index == _Current.Length )
            {
                AllocateBuffer();
            }

            _Current[ _Index++ ] = item;
            _Count++;
        }

        /// <summary>
        /// Adds a range of items to this builder.
        /// </summary>
        /// <param name="items">The sequence to add.</param>
        /// <remarks>
        /// It is the caller's responsibility to ensure that adding <paramref name="items"/>
        /// does not cause the builder to exceed its maximum capacity.
        /// </remarks>
        public void AddRange( IEnumerable< T > items )
        {
#if DEBUG
            Debug.Assert( items != null ); 
#endif
            using ( var enumerator = items.GetEnumerator() )
            {
                T[] destination = _Current;
                int index = _Index;

                // Continuously read in items from the enumerator, updating _count
                // and _index when we run out of space.

                while ( enumerator.MoveNext() )
                {
                    if ( index == destination.Length )
                    {
                        // No more space in this buffer. Resize.
                        _Count += index - _Index;
                        _Index = index;
                        AllocateBuffer();
                        destination = _Current;
                        index = _Index; // May have been reset to 0
                    }

                    destination[ index++ ] = enumerator.Current;
                }

                // Final update to _count and _index.
                _Count += index - _Index;
                _Index = index;
            }
        }

        /// <summary>
        /// Copies the contents of this builder to the specified array.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The index in <see cref="array"/> to start copying.</param>
        /// <param name="count">The number of items to copy.</param>
        public void CopyTo( T[] array, int arrayIndex, int count )
        {
#if DEBUG
            Debug.Assert( 0 <= arrayIndex );
            Debug.Assert( 0 <= count && count <= Count );
            Debug.Assert( count <= (array != null ? array.Length : 0) - arrayIndex ); 
#endif
            for ( int i = -1; 0 < count; i++ )
            {
                // Find the buffer we're copying from.
                T[] buffer = ((i < 0) ? _First : ((i < _Buffers.Count) ? _Buffers[ i ] : _Current));

                // Copy until we satisfy count, or we reach the end of the buffer.
                int toCopy = Math.Min( count, buffer.Length );
                Array.Copy( buffer, 0, array, arrayIndex, toCopy );

                // Increment variables to that position.
                count -= toCopy;
                arrayIndex += toCopy;
            }
        }

        /// <summary>
        /// Adds an item to this builder.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <remarks>
        /// Use <see cref="Add"/> if adding to the builder is a bottleneck for your use case.
        /// Otherwise, use <see cref="SlowAdd"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SlowAdd( T item ) => Add( item );

        /// <summary>
        /// Returns an array representation of this builder.
        /// </summary>
        public T[] ToArray()
        {
            if ( _Count == _First.Length )
            {
                // No resizing to do.
                return _First;
            }

            var array = new T[ _Count ];
            CopyTo( array, 0, _Count );
            return array;
        }

        private void AllocateBuffer()
        {
            // - On the first few adds, simply resize _first.
            // - When we pass ResizeLimit, allocate ResizeLimit elements for _current
            //   and start reading into _current. Set _index to 0.
            // - When _current runs out of space, add it to _buffers and repeat the
            //   above step, except with _current.Length * 2.
            // - Make sure we never pass _maxCapacity in all of the above steps.
#if DEBUG
            Debug.Assert( (uint) _Count < (uint) _MaxCapacity );
            Debug.Assert( _Index == _Current.Length, "AllocateBuffer() was called, but there's more space." ); 
#endif
            // If _count is int.MinValue, we want to go down the other path which will raise an exception.
            if ( (uint) _Count < (uint) RESIZE_LIMIT )
            {
                // We haven't passed ResizeLimit. Resize _first, copying over the previous items.
#if DEBUG
                Debug.Assert( _Current == _First && _Count == _First.Length ); 
#endif
                int nextCapacity = Math.Min( ((_Count == 0) ? STARTING_CAPACITY : _Count * 2), _MaxCapacity );

                _Current = new T[ nextCapacity ];
                Array.Copy( _First, 0, _Current, 0, _Count );
                _First = _Current;
            }
            else
            {
#if DEBUG
                Debug.Assert( RESIZE_LIMIT < _MaxCapacity );
                Debug.Assert( _Count == RESIZE_LIMIT ^ _Current != _First ); 
#endif
                int nextCapacity;
                if ( _Count == RESIZE_LIMIT )
                {
                    nextCapacity = RESIZE_LIMIT;
                }
                else
                {
                    // Example scenario: Let's say _count == 256.
                    // Then our buffers look like this: | 32 | 32 | 64 | 128 |
                    // As you can see, our count will be just double the last buffer.
                    // Now, say _maxCapacity is 500. We will find the right amount to allocate by
                    // doing min(256, 500 - 256). The lhs represents double the last buffer,
                    // the rhs the limit minus the amount we've already allocated.
#if DEBUG
                    Debug.Assert( RESIZE_LIMIT * 2 <= _Count );
                    Debug.Assert( _Count == _Current.Length * 2 ); 
#endif
                    _Buffers.Add( _Current );
                    nextCapacity = Math.Min( _Count, _MaxCapacity - _Count );
                }

                _Current = new T[ nextCapacity ];
                _Index = 0;
            }
        }
    }
}