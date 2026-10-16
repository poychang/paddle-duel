using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using PaddleDuel.Infrastructure.Store;
using Windows.Services.Store;
using GatewayStoreProduct = PaddleDuel.Infrastructure.Store.StoreProduct;
using GatewayStorePurchaseResult = PaddleDuel.Infrastructure.Store.StorePurchaseResult;

namespace PaddleDuel.App.Store;

public sealed class WindowsStoreGateway : IStoreGateway
{
    private readonly StoreContext storeContext;

    public WindowsStoreGateway(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            throw new ArgumentException("Store dialogs require the owning window.", nameof(windowHandle));
        }

        storeContext = StoreContext.GetDefault();
        WinRT.Interop.InitializeWithWindow.Initialize(storeContext, windowHandle);
    }

    public async ValueTask<(StoreOperationStatus Status, IReadOnlyList<GatewayStoreProduct> Products)> GetProductsAsync(
        IReadOnlyList<string> storeIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = await storeContext.GetAssociatedStoreProductsAsync(new[] { StorePlayCatalog.ProductKind });
            if (result.ExtendedError is not null)
            {
                Debug.WriteLine($"Store product query failed: {result.ExtendedError}");
                return (StoreOperationStatus.NetworkError, []);
            }

            var products = result.Products.Values
                .Where(product => storeIds.Contains(product.StoreId, StringComparer.Ordinal)
                    && product.ProductKind == StorePlayCatalog.ProductKind)
                .Select(product =>
                {
                    return new GatewayStoreProduct(
                        product.StoreId,
                        product.Title,
                        product.Price.FormattedPrice,
                        StorePlayCatalog.GetUnitsPerPurchase(product.StoreId));
                })
                .ToArray();
            return (StoreOperationStatus.Succeeded, products);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            Debug.WriteLine($"Store product query failed: {error}");
            return (StoreOperationStatus.NetworkError, []);
        }
    }

    public async ValueTask<(StoreOperationStatus Status, StoreBalance? Balance)> GetBalanceAsync(
        string storeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = StorePlayCatalog.GetUnitsPerPurchase(storeId);
        try
        {
            var result = await storeContext.GetConsumableBalanceRemainingAsync(storeId);
            if (result.ExtendedError is not null)
            {
                Debug.WriteLine($"Store balance query failed: {result.ExtendedError}");
            }
            var status = MapStatus(result.Status);
            return status == StoreOperationStatus.Succeeded
                ? (status, new StoreBalance(storeId, result.BalanceRemaining))
                : (status, null);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            Debug.WriteLine($"Store balance query failed: {error}");
            return (StoreOperationStatus.NetworkError, null);
        }
    }

    public async ValueTask<GatewayStorePurchaseResult> RequestPurchaseAsync(
        string storeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = StorePlayCatalog.GetUnitsPerPurchase(storeId);
        try
        {
            var result = await storeContext.RequestPurchaseAsync(storeId);
            if (result.ExtendedError is not null)
            {
                Debug.WriteLine($"Store purchase failed: {result.ExtendedError}");
            }
            return new GatewayStorePurchaseResult(MapStatus(result.Status));
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            Debug.WriteLine($"Store purchase failed: {error}");
            return new GatewayStorePurchaseResult(StoreOperationStatus.NetworkError);
        }
    }

    public async ValueTask<StoreFulfillmentResult> ReportConsumableFulfillmentAsync(
        string storeId,
        uint quantity,
        string trackingId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = StorePlayCatalog.GetUnitsPerPurchase(storeId);
        try
        {
            var result = await storeContext.ReportConsumableFulfillmentAsync(
                storeId,
                quantity,
                ToTrackingGuid(trackingId));
            if (result.ExtendedError is not null)
            {
                Debug.WriteLine($"Store fulfillment failed: {result.ExtendedError}");
            }
            return new StoreFulfillmentResult(
                MapStatus(result.Status),
                trackingId,
                result.BalanceRemaining);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            Debug.WriteLine($"Store fulfillment failed: {error}");
            return new StoreFulfillmentResult(
                StoreOperationStatus.NetworkError,
                trackingId,
                0);
        }
    }

    private static StoreOperationStatus MapStatus(StorePurchaseStatus status) => status switch
    {
        StorePurchaseStatus.Succeeded or StorePurchaseStatus.AlreadyPurchased => StoreOperationStatus.Succeeded,
        StorePurchaseStatus.NotPurchased => StoreOperationStatus.Cancelled,
        StorePurchaseStatus.NetworkError => StoreOperationStatus.NetworkError,
        StorePurchaseStatus.ServerError => StoreOperationStatus.ServerError,
        _ => StoreOperationStatus.NotAvailable,
    };

    private static StoreOperationStatus MapStatus(StoreConsumableStatus status) => status switch
    {
        StoreConsumableStatus.Succeeded => StoreOperationStatus.Succeeded,
        StoreConsumableStatus.InsufficentQuantity => StoreOperationStatus.InsufficientQuantity,
        StoreConsumableStatus.NetworkError => StoreOperationStatus.NetworkError,
        StoreConsumableStatus.ServerError => StoreOperationStatus.ServerError,
        _ => StoreOperationStatus.NotAvailable,
    };

    private static Guid ToTrackingGuid(string trackingId)
    {
        if (Guid.TryParse(trackingId, out var guid))
        {
            return guid;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(trackingId));
        return new Guid(hash.AsSpan(0, 16));
    }
}