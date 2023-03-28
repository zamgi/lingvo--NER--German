using System;
using System.Runtime.InteropServices;

namespace Lingvo.NER.Rules.crfsuite
{   
	/// <summary>
    /// N-грамма
	/// </summary>
    unsafe public sealed class CRFNgram : IDisposable
    {
        private readonly GCHandle _GCHandle;
        private char*             _AttributesHeaderBase;

		/// <summary>
		/// .ctor()
		/// </summary>
        /// <param name="attributes">Составные части N-граммы</param>
		public CRFNgram( CRFAttribute[] attributes )
        {
            Attributes = attributes;

            var attrs_len = Attributes.Length;
            switch ( attrs_len )
            {
                case 1:
                #region
                {
                    Attribute_0 = Attributes[ 0 ];
                    //CRFAttributes  = null;

                    AttributesHeader = Attribute_0.AttributeName + "[" + Attribute_0.Position + ']' + '=';

                }
                #endregion
                break;

                case 2:
                #region
                {
                    Attribute_0 = Attributes[ 0 ];
                    Attribute_1 = Attributes[ 1 ];
                    //CRFAttributes  = null;

                    AttributesHeader = Attribute_0.AttributeName + "[" + Attribute_0.Position + ']' + '|' +
                                       Attribute_1.AttributeName + "[" + Attribute_1.Position + ']' + '=';
                }
                #endregion
                break;

                case 3:
                #region
                {
                    Attribute_0 = Attributes[ 0 ];
                    Attribute_1 = Attributes[ 1 ];
                    Attribute_2 = Attributes[ 2 ];
                    //CRFAttributes  = null;

                    AttributesHeader = Attribute_0.AttributeName + "[" + Attribute_0.Position + ']' + '|' +
                                       Attribute_1.AttributeName + "[" + Attribute_1.Position + ']' + '|' +
                                       Attribute_2.AttributeName + "[" + Attribute_2.Position + ']' + '=';
                }
                #endregion
                break;

                case 4:
                #region
                {
                    Attribute_0 = Attributes[ 0 ];
                    Attribute_1 = Attributes[ 1 ];
                    Attribute_2 = Attributes[ 2 ];
                    Attribute_3 = Attributes[ 3 ];
                    //CRFAttributes  = null;

                    AttributesHeader = Attribute_0.AttributeName + "[" + Attribute_0.Position + ']' + '|' +
                                       Attribute_1.AttributeName + "[" + Attribute_1.Position + ']' + '|' +
                                       Attribute_2.AttributeName + "[" + Attribute_2.Position + ']' + '|' +
                                       Attribute_3.AttributeName + "[" + Attribute_3.Position + ']' + '=';
                }
                #endregion
                break;

                case 5:
                #region
                {
                    Attribute_0 = Attributes[ 0 ];
                    Attribute_1 = Attributes[ 1 ];
                    Attribute_2 = Attributes[ 2 ];
                    Attribute_3 = Attributes[ 3 ];
                    Attribute_4 = Attributes[ 4 ];
                    //CRFAttributes  = null;

                    AttributesHeader = Attribute_0.AttributeName + "[" + Attribute_0.Position + ']' + '|' +
                                       Attribute_1.AttributeName + "[" + Attribute_1.Position + ']' + '|' +
                                       Attribute_2.AttributeName + "[" + Attribute_2.Position + ']' + '|' +
                                       Attribute_3.AttributeName + "[" + Attribute_3.Position + ']' + '|' +
                                       Attribute_4.AttributeName + "[" + Attribute_4.Position + ']' + '=';
                }
                #endregion
                break;

                case 6:
                #region
                {
                    Attribute_0 = Attributes[ 0 ];
                    Attribute_1 = Attributes[ 1 ];
                    Attribute_2 = Attributes[ 2 ];
                    Attribute_3 = Attributes[ 3 ];
                    Attribute_4 = Attributes[ 4 ];
                    Attribute_5 = Attributes[ 5 ];
                    //CRFAttributes  = null;

                    AttributesHeader = Attribute_0.AttributeName + "[" + Attribute_0.Position + ']' + '|' +
                                       Attribute_1.AttributeName + "[" + Attribute_1.Position + ']' + '|' +
                                       Attribute_2.AttributeName + "[" + Attribute_2.Position + ']' + '|' +
                                       Attribute_3.AttributeName + "[" + Attribute_3.Position + ']' + '|' +
                                       Attribute_4.AttributeName + "[" + Attribute_4.Position + ']' + '|' +
                                       Attribute_5.AttributeName + "[" + Attribute_5.Position + ']' + '=';
                }
                #endregion
                break;

                default:
                #region
                {
                    for ( var j = 0; j < attrs_len; j++ )
                    {
                        var attr = Attributes[ j ];

                        AttributesHeader += attr.AttributeName + "[" + attr.Position + ']' + '|';
                    }
                    AttributesHeader = AttributesHeader.Remove( AttributesHeader.Length - 1 ) + '=';
                }
                #endregion
                break;
            }

            AttributesLength    = attrs_len;
            AttributesHeaderLength = AttributesHeader.Length;

            _GCHandle = GCHandle.Alloc( AttributesHeader, GCHandleType.Pinned );
            _AttributesHeaderBase = (char*) _GCHandle.AddrOfPinnedObject().ToPointer();
        }
        ~CRFNgram()
        {
            if ( _AttributesHeaderBase != null )
            {
                _GCHandle.Free();
                _AttributesHeaderBase = null;
            }
        }
        public void Dispose()
        {
            if ( _AttributesHeaderBase != null )
            {
                _GCHandle.Free();
                _AttributesHeaderBase = null;
            }
            GC.SuppressFinalize( this );
        }

        /// <summary>
        /// Составные части N-граммы
        /// </summary>
        public readonly CRFAttribute[] Attributes;
        /// <summary>
        /// 
        /// </summary>
        public readonly int AttributesLength;

        public CRFAttribute Attribute_0 { get; }
        public CRFAttribute Attribute_1 { get; }
        public CRFAttribute Attribute_2 { get; }
        public CRFAttribute Attribute_3 { get; }
        public CRFAttribute Attribute_4 { get; }
        public CRFAttribute Attribute_5 { get; }

        public readonly string AttributesHeader;
        public readonly int    AttributesHeaderLength;
        unsafe public char* CopyAttributesHeaderChars( char* attributeBuffer )
        {
            for ( var ptr = _AttributesHeaderBase; ; ptr++ )
            {
                var ch = *ptr;
                if ( ch == '\0' )
                    break;
                *(attributeBuffer++) = ch;
            }
            return (attributeBuffer);
        }
        unsafe public byte* CopyAttributesHeaderChars( byte* attributeBuffer )
        {
            for ( var ptr = _AttributesHeaderBase; ; ptr++ )
            {
                var ch = *ptr;
                if ( ch == '\0' )
                    break;
                *(attributeBuffer++) = (byte) ch;
            }
            return (attributeBuffer);
        }

        public bool CanTemplateBeApplied( int wordIndex, int wordsCount )
        {
            foreach ( CRFAttribute attr in Attributes )
            {
                int index = wordIndex + attr.Position;
                if ( (index < 0) || (wordsCount <= index) )
                {
                    return (false);
                }
            }
            return (true);
        }
#if DEBUG
        public override string ToString() => AttributesHeader; 
#endif
    }
}