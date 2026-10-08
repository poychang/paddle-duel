namespace PaddleDuel.Infrastructure.Store;

public enum StoreOperationStatus
{
    Succeeded,
    Cancelled,
    NetworkError,
    ServerError,
    NotSignedIn,
    NotAvailable,
    InsufficientQuantity,
    AlreadyFulfilled,
}

public sealed record StoreProduct(
    string StoreId,
    string DisplayName,
    string FormattedPrice,
    uint UnitsPerPurchase);

public sealed record StoreBalance(
    string StoreId,
    uint UnitsRemaining);

public sealed record StorePurchaseResult(
    StoreOperationStatus Status,
    StoreProduct? Product = null);

public sealed record StoreFulfillmentResult(
    StoreOperationStatus Status,
    string TrackingId,
    uint BalanceRemaining);

public interface IStoreGateway
{
    ValueTask<(StoreOperationStatus Status, IReadOnlyList<StoreProduct> Products)> GetProductsAsync(
        IReadOnlyList<string> storeIds,
        CancellationToken cancellationToken = default);

    ValueTask<(StoreOperationStatus Status, StoreBalance? Balance)> GetBalanceAsync(
        string storeId,
        CancellationToken cancellationToken = default);

    ValueTask<StorePurchaseResult> RequestPurchaseAsync(
        string storeId,
        CancellationToken cancellationToken = default);

    ValueTask<StoreFulfillmentResult> ReportConsumableFulfillmentAsync(
        string storeId,
        uint quantity,
        string trackingId,
        CancellationToken cancellationToken = default);
}