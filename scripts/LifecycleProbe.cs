#:property TargetFramework=net10.0
#:property PublishAot=false
#:project ..\src\PaddleDuel.Infrastructure\PaddleDuel.Infrastructure.csproj

using PaddleDuel.Core;
using PaddleDuel.Infrastructure;
using PaddleDuel.Infrastructure.Store;

if (args.Length != 3 || args[0] is not ("prepare" or "retry"))
{
    throw new ArgumentException("Usage: LifecycleProbe.cs prepare|retry <LocalState> <test-run-guid>");
}

var directory = Path.GetFullPath(args[1]);
var expectedDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Packages", "25526PoyChang.PaddleDuel_hwmjejkjqxx7e", "LocalState");
var runId = Guid.Parse(args[2]).ToString();
if (!string.Equals(directory, expectedDirectory, StringComparison.OrdinalIgnoreCase)
    || File.ReadAllText(Path.Combine(directory, "lifecycle-test-owner.txt")) != runId)
{
    throw new InvalidOperationException("Refusing to modify state outside the marked lifecycle test installation.");
}

const string TestStoreId = "lifecycle.test.plays10";
var store = new AtomicJsonFreePlayQuotaStore(directory);
var journal = new JsonPendingFulfillmentJournal(directory);
var gateway = new FakeStoreGateway([new StoreProduct(TestStoreId, "Lifecycle fixture", "Not for sale", 10)]);
Require((await gateway.RequestPurchaseAsync(TestStoreId)).Status == StoreOperationStatus.Succeeded,
    "Fake gateway setup failed.");

if (args[0] == "prepare")
{
    var previous = await store.LoadAsync();
    Require(previous is not null && previous.CompletedMatches.Count == 0, "Expected a fresh quota.");
    Require((await journal.LoadAsync()).Count == 0, "Refusing to replace an existing pending journal.");

    await File.WriteAllTextAsync(Path.Combine(directory, "free-play-quota.json"), "{lifecycle-invalid-json");
    Require(await store.LoadAsync() is null, "Malformed quota was not quarantined.");
    Require(File.Exists(Path.Combine(directory, "free-play-quota.json.corrupt")), "Missing quarantine file.");

    var clock = new ProbeClock(DateTimeOffset.UtcNow.AddDays(2));
    var quota = new DailyFreePlayQuota(clock, store);
    for (var index = 0; index < 2; index++)
    {
        var start = await quota.TryStartMatchAsync();
        Require(start.IsAllowed && start.Session is not null, "Could not create a fixture match.");
        Require(await quota.CompleteMatchAsync(start.Session!) == FreePlayCompletionStatus.Consumed,
            "Fixture match did not consume exactly once.");
        Require(await quota.CompleteMatchAsync(start.Session!) == FreePlayCompletionStatus.AlreadyConsumed,
            "Fixture completion was not idempotent.");
    }
    Require((await quota.GetAvailabilityAsync()).RemainingPlays == 1, "Expected one remaining free play.");

    gateway.FulfillmentStatus = StoreOperationStatus.NetworkError;
    var coordinator = new PendingFulfillmentCoordinator(() => gateway, journal);
    Require(await coordinator.FulfillAsync(TestStoreId, 1, runId) == EntitlementCompletionStatus.Pending,
        "Failed fake fulfillment did not remain pending.");
    Require((await journal.LoadAsync()).Single() == new PendingFulfillment(TestStoreId, 1, runId),
        "Incorrect persisted tracking ID or quantity.");
    Console.WriteLine("PASS: production quota/journal writers created rollback-safe quota, quarantine and pending fixture.");
}
else
{
    var state = await store.LoadAsync() ?? throw new InvalidOperationException("Upgrade lost the quota.");
    Require(state.CompletedMatches.Count == 2, "Upgrade lost completed match IDs.");
    var quota = new DailyFreePlayQuota(new ProbeClock(DateTimeOffset.UtcNow), store);
    var availability = await quota.GetAvailabilityAsync();
    Require(availability.QuotaDate == state.LastObservedUtcDate && availability.RemainingPlays == 1,
        "Restart/clock rollback granted a new allowance.");
    var prior = state.CompletedMatches[0];
    Require(await quota.CompleteMatchAsync(new FreePlayMatchSession(prior.MatchId, prior.QuotaDate))
        == FreePlayCompletionStatus.AlreadyConsumed, "Upgrade lost quota idempotency.");
    Require(File.ReadAllText(Path.Combine(directory, "free-play-quota.json.corrupt")) == "{lifecycle-invalid-json",
        "Upgrade lost the quarantined data.");

    var fulfillment = new PendingFulfillment(TestStoreId, 1, runId);
    Require((await journal.LoadAsync()).Single() == fulfillment, "Upgrade changed pending fulfillment.");
    var coordinator = new PendingFulfillmentCoordinator(() => gateway, journal);
    Require(await coordinator.RetryPendingAsync() == 1, "Pending retry did not complete.");
    Require((await journal.LoadAsync()).Count == 0, "Successful retry did not clear the journal.");
    Require((await gateway.GetBalanceAsync(TestStoreId)).Balance?.UnitsRemaining == 9, "Incorrect fake balance.");

    // Model a restart after Store success but before the local journal removal became durable.
    await journal.UpsertAsync(fulfillment);
    var recreated = new PendingFulfillmentCoordinator(
        () => gateway, new JsonPendingFulfillmentJournal(directory));
    Require(await recreated.RetryPendingAsync() == 1, "Already-fulfilled retry did not clear the journal.");
    Require(await recreated.RetryPendingAsync() == 0, "An empty journal retried again.");
    Require((await gateway.GetBalanceAsync(TestStoreId)).Balance?.UnitsRemaining == 9, "Duplicate consumption.");
    Require(gateway.FulfillmentTrackingIds.Single() == runId, "Retry changed the tracking ID.");
    Console.WriteLine("PASS: preserved quota/date/IDs, rollback protection, pending retry and already-fulfilled idempotency.");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed record ProbeClock(DateTimeOffset UtcNow) : IUtcClock;
