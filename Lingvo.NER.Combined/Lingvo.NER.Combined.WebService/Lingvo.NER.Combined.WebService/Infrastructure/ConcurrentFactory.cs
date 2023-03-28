using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Lingvo.NER.NeuralNetwork;
using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;
using _NerRules_word_t = Lingvo.NER.Rules.tokenizing.word_t;

namespace Lingvo.NER.Combined.WebService
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ConcurrentFactory : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly struct ModelInfoConfig
        {
            public Predictor  predictor     { get; init; }
            public string     modelFilePath { get; init; }
            public bool       upperCase     { get; init; }
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class ModelTuple
        {
            public ModelTuple( in ModelInfoConfig t )
            {
                WeakRef       = new WeakReference< Predictor >( t.predictor );
                ModelFilePath = t.modelFilePath;
                UpperCase     = t.upperCase;
            }
            public WeakReference< Predictor > WeakRef       { get; }
            public string                     ModelFilePath { get; }
            public bool                       UpperCase     { get; }
        }

        #region [.ctor().]
        private SemaphoreSlim _Semaphore;
        private NNERConfig    _NNERConfig;
        private ConcurrentStack< NERCombined_Processor > _Stack;
        private IReadOnlyDictionary< string, ModelTuple > _NNERModelInfoByType;
        private bool                 _EnableLog;
        private string               _LogFileName;
        private AsyncCriticalSection _LogCS;
        public ConcurrentFactory( IReadOnlyDictionary< string, ModelInfoConfig > nnerModelInfoByType, NNERConfig nnerConfig, in NERCombinedConfig_ForOuterNNERPredictor cfg, int instanceCount )
        {
            if ( instanceCount <= 0 )           throw (new ArgumentException( nameof(instanceCount) ));
            if ( !nnerModelInfoByType.AnyEx() ) throw (new ArgumentException( nameof(nnerModelInfoByType) ));
            if ( nnerConfig == null )           throw (new ArgumentException( nameof(nnerConfig) ));
            //------------------------------------------------------------------------------------------------------//

            _NNERConfig          = nnerConfig;
            _Semaphore           = new SemaphoreSlim( instanceCount, instanceCount );
            _NNERModelInfoByType = nnerModelInfoByType.ToDictionary( p => p.Key, p => new ModelTuple( p.Value ) );

            _EnableLog = _NNERConfig.Log.Enable;
            if ( _EnableLog )
            {
                _LogFileName = Path.GetFullPath( _NNERConfig.Log.LogFileName );
                _LogCS       = AsyncCriticalSection.Create();
            }

            _Stack = new ConcurrentStack< NERCombined_Processor >();
            for ( int i = 0; i < instanceCount; i++ )
            {
                _Stack.Push( NERCombined_Processor.CreateForOuterNNERPredictor( cfg ) );
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
            _LogCS    .Dispose();
        }
        #endregion

        [M(O.AggressiveInlining)] private Predictor GetPredictor( ModelTuple t )
        {
            if ( !t.WeakRef.TryGetTarget( out var predictor ) || (predictor == null) )
            {
                lock ( t.WeakRef )
                {
                    if ( !t.WeakRef.TryGetTarget( out predictor ) || (predictor == null) )
                    {
                        var opts = JsonConvert.DeserializeObject< NNERConfig >( JsonConvert.SerializeObject( _NNERConfig ) );
                        opts.ModelFilePath = t.ModelFilePath;

                        var sl = SeqLabel.Create4Predict( opts );
                        predictor = new Predictor( sl, t.UpperCase );
                        t.WeakRef.SetTarget( predictor );
                    }
                }
            }
            return (predictor);
        }
        [M(O.AggressiveInlining)] private ModelTuple GetModelTuple( string modelType, bool getFirstModelTypeIfMissing )
        {
            ModelTuple mt;
            if ( getFirstModelTypeIfMissing )
            {
                if ( modelType.IsNullOrEmpty() ) mt = _NNERModelInfoByType.Values.First();
                else if ( !_NNERModelInfoByType.TryGetValue( modelType, out mt ) ) throw (new ArgumentNullException( nameof(modelType) ));
            }
            else
            {
                if ( modelType.IsNullOrEmpty() ) throw (new ArgumentNullException( nameof(modelType) ));
                if ( !_NNERModelInfoByType.TryGetValue( modelType, out mt ) ) throw (new ArgumentNullException( nameof(modelType) ));
            }
            return (mt);
        }
        public async Task< (List< _NerRules_word_t > nerWords, Exception error) > TryRunAsync( string text, string modelType, bool getFirstModelTypeIfMissing = false /*, int? maxPredictSentLength = null, float cutDropout = 0.1f*/ )
        {
            try
            {
                var mt = GetModelTuple( modelType, getFirstModelTypeIfMissing );
                //--------------------------------------------------------------//

                await _Semaphore.WaitAsync().CAX();
                var nerCombinedProcessor = default(NERCombined_Processor);
                try
                {
                    var predictor = GetPredictor( mt );
                    nerCombinedProcessor = Pop( _Stack );

                    var nerWords = nerCombinedProcessor.ProcessText( text, predictor );
                    return (nerWords, default);
                }
                finally
                {
                    Push( _Stack, nerCombinedProcessor );
                    _Semaphore.Release();
                }
            }
            catch ( Exception ex )
            {
                return (default, ex);
            }
        }
        
        
        public IEnumerable< string > GetModelInfoKeys() => _NNERModelInfoByType.Keys;

        public async Task LogToFile( string msg )
        {
            if ( _EnableLog )
            {
                await _LogCS.EnterAsync().CAX();
                try
                {
                    await File.AppendAllTextAsync( _LogFileName, $"{DateTime.Now:dd.MM.yyyy, HH:mm}\r\nTEXT: '{msg}'\r\n------------------------------------------------------------------\r\n\r\n", Encoding.UTF8 ).CAX();
                }
                catch ( Exception ex )
                {
                    _EnableLog = false;
                    Debug.WriteLine( ex );
                }
                finally
                {
                    _LogCS.Exit();
                }
            }
        }
        public async Task< string > ReadLogFile()
        {
            await _LogCS.EnterAsync().CAX();
            try
            {
                var text = await File.ReadAllTextAsync( _LogFileName, Encoding.UTF8 ).CAX();
                return (text);
            }
            catch ( Exception ex )
            {
                return (ex.ToString());
            }
            finally
            {
                _LogCS.Exit();
            }
        }
        public async Task DeleteLogFile()
        {
            await _LogCS.EnterAsync().CAX();
            try
            {
                File.Delete( _LogFileName );
            }
            finally
            {
                _LogCS.Exit();
            }
        }

        private static T Pop< T >( ConcurrentStack< T > stack )
        {
            for ( T t; stack.TryPop( out t ); )
            {
                return (t);
            }
            return (default);
        }
        private static void Push< T >( ConcurrentStack< T > stack, T t )
        {
            if ( t != null )
            {
                stack.Push( t );
            }
        }
    }
}
