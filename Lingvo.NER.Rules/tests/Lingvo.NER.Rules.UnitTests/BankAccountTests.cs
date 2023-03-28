using System.Collections.Generic;
using System.Linq;

using Xunit;

using Lingvo.NER.Rules.BankAccounts;
using Lingvo.NER.Rules.tokenizing;
using BA = Lingvo.NER.Rules.BankAccounts.BankAccountTypeEnum;
using NT = Lingvo.NER.Rules.NerOutputType;

namespace Lingvo.NER.Rules.tests.BankAccounts
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class BankAccountTests : NerTestsBase
    {
        [Fact] public void T_1()
        {
            using var np = CreateNerProcessor();
             
            np.AT( @"Kontoinhaber: Thorsten Fehr Kontonummer: 9161183273
Geldinstitut: Sparkasse KölnBonn Bankleitzahl: 37050198
IBAN: DE91 3705 0198 0061 1832 73 BIC: COLSDE33XXX",
                    new[] { (NT.AccountNumber, BA.BankCode_AccountNumber, "37050198", "9161183273", "Sparkasse KölnBonn", "Thorsten Fehr"),
                            (NT.AccountNumber, BA.IBAN, "3705 0198".NoWhitespace(), "0061 1832 73".NoWhitespace(), null, null) } );

            np.AT( @"Kontoinhaber: Thorsten Fehr Kontonummer: 9161183273
Geldinstitut: Sparkasse KölnBonn Bankleitzahl: 37050198
IBAN: DE91 3705 0198 0061 1832 73 BIC: COLSDE33XXX

Kontoinhaber: Thorsten Fehr Kontonummer: 9161183273
Geldinstitut: Sparkasse KölnBonn Bankleitzahl: 37050198
IBAN: DE91 3705 0198 0061 1832 73 BIC: COLSDE33XXX",
                    new[] { (NT.AccountNumber, BA.BankCode_AccountNumber, "37050198", "9161183273", "Sparkasse KölnBonn", "Thorsten Fehr"),
                            (NT.AccountNumber, BA.IBAN, "3705 0198".NoWhitespace(), "0061 1832 73".NoWhitespace(), null, null),
                            (NT.AccountNumber, BA.BankCode_AccountNumber, "37050198", "9161183273", "Sparkasse KölnBonn", "Thorsten Fehr"),
                            (NT.AccountNumber, BA.IBAN, "3705 0198".NoWhitespace(), "0061 1832 73".NoWhitespace(), null, null) } );

            np.AT( @"IBAN: DE91370501980061183273 BIC: COLSDE33XXX", (NT.AccountNumber, BA.IBAN, "37050198", "0061183273", null, null) );
            np.AT( @"IBAN DE91370501980061183273 BIC: COLSDE33XXX", (NT.AccountNumber, BA.IBAN, "37050198", "0061183273", null, null) );
        }

        [Fact] public void T_2()
        {
            using var np = CreateNerProcessor();
             
            np.AT( "Postbank Stuttgart (BLZ 60010070), Kto.-Nr. 385550708", (NT.AccountNumber, BA.BankCode_AccountNumber, "60010070", "385550708", null, null) );
            np.AT( "UniCredit Bank · IBAN DE55 3702 0090 0003 7512 10", (NT.AccountNumber, BA.IBAN, "3702 0090".NoWhitespace(), "0003 7512 10".NoWhitespace(), null, null) );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static void AT( this NerProcessor np, string text, (NT nerOutputType, BA bat, string bankCode, string accountNumber, string bankName, string accountOwner) p ) => np.AT( text, new[] { p } );
        public static void AT( this NerProcessor np, string text, IList< (NT nerOutputType, BA bat, string bankCode, string accountNumber, string bankName, string accountOwner) > refs ) => np.Run_UseSimpleSentsAllocate_v1( text ).Check( refs );

        public static void Check( this IList< word_t > words, IList< (NT nerOutputType, BA bat, string bankCode, string accountNumber, string bankName, string accountOwner) > refs )
        {
            var hyps = (from w in words
                        where (w.nerOutputType == NT.AccountNumber)
                        let b = (BankAccountWord) w
                        select (b.nerOutputType, b.BankAccountType, b.BankCode, b.AccountNumber, b.BankName, b.AccountOwner)
                       ).ToArray();

            var startIndex = 0;
            foreach ( var p in refs )
            {
                startIndex = hyps.IndexOf( in p, startIndex );
                if ( startIndex == -1 )
                {
                    Assert.True( false );
                }
                startIndex++;
            }
            Assert.True( 0 < startIndex );
        }

        private static int IndexOf( this IList< (NT nerOutputType, BA bat, string bankCode, string accountNumber, string bankName, string accountOwner) > pairs,
                                             in (NT nerOutputType, BA bat, string bankCode, string accountNumber, string bankName, string accountOwner) p,
                                             int startIndex )
        {
            for ( var len = pairs.Count; startIndex < len; startIndex++ )
            {
                if ( IsEqual( p, pairs[ startIndex ] ) )
                {
                    return (startIndex);
                }
            }
            return (-1);
        }
        private static bool IsEqual( in (NT nerOutputType, BA bankAccountType, string bankCode, string accountNumber, string bankName, string accountOwner) x,
                                     in (NT nerOutputType, BA bankAccountType, string bankCode, string accountNumber, string bankName, string accountOwner) y )
            => (x.nerOutputType == y.nerOutputType) && (x.bankAccountType == y.bankAccountType) &&
               (x.bankCode == y.bankCode) && (x.accountNumber == y.accountNumber) &&
               (x.bankName == y.bankName) && (x.accountOwner == y.accountOwner);
    }
}
