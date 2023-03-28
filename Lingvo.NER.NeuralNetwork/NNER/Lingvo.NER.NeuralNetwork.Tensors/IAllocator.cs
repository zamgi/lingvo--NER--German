namespace Lingvo.NER.NeuralNetwork.Tensors
{
    public interface IAllocator
    {
        Storage Allocate( DType elementType, long elementCount );
        float GetAllocatedMemoryRatio();
    }
}
