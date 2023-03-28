using System;

namespace Lingvo.NER.NeuralNetwork.Tensors.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class DelegateDisposable : IDisposable
    {
        private readonly Action _Action;
        public DelegateDisposable( Action action ) => _Action = action;
        public virtual void Dispose() => _Action();
    }
}
