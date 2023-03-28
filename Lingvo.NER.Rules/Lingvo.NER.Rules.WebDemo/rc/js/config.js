var config = (function () {
    var MAX_INPUTTEXT_LENGTH  = 1000000,
        LOCALSTORAGE_KEY      = 'Lingvo.NER.Rules.'
        LOCALSTORAGE_TEXT_KEY = LOCALSTORAGE_KEY + 'text',

        DEFAULT_TEXT = 
'JULIA BRUMM; Julia Brumm \r\n' +
'----------------------------------- \r\n' +
'Verwendete Literatur: \r\n' +
'Petra, Freudenberger-Lötz und Anita, Müller-Friese: Schatztruhe Religion. Materialien für den fächerverbindenden Unterricht in der Grundschule. Teil 1.  \r\n' +
'x Freudenberger-Lotz, Petra  x \r\n' +
'Freudenberger-Lötz, Petra und Müller-Friese, Anita: Schatztruhe Religion. Materialien für den fächerverbindenden Unterricht in der Grundschule. Teil 1. Calwer. Stuttgart. 2005 \r\n' +
'Wuckelt, Agnes und Seifert, Viola: Ich bin Naomi und wer bist du? Interreligiöses Lernen in der Grundschulee \r\n' +
'---------------------------------- \r\n' +
'Man kann gar nicht genug Namen verwenden, Gabi. Schmidt Spiele ist ein sehr alter Hersteller von Gesellschaftsspielen. \r\n' +
'Max Goldt aber auch. Goldt ist dazu noch ein guter Autor. Sten Laurel und Oliver Hardy waren Ikonen \r\n' +
'Terms like Er, Mein or Oma are not Names and should not be found.  \r\n' +
'Theo ist Klaras kleiner Bruder. Er ist noch winzig \r\n' +
'Ich habe das Krokodil von Nadine, Brigitte. Mein Name ist Anne und das ist ein Krokodil…. \r\n' +
'Oma und Opa aus Frankfurt. Oma und Opa aus Nassau, Tante Katja und Onkel Marc sind gekommen. \r\n' +
'---------------------------------- \r\n' +
'Ein Fließtext zum Verstecken von persönlichen Informationen. Thorsten Wiese. Da kann einfach mal was versteckt sein, ohne, dass man es merkt. Hier vielleicht oder hier Teststraße 12, 99998 Mülheim an Ruhr.  \r\n' +
' \r\n' +
'Uwe Rösler \r\n' +
'Andrea Hübner \r\n' +
'Geboren: 1.1.1980 / Köln \r\n' +
'Familienstand: ledig \r\n' +
'Fantasiestr. 11 \r\n' +
'12345 Beispielstadt \r\n' +
'0123 / 45 67 89 0 \r\n' +
'tel. 0123 / 45 67 89 0 \r\n' +
'2045 / 45 67 89 0 \r\n' +
'Vertretungsberechtigter Geschäftsführer ist Konrad Dießl - Tel 089/40287399 - Fax 089/40287430 - info@test-zahnzusatzversicherung.de – www.test-zahnzusatzversicherung.de. \r\n' +
' \r\n' +
'Abrechnungszeitpunkt Geburtsdatum: 13.07.2020  \r\n' +
'IBAN: DE8XXXXXXXXXXXXXXXX469  \r\n' +
'BIC: PBNKDEFFXXX  \r\n' +
'Name der Bank: Postbank  \r\n' +
'Mandatsreferenz: 0020001759416  \r\n' +
'Gläubiger-Identifikationsnummer: DE74ZZZ00000045294  \r\n' +
'Aus technischen Gründen kann es vorkommen, dass der oben genannte Betrag um mehrere Tage verzögert abgebucht wird.  \r\n' +
'Unter mein.ionos.de finden Sie alle Informationen zu Ihren Abrechnungen. Antworten auf Fragen rund um Ihre Rechnungen und Verträge finden Sie zudem im  \r\n' +
'Hilfe-Center.  \r\n' +
'Vorstand: Dr. Christian Böing, Hüseyin Dogan, Dr. Martin Endreß, Hans-Henning Kettler, Arthur Mai, Matthias Steinberg, Achim Weiss  \r\n' +
'Hauptsitz Montabaur, Amtsgericht Montabaur, HRB 24498 · USt-IdNr.: DE815563912  \r\n' +
'Commerzbank AG, BLZ 200 400 00, Konto 630 148 502, IBAN DE83 2004 0000 0630 1485 02, BIC COBADEHHXXX  \r\n' +
'Rechnungsdatum: 14.07.2020  \r\n' +
'Rechnungsnummer: 100073902461  \r\n' +
'Vertragsnummer: V69747615  \r\n' +
'Kundennummer: K6398314  \r\n' +
'Brauchen Sie Hilfe: www.ionos.de/hilfe  \r\n' +
'Mein IONOS: mein.ionos.de/invoices  \r\n' +
'E-Mail: rechnungsstelle@ionos.de  \r\n' +
'Telefon: 0721 170 555  \r\n' +
'Servicezeiten: täglich rund um die Uhr  \r\n' +
'Bitte halten Sie für Gespräche mit unseren  \r\n' +
'Mitarbeitern Ihre persönliche Telefon PIN zur  \r\n' +
'schnellen und sicheren Authentifizierung bereit.  \r\n' +
'Diese können Sie unter mein.ionos.de setzen  \r\n' +
'und verwalten.  \r\n' +
'Herr  \r\n' +
'Maxim Tarassenko  \r\n' +
'Schultenberg 54  \r\n' +
'45470 Mülheim an der Ruhr  \r\n' +
'Rechnung  \r\n' +
'1&1 IONOS SE  \r\n' +
'Elgendorfer Str. 57  \r\n' +
'56410 Montabaur  \r\n' +
'Kopie vom 16.08.2020 \r\n' +
' \r\n' +
'Abrechnungszeitpunkt: 13.07.2020  \r\n' +
'Angela Merkel - Koch, Zuzanna Koch-Muller \r\n' +
'Ihre Rechnung (SSL)  \r\n' +
'Geboren: 1.1.1980 / Köln \r\n' +
'Die Daten Ihres SEPA-Lastschriftmandats lauten:  \r\n' +
'IBAN: DE8XXXXXXXXXXXXXXXX469  \r\n' +
'BIC: PBNKDEFFXXX  \r\n' +
'Name der Bank: Postbank  \r\n' +
'Mandatsreferenz: 0020001759416  \r\n' +
'Gläubiger-Identifikationsnummer: DE74ZZZ00000045294  \r\n' +
'5432110051294969, 5432 1100 5129 4969  \r\n' +
'Aus technischen Gründen kann es vorkommen, dass der oben genannte Betrag um mehrere Tage verzögert abgebucht wird.  \r\n' +
'L6Z3PGVYC ; L6Z3PGVYC3 ; LF3ZT4WC0  ; LF3ZT4WC09; \r\n' +
'Unter mein.ionos.de finden Sie alle Informationen zu Ihren Abrechnungen. Antworten auf Fragen rund um Ihre Rechnungen und Verträge finden Sie zudem im  \r\n' +
'Hilfe-Center.  \r\n' +
'Vorstand: Dr. Christian Böing, Hüseyin Dogan, Dr. Martin Endreß, Hans-Henning Kettler, Arthur Mai, Matthias Steinberg, Achim Weiss  \r\n' +
'Hauptsitz Montabaur, Amtsgericht Montabaur, HRB 24498 · USt-IdNr.: DE815563912  \r\n' +
'Commerzbank AG, BLZ 200 400 00, Konto 630 148 502, IBAN DE83 2004 0000 0630 1485 02, BIC COBADEHHXXX  \r\n' +
'Rechnungsdatum: 14.07.2020  \r\n' +
'Rechnungsnummer: 100073902461  \r\n' +
'Vertragsnummer: V69747615 , Kundennummer: K6398314; \r\n' +
'Brauchen Sie Hilfe: www.ionos.de/hilfe  \r\n' +
'Mein IONOS: mein.ionos.de/invoices  \r\n' +
'E-Mail: rechnungsstelle@ionos.de  \r\n' +
'Telefon: 0721 170 555  \r\n' +
'Herr  \r\n' +
'Maxim Tarassenko  \r\n' +
'Schultenberg 54  \r\n' +
'45470 Mülheim an der Ruhr  \r\n' +
'Staatsangehörigkeit russisch  \r\n' +
'Geburtsort Rotenburg (Wümme)  \r\n' +
'Beziehungsstatus eingetragene Lebenspartnerschaft  \r\n' +
'Rechnung  \r\n' +
'1&1 IONOS SE  \r\n' +
'Elgendorfer Str. 57  \r\n' +
'56410 Montabaur  \r\n' +
'Kopie vom 16.08.2020  \r\n' +
' \r\n' +
'Alexander Diener  \r\n' +
'Political Home democratic republic of the congo  \r\n' +
'Ursprungsort Garmisch-Partenkirchen  \r\n' +
'Familienstand ledig \r\n' +
' \r\n' +
'Health insurance: "I526064554, A123456780, K734027627, Z610573490, Q327812091" \r\n' +
'Driver licenses: "J010000SD51, N0704578035, F0100LQUA01, J430A1RZN11" \r\n' +
'Social securities: "53 270139 W 032, 13 020281 W 025, 04-150872-P-084" \r\n' +
'Tax identifications New: "Steueridentifikationsnr. 81872495633, Bundesweite Identifikationsnr. 67 624 305 982, Tax-ID 86 095 742 719, Tax Identification No. 57549285017,  Steuer IdNr.  25 768 131 411" \r\n' +
'Tax identifications Old: "Steuernummer  4151081508156, Steuer Nummer 013 815 08153, Steuernr.     151/815/08156" \r\n' +
'Car numbers: "D-KA1234, D-KA-8136" \r\n'
;

    return {
        MAX_INPUTTEXT_LENGTH : MAX_INPUTTEXT_LENGTH,
        LOCALSTORAGE_KEY     : LOCALSTORAGE_KEY,
        LOCALSTORAGE_TEXT_KEY: LOCALSTORAGE_TEXT_KEY,        
        DEFAULT_TEXT         : DEFAULT_TEXT
    };
})();