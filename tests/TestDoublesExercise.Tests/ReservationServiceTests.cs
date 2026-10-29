using Moq;

namespace TestDoublesExercise.Tests;

public class ReservationServiceTests
{
    [Fact]
    public void Reserve_WithEnoughSeats_ReturnsTrue()
    {
        var stub = new SeatAvailabilityStub("EVENT1", 3);
        var service = new ReservationService(stub, new FakeRepository(true), new EmptySender());

        var result = service.Reserve("test@mail.ch", "EVENT1", 2);

        Assert.True(result);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_ReturnsFalse()
    {
        var stub = new SeatAvailabilityStub("EVENT1", 1);
        var service = new ReservationService(stub, new FakeRepository(true), new EmptySender());

        var result = service.Reserve("test@mail.ch", "EVENT1", 2);

        Assert.False(result);
    }

    [Fact]
    public void Reserve_WithExactlyEnoughSeats_Succeeds()
    {
        var stub = new SeatAvailabilityStub("EVENT1", 2);
        var service = new ReservationService(stub, new FakeRepository(true), new EmptySender());

        var result = service.Reserve("test@mail.ch", "EVENT1", 2);

        Assert.True(result);
    }

    [Fact]
    public void Reserve_WithZeroSeats_ThrowsBeforeCallingDependencies()
    {
        var stub = new SeatAvailabilityStub("EVENT1", 3);
        var repository = new FakeRepository(true);
        var spy = new SenderSpy();
        var service = new ReservationService(stub, repository, spy);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.Reserve("test@mail.ch", "EVENT1", 0));

        Assert.Equal(0, stub.Calls);
        Assert.Empty(repository.Saved);
        Assert.Empty(spy.Sent);
    }

    [Fact]
    public void Reserve_WithNegativeSeats_ThrowsBeforeCallingDependencies()
    {
        var stub = new SeatAvailabilityStub("EVENT1", 3);
        var repository = new FakeRepository(true);
        var spy = new SenderSpy();
        var service = new ReservationService(stub, repository, spy);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.Reserve("test@mail.ch", "EVENT1", -1));

        Assert.Equal(0, stub.Calls);
        Assert.Empty(repository.Saved);
        Assert.Empty(spy.Sent);
    }

    [Fact]
    public void Reserve_WithEmptyEventCode_ThrowsBeforeCallingDependencies()
    {
        var stub = new SeatAvailabilityStub("EVENT1", 3);
        var repository = new FakeRepository(true);
        var spy = new SenderSpy();
        var service = new ReservationService(stub, repository, spy);

        Assert.Throws<ArgumentException>(() => service.Reserve("test@mail.ch", "", 2));

        Assert.Equal(0, stub.Calls);
        Assert.Empty(repository.Saved);
        Assert.Empty(spy.Sent);
    }

    [Fact]
    public void Reserve_WhenSaved_SendsOneConfirmation()
    {
        var spy = new SenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub("EVENT1", 3), new FakeRepository(true), spy);

        var result = service.Reserve("test@mail.ch", "EVENT1", 2);

        Assert.True(result);
        Assert.Single(spy.Sent);
        Assert.Equal("test@mail.ch", spy.Sent[0].CustomerEmail);
        Assert.Equal("EVENT1", spy.Sent[0].EventCode);
        Assert.Equal(2, spy.Sent[0].SeatCount);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotSaveOrSend()
    {
        var repository = new FakeRepository(true);
        var spy = new SenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub("EVENT1", 1), repository, spy);

        var result = service.Reserve("test@mail.ch", "EVENT1", 2);

        Assert.False(result);
        Assert.Empty(repository.Saved);
        Assert.Empty(spy.Sent);
    }

    [Fact]
    public void Reserve_WhenSavingFails_DoesNotSendConfirmation()
    {
        var spy = new SenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub("EVENT1", 3), new FakeRepository(false), spy);

        var result = service.Reserve("test@mail.ch", "EVENT1", 2);

        Assert.False(result);
        Assert.Empty(spy.Sent);
    }

    [Fact]
    public void Reserve_WithEnoughSeats_SavesReservationWithMoq()
    {
        var repository = new Mock<IReservationRepository>();
        repository.Setup(r => r.TrySave(It.IsAny<Reservation>())).Returns(true);
        var spy = new SenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub("EVENT1", 3), repository.Object, spy);

        var result = service.Reserve("test@mail.ch", "EVENT1", 2);

        Assert.True(result);
        repository.Verify(r => r.TrySave(It.Is<Reservation>(x =>
            x.CustomerEmail == "test@mail.ch" && x.EventCode == "EVENT1" && x.SeatCount == 2)), Times.Once());
        Assert.Single(spy.Sent);
    }

    [Fact]
    public void Reserve_WhenSavingFailsWithMoq_ReturnsFalse()
    {
        var repository = new Mock<IReservationRepository>();
        repository.Setup(r => r.TrySave(It.IsAny<Reservation>())).Returns(false);
        var spy = new SenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub("EVENT1", 3), repository.Object, spy);

        var result = service.Reserve("test@mail.ch", "EVENT1", 2);

        Assert.False(result);
        repository.Verify(r => r.TrySave(It.Is<Reservation>(x =>
            x.CustomerEmail == "test@mail.ch" && x.EventCode == "EVENT1" && x.SeatCount == 2)), Times.Once());
        Assert.Empty(spy.Sent);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotCallRepository()
    {
        var repository = new Mock<IReservationRepository>();
        repository.Setup(r => r.TrySave(It.IsAny<Reservation>())).Returns(true);
        var spy = new SenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub("EVENT1", 1), repository.Object, spy);

        var result = service.Reserve("test@mail.ch", "EVENT1", 2);

        Assert.False(result);
        repository.Verify(r => r.TrySave(It.IsAny<Reservation>()), Times.Never());
        Assert.Empty(spy.Sent);
    }

    [Fact]
    public void Reserve_WithWhitespaceEmail_ThrowsBeforeCallingDependencies()
    {
        var stub = new SeatAvailabilityStub("EVENT1", 3);
        var repository = new Mock<IReservationRepository>();
        var spy = new SenderSpy();
        var service = new ReservationService(stub, repository.Object, spy);

        Assert.Throws<ArgumentException>(() => service.Reserve("   ", "EVENT1", 2));

        Assert.Equal(0, stub.Calls);
        repository.Verify(r => r.TrySave(It.IsAny<Reservation>()), Times.Never());
        Assert.Empty(spy.Sent);
    }

    private class SeatAvailabilityStub : ISeatAvailability
    {
        private readonly string _eventCode;
        private readonly int _seats;

        public SeatAvailabilityStub(string eventCode, int seats)
        {
            _eventCode = eventCode;
            _seats = seats;
        }

        public int Calls { get; private set; }

        public int GetAvailableSeats(string eventCode)
        {
            Calls++;
            if (eventCode == _eventCode)
            {
                return _seats;
            }
            return 0;
        }
    }

    private class FakeRepository : IReservationRepository
    {
        private readonly bool _result;

        public FakeRepository(bool result)
        {
            _result = result;
        }

        public List<Reservation> Saved { get; } = new List<Reservation>();

        public bool TrySave(Reservation reservation)
        {
            Saved.Add(reservation);
            return _result;
        }
    }

    private class EmptySender : IConfirmationSender
    {
        public void Send(Reservation reservation)
        {
        }
    }

    private class SenderSpy : IConfirmationSender
    {
        public List<Reservation> Sent { get; } = new List<Reservation>();

        public void Send(Reservation reservation)
        {
            Sent.Add(reservation);
        }
    }
}
