using System.Collections.Generic;

using Lingvo.NER.Rules.core.Infrastructure;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;
using NOT = Lingvo.NER.Rules.NerOutputType;

namespace Lingvo.NER.Rules.NerPostMerging
{
    /// <summary>
    ///
    /// </summary>
    internal static class NerPostMerger_v1
    {
        private const int MAX_DISTANCE_BETWEEN_ENTITIES_IN_SEP_WORDS = 7;

        private static Searcher_v1 _Searcher;
        static NerPostMerger_v1()
        {
            //-5-
            var ngrams = Permutator.GetPermutations( new[] { NOT.Name, NOT.PhoneNumber, NOT.Address, NOT.Url, NOT.AccountNumber, } );
                                                             //---NOT.CustomerNumber, NOT.Birthday, NOT.CreditCard, NOT.PassportIdCardNumber, NOT.CarNumber, NOT.HealthInsurance } ); > 3 000 000 !!!

            //-4-
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.PhoneNumber, NOT.Address, NOT.Url } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.PhoneNumber, NOT.Address, NOT.AccountNumber } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.PhoneNumber, NOT.Url, NOT.AccountNumber } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.Address    , NOT.Url, NOT.AccountNumber } ) );

            //-3-
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.PhoneNumber, NOT.Address } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.PhoneNumber, NOT.Url } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.PhoneNumber, NOT.AccountNumber } ) );

            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.Address, NOT.Url } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.Address, NOT.AccountNumber } ) );

            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.Url, NOT.AccountNumber } ) );

            //-2-
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.PhoneNumber } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.Address } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.Url } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.AccountNumber } ) );

            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.CustomerNumber } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.Birthday } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.CreditCard } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.PassportIdCardNumber } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.CarNumber } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.HealthInsurance } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.DriverLicense } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.SocialSecurity } ) );
            ngrams.AddRange( Permutator.GetPermutations( new[] { NOT.Name, NOT.TaxIdentification } ) );

            _Searcher = new Searcher_v1( ngrams, MAX_DISTANCE_BETWEEN_ENTITIES_IN_SEP_WORDS );
        }

        public static void Run( DirectAccessList< (word_t w, int orderNum) > nerWords, List< NerUnitedEntity_v1 > nerUnitedEntities )
        {
            if ( _Searcher.TryFindAll( nerWords, out var ss ) )
            {
                var prev_endIndex = - 1;
                foreach ( var sr in ss )
                {
                    if ( !sr.IsIntersectWith( prev_endIndex ) && NerUnitedEntity_v1.TryCreate( nerWords, in sr, out var nue ) )
                    {
                        nerUnitedEntities.Add( nue );

                        prev_endIndex = sr.EndIndex() - 1;
                    }
                }
            }
        }
        [M(O.AggressiveInlining)] private static bool IsIntersectWith( in this SearchResult_v1 sr, int index ) => (sr.StartIndex <= index) && (index <= sr.EndIndex() - 1);
    }
}
