using System.Runtime.InteropServices;
using PaddleDuel.Core;
using PaddleDuel.Infrastructure;
using Microsoft.Windows.Storage;

namespace PaddleDuel.App.Storage;

public sealed class LocalStateFreePlayQuotaStore : IFreePlayQuotaStore
{
    private const int AppModelErrorNoPackage = 15700;
    private readonly AtomicJsonFreePlayQuotaStore innerStore;

    public LocalStateFreePlayQuotaStore()
    {
        innerStore = new AtomicJsonFreePlayQuotaStore(GetLocalStatePath());
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

    private static string GetLocalStatePath()
    {
        var packageFullNameLength = 0;
        if (GetCurrentPackageFullName(ref packageFullNameLength, null) != AppModelErrorNoPackage)
        {
            using var applicationData = ApplicationData.GetDefault();
            return applicationData.LocalPath;
        }

        // Preserve the original unpackaged data location across product renames.
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Arcade1972");
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(
        ref int packageFullNameLength,
        char[]? packageFullName);
}