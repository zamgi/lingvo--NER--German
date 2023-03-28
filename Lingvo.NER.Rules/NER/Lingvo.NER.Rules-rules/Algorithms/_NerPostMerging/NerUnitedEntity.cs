using System.Collections.Generic;

using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.NerPostMerging
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class NerUnitedEntity
    {
        private NerUnitedEntity() { }
        internal static bool TryCreate( DirectAccessList< (word_t w, int orderNum) > nerWords, in SearchResult sr, out NerUnitedEntity nue )
        {
            switch ( sr.Length )
            {
                case 0: case 1:
                    nue = default;
                return (false);

                case 2:
                    nue = new NerUnitedEntity()
                    { 
                        Word_1 = get_word( nerWords, sr.StartIndex     ),
                        Word_2 = get_word( nerWords, sr.StartIndex + 1 )
                    };
                return (true);

                case 3:
                    nue = new NerUnitedEntity()
                    {
                        Word_1 = get_word( nerWords, sr.StartIndex     ),
                        Word_2 = get_word( nerWords, sr.StartIndex + 1 ),
                        Word_3 = get_word( nerWords, sr.StartIndex + 2 )
                    };
                return (true);

                case 4:
                    nue = new NerUnitedEntity()
                    {
                        Word_1 = get_word( nerWords, sr.StartIndex     ),
                        Word_2 = get_word( nerWords, sr.StartIndex + 1 ),
                        Word_3 = get_word( nerWords, sr.StartIndex + 2 ),
                        Word_4 = get_word( nerWords, sr.StartIndex + 3 )
                    };
                return (true);

                case 5:
                    nue = new NerUnitedEntity()
                    {
                        Word_1 = get_word( nerWords, sr.StartIndex     ),
                        Word_2 = get_word( nerWords, sr.StartIndex + 1 ),
                        Word_3 = get_word( nerWords, sr.StartIndex + 2 ),
                        Word_4 = get_word( nerWords, sr.StartIndex + 3 ),
                        Word_5 = get_word( nerWords, sr.StartIndex + 4 )
                    };
                return (true);

                default:
                    nue = new NerUnitedEntity()
                    {
                        Word_1 = get_word( nerWords, sr.StartIndex     ),
                        Word_2 = get_word( nerWords, sr.StartIndex + 1 ),
                        Word_3 = get_word( nerWords, sr.StartIndex + 2 ),
                        Word_4 = get_word( nerWords, sr.StartIndex + 3 ),
                        Word_5 = get_word( nerWords, sr.StartIndex + 4 )
                    };
                    var a = new word_t[ sr.Length - 5 ];
                    for ( int j = 0, i = sr.StartIndex + 5, end = sr.EndIndex(); i < end; i++ )
                    {
                        a[ j++ ] = get_word( nerWords, i );
                    }
                    nue.Word_6_and_more = a;
                return (true);
            }
        }

        [M(O.AggressiveInlining)] private static word_t get_word( DirectAccessList< (word_t w, int orderNum) > lst, int idx )
        {
            ref readonly var t = ref lst._Items[ idx ];
            return (t.w);
        }

        public word_t Word_1 { get; private set; }
        public word_t Word_2 { get; private set; }
        public word_t Word_3 { get; private set; }
        public word_t Word_4 { get; private set; }
        public word_t Word_5 { get; private set; } 
        public IReadOnlyList< word_t > Word_6_and_more { get; private set; }
#if DEBUG
        private static string t( word_t w ) => ((w != null) ? $"; {w}" : null);
        public override string ToString() => $"{Word_1}; {Word_2}" + t( Word_3 ) + t( Word_4 ) + t( Word_5 ); 
#endif
    }
}