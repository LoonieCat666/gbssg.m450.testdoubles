# gbssg.m450.testdoubles

Lösung zur Lernaufgabe **Testen mit Test Doubles** (M450), inklusive der grösseren Aufgabe für Fortgeschrittene (Gruppenreservierung).

- Aufgabenstellung: [`test-doubles/AUFGABE.md`](test-doubles/AUFGABE.md)
- Service inkl. `ReserveBatch`: [`test-doubles/ReservationService.cs`](test-doubles/ReservationService.cs)
- Eigene Doubles (Stub, Spies): [`tests/TestDoublesExercise.Tests/ReservationTestDoubles.cs`](tests/TestDoublesExercise.Tests/ReservationTestDoubles.cs)
- Tests Aufgaben 1–5: [`tests/TestDoublesExercise.Tests/ReservationServiceTests.cs`](tests/TestDoublesExercise.Tests/ReservationServiceTests.cs)
- Tests Gruppenreservierung: [`tests/TestDoublesExercise.Tests/ReserveBatchTests.cs`](tests/TestDoublesExercise.Tests/ReserveBatchTests.cs)
- Antworten zu Aufgabe 1, 2, 3, 5 und zum Fehler beim Bestätigungsversand: [`ANTWORTEN.md`](ANTWORTEN.md)

```bash
dotnet test tests/TestDoublesExercise.Tests/TestDoublesExercise.Tests.csproj
```
