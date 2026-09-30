using Arcade1972.Core;

namespace Arcade1972.Infrastructure.Store;

public enum PlayEntitlementSource
{
    Free,
    Paid,
}

public enum EntitlementStartStatus
{
    Allowed,
    NoEntitlement,
    StoreUnavailable,
}

public enum EntitlementCompletionStatus
{
    Consumed,
    AlreadyConsumed,
    Pending,
    Failed,
}

public sealed record PlayEntitlementSession(
    Guid MatchId,
    PlayEntitlementSource Source,
    FreePlayMatchSession? FreePlaySession,
    string? PaidStoreId,
    string TrackingId);

public sealed record EntitlementStartResult(
    EntitlementStartStatus Status,
    PlayEntitlementSession? Session);

public interface IPlayEntitlementService
{
    ValueTask<EntitlementStartResult> TryStartMatchAsync(
        CancellationToken cancellationToken = default);

    ValueTask<EntitlementCompletionStatus> CompleteMatchAsync(
        PlayEntitlementSession session,
        CancellationToken cancellationToken = default);
}

public sealed class PlayEntitlementService(
    DailyFreePlayQuota freePlayQuota,
    Func<IStoreGateway> storeGatewayFactory,
    string onePlayStoreId,
    string tenPlayStoreId) : IPlayEntitlementService
{
    private IStoreGateway? storeGateway;

    public async ValueTask<EntitlementStartResult> TryStartMatchAsync(
        CancellationToken cancellationToken = default)
    {
        var freeStart = await freePlayQuota.TryStartMatchAsync(cancellationToken);
        if (freeStart.IsAllowed)
        {
            var freeSession = freeStart.Session!;
            return new EntitlementStartResult(
                EntitlementStartStatus.Allowed,
                new PlayEntitlementSession(
                    freeSession.MatchId,
                    PlayEntitlementSource.Free,
                    freeSession,
                    null,
                    CreateTrackingId(freeSession.MatchId)));
        }

        storeGateway ??= storeGatewayFactory();
        var onePlayBalance = await storeGateway.GetBalanceAsync(
            onePlayStoreId,
            cancellationToken);
        var tenPlayBalance = await storeGateway.GetBalanceAsync(
            tenPlayStoreId,
            cancellationToken);
        if (onePlayBalance.Status != StoreOperationStatus.Succeeded
            || tenPlayBalance.Status != StoreOperationStatus.Succeeded)
        {
            return new EntitlementStartResult(EntitlementStartStatus.StoreUnavailable, null);
        }

        var selectedStoreId = onePlayBalance.Balance!.UnitsRemaining > 0
            ? onePlayStoreId
            : tenPlayBalance.Balance!.UnitsRemaining > 0
                ? tenPlayStoreId
                : null;
        if (selectedStoreId is null)
        {
            return new EntitlementStartResult(EntitlementStartStatus.NoEntitlement, null);
        }

        var matchId = Guid.NewGuid();
        return new EntitlementStartResult(
            EntitlementStartStatus.Allowed,
            new PlayEntitlementSession(
                matchId,
                PlayEntitlementSource.Paid,
                null,
                selectedStoreId,
                CreateTrackingId(matchId)));
    }

    public async ValueTask<EntitlementCompletionStatus> CompleteMatchAsync(
        PlayEntitlementSession session,
        CancellationToken cancellationToken = default)
    {
        if (session.Source == PlayEntitlementSource.Free)
        {
            var status = await freePlayQuota.CompleteMatchAsync(
                session.FreePlaySession
                    ?? throw new InvalidOperationException("Free entitlement session is missing."),
                cancellationToken);
            return status switch
            {
                FreePlayCompletionStatus.Consumed => EntitlementCompletionStatus.Consumed,
                FreePlayCompletionStatus.AlreadyConsumed => EntitlementCompletionStatus.AlreadyConsumed,
                _ => EntitlementCompletionStatus.Failed,
            };
        }

        if (session.PaidStoreId is null)
        {
            return EntitlementCompletionStatus.Failed;
        }

        storeGateway ??= storeGatewayFactory();
        var result = await storeGateway.ReportConsumableFulfillmentAsync(
            session.PaidStoreId,
            1,
            session.TrackingId,
            cancellationToken);
        return result.Status switch
        {
            StoreOperationStatus.Succeeded => EntitlementCompletionStatus.Consumed,
            StoreOperationStatus.AlreadyFulfilled => EntitlementCompletionStatus.AlreadyConsumed,
            StoreOperationStatus.NetworkError or StoreOperationStatus.ServerError
                => EntitlementCompletionStatus.Pending,
            _ => EntitlementCompletionStatus.Failed,
        };
    }

    private static string CreateTrackingId(Guid matchId) => $"play:{matchId:N}";
}