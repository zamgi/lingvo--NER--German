using System;
using System.Threading.Tasks;
using System.Web;

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ProcessHandler : HttpTaskAsyncHandler
    {
        static ProcessHandler() => Environment.CurrentDirectory = HttpContext.Current.Server.MapPath( "~/" );

        public override async Task ProcessRequestAsync( HttpContext context )
        {
            try
            {
                var text        = context.GetRequestStringParam( "text", ConfigEx.MAX_INPUTTEXT_LENGTH );
                var reloadModel = context.Request[ "reloadModel" ].Try2Bool( false );
                var addressOnly = context.Request[ "addressOnly" ].Try2Bool( false );
                var v2          = context.Request[ "v2"          ].Try2Bool( true  );

                var cf = await ConcurrentFactoryHelper.GetConcurrentFactory_Async( reloadModel );
                if ( addressOnly )
                {
                    var words = await cf.Run_UseSimpleSentsAllocate_Address( text );

                    context.Response.SendJsonResponse( words );
                }
                else if ( v2 )
                {
                    var (nerWords, nerUnitedEntities, relevanceRanking) = await cf.Run_UseSimpleSentsAllocate_v2( text );

                    context.Response.SendJsonResponse( nerWords, nerUnitedEntities, relevanceRanking );
                }
                else
                {
                    var words = await cf.Run_UseSimpleSentsAllocate_v1( text );

                    context.Response.SendJsonResponse( words );
                }
            }
            catch ( Exception ex )
            {
                context.Response.SendJsonResponse( ex );
            }            
        }
    }
}