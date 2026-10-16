using PaddleDuel.Infrastructure.Store;

namespace PaddleDuel.Tests;

public sealed class PendingFulfillmentTests : IDisposable
{
    private readonly string directoryPath = Path.Combine(
        Path.GetTempPath(),
        "PaddleDuel.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task FailedFulfillmentRemainsInJournalForRetry()
    {
        var gateway = CreateGateway();
        var journal = new JsonPendingFulfillmentJournal(directoryPath);
        var coordinator = new PendingFulfillmentCoordinator(() => gateway, journal);
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
        var coordinator = new PendingFulfillmentCoordinator(() => gateway, journal);
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
        var first = new PendingFulfillmentCoordinator(() => gateway, journal);
        await gateway.RequestPurchaseAsync("test.play.1");
        gateway.FulfillmentStatus = StoreOperationStatus.NetworkError;
        await first.FulfillAsync("test.play.1", 1, "tracking-3");

        gateway.FulfillmentStatus = StoreOperationStatus.Succeeded;
        var recreated = new PendingFulfillmentCoordinator(
            () => gateway,
            new JsonPendingFulfillmentJournal(directoryPath));
        var completed = await recreated.RetryPendingAsync();

        Assert.Equal(1, completed);
        Assert.Empty(await journal.LoadAsync());
    }

    [Fact]
    public async Task RetryAfterStoreSuccessBeforeJournalRemovalDoesNotConsumeTwice()
    {
        var gateway = CreateGateway();
        await gateway.RequestPurchaseAsync("test.play.1");
        await gateway.RequestPurchaseAsync("test.play.1");
        var journal = new JsonPendingFulfillmentJournal(directoryPath);
        var fulfillment = new PendingFulfillment("test.play.1", 1, Guid.NewGuid().ToString());
        await journal.UpsertAsync(fulfillment);

        var first = await gateway.ReportConsumableFulfillmentAsync(
            fulfillment.StoreId, fulfillment.Quantity, fulfillment.TrackingId);
        Assert.Equal(StoreOperationStatus.Succeeded, first.Status);
        Assert.Equal(1u, first.BalanceRemaining);

        var recreated = new PendingFulfillmentCoordinator(
            () => gateway,
            new JsonPendingFulfillmentJournal(directoryPath));
        Assert.Equal(1, await recreated.RetryPendingAsync());
        Assert.Equal(0, await recreated.RetryPendingAsync());
        Assert.Empty(await new JsonPendingFulfillmentJournal(directoryPath).LoadAsync());
        Assert.Equal(fulfillment.TrackingId, Assert.Single(gateway.FulfillmentTrackingIds));
        var balance = await gateway.GetBalanceAsync(fulfillment.StoreId);
        Assert.Equal(1u, balance.Balance!.UnitsRemaining);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{\"StoreId\":\"test.play.1\",\"Quantity\":0,\"TrackingId\":\"a\"}]")]
    [InlineData("[{\"StoreId\":\"test.play.1\",\"Quantity\":1,\"TrackingId\":\"\"}]")]
    [InlineData("[{\"StoreId\":\"test.play.1\",\"Quantity\":1,\"TrackingId\":\"a\"},{\"StoreId\":\"test.play.1\",\"Quantity\":1,\"TrackingId\":\"a\"}]")]
    public async Task InvalidJournalFailsClosedWithoutErasingUnsettledTransactions(string content)
    {
        Directory.CreateDirectory(directoryPath);
        var filePath = Path.Combine(directoryPath, "pending-fulfillments.json");
        await File.WriteAllTextAsync(filePath, content);
        var gateway = CreateGateway();
        var journal = new JsonPendingFulfillmentJournal(directoryPath);
        var coordinator = new PendingFulfillmentCoordinator(() => gateway, journal);

        var error = await Record.ExceptionAsync(async () => await coordinator.RetryPendingAsync());
        Assert.True(error is System.Text.Json.JsonException or InvalidDataException, error?.ToString());
        Assert.Equal(content, await File.ReadAllTextAsync(filePath));
        Assert.Empty(gateway.FulfillmentTrackingIds);
    }

    [Fact]
    public async Task ConcurrentCompletionAndActivationRetryConsumeOnlyOnce()
    {
        var gateway = CreateGateway();
        await gateway.RequestPurchaseAsync("test.play.1");
        await gateway.RequestPurchaseAsync("test.play.1");
        var journal = new JsonPendingFulfillmentJournal(directoryPath);
        var coordinator = new PendingFulfillmentCoordinator(() => gateway, journal);

        await Task.WhenAll(
            coordinator.FulfillAsync("test.play.1", 1, "same-match").AsTask(),
            coordinator.RetryPendingAsync().AsTask(),
            coordinator.FulfillAsync("test.play.1", 1, "same-match").AsTask());

        Assert.False(await coordinator.HasPendingAsync());
        Assert.Single(gateway.FulfillmentTrackingIds);
        Assert.Equal(1u, (await gateway.GetBalanceAsync("test.play.1")).Balance!.UnitsRemaining);
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