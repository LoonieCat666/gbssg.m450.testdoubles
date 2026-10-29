# Antworten

Ich hab xUnit genommen.

## Aufgabe 1

Der ReservationService braucht ja nur das Interface ISeatAvailability und keine echte Klasse. Also kann man einfach einen eigenen Stub reingeben (SeatAvailabilityStub) und schon testen, auch wenn es die richtige Implementierung noch nicht gibt.

Vom Interface weiss man nur, dass GetAvailableSeats einen Veranstaltungscode bekommt und eine Zahl zurückgibt. Woher die Zahl kommt weiss man nicht.

Meine Annahme: es kommt die Anzahl freie Plätze zurück und die ist nie negativ. Wenn der Code unbekannt ist gibt mein Stub einfach 0 zurück.

## Aufgabe 2

Meine Doubles schreiben mit wann sie aufgerufen werden (Stub merkt sich die Codes, die Spies die Reservationen). Nach der Exception schau ich einfach ob die Listen leer sind. Dann weiss ich dass nichts aufgerufen wurde, ohne dass ich am Service was ändern muss.

## Aufgabe 3

Der Stub gibt dem Service Daten zurück (wie viele Plätze frei sind), der Spy gibt nichts zurück sondern merkt sich nur was mit ihm gemacht wurde damit ich das nachher prüfen kann.

## Aufgabe 5

Für Verfügbarkeit und Sender hab ich meine eigenen Doubles genommen, die hatte ich eh schon und Assert.Empty ist einfach zu lesen. Fürs Repository hab ich Moq genommen weil man mit Times.Never() und VerifyNoOtherCalls() direkt sieht dass gar nichts aufgerufen wurde, sonst hätte ich im Spy jede Methode einzeln mitschreiben müssen.

## Fortgeschrittene: was wenn Send eine Exception wirft?

Dann ist die Reservation schon gespeichert aber der Kunde kriegt keine Bestätigung (bei ReserveBatch vielleicht nur ein paar). Der Aufrufer sieht nur die Exception und denkt es hat nicht geklappt, probiert nochmal und dann ist es doppelt reserviert.

Lösung wäre z.B. dass man die Bestätigung nicht direkt schickt sondern zuerst als "noch offen" mit der Reservation abspeichert. Dann schickt ein anderer Prozess die offenen Bestätigungen und probiert es halt nochmal wenn es nicht geht.
