using Moq;

namespace TestDoublesExercise.Tests;

public interface IStockReader
{
    int GetStock(string productCode);
}

public interface IOrderNotifier
{
    void Notify(string productCode);
}

public sealed class StockCheckService(IStockReader stockReader, IOrderNotifier notifier)
{
    public bool TryOrder(string productCode)
    {
        if (stockReader.GetStock(productCode) <= 0)
        {
            return false;
        }

        notifier.Notify(productCode);
        return true;
    }
}

public sealed class TestDoubleExamples
{
    [Fact]
    public void HandwrittenStub_ControlsTheStockResponse()
    {
        var stock = new FixedStockStub(0);
        var notifier = new RecordingOrderSpy();
        var service = new StockCheckService(stock, notifier);

        var result = service.TryOrder("BOOK-1");

        Assert.False(result);
        Assert.Empty(notifier.NotifiedProductCodes);
    }

    [Fact]
    public void HandwrittenSpy_RecordsTheNotification()
    {
        var notifier = new RecordingOrderSpy();
        var service = new StockCheckService(new FixedStockStub(2), notifier);

        var result = service.TryOrder("BOOK-1");

        Assert.True(result);
        Assert.Equal("BOOK-1", Assert.Single(notifier.NotifiedProductCodes));
    }

    [Fact]
    public void Moq_ConfiguresAndVerifiesTheStockReader()
    {
        var stock = new Mock<IStockReader>();
        stock.Setup(reader => reader.GetStock("BOOK-1")).Returns(2);
        var notifier = new RecordingOrderSpy();
        var service = new StockCheckService(stock.Object, notifier);

        var result = service.TryOrder("BOOK-1");

        Assert.True(result);
        stock.Verify(reader => reader.GetStock("BOOK-1"), Times.Once());
        Assert.Equal("BOOK-1", Assert.Single(notifier.NotifiedProductCodes));
    }

    private sealed class FixedStockStub(int availableItems) : IStockReader
    {
        public int GetStock(string productCode) => availableItems;
    }

    private sealed class RecordingOrderSpy : IOrderNotifier
    {
        private readonly List<string> _notifiedProductCodes = [];

        public IReadOnlyList<string> NotifiedProductCodes => _notifiedProductCodes;

        public void Notify(string productCode) => _notifiedProductCodes.Add(productCode);
    }
}
