using System.Collections.Generic;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Algorithms
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class BracketBalancer
    {
        /// <summary>
        /// 
        /// </summary>
        public enum OpenCloseTypeEnum
        {
            Open, //Left
            Close, //Right,
            OpenAndClose,
        }
        /// <summary>
        /// 
        /// </summary>
        public readonly struct BracketTuple
        {
            public BracketTuple( char ch, OpenCloseTypeEnum openCloseType, int bracketType ) => (Char, OpenCloseType, BracketType) = (ch, openCloseType, bracketType);
            public char              Char          { [M(O.AggressiveInlining)] get; }
            public OpenCloseTypeEnum OpenCloseType { [M(O.AggressiveInlining)] get; }
            public int               BracketType   { [M(O.AggressiveInlining)] get; }
#if DEBUG
            public override string ToString() => $"'{Char}', {OpenCloseType}, {BracketType}"; 
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        public readonly struct ParseError
        {
            public ParseError( in BracketTuple t, int idx ) => (Tuple, SourceIndex) = (t, idx);
            public BracketTuple Tuple       { [M(O.AggressiveInlining)] get; }
            public int          SourceIndex { [M(O.AggressiveInlining)] get; }
#if DEBUG
            public override string ToString() => $"{Tuple}, index: {SourceIndex}"; 
#endif
        }

        #region [.ctor().]
        private Dictionary< char, BracketTuple > _Dict;
        private Stack< ParseError >     _BracketStack;
        private Dictionary< char, int > _OpenAndCloseCountDict;

        public BracketBalancer( IList< (char leftBracket, char rightBracket) > brackets )
        {
            _Dict = new Dictionary< char, BracketTuple >( brackets.Count );
            var bracketType = 0;
            foreach ( var t in brackets )
            {
                if ( t.leftBracket == t.rightBracket )
                {
                    var pt = new BracketTuple( t.leftBracket, OpenCloseTypeEnum.OpenAndClose, bracketType );
                    _Dict.Add( pt.Char, pt );
                }
                else
                {
                    var pt = new BracketTuple( t.leftBracket, OpenCloseTypeEnum.Open, bracketType );
                    _Dict.Add( pt.Char, pt );

                    pt = new BracketTuple( t.rightBracket, OpenCloseTypeEnum.Close, bracketType );
                    _Dict.Add( pt.Char, pt );
                }
                bracketType++;
            }

            _BracketStack          = new Stack< ParseError >();
            _OpenAndCloseCountDict = new Dictionary< char, int >();
        }
        #endregion

        public void Reset()
        {
            _BracketStack.Clear();
            _OpenAndCloseCountDict.Clear();
        }
        public bool Process( char ch, int outerIndex, out ParseError error )
        {
            if ( _Dict.TryGetValue( ch, out var t ) )
            {
                switch ( t.OpenCloseType )
                {
                    case OpenCloseTypeEnum.Open:
                        _BracketStack.Push( new ParseError( in t, outerIndex ) );
                    break;

                    case OpenCloseTypeEnum.Close:
                        if ( _BracketStack.Count == 0 )
                        {
                            error = new ParseError( in t, outerIndex );
                            return (false);
                        }
                        var err = _BracketStack.Pop();
                        if ( err.Tuple.BracketType != t.BracketType )
                        {
                            error = err;
                            return (false);
                        }
                    break;

                    case OpenCloseTypeEnum.OpenAndClose:                            
                        if ( !_OpenAndCloseCountDict.TryGetValue( ch, out var count ) || ((count % 2) == 0) )
                        {
                            _OpenAndCloseCountDict[ ch ] = count + 1;
                            _BracketStack.Push( new ParseError( in t, outerIndex ) );
                        }
                        else
                        {
                            err = _BracketStack.Pop();
                            if ( err.Tuple.BracketType != t.BracketType )
                            {
                                error = err;
                                return (false);
                            }
                            _OpenAndCloseCountDict[ ch ] = count - 1;
                        }
                    break;
                }
            }

            error = default;
            return (true);
        }
        public bool TryGetFirstError( out ParseError error )
        {
            if ( _BracketStack.Count != 0 )
            {
                error = _BracketStack.Pop();
                return (true);
            }
            error = default;
            return (false);
        }
    }
}
