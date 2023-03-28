using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Lingvo.NER.Rules.NerPostMerging;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class ConcurrentFactory : IDisposable
    {
        private readonly SemaphoreSlim                   _Semaphore;
        private readonly ConcurrentStack< NerProcessor > _Stack;
        private readonly NerProcessorConfig              _NerProcessorConfig;

        public ConcurrentFactory( NerProcessorConfig config, int instanceCount )
        {
            if ( instanceCount <= 0 ) throw (new ArgumentException( "instanceCount" ));
            if ( config == null     ) throw (new ArgumentNullException( "config" ));

            _NerProcessorConfig = config;
            _Semaphore = new SemaphoreSlim( instanceCount, instanceCount );
            _Stack     = new ConcurrentStack< NerProcessor >();
            for ( int i = 0; i < instanceCount; i++ )
            {
                _Stack.Push( new NerProcessor( config ) );
            }
        }
        public void Dispose()
        {
            foreach ( var worker in _Stack )
            {
                worker.Dispose();
            }
            _Stack.Clear();
            _Semaphore.Dispose();
            _NerProcessorConfig.Dispose();
        }
        public void Dispose_NoThrow()
        {
            try
            {
                Dispose();
            }
            catch
            {
                ;
            }
        }

        public async Task< word_t[] > Run_UseSimpleSentsAllocate_v1( string text )
        {
            await _Semaphore.WaitAsync().ConfigureAwait( false );

            var worker = default(NerProcessor);
            try
            {
                worker = Pop( _Stack );
                
                var result = worker.Run_UseSimpleSentsAllocate_v1( text ).ToArray();
                return (result);
            }
            finally
            {
                if ( worker != null )
                {
                    _Stack.Push( worker );
                }
                _Semaphore.Release();
            }

            throw (new InvalidOperationException( $"{this.GetType().Name}: nothing to return (fusking)" ));
        }
        public async Task< (word_t[] nerWords, NerUnitedEntity[] nerUnitedEntities, int relevanceRanking) > Run_UseSimpleSentsAllocate_v2( string text )
        {
            await _Semaphore.WaitAsync().ConfigureAwait( false );

            var worker = default(NerProcessor);
            try
            {
                worker = Pop( _Stack );
                
                var t = worker.Run_UseSimpleSentsAllocate_v2( text );
                var result = (t.nerWords.ToArray(), t.nerUnitedEntities.ToArray(), t.relevanceRanking);
                return (result);
            }
            finally
            {
                if ( worker != null )
                {
                    _Stack.Push( worker );
                }
                _Semaphore.Release();
            }

            throw (new InvalidOperationException( $"{this.GetType().Name}: nothing to return (fusking)" ));
        }
        public async Task< word_t[] > Run_UseSimpleSentsAllocate_Address( string text )
        {
            await _Semaphore.WaitAsync().ConfigureAwait( false );

            var worker = default(NerProcessor);
            try
            {
                worker = Pop( _Stack );
                
                var result = worker.Run_UseSimpleSentsAllocate_Address( text ).ToArray();
                return (result);
            }
            finally
            {
                if ( worker != null )
                {
                    _Stack.Push( worker );
                }
                _Semaphore.Release();
            }

            throw (new InvalidOperationException( $"{this.GetType().Name}: nothing to return (fusking)" ));
        }

        private static T Pop< T >( ConcurrentStack< T > stack )
        {
            for ( T t; stack.TryPop( out t ); )
            {
                return (t);
            }
            return (default(T));
        }
    }
}
