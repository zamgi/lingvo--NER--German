using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Lingvo.NER.Rules.NerPostMerging;
using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.WebService
{
    /// <summary>
    /// 
    /// </summary>
    public interface INerProcessorHolder : IDisposable
    {
        bool Run( string text, out IReadOnlyCollection< NerOutputType > nerOutputTypes, out int relevanceRanking );
        bool Run( string text, out IReadOnlyList< word_t > nerWords, out int relevanceRanking );
        bool Run( string text, out IReadOnlyList< word_t > nerWords, out IReadOnlyList< NerUnitedEntity > nerUnitedEntities, out int relevanceRanking );
    }

    /// <summary>
    /// 
    /// </summary>
    public interface INerProcessorFactory
    {
        //(List< word_t > nerWords, List< NerUnitedEntity_v2 > nerUnitedEntities) Run_UseSimpleSentsAllocate_v2( string text );
        //IList< word_t > Run( string text );
        bool Run( string text, out IReadOnlyCollection< NerOutputType > nerOutputTypes, out int relevanceRanking );
        bool Run( string text, out IReadOnlyList< word_t > nerWords, out int relevanceRanking );
        bool Run( string text, out IReadOnlyList< word_t > nerWords, out IReadOnlyList< NerUnitedEntity > nerUnitedEntities, out int relevanceRanking );
        INerProcessorHolder GetNerProcessorHolder();
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class NerProcessorFactory : INerProcessorFactory, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class NerOutputType_Comparer : IComparer< NerOutputType >
        {
            public static NerOutputType_Comparer Inst { get; } = new NerOutputType_Comparer();
            private NerOutputType_Comparer() { }
            public int Compare( NerOutputType x, NerOutputType y ) => string.Compare( x.ToText(), y.ToText() );
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class Tuple : IDisposable
        {
            public NerProcessor NerProcessor;
            public SortedSet< NerOutputType > NerOutputTypeSet;
            public List< word_t >             NerWords;
            public List< NerUnitedEntity >    NerUnitedEntities;
            public Tuple( NerProcessor nerProcessor )
            {
                NerProcessor      = nerProcessor;
                NerOutputTypeSet  = new SortedSet< NerOutputType >( NerOutputType_Comparer.Inst );
                NerWords          = new List< word_t >();
                NerUnitedEntities = new List< NerUnitedEntity >();
            }
            public void Dispose() => NerProcessor.Dispose();            
        }
        /// <summary>
        /// 
        /// </summary>
        private struct NerProcessorHolder : INerProcessorHolder
        {
            #region [.ctor().]
            private Tuple _Tuple;
            private ObjectPoolDisposable< Tuple > _NerProcessorPool;
            public NerProcessorHolder( Tuple t, ObjectPoolDisposable< Tuple > nerProcessorPool )
            {
                _Tuple = t;
                _NerProcessorPool = nerProcessorPool;
            }
            public void Dispose()
            {
                if ( _Tuple != null )
                {
                    _NerProcessorPool.Release( _Tuple );
                    _Tuple = null;
                }
            }
            #endregion

            public bool Run( string text, out IReadOnlyCollection< NerOutputType > nerOutputTypes, out int relevanceRanking )
            {
                var (_nerWords, _nerUnitedEntities, _relevanceRanking) = _Tuple.NerProcessor.Run_UseSimpleSentsAllocate_v2( text );
                if ( _nerWords.Count != 0 )
                {
                    var set = _Tuple.NerOutputTypeSet;
                    set.Clear();
                    foreach ( var word in _nerWords )
                    {
                        set.Add( word.nerOutputType );
                    }
                    nerOutputTypes   = set;
                    relevanceRanking = _relevanceRanking;
                    return (true);
                }
                nerOutputTypes   = default;
                relevanceRanking = default;
                return (false);
            }
            public bool Run( string text, out IReadOnlyList< word_t > nerWords, out int relevanceRanking )
            {
                var (_nerWords, _nerUnitedEntities, _relevanceRanking ) = _Tuple.NerProcessor.Run_UseSimpleSentsAllocate_v2( text );
                if ( _nerWords.Count != 0 )
                {
                    var lst = _Tuple.NerWords;
                    lst.Clear();
                    foreach ( var w in _nerWords )
                    {
                        lst.Add( w );
                    }

                    nerWords         = lst;
                    relevanceRanking = _relevanceRanking;
                    return (true);
                }
                nerWords         = default;
                relevanceRanking = default;
                return (false);
            }
            public bool Run( string text, out IReadOnlyList< word_t > nerWords, out IReadOnlyList< NerUnitedEntity > nerUnitedEntities, out int relevanceRanking )
            {
                var (_nerWords, _nerUnitedEntities, _relevanceRanking ) = _Tuple.NerProcessor.Run_UseSimpleSentsAllocate_v2( text );
                if ( _nerWords.Count != 0 )
                {
                    var lst = _Tuple.NerWords;
                    lst.Clear();
                    foreach ( var w in _nerWords )
                    {
                        lst.Add( w );
                    }

                    var lst2 = _Tuple.NerUnitedEntities;
                    lst2.Clear();
                    foreach ( var nue in _nerUnitedEntities )
                    {
                        lst2.Add( nue );
                    }

                    nerWords          = lst;
                    nerUnitedEntities = lst2;
                    relevanceRanking  = _relevanceRanking;
                    return (true);
                }
                nerWords          = default;
                nerUnitedEntities = default;
                relevanceRanking  = default;
                return (false);
            }
        }

        #region [.ctor().]
        private ObjectPoolDisposable< Tuple > _NerProcessorPool;
        public NerProcessorFactory( NerProcessorConfig config, int objectInstanceCount )
            => _NerProcessorPool = new ObjectPoolDisposable< Tuple >( objectInstanceCount, () => new Tuple( new NerProcessor( config ) ) );
        public void Dispose() => _NerProcessorPool.Dispose();
        #endregion

        public bool Run( string text, out IReadOnlyCollection< NerOutputType > nerOutputTypes, out int relevanceRanking )
        {
            var t = _NerProcessorPool.Get();
            try
            {
                var (nerWords, nerUnitedEntities, _relevanceRanking ) = t.NerProcessor.Run_UseSimpleSentsAllocate_v2( text );
                if ( nerWords.Count != 0 )
                {
                    var set = t.NerOutputTypeSet;
                    set.Clear();
                    foreach ( var word in nerWords )
                    {
                        set.Add( word.nerOutputType );
                    }

                    var array = new NerOutputType[ set.Count ];
                    var i = 0;
                    foreach ( var nerOutputType in set )
                    {
                        array[ i++ ] = nerOutputType;
                    }
                    nerOutputTypes   = array;
                    relevanceRanking = _relevanceRanking;
                    return (true);
                }
                nerOutputTypes   = default;
                relevanceRanking = default;
                return (false);
            }
            finally
            {
                _NerProcessorPool.Release( t );
            }
        }
        public bool Run( string text, out IReadOnlyList< word_t > nerWords, out int relevanceRanking )
        {
            var t = _NerProcessorPool.Get();
            try
            {
                var (_nerWords, _nerUnitedEntities, _relevanceRanking ) = t.NerProcessor.Run_UseSimpleSentsAllocate_v2( text );
                if ( _nerWords.Count != 0 )
                {
                    var array = new word_t[ _nerWords.Count ];
                    var i = 0;
                    foreach ( var w in _nerWords )
                    {
                        array[ i++ ] = w;
                    }
                    nerWords         = array;
                    relevanceRanking = _relevanceRanking;
                    return (true);
                }
                nerWords         = default;
                relevanceRanking = default;
                return (false);
            }
            finally
            {
                _NerProcessorPool.Release( t );
            }
        }
        public bool Run( string text, out IReadOnlyList< word_t > nerWords, out IReadOnlyList< NerUnitedEntity > nerUnitedEntities, out int relevanceRanking )
        {
            var t = _NerProcessorPool.Get();
            try
            {
                var (_nerWords, _nerUnitedEntities, _relevanceRanking ) = t.NerProcessor.Run_UseSimpleSentsAllocate_v2( text );
                if ( _nerWords.Count != 0 )
                {
                    var array = new word_t[ _nerWords.Count ];
                    var i = 0;
                    foreach ( var w in _nerWords )
                    {
                        array[ i++ ] = w;
                    }

                    var array2 = new NerUnitedEntity[ _nerUnitedEntities.Count ];
                    i = 0;
                    foreach ( var nue in _nerUnitedEntities )
                    {
                        array2[ i++ ] = nue;
                    }

                    nerWords          = array;
                    nerUnitedEntities = array2;
                    relevanceRanking  = _relevanceRanking;
                    return (true);
                }
                nerWords          = default;
                nerUnitedEntities = default;
                relevanceRanking  = default;
                return (false);
            }
            finally
            {
                _NerProcessorPool.Release( t );
            }
        }
        public INerProcessorHolder GetNerProcessorHolder() => new NerProcessorHolder( _NerProcessorPool.Get(), _NerProcessorPool );
    }
}
