using System.Text.Json;
using PaddleDuel.Core;

namespace PaddleDuel.Infrastructure;

public sealed class AtomicJsonFreePlayQuotaStore(
    string directoryPath,
    string fileName = "free-play-quota.json") : IFreePlayQuotaStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string stateFilePath = Path.Combine(directoryPath, fileName);
    private readonly string corruptFilePath = Path.Combine(directoryPath, $"{fileName}.corrupt");

    public async ValueTask<FreePlayQuotaState?> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(stateFilePath))
            {
                return null;
            }

            try
            {
                FreePlayQuotaState? state;
                await using (var stream = new FileStream(
                    stateFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    state = await JsonSerializer.DeserializeAsync<FreePlayQuotaState>(
                        stream,
                        SerializerOptions,
                        cancellationToken);
                }

                return IsValid(state) ? state : QuarantineCorruptState();
            }
            catch (JsonException)
            {
                return QuarantineCorruptState();
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask SaveAsync(
        FreePlayQuotaState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!IsValid(state))
        {
            throw new ArgumentException("Quota state contains invalid values.", nameof(state));
        }

        await gate.WaitAsync(cancellationToken);
        string? temporaryFilePath = null;
        try
        {
            Directory.CreateDirectory(directoryPath);
            temporaryFilePath = Path.Combine(
                directoryPath,
                $"{fileName}.{Guid.NewGuid():N}.tmp");

            await using (var stream = new FileStream(
                temporaryFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    state,
                    SerializerOptions,
                    cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryFilePath, stateFilePath, overwrite: true);
            temporaryFilePath = null;
        }
        finally
        {
            if (temporaryFilePath is not null)
            {
                File.Delete(temporaryFilePath);
            }

            gate.Release();
        }
    }

    private static bool IsValid(FreePlayQuotaState? state)
    {
        return state is not null
            && state.LastObservedUtcDate != default
            && state.CompletedMatches is not null
            && state.CompletedMatches.All(match =>
                match.MatchId != Guid.Empty && match.QuotaDate != default);
    }

    private FreePlayQuotaState? QuarantineCorruptState()
    {
        Directory.CreateDirectory(directoryPath);
        File.Move(stateFilePath, corruptFilePath, overwrite: true);
        return null;
    }
}