using System;

using Lingvo.NER.Rules.tokenizing;

namespace Lingvo.NER.Rules.Birthdays
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class BirthdayWord : word_t
    {
        public BirthdayWord( int _startIndex, in DateTime birthdayDateTime ) 
        {
            startIndex       = _startIndex;
            nerInputType     = NerInputType.Num;
            nerOutputType    = NerOutputType.Birthday;
            BirthdayDateTime = birthdayDateTime;
        }
        public DateTime BirthdayDateTime { get; }
#if DEBUG
        public override string ToString() => $"BIRTHDAY-WORD => '{BirthdayDateTime:dd.MM.yyyy}'"; 
#endif
    }
}