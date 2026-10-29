namespace TestDoublesExercise.Tests;

// Stub: liefert für festgelegte Veranstaltungscodes eine feste Anzahl freier Plätze.
// Er berechnet keine echte Verfügbarkeit. Unbekannte Codes haben 0 freie Plätze.
// Die abgefragten Codes werden mitgeschrieben, damit Tests fehlende oder doppelte
// Abfragen sichtbar machen können.
internal sealed class SeatAvailabilityStub : ISeatAvailability
{
    private readonly Dictionary<string, int> _availableSeatsPerEvent;
    private readonly List<string> _requestedEventCodes = [];

    public SeatAvailabilityStub(string eventCode, int availableSeats)
        : this(new Dictionary<string, int> { [eventCode] = availableSeats })
    {
    }

    public SeatAvailabilityStub(Dictionary<string, int> availableSeatsPerEvent)
    {
        _availableSeatsPerEvent = availableSeatsPerEvent;
    }

    public IReadOnlyList<string> RequestedEventCodes => _requestedEventCodes;

    public int GetAvailableSeats(string eventCode)
    {
        _requestedEventCodes.Add(eventCode);
        return _availableSeatsPerEvent.TryGetValue(eventCode, out var seats) ? seats : 0;
    }
}

// Einfaches Repository-Double: Speichern gelingt immer, es wird nichts geprüft.
internal sealed class AlwaysSucceedingRepository : IReservationRepository
{
    public bool TrySave(Reservation reservation) => true;
}

// Repository-Spy: liefert ein festgelegtes Ergebnis und zeichnet alle Speicherversuche auf.
internal sealed class RepositorySpy(bool saveResult) : IReservationRepository
{
    private readonly List<Reservation> _savedReservations = [];

    public IReadOnlyList<Reservation> SavedReservations => _savedReservations;

    public bool TrySave(Reservation reservation)
    {
        _savedReservations.Add(reservation);
        return saveResult;
    }
}

// Sender, der nichts tut (Dummy) – für Tests, in denen die Bestätigung keine Rolle spielt.
internal sealed class NoOpConfirmationSender : IConfirmationSender
{
    public void Send(Reservation reservation)
    {
    }
}

// Spy: zeichnet alle übergebenen Reservierungen in ihrer Reihenfolge auf.
internal sealed class ConfirmationSenderSpy : IConfirmationSender
{
    private readonly List<Reservation> _sentReservations = [];

    public IReadOnlyList<Reservation> SentReservations => _sentReservations;

    public void Send(Reservation reservation) => _sentReservations.Add(reservation);
}
