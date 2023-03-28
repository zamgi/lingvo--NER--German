using System.Collections.Generic;
using System.Text;

namespace Lingvo.NER.NeuralNetwork.Tensors.Cuda.RuntimeCompiler
{
    /// <summary>
    /// 
    /// </summary>
    public class KernelConfig
    {
        private readonly SortedDictionary<string, string> _Values = new SortedDictionary<string, string>();
        public KernelConfig() { }

        public IEnumerable<string> Keys => _Values.Keys;
        public IEnumerable<KeyValuePair<string, string>> AllValues() => _Values;

        public override bool Equals( object obj )
        {
            var o = obj as KernelConfig;
            if ( o == null )
            {
                return (false);
            }

            if ( _Values.Count != o._Values.Count )
            {
                return (false);
            }

            foreach ( KeyValuePair<string, string> kvp in _Values )
            {
                if ( _Values.TryGetValue( kvp.Key, out string oValue ) )
                {
                    if ( !kvp.Value.Equals( oValue ) )
                    {
                        return (false);
                    }
                }
                else
                {
                    return (false);
                }
            }

            return (true);
        }
        public override int GetHashCode()
        {
            int result = 0;
            foreach ( KeyValuePair<string, string> kvp in _Values )
            {
                result ^= kvp.Key.GetHashCode();
                result ^= kvp.Value.GetHashCode();
            }
            return result;
        }

        public bool ContainsKey( string name ) => _Values.ContainsKey( name );
        public void Set( string name, string value ) => _Values[ name ] = value;
        public string ApplyToTemplate( string templateCode )
        {
            var fullCode = new StringBuilder();
            foreach ( KeyValuePair<string, string> item in _Values )
            {
                fullCode.AppendFormat( "#define {0} {1}\n", item.Key, item.Value );
            }
            fullCode.AppendLine( templateCode );
            return fullCode.ToString();
        }
    }
}
