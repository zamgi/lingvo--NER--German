using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Lingvo.NER.NeuralNetwork.NerPostMerging;
using Lingvo.NER.NeuralNetwork.Tokenizing;
using Lingvo.NER.NeuralNetwork.Utils;
using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;
using Lingvo.NER.NeuralNetwork.Tensors;

namespace Lingvo.NER.NeuralNetwork.WebService
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
            public Tokenizer  tokenizer     { get; init; }
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
                Tokenizer     = t.tokenizer;
                UpperCase     = t.upperCase;
            }
            public WeakReference< Predictor > WeakRef       { get; }
            public string                     ModelFilePath { get; }
            public Tokenizer                  Tokenizer     { get; }
            public bool                       UpperCase     { get; }
        }

        #region [.ctor().]
        private SemaphoreSlim _Semaphore;
        private Config        _Opts;
        private IReadOnlyDictionary< string, ModelTuple > _SLByType;
        private bool                 _EnableLog;
        private string               _LogFileName;
        private AsyncCriticalSection _LogCS;
        public ConcurrentFactory( IReadOnlyDictionary< string, ModelInfoConfig > slByType, Config opts, int instanceCount )
        {
            if ( instanceCount <= 0 ) throw (new ArgumentException( nameof(instanceCount) ));
            if ( slByType == null )   throw (new ArgumentException( nameof(slByType) ));
            if ( !slByType.Any() )    throw (new ArgumentException( nameof(slByType) ));
            if ( opts == null )       throw (new ArgumentException( nameof(opts) ));
            //------------------------------------------------------------------------------------------------------//

            _Opts      = opts;
            _Semaphore = new SemaphoreSlim( instanceCount, instanceCount );
            _SLByType  = slByType.ToDictionary( p => p.Key, p => new ModelTuple( p.Value ) );
            _EnableLog = _Opts.Log.Enable;
            if ( _EnableLog )
            {
                _LogFileName = Path.GetFullPath( _Opts.Log.LogFileName );
                _LogCS       = AsyncCriticalSection.Create();
            }
        }
        public void Dispose()
        {
            _Semaphore.Dispose();
            _LogCS    .Dispose();
        }
        #endregion

        //private static IList< ResultVM.TupleVM > EMPTY = new List< ResultVM.TupleVM >();
        //private static Exception EMPTY_Exception = new Exception( "EMPTY text" );
        [M(O.AggressiveInlining)] private Predictor GetPredictor( ModelTuple t )
        {
            if ( !t.WeakRef.TryGetTarget( out var predictor ) || (predictor == null) )
            {
                lock ( t.WeakRef )
                {
                    if ( !t.WeakRef.TryGetTarget( out predictor ) || (predictor == null) )
                    {
                        var opts = JsonConvert.DeserializeObject< Config >( JsonConvert.SerializeObject( _Opts ) );
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
                if ( modelType.IsNullOrEmpty() ) mt = _SLByType.Values.First();
                else if ( !_SLByType.TryGetValue( modelType, out mt ) ) throw (new ArgumentNullException( nameof(modelType) ));
            }
            else
            {
                if ( modelType.IsNullOrEmpty() ) throw (new ArgumentNullException( nameof(modelType) ));
                if ( !_SLByType.TryGetValue( modelType, out mt ) ) throw (new ArgumentNullException( nameof(modelType) ));
            }
            return (mt);
        }
        [M(O.AggressiveInlining)] private
#if !(DEBUG)
            static
#endif
            IList< ResultVM.TupleVM > CreateResultTuples( List< word_t > input_words, IList< string > output_words, string text, bool makePostMerge, bool upperCasePostMerge )
        {
            if ( makePostMerge )
            {
                input_words.SetNNerOutputType( output_words );
                NerPostMerger.Run_Merge( input_words, upperCasePostMerge );

                var len = input_words.Count;
                var res = new ResultVM.TupleVM[ len ];
                for ( var i = 0; i < len; i++ )
                {
                    var w = input_words[ i ];
                    res[ i ]= new ResultVM.TupleVM() { Word = text.Substring( w.startIndex, w.length ), Ner = w.nerOutputType.ToText() };
                }
                return (res);
            }
            else
            {
#if DEBUG                
                Debug.Assert( (input_words.Count == output_words.Count) || ((input_words.Count > output_words.Count) && (_Opts.MaxPredictSentLength == output_words.Count)) );
#endif
                var len = Math.Min( input_words.Count, output_words.Count );
                var res = new ResultVM.TupleVM[ len ];
                for ( var i = 0; i < len; i++ )
                {
                    var w = input_words[ i ];
                    res[ i ]= new ResultVM.TupleVM() { Word = text.Substring( w.startIndex, w.length ), Ner = output_words[ i ] };
                }
                return (res);
            }
        }
        public async Task< (IList< ResultVM.TupleVM > result, Exception error) > TryRunAsync( string text, bool makePostMerge, string modelType, bool getFirstModelTypeIfMissing = false /*, int? maxPredictSentLength = null, float cutDropout = 0.1f*/ )
        {
            try
            {
                var mt = GetModelTuple( modelType, getFirstModelTypeIfMissing );
                //------------------------------------------------------------------------------------------------------//

                await _Semaphore.WaitAsync().CAX();
                try
                {
                    var p = GetPredictor( mt );
                    if ( !mt.Tokenizer.TryTokenizeBySentsWithLock( text, out var input_sents ) )
                    {
                        return (new[] { new ResultVM.TupleVM() { Word = text, Ner = NerOutputType.Other.ToText() } }, default);
                        //---return (EMPTY, EMPTY_Exception);
                    }

                    if ( input_sents.Count == 1 )
                    {
                        var input_words  = input_sents[ 0 ];
                        var input_tokens = Tokenizer.ToNerInputTokens( input_words, mt.UpperCase );
                        var output_words = p.Predict( input_tokens );

                        var res = CreateResultTuples( input_words, output_words, text, makePostMerge, mt.UpperCase );
                        return (res, default);
                    }
                    else
                    {
                        var sd  = new SortedDictionary< long, IList< ResultVM.TupleVM > >();
                        var cnt = 0;
                        Parallel.ForEach( input_sents, (input_words, _, i) =>
                        {
                            var input_tokens = Tokenizer.ToNerInputTokens( input_words, mt.UpperCase );
                            var output_words = p.Predict( input_tokens );

                            var res = CreateResultTuples( input_words, output_words, text, makePostMerge, mt.UpperCase );

                            sd.AddWithLock( i, res );
                            Interlocked.Add( ref cnt, res.Count );
                        });
                        var result = sd.Values.SelectMany( t => t ).ToList( cnt );
                        return (result, default);
                    }
                }
                finally
                {
                    _Semaphore.Release();
                }
            }
            catch ( Exception ex )
            {
                return (default, ex);
            }
        }
        
        
        public IEnumerable< string > GetModelInfoKeys() => _SLByType.Keys;

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
    }
}
