using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Lingvo.NER.NeuralNetwork.Utils;

using Microsoft.AspNetCore.Mvc;
#if DEBUG
using Microsoft.Extensions.Logging;
#endif

namespace Lingvo.NER.NeuralNetwork.WebService.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController, Route("[controller]")]
    public sealed class NNERController : ControllerBase
    {
        #region [.ctor().]
        private readonly ConcurrentFactory _ConcurrentFactory;
#if DEBUG
        private readonly ILogger< NNERController > _Logger;
        public NNERController( ConcurrentFactory concurrentFactory, ILogger< NNERController > logger )
        {
            _ConcurrentFactory = concurrentFactory;
            _Logger            = logger;
        }
#else
        public NNERController( ConcurrentFactory concurrentFactory ) => _ConcurrentFactory = concurrentFactory;
#endif
        #endregion

        private static (IList< ResultVM.TupleVM >, Exception) EMPTY = (new List< ResultVM.TupleVM >(), default);
        private static ResultVM EMPTY_Result = new ResultVM() { NerResults = new List< ResultVM.NerResultVM >() };
        [HttpPost, Route(WebApiConsts.NNER.Run)] public async Task< IActionResult > Run( ParamsVM m )
        {
            try
            {
                await _ConcurrentFactory.LogToFile( m.Text );
#if DEBUG
                _Logger.LogInformation( $"start '{m.ToText()}'..." );
#endif
                ResultVM result;
                if ( !m.Text.IsNullOrWhiteSpace() )
                {
                    var sents = m.Text.Split( '\n' );
                    if ( 1 < sents.Length )
                    {
                        var sd = new SortedDictionary< int, (IList< ResultVM.TupleVM > nerResult, Exception error) >();
                        await sents.ForEachAsync( async (sent, i, _) =>
                        {
                            (IList< ResultVM.TupleVM > nerResult, Exception error) t;
                            if ( !sent.IsNullOrWhiteSpace() )
                            {
                                t = await _ConcurrentFactory.TryRunAsync( sent, m.MakePostMerge, m.ModelType, getFirstModelTypeIfMissing: true/*, m.MaxPredictSentLength, cutDropout: 0*/ ).CAX();
                            }
                            else
                            {
                                t = EMPTY;
                            }

                            sd.AddWithLock( i, t );
                        })
                        .CAX();
                        result = sd.Values.ToResultVM();
                    }
                    else
                    {
                        var t = await _ConcurrentFactory.TryRunAsync( m.Text, m.MakePostMerge, m.ModelType, getFirstModelTypeIfMissing: true ).CAX();
                        result = t.ToResultVM();
                    }
                }
                else
                {
                    result = EMPTY_Result;
                }
#if DEBUG
                _Logger.LogInformation( $"end '{m.ToText()}'." );
#endif
                return Ok( result );
            }
            catch ( Exception ex )
            {
                return Ok( ex.ToErrorVM() );
            }
        }

        [HttpGet, Route(WebApiConsts.NNER.GetModelInfoKeys)] public IActionResult GetModelInfoKeys()
        {
            try
            {
                return Ok( _ConcurrentFactory.GetModelInfoKeys() );
            }
            catch ( Exception ex )
            {
                return Ok( ex.ToErrorVM() );
            }
        }

        [HttpGet, Route(WebApiConsts.NNER.Log)] public Task< IActionResult > Log() => ReadLogFile();
        [HttpGet, Route(WebApiConsts.NNER.ReadLogFile)] public async Task< IActionResult > ReadLogFile()
        {
            try
            {
                var text = await _ConcurrentFactory.ReadLogFile();
                return Ok( text );
            }
            catch ( Exception ex )
            {
                return Ok( ex.ToErrorVM() );
            }
        }
        [HttpGet, Route(WebApiConsts.NNER.DelLog)] public Task< IActionResult > DelLog() => DeleteLogFile();
        [HttpGet, Route(WebApiConsts.NNER.DeleteLogFile)] public async Task< IActionResult > DeleteLogFile()
        {
            try
            {
                await _ConcurrentFactory.DeleteLogFile();
                return Ok( "Success" );
            }
            catch ( Exception ex )
            {
                return Ok( ex.ToErrorVM() );
            }
        }        
    }
}
