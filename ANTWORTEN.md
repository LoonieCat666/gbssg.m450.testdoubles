# Antworten zu den Aufgaben

Gewähltes Testframework: **xUnit** (`tests/TestDoublesExercise.Tests`).

## Aufgabe 1: Nur die Schnittstelle ist bekannt

`ReservationService` hängt nur von der Schnittstelle `ISeatAvailability` ab, nicht von einer konkreten Klasse. Darum kann ich im Testprojekt einen eigenen Stub (`SeatAvailabilityStub`) übergeben und den Service schon jetzt testen.

Die Schnittstelle sagt bereits: Es gibt eine Methode `GetAvailableSeats`, sie bekommt einen Veranstaltungscode (`string`) und gibt eine Anzahl freier Plätze (`int`) zurück. Sie sagt nicht, woher die Zahl kommt und was bei einem unbekannten Code passiert.

Meine fachliche Annahme: `GetAvailableSeats` gibt für einen Veranstaltungscode die Anzahl der aktuell noch freien Plätze zurück, nie einen negativen Wert. Für einen unbekannten Code liefert mein Stub 0 freie Plätze.

## Aufgabe 2: Fehlende Aufrufe sichtbar machen

Ich verwende Doubles, die jeden Aufruf mitschreiben: Der Stub merkt sich die abgefragten Veranstaltungscodes, der `RepositorySpy` die gespeicherten Reservierungen und der `ConfirmationSenderSpy` die versendeten Bestätigungen. Nach der Exception prüfe ich, dass alle drei Listen leer sind. Der Produktionscode muss dafür nicht geändert werden.

## Aufgabe 3: Stub und Spy

Mein Stub liefert dem Service kontrollierte Daten (die Anzahl freier Plätze), damit der Test einen bestimmten Fall herstellen kann. Mein Spy liefert nichts zurück, sondern zeichnet die Aufrufe auf, damit der Test nachher prüfen kann, ob und womit der Service ihn aufgerufen hat.

## Aufgabe 5: Handgeschrieben oder Moq?

Für die Verfügbarkeit und den Sender verwende ich meine handgeschriebenen Doubles (`SeatAvailabilityStub`, `ConfirmationSenderSpy`), weil sie schon existieren und die Prüfung mit `Assert.Empty(...)` sehr gut lesbar ist. Für das Repository verwende ich Moq, weil ich mit `Verify(..., Times.Never())` und `VerifyNoOtherCalls()` ohne zusätzlichen Code prüfen kann, dass keine einzige Methode des Repositorys aufgerufen wurde. Ein handgeschriebener Spy müsste dafür jede Methode einzeln mitschreiben.

## Fortgeschrittene: `Send` wirft nach erfolgreichem Speichern eine Exception

Die Reservierung ist dann bereits gespeichert, aber der Kunde bekommt keine Bestätigung (oder bei `ReserveBatch` nur ein Teil der Kunden). Der Aufrufer bekommt eine Exception und denkt eventuell, die Reservierung sei fehlgeschlagen. Er versucht es nochmals, und es entsteht eine doppelte Reservierung. Die Plätze sind belegt, obwohl der Kunde nichts davon weiss.

Mögliche fachliche Lösung: Die Bestätigung wird nicht direkt verschickt, sondern als „offene Bestätigung“ zusammen mit der Reservierung gespeichert (Outbox). Ein separater Prozess verschickt offene Bestätigungen und wiederholt den Versand bei Fehlern, bis er gelingt. Die Reservierung bleibt gültig, und der Kunde kann seine Reservierung zusätzlich im Kundenkonto einsehen.

## Tests ausführen

```bash
dotnet test tests/TestDoublesExercise.Tests/TestDoublesExercise.Tests.csproj
```
