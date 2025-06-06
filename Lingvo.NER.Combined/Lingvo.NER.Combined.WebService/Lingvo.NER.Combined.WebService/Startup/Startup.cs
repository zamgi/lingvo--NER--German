using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace Lingvo.NER.Combined.WebService
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
            services.AddControllers().AddJsonOptions( opts =>
            {
                opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                opts.JsonSerializerOptions.Converters.Add( new JsonStringEnumConverter() );
            });
            services.AddCors( opts =>
            {
                var cors = _Configuration.GetSection( "CORS" ).Get< string[] >();
                if ( cors != null )
                {
                    // this defines a CORS policy called "CORS_DEFAULT"
                    opts.AddPolicy( CORS_DEFAULT, policy => policy.WithOrigins( cors ).AllowAnyHeader().AllowAnyMethod()/*.AllowAnyOrigin().AllowCredentials()*/ );
                }
            });

            //For application running on IIS:
            services.Configure< IISServerOptions >( opts => opts.MaxRequestBodySize = int.MaxValue );
            //For application running on Kestrel:
            services.Configure< KestrelServerOptions >( opts => opts.Limits.MaxRequestBodySize = int.MaxValue );
            //Form's MultipartBodyLengthLimit
            services.Configure< FormOptions >( opts =>
            {
                opts.ValueLengthLimit            = int.MaxValue;
                opts.MultipartBodyLengthLimit    = int.MaxValue; // if don't set default value is: 128 MB
                opts.MultipartHeadersLengthLimit = int.MaxValue;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure( IApplicationBuilder app, IWebHostEnvironment env )
        {
            if ( env.IsDevelopment() )
            {
                app.UseDeveloperExceptionPage();
            }

            #region comm. handmade set 'ContentRootPath'
            /*
            var dir     = Path.GetDirectoryName( Path.GetDirectoryName( Process.GetCurrentProcess().MainModule.FileName ) );
            var wwwroot = Path.Combine( dir, "wwwroot" );
            env.ContentRootFileProvider = new PhysicalFileProvider( dir );
            env.ContentRootPath = dir;
            env.WebRootPath     = wwwroot;
            app.UseStaticFiles( new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider( Path.Combine( env.WebRootPath, "images" ) ),
                RequestPath  = "/MyImages"
            });
            */
            #endregion

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseCors( CORS_DEFAULT );
            //---app.UseCors( configurePolicy => configurePolicy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().AllowCredentials() );

            app.UseEndpoints( endpoints => endpoints.MapControllers() );
#if DEBUG
            OpenBrowserIfRunAsConsole( app );
# endif
        }

        private static void OpenBrowserIfRunAsConsole( IApplicationBuilder app )
        {
            #region [.open browser if run as console.]
            if ( !WindowsServiceHelpers.IsWindowsService() ) //IsRunAsConsole
            {
                var server    = app.ApplicationServices.GetRequiredService< IServer >();
                var addresses = server.Features?.Get< IServerAddressesFeature >()?.Addresses;
                var address   = addresses?.FirstOrDefault( a => a.StartsWith( "https:" ) ) ?? addresses?.FirstOrDefault();
                
                if ( address == null )
                {
                    var config = app.ApplicationServices.GetService< IConfiguration >();
                    address = config.GetSection( "Kestrel:Endpoints:Https:Url" ).Value ??
                              config.GetSection( "Kestrel:Endpoints:Http:Url"  ).Value;
                }

                if ( address != null )
                {
                    address = address.Replace( "/*:", "/localhost:" );

                    using ( Process.Start( new ProcessStartInfo( address.TrimEnd( '/' ) + '/' ) { UseShellExecute = true } ) ) { };
                }                
            }
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        private static class WindowsServiceHelpers
        {
            public static bool IsWindowsService()
            {
                if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
                {
                    return (false);
                }
                using var parentProcess = Win32.GetParentProcess();
                if ( parentProcess == null )
                {
                    return (false);
                }
                if ( parentProcess.SessionId == 0 )
                {
                    return (string.Compare( "services", parentProcess.ProcessName, true ) == 0);
                }
                return (false);
            }            
        }
        /// <summary>
        /// 
        /// </summary>
        private static class Win32
        {
            /// <summary>
            /// 
            /// </summary>
	        [Flags] private enum SnapshotFlags : uint
	        {
		        HeapList = 0x1u,
		        Process  = 0x2u,
		        Thread   = 0x4u,
		        Module   = 0x8u,
		        Module32 = 0x10u,
		        All      = 0xFu,
		        Inherit  = 0x80000000u,
		        NoHeaps  = 0x40000000u
	        }
            /// <summary>
            /// 
            /// </summary>
	        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
	        private struct PROCESSENTRY32
	        {
		        private const int MAX_PATH = 260;
		        internal uint dwSize;
		        internal uint cntUsage;
		        internal uint th32ProcessID;
		        internal IntPtr th32DefaultHeapID;
		        internal uint th32ModuleID;
		        internal uint cntThreads;
		        internal uint th32ParentProcessID;
		        internal int pcPriClassBase;
		        internal uint dwFlags;
		        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		        internal string szExeFile;
	        }

	        [DllImport("kernel32", SetLastError=true)] private static extern IntPtr CreateToolhelp32Snapshot( SnapshotFlags dwFlags, uint th32ProcessID );
            [DllImport("kernel32", CharSet=CharSet.Auto, SetLastError=true)] private static extern bool Process32First( [In] IntPtr hSnapshot, ref PROCESSENTRY32 lppe );
            [DllImport("kernel32", CharSet=CharSet.Auto, SetLastError=true)] private static extern bool Process32Next( [In] IntPtr hSnapshot, ref PROCESSENTRY32 lppe );
            [DllImport("kernel32", SetLastError=true)][return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseHandle( [In] IntPtr hObject );

            public static Process GetParentProcess()
	        {
		        var intPtr = IntPtr.Zero;
		        try
		        {
                    intPtr = CreateToolhelp32Snapshot( SnapshotFlags.Process, 0u );
			        var lppe = new PROCESSENTRY32() { dwSize = (uint) Marshal.SizeOf( typeof(PROCESSENTRY32) ) };
                    if ( Process32First( intPtr, ref lppe ) )
                    {
                        int id;
                        using ( var p = Process.GetCurrentProcess() )
                        {
                            id = p.Id;
                        }
				        do
				        {
                            if ( id == lppe.th32ProcessID )
                            {
                                return (Process.GetProcessById( (int) lppe.th32ParentProcessID ));
                            }
                        }
                        while ( Process32Next( intPtr, ref lppe ) );
                    }
		        }
                catch ( Exception ex )
                {
                    Debug.WriteLine( ex );
                }
                finally
                {
                    CloseHandle( intPtr );
                }
                return (null);
            }
        }
    }
}
