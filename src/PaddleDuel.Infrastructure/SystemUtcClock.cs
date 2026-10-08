using PaddleDuel.Core;

namespace PaddleDuel.Infrastructure;

public sealed class SystemUtcClock : IUtcClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}