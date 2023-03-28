using System;
using System.Web;

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static bool Try2Bool( this string value, bool defaultValue ) => (value != null) && bool.TryParse( value, out var result ) ? result : defaultValue;

        public static T ToEnum< T >( this string value ) where T : struct => (T) Enum.Parse( typeof(T), value, true );

        public static string GetRequestStringParam( this HttpContext context, string paramName, int maxLength )
        {
            var value = context.Request[ paramName ];
            if ( (value != null) && (maxLength < value.Length) && (0 < maxLength) )
            {
                return (value.Substring( 0, maxLength ));
            }
            return (value);
        }
    }
}