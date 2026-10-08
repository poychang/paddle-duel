namespace PaddleDuel.Infrastructure.Store;

public enum StorePurchaseUiStatus
{
    Purchased,
    Cancelled,
    NetworkError,
    ServerError,
    NotSignedIn,
    NotAvailable,
    AlreadyInProgress,
}

public sealed record StorePurchaseUiResult(
    StorePurchaseUiStatus Status,
    StoreProduct? Product = null);

public sealed class StorePurchaseCoordinator(IStoreGateway gateway)
{
    private int purchaseInProgress;

    public bool IsPurchaseInProgress => Volatile.Read(ref purchaseInProgress) == 1;

    public async ValueTask<StorePurchaseUiResult> RequestPurchaseAsync(
        string storeId,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref purchaseInProgress, 1, 0) != 0)
        {
            return new StorePurchaseUiResult(StorePurchaseUiStatus.AlreadyInProgress);
        }

        try
        {
            var result = await gateway.RequestPurchaseAsync(storeId, cancellationToken);
            return result.Status switch
            {
                StoreOperationStatus.Succeeded => new StorePurchaseUiResult(
                    StorePurchaseUiStatus.Purchased,
                    result.Product),
                StoreOperationStatus.Cancelled => new StorePurchaseUiResult(
                    StorePurchaseUiStatus.Cancelled,
                    result.Product),
                StoreOperationStatus.NetworkError => new StorePurchaseUiResult(
                    StorePurchaseUiStatus.NetworkError,
                    result.Product),
                StoreOperationStatus.ServerError => new StorePurchaseUiResult(
                    StorePurchaseUiStatus.ServerError,
                    result.Product),
                StoreOperationStatus.NotSignedIn => new StorePurchaseUiResult(
                    StorePurchaseUiStatus.NotSignedIn,
                    result.Product),
                _ => new StorePurchaseUiResult(
                    StorePurchaseUiStatus.NotAvailable,
                    result.Product),
            };
        }
        finally
        {
            Volatile.Write(ref purchaseInProgress, 0);
        }
    }
}