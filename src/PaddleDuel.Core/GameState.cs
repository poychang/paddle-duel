namespace PaddleDuel.Core;

public enum MatchPhase
{
    Playing,
    Finished,
}

public enum PlayerSide
{
    Left,
    Right,
}

public readonly record struct PaddleInput(double LeftAxis, double RightAxis);

public readonly record struct PaddleState(double Y);

public readonly record struct BallState(
    double X,
    double Y,
    double VelocityX,
    double VelocityY);

public sealed record GameState(
    BallState Ball,
    PaddleState LeftPaddle,
    PaddleState RightPaddle,
    int LeftScore,
    int RightScore,
    int RallyHits,
    MatchPhase Phase,
    PlayerSide? Winner);