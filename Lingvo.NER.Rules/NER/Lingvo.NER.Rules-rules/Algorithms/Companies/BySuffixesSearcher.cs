using System;
using System.Collections.Generic;

using Lingvo.NER.Rules.core;
using Lingvo.NER.Rules.tokenizing;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace Lingvo.NER.Rules.Companies
{
    /// <summary>
    /// 
    /// </summary>
    internal struct BySuffixesSearcher
    {
        #region [.cctor().]
        private const int UPPER_ABBREVIATION_MIN_LEN = 2;

        private static Set< string > _AllowedNonFirstUpperBetweenExpandPreambles;
        static BySuffixesSearcher()
        {
            var ps = new[]
            {
                "für", "zur", "zum", "am", "an"
            };
            _AllowedNonFirstUpperBetweenExpandPreambles = new Set< string >( ps, ps.Length );
        }
        #endregion

        #region [.ctor().]
        private IWcd_Find2Right _Suffixes;
        private int             _MaxDistance;
        private IWcd_Find2Left  _PrefixesPrevSuffixes;
        private IWcd_Find2Left  _ExpandPreambless;
        public BySuffixesSearcher( int maxDistance, IWcd_Find2Right suffixes, IWcd_Find2Left prefixesPrevSuffixes, IWcd_Find2Left expandPreambles )
        {
            _MaxDistance          = maxDistance;
            _Suffixes             = suffixes;            
            _PrefixesPrevSuffixes = prefixesPrevSuffixes;
            _ExpandPreambless     = expandPreambles;
        }
        #endregion

        #region [.descr.]
        /*3. Если на удалении 1-4 слов слева от окончания находится одно из слов следующего списка:
            Deutsche
            Deutsches
            Mitteldeutsche
            Hallenbad
            Theater
            Internationale
            Kulturregion
            Tourismus
            Stadtwerke
            Kommunale
            Hotel
            (список будет расширяться)
        и все слова пишутся с заглавной буквы (исключения – «für», «zur», «zum», «am», «an»), то все эти слова являются частью названия фирмы. 
        Примеры:
            Mitteldeutsche Medienförderung GmbH
            Westerwald Gästeservice e.V.
            Hallenbad Diez-Limburg GmbH
            Internationale Bodensee -  Tourismus GmbH
            Kulturregion Frankfurt RheinMain gGmbH
            Theater Lübeck gGmbH
            Deutsches Zentrum für Altersfragen e.V.
            Deutsches Institut für Entwicklungspolitik gGmbH
            Deutsche Bahn AG
            Stadtwerke Duisburg AG
            Hotel am Badersee ABG GmbH
        */
        #endregion
        [M(O.AggressiveInlining)] private bool TryFindByPreambles( List< word_t > words, int startIndex, out int length )
        {
            var has_AllowedNonFirstUpper = false;
            var last_preamble_idx = -1;
            var last_preamble_len = 0;
            for ( int i = startIndex, end_i = Math.Max( 0, i - _MaxDistance + 1 ); end_i <= i; i-- )
            {
                var w = words[ i ];
                if ( !w.IsOutputTypeOther() ) break;

                var v = w.valueOriginal;
                if ( v == null ) break;

                if ( !w.IsFirstLetterUpper() )
                {
                    if ( has_AllowedNonFirstUpper ) break;
                    if ( _AllowedNonFirstUpperBetweenExpandPreambles.Contains( v ) )
                    {
                        has_AllowedNonFirstUpper = true;
                    }
                }
                else if ( _ExpandPreambless.TryFind2Left( words, i, out var preamble_len ) )
                {
                    last_preamble_idx = i; last_preamble_len = preamble_len;
                }
                else if ( _Suffixes.Contains( words, i ) )
                {
                    if ( last_preamble_idx != -1 )
                    {
                        break;
                    }
                    length = default;
                    return (false);
                }
            }
            if ( last_preamble_idx != -1 )
            {
                length = (startIndex - last_preamble_idx);
                if ( length != 0 )
                {
                    length += last_preamble_len;
                    return (true);
                }
            }
            length = default;
            return (false);
        }

        #region [.descr.]
        /*Если перед окончанием „mbH“ идет слово, содержащее слово „gesellschaft“, то и предыдущее слово (с заглавной буквы) тоже является частью названия:
            "Frankenförder Forschungsgesellschaft mbH"
        */
        #endregion
        [M(O.AggressiveInlining)] private bool TryFindBySpecEnding( List< word_t > words, int startIndex, out int length )
        {
            const string SPEC        = "mbH";
            const string SPEC_ENDING = "gesellschaft";

            if ( (0 < startIndex) && (words[ startIndex + 1 ].valueOriginal == SPEC) )
            {
                var w = words[ startIndex ];
                if ( w.valueOriginal != null )
                {
                    var i = w.length - SPEC_ENDING.Length;
                    if ( (0 < i) && StringsHelper.IsEqual( w.valueOriginal, i, SPEC_ENDING ) )
                    {
                        w = words[ startIndex - 1 ];
                        if ( (w.valueOriginal != null) && w.IsFirstLetterUpper() )
                        {
                            length = 2;
                            return (true);
                        }                        
                    }
                }
            }
            length = default;
            return (false);
        }

        #region [.descr.]
        /*Предшедствующее (суффиксу/нескольким_суффиксам) слово (с заглавной буквы)(нет - любое слово) при этом есть часть названия, примеры:
            Bayer AG
            zamgi GmbH
            databyte GmbH
            TUI AG
            Booqua Ltd. & Co KG
            innogy SE
            Aspera Inc.
        */
        #endregion
        [M(O.AggressiveInlining)] private bool TryFindByPrevWord( List< word_t > words, int startIndex, out int length )
        {
            var w = (0 <= startIndex) ? words[ startIndex ] : null;
            if ( w?.valueOriginal != null )
            {
                if ( /*w.IsInputTypeNum() && */w.IsExtraWordTypeIntegerNumber() )
                {
                    const int MAX_NUM_LENGTH = 3;
                    if ( (w.length <= MAX_NUM_LENGTH) && (0 < startIndex) )
                    {
                        var w1 = words[ startIndex - 1 ];
                        if ( (w1.valueOriginal != null) && w1.IsFirstLetterUpper() /*&& !w1.IsInputTypeNum() && !w1.IsExtraWordTypePunctuation()*/ )
                        {
                            length = 2;
                            return (true);
                        }
                    }

                    //length = 1;
                    //return (true);
                }
                else if ( w.IsContainsLetters() /*w.IsFirstLetterUpper()*/ )
                {
                    length = 1;
                    return (true);
                }
            }
            length = default;
            return (false);
        }

        #region [.descr.]
        /*4. Если на удалении 1-4 слов слева от окончания находится сокращение (то есть все буквы заглавные) и все слова пишутся с заглавной буквы, то все эти слова являются частью названия фирмы. Примеры:
            "VBB Verkehrsverbund Berlin-Brandenburg GmbH"
        */
        #endregion
        [M(O.AggressiveInlining)] private bool TryExpandByUpperAbbreviation( List< word_t > words, int startIndex, out int length )
        {
            for ( int i = startIndex, end_i = Math.Max( 0, i - _MaxDistance + 1 ); end_i <= i; i-- )
            {
                var w = words[ i ];
                if ( !w.IsOutputTypeOther() ) break;

                var v = w.valueOriginal;
                if ( v == null ) break;

                if ( !w.IsFirstLetterUpper() )
                {
                    break;
                }
                else if ( w.IsUpperAbbreviation( UPPER_ABBREVIATION_MIN_LEN ) )
                {
                    length = (startIndex - i) + 1;
                    return (true);
                }
                else if ( _Suffixes.Contains( words, i ) )
                {
                    break;
                }
            }
            length = default;
            return (false);
        }

        #region [.descr.]
        /*5. Если перед найденным названием идет сокращение (то есть все буквы заглавные), то это сокращение тоже является частью названия:
            KBE Kommunale Beteiligungsgesellschaft mbH
            HSH Finanzfonds AöR
            VEBEG Gesellschaft mbH
            DB Netz AG
            TVO  Schuster GmbH & Co. KG
            A+B Pertler GmbH
        */
        #endregion
        [M(O.AggressiveInlining)] private int ExpandByUpperAbbreviation( List< word_t > words, int startIndex )
        {
            var w = (0 <= startIndex) ? words[ startIndex ] : null;
            return (((w?.valueOriginal != null) && w.IsUpperAbbreviation( UPPER_ABBREVIATION_MIN_LEN )) ? 1 : 0);
        }

        #region [.public method's.]
        [M(O.AggressiveInlining)] private bool TryGetStart( List< word_t > words, int idx, out int length )
        {
            //[idx == "prev suffix word"]

            if ( TryFindByPreambles( words, idx, out length ) )
            {
                length += ExpandByUpperAbbreviation( words, idx - length );
                return (true);
            }
            
            if ( TryFindBySpecEnding( words, idx, out length ) )
            {
                length += ExpandByUpperAbbreviation( words, idx - length );
                return (true);
            }

            if ( TryFindByPrevWord( words, idx, out length ) )
            {
                if ( TryExpandByUpperAbbreviation( words, idx - length, out var length_2 ) )
                {
                    length += length_2;
                }
                return (true);
            }

            return (false);
        }
        [M(O.AggressiveInlining)] public bool TryFind( List< word_t > words, int startIndex, ref SearchResult result )
        {
            if ( _Suffixes.TryFind2Right( words, startIndex, out var length ) )
            {
                var idx = startIndex;
                if ( _PrefixesPrevSuffixes.TryFind2Left( words, idx - 1, out var length_2 ) )
                {
                    idx -= length_2;
                }

                if ( TryGetStart( words, idx - 1, out var length_2_left ) )
                {
                    result = new SearchResult( idx - length_2_left, length + length_2 + length_2_left );
                    return (true);
                }
            }
            return (false);
        }
        #endregion
#if DEBUG
        public override string ToString() => $"{_Suffixes}";
#endif
    }
}
