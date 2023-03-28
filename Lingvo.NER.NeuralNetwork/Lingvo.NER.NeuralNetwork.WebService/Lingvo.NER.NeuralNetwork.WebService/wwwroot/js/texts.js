var MAX_INPUTTEXT_LENGTH       = 100000,
    LOCALSTORAGE_KEY           = 'Lingvo.NER.NeuralNetwork.',
    LOCALSTORAGE_TEXT_KEY      = LOCALSTORAGE_KEY + 'text',
    LOCALSTORAGE_MODELTYPE_KEY = LOCALSTORAGE_KEY + 'modelType',
    //LOCALSTORAGE_MAXPREDICTSENTLENGTH_KEY = LOCALSTORAGE_KEY + 'maxPredictSentLength',
    DEFAULT_TEXT = 'das ist fantastisch.' +
'\r\n\r\n' +
'Angela Dorothea Merkel (geb. Kasner; * 17. Juli 1954 in Hamburg) ist eine deutsche Politikerin (CDU). Sie ist seit dem 22. November 2005 Bundeskanzlerin der Bundesrepublik Deutschland. Von April 2000 bis Dezember 2018 war sie Bundesvorsitzende der CDU.' +
'\r\n\r\n' +
'Merkel wuchs in der DDR auf und war dort als Physikerin am Zentralinstitut für Physikalische Chemie tätig. Erstmals politisch aktiv wurde sie während der Wendezeit in der Partei Demokratischer Aufbruch, die sich 1990 der CDU anschloss. In der ersten und letzten demokratisch gewählten Regierung der DDR übte sie das Amt der stellvertretenden Regierungssprecherin aus.' +
'\r\n\r\n' +
'Bei der Bundestagswahl am 2. Dezember 1990 errang sie erstmals ein Bundestagsmandat. Bei den folgenden sieben Bundestagswahlen wurde sie in ihrem Wahlkreis in Vorpommern direkt gewählt. Von 1991 bis 1994 war Merkel Bundesministerin für Frauen und Jugend im Kabinett Kohl IV und von 1994 bis 1998 Bundesministerin für Umwelt, Naturschutz und Reaktorsicherheit im Kabinett Kohl V. Von 1998 bis zu ihrer Wahl zur Bundesvorsitzenden der Partei im Jahr 2000 amtierte sie als Generalsekretärin der CDU.' +
'\r\n\r\n' +
'Leonhard Euler (lateinisch Leonhardus Eulerus; * 15. April 1707 in Basel; † 7. Septemberjul. / 18. September 1783greg. in Sankt Petersburg) war ein Schweizer Mathematiker, Physiker, Astronom, Geograph, Logiker und Ingenieur.\r\n' +
'\r\n' +
'Er machte wichtige und weitreichende Entdeckungen in vielen Zweigen der Mathematik, wie beispielsweise der Infinitesimalrechnung und der Graphentheorie. Gleichzeitig leistete Euler fundamentale Beiträge auf anderen Gebieten wie der Topologie und der analytischen Zahlentheorie. Er prägte grosse Teile der bis heute weltweit gebräuchlichen mathematischen Terminologie und Notation. Beispielsweise führte Euler den Begriff der mathematischen Funktion in die Analysis ein. Er ist zudem für seine Arbeiten in der Mechanik, Strömungsdynamik, Optik, Astronomie und Musiktheorie bekannt.\r\n' +
'\r\n' +
'Euler, der den grössten Teil seines Lebens in Sankt Petersburg und in Berlin verbrachte, war einer der bedeutendsten Mathematiker des 18. Jahrhunderts. Seine herausragenden Leistungen ebbten auch nach seiner Erblindung im Jahre 1771 nicht ab und wurden bereits von seinen Zeitgenossen anerkannt. Er gilt heute als einer der brillantesten und produktivsten Mathematiker aller Zeiten. Seine gesammelten Schriften Opera omnia umfassen bisher 76 Bände – ein mathematisches Werk, dessen Umfang bis heute unerreicht bleibt.\r\n' +
'\r\n' +
'Leonhard Euler zu Ehren erhielten zwei mathematische Konstanten seinen Namen: die Eulersche Zahl (Basis des natürlichen Logarithmus) und die Euler-Mascheroni-Konstante aus der Zahlentheorie, die gelegentlich auch Eulersche Konstante genannt wird.\r\n' +
'\r\n' +
'Leonhard Eulers Arbeiten inspirierten viele Generationen von Mathematikern, darunter Pierre-Simon Laplace, Carl Gustav Jacobi und Carl Friedrich Gauß, nachhaltig. Laplace soll zu seinen Schülern gesagt haben: «Lest Euler, er ist unser aller Meister!».';
