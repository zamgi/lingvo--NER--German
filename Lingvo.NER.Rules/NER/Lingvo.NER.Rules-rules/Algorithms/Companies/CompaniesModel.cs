using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.core.Infrastructure;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Companies
{
    /// <summary>
    /// 
    /// </summary>
    public interface IWcd_Find2Right
    {
        bool TryFind2Right( IList< word_t > words, int startIndex, out int length );
#if DEBUG
        IEnumerable< string > Visit();
        int TopLevelCount { get; }
        int Count { get; }
#endif
    }
    /// <summary>
    /// 
    /// </summary>
    public interface IWcd_Find2Left
    {
        bool TryFind2Left( IList< word_t > words, int startIndex, out int length );
#if DEBUG
        IEnumerable< string > Visit();
        int TopLevelCount { get; }
        int Count { get; }
#endif
    }
    /// <summary>
    /// 
    /// </summary>
    public interface IWcd_WithAppend : IWcd_Find2Right, IWcd_Find2Left
    {
        void Add( IList< word_t > words ); //, bool each_is_leaf = false );
        void Add( string word );
#if DEBUG
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        IEnumerable< string > Visit();
        int TopLevelCount { get; }
        int Count { get; }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal abstract class Wcd_Base< T > : IWcd_WithAppend, IWcd_Find2Right, IWcd_Find2Left 
        where T : Wcd_Base< T >, new()
    {
        private Map< string, Wcd_Base< T > > _Slots;
        private bool _IsLeaf;
        protected Wcd_Base( int capacity ) => _Slots = Map< string, Wcd_Base< T > >.CreateWithCloserCapacity( capacity );
        protected Wcd_Base() => _Slots = new Map< string, Wcd_Base< T > >();

        protected abstract string GetValue( word_t w );

        #region [.append words.]
        public void Add( IList< word_t > words ) //, bool each_is_leaf = false )
        {
            var idx   = 0;
            var count = words.Count;
            for ( Wcd_Base< T > _this = this, _this_next; ; )
            {
                #region [.save.]
                // every word may end the sentence
                //if ( each_is_leaf )
                //{
                //    _this._IsLeaf = true;
                //}
                if ( count == idx )
                {
                    _this._IsLeaf = true;
                    return;
                }
                #endregion

                var v = GetValue( words[ idx ] );
                if ( !_this._Slots.TryGetValue( v, out _this_next ) )
                {
                    //add next word in chain
                    _this_next = new T();
                    _this._Slots.Add( v, _this_next );
                }
                _this = _this_next;
                idx++;
            }
        }
        public void Add( string word )
        {
            if ( !_Slots.TryGetValue( word, out var next ) )
            {
                //add next word in chain
                next = new T() { _IsLeaf = true };
                _Slots.Add( word, next );
            }
            else
            {
                next._IsLeaf = true;
            }
        }
        #endregion

        #region [.try get.]
        [M(O.AggressiveInlining)] public bool TryFind2Right( IList< word_t > words, int startIndex, out int length )
        {
            length = default;

            var startIndex_saved = startIndex;
            var count            = words.Count;
            for ( Wcd_Base< T > _this = this, _this_next; ; )
            {
                #region [.get.]
                if ( _this._IsLeaf )
                {
                    length = (startIndex - startIndex_saved);
                }
                #endregion

                if ( count == startIndex )
                {
                    break;
                }

                var v = GetValue( words[ startIndex ] );
                if ( !_this._Slots.TryGetValue( v, out _this_next ) )
                {
                    break;
                }
                _this = _this_next;
                startIndex++;
            }

            return (length != 0);
        }
        [M(O.AggressiveInlining)] public bool TryFind2Left( IList< word_t > words, int startIndex, out int length )
        {
            length = default;

            var startIndex_saved = startIndex;
            for ( Wcd_Base< T > _this = this, _this_next; ; )
            {
                #region [.get.]
                if ( _this._IsLeaf )
                {
                    length = (startIndex_saved - startIndex);
                }
                #endregion

                if ( startIndex < 0 )
                {
                    break;
                }

                var v = GetValue( words[ startIndex ] );
                if ( (v == null) || !_this._Slots.TryGetValue( v, out _this_next ) )
                {
                    break;
                }
                _this = _this_next;
                startIndex--;
            }

            return (length != 0);
        }
        #endregion
#if DEBUG
        public override string ToString() => $"cnt: {_Slots.Count}{(_IsLeaf ? ", Leaf" : null)}";

        public int TopLevelCount => _Slots.Count;
        public int Count
        {
            get
            {
                var cnt = 0;
                CalcAllCount( this, ref cnt );
                return (cnt);
            }
        }
        private static void CalcAllCount( Wcd_Base< T > root, ref int cnt )
        {
            foreach ( var wcd in root._Slots.GetValues() )
            {
                if ( wcd._IsLeaf )
                {
                    cnt++;
                }

                CalcAllCount( wcd, ref cnt );
            }
        }

        public IEnumerable< string > Visit() => Visit( this );
        private static IEnumerable< string > Visit( Wcd_Base< T > root )
        {
            foreach ( var p in root._Slots )
            {
                var wcd = p.Value;
                if ( wcd._IsLeaf )
                {
                    yield return (p.Key);
                }

                foreach ( var text in Visit( wcd ) )
                {
                    yield return (p.Key + ' ' + text);
                }
            }
        }
#endif
    }
    /// <summary>
    /// 
    /// </summary>
    internal sealed class Wcd_CaseSensitive : Wcd_Base< Wcd_CaseSensitive >
    {
        public Wcd_CaseSensitive( int capacity ) : base( capacity ) { }
        public Wcd_CaseSensitive() : base() { }
        protected override string GetValue( word_t w ) => w.valueOriginal;
    }
    /// <summary>
    /// 
    /// </summary>
    internal sealed class Wcd_IgnoreCase : Wcd_Base< Wcd_IgnoreCase >
    {
        public Wcd_IgnoreCase( int capacity ) : base( capacity ) { }
        public Wcd_IgnoreCase() : base() { }
        protected override string GetValue( word_t w ) => w.valueUpper;
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ICompaniesModel
    {
        IWcd_Find2Right CompanyVocab         { get; }
        IWcd_Find2Right Prefixes             { get; }
        IWcd_Find2Right Suffixes             { get; }
        IWcd_Find2Left  PrefixesPrevSuffixes { get; }
        IWcd_Find2Left  ExpandPreambles      { get; }
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class CompaniesModel : ICompaniesModel
    {
        /// <summary>
        /// 
        /// </summary>
        public struct InputParams
        {
            public (string Filename, int? Capacity) CompanyVocab         { get; set; }
            public (string Filename, int? Capacity) Prefixes             { get; set; }
            public (string Filename, int? Capacity) Suffixes             { get; set; }
            public (string Filename, int? Capacity) PrefixesPrevSuffixes { get; set; }
            public (string Filename, int? Capacity) ExpandPreambles      { get; set; }            
        }
        /// <summary>
        /// 
        /// </summary>
        public struct InputParams_2
        {
            public (StreamReader Sr, int? Capacity) CompanyVocab         { get; set; }
            public (StreamReader Sr, int? Capacity) Prefixes             { get; set; }
            public (StreamReader Sr, int? Capacity) Suffixes             { get; set; }
            public (StreamReader Sr, int? Capacity) PrefixesPrevSuffixes { get; set; }
            public (StreamReader Sr, int? Capacity) ExpandPreambles      { get; set; }
        }

        #region [.ctor().]
        private IWcd_Find2Right _CompanyVocab;
        private IWcd_Find2Right _Prefixes;
        private IWcd_Find2Right _Suffixes;
        private IWcd_Find2Left  _PrefixesPrevSuffixes;
        private IWcd_Find2Left  _ExpandPreambles;
        public CompaniesModel( in InputParams p )
        {
            var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate();
            using ( tokenizer )
            {
                _CompanyVocab         = Init_CompanyVocab        ( p.CompanyVocab        , tokenizer, ignoreCase: true  );
                _Prefixes             = Init_Prefixes_Suffixes   ( p.Prefixes            , tokenizer, ignoreCase: false );
                _Suffixes             = Init_Prefixes_Suffixes   ( p.Suffixes            , tokenizer, ignoreCase: true, makeVariantWithCollapseWhitespaces: true );
                _PrefixesPrevSuffixes = Init_PrefixesPrevSuffixes( p.PrefixesPrevSuffixes, tokenizer, ignoreCase: false );
                _ExpandPreambles      = Init_PrefixesPrevSuffixes( p.ExpandPreambles     , tokenizer, ignoreCase: false );
            }
        }
        public CompaniesModel( in InputParams_2 p )
        {
            var tokenizer = Tokenizer.Create4NoSentsNoUrlsAllocate();
            using ( tokenizer )
            {
                _CompanyVocab         = Init_CompanyVocab        ( p.CompanyVocab        .Sr, p.CompanyVocab        .Capacity, tokenizer, ignoreCase: true  );
                _Prefixes             = Init_Prefixes_Suffixes   ( p.Prefixes            .Sr, p.Prefixes            .Capacity, tokenizer, ignoreCase: false );
                _Suffixes             = Init_Prefixes_Suffixes   ( p.Suffixes            .Sr, p.Suffixes            .Capacity, tokenizer, ignoreCase: true, makeVariantWithCollapseWhitespaces: true );
                _PrefixesPrevSuffixes = Init_PrefixesPrevSuffixes( p.PrefixesPrevSuffixes.Sr, p.PrefixesPrevSuffixes.Capacity, tokenizer, ignoreCase: false );
                _ExpandPreambles      = Init_PrefixesPrevSuffixes( p.ExpandPreambles     .Sr, p.ExpandPreambles     .Capacity, tokenizer, ignoreCase: false );
            }
        }
        #endregion

        #region [.Init.]
        private static IWcd_Find2Right Init_CompanyVocab( in (string Filename, int? Capacity) p, Tokenizer tokenizer, bool ignoreCase )
        {
            using ( var sr = new StreamReader( p.Filename ) )
            {
                return (Init_CompanyVocab( sr, p.Capacity, tokenizer, ignoreCase ));
            }
        }
        unsafe private static IWcd_Find2Right Init_CompanyVocab( StreamReader sr, int? capacity, Tokenizer tokenizer, bool ignoreCase )
        {
            IWcd_WithAppend wcd = ignoreCase ? new Wcd_IgnoreCase   ( capacity.GetValueOrDefault() )
                                             : new Wcd_CaseSensitive( capacity.GetValueOrDefault() );
            var ph             = new NerProcessorHelper( tokenizer.InputTypeProcessor );
            var vc             = new VersionCombiner< string >();
            var buf            = new StringBuilder();
            var dashes         = xlat.GetHyphens< char[] >();
            var dashes_str     = dashes.Select( c => c.ToString() ).ToArray();
            var can_miss_chars = new[] { '.', '&', '"', '\'', '!' };
            //var dashes__and__can_miss_chars = dashes.Concat( can_miss_chars ).ToArray();
            var cvc            = new  MissCharsVersionCombiner( can_miss_chars );
#if DEBUG
            var has_dashes___and___has_can_miss_chars = 0; 
#endif

            [M(O.AggressiveInlining)] void process_core( string line )
            {
                //1.
                var tokens = tokenizer.Run_NoSentsNoUrlsAllocate( line );
                wcd.Add( tokens );

                var last_ch = tokens.Last().valueOriginal.LastChar();
                if ( last_ch == '.' )
                {
                    tokens = tokenizer.Run_NoSentsNoUrlsAllocate( line + " 1234567" ).RemoveLast();
                    wcd.Add( tokens );
                }

                //2.
                var cnt = tokens.Count;
                ph.UnstuckNumbersFromOthers__NEW( tokens );
                if ( cnt != tokens.Count )
                {
                    wcd.Add( tokens );
                }

                //3.
                if ( ph.TrimEndsPunctuationsFromOthers( tokens ) )
                {
                    wcd.Add( tokens );
                }
            };
            [M(O.AggressiveInlining)] void process( string line )
            {
                process_core( line );

                var has_dashes         = line.ContainsAny( dashes );
                var has_can_miss_chars = line.ContainsAny( can_miss_chars );

                if ( has_dashes )
                {
                    if ( has_can_miss_chars )
                    {
#if DEBUG
                        has_dashes___and___has_can_miss_chars++; 
#endif
                        var array = line.Split( dashes, StringSplitOptions.RemoveEmptyEntries );

                        var line_2 = array.JoinWithSep( buf );
                        process_core( line_2 );
                        foreach ( var arr_2 in cvc.GetVersions( line_2 ) )
                        {
                            process_core( arr_2.JoinWithSep( buf ) );
                            process_core( arr_2.JoinWithTrimWhitespaces( buf ) );
                        }

                        foreach ( var dash in dashes_str )
                        {
                            foreach ( var arr in vc.GetVersions( array, dash, returnOriginArray: false ) )
                            {
                                line_2 = arr.JoinWithSep( buf );
                                foreach ( var arr_2 in cvc.GetVersions( line_2 ) )
                                {
                                    process_core( arr_2.JoinWithSep( buf ) );
                                    process_core( arr_2.JoinWithTrimWhitespaces( buf ) );
                                }

                                line_2 = arr.JoinWithTrimWhitespaces( buf );
                                foreach ( var arr_2 in cvc.GetVersions( line_2 ) )
                                {
                                    process_core( arr_2.JoinWithSep( buf ) );
                                    process_core( arr_2.JoinWithTrimWhitespaces( buf ) );
                                }
                            }
                        }
                    }
                    else
                    {
                        var array = line.Split( dashes, StringSplitOptions.RemoveEmptyEntries );

                        process_core( array.JoinWithSep( buf ) );

                        foreach ( var dash in dashes_str )
                        {
                            foreach ( var arr in vc.GetVersions( array, dash, returnOriginArray: false ) )
                            {
                                process_core( arr.JoinWithSep( buf ) );
                            }
                        }
                    }
                }
                else if ( has_can_miss_chars )
                {
                    foreach ( var arr in cvc.GetVersions( line ) )
                    {
                        process_core( arr.JoinWithSep( buf ) );
                        process_core( arr.JoinWithTrimWhitespaces( buf ) );
                    }
                }

                #region comm. prev.
                /*
                #region [.dashes.]
                if ( line.ContainsAny( dashes ) )
                {
                    var array = line.Split( dashes, StringSplitOptions.RemoveEmptyEntries );

                    process_core( array.JoinWithSep( buf ) );

                    foreach ( var dash in dashes_str )
                    {
                        foreach ( var arr in vc.GetVersions( array, dash, returnOriginArray: false ) )
                        {
                            process_core( arr.JoinWithSep( buf ) );
                        }
                    }
                }
                #endregion

                #region [.can_miss_chars.]
                if ( line.ContainsAny( can_miss_chars ) )
                {
                    foreach ( var arr in cvc.GetVersions( line ) )
                    {
                        process_core( arr.JoinWithSep( buf ) );
                        process_core( arr.JoinWithTrimWhitespaces( buf ) );
                    }
                }
                #endregion
                //*/
                #endregion
            };

            var line_2 = default(string);
            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                if ( line.IsNullOrWhiteSpace() || (line[ 0 ] == '#') ) continue;

                process( line );
                if ( tokenizer.TryNormalizeUmlautes( line, ref line_2 ) )
                {
                    process( line_2 );
                }
            }
#if DEBUG
            //---var __vocab_content__ = wcd.Visit().ToList( wcd.Count ).SortEx();
#endif
            return (wcd);
        }

        private static IWcd_Find2Right Init_Prefixes_Suffixes( in (string Filename, int? Capacity) p, Tokenizer tokenizer, bool ignoreCase, bool makeVariantWithCollapseWhitespaces = false )
        {
            using ( var sr = new StreamReader( p.Filename ) )
            {
                return (Init_Prefixes_Suffixes( sr, p.Capacity, tokenizer, ignoreCase, makeVariantWithCollapseWhitespaces ));
            }
        }
        private static IWcd_Find2Right Init_Prefixes_Suffixes( StreamReader sr, int? capacity, Tokenizer tokenizer, bool ignoreCase, bool makeVariantWithCollapseWhitespaces = false )
        {
            IWcd_WithAppend wcd = ignoreCase ? new Wcd_IgnoreCase   ( capacity.GetValueOrDefault() )
                                             : new Wcd_CaseSensitive( capacity.GetValueOrDefault() );
            var buf = new StringBuilder();

            //local method
            [M(O.AggressiveInlining)] void process( string line )
            {
                if ( StringsHelper.ContainsOnlyLetters( line ) )
                {
                    if ( ignoreCase ) StringsHelper.ToUpperInvariantInPlace( line );
                    wcd.Add( line );
                }
                else
                {
                    #region [-1-]
                    var tokens = tokenizer.Run_NoSentsNoUrlsAllocate( line );
                    wcd.Add( tokens );

                    var last_ch = tokens.Last().valueOriginal.LastChar();
                    if ( last_ch == '.' )
                    {
                        tokens = tokenizer.Run_NoSentsNoUrlsAllocate( line + " 1234567" ).RemoveLast();
                        wcd.Add( tokens );
                    }
                    #endregion

                    if ( makeVariantWithCollapseWhitespaces && line.ContainsInBetween(' ') )
                    {
                        var line_2 = line.RemoveWhitespaces( buf );
                        tokens = tokenizer.Run_NoSentsNoUrlsAllocate( line_2 );
                        wcd.Add( tokens );

                        if ( 1 < tokens.Count )
                        {
                            buf.Clear();
                            foreach ( var t in tokens )
                            {
                                buf.Append( (ignoreCase ? t.valueUpper : t.valueOriginal) );
                            }
                            wcd.Add( buf.ToString() );
                        }
                    }
                }
            };

            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                if ( line.IsNullOrWhiteSpace() ) continue;

                process( line );
                process( tokenizer.NormalizeUmlautes( line ) );
            }

            return (wcd);
        }

        private static IWcd_Find2Left Init_PrefixesPrevSuffixes( in (string Filename, int? Capacity) p, Tokenizer tokenizer, bool ignoreCase )
        {
            using ( var sr = new StreamReader( p.Filename ) )
            {
                return (Init_PrefixesPrevSuffixes( sr, p.Capacity, tokenizer, ignoreCase ));
            }
        }
        private static IWcd_Find2Left Init_PrefixesPrevSuffixes( StreamReader sr, int? capacity, Tokenizer tokenizer, bool ignoreCase )
        {
            IWcd_WithAppend wcd = ignoreCase ? new Wcd_IgnoreCase   ( capacity.GetValueOrDefault() )
                                             : new Wcd_CaseSensitive( capacity.GetValueOrDefault() );

            //local method
            [M(O.AggressiveInlining)] void process_line_reverse( string line )
            {
                if ( StringsHelper.ContainsOnlyLetters( line ) )
                {
                    if ( ignoreCase ) StringsHelper.ToUpperInvariantInPlace( line );
                    wcd.Add( line );
                }
                else
                {
                    #region [-1-]
                    var tokens = tokenizer.Run_NoSentsNoUrlsAllocate( line );

                    var last_ch_is_dot = (tokens.Last().valueOriginal.LastChar() == '.');

                    tokens.Reverse();
                    wcd.Add( tokens );

                    if ( last_ch_is_dot )
                    {
                        tokens = tokenizer.Run_NoSentsNoUrlsAllocate( line + " 1234567" ).RemoveLast();
                        tokens.Reverse();
                        wcd.Add( tokens );
                    }
                    #endregion
                }
            };

            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                if ( line.IsNullOrWhiteSpace() ) continue;

                process_line_reverse( line );
                process_line_reverse( tokenizer.NormalizeUmlautes( line ) );
            }

            return (wcd);
        }
        #endregion

        public IWcd_Find2Right CompanyVocab         { [M(O.AggressiveInlining)] get => _CompanyVocab; }
        public IWcd_Find2Right Prefixes             { [M(O.AggressiveInlining)] get => _Prefixes; }
        public IWcd_Find2Right Suffixes             { [M(O.AggressiveInlining)] get => _Suffixes; }
        public IWcd_Find2Left  PrefixesPrevSuffixes { [M(O.AggressiveInlining)] get => _PrefixesPrevSuffixes; }
        public IWcd_Find2Left  ExpandPreambles      { [M(O.AggressiveInlining)] get => _ExpandPreambles; }
    }
}
