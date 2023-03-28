using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Lingvo.NER.NeuralNetwork.Utils
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class Arg : Attribute
    {
        public string Title;
        public bool   Optional;
        public string Name;

        public Arg( string title, string name, bool optional = true )
        {
            Title    = title;
            Name     = name;
            Optional = optional;
        }

        public string UsageLineText() => (Optional ? "[" : null) + ($"-{Name}: {Title}") + (Optional ? "]" : null);

        public override string ToString() => $"'{Name}', '{Title}', optional: {Optional.ToString().ToUpper()}";
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class ArgField
    {
        private object _O;
        private FieldInfo _Fi;
        private Arg _A;
        private bool _IsSet;

        public ArgField( object o, FieldInfo fi, Arg a )
        {
            _O = o;
            _Fi = fi;
            _A = a;
            _IsSet = false;
        }

        public Arg Arg => _A;

        public void Set( string val )
        {
            try
            {
                if ( _Fi.FieldType == typeof(string) )
                {
                    _Fi.SetValue( _O, val );
                }
                else
                {
                    Type argumentType = _Fi.FieldType.IsGenericType && _Fi.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>) ? _Fi.FieldType.GenericTypeArguments[ 0 ] : _Fi.FieldType;

                    MethodInfo mi = argumentType.GetMethod( "Parse", new Type[] { typeof(string) } );
                    if ( mi != null )
                    {
                        object? oValue = mi.Invoke( null, new object[] { val } );
                        _Fi.SetValue( _O, oValue );
                    }
                    else if ( argumentType.IsEnum )
                    {
                        object oValue = Enum.Parse( _Fi.FieldType, val );
                        _Fi.SetValue( _O, oValue );
                    }
                }
                _IsSet = true;
            }
            catch ( Exception ex )
            {
                throw (new ArgumentException( $"Failed to set value of '{_A}', Error: '{ex.Message}', Call Stack: '{ex.StackTrace}'" ));
            }
        }
        public void Validate()
        {
            if ( !_A.Optional && !_IsSet )
            {
                throw (new ArgumentException( $"Failed to specify value for required {_A}" ));
            }
        }

        public override string ToString() => _A.ToString();
    }

    /// <summary>
    /// 
    /// </summary>
    public static class ArgParser
    {
        public static List< ArgField > Parse( string[] args, object o )
        {
            var args_lst = new List< ArgField >();
            var typeArgAttr = typeof(Arg);
            var t = o.GetType();
            foreach ( FieldInfo fi in t.GetFields( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance ) )
            {
                foreach ( Arg arg in fi.GetCustomAttributes( typeArgAttr, true ) )
                {
                    args_lst.Add( new ArgField( o, fi, arg ) );
                }
            }

            try
            {
                for ( int i = 0; i < args.Length; i++ )
                {
                    if ( args[ i ].StartsWith( "-" ) )
                    {
                        var argName  = args[ i ].Substring( 1 );
                        var argValue = args[ i + 1 ];

                        ArgField intarg = GetArgByName( args_lst, argName );
                        if ( intarg == null )
                        {
                            throw (new ArgumentException( $"'{argName}' is not a valid parameter" ));
                        }

                        intarg.Set( argValue );

                        i++;
                    }
                }

                foreach ( ArgField a in args_lst )
                {
                    a.Validate();
                }
            }
            catch ( Exception ex )
            {
                Console.Error.WriteLine( ex.Message );
                Usage( args_lst );
            }

            return (args_lst);
        }

        private static ArgField GetArgByName( List< ArgField > args, string name )
        {
            foreach ( var a in args )
            {
                if ( string.Compare( a.Arg.Name, name, true ) == 0 )
                {
                    return a;
                }
            }
            return (null);
        }

        public static void Usage( List< ArgField > args, bool forceExit = true )
        {
            Console.Error.WriteLine( $"Usage: {Process.GetCurrentProcess().ProcessName} [parameters...]" );
            foreach ( var p in args )
            {
                Console.Error.WriteLine( $"\t[-{p.Arg.Name}: {p.Arg.Title}]" );
            }
            if ( forceExit )
            {
                Environment.Exit( -1 );
            }
        }
    }
}

