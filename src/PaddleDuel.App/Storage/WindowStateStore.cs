using System.Text.Json;

namespace PaddleDuel.App.Storage;

public sealed record WindowStateSnapshot(
    int X,
    int Y,
    int Width,
    int Height,
    bool IsFullScreen);

public sealed class WindowStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string filePath = Path.Combine(
        LocalStateDirectory.GetPath(),
        "window-state.json");

    public WindowStateSnapshot? Load()
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var state = JsonSerializer.Deserialize<WindowStateSnapshot>(
                File.ReadAllText(filePath),
                SerializerOptions);
            return IsValid(state) ? state : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void Save(WindowStateSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!IsValid(state))
        {
            throw new ArgumentException("Window state contains invalid values.", nameof(state));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state, SerializerOptions));
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

    private static bool IsValid(WindowStateSnapshot? state)
    {
        return state is not null
            && state.Width >= 480
            && state.Height >= 360;
    }
}