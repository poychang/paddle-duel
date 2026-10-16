using PaddleDuel.Core;
using PaddleDuel.Infrastructure.Store;

namespace PaddleDuel.Tests;

public sealed class PlayEntitlementServiceTests : IDisposable
{
    private readonly string directoryPath = Path.Combine(
        Path.GetTempPath(), "PaddleDuel.Tests", Guid.NewGuid().ToString("N"));
    private const string OnePlayStoreId = "test.play.1";
    private const string TenPlayStoreId = "test.play.10";
    private static readonly DateTimeOffset InitialTime =
        new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FreePlayIsSelectedBeforeStoreBalance()
    {
        var gateway = CreateGateway();
        await gateway.RequestPurchaseAsync(TenPlayStoreId);
        var service = CreateService(gateway);

        var start = await service.TryStartMatchAsync();

        Assert.Equal(EntitlementStartStatus.Allowed, start.Status);
        Assert.Equal(PlayEntitlementSource.Free, start.Session!.Source);
        Assert.Null(start.Session.PaidStoreId);
    }

    [Fact]
    public async Task PaidFallbackPrefersOnePlayPoolThenTenPlayPool()
    {
        var gateway = CreateGateway();
        var service = CreateService(gateway);
        await CompleteFreePlaysAsync(service);
        await gateway.RequestPurchaseAsync(TenPlayStoreId);
        await gateway.RequestPurchaseAsync(OnePlayStoreId);

        var onePlayStart = await service.TryStartMatchAsync();
        Assert.Equal(
            EntitlementCompletionStatus.Consumed,
            await service.CompleteMatchAsync(onePlayStart.Session!));
        var tenPlayStart = await service.TryStartMatchAsync();

        Assert.Equal(PlayEntitlementSource.Paid, onePlayStart.Session!.Source);
        Assert.Equal(OnePlayStoreId, onePlayStart.Session.PaidStoreId);
        Assert.Equal(PlayEntitlementSource.Paid, tenPlayStart.Session!.Source);
        Assert.Equal(TenPlayStoreId, tenPlayStart.Session.PaidStoreId);
    }

    [Fact]
    public async Task NoBalancesReturnsNoEntitlement()
    {
        var service = CreateService(CreateGateway());
        await CompleteFreePlaysAsync(service);

        var start = await service.TryStartMatchAsync();

        Assert.Equal(EntitlementStartStatus.NoEntitlement, start.Status);
        Assert.Null(start.Session);
    }

    [Fact]
    public async Task StoreBalanceErrorStopsPaidStartWithoutCreatingASession()
    {
        var gateway = CreateGateway();
        var service = CreateService(gateway);
        await CompleteFreePlaysAsync(service);
        gateway.BalanceQueryStatus = StoreOperationStatus.NetworkError;

        var start = await service.TryStartMatchAsync();

        Assert.Equal(EntitlementStartStatus.StoreUnavailable, start.Status);
        Assert.Null(start.Session);
    }

    [Fact]
    public async Task PaidCompletionReportsOneUnitWithDeterministicTrackingId()
    {
        var gateway = CreateGateway();
        var service = CreateService(gateway);
        await CompleteFreePlaysAsync(service);
        await gateway.RequestPurchaseAsync(OnePlayStoreId);
        var start = await service.TryStartMatchAsync();
        Assert.NotNull(start.Session);

        var completion = await service.CompleteMatchAsync(start.Session);
        var retry = await service.CompleteMatchAsync(start.Session);
        var balance = await gateway.GetBalanceAsync(OnePlayStoreId);

        Assert.Equal(EntitlementCompletionStatus.Consumed, completion);
        Assert.Equal(EntitlementCompletionStatus.AlreadyConsumed, retry);
        Assert.Equal(0u, balance.Balance!.UnitsRemaining);
        Assert.StartsWith("play:", start.Session.TrackingId, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StoreFailureDuringPaidCompletionBecomesPending()
    {
        var gateway = CreateGateway();
        var service = CreateService(gateway);
        await CompleteFreePlaysAsync(service);
        await gateway.RequestPurchaseAsync(OnePlayStoreId);
        var start = await service.TryStartMatchAsync();
        gateway.FulfillmentStatus = StoreOperationStatus.NetworkError;

        var completion = await service.CompleteMatchAsync(start.Session!);

        Assert.Equal(EntitlementCompletionStatus.Pending, completion);
        var entry = Assert.Single(await new JsonPendingFulfillmentJournal(directoryPath).LoadAsync());
        Assert.Equal(start.Session!.TrackingId, entry.TrackingId);
        Assert.Equal(OnePlayStoreId, entry.StoreId);
        Assert.Equal(1u, entry.Quantity);
    }

    [Fact]
    public async Task PendingPaidCompletionCanBeRetriedWithTheSameSession()
    {
        var gateway = CreateGateway();
        var service = CreateService(gateway);
        await CompleteFreePlaysAsync(service);
        await gateway.RequestPurchaseAsync(OnePlayStoreId);
        var start = await service.TryStartMatchAsync();
        gateway.FulfillmentStatus = StoreOperationStatus.NetworkError;

        var pending = await service.CompleteMatchAsync(start.Session!);
        gateway.FulfillmentStatus = StoreOperationStatus.Succeeded;
        var retried = await service.CompleteMatchAsync(start.Session!);

        Assert.Equal(EntitlementCompletionStatus.Pending, pending);
        Assert.Equal(EntitlementCompletionStatus.Consumed, retried);
    }

    [Fact]
    public async Task RecreatedServiceCanCompleteAnExistingPaidSession()
    {
        var gateway = CreateGateway();
        var firstService = CreateService(gateway);
        await CompleteFreePlaysAsync(firstService);
        await gateway.RequestPurchaseAsync(OnePlayStoreId);
        var start = await firstService.TryStartMatchAsync();
        var recreatedService = CreateService(gateway);

        var completion = await recreatedService.CompleteMatchAsync(start.Session!);

        Assert.Equal(EntitlementCompletionStatus.Consumed, completion);
    }

    [Fact]
    public async Task RestartRetriesJournalBeforeOfferingAnotherMatchWithoutOriginalSession()
    {
        var gateway = CreateGateway();
        var quota = new DailyFreePlayQuota(new FakeClock(InitialTime), new MemoryQuotaStore());
        var service = CreateService(gateway, quota);
        await CompleteFreePlaysAsync(service);
        await gateway.RequestPurchaseAsync(OnePlayStoreId);
        var paid = await service.TryStartMatchAsync();
        gateway.FulfillmentStatus = StoreOperationStatus.NetworkError;
        await service.CompleteMatchAsync(paid.Session!);

        var restarted = CreateService(gateway, quota);
        var blocked = await restarted.TryStartMatchAsync();
        Assert.Equal(EntitlementStartStatus.FulfillmentPending, blocked.Status);
        Assert.Null(blocked.Session);

        gateway.FulfillmentStatus = StoreOperationStatus.Succeeded;
        var recovered = await restarted.TryStartMatchAsync();
        Assert.Equal(EntitlementStartStatus.NoEntitlement, recovered.Status);
        Assert.Empty(await new JsonPendingFulfillmentJournal(directoryPath).LoadAsync());
        Assert.Equal(paid.Session!.TrackingId, Assert.Single(gateway.FulfillmentTrackingIds));
        Assert.Equal(0u, (await gateway.GetBalanceAsync(OnePlayStoreId)).Balance!.UnitsRemaining);
    }

    [Fact]
    public async Task PendingFulfillmentAlsoBlocksFreshDailyAllowance()
    {
        var gateway = CreateGateway();
        var journal = new JsonPendingFulfillmentJournal(directoryPath);
        await journal.UpsertAsync(new PendingFulfillment(OnePlayStoreId, 1, "previous-day"));
        gateway.FulfillmentStatus = StoreOperationStatus.NetworkError;

        var result = await CreateService(gateway).TryStartMatchAsync();

        Assert.Equal(EntitlementStartStatus.FulfillmentPending, result.Status);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task FreeStartDoesNotConstructStoreGatewayWhenJournalIsEmpty()
    {
        IStoreGateway UnexpectedGateway() => throw new InvalidOperationException("Unexpected Store access.");
        var quota = new DailyFreePlayQuota(new FakeClock(InitialTime), new MemoryQuotaStore());
        var service = new PlayEntitlementService(
            quota, UnexpectedGateway, OnePlayStoreId, TenPlayStoreId,
            new PendingFulfillmentCoordinator(UnexpectedGateway, new JsonPendingFulfillmentJournal(directoryPath)));

        Assert.Equal(EntitlementStartStatus.Allowed, (await service.TryStartMatchAsync()).Status);
    }

    private PlayEntitlementService CreateService(FakeStoreGateway gateway, DailyFreePlayQuota? quota = null)
    {
        return new PlayEntitlementService(
            quota ?? new DailyFreePlayQuota(new FakeClock(InitialTime), new MemoryQuotaStore()),
            () => gateway,
            OnePlayStoreId,
            TenPlayStoreId,
            new PendingFulfillmentCoordinator(() => gateway, new JsonPendingFulfillmentJournal(directoryPath)));
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
        [
            new StoreProduct(OnePlayStoreId, "1 Play", "Test $1", 1),
            new StoreProduct(TenPlayStoreId, "10 Plays", "Test $10", 10),
        ]);
    }

    private static async Task CompleteFreePlaysAsync(PlayEntitlementService service)
    {
        for (var index = 0; index < 3; index++)
        {
            var start = await service.TryStartMatchAsync();
            await service.CompleteMatchAsync(start.Session!);
        }
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : IUtcClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class MemoryQuotaStore : IFreePlayQuotaStore
    {
        public FreePlayQuotaState? State { get; private set; }

        public ValueTask<FreePlayQuotaState?> LoadAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(State);

        public ValueTask SaveAsync(FreePlayQuotaState state, CancellationToken cancellationToken = default)
        {
            State = state;
            return ValueTask.CompletedTask;
        }
    }
}