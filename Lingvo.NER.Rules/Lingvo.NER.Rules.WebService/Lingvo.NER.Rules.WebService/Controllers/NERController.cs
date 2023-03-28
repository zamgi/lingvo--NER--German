using System;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Lingvo.NER.Rules.WebService.Controllers
{
    [ApiController, Route("[controller]")]
    public sealed class NERController : ControllerBase
    {
        #region [.ctor().]
        private readonly INerProcessorFactory _NerProcessorFactory;        
#if DEBUG
        private readonly ILogger< NERController > _Logger;
#endif
#if DEBUG
        public NERController( INerProcessorFactory nerProcessorFactory, ILogger< NERController > logger )
        {
            _NerProcessorFactory = nerProcessorFactory;
            _Logger              = logger;
        }
#else
        public NERController( INerProcessorFactory nerProcessorFactory ) => _NerProcessorFactory = nerProcessorFactory;
#endif
        #endregion

        [HttpPost, Route(WebApiConsts.NER.Run)] public IActionResult Run( InitParamsVM m )
        {
            try
            {
#if DEBUG
                _Logger.LogInformation( $"start NER '{m.Text.Cut()}'..." );
#endif
                ResultVM result;
                if ( _NerProcessorFactory.Run( m.Text, out var words, out var nerUnitedEntities, out var relevanceRanking ) )
                {
                    result = new ResultVM( in m, words, nerUnitedEntities, relevanceRanking );
                }
                else
                {
                    result = new ResultVM( in m );
                }
#if DEBUG
                _Logger.LogInformation( $"end NER '{m.Text.Cut()}'." );
#endif
                return Ok( result );
            }
            catch ( Exception ex )
            {
                return Ok( new ResultVM( in m, ex ) );
                //---return StatusCode( 500, new ResultVM( in m, ex ) ); //Internal Server Error
            }
        }
    }
}
