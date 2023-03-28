using System;
using System.Collections.Generic;
using System.IO;

using Lingvo.NER.NeuralNetwork.Models;
using Lingvo.NER.NeuralNetwork.Text;

namespace Lingvo.NER.NeuralNetwork.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public enum SentTagsEnum
    {
        END = 0,
        START,
        UNK
    }

    /// <summary>
    /// 
    /// </summary>

    public class Vocab
    {
        public const int START_MEANING_INDEX = 3;

        private Dictionary< string, int > _WordToIndex;
        private Dictionary< int, string > _IndexToWord;
        private bool _IgnoreCase;

        public IReadOnlyCollection< string > Items => _WordToIndex.Keys;
        public int Count => _IndexToWord.Count;
        public bool IgnoreCase => _IgnoreCase;
        public Dictionary< string, int > _GetWordToIndex_() => _WordToIndex;
        public Dictionary< int, string > _GetIndexToWord_() => _IndexToWord;

        public static (Dictionary< string, int > wordToIndex, Dictionary< int, string > indexToWord, bool ignoreCase) CreateDicts( bool ignoreCase )
        {
            var wordToIndex = ignoreCase ? new Dictionary< string, int >( StringComparer.InvariantCultureIgnoreCase ) : new Dictionary< string, int >();
            var indexToWord = new Dictionary< int, string >();

            wordToIndex[ BuildInTokens.EOS ] = (int) SentTagsEnum.END;
            wordToIndex[ BuildInTokens.BOS ] = (int) SentTagsEnum.START;
            wordToIndex[ BuildInTokens.UNK ] = (int) SentTagsEnum.UNK;

            indexToWord[ (int) SentTagsEnum.END   ] = BuildInTokens.EOS;
            indexToWord[ (int) SentTagsEnum.START ] = BuildInTokens.BOS;
            indexToWord[ (int) SentTagsEnum.UNK   ] = BuildInTokens.UNK;

            return (wordToIndex, indexToWord, ignoreCase);
        }

        public Vocab( bool ignoreCase = false ) => (_WordToIndex, _IndexToWord, _IgnoreCase) = CreateDicts( ignoreCase );
        public Vocab( Vocab_4_ProtoBufSerializer v )
        {
            var wordToIndex = v.IgnoreCase ? new Dictionary< string, int >( v._GetWordToIndex_().Count, StringComparer.InvariantCultureIgnoreCase )
                                           : new Dictionary< string, int >( v._GetWordToIndex_().Count );
            foreach ( var p in v._GetWordToIndex_() )
            {
                wordToIndex[ p.Key ] = p.Value;
            }
            _WordToIndex = wordToIndex;
            _IndexToWord = v._GetIndexToWord_();
            _IgnoreCase = v.IgnoreCase;
        }
        public Vocab( in (Dictionary< string, int > wordToIndex, Dictionary< int, string > indexToWord, bool ignoreCase) t ) => (_WordToIndex, _IndexToWord, _IgnoreCase) = t;

        public IReadOnlyCollection< string > GetAllTokens( bool keepBuildInTokens = true )
        {
            if ( keepBuildInTokens )
            {
                return (Items);
            }
            else
            {
                var results = new List< string >( Items.Count );
                foreach ( var item in Items )
                {
                    if ( !BuildInTokens.IsPreDefinedToken( item ) )
                    {
                        results.Add( item );
                    }
                }
                return (results);
            }
        }

        /// <summary>
        /// Load vocabulary from given files
        /// </summary>
        public Vocab( string vocabFilePath, bool ignoreCase = false )
        {
            Logger.WriteLine( "Loading vocabulary files..." );

            (_WordToIndex, _IndexToWord, _IgnoreCase) = CreateDicts( ignoreCase );

            using var sr = new StreamReader( vocabFilePath );
            //Build word index for both source and target sides
            int q = START_MEANING_INDEX;
            for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
            {
                var idx  = line.IndexOf( '\t' );
                var word = (idx == -1) ? line : line.Substring( 0, idx );
                if ( string.IsNullOrEmpty( word ) ) continue;

                if ( !BuildInTokens.IsPreDefinedToken( word ) )
                {
                    _WordToIndex[ word ] = q;
                    _IndexToWord[ q    ] = word;
                    q++;
                }
            }
        }

        public void MergeVocab( Vocab srcVocab )
        {
            var maxId = 0;
            foreach ( var id in _WordToIndex.Values )
            {
                if ( id > maxId )
                {
                    maxId = id;
                }
            }

            maxId++;
            foreach ( var p in srcVocab._WordToIndex )
            {
                if ( !_WordToIndex.ContainsKey( p.Key ) )
                {
                    _WordToIndex.Add( p.Key, maxId );
                    _IndexToWord.Add( maxId, p.Key );
                    maxId++;
                }
            }
        }
        public void DumpVocab( string fileName )
        {
            using var sw = new StreamWriter( fileName, append: false );
            foreach ( var p in _IndexToWord )
            {
                sw.Write( p.Value );
                sw.Write( '\t' );
                sw.WriteLine( p.Key );
            }
        }

        public string GetString( int idx ) => _IndexToWord.TryGetValue( idx, out var letter ) ? letter : BuildInTokens.UNK;
        public List< string > ConvertIdsToString( IList< float > idxs )
        {
            var result = new List< string >( idxs.Count );
            foreach ( int idx in idxs )
            {
                if ( !_IndexToWord.TryGetValue( idx, out var letter ) )
                {
                    letter = BuildInTokens.UNK;
                }
                result.Add( letter );
            }
            return (result);
        }
        public string ConvertIdsToString( int idx )
        {
            if ( !_IndexToWord.TryGetValue( idx, out var letter ) )
            {
                letter = BuildInTokens.UNK;
            }
            return (letter);
        }
        public List< List< string > > ConvertIdsToString( List< List< int > > seqs )
        {
            var result = new List< List< string > >( seqs.Count );
            foreach ( var seq in seqs )
            {
                var r = new List< string >( seq.Count );
                foreach ( int idx in seq )
                {
                    if ( !_IndexToWord.TryGetValue( idx, out string letter ) )
                    {
                        letter = BuildInTokens.UNK;
                    }
                    r.Add( letter );
                }
                result.Add( r );
            }
            return result;
        }
        public List< List< List< string > > > ExtractTokens( List< List< BeamSearchStatus > > beam2batch2seq )
        {
            var result = new List< List< List< string > > >( beam2batch2seq.Count );
            foreach ( var batch2seq in beam2batch2seq )
            {
                var b = new List< List< string > >( batch2seq.Count );
                foreach ( var seq in batch2seq )
                {
                    var r = new List< string >( seq.OutputIds.Count );
                    foreach ( int idx in seq.OutputIds )
                    {
                        if ( !_IndexToWord.TryGetValue( idx, out string letter ) )
                        {
                            letter = BuildInTokens.UNK;
                        }
                        r.Add( letter );
                    }
                    b.Add( r );
                }
                result.Add( b );
            }
            return (result);
        }
        public int GetWordIndex( string word )
        {
            if ( !_WordToIndex.TryGetValue( word, out int id ) )
            {
                id = (int) SentTagsEnum.UNK;
            }
            return (id);
        }
        public bool ContainsWord( string word ) => _WordToIndex.ContainsKey( word );
        public List< List< int > > GetWordIndex( List< List< string > > seqs )
        {
            var result = new List< List< int > >( seqs.Count );
            foreach ( var seq in seqs )
            {
                var r = new List<int>( seq.Count );
                foreach ( var word in seq )
                {
                    if ( !_WordToIndex.TryGetValue( word, out int id ) )
                    {
                        id = (int) SentTagsEnum.UNK;
                    }
                    r.Add( id );
                }
                result.Add( r );
            }
            return (result);
        }
    }
}
