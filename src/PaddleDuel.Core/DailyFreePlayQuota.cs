namespace PaddleDuel.Core;

public interface IUtcClock
{
    DateTimeOffset UtcNow { get; }
}

public interface IFreePlayQuotaStore
{
    ValueTask<FreePlayQuotaState?> LoadAsync(CancellationToken cancellationToken = default);

    ValueTask SaveAsync(
        FreePlayQuotaState state,
        CancellationToken cancellationToken = default);
}

public sealed record CompletedFreePlay(Guid MatchId, DateOnly QuotaDate);

public sealed record FreePlayQuotaState(
    DateOnly LastObservedUtcDate,
    IReadOnlyList<CompletedFreePlay> CompletedMatches);

public sealed record FreePlayMatchSession(Guid MatchId, DateOnly QuotaDate);

public sealed record FreePlayAvailability(DateOnly QuotaDate, int RemainingPlays);

public sealed record FreePlayStartResult(
    bool IsAllowed,
    FreePlayMatchSession? Session,
    FreePlayAvailability Availability);

public enum FreePlayCompletionStatus
{
    Consumed,
    AlreadyConsumed,
    QuotaExhausted,
}

public sealed class DailyFreePlayQuota(
    IUtcClock clock,
    IFreePlayQuotaStore store,
    int dailyLimit = 3)
{
    public const int CompletedMatchRetentionDays = 400;

    private readonly SemaphoreSlim gate = new(1, 1);

    public int DailyLimit { get; } = dailyLimit > 0
        ? dailyLimit
        : throw new ArgumentOutOfRangeException(nameof(dailyLimit));

    public async ValueTask<FreePlayAvailability> GetAvailabilityAsync(
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadObservedStateAsync(cancellationToken);
            return GetAvailability(state, state.LastObservedUtcDate);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<FreePlayStartResult> TryStartMatchAsync(
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadObservedStateAsync(cancellationToken);
            var availability = GetAvailability(state, state.LastObservedUtcDate);
            if (availability.RemainingPlays == 0)
            {
                return new FreePlayStartResult(false, null, availability);
            }

            var session = new FreePlayMatchSession(Guid.NewGuid(), availability.QuotaDate);
            return new FreePlayStartResult(true, session, availability);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<FreePlayCompletionStatus> CompleteMatchAsync(
        FreePlayMatchSession session,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadObservedStateAsync(cancellationToken);
            if (state.CompletedMatches.Any(match => match.MatchId == session.MatchId))
            {
                return FreePlayCompletionStatus.AlreadyConsumed;
            }

            if (CountCompletedMatches(state, session.QuotaDate) >= DailyLimit)
            {
                return FreePlayCompletionStatus.QuotaExhausted;
            }

            var completedMatches = state.CompletedMatches
                .Append(new CompletedFreePlay(session.MatchId, session.QuotaDate))
                .ToArray();
            await store.SaveAsync(state with { CompletedMatches = completedMatches }, cancellationToken);
            return FreePlayCompletionStatus.Consumed;
        }
        finally
        {
            gate.Release();
        }
    }

    private async ValueTask<FreePlayQuotaState> LoadObservedStateAsync(
        CancellationToken cancellationToken)
    {
        var currentUtcDate = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var storedState = await store.LoadAsync(cancellationToken);
        if (storedState is null)
        {
            var newState = new FreePlayQuotaState(currentUtcDate, []);
            await store.SaveAsync(newState, cancellationToken);
            return newState;
        }

        var observedState = currentUtcDate > storedState.LastObservedUtcDate
            ? storedState with { LastObservedUtcDate = currentUtcDate }
            : storedState;
        var compactedState = CompactCompletedMatches(observedState);

        if (ReferenceEquals(compactedState, storedState))
        {
            return storedState;
        }

        await store.SaveAsync(compactedState, cancellationToken);
        return compactedState;
    }

    private static FreePlayQuotaState CompactCompletedMatches(FreePlayQuotaState state)
    {
        var cutoffDate = state.LastObservedUtcDate.AddDays(-CompletedMatchRetentionDays);
        var retainedMatches = state.CompletedMatches
            .Where(match => match.QuotaDate >= cutoffDate)
            .ToArray();

        return retainedMatches.Length == state.CompletedMatches.Count
            ? state
            : state with { CompletedMatches = retainedMatches };
    }

    private FreePlayAvailability GetAvailability(FreePlayQuotaState state, DateOnly quotaDate)
    {
        var remainingPlays = Math.Max(0, DailyLimit - CountCompletedMatches(state, quotaDate));
        return new FreePlayAvailability(quotaDate, remainingPlays);
    }

    private static int CountCompletedMatches(FreePlayQuotaState state, DateOnly quotaDate)
    {
        return state.CompletedMatches.Count(match => match.QuotaDate == quotaDate);
    }
}