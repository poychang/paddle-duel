namespace PaddleDuel.Core;

public enum ClassicAiDifficulty
{
    Easy,
    Medium,
    Hard,
}

public sealed record ClassicAiDifficultyProfile(
    double ReactionIntervalSeconds,
    double AimOffset)
{
    public static ClassicAiDifficultyProfile For(ClassicAiDifficulty difficulty)
    {
        return difficulty switch
        {
            ClassicAiDifficulty.Easy => new(0.24, 6),
            ClassicAiDifficulty.Medium => new(0.12, 2),
            ClassicAiDifficulty.Hard => new(0.06, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
        };
    }
}

public sealed class ClassicAiController
{
    private readonly Classic1972Rules rules;
    private readonly double reactionIntervalSeconds;
    private readonly double aimOffset;
    private double elapsedSinceReaction = double.MaxValue;
    private double currentAxis;

    public ClassicAiController(
        Classic1972Rules rules,
        double reactionIntervalSeconds = 0.12,
        double aimOffset = 0)
    {
        this.rules = rules;
        this.reactionIntervalSeconds = reactionIntervalSeconds;
        this.aimOffset = aimOffset;
        Difficulty = null;
    }

    public ClassicAiController(Classic1972Rules rules, ClassicAiDifficulty difficulty)
    {
        this.rules = rules;
        var profile = ClassicAiDifficultyProfile.For(difficulty);
        reactionIntervalSeconds = profile.ReactionIntervalSeconds;
        aimOffset = profile.AimOffset;
        Difficulty = difficulty;
    }

    public ClassicAiDifficulty? Difficulty { get; }

    public double ReactionIntervalSeconds => reactionIntervalSeconds;

    public double AimOffset => aimOffset;

    public double Update(GameState state, double elapsedSeconds)
    {
        elapsedSinceReaction += Math.Max(0, elapsedSeconds);
        if (elapsedSinceReaction < reactionIntervalSeconds)
        {
            return currentAxis;
        }

        elapsedSinceReaction = 0;
        var paddleCenter = state.RightPaddle.Y + (rules.PaddleHeight / 2);
        var targetY = rules.FieldHeight / 2;

        if (state.Ball.VelocityX > 0)
        {
            var paddleX = rules.FieldWidth - rules.PaddleInset - rules.PaddleWidth;
            var secondsToPaddle = Math.Max(0, (paddleX - state.Ball.X) / state.Ball.VelocityX);
            targetY = ReflectWithinField(state.Ball.Y + (state.Ball.VelocityY * secondsToPaddle));
        }

        var error = targetY + aimOffset - paddleCenter;
        currentAxis = Math.Abs(error) < 1 ? 0 : Math.Sign(error);
        return currentAxis;
    }

    public void Reset()
    {
        elapsedSinceReaction = double.MaxValue;
        currentAxis = 0;
    }

    private double ReflectWithinField(double y)
    {
        var radius = rules.BallSize / 2;
        var span = rules.FieldHeight - (2 * radius);
        var period = 2 * span;
        var position = (y - radius) % period;
        if (position < 0)
        {
            position += period;
        }

        return radius + (position <= span ? position : period - position);
    }
}