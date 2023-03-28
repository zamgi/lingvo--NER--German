using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules
{
    /// <summary>
    /// 
    /// </summary>
    public interface INerInputTypeProcessor
    {
        unsafe (NerInputType nerInputType, ExtraWordType extraWordType) GetNerInputType( char* _base, int length );
        (NerInputType nerInputType, ExtraWordType extraWordType) GetNerInputType( word_t word );
    }

    /// <summary>
    /// 
    /// </summary>
    public interface INerInputTypeProcessorFactory
    {
        INerInputTypeProcessor CreateInstance();
    }
}
