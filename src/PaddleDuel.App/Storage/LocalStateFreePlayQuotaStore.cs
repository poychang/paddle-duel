using PaddleDuel.Core;
using PaddleDuel.Infrastructure;

namespace PaddleDuel.App.Storage;

public sealed class LocalStateFreePlayQuotaStore : IFreePlayQuotaStore
{
    private readonly AtomicJsonFreePlayQuotaStore innerStore;

    public LocalStateFreePlayQuotaStore()
    {
        innerStore = new AtomicJsonFreePlayQuotaStore(LocalStateDirectory.GetPath());
    }

    public ValueTask<FreePlayQuotaState?> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        return innerStore.LoadAsync(cancellationToken);
    }

    public ValueTask SaveAsync(
        FreePlayQuotaState state,
        CancellationToken cancellationToken = default)
    {
        return innerStore.SaveAsync(state, cancellationToken);
    }

}