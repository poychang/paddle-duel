using Arcade1972.Infrastructure.Store;

namespace Arcade1972.Tests;

public sealed class StorePurchaseCoordinatorTests
{
    private const string StoreId = "test.play.1";

    [Fact]
    public async Task PurchaseStatusMapsToUiState()
    {
        var gateway = CreateGateway();
        var coordinator = new StorePurchaseCoordinator(gateway);
        gateway.PurchaseStatus = StoreOperationStatus.NetworkError;

        var result = await coordinator.RequestPurchaseAsync(StoreId);

        Assert.Equal(StorePurchaseUiStatus.NetworkError, result.Status);
        Assert.False(coordinator.IsPurchaseInProgress);
    }

    [Theory]
    [InlineData(StoreOperationStatus.Cancelled, StorePurchaseUiStatus.Cancelled)]
    [InlineData(StoreOperationStatus.ServerError, StorePurchaseUiStatus.ServerError)]
    [InlineData(StoreOperationStatus.NotSignedIn, StorePurchaseUiStatus.NotSignedIn)]
    [InlineData(StoreOperationStatus.NotAvailable, StorePurchaseUiStatus.NotAvailable)]
    public async Task PurchaseErrorsRemainActionable(
        StoreOperationStatus gatewayStatus,
        StorePurchaseUiStatus expectedStatus)
    {
        var gateway = CreateGateway();
        var coordinator = new StorePurchaseCoordinator(gateway);
        gateway.PurchaseStatus = gatewayStatus;

        var result = await coordinator.RequestPurchaseAsync(StoreId);

        Assert.Equal(expectedStatus, result.Status);
    }

    [Fact]
    public async Task ConcurrentPurchaseRequestsAllowOnlyOneOperation()
    {
        var gateway = CreateGateway();
        gateway.PurchaseDelay = TimeSpan.FromMilliseconds(100);
        var coordinator = new StorePurchaseCoordinator(gateway);

        var first = coordinator.RequestPurchaseAsync(StoreId).AsTask();
        var second = await coordinator.RequestPurchaseAsync(StoreId);
        var firstResult = await first;

        Assert.Equal(StorePurchaseUiStatus.AlreadyInProgress, second.Status);
        Assert.Equal(StorePurchaseUiStatus.Purchased, firstResult.Status);
        Assert.Single(gateway.PurchasedStoreIds);
    }

    [Fact]
    public async Task CoordinatorAllowsRetryAfterAnOperationFinishes()
    {
        var gateway = CreateGateway();
        var coordinator = new StorePurchaseCoordinator(gateway);
        gateway.PurchaseStatus = StoreOperationStatus.NetworkError;

        var failed = await coordinator.RequestPurchaseAsync(StoreId);
        gateway.PurchaseStatus = StoreOperationStatus.Succeeded;
        var retried = await coordinator.RequestPurchaseAsync(StoreId);

        Assert.Equal(StorePurchaseUiStatus.NetworkError, failed.Status);
        Assert.Equal(StorePurchaseUiStatus.Purchased, retried.Status);
    }

    private static FakeStoreGateway CreateGateway()
    {
        return new FakeStoreGateway(
        [new StoreProduct(StoreId, "1 Play", "Test $1", 1)]);
    }
}