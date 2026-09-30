namespace Arcade1972.Infrastructure.Store;

public sealed class FakeStoreGateway : IStoreGateway
{
    private readonly Dictionary<string, StoreProduct> products;
    private readonly Dictionary<string, uint> balances;
    private readonly Dictionary<string, StoreFulfillmentResult> fulfilledTrackingIds = [];

    public FakeStoreGateway(IEnumerable<StoreProduct> products)
    {
        this.products = products.ToDictionary(product => product.StoreId, StringComparer.Ordinal);
        balances = this.products.Keys.ToDictionary(storeId => storeId, _ => 0u, StringComparer.Ordinal);
    }

    public StoreOperationStatus ProductQueryStatus { get; set; } = StoreOperationStatus.Succeeded;

    public StoreOperationStatus BalanceQueryStatus { get; set; } = StoreOperationStatus.Succeeded;

    public StoreOperationStatus PurchaseStatus { get; set; } = StoreOperationStatus.Succeeded;

    public StoreOperationStatus FulfillmentStatus { get; set; } = StoreOperationStatus.Succeeded;

    public IReadOnlyList<string> PurchasedStoreIds { get; private set; } = [];

    public IReadOnlyList<string> FulfillmentTrackingIds => fulfilledTrackingIds.Keys.ToArray();

    public ValueTask<(StoreOperationStatus Status, IReadOnlyList<StoreProduct> Products)> GetProductsAsync(
        IReadOnlyList<string> storeIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ProductQueryStatus != StoreOperationStatus.Succeeded)
        {
            return ValueTask.FromResult((ProductQueryStatus, (IReadOnlyList<StoreProduct>)[]));
        }

        var result = storeIds
            .Where(products.ContainsKey)
            .Select(storeId => products[storeId])
            .ToArray();
        return ValueTask.FromResult((StoreOperationStatus.Succeeded, (IReadOnlyList<StoreProduct>)result));
    }

    public ValueTask<(StoreOperationStatus Status, StoreBalance? Balance)> GetBalanceAsync(
        string storeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!balances.TryGetValue(storeId, out var unitsRemaining))
        {
            return ValueTask.FromResult((StoreOperationStatus.NotAvailable, (StoreBalance?)null));
        }

        return ValueTask.FromResult(
            (BalanceQueryStatus, BalanceQueryStatus == StoreOperationStatus.Succeeded
                ? new StoreBalance(storeId, unitsRemaining)
                : null));
    }

    public ValueTask<StorePurchaseResult> RequestPurchaseAsync(
        string storeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!products.TryGetValue(storeId, out var product))
        {
            return ValueTask.FromResult(new StorePurchaseResult(StoreOperationStatus.NotAvailable));
        }

        if (PurchaseStatus != StoreOperationStatus.Succeeded)
        {
            return ValueTask.FromResult(new StorePurchaseResult(PurchaseStatus, product));
        }

        balances[storeId] += product.UnitsPerPurchase;
        PurchasedStoreIds = PurchasedStoreIds.Append(storeId).ToArray();
        return ValueTask.FromResult(new StorePurchaseResult(StoreOperationStatus.Succeeded, product));
    }

    public ValueTask<StoreFulfillmentResult> ReportConsumableFulfillmentAsync(
        string storeId,
        uint quantity,
        string trackingId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (fulfilledTrackingIds.TryGetValue(trackingId, out var previous))
        {
            return ValueTask.FromResult(previous with
            {
                Status = StoreOperationStatus.AlreadyFulfilled,
            });
        }

        if (!balances.TryGetValue(storeId, out var balance))
        {
            return ValueTask.FromResult(new StoreFulfillmentResult(
                StoreOperationStatus.NotAvailable,
                trackingId,
                0));
        }

        if (FulfillmentStatus != StoreOperationStatus.Succeeded)
        {
            return ValueTask.FromResult(new StoreFulfillmentResult(
                FulfillmentStatus,
                trackingId,
                balance));
        }

        if (quantity == 0 || quantity > balance)
        {
            return ValueTask.FromResult(new StoreFulfillmentResult(
                StoreOperationStatus.InsufficientQuantity,
                trackingId,
                balance));
        }

        var result = new StoreFulfillmentResult(
            StoreOperationStatus.Succeeded,
            trackingId,
            balance - quantity);
        balances[storeId] = result.BalanceRemaining;
        fulfilledTrackingIds[trackingId] = result;
        return ValueTask.FromResult(result);
    }
}