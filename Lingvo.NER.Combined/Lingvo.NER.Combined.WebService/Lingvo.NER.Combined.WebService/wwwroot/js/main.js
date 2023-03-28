$(document).ready(function () {
    var textOnChange = function () {
        var _len = $('#text').val().length;
        var len = _len.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
        var $textLength = $('#textLength');
        $textLength.html('text length: ' + len + ' chars').toggleClass('max-inputtext-length', (MAX_INPUTTEXT_LENGTH < _len));
    };
    var getText = function ($text) {
        var text = trim_text($text.val().toString());
        if (is_text_empty(text)) { alert('Enter text for NER.'); $text.focus(); return (null); }
        return (text);
    };
    var ner_prefixes = ['names', 'orgs', 'geos', 'misc',
                        'address', 'phones', 'urls', 'banks', 'customerNumbers', 'birthdays', 'creditCards', 'passportIdCardNumbers', 'nationalities', 
                        'birthplaces', 'maritalStatuses', 'carNumbers', 'healthInsurances', 'driverLicenses', 'socialSecurities', 'taxIdentifications', 'companies'];

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
            $enabled_ner_ss  = $(document.styleSheets).filter(function (_, ss) { return (ss.href?.toLowerCase().indexOf(enabled_ner) !== -1); });
        if ($disabled_ner_ss.length) $disabled_ner_ss[0].disabled = true;
        if ($enabled_ner_ss.length) $enabled_ner_ss[0].disabled = false;

    }).trigger('change').parent('div').parent('div').show();

    (function () {
        //$('#maxPredictSentLength').val(localStorage.getItem(LOCALSTORAGE_MAXPREDICTSENTLENGTH_KEY) || 32);
        $('#text').val(localStorage.getItem(LOCALSTORAGE_TEXT_KEY) || DEFAULT_TEXT).focus();

        $.get('/NERCombined/GetModelInfoKeys?' + Math.random())//.fail()
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

        var $result = $('#processResult table').empty();
        $.ajax({
            type: 'POST', contentType: 'application/json; charset=utf-8',
            url: '/NERCombined/Run',
            data: JSON.stringify({ text: text, modelType: modelType /*, maxPredictSentLength: $('#maxPredictSentLength').val() || 32*/ }),
            error: function () { processing_end_with_error('Server error.'); },
            success: function (responce) {
                if (responce.errorMessage) { processing_end_with_error(responce.errorMessage + '          (' + responce.fullErrorMessage + ')'); return; }
                if (!responce.words || !responce.words.length) { processing_end_with_undefined(); return; }

                var get_ner_id = (function () { var hs = {}, get = function (ner) { var n = hs[ner]; n = (n === undefined) ? 0 : (n + 1); hs[ner] = n; return (ner + '-' + n); }; return (get); })();
                var correct_ner = function (ner) {
                    switch (ner) {
                        case 'PERSON': case 'PERSON__NNER': return ('Name');
                        default: return (ner.replaceAll('__NNER', ''));
                    }
                };

                processing_end_without_error();
                var ner_htmls = ['<tr><td>'];
                var startIndex = 0;
                for (var i = 0, len = responce.words.length - 1; i <= len; i++) {
                    var word = responce.words[i], ner = correct_ner(word.ner), ner_id = get_ner_id(ner),
                        $span = $('<span>').attr('ner', ner).addClass(ner).attr('id', ner_id).html(text.substr(word.i, word.l).replaceAll('  ', '&nbsp;&nbsp;'));
                    if (i === 0) {
                        $('#bookmarkFirst').attr('href', '#' + ner_id);
                    } else if (i === len) {
                        $('#bookmarkLast').attr('href', '#' + ner_id);
                    }
                    ner_htmls.push(text.substr(startIndex, word.i - startIndex).replaceAll('  ', '&nbsp;&nbsp;'));
                    ner_htmls.push($span[ 0 ].outerHTML);
                    startIndex = word.i + word.l;
                }
                ner_htmls.push(text.substr(startIndex, text.length - startIndex).replaceAll('  ', '&nbsp;&nbsp;'));
                ner_htmls.push('<br><br></td></tr>');
                var ner_html = ner_htmls.join('').replaceAll('\r\n', '<br/>').replaceAll('\n', '<br/>').replaceAll('\t', '&nbsp;&nbsp;&nbsp;&nbsp;');
                $result.html(ner_html);
                $('#totalEntitiesCount').text('total entities count: ' + responce.words.length);
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
        ner_checkboxs.forEach(o => (o.click ? o.click() : nerShowRoutine(o.id, o.nerClass, o.title)));

        var next = function (a_id, count_id) {
            var $a = $(a_id), //ner_2 = $a.attr('ner_2'),
                $ss = $('span[id^="' + $a.attr('ner') + '-"]' /*+ (ner_2 ? ', span[id^="' + ner_2 + '-"]' : '')*/);
            if ($ss.length) {
                $a.attr('href', '#' + $($ss[ 0 ]).attr('id'));
            } 
            $a.toggle(0 < $ss.length).attr('ner-num', 1);
            $(count_id).text('(' + ($ss.length || '-') + ')');
        };
        var prev = function (a_id) {
            var $a = $(a_id), //ner_2 = $a.attr('ner_2'),
                $ss = $('span[id^="' + $a.attr('ner') + '-"]' /*+ (ner_2 ? ', span[id^="' + ner_2 + '-"]' : '')*/);
            if ($ss.length) {
                $a.attr('href', '#' + $($ss[ $ss.length - 1 ]).attr('id'));
            }
            $a.toggle(1 < $ss.length).attr('ner-num', $ss.length - 2);
        };
        var next_prev = function (prefix) { next('#' + prefix + '-next', '#' + prefix + '-count'); prev('#' + prefix + '-prev'); };
        for (var i = ner_prefixes.length - 1; 0 <= i; i--) next_prev( ner_prefixes[ i ] );
    };

    function nerShowRoutine(checkbox_id, nerClass, title) {
        if ($(checkbox_id).is(':checked')) {
            if (!title) title = nerClass;
            var title_func = function () { return (title + ': \'' + $(this).text() + '\''); };
            $('span.--' + nerClass + '--').removeClass('--' + nerClass + '--').addClass(nerClass).attr('title', title_func);
            $('span.' + nerClass).attr('title', title_func);
        } else {
            $('span.' + nerClass).removeClass(nerClass).addClass('--' + nerClass + '--').removeAttr('title');
        }
    };

    var ner_checkboxs = [
        { id: '#namesNerCheckbox', click: function() { /*nerShowRoutine('#namesNerCheckbox', 'PERSON', 'person');*/ nerShowRoutine('#namesNerCheckbox', 'Name', 'person'); } },
        { id: '#orgsNerCheckbox', title: 'organization', nerClass: 'ORGANIZATION' },
        { id: '#geosNerCheckbox', title: 'geo/location', nerClass: 'LOCATION' },
        { id: '#miscNerCheckbox', title: 'miscellaneous', nerClass: 'MISCELLANEOUS' },

        { id: '#addressNerCheckbox', nerClass: 'Address' },
        { id: '#phonesNerCheckbox', title: 'Phone number', nerClass: 'PhoneNumber' },
        { id: '#urlsNerCheckbox', click: function() { nerShowRoutine('#urlsNerCheckbox', 'Url'); nerShowRoutine('#urlsNerCheckbox', 'Email'); } },
        { id: '#banksNerCheckbox', title: 'BankAccount', nerClass: 'AccountNumber' },
        { id: '#customerNumbersNerCheckbox', title: 'Customer number', nerClass: 'CustomerNumber' },
        { id: '#birthdaysNerCheckbox', nerClass: 'Birthday' },
        { id: '#nationalitiesNerCheckbox', nerClass: 'Nationality' },
        { id: '#birthplacesNerCheckbox', nerClass: 'Birthplace' },
        { id: '#maritalStatusesNerCheckbox', title: 'Marital-status', nerClass: 'MaritalStatus' },
        { id: '#creditCardsNerCheckbox', title: 'Credit-card', nerClass: 'CreditCard' },
        { id: '#passportIdCardNumbersNerCheckbox', title: 'passport-or-id_card-number', nerClass: 'PassportIdCardNumber' },
        { id: '#carNumbersNerCheckbox', title: 'Car-number', nerClass: 'CarNumber' },
        { id: '#healthInsurancesNerCheckbox', title: 'Health-insurance', nerClass: 'HealthInsurance' },
        { id: '#driverLicensesNerCheckbox', title: 'Driver-license', nerClass: 'DriverLicense' },
        { id: '#socialSecuritiesNerCheckbox', title: 'Social-security', nerClass: 'SocialSecurity' },
        { id: '#taxIdentificationsNerCheckbox', title: 'Tax-identification', nerClass: 'TaxIdentification' },
        { id: '#companiesNerCheckbox', nerClass: 'Company' }
    ];
    for (var i = ner_checkboxs.length - 1; 0 <= i; i--) {
        var o = ner_checkboxs[i];
        if (o.click) 
            $(o.id).click(o.click);
        else          
            $(o.id).click( (function (x) {
                return function () { nerShowRoutine(x.id, x.nerClass, x.title); };
            })(o) );
    }
    $(ner_checkboxs.map((o) => o.id).join(','))
        .each(function (_, ch) { $(ch).prop('checked', localStorage.getItem(LOCALSTORAGE_TEXT_KEY + '.' + $(ch).attr('id')) !== 'false' ); })
        .click(function () { var isck = $(this).is(':checked'), key = LOCALSTORAGE_TEXT_KEY + '.' + $(this).attr('id'); if (!isck) localStorage.setItem(key, 'false'); else localStorage.removeItem(key); });
    $('#checkUncheckAllCheckbox').click(function () { $(ner_checkboxs.map((o) => o.id)/*.filter(id => id !== '#unitedEntitiesNerCheckbox')*/.join(',')).prop('checked', !this.checked).click(); });
    $('#bookmarkFirst, #bookmarkLast').click(function () { scroll_to_href($(this)); return (false); });

    var scroll_to_window_hash = function () { try { $(window).trigger('hashchange'); var $p = $('#processResult'), $x = $(window.location.hash); $p.animate({ scrollTop: $p.scrollTop() + $x.offset().top - $p.offset().top }, 0); } catch (e) { ; } },
        scroll_to_href = function ($a) {
            var $p = $('#processResult'), href = $a.attr('href'), $x = $('span[id="' + href.substring(1) + '"]');
            $p.animate({ scrollTop: $p.scrollTop() + $x.offset().top - $p.offset().top }, 170/*250*/, 'swing', function () { window.location.hash = href; });
        };
    var next = function () {
        var $a = $(this), ner = $a.attr('ner'), n = parseInt($a.attr('ner-num')), $ss = $('span[id^="' + ner + '-"]');
        if ($ss.length <= n) {
            n = 0;
        } else if (n < 0) {
            n = $ss.length - 1;
        }

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
        var $a = $(this), ner = $a.attr('ner'), n = parseInt($a.attr('ner-num')), $ss = $('span[id^="' + ner + '-"]');
        if (n < 0) {
            n = $ss.length - 1;
        } else if ($ss.length <= n) {
            n = 0;
        }

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
