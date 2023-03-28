using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

#if NETCOREAPP
using System.Runtime.Loader;
#else
using System.Linq;
#endif

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    public interface IResourceAssemblyLoader : IDisposable
    {
        T GetProperty< T >( string name );
        bool TryGetProperty< T >( string name, out T t );
        IEnumerable< (string name, T t) > GetAllProperties< T >();
    }

#if NETCOREAPP
    /// <summary>
    /// 
    /// </summary>
    public class ResourceAssemblyLoader : IResourceAssemblyLoader, IDisposable
    {
        private AssemblyLoadContext _AssemblyLoadContext;
        private Type _ResourcesClassType;
        public ResourceAssemblyLoader( string resourceAssemblyPath, string resourcesClassName )
        {
            _AssemblyLoadContext = new AssemblyLoadContext( nameof(ResourceAssemblyLoader), isCollectible: true );

            var assemblyPath = Path.GetFullPath( resourceAssemblyPath );
            var assembly = _AssemblyLoadContext.LoadFromAssemblyPath( assemblyPath );

            _ResourcesClassType = assembly.GetType( resourcesClassName, throwOnError: true, ignoreCase: true );
        }
        public virtual void Dispose() => _AssemblyLoadContext.Unload();

        public T GetProperty< T >( string name )
        {
            var prop = _ResourcesClassType.GetProperty( name, BindingFlags.Public | BindingFlags.Static ) ?? throw (new InvalidDataException( $"Unknown property: '{name}'." ));
            var t = (T) prop.GetValue( null );
            return (t);
        }
        public bool TryGetProperty< T >( string name, out T t )
        {
            var prop = _ResourcesClassType.GetProperty( name, BindingFlags.Public | BindingFlags.Static );
            if ( (prop != null) && (prop.GetValue( null ) is T _t) )
            {
                t = _t;
                return (true);
            }
            t = default;
            return (false);
        }

        public IEnumerable< (string name, T t) > GetAllProperties< T >()
        {
            var props = _ResourcesClassType.GetProperties( BindingFlags.Public | BindingFlags.Static );
            foreach ( var prop in props )
            {
                if ( prop.GetValue( null ) is T t )
                {
                    yield return (prop.Name, t);
                }
            }
        }
    }
#else
    /// <summary>
    /// 
    /// </summary>
    public class ResourceAssemblyLoader : IResourceAssemblyLoader, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class ProxyDomain : MarshalByRefObject
        {
            public (Dictionary< string, byte[] > bytesResources, Dictionary< string, string > stringResources) LoadAssemblyResources( string assemblyPath, string resourcesClassName )
            {
                var assembly = Assembly.LoadFrom( assemblyPath );
                var resourcesClassType = assembly.GetType( resourcesClassName, throwOnError: true, ignoreCase: true );

                var props = resourcesClassType.GetProperties( BindingFlags.Public | BindingFlags.Static );
                var bytesResources  = new Dictionary< string, byte[] >( props.Length );
                var stringResources = new Dictionary< string, string >( props.Length );
                foreach ( var prop in props )
                {
                    var v = prop.GetValue( null );
                    if ( v is byte[] bytes )
                    {
                        bytesResources[ prop.Name ] = bytes;
                    }
                    else if ( v is string s )
                    {
                        stringResources[ prop.Name ] = s;
                    }
                }
                return (bytesResources, stringResources);
            }
        }
        ///// <summary>
        ///// 
        ///// </summary>
        //private sealed class PropertyInfo_EX : PropertyInfo
        //{
        //    public PropertyInfo_EX( string name ) => Name = name;
        //    public override string Name { get; }
        //    public override Type PropertyType => throw (new NotImplementedException());
        //    public override PropertyAttributes Attributes => throw (new NotImplementedException());
        //    public override bool CanRead => throw (new NotImplementedException());
        //    public override bool CanWrite => throw (new NotImplementedException());
        //    public override Type DeclaringType => throw (new NotImplementedException());
        //    public override Type ReflectedType => throw (new NotImplementedException());
        //    public override MethodInfo[] GetAccessors( bool nonPublic ) => throw (new NotImplementedException());
        //    public override object[] GetCustomAttributes( bool inherit ) => throw (new NotImplementedException());
        //    public override object[] GetCustomAttributes( Type attributeType, bool inherit ) => throw (new NotImplementedException());
        //    public override MethodInfo GetGetMethod( bool nonPublic ) => throw (new NotImplementedException());
        //    public override ParameterInfo[] GetIndexParameters() => throw (new NotImplementedException());
        //    public override MethodInfo GetSetMethod( bool nonPublic ) => throw (new NotImplementedException());
        //    public override object GetValue( object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture ) => throw (new NotImplementedException());
        //    public override bool IsDefined( Type attributeType, bool inherit ) => throw (new NotImplementedException());
        //    public override void SetValue( object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture ) => throw (new NotImplementedException());
        //}


        private Dictionary< string, byte[] > _BytesResources;
        private Dictionary< string, string > _StringResources;
        public ResourceAssemblyLoader( string resourceAssemblyPath, string resourcesClassName )
        {
            var appDomain = AppDomain.CreateDomain( nameof(ResourceAssemblyLoader) );
            try
            {
                ProxyDomain proxyDomain;
                try
                {
                    proxyDomain = (ProxyDomain) appDomain.CreateInstanceAndUnwrap( typeof(ProxyDomain).Assembly.FullName, typeof(ProxyDomain).FullName );
                }
                catch ( FileNotFoundException ) //appear in asp.net web-app
                {
                    AppDomain.Unload( appDomain );

                    var setup = new AppDomainSetup() { ApplicationBase = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) };
                    appDomain = AppDomain.CreateDomain( nameof(ResourceAssemblyLoader), null, setup );                    
                    
                    proxyDomain = (ProxyDomain) appDomain.CreateInstanceAndUnwrap( typeof(ProxyDomain).Assembly.FullName, typeof(ProxyDomain).FullName );
                }                

                var assemblyPath = Path.GetFullPath( resourceAssemblyPath );
                (_BytesResources, _StringResources) = proxyDomain.LoadAssemblyResources( assemblyPath, resourcesClassName );
            }
            finally
            {
                AppDomain.Unload( appDomain );
            }
        }
        public virtual void Dispose() { }

        public T GetProperty< T >( string name ) //=> (T) (object) _BytesResources[ name ];
        {
            if ( _BytesResources.TryGetValue( name, out var bytes ) )
            {
                return ((T) (object) bytes);
            }
            if ( _StringResources.TryGetValue( name, out var s ) )
            {
                return ((T) (object) s);
            }

            throw (new InvalidDataException( $"Unknown property: '{name}'." ));
        }
        public bool TryGetProperty< T >( string name, out T t )
        {
            if ( (typeof(T) == typeof(byte[])) && _BytesResources.TryGetValue( name, out var bytes ) )
            {
                t = (T) (object) bytes;
                return (true);
            }
            if ( (typeof(T) == typeof(string)) && _StringResources.TryGetValue( name, out var s ) )
            {
                t = (T) (object) s;
                return (true);
            }
            t = default;
            return (false);
        }

        public IEnumerable< (string name, T t) > GetAllProperties< T >() //=> _BytesResources.Select( p => (p.Key, (T) (object) p.Value) );
        {
            if ( typeof(T) == typeof(byte[]) )
            {
                return (_BytesResources.Select( p => (p.Key, (T) (object) p.Value) ));
            }
            if ( typeof(T) == typeof(string) )
            {
                return (_StringResources.Select( p => (p.Key, (T) (object) p.Value) ));
            }

            throw (new InvalidDataException( $"Unknown property type: '{typeof(T)}'." ));
        }
    }
#endif
}

