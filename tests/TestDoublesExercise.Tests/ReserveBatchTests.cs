using Moq;

namespace TestDoublesExercise.Tests;

// Grössere Aufgabe für Fortgeschrittene: Gruppenreservierung
public sealed class ReserveBatchTests
{
    private const string Concert = "CONCERT-1";
    private const string Theatre = "THEATRE-7";

    // Regel 1: Ungültige Eingaben ------------------------------------------

    [Fact]
    public void ReserveBatch_WithNull_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(Concert, availableSeats: 10);
        var repository = new RepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act
        Action action = () => service.ReserveBatch(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        Assert.Empty(repository.SavedBatches);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void ReserveBatch_WithEmptyList_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(Concert, availableSeats: 10);
        var repository = new RepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act
        Action action = () => service.ReserveBatch([]);

        // Assert
        Assert.Throws<ArgumentException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        Assert.Empty(repository.SavedBatches);
        Assert.Empty(sender.SentReservations);
    }

    [Theory]
    [InlineData("", Concert, 1)]
    [InlineData("   ", Concert, 1)]
    [InlineData("anna@example.com", "", 1)]
    [InlineData("anna@example.com", "  ", 1)]
    public void ReserveBatch_WithMissingEmailOrEventCode_ThrowsBeforeCallingDependencies(
        string email, string eventCode, int seatCount)
    {
        // Arrange: Der ungültige Eintrag steht bewusst an zweiter Stelle.
        var availability = new SeatAvailabilityStub(Concert, availableSeats: 10);
        var repository = new RepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);
        Reservation[] requests =
        [
            new("ben@example.com", Concert, 1),
            new(email, eventCode, seatCount),
        ];

        // Act
        Action action = () => service.ReserveBatch(requests);

        // Assert
        Assert.Throws<ArgumentException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        Assert.Empty(repository.SavedBatches);
        Assert.Empty(sender.SentReservations);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReserveBatch_WithNonPositiveSeatCount_ThrowsBeforeCallingDependencies(int seatCount)
    {
        // Arrange
        var availability = new SeatAvailabilityStub(Concert, availableSeats: 10);
        var repository = new Mock<IReservationRepository>();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);
        Reservation[] requests =
        [
            new("ben@example.com", Concert, 1),
            new("anna@example.com", Concert, seatCount),
        ];

        // Act
        Action action = () => service.ReserveBatch(requests);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.SentReservations);
    }

    // Regel 2 und 3: Plätze pro Veranstaltung addieren ----------------------

    [Fact]
    public void ReserveBatch_WhenCombinedSeatsExceedCapacity_DoesNotSaveOrSend()
    {
        // Arrange: Je 2 Plätze passen einzeln in 3 freie Plätze, zusammen (4) aber nicht.
        var availability = new SeatAvailabilityStub(Concert, availableSeats: 3);
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySaveBatch(It.IsAny<IReadOnlyList<Reservation>>())).Returns(true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);
        Reservation[] requests =
        [
            new("anna@example.com", Concert, 2),
            new("ben@example.com", Concert, 2),
        ];

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.False(result);
        Assert.Equal([Concert], availability.RequestedEventCodes);
        repository.Verify(repo => repo.TrySaveBatch(It.IsAny<IReadOnlyList<Reservation>>()), Times.Never());
        repository.Verify(repo => repo.TrySave(It.IsAny<Reservation>()), Times.Never());
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void ReserveBatch_WhenOneOfSeveralEventsHasTooFewSeats_DoesNotSaveOrSend()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(new Dictionary<string, int>
        {
            [Concert] = 10,
            [Theatre] = 1,
        });
        var repository = new RepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);
        Reservation[] requests =
        [
            new("anna@example.com", Concert, 2),
            new("ben@example.com", Theatre, 2),
        ];

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.False(result);
        Assert.Empty(repository.SavedBatches);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void ReserveBatch_QueriesAvailabilityExactlyOncePerDistinctEvent()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(new Dictionary<string, int>
        {
            [Concert] = 10,
            [Theatre] = 10,
        });
        var service = new ReservationService(availability, new AlwaysSucceedingRepository(), new NoOpConfirmationSender());
        Reservation[] requests =
        [
            new("anna@example.com", Concert, 1),
            new("ben@example.com", Theatre, 1),
            new("carla@example.com", Concert, 1),
        ];

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.True(result);
        Assert.Equal(2, availability.RequestedEventCodes.Count);
        Assert.Single(availability.RequestedEventCodes, Concert);
        Assert.Single(availability.RequestedEventCodes, Theatre);
    }

    [Fact]
    public void ReserveBatch_WhenCombinedSeatsExactlyMatchCapacity_Succeeds()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(Concert, availableSeats: 4);
        var service = new ReservationService(availability, new AlwaysSucceedingRepository(), new NoOpConfirmationSender());
        Reservation[] requests =
        [
            new("anna@example.com", Concert, 2),
            new("ben@example.com", Concert, 2),
        ];

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.True(result);
    }

    // Regel 4: Alle Reservierungen in einem Aufruf speichern ---------------

    [Fact]
    public void ReserveBatch_WithEnoughSeats_SavesAllReservationsInOneBatchCall_Moq()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(new Dictionary<string, int>
        {
            [Concert] = 5,
            [Theatre] = 5,
        });
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySaveBatch(It.IsAny<IReadOnlyList<Reservation>>())).Returns(true);
        var service = new ReservationService(availability, repository.Object, new ConfirmationSenderSpy());
        Reservation[] requests =
        [
            new("anna@example.com", Concert, 2),
            new("ben@example.com", Theatre, 3),
            new("carla@example.com", Concert, 1),
        ];

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.True(result);
        repository.Verify(
            repo => repo.TrySaveBatch(It.Is<IReadOnlyList<Reservation>>(saved =>
                saved.Count == 3
                && saved[0].CustomerEmail == "anna@example.com" && saved[0].EventCode == Concert && saved[0].SeatCount == 2
                && saved[1].CustomerEmail == "ben@example.com" && saved[1].EventCode == Theatre && saved[1].SeatCount == 3
                && saved[2].CustomerEmail == "carla@example.com" && saved[2].EventCode == Concert && saved[2].SeatCount == 1)),
            Times.Once());
        repository.Verify(repo => repo.TrySave(It.IsAny<Reservation>()), Times.Never());
        repository.VerifyNoOtherCalls();
    }

    // Regel 5: Bestätigungen ------------------------------------------------

    [Fact]
    public void ReserveBatch_WhenBatchSaveFails_ReturnsFalseAndSendsNothing()
    {
        // Arrange
        var repository = new RepositorySpy(saveResult: false);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(Concert, availableSeats: 10), repository, sender);
        Reservation[] requests =
        [
            new("anna@example.com", Concert, 2),
            new("ben@example.com", Concert, 2),
        ];

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.False(result);
        Assert.Single(repository.SavedBatches);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void ReserveBatch_WhenSaved_SendsOneConfirmationPerRequestInInputOrder()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(new Dictionary<string, int>
        {
            [Concert] = 10,
            [Theatre] = 10,
        });
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, new RepositorySpy(saveResult: true), sender);
        Reservation[] requests =
        [
            new("anna@example.com", Concert, 2),
            new("ben@example.com", Theatre, 1),
            new("carla@example.com", Concert, 3),
        ];

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.True(result);
        Assert.Equal(requests, sender.SentReservations);
    }
}
