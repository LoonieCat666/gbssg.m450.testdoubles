namespace TestDoublesExercise.Tests;

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

internal sealed class AlwaysSucceedingRepository : IReservationRepository
{
    public bool TrySave(Reservation reservation) => true;
}

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

internal sealed class NoOpConfirmationSender : IConfirmationSender
{
    public void Send(Reservation reservation)
    {
    }
}

internal sealed class ConfirmationSenderSpy : IConfirmationSender
{
    private readonly List<Reservation> _sentReservations = [];

    public IReadOnlyList<Reservation> SentReservations => _sentReservations;

    public void Send(Reservation reservation) => _sentReservations.Add(reservation);
}
