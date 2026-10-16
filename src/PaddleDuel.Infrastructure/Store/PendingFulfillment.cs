using System.Text.Json;

namespace PaddleDuel.Infrastructure.Store;

public sealed record PendingFulfillment(
    string StoreId,
    uint Quantity,
    string TrackingId);

public interface IPendingFulfillmentJournal
{
    ValueTask<IReadOnlyList<PendingFulfillment>> LoadAsync(
        CancellationToken cancellationToken = default);

    ValueTask UpsertAsync(
        PendingFulfillment fulfillment,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(
        string trackingId,
        CancellationToken cancellationToken = default);
}

public sealed class JsonPendingFulfillmentJournal(
    string directoryPath,
    string fileName = "pending-fulfillments.json") : IPendingFulfillmentJournal
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string filePath = Path.Combine(directoryPath, fileName);

    public async ValueTask<IReadOnlyList<PendingFulfillment>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadUnsafeAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask UpsertAsync(
        PendingFulfillment fulfillment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fulfillment);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var items = await LoadUnsafeAsync(cancellationToken);
            var updated = items
                .Where(item => item.TrackingId != fulfillment.TrackingId)
                .Append(fulfillment)
                .ToArray();
            await SaveUnsafeAsync(updated, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask RemoveAsync(
        string trackingId,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var items = await LoadUnsafeAsync(cancellationToken);
            var updated = items
                .Where(item => item.TrackingId != trackingId)
                .ToArray();
            await SaveUnsafeAsync(updated, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async ValueTask<IReadOnlyList<PendingFulfillment>> LoadUnsafeAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var items = JsonSerializer.Deserialize<List<PendingFulfillment>>(json, SerializerOptions)
            ?? throw new InvalidDataException("The fulfillment journal must contain an array.");
        if (items.Any(item => item is null || string.IsNullOrWhiteSpace(item.StoreId)
            || item.Quantity == 0 || string.IsNullOrWhiteSpace(item.TrackingId))
            || items.Select(item => item.TrackingId).Distinct(StringComparer.Ordinal).Count() != items.Count)
        {
            throw new InvalidDataException("The fulfillment journal contains invalid or duplicate entries.");
        }

        return items;
    }

    private async ValueTask SaveUnsafeAsync(
        IReadOnlyList<PendingFulfillment> items,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directoryPath);
        var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var json = JsonSerializer.Serialize(items, SerializerOptions);
            await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
            File.Move(temporaryPath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}