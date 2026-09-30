using System.Security.Cryptography;
using System.Text;
using Arcade1972.Infrastructure.Store;
using Windows.Services.Store;
using GatewayStoreProduct = Arcade1972.Infrastructure.Store.StoreProduct;
using GatewayStorePurchaseResult = Arcade1972.Infrastructure.Store.StorePurchaseResult;

namespace Arcade1972.App.Store;

public sealed class WindowsStoreGateway : IStoreGateway
{
    private readonly StoreContext storeContext = StoreContext.GetDefault();

    public async ValueTask<(StoreOperationStatus Status, IReadOnlyList<GatewayStoreProduct> Products)> GetProductsAsync(
        IReadOnlyList<string> storeIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = await storeContext.GetAssociatedStoreProductsAsync(["Consumable"]);
            if (result.ExtendedError is not null)
            {
                return (StoreOperationStatus.NetworkError, []);
            }

            var products = storeIds
                .Where(result.Products.ContainsKey)
                .Select(storeId =>
                {
                    var product = result.Products[storeId];
                    return new GatewayStoreProduct(
                        storeId,
                        product.Title,
                        product.Price.FormattedPrice,
                        0);
                })
                .ToArray();
            return (StoreOperationStatus.Succeeded, products);
        }
        catch (Exception)
        {
            return (StoreOperationStatus.NetworkError, []);
        }
    }

    public async ValueTask<(StoreOperationStatus Status, StoreBalance? Balance)> GetBalanceAsync(
        string storeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = await storeContext.GetConsumableBalanceRemainingAsync(storeId);
            var status = MapStatus(result.Status.ToString());
            return status == StoreOperationStatus.Succeeded
                ? (status, new StoreBalance(storeId, result.BalanceRemaining))
                : (status, null);
        }
        catch (Exception)
        {
            return (StoreOperationStatus.NetworkError, null);
        }
    }

    public async ValueTask<GatewayStorePurchaseResult> RequestPurchaseAsync(
        string storeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = await storeContext.RequestPurchaseAsync(storeId);
            return new GatewayStorePurchaseResult(MapStatus(result.Status.ToString()));
        }
        catch (Exception)
        {
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
        try
        {
            var result = await storeContext.ReportConsumableFulfillmentAsync(
                storeId,
                quantity,
                ToTrackingGuid(trackingId));
            return new StoreFulfillmentResult(
                MapStatus(result.Status.ToString()),
                trackingId,
                result.BalanceRemaining);
        }
        catch (Exception)
        {
            return new StoreFulfillmentResult(
                StoreOperationStatus.NetworkError,
                trackingId,
                0);
        }
    }

    private static StoreOperationStatus MapStatus(string status) => status switch
    {
        "Succeeded" => StoreOperationStatus.Succeeded,
        "AlreadyPurchased" => StoreOperationStatus.Succeeded,
        "AlreadyFulfilled" => StoreOperationStatus.AlreadyFulfilled,
        "NotPurchased" => StoreOperationStatus.Cancelled,
        "NetworkError" => StoreOperationStatus.NetworkError,
        "ServerError" => StoreOperationStatus.ServerError,
        "InsufficientQuantity" => StoreOperationStatus.InsufficientQuantity,
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