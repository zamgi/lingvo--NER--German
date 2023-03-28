$(document).ready(function () {
    $.fn.set_attr_if = function (attrName, value) { if (attrName && value) this.attr(attrName, value); return this; };
    var textOnChange = function () {
        var _len = $('#text').val().length;
        var len = _len.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
        var $textLength = $('#textLength');
        $textLength.html('length of text: ' + len + ' characters');
        if (config.MAX_INPUTTEXT_LENGTH < _len) $textLength.addClass('max-inputtext-length');
        else $textLength.removeClass('max-inputtext-length');
    };
    var getText = function ($text) {
        var text = trim_text($text.val().toString());
        if (is_text_empty(text)) { alert('Enter the text to be processed.'); $text.focus(); return (null); }
        if (config.MAX_INPUTTEXT_LENGTH < text.length) {
            if (!confirm('Exceeded the recommended limit ' + config.MAX_INPUTTEXT_LENGTH + ' characters (on the ' + (text.length - config.MAX_INPUTTEXT_LENGTH) + ' characters).\r\nText will be truncated, continue?')) { return (null); }
            text = text.substr(0, config.MAX_INPUTTEXT_LENGTH); $text.val(text); $text.change();
        }
        return (text);
    };
    var ner_prefixes = ['address', 'phones', 'urls', 'banks', 'names', 'customerNumbers', 'birthdays', 'creditCards', 'passportIdCardNumbers', 'nationalities',
                        'birthplaces', 'maritalStatuses', 'carNumbers', 'healthInsurances', 'driverLicenses', 'socialSecurities', 'taxIdentifications', 'companies'];

    $('#text').focus(textOnChange).change(textOnChange).keydown(textOnChange).keyup(textOnChange).select(textOnChange).focus();
    $(window).resize(function () { $('#processResult').height($(window).height() - $('#processResult').position().top - 10); }).trigger('resize');
    $(window).on('hashchange', function () { $('span').removeClass('ACTIVE_NER_ENTITY'); try { $(window.location.hash).addClass('ACTIVE_NER_ENTITY'); } catch (e) { ; } });
    $('#nerStyleCheckbox').prop('checked', !!localStorage.getItem(config.LOCALSTORAGE_KEY + 'nerStyleCheckbox')).change(function () {
        var $this = $(this), ch = $this.is(':checked'), k = config.LOCALSTORAGE_KEY + 'nerStyleCheckbox',
            ner_v1 = '/ner_v1.css'.toLowerCase(), ner_v2 = '/ner_v2.css'.toLowerCase();
        if (ch) localStorage.setItem(k, true); else localStorage.removeItem(k);
        
        var disabled_ner = ch ? ner_v1 : ner_v2,
            enabled_ner = ch ? ner_v2 : ner_v1;
        var $disabled_ner_ss = $(document.styleSheets).filter(function (_, ss) { return (ss.href?.toLowerCase().indexOf(disabled_ner) !== -1); }),
            $enabled_ner_ss = $(document.styleSheets).filter(function (_, ss) { return (ss.href?.toLowerCase().indexOf(enabled_ner) !== -1); });
        if ($disabled_ner_ss.length) $disabled_ner_ss[0].disabled = true;
        if ($enabled_ner_ss.length) $enabled_ner_ss[0].disabled = false;

    }).trigger('change').parent('div').parent('div').show();

    (function () {
        function isGooglebot() { return (navigator.userAgent.toLowerCase().indexOf('googlebot/') !== -1); };
        if (isGooglebot()) return;

        var text = localStorage.getItem(config.LOCALSTORAGE_TEXT_KEY);
        if (!text || !text.length) text = config.DEFAULT_TEXT; 
        $('#text').val(text).focus();
        $.ajax({ type: 'POST', url: 'Ner/Run', contentType: 'application/json; charset=utf-8', data: JSON.stringify({ text: '_dummy_' }) });
    })();
    $('#resetText2Default').click(function () { $('#text').val(''); setTimeout(function () { $('#text').val(config.DEFAULT_TEXT).focus(); }, 100); });

    $('#mainPageContent').on('click', '#processButton', function () {
        if ($(this).hasClass('disabled')) return (false);

        var text = getText($('#text'));
        if (!text) return (false);

        processing_start();
        if (text !== config.DEFAULT_TEXT) localStorage.setItem(config.LOCALSTORAGE_TEXT_KEY, text); else localStorage.removeItem(config.LOCALSTORAGE_TEXT_KEY);
        var reloadModel = $('#reloadModel').is(':checked');
        if (reloadModel) $('#reloadModel').prop('checked', false);

        var $result = $('#processResult table').empty();
        var model = JSON.stringify({
            text: text,
            reloadModel: reloadModel,
            //ReturnInputText: false,
            ReturnUnitedEntities: true,
            ReturnWordValue: true
        });
        $.ajax({
            type: 'POST', contentType: 'application/json; charset=utf-8',
            url: 'Ner/Run', data: model,
            error: function () { processing_end_with_error('Server error.'); },
            success: function (resp) {
                if (resp.err) { processing_end_with_error(resp.err); return; }
                if (!resp.words || !resp.words.length) { processing_end_with_undefined(); return; }

                var uhs = (function () {
                    var hs = {};
                    if (resp.unitedEntities && resp.unitedEntities.length) {
                        for (var i = 0, len = resp.unitedEntities.length; i < len; i++) {
                            var ue = resp.unitedEntities[i];
                            hs[ue.i] = ue.i + ue.l;
                        }
                    }
                    return (hs);
                })();
                var get_ner_id = (function () { var hs = {}, get = function (ner) { var n = hs[ner]; n = (n === undefined) ? 0 : (n + 1); hs[ner] = n; return (ner + '-' + n); }; return (get); })();

                processing_end_without_error();
                var ner_htmls = ['<tr><td>'];
                var startIndex = 0;
                var current_unitedEntity_endIndex = -1;
                for (var i = 0, len = resp.words.length - 1; i <= len; i++) {
                    var word = resp.words[i];
                    ner_htmls.push(text.substr(startIndex, word.i - startIndex).replaceAll('  ', '&nbsp;&nbsp;'));
                    var ner_id = get_ner_id(word.ner);
                    var $span = $('<span>').addClass(word.ner).attr('id', ner_id)
                        .set_attr_if('street'  , word.street  )
                        .set_attr_if('houseNum', word.houseNum)
                        .set_attr_if('indexNum', word.indexNum)
                        .set_attr_if('city'    , word.city    )
                        .set_attr_if('urlType' , word.urlType )
                        .set_attr_if('accountNumber'  , word.accountNumber  )
                        .set_attr_if('accountOwner'   , word.accountOwner   )
                        .set_attr_if('bankCode'       , word.bankCode       )
                        .set_attr_if('bankName'       , word.bankName       )
                        .set_attr_if('bankAccountType', word.bankAccountType)
                        .set_attr_if('customerNumber' , word.customerNumber )
                        .set_attr_if('firstName'    , word.firstName    )
                        .set_attr_if('surName'      , word.surName      )
                        .set_attr_if('nameType'     , word.nameType     )
                        .set_attr_if('maritalStatus', word.maritalStatus)
                        .set_attr_if('birthdayDateTime', word.birthdayDateTime)
                        .set_attr_if('birthplace'      , word.birthplace)
                        .set_attr_if('nationality'     , word.nationality)
                        .set_attr_if('creditCardNumber'     , word.creditCardNumber     )
                        .set_attr_if('passportIdCardNumber' , word.passportIdCardNumber )
                        .set_attr_if('carNumber'            , word.carNumber            )
                        .set_attr_if('healthInsuranceNumber', word.healthInsuranceNumber)
                        .set_attr_if('driverLicense'        , word.driverLicense        )
                        .set_attr_if('socialSecurity'       , word.socialSecurity       )
                        .set_attr_if('taxIdentification'    , word.taxIdentification    )
                        .set_attr_if('companyName'          , word.companyName          )
                        .html(text.substr(word.i, word.l).replaceAll('  ', '&nbsp;&nbsp;'));
                    if (i === 0) {
                        $('#bookmarkFirst').attr('href', '#' + ner_id); //$span.attr('id', 'first-entity');
                    } else if (i === len) {
                        $('#bookmarkLast').attr('href', '#' + ner_id); //$span.attr('id', 'last-entity');
                    }
                    var unitedEntity_endIndex = uhs[ word.i ];
                    if (unitedEntity_endIndex) {
                        ner_htmls.push('<div class="UnitedEntity">');
                        current_unitedEntity_endIndex = unitedEntity_endIndex;
                    }
                    ner_htmls.push($span[ 0 ].outerHTML);
                    startIndex = word.i + word.l;

                    if (startIndex /*(word.i + word.l)*/ === current_unitedEntity_endIndex) {
                        ner_htmls.push('</div>');
                        current_unitedEntity_endIndex = -1;
                    }
                }
                if (current_unitedEntity_endIndex !== -1) ner_htmls.push('</div>');
                
                ner_htmls.push(text.substr(startIndex, text.length - startIndex).replaceAll('  ', '&nbsp;&nbsp;') + '</td></tr>');
                var ner_html = ner_htmls.join('').replaceAll('\r\n', '<br/>').replaceAll('\n', '<br/>').replaceAll('\t', '&nbsp;&nbsp;&nbsp;&nbsp;');
                $result.html(ner_html);
                $('#totalEntitiesCount').text('total entities count: ' + resp.words.length + ', relevance ranking: (' + resp.relevanceRanking + ')');
                apply_ner_titles();
                scroll_to_window_hash();                
            }
        });

    });

    function processing_start() {
        $('#text').addClass('no-change').attr('readonly', 'readonly').attr('disabled', 'disabled');
        $('.result-info').show().removeClass('error').removeClass('cls-undefined').html('processing... <label id="processingTickLabel"></label>');
        $('#processButton, #resetText2Default, #modelType, label[for="modelType"], #reloadModelDiv').hide();
        $('#totalEntitiesCount, #bookmarkFirst, #bookmarkLast').hide();
        $('#processResult tbody').empty();

        processingTickCount = 1;
        setTimeout(processing_tick, 1000);
    };
    function processing_end() {
        $('#text').removeClass('no-change').removeAttr('readonly').removeAttr('disabled');            
        $('#processButton, #resetText2Default, #modelType, label[for="modelType"], #reloadModelDiv').show();//.removeClass('disabled');
            $('#resetText2Default').css('display','');
        $('.result-info').removeClass('error').text('');
    };
    function processing_end_without_error() { processing_end(); $('.result-info').hide(); $('#totalEntitiesCount, #bookmarkFirst, #bookmarkLast').show(); };
    function processing_end_with_error(msg) { processing_end(); $('.result-info').addClass('error').append($('<span>').text(msg)); };
    function processing_end_with_undefined() { processing_end(); $('.result-info').addClass('cls-undefined').append($('<span>').text('NER for text is undefined.')); };
    function trim_text(text) { return (text.replace(/(^\s+)|(\s+$)/g, '')); };
    function is_text_empty(text) { return (text.replace(/(^\s+)|(\s+$)/g, '') === ''); };

    String.prototype.insert = function (index, str) { return (0 < index) ? (this.substring(0, index) + str + this.substring(index, this.length)) : (str + this); };
    String.prototype.replaceAll = function (token, newToken, ignoreCase) {
        var str = this + '', i = -1;
        if (typeof token === 'string') {
            if (ignoreCase) {
                token = token.toLowerCase();
                while ((i = str.toLowerCase().indexOf(token, i >= 0 ? i + newToken.length : 0)) !== -1) {
                    str = str.substring(0, i) + newToken + str.substring(i + token.length);
                }
            } else {
                return this.split(token).join(newToken);
            }
        }
        return (str);
    };

    var processingTickCount = 1;
    function processing_tick() {
        var n2 = function (n) { n = n.toString(); return ((n.length === 1) ? ('0' + n) : n); }
        var d = new Date(new Date(new Date(new Date().setHours(0)).setMinutes(0)).setSeconds(processingTickCount));
        var t = n2(d.getHours()) + ':' + n2(d.getMinutes()) + ':' + n2(d.getSeconds()); //d.toLocaleTimeString();
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
        ner_checkboxs.forEach(o => o.click());

        var next = function (a_id, count_id) {
            var $a = $(a_id), $ss = $('span[id^="' + $a.attr('ner') + '-"]');
            if ($ss.length) {
                $a.attr('href', '#' + $($ss[ 0 ]).attr('id'));
            } 
            $a.toggle(0 < $ss.length).attr('ner-num', 1);
            $(count_id).text('(' + ($ss.length || '-') + ')');
        };
        var prev = function (a_id) {
            var $a = $(a_id), $ss = $('span[id^="' + $a.attr('ner') + '-"]');
            if ($ss.length) {
                $a.attr('href', '#' + $($ss[ $ss.length - 1 ]).attr('id'));
            }
            $a.toggle(1 < $ss.length).attr('ner-num', $ss.length - 2);
        };
        var next_prev = function (prefix) { next('#' + prefix + '-next', '#' + prefix + '-count'); prev('#' + prefix + '-prev'); };
        for (var i = ner_prefixes.length - 1; 0 <= i; i--) next_prev( ner_prefixes[ i ] );
    };

    function addressNerShow() {
        nerShowRoutine('#addressNerCheckbox', 'Address', function () {
            var $this = $(this), street = $this.attr('street'),
                houseNum = $this.attr('houseNum'),
                indexNum = $this.attr('indexNum'),
                city = $this.attr('city');
            $this.attr('title', 'Address' + (street ? (",\r\nstreet: '" + street + "'") : '')
                + (houseNum ? (",\r\nhouse: '" + houseNum + "'") : '')
                + (indexNum ? (",\r\nindex: '" + indexNum + "'") : '')
                + (city ? (",\r\ncity: '" + city + "'") : ''));
        });
    };
    function phonesNerShow() {
        nerShowRoutine('#phonesNerCheckbox', 'PhoneNumber', function () {
            var $this = $(this), city = $this.attr('city');
            $this.attr('title', 'Phone number' + (city ? (",\r\nOrtsnetzname: '" + city + "'") : ''));
        });
    };
    function urlsNerShow() {
        var processItemFunc = function () {
            var $this = $(this);
            $this.attr('title', $this.attr('urlType') + ':  ' + $this.text());
        };
        nerShowRoutine('#urlsNerCheckbox', 'Url', processItemFunc);
        nerShowRoutine('#urlsNerCheckbox', 'Email', processItemFunc);
    };
    function banksNerShow() {
        nerShowRoutine('#banksNerCheckbox', 'AccountNumber', function () {
            var $this = $(this), bankCode = $this.attr('bankCode'),
                accountNumber = $this.attr('accountNumber'),
                bankName = $this.attr('bankName'),
                accountOwner = $this.attr('accountOwner');
            $this.attr('title', "BankAccount: '" + $this.attr('bankAccountType') + "'"
                + (bankCode ? (",\r\nbankCode: '" + bankCode + "'") : '')
                + (accountNumber ? (",\r\naccountNumber: '" + accountNumber + "'") : '')
                + (bankName ? (",\r\nbankName: '" + bankName + "'") : '')
                + (accountOwner ? (",\r\naccountOwner: '" + accountOwner + "'") : ''));
        });
    };
    function customerNumbersNerShow() {
        nerShowRoutine('#customerNumbersNerCheckbox', 'CustomerNumber', function () {
            var $this = $(this), customerNumber = $this.attr('customerNumber');
            $this.attr('title', "customer-number: '" + customerNumber + "'");
        });
    };
    function namesNerShow() {
        nerShowRoutine('#namesNerCheckbox', 'Name', function () {
            var $this = $(this), firstName = $this.attr('firstName'),
                surName = $this.attr('surName'),
                nameType = $this.attr('nameType');
            $this.attr('title', 'Individuals/Human names'
                + (firstName ? (",\r\nfirst-name: '" + firstName + "'") : '')
                + (surName ? (",\r\nsur-name: '" + surName + "'") : '')
                + (nameType ? (",\r\ntype: '" + nameType + "'") : ''));
        });
    };
    function unitedEntitiesNerShow() {
        //nerShowRoutine('#unitedEntitiesNerCheckbox', 'UnitedEntity', function () { $(this).attr('title', 'United entity'); });
        var ner_checkbox_id = '#unitedEntitiesNerCheckbox', nerClass = 'UnitedEntity';
        if ($(ner_checkbox_id).is(':checked')) {
            $('div.--' + nerClass + '--').removeClass('--' + nerClass + '--').addClass(nerClass);
            $('div.' + nerClass).attr('title', 'United entity');
        } else {
            $('div.' + nerClass).removeClass(nerClass).addClass('--' + nerClass + '--').attr('title', '');
        }
    };
    function birthdaysNerShow() {
        nerShowRoutine('#birthdaysNerCheckbox', 'Birthday', function () {
            var $this = $(this), birthdayDateTime = $this.attr('birthdayDateTime');
            $this.attr('title', "birthday: '" + birthdayDateTime + "'");
        });
    };
    function birthplacesNerShow() {
        nerShowRoutine('#birthplacesNerCheckbox', 'Birthplace', function () {
            var $this = $(this), birthplace = $this.attr('birthplace');
            $this.attr('title', "birthplace: '" + birthplace + "'");
        });
    };
    function maritalStatusesNerShow() {
        nerShowRoutine('#maritalStatusesNerCheckbox', 'MaritalStatus', function () {
            var $this = $(this), maritalStatus = $this.attr('maritalStatus');
            $this.attr('title', "marital-status: '" + maritalStatus + "'");
        });
    };
    function nationalitiesNerShow() {
        nerShowRoutine('#nationalitiesNerCheckbox', 'Nationality', function () {
            var $this = $(this), nationality = $this.attr('nationality');
            $this.attr('title', "nationality: '" + nationality + "'");
        });
    };
    function creditCardsNerShow() {
        nerShowRoutine('#creditCardsNerCheckbox', 'CreditCard', function () {
            var $this = $(this), creditCardNumber = $this.attr('creditCardNumber');
            $this.attr('title', "credit-card: '" + creditCardNumber + "'");
        });
    };    
    function passportIdCardNumbersNerShow() {
        nerShowRoutine('#passportIdCardNumbersNerCheckbox', 'PassportIdCardNumber', function () {
            var $this = $(this), passportIdCardNumber = $this.attr('passportIdCardNumber');
            $this.attr('title', "passport-or-id_card-number: '" + passportIdCardNumber + "'");
        });
    };
    function carNumbersNerShow() {
        nerShowRoutine('#carNumbersNerCheckbox', 'CarNumber', function () {
            var $this = $(this), carNumber = $this.attr('carNumber');
            $this.attr('title', "car-number: '" + carNumber + "'");
        });
    };    
    function healthInsurancesNerShow() {
        nerShowRoutine('#healthInsurancesNerCheckbox', 'HealthInsurance', function () {
            var $this = $(this), healthInsuranceNumber = $this.attr('healthInsuranceNumber');
            $this.attr('title', "health-insurance: '" + healthInsuranceNumber + "'");
        });
    };
    function driverLicensesNerShow() {
        nerShowRoutine('#driverLicensesNerCheckbox', 'DriverLicense', function () {
            var $this = $(this), driverLicense = $this.attr('driverLicense');
            $this.attr('title', "driver-license: '" + driverLicense + "'");
        });
    };
    function socialSecuritiesNerShow() {
        nerShowRoutine('#socialSecuritiesNerCheckbox', 'SocialSecurity', function () {
            var $this = $(this), socialSecurity = $this.attr('socialSecurity');
            $this.attr('title', "social-security: '" + socialSecurity + "'");
        });
    };
    function taxIdentificationsNerShow() {
        nerShowRoutine('#taxIdentificationsNerCheckbox', 'TaxIdentification', function () {
            var $this = $(this), taxIdentification = $this.attr('taxIdentification');
            $this.attr('title', "tax-identification: '" + taxIdentification + "'")
        });
    };
    function companiesNerShow() {
        nerShowRoutine('#companiesNerCheckbox', 'Company', function () {
            var $this = $(this), companyName = $this.attr('companyName');
            $this.attr('title', "company: '" + companyName + "'")
        });
    };
    function nerShowRoutine(ner_checkbox_id, nerClass, processItemFunc) {
        if ($(ner_checkbox_id).is(':checked')) {
            $('span.--' + nerClass + '--').removeClass('--' + nerClass + '--').addClass(nerClass);
            $('span.' + nerClass).each(processItemFunc);
        } else {
            $('span.' + nerClass).removeClass(nerClass).addClass('--' + nerClass + '--').attr('title', '');
        }
    };

    var ner_checkboxs = [
        { id: '#addressNerCheckbox', click: addressNerShow },
        { id: '#phonesNerCheckbox', click: phonesNerShow },
        { id: '#urlsNerCheckbox', click: urlsNerShow },
        { id: '#banksNerCheckbox', click: banksNerShow },
        { id: '#customerNumbersNerCheckbox', click: customerNumbersNerShow },
        { id: '#namesNerCheckbox', click: namesNerShow },
        { id: '#birthdaysNerCheckbox', click: birthdaysNerShow },
        { id: '#birthplacesNerCheckbox', click: birthplacesNerShow },
        { id: '#maritalStatusesNerCheckbox', click: maritalStatusesNerShow },
        { id: '#nationalitiesNerCheckbox', click: nationalitiesNerShow },
        { id: '#creditCardsNerCheckbox', click: creditCardsNerShow },
        { id: '#passportIdCardNumbersNerCheckbox', click: passportIdCardNumbersNerShow },
        { id: '#carNumbersNerCheckbox', click: carNumbersNerShow },
        { id: '#healthInsurancesNerCheckbox', click: healthInsurancesNerShow },
        { id: '#driverLicensesNerCheckbox', click: driverLicensesNerShow },
        { id: '#socialSecuritiesNerCheckbox', click: socialSecuritiesNerShow },
        { id: '#taxIdentificationsNerCheckbox', click: taxIdentificationsNerShow },
        { id: '#companiesNerCheckbox', click: companiesNerShow },
        { id: '#unitedEntitiesNerCheckbox', click: unitedEntitiesNerShow }
    ];
    ner_checkboxs.forEach(o => $(o.id).click(o.click));
    $(ner_checkboxs.map((o) => o.id).join(','))
        .each(function (_, ch) { $(ch).prop('checked', localStorage.getItem(config.LOCALSTORAGE_KEY + $(ch).attr('id')) !== 'false'); })
        .click(function () { var isck = $(this).is(':checked'), key = config.LOCALSTORAGE_KEY + $(this).attr('id'); if (!isck) localStorage.setItem(key, 'false'); else localStorage.removeItem(key); });
    $('#checkUncheckAllCheckbox').click(function () { $(ner_checkboxs.map((o) => o.id).filter(id => id !== '#unitedEntitiesNerCheckbox').join(',')).prop('checked', !this.checked).click(); });
    $('#bookmarkFirst, #bookmarkLast').click(function () { scroll_to_href($(this)); return (false); });

    var scroll_to_window_hash = function () { try { $(window).trigger('hashchange'); var $p = $('#processResult'), $x = $(window.location.hash); $p.animate({ scrollTop: $p.scrollTop() + $x.offset().top - $p.offset().top }, 0); } catch (e) { ; } },
        scroll_to_href = function ($a) {
            var $p = $('#processResult'), href = $a.attr('href'), $x = $('span[id="' + href.substring(1) + '"]');
            $p.animate({ scrollTop: $p.scrollTop() + $x.offset().top - $p.offset().top }, 170, 'swing', function () { window.location.hash = href; });
        };
    var next = function () {
        var $a = $(this), ner = $a.attr('ner'), n = parseInt($a.attr('ner-num')), $ss = $('span[id^="' + ner + '-"]');
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
        var $a = $(this), ner = $a.attr('ner'), n = parseInt($a.attr('ner-num')), $ss = $('span[id^="' + ner + '-"]');
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
    for (var i = ner_prefixes.length - 1; 0 <= i; i--) next_prev(ner_prefixes[i]);
});