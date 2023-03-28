$(document).ready(function () {
    var textOnChange = function () {
        var _len = $('#text').val().length;
        var len = _len.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
        var $textLength = $('#textLength');
        $textLength.html('text length: ' + len + ' chars').toggleClass('max-inputtext-length', (MAX_INPUTTEXT_LENGTH < _len));
    };
    var getText = function ($text) { var text = trim_text($text.val().toString()); if (is_text_empty(text)) { alert('Enter text for NER.'); $text.focus(); return (null); } return (text); };
    var ner_prefixes = ['names', 'orgs', 'geos', 'misc'];

    $('#text').focus(textOnChange).change(textOnChange).keydown(textOnChange).keyup(textOnChange).select(textOnChange).focus();
    $(window).resize(function () { $('#processResult').height($(window).height() - $('#processResult').position().top - 25); }).trigger('resize');
    $(window).on('hashchange', function () { $('span').removeClass('ACTIVE_NER_ENTITY'); try { $(window.location.hash).addClass('ACTIVE_NER_ENTITY'); } catch (e) { ; } });
    $('#nerStyleCheckbox').prop('checked', !!localStorage.getItem(LOCALSTORAGE_KEY + 'nerStyleCheckbox')).change(function () {
        var $this = $(this), ch = $this.is(':checked'), k = LOCALSTORAGE_KEY + 'nerStyleCheckbox',
            ner_v1 = '/ner_v1.css'.toLowerCase(), ner_v2 = '/ner_v2.css'.toLowerCase();
        if (ch) localStorage.setItem(k, true); else localStorage.removeItem(k);

        var disabled_ner = ch ? ner_v1 : ner_v2,
            enabled_ner  = ch ? ner_v2 : ner_v1;
        var $disabled_ner_ss = $(document.styleSheets).filter(function (_, ss) { return (ss.href?.toLowerCase().indexOf(disabled_ner) !== -1); }),
            $enabled_ner_ss = $(document.styleSheets).filter(function (_, ss) { return (ss.href?.toLowerCase().indexOf(enabled_ner) !== -1); });
        if ($disabled_ner_ss.length) $disabled_ner_ss[0].disabled = true;
        if ($enabled_ner_ss.length) $enabled_ner_ss[0].disabled = false;

    }).trigger('change').parent('div').parent('div').show();

    (function () {
        //$('#maxPredictSentLength').val(localStorage.getItem(LOCALSTORAGE_MAXPREDICTSENTLENGTH_KEY) || 32);
        $('#text').val(localStorage.getItem(LOCALSTORAGE_TEXT_KEY) || DEFAULT_TEXT).focus();

        $.get('/NNER/GetModelInfoKeys?' + Math.random())//.fail()
         .done(function (responce) {
             if (!responce.errorMessage && responce.length) {
                 var $mt = $('#modelType').empty();
                 for (var i = 0, len = responce.length; i < len; i++) {
                     var mt = responce[i], t = mt;//.replaceAll('_', ' ').replaceAll('de', '').trim();
                     $mt.append( $('<option>').attr('value', mt).text( t ) );
                 }

                 $mt.val( localStorage.getItem( LOCALSTORAGE_MODELTYPE_KEY ) );
                 if ( !$mt.val() ) $mt.val( $mt.find('option').val() );
             }
         });
    })();
    $('#resetText2Default').click(function () { $('#text').val(''); setTimeout(function () { $('#text').val(DEFAULT_TEXT).focus(); }, 100); });

    $('#mainPageContent').on('click', '#processButton', function () {
        if ($(this).hasClass('disabled')) return (false);

        var text = getText($('#text'));
        if (!text) return (false);

        processing_start();
        var modelType = $('#modelType').val();
        if (text !== DEFAULT_TEXT) localStorage.setItem(LOCALSTORAGE_TEXT_KEY, text); else localStorage.removeItem(LOCALSTORAGE_TEXT_KEY);
        localStorage.setItem(LOCALSTORAGE_MODELTYPE_KEY, modelType);
        //localStorage.setItem(LOCALSTORAGE_MAXPREDICTSENTLENGTH_KEY, $('#maxPredictSentLength').val());
        var makePostMerge = true; //$('#makePostMerge').is(':checked');        

        var $result = $('#processResult table').empty();
        $.ajax({
            type: 'POST', contentType: 'application/json; charset=utf-8', //, dataType: 'json'
            url: '/NNER/Run',
            data: JSON.stringify({ text: text, modelType: modelType, makePostMerge: makePostMerge/*, maxPredictSentLength: $('#maxPredictSentLength').val() || 32*/ }),
            error: function () { processing_end_with_error('Server error.'); },
            success: function (responce) {
                if (responce.errorMessage) { processing_end_with_error(responce.errorMessage + '          (' + responce.fullErrorMessage + ')'); return; }
                if (!responce.nerResults || !responce.nerResults.length) { processing_end_with_undefined(); return; }

                var get_ner_id = (function () { var hs = {}, get = function (ner) { var n = hs[ner]; n = (n === undefined) ? 0 : (n + 1); hs[ner] = n; return (ner + '-' + n); }; return (get); })();

                processing_end_without_error();
                var ner_htmls = ['<tr><td>'], first_ner_id, last_ner_id, ner_words_cnt = 0;
                for (var i = 0, len = responce.nerResults.length ; i < len; i++) {
                    var nr = responce.nerResults[ i ];
                    if (nr.error && nr.error.errorMessage) {
                        ner_htmls.push( $('<div>').addClass('error bold').text(nr.error.errorMessage + '          (' + nr.error.fullErrorMessage + ')')[ 0 ].outerHTML );
                    } else {
                        for (var j = 0, ts = nr.tuples, len_2 = ts.length; j < len_2; j++) {
                            var t = ts[ j ], ner_id = get_ner_id(t.ner),
                                $span = $('<span>').attr('ner', t.ner).addClass(t.ner).text(t.word).attr('id', ner_id);
                            ner_htmls.push($span[ 0 ].outerHTML);
                            ner_htmls.push(' ');

                            if (t.ner !== 'O' && t.ner !== 'Other') {
                                ner_words_cnt++;
                                if (!first_ner_id) {
                                    first_ner_id = ner_id;
                                    $('#bookmarkFirst').attr('href', '#' + ner_id);
                                }
                                last_ner_id = ner_id;
                            }
                        }
                        ner_htmls.push('<br>');
                    }                    
                }
                if (last_ner_id) $('#bookmarkLast').attr('href', '#' + last_ner_id);
                ner_htmls.push('<br><br></td></tr>');
                var ner_html = ner_htmls.join('');//.replaceAll('\r\n', '<br/>').replaceAll('\n', '<br/>').replaceAll('\t', '&nbsp;&nbsp;&nbsp;&nbsp;');
                $result.html(ner_html);
                $('#totalEntitiesCount').text('total entities count: ' + ner_words_cnt);
                apply_ner_titles();
                scroll_to_window_hash();
            }
        });
    });

    function processing_start() {
        $('#text').addClass('no-change').attr('readonly', 'readonly').attr('disabled', 'disabled');
        $('.result-info').show().removeClass('error').removeClass('cls-undefined').html('processing... <label id="processingTickLabel"></label>');
        $('#processButton, #resetText2Default, #modelType, label[for="modelType"]').hide();//.addClass('disabled');
        $('#totalEntitiesCount, #bookmarkFirst, #bookmarkLast').hide();
        $('#processResult tbody').empty();

        processingTickCount = 1;
        setTimeout(processing_tick, 1000);
    };
    function processing_end() {
        $('#text').removeClass('no-change').removeAttr('readonly').removeAttr('disabled');        
        $('#processButton, #resetText2Default, #modelType, label[for="modelType"]').show();//.removeClass('disabled');

        //var $d = $('<div class="elapsed">').append( $('<span>').text('elapsed: ') ).append( $('<label>').text( processingTickCount_to_text(processingTickCount) ) ).css('color', 'silver').css('font-size', '11px').css('font-weight', '100');
        $('.result-info').removeClass('error').text('');

        var $s = $('.page-box-button');
        $s.find('div.elapsed').remove(); //$s.append($d);
    };
    function processing_end_without_error() { processing_end(); $('.result-info').hide(); $('#totalEntitiesCount, #bookmarkFirst, #bookmarkLast').show(); };
    function processing_end_with_error(msg) { processing_end(); $('.result-info').addClass('error').append( $('<span>').text( msg ) ); };
    function processing_end_with_undefined() { processing_end(); $('.result-info').addClass('cls-undefined').append( $('<span>').text('NER for text is undefined.') ); };
    function trim_text(text) { return (text.replace(/(^\s+)|(\s+$)/g, '')); };
    function is_text_empty(text) { return (text.replace(/(^\s+)|(\s+$)/g, '') === ''); };

    var processingTickCount = 1;
    function processingTickCount_to_text( ptc ) {
        var n2 = function (n) { n = n.toString(); return ((n.length === 1) ? ('0' + n) : n); }
        var d  = new Date(new Date(new Date(new Date().setHours(0)).setMinutes(0)).setSeconds( ptc ));
        var t  = n2(d.getHours()) + ':' + n2(d.getMinutes()) + ':' + n2(d.getSeconds()); //d.toLocaleTimeString();
        return (t);
    };
    function processing_tick() {
        var t = processingTickCount_to_text( processingTickCount );
        var $s = $('#processingTickLabel');
        if ($s.length) {
            $s.text(t);
            processingTickCount++;
            setTimeout(processing_tick, 1000);
        } else {
            processingTickCount = 1;
        }
    };

    function apply_ner_titles() {
        namesNerShow(); orgsNerShow(); geosNerShow(); miscNerShow();

        var next = function (a_id, count_id) {
            var $a = $(a_id), ner = $a.attr('ner'), nerMerged = $a.attr('nerMerged'),
                $ss = $('span[id^="B-' + ner + '-"], span[id^="I-' + ner + '-"], span[id^="' + nerMerged + '-"]');
            if ($ss.length) {
                $a.attr('href', '#' + $($ss[ 0 ]).attr('id'));
            } 
            $a.toggle(0 < $ss.length).attr('ner-num', 1);
            $(count_id).text('(' + ($ss.length || '-') + ')');
        };
        var prev = function (a_id) {
            var $a = $(a_id), ner = $a.attr('ner'), nerMerged = $a.attr('nerMerged'),
                $ss = $('span[id^="B-' + ner + '-"], span[id^="I-' + ner + '-"], span[id^="' + nerMerged + '-"]');
            if ($ss.length) {
                $a.attr('href', '#' + $($ss[ $ss.length - 1 ]).attr('id'));
            }
            $a.toggle(1 < $ss.length).attr('ner-num', $ss.length - 2);
        };
        var next_prev = function (prefix) { next('#' + prefix + '-next', '#' + prefix + '-count'); prev('#' + prefix + '-prev'); };
        for (var i = ner_prefixes.length - 1; 0 <= i; i--) next_prev( ner_prefixes[ i ] );
    };

    function namesNerShow() { nerShowRoutine($('#namesNerCheckbox').is(':checked') ? 'person' : null, 'PER', 'PERSON'); };
    function orgsNerShow() { nerShowRoutine($('#orgsNerCheckbox').is(':checked') ? 'organization' : null, 'ORG', 'ORGANIZATION'); };
    function geosNerShow() { nerShowRoutine($('#geosNerCheckbox').is(':checked') ? 'geo/location' : null, 'LOC', 'LOCATION'); };
    function miscNerShow() { nerShowRoutine($('#miscNerCheckbox').is(':checked') ? 'miscellaneous' : null, 'MISC', 'MISCELLANEOUS'); };
    function nerShowRoutine(title, nerClass, nerClassMerged) {
        var b_nerClass = 'B-' + nerClass, i_nerClass = 'I-' + nerClass;
        if (title) {
            var title_func = function () { return (title + ': \'' + $(this).text() + '\''); };

            $('span.--' + b_nerClass + '--').removeClass('--' + b_nerClass + '--').addClass(b_nerClass).attr('title', title);
            $('span.--' + i_nerClass + '--').removeClass('--' + i_nerClass + '--').addClass(i_nerClass).attr('title', title);
            $('span.--' + nerClassMerged + '--').removeClass('--' + nerClassMerged + '--').addClass(nerClassMerged).attr('title', title_func);
            $('span.' + b_nerClass + ',' + 'span.' + i_nerClass + ',' + 'span.' + nerClassMerged).attr('title', title_func);
        } else {
            $('span.' + b_nerClass).removeClass(b_nerClass).addClass('--' + b_nerClass + '--').removeAttr('title');
            $('span.' + i_nerClass).removeClass(i_nerClass).addClass('--' + i_nerClass + '--').removeAttr('title');
            $('span.' + nerClassMerged).removeClass(nerClassMerged).addClass('--' + nerClassMerged + '--').removeAttr('title');
        }
    };

    $('#namesNerCheckbox').click(namesNerShow);
    $('#orgsNerCheckbox').click(orgsNerShow);
    $('#geosNerCheckbox').click(geosNerShow);
    $('#miscNerCheckbox').click(miscNerShow);
    $('#namesNerCheckbox, #orgsNerCheckbox, #geosNerCheckbox, #miscNerCheckbox')
        .each(function (_, ch) { $(ch).prop('checked', localStorage.getItem(LOCALSTORAGE_TEXT_KEY + '.' + $(ch).attr('id')) !== 'false' ); })
        .click(function () { var isck = $(this).is(':checked'), key = LOCALSTORAGE_TEXT_KEY + '.' + $(this).attr('id'); if (!isck) localStorage.setItem(key, 'false'); else localStorage.removeItem(key); });
    $('#bookmarkFirst, #bookmarkLast').click(function () { scroll_to_href($(this)); return (false); });

    var scroll_to_window_hash = function () { try { $(window).trigger('hashchange'); var $p = $('#processResult'), $x = $(window.location.hash); $p.animate({ scrollTop: $p.scrollTop() + $x.offset().top - $p.offset().top }, 0); } catch (e) { ; } },
        scroll_to_href = function ($a) {
            var $p = $('#processResult'), href = $a.attr('href'), $x = $('span[id="' + href.substring(1) + '"]');
            $p.animate({ scrollTop: $p.scrollTop() + $x.offset().top - $p.offset().top }, 170/*250*/, 'swing', function () { window.location.hash = href; });
        };
    var next = function () {
        var $a = $(this), ner = $a.attr('ner'), nerMerged = $a.attr('nerMerged'),
            n = parseInt($a.attr('ner-num')),
            $ss = $('span[id^="B-' + ner + '-"], span[id^="I-' + ner + '-"], span[id^="' + nerMerged + '-"]');
        if ($ss.length <= n) n = 0; else if (n < 0) n = $ss.length - 1;

        scroll_to_href($a);
        var _this = this,
            $other = $('a[ner="' + ner + '"]').filter(function (_, x) { return x !== _this; }),
            pn = (n - 2), nn = $a.attr('href');
            if ( pn < 0 ) pn = $ss.length - 1;
            $other.attr('href', nn.substr(0, nn.indexOf('-')) + '-' + pn).attr('ner-num', pn - 1);
        $a.attr('href', '#' + $($ss[n]).attr('id')).attr('ner-num', n + 1);
        return (false);
    };
    var prev = function () {
        var $a = $(this), ner = $a.attr('ner'), nerMerged = $a.attr('nerMerged'),
            n = parseInt($a.attr('ner-num')),
            $ss = $('span[id^="B-' + ner + '-"], span[id^="I-' + ner + '-"], span[id^="' + nerMerged + '-"]');
        if (n < 0) n = $ss.length - 1; else if ($ss.length <= n) n = 0;

        scroll_to_href($a);
        var _this = this,
            $other = $('a[ner="' + ner + '"]').filter(function (_, x) { return x !== _this; }),
            pn = (n + 2), nn = $a.attr('href');
            if ( $ss.length <= pn ) pn = 0;
            $other.attr('href', nn.substr(0, nn.indexOf('-')) + '-' + pn).attr('ner-num', pn + 1);
        $a.attr('href', '#' + $($ss[n]).attr('id')).attr('ner-num', n - 1);
        return (false);
    };
    var next_prev = function (prefix) { $('#' + prefix + '-next').click(next); $('#' + prefix + '-prev').click(prev); };
    for (var i = ner_prefixes.length - 1; 0 <= i; i--) next_prev( ner_prefixes[ i ] );
});
