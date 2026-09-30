using Arcade1972.Infrastructure.Store;

namespace Arcade1972.Tests;

public sealed class FakeStoreGatewayTests
{
    private const string OnePlayStoreId = "test.play.1";
    private const string TenPlayStoreId = "test.play.10";

    private static FakeStoreGateway CreateGateway()
    {
        return new FakeStoreGateway(
        [
            new StoreProduct(OnePlayStoreId, "1 Play", "Test $1", 1),
            new StoreProduct(TenPlayStoreId, "10 Plays", "Test $10", 10),
        ]);
    }

    [Fact]
    public async Task GetProducts_ReturnsStoreMetadataWithoutHardcodedUiLogic()
    {
        var gateway = CreateGateway();

        var result = await gateway.GetProductsAsync([OnePlayStoreId, TenPlayStoreId]);

        Assert.Equal(StoreOperationStatus.Succeeded, result.Status);
        Assert.Collection(
            result.Products,
            one => Assert.Equal((OnePlayStoreId, "1 Play", "Test $1", (uint)1),
                (one.StoreId, one.DisplayName, one.FormattedPrice, one.UnitsPerPurchase)),
            ten => Assert.Equal((TenPlayStoreId, "10 Plays", "Test $10", (uint)10),
                (ten.StoreId, ten.DisplayName, ten.FormattedPrice, ten.UnitsPerPurchase)));
    }

    [Fact]
    public async Task ProductQueryError_IsReturnedToTheCaller()
    {
        var gateway = CreateGateway();
        gateway.ProductQueryStatus = StoreOperationStatus.NetworkError;

        var result = await gateway.GetProductsAsync([OnePlayStoreId]);

        Assert.Equal(StoreOperationStatus.NetworkError, result.Status);
        Assert.Empty(result.Products);
    }

    [Fact]
    public async Task SuccessfulPurchase_AddsStoreDefinedUnitsToBalance()
    {
        var gateway = CreateGateway();

        var purchase = await gateway.RequestPurchaseAsync(TenPlayStoreId);
        var balance = await gateway.GetBalanceAsync(TenPlayStoreId);

        Assert.Equal(StoreOperationStatus.Succeeded, purchase.Status);
        Assert.Equal(10u, balance.Balance!.UnitsRemaining);
    }

    [Fact]
    public async Task PurchaseError_DoesNotChangeBalance()
    {
        var gateway = CreateGateway();
        gateway.PurchaseStatus = StoreOperationStatus.NetworkError;

        var purchase = await gateway.RequestPurchaseAsync(OnePlayStoreId);
        var balance = await gateway.GetBalanceAsync(OnePlayStoreId);

        Assert.Equal(StoreOperationStatus.NetworkError, purchase.Status);
        Assert.Equal(0u, balance.Balance!.UnitsRemaining);
    }

    [Fact]
    public async Task CancelledPurchase_DoesNotChangeBalance()
    {
        var gateway = CreateGateway();
        gateway.PurchaseStatus = StoreOperationStatus.Cancelled;

        var purchase = await gateway.RequestPurchaseAsync(OnePlayStoreId);
        var balance = await gateway.GetBalanceAsync(OnePlayStoreId);

        Assert.Equal(StoreOperationStatus.Cancelled, purchase.Status);
        Assert.Equal(0u, balance.Balance!.UnitsRemaining);
    }

    [Fact]
    public async Task FulfillmentIsIdempotentForTheSameTrackingId()
    {
        var gateway = CreateGateway();
        await gateway.RequestPurchaseAsync(TenPlayStoreId);

        var first = await gateway.ReportConsumableFulfillmentAsync(TenPlayStoreId, 1, "tracking-1");
        var retry = await gateway.ReportConsumableFulfillmentAsync(TenPlayStoreId, 1, "tracking-1");
        var balance = await gateway.GetBalanceAsync(TenPlayStoreId);

        Assert.Equal(StoreOperationStatus.Succeeded, first.Status);
        Assert.Equal(StoreOperationStatus.AlreadyFulfilled, retry.Status);
        Assert.Equal(9u, balance.Balance!.UnitsRemaining);
    }

    [Fact]
    public async Task FulfillmentRejectsMoreUnitsThanTheStoreBalance()
    {
        var gateway = CreateGateway();
        await gateway.RequestPurchaseAsync(OnePlayStoreId);

        var result = await gateway.ReportConsumableFulfillmentAsync(OnePlayStoreId, 2, "tracking-2");

        Assert.Equal(StoreOperationStatus.InsufficientQuantity, result.Status);
        Assert.Equal(1u, result.BalanceRemaining);
    }
}