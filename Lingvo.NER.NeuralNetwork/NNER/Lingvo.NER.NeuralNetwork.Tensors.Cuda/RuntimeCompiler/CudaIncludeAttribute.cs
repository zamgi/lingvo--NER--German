using System;

namespace Lingvo.NER.NeuralNetwork.Tensors.Cuda.RuntimeCompiler
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class CudaIncludeAttribute : Attribute
    {
        public string FieldName   { get; private set; }
        public string IncludeName { get; private set; }

        public CudaIncludeAttribute( string fieldName, string includeName )
        {
            FieldName   = fieldName;
            IncludeName = includeName;
        }
    }
}
