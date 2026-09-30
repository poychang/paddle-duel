namespace Arcade1972.Infrastructure.Store;

public sealed class PendingFulfillmentCoordinator(
    IStoreGateway gateway,
    IPendingFulfillmentJournal journal)
{
    public async ValueTask<EntitlementCompletionStatus> FulfillAsync(
        string storeId,
        uint quantity,
        string trackingId,
        CancellationToken cancellationToken = default)
    {
        await journal.UpsertAsync(
            new PendingFulfillment(storeId, quantity, trackingId),
            cancellationToken);
        return await TryFulfillAsync(
            new PendingFulfillment(storeId, quantity, trackingId),
            cancellationToken);
    }

    public async ValueTask<int> RetryPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var pending = await journal.LoadAsync(cancellationToken);
        var completed = 0;
        foreach (var fulfillment in pending)
        {
            var result = await TryFulfillAsync(fulfillment, cancellationToken);
            if (result is EntitlementCompletionStatus.Consumed
                or EntitlementCompletionStatus.AlreadyConsumed)
            {
                completed++;
            }
        }

        return completed;
    }

    private async ValueTask<EntitlementCompletionStatus> TryFulfillAsync(
        PendingFulfillment fulfillment,
        CancellationToken cancellationToken)
    {
        var result = await gateway.ReportConsumableFulfillmentAsync(
            fulfillment.StoreId,
            fulfillment.Quantity,
            fulfillment.TrackingId,
            cancellationToken);
        switch (result.Status)
        {
            case StoreOperationStatus.Succeeded:
                await journal.RemoveAsync(fulfillment.TrackingId, cancellationToken);
                return EntitlementCompletionStatus.Consumed;
            case StoreOperationStatus.AlreadyFulfilled:
                await journal.RemoveAsync(fulfillment.TrackingId, cancellationToken);
                return EntitlementCompletionStatus.AlreadyConsumed;
            case StoreOperationStatus.NetworkError:
            case StoreOperationStatus.ServerError:
                return EntitlementCompletionStatus.Pending;
            default:
                return EntitlementCompletionStatus.Failed;
        }
    }
}