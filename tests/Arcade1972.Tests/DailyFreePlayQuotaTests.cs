using Arcade1972.Core;

namespace Arcade1972.Tests;

public sealed class DailyFreePlayQuotaTests
{
    private static readonly DateTimeOffset InitialTime =
        new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveDailyLimit(int dailyLimit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DailyFreePlayQuota(
                new FakeClock(InitialTime),
                new MemoryQuotaStore(),
                dailyLimit));
    }

    [Fact]
    public async Task Availability_StartsWithThreePlaysAndResetsAtUtcMidnight()
    {
        var clock = new FakeClock(InitialTime);
        var quota = new DailyFreePlayQuota(clock, new MemoryQuotaStore());

        await CompleteMatchesAsync(quota, 3);
        Assert.Equal(0, (await quota.GetAvailabilityAsync()).RemainingPlays);

        clock.UtcNow = new DateTimeOffset(2026, 9, 25, 0, 0, 0, TimeSpan.Zero);
        var nextDay = await quota.GetAvailabilityAsync();

        Assert.Equal(new DateOnly(2026, 9, 25), nextDay.QuotaDate);
        Assert.Equal(3, nextDay.RemainingPlays);
    }

    [Fact]
    public async Task StartingOrAbandoningAMatchDoesNotConsumeAPlay()
    {
        var quota = new DailyFreePlayQuota(new FakeClock(InitialTime), new MemoryQuotaStore());

        var started = await quota.TryStartMatchAsync();
        var afterAbandon = await quota.GetAvailabilityAsync();

        Assert.True(started.IsAllowed);
        Assert.NotNull(started.Session);
        Assert.Equal(3, afterAbandon.RemainingPlays);
    }

    [Fact]
    public async Task CompletingAMatchConsumesOnePlayOnlyOnce()
    {
        var quota = new DailyFreePlayQuota(new FakeClock(InitialTime), new MemoryQuotaStore());
        var started = await quota.TryStartMatchAsync();

        var firstCompletion = await quota.CompleteMatchAsync(started.Session!);
        var duplicateCompletion = await quota.CompleteMatchAsync(started.Session!);

        Assert.Equal(FreePlayCompletionStatus.Consumed, firstCompletion);
        Assert.Equal(FreePlayCompletionStatus.AlreadyConsumed, duplicateCompletion);
        Assert.Equal(2, (await quota.GetAvailabilityAsync()).RemainingPlays);
    }

    [Fact]
    public async Task MatchSpanningMidnightConsumesItsStartDateQuota()
    {
        var clock = new FakeClock(
            new DateTimeOffset(2026, 9, 24, 23, 59, 0, TimeSpan.Zero));
        var store = new MemoryQuotaStore();
        var quota = new DailyFreePlayQuota(clock, store);
        var started = await quota.TryStartMatchAsync();

        clock.UtcNow = new DateTimeOffset(2026, 9, 25, 0, 1, 0, TimeSpan.Zero);
        var completion = await quota.CompleteMatchAsync(started.Session!);
        var currentDay = await quota.GetAvailabilityAsync();

        Assert.Equal(FreePlayCompletionStatus.Consumed, completion);
        Assert.Equal(3, currentDay.RemainingPlays);
        Assert.Contains(
            store.State!.CompletedMatches,
            match => match.QuotaDate == new DateOnly(2026, 9, 24));
    }

    [Fact]
    public async Task ClockRollbackDoesNotGrantAnotherAllowance()
    {
        var clock = new FakeClock(InitialTime);
        var quota = new DailyFreePlayQuota(clock, new MemoryQuotaStore());
        await CompleteMatchesAsync(quota, 3);

        clock.UtcNow = InitialTime.AddDays(-1);
        var rolledBack = await quota.GetAvailabilityAsync();

        Assert.Equal(new DateOnly(2026, 9, 24), rolledBack.QuotaDate);
        Assert.Equal(0, rolledBack.RemainingPlays);
    }

    [Fact]
    public async Task AvailabilityCompactsOnlyCompletedMatchesOutsideRetentionWindow()
    {
        var quotaDate = new DateOnly(2026, 9, 24);
        var oldDate = quotaDate.AddDays(-DailyFreePlayQuota.CompletedMatchRetentionDays - 1);
        var recentDate = quotaDate.AddDays(-DailyFreePlayQuota.CompletedMatchRetentionDays);
        var oldMatch = new CompletedFreePlay(Guid.NewGuid(), oldDate);
        var recentMatch = new CompletedFreePlay(Guid.NewGuid(), recentDate);
        var store = new MemoryQuotaStore(new FreePlayQuotaState(
            quotaDate,
            [oldMatch, recentMatch]));
        var quota = new DailyFreePlayQuota(new FakeClock(InitialTime), store);

        await quota.GetAvailabilityAsync();

        Assert.DoesNotContain(store.State!.CompletedMatches, match => match.MatchId == oldMatch.MatchId);
        Assert.Contains(store.State.CompletedMatches, match => match.MatchId == recentMatch.MatchId);
        Assert.Equal(quotaDate, store.State.LastObservedUtcDate);
    }

    [Fact]
    public async Task ANewMatchIsRejectedAfterTheDailyLimit()
    {
        var quota = new DailyFreePlayQuota(new FakeClock(InitialTime), new MemoryQuotaStore());
        await CompleteMatchesAsync(quota, 3);

        var rejected = await quota.TryStartMatchAsync();

        Assert.False(rejected.IsAllowed);
        Assert.Null(rejected.Session);
        Assert.Equal(0, rejected.Availability.RemainingPlays);
    }

    private static async Task CompleteMatchesAsync(DailyFreePlayQuota quota, int count)
    {
        for (var index = 0; index < count; index++)
        {
            var started = await quota.TryStartMatchAsync();
            Assert.True(started.IsAllowed);
            Assert.Equal(
                FreePlayCompletionStatus.Consumed,
                await quota.CompleteMatchAsync(started.Session!));
        }
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : IUtcClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class MemoryQuotaStore(FreePlayQuotaState? initialState = null) : IFreePlayQuotaStore
    {
        public FreePlayQuotaState? State { get; private set; } = initialState;

        public ValueTask<FreePlayQuotaState?> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(State);
        }

        public ValueTask SaveAsync(
            FreePlayQuotaState state,
            CancellationToken cancellationToken = default)
        {
            State = state;
            return ValueTask.CompletedTask;
        }
    }
}