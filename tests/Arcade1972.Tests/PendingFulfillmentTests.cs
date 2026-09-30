using Arcade1972.Infrastructure.Store;

namespace Arcade1972.Tests;

public sealed class PendingFulfillmentTests : IDisposable
{
    private readonly string directoryPath = Path.Combine(
        Path.GetTempPath(),
        "Arcade1972.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task FailedFulfillmentRemainsInJournalForRetry()
    {
        var gateway = CreateGateway();
        var journal = new JsonPendingFulfillmentJournal(directoryPath);
        var coordinator = new PendingFulfillmentCoordinator(gateway, journal);
        await gateway.RequestPurchaseAsync("test.play.1");
        gateway.FulfillmentStatus = StoreOperationStatus.NetworkError;

        var result = await coordinator.FulfillAsync("test.play.1", 1, "tracking-1");
        var pending = await journal.LoadAsync();

        Assert.Equal(EntitlementCompletionStatus.Pending, result);
        Assert.Single(pending);
        Assert.Equal("tracking-1", pending[0].TrackingId);
    }

    [Fact]
    public async Task RetrySuccessRemovesPendingFulfillment()
    {
        var gateway = CreateGateway();
        var journal = new JsonPendingFulfillmentJournal(directoryPath);
        var coordinator = new PendingFulfillmentCoordinator(gateway, journal);
        await gateway.RequestPurchaseAsync("test.play.1");
        gateway.FulfillmentStatus = StoreOperationStatus.NetworkError;
        await coordinator.FulfillAsync("test.play.1", 1, "tracking-2");

        gateway.FulfillmentStatus = StoreOperationStatus.Succeeded;
        var completed = await coordinator.RetryPendingAsync();

        Assert.Equal(1, completed);
        Assert.Empty(await journal.LoadAsync());
    }

    [Fact]
    public async Task JournalSurvivesCoordinatorRecreation()
    {
        var gateway = CreateGateway();
        var journal = new JsonPendingFulfillmentJournal(directoryPath);
        var first = new PendingFulfillmentCoordinator(gateway, journal);
        await gateway.RequestPurchaseAsync("test.play.1");
        gateway.FulfillmentStatus = StoreOperationStatus.NetworkError;
        await first.FulfillAsync("test.play.1", 1, "tracking-3");

        gateway.FulfillmentStatus = StoreOperationStatus.Succeeded;
        var recreated = new PendingFulfillmentCoordinator(
            gateway,
            new JsonPendingFulfillmentJournal(directoryPath));
        var completed = await recreated.RetryPendingAsync();

        Assert.Equal(1, completed);
        Assert.Empty(await journal.LoadAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private static FakeStoreGateway CreateGateway()
    {
        return new FakeStoreGateway(
        [new StoreProduct("test.play.1", "1 Play", "Test $1", 1)]);
    }
}