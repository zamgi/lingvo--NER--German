using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// 
    /// </summary>
    internal interface IObjectHolder< T > : IDisposable
    {
        T Value { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    internal class ObjectPool< T > : IDisposable
        where T : class
    {
        private SemaphoreSlim        _Semaphore;
        private ConcurrentStack< T > _Stack;

        public ObjectPool( int objectInstanceCount, Func< T > objectConstructorFunc )
        {
            if ( objectInstanceCount <= 0 )      throw (new ArgumentException( nameof(objectInstanceCount) ));
            if ( objectConstructorFunc == null ) throw (new ArgumentNullException( nameof(objectConstructorFunc) ));
            //-----------------------------------------------//

            _Semaphore = new SemaphoreSlim( objectInstanceCount, objectInstanceCount );
            _Stack     = new ConcurrentStack< T >();
            for ( int i = 0; i < objectInstanceCount; i++ )
            {
                _Stack.Push( objectConstructorFunc() );
            }
        }
        public void Dispose()
        {
            if ( _Semaphore != null )
            {
                _Semaphore.Dispose();
                _Semaphore = null;

                DisposeInternal();
            }            
        }

        protected virtual void DisposeInternal() { }
        protected IReadOnlyCollection< T > GetObjects() => _Stack.ToArray();

        /// <summary>
        /// 
        /// </summary>
        private struct Releaser : IObjectHolder< T >, IDisposable
        {
            private ObjectPool< T > _ObjectPool;
            
            [M(O.AggressiveInlining)] public Releaser( ObjectPool< T > objectPool, T t ) => (_ObjectPool, Value) = (objectPool, t);

            public T Value { [M(O.AggressiveInlining)] get; [M(O.AggressiveInlining)] private set; }

            public void Dispose()
            {
                if ( Value != null )
                {
                    _ObjectPool.Release( Value );
                    Value = null;
                }
            }
        }

        [M(O.AggressiveInlining)] public T Get()
        {
            _Semaphore.Wait();

            for( ; ; )
            {
                if ( _Stack.TryPop( out var t ) )
                {
                    return (t);
                }
            }
        }
        [M(O.AggressiveInlining)] public async Task< T > GetAsync()
        {
            await _Semaphore.WaitAsync();

            for (; ; )
            {
                if ( _Stack.TryPop( out var t ) )
                {
                    return (t);
                }
            }
        }
        [M(O.AggressiveInlining)] public void Release( T t )
        {
            Debug.Assert( t != null );
            //if ( t != null )
            //{
                _Stack.Push( t );
                _Semaphore.Release();
            //}            
        }

        public IObjectHolder< T > GetHolder()
        {
            _Semaphore.Wait();

            for( ; ; )
            {
                if ( _Stack.TryPop( out var t ) )
                {
                    return (new Releaser( this, t ));
                }
            }
        }
        public async Task< IObjectHolder< T > > GetHolderAsync()
        {
            await _Semaphore.WaitAsync();

            for (; ; )
            {
                if ( _Stack.TryPop( out var t ) )
                {
                    return (new Releaser( this, t ));
                }
            }
        }

        public int CurrentCount_Semaphore => _Semaphore.CurrentCount;
        public int CurrentCount_Stack     => _Stack.Count;
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class ObjectPoolDisposable< T > : ObjectPool< T >, IDisposable
        where T : class, IDisposable
    {
        public ObjectPoolDisposable( int objectInstanceCount, Func< T > objectConstructorFunc ) 
            : base( objectInstanceCount, objectConstructorFunc ) { }

        protected override void DisposeInternal()
        {
            foreach ( var t in base.GetObjects() )
            {
                t.Dispose();
            }
        }
    }
}