using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
#if DEBUG
using Microsoft.Extensions.Logging;
#endif

using Lingvo.NER.NeuralNetwork;
//using _NerRules_word_t = Lingvo.NER.Rules.tokenizing.word_t;

namespace Lingvo.NER.Combined.WebService.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController, Route("[controller]")]
    public sealed class NERCombinedController : ControllerBase
    {
        #region [.ctor().]
        private readonly ConcurrentFactory _ConcurrentFactory;
#if DEBUG
        private readonly ILogger< NERCombinedController > _Logger;
        public NERCombinedController( ConcurrentFactory concurrentFactory, ILogger< NERCombinedController > logger )
        {
            _ConcurrentFactory = concurrentFactory;
            _Logger            = logger;
        }
#else
        public NERCombinedController( ConcurrentFactory concurrentFactory ) => _ConcurrentFactory = concurrentFactory;
#endif
        #endregion

        private static ResultVM EMPTY_Result = new ResultVM() { Words = new List< ResultVM.WordInfo >() };
        [HttpPost, Route(WebApiConsts.NERCombined.Run)] public async Task< IActionResult > Run( InitParamsVM m )
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
                    var t = await _ConcurrentFactory.TryRunAsync( m.Text, m.ModelType, getFirstModelTypeIfMissing: true ).CAX();
                    result = t.ToResultVM( m );
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

        [HttpGet, Route(WebApiConsts.NERCombined.GetModelInfoKeys)] public IActionResult GetModelInfoKeys()
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

        [HttpGet, Route(WebApiConsts.NERCombined.Log)] public Task< IActionResult > Log() => ReadLogFile();
        [HttpGet, Route(WebApiConsts.NERCombined.ReadLogFile)] public async Task< IActionResult > ReadLogFile()
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
        [HttpGet, Route(WebApiConsts.NERCombined.DelLog)] public Task< IActionResult > DelLog() => DeleteLogFile();
        [HttpGet, Route(WebApiConsts.NERCombined.DeleteLogFile)] public async Task< IActionResult > DeleteLogFile()
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
