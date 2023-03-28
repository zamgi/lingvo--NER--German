using System.Collections.Generic;
using System.IO;

namespace Lingvo.NER.Rules.BankAccounts
{
    /// <summary>
    /// 
    /// </summary>
    public interface IBankAccountsModel
    {
        bool IsBankCode( string value );
        bool IsBankCode( int value );
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class BankAccountsModel : IBankAccountsModel
    {
        private Set< int > _BankCodes;
        public BankAccountsModel( in (string bankCodesFilename, int? capacity) t ) : this( t.bankCodesFilename, t.capacity ) { }
        public BankAccountsModel( string bankCodesFilename, int? capacity = null ) => _BankCodes = Init_BankCodes( bankCodesFilename, capacity );
        public BankAccountsModel( StreamReader sr, int? capacity = null ) => _BankCodes = Init_BankCodes( sr, capacity );

        #region [.Bank Codes.]
        private static Set< int > Init_BankCodes( string bankCodesFilename, int? capacity )
        {
            #region comm. [.binary format.]
            /*
            using ( var fs = File.OpenRead( bankCodes.Filename ) )
            using ( var br = new BinaryReader( fs ) )
            {
                var len = (int) (fs.Length / sizeof(int));

                _ZipCodes = new Set< int >( len );

                for ( int i = 0; i < len; i++ )
                {
                    var zipCode = br.ReadInt32();
                    _ZipCodes.Add( zipCode );
                }
            }
            */
            #endregion

            #region [.string format.]
            //*
            using ( var sr = new StreamReader( bankCodesFilename ) )
            {
                return (Init_BankCodes( sr, capacity ));
            }
            //*/
            #endregion
        }
        private static Set< int > Init_BankCodes( StreamReader sr, int? capacity )
        {
            var bankCodes = new Set< int >( capacity.GetValueOrDefault() );

            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                if ( int.TryParse( line, out var zipCode ) || int.TryParse( line.Trim(), out zipCode ) )
                {
                    bankCodes.Add( zipCode );
                }
            }

            return (bankCodes);
        }
        public bool IsBankCode( string value ) => int.TryParse( value, out var i ) && _BankCodes.Contains( i );
        public bool IsBankCode( int value ) => _BankCodes.Contains( value );
        #endregion
    }
}
