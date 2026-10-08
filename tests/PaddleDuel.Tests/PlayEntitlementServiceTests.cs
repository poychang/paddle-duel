using PaddleDuel.Core;
using PaddleDuel.Infrastructure.Store;

namespace PaddleDuel.Tests;

public sealed class PlayEntitlementServiceTests
{
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

    private static PlayEntitlementService CreateService(FakeStoreGateway gateway)
    {
        return new PlayEntitlementService(
            new DailyFreePlayQuota(new FakeClock(InitialTime), new MemoryQuotaStore()),
            () => gateway,
            OnePlayStoreId,
            TenPlayStoreId);
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