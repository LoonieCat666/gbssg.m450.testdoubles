using Moq;

namespace TestDoublesExercise.Tests;

public sealed class ReservationServiceTests
{
    private const string Email = "anna@example.com";
    private const string EventCode = "CONCERT-1";

    [Fact]
    public void Reserve_WithEnoughSeats_ReturnsTrue()
    {
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 3);
        var service = new ReservationService(availability, new AlwaysSucceedingRepository(), new NoOpConfirmationSender());

        var result = service.Reserve(Email, EventCode, seatCount: 2);

        Assert.True(result);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_ReturnsFalse()
    {
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 1);
        var service = new ReservationService(availability, new AlwaysSucceedingRepository(), new NoOpConfirmationSender());

        var result = service.Reserve(Email, EventCode, seatCount: 2);

        Assert.False(result);
    }

    [Fact]
    public void Reserve_WithExactlyEnoughSeats_Succeeds()
    {
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 2);
        var service = new ReservationService(availability, new AlwaysSucceedingRepository(), new NoOpConfirmationSender());

        var result = service.Reserve(Email, EventCode, seatCount: 2);

        Assert.True(result);
    }

    [Fact]
    public void Reserve_WithZeroSeats_ThrowsBeforeCallingDependencies()
    {
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 3);
        var repository = new RepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        Action action = () => service.Reserve(Email, EventCode, seatCount: 0);

        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithNegativeSeats_ThrowsBeforeCallingDependencies()
    {
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 3);
        var repository = new RepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        Action action = () => service.Reserve(Email, EventCode, seatCount: -1);

        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithEmptyEventCode_ThrowsBeforeCallingDependencies()
    {
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 3);
        var repository = new RepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        Action action = () => service.Reserve(Email, eventCode: "", seatCount: 2);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("eventCode", exception.ParamName);
        Assert.Empty(availability.RequestedEventCodes);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WhenSaved_SendsExactlyOneConfirmationWithReservationData()
    {
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, availableSeats: 3),
            new AlwaysSucceedingRepository(),
            sender);

        var result = service.Reserve(Email, EventCode, seatCount: 2);

        Assert.True(result);
        var confirmation = Assert.Single(sender.SentReservations);
        Assert.Equal(Email, confirmation.CustomerEmail);
        Assert.Equal(EventCode, confirmation.EventCode);
        Assert.Equal(2, confirmation.SeatCount);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotSaveOrSend()
    {
        var repository = new RepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, availableSeats: 1), repository, sender);

        var result = service.Reserve(Email, EventCode, seatCount: 2);

        Assert.False(result);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WhenSavingFails_DoesNotSendConfirmation()
    {
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, availableSeats: 3),
            new RepositorySpy(saveResult: false),
            sender);

        var result = service.Reserve(Email, EventCode, seatCount: 2);

        Assert.False(result);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithEnoughSeats_SavesCorrectReservationOnce_Moq()
    {
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySave(It.IsAny<Reservation>())).Returns(true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, availableSeats: 3), repository.Object, sender);

        var result = service.Reserve(Email, EventCode, seatCount: 2);

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
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySave(It.IsAny<Reservation>())).Returns(false);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, availableSeats: 3), repository.Object, sender);

        var result = service.Reserve(Email, EventCode, seatCount: 2);

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
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySave(It.IsAny<Reservation>())).Returns(true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, availableSeats: 1), repository.Object, sender);

        var result = service.Reserve(Email, EventCode, seatCount: 2);

        Assert.False(result);
        repository.Verify(repo => repo.TrySave(It.IsAny<Reservation>()), Times.Never());
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithWhitespaceEmail_ThrowsBeforeCallingDependencies()
    {
        var availability = new SeatAvailabilityStub(EventCode, availableSeats: 3);
        var repository = new Mock<IReservationRepository>();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);

        Action action = () => service.Reserve("   ", EventCode, seatCount: 2);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("customerEmail", exception.ParamName);
        Assert.Empty(availability.RequestedEventCodes);
        repository.Verify(repo => repo.TrySave(It.IsAny<Reservation>()), Times.Never());
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.SentReservations);
    }
}
