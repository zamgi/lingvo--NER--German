using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace Lingvo.NER.Rules.WebService
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class Startup
    {
        private const string CORS_DEFAULT = "CORS_DEFAULT";
        private IConfiguration _Configuration;
        public Startup( IConfiguration configuration ) => _Configuration = configuration;        

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices( IServiceCollection services )
        {
            services.AddControllers().AddJsonOptions( options =>
            {
                //options.JsonSerializerOptions.IgnoreNullValues = true;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add( new JsonStringEnumConverter() );
            });
            services.AddCors( (options) =>
            {
                var cors = _Configuration.GetSection( "CORS" ).Get< string[] >();
                if ( cors != null )
                {
                    // this defines a CORS policy called "CORS_DEFAULT"
                    options.AddPolicy( CORS_DEFAULT, (policy) => policy.WithOrigins( cors ).AllowAnyHeader().AllowAnyMethod()/*.AllowAnyOrigin().AllowCredentials()*/ );
                }
            });

            //For application running on IIS:
            services.Configure< IISServerOptions >( options =>
            {
                options.MaxRequestBodySize = int.MaxValue;
            });
            //For application running on Kestrel:
            services.Configure< KestrelServerOptions >( options =>
            {
                options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
            });
            //Form's MultipartBodyLengthLimit
            services.Configure< FormOptions >( x =>
            {
                x.ValueLengthLimit            = int.MaxValue;
                x.MultipartBodyLengthLimit    = int.MaxValue; // if don't set default value is: 128 MB
                x.MultipartHeadersLengthLimit = int.MaxValue;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure( IApplicationBuilder app, IWebHostEnvironment env )
        {
            if ( env.IsDevelopment() )
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseDefaultFiles();

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseCors( CORS_DEFAULT );
            //---app.UseCors( configurePolicy => configurePolicy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().AllowCredentials() );

            app.UseEndpoints( endpoints => endpoints.MapControllers() );
            //-------------------------------------------------------------//
            
            OpenBrowserIfRunAsConsole( app );
        }
        private static void OpenBrowserIfRunAsConsole( IApplicationBuilder app )
        {
            #region [.open browser if run as console.]
            if ( !WindowsServiceHelpers.IsWindowsService() ) //IsRunAsConsole
            {
                var server    = app.ApplicationServices.GetRequiredService< IServer >();
                var addresses = server.Features?.Get< IServerAddressesFeature >()?.Addresses;
                var address   = addresses?.FirstOrDefault();
                
                if ( address == null )
                {
                    var config = app.ApplicationServices.GetService< IConfiguration >();
                    address = config.GetSection( "Kestrel:Endpoints:Https:Url" ).Value ??
                              config.GetSection( "Kestrel:Endpoints:Http:Url"  ).Value;
                    if ( address != null )
                    {
                        address = address.Replace( "/*:", "/localhost:" );
                    }
                }

                //System.Console.WriteLine( $"[ADDRESS: {address ?? "NULL"}]" );

                if ( address != null )
                {
                    using ( Process.Start( new ProcessStartInfo( address.TrimEnd('/') + "/index.html" /*"http://localhost:1234"*/ ) { UseShellExecute = true } ) ) { };
                }                
            }
            #endregion
        }
    }
}
