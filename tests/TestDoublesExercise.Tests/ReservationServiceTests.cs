using Moq;

namespace TestDoublesExercise.Tests;

public sealed class ReservationServiceTests
{
    private const string Email = "anna@example.com";
    private const string EventCode = "CONCERT-1";

    // ---------------------------------------------------------------------
    // Aufgabe 1: Verfügbarkeits-Stub (nur die Schnittstelle ist bekannt)
    // ---------------------------------------------------------------------

    [Fact]
    public void Reserve_WithEnoughSeats_ReturnsTrue()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 3);
        var service = new ReservationService(availability, new AlwaysSucceedingRepository(), new NoOpConfirmationSender());

        // Act
        var result = service.Reserve(Email, EventCode, seatCount: 2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_ReturnsFalse()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 1);
        var service = new ReservationService(availability, new AlwaysSucceedingRepository(), new NoOpConfirmationSender());

        // Act
        var result = service.Reserve(Email, EventCode, seatCount: 2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Reserve_WithExactlyEnoughSeats_Succeeds()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 2);
        var service = new ReservationService(availability, new AlwaysSucceedingRepository(), new NoOpConfirmationSender());

        // Act
        var result = service.Reserve(Email, EventCode, seatCount: 2);

        // Assert
        Assert.True(result);
    }

    // ---------------------------------------------------------------------
    // Aufgabe 2: Grenzfälle und Isolation
    // Fehlende Aufrufe werden sichtbar, weil Stub und Spies jeden Aufruf mitschreiben.
    // ---------------------------------------------------------------------

    [Fact]
    public void Reserve_WithZeroSeats_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 3);
        var repository = new RepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act
        Action action = () => service.Reserve(Email, EventCode, seatCount: 0);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithNegativeSeats_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 3);
        var repository = new RepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act
        Action action = () => service.Reserve(Email, EventCode, seatCount: -1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithEmptyEventCode_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 3);
        var repository = new RepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act
        Action action = () => service.Reserve(Email, eventCode: "", seatCount: 2);

        // Assert
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("eventCode", exception.ParamName);
        Assert.Empty(availability.RequestedEventCodes);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    // ---------------------------------------------------------------------
    // Aufgabe 3: Bestätigungs-Spy
    // ---------------------------------------------------------------------

    [Fact]
    public void Reserve_WhenSaved_SendsExactlyOneConfirmationWithReservationData()
    {
        // Arrange
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, availableSeats: 3),
            new AlwaysSucceedingRepository(),
            sender);

        // Act
        var result = service.Reserve(Email, EventCode, seatCount: 2);

        // Assert
        Assert.True(result);
        var confirmation = Assert.Single(sender.SentReservations);
        Assert.Equal(Email, confirmation.CustomerEmail);
        Assert.Equal(EventCode, confirmation.EventCode);
        Assert.Equal(2, confirmation.SeatCount);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotSaveOrSend()
    {
        // Arrange
        var repository = new RepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, availableSeats: 1), repository, sender);

        // Act
        var result = service.Reserve(Email, EventCode, seatCount: 2);

        // Assert
        Assert.False(result);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WhenSavingFails_DoesNotSendConfirmation()
    {
        // Arrange
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, availableSeats: 3),
            new RepositorySpy(saveResult: false),
            sender);

        // Act
        var result = service.Reserve(Email, EventCode, seatCount: 2);

        // Assert
        Assert.False(result);
        Assert.Empty(sender.SentReservations);
    }

    // ---------------------------------------------------------------------
    // Aufgabe 4: Moq für das Repository
    // ---------------------------------------------------------------------

    [Fact]
    public void Reserve_WithEnoughSeats_SavesCorrectReservationOnce_Moq()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySave(It.IsAny<Reservation>())).Returns(true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, availableSeats: 3), repository.Object, sender);

        // Act
        var result = service.Reserve(Email, EventCode, seatCount: 2);

        // Assert
        Assert.True(result);
        repository.Verify(
            repo => repo.TrySave(It.Is<Reservation>(r =>
                r.CustomerEmail == Email && r.EventCode == EventCode && r.SeatCount == 2)),
            Times.Once());
        repository.VerifyNoOtherCalls();
        Assert.Equal(new Reservation(Email, EventCode, 2), Assert.Single(sender.SentReservations));
    }

    [Fact]
    public void Reserve_WhenSavingFails_ReturnsFalseAndSendsNothing_Moq()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySave(It.IsAny<Reservation>())).Returns(false);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, availableSeats: 3), repository.Object, sender);

        // Act
        var result = service.Reserve(Email, EventCode, seatCount: 2);

        // Assert
        Assert.False(result);
        repository.Verify(
            repo => repo.TrySave(It.Is<Reservation>(r =>
                r.CustomerEmail == Email && r.EventCode == EventCode && r.SeatCount == 2)),
            Times.Once());
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotCallRepository()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySave(It.IsAny<Reservation>())).Returns(true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, availableSeats: 1), repository.Object, sender);

        // Act
        var result = service.Reserve(Email, EventCode, seatCount: 2);

        // Assert
        Assert.False(result);
        repository.Verify(repo => repo.TrySave(It.IsAny<Reservation>()), Times.Never());
        Assert.Empty(sender.SentReservations);
    }

    // ---------------------------------------------------------------------
    // Aufgabe 5: Handgeschrieben oder Moq?
    // Verfügbarkeit: eigener Stub (zeichnet Abfragen auf, bereits vorhanden).
    // Repository: Moq, weil Verify(..., Times.Never()) ohne eigenen Code zeigt,
    //             dass TrySave nicht aufgerufen wurde.
    // Sender: eigener Spy, weil er schon existiert und leicht lesbar ist.
    // ---------------------------------------------------------------------

    [Fact]
    public void Reserve_WithWhitespaceEmail_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 3);
        var repository = new Mock<IReservationRepository>();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);

        // Act
        Action action = () => service.Reserve("   ", EventCode, seatCount: 2);

        // Assert
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("customerEmail", exception.ParamName);
        Assert.Empty(availability.RequestedEventCodes);
        repository.Verify(repo => repo.TrySave(It.IsAny<Reservation>()), Times.Never());
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.SentReservations);
    }
}
