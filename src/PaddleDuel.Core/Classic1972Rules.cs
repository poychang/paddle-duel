namespace PaddleDuel.Core;

public sealed record Classic1972Rules
{
    public double FieldWidth { get; init; } = 256;
    public double FieldHeight { get; init; } = 192;
    public double PaddleWidth { get; init; } = 4;
    public double PaddleHeight { get; init; } = 32;
    public double PaddleInset { get; init; } = 16;
    public double PaddleSpeed { get; init; } = 120;
    public double PaddleTopDeadZone { get; init; } = 8;
    public double BallSize { get; init; } = 4;
    public double InitialBallSpeedX { get; init; } = 90;
    public double InitialBallSpeedY { get; init; } = 30;
    public double BounceSpeedY { get; init; } = 75;
    public double SpeedIncreasePerHit { get; init; } = 6;
    public double MaximumBallSpeedX { get; init; } = 174;
    public int WinningScore { get; init; } = 11;
    public int TickRate { get; init; } = 120;

    public double GetBounceFactor(int segment) => segment switch
    {
        0 => -1.00,
        1 => -0.75,
        2 => -0.50,
        3 => -0.25,
        4 => 0.25,
        5 => 0.50,
        6 => 0.75,
        7 => 1.00,
        _ => throw new ArgumentOutOfRangeException(nameof(segment)),
    };
}