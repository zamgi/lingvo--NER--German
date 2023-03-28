using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;
using TP = System.Runtime.TargetedPatchingOptOutAttribute;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class SimplyLinkedList< T > : ICollection< T >, IEnumerable< T >, ICollection, IEnumerable
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Node
        {
            internal Node _Next;
            internal T    _Item;

            public Node Next => _Next;
            public T Value { [M(O.AggressiveInlining), TP(REASON)] get => _Item; [M(O.AggressiveInlining), TP(REASON)] set => _Item = value; }
    
            [M(O.AggressiveInlining), TP(REASON)] public Node( T value ) => _Item = value;
            [M(O.AggressiveInlining), TP(REASON)] public Node( in T value ) => _Item = value;

            [M(O.AggressiveInlining)] internal void Invalidate() => _Next = null;
#if DEBUG
            public override string ToString() => $"{_Item}";
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public struct Enumerator : IEnumerator< T >
        {
            private Node _Node;
            private T _Current;

            internal Enumerator( SimplyLinkedList< T > list )
            {
                _Node = list._Head;
                _Current = default;
            }
            public void Dispose() { }

            public T Current { [M(O.AggressiveInlining), TP(REASON)] get => _Current; }
            object IEnumerator.Current => _Current;
            void IEnumerator.Reset() => throw (new NotImplementedException());
            public void Reset() => throw (new NotImplementedException());

            public bool MoveNext()
            {
                if ( _Node == null )
                {
                    return (false);
                }
                _Current = _Node._Item;
                _Node = _Node._Next;
                return (true);
            }
        }

        private const string REASON = "Performance critical to inline this type of method across NGen image boundaries";

        private Node _Head;
        private Node _Tail;
        private int  _Count;

        public int Count { [TP(REASON)] get => _Count; }
        public Node Head { [TP(REASON)] get => _Head; }
        //public Node Tail { [TP(REASON)] get => _Tail; }
        bool ICollection< T >.IsReadOnly => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => throw (new NotImplementedException());

        public void Add( T value )
        {
            var node = new Node( value );
            if ( _Head == null )
            {
                _Head = node;                
                _Tail = node;
            }
            else
            {
                _Tail._Next = node;
                _Tail = node;
            }
            _Count++;
        }
        public void AddLast( T value ) => Add( value );
        public void AddFirst( T value )
        {
            var node = new Node( value );
            if ( _Head == null )
            {
                _Head = node;
                _Tail = node;
            }
            else
            {
                node._Next = _Head;
                _Head = node;

                //var t = _Head;
                //_Head = node;
                //_Head._Next = t;
            }
            _Count++;
        }
        public void Clear()
        {            
            for ( var next = _Head; next != null; )
            {
                var node = next;
                next = next._Next;
                node.Invalidate();
            }
            _Head = null;
            _Tail = null;
            _Count = 0;
        }
        public bool Contains( T value ) => (Find( value ) != null);
        public Node Find( T value )
        {
            var next = _Head;
            if ( next != null )
            {
                var comparer = EqualityComparer< T >.Default;
                if ( value != null )
                {
                    while ( !comparer.Equals( next._Item, value ) )
                    {
                        next = next._Next;
                        if ( next == _Head )
                        {
                            return (null); 
                        }
                    }
                    return (next);
                }
                while ( next._Item != null )
                {
                    next = next._Next;
                    if ( next == _Head )
                    {
                        return (null);
                    }
                }
                return (next);
            }
            return (null);
        }
        IEnumerator< T > IEnumerable< T >.GetEnumerator() => new Enumerator( this );
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator( this );
        public bool Remove( T value ) => throw (new NotImplementedException());
        public void Remove( Node node ) => throw (new NotImplementedException());        
        public void CopyTo( T[] array, int index )
        {
            if ( array == null )                     throw (new ArgumentNullException( nameof(array) ));
            if ( index < 0 || index > array.Length ) throw (new ArgumentOutOfRangeException( nameof(index), "IndexOutOfRange" ));
            if ( array.Length - index < Count )      throw (new ArgumentException( "Arg_InsufficientSpace" ));

            for ( var next = _Head; next != null; next = next._Next )
            {
                array[ index++ ] = next._Item;
            }
            #region comm
            //var next = _Head;
            //if ( next != null )
            //{
            //    do
            //    {
            //        array[ index++ ] = next._Item;
            //        next = next._Next;
            //    }
            //    while ( next != null );
            //} 
            #endregion
        }
        void ICollection.CopyTo( Array array, int index )
        {
            if ( array == null )                 throw new ArgumentNullException( nameof(array) );
            if ( array.Rank != 1 )               throw (new ArgumentException( "Arg_MultiRank" ));
            if ( array.GetLowerBound( 0 ) != 0 ) throw (new ArgumentException( "Arg_NonZeroLowerBound" ));
            if ( index < 0 )                     throw (new ArgumentOutOfRangeException( nameof(index), "IndexOutOfRange" ));
            if ( array.Length - index < _Count ) throw (new ArgumentException( "Arg_InsufficientSpace" ));

            var array2 = array as T[];
            if ( array2 != null )
            {
                CopyTo( array2, index );
                return;
            }
            Type elementType = array.GetType().GetElementType();
            Type typeFromHandle = typeof(T);
            if ( !elementType.IsAssignableFrom( typeFromHandle ) && !typeFromHandle.IsAssignableFrom( elementType ) ) throw (new ArgumentException( "Invalid_Array_Type" ));

            var array3 = array as object[];
            if ( array3 == null ) throw (new ArgumentException( "Invalid_Array_Type" ));

            try
            {
                for ( var next = _Head; next != null; next = next._Next )
                {
                    array3[ index++ ] = next._Item;
                }
            }
            catch ( ArrayTypeMismatchException )
            {
                throw (new ArgumentException( "Invalid_Array_Type" ));
            }
            #region comm
            //var next = _Head;
            //try
            //{
            //    if ( next != null )
            //    {
            //        do
            //        {
            //            array3[ index++ ] = next._Item;
            //            next = next._Next;
            //        }
            //        while ( next != null );
            //    }
            //}
            //catch ( ArrayTypeMismatchException )
            //{
            //    throw (new ArgumentException( "Invalid_Array_Type" ));
            //} 
            #endregion
        }
        public T[] ToArray()
        {
            var array = new T[ _Count ];
            var index = 0;
            for ( var next = _Head; next != null; next = next._Next )
            {
                array[ index++ ] = next._Item;
            }
            return (array);
        }
        public T[] ToArrayReverse()
        {
            var array = new T[ _Count ];
            var index = _Count - 1;
            for ( var next = _Head; next != null; next = next._Next )
            {
                array[ index-- ] = next._Item;
            }
            return (array);
        }
#if DEBUG
        public override string ToString() => $"count: {Count}";
#endif
    }
}
