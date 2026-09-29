using Arcade1972.Core;

namespace Arcade1972.Tests;

public sealed class ClassicAiControllerTests
{
    [Fact]
    public void DifficultyProfilesBecomeProgressivelyMoreResponsive()
    {
        var rules = new Classic1972Rules();
        var easy = new ClassicAiController(rules, ClassicAiDifficulty.Easy);
        var medium = new ClassicAiController(rules, ClassicAiDifficulty.Medium);
        var hard = new ClassicAiController(rules, ClassicAiDifficulty.Hard);

        Assert.Equal(ClassicAiDifficulty.Easy, easy.Difficulty);
        Assert.True(easy.ReactionIntervalSeconds > medium.ReactionIntervalSeconds);
        Assert.True(medium.ReactionIntervalSeconds > hard.ReactionIntervalSeconds);
        Assert.True(Math.Abs(easy.AimOffset) > Math.Abs(medium.AimOffset));
        Assert.Equal(0, hard.AimOffset);
    }

    [Fact]
    public void Update_MovesTowardPredictedBallPosition()
    {
        var rules = new Classic1972Rules();
        var simulation = new ClassicGameSimulation(rules);
        var state = simulation.CreateInitialState() with
        {
            Ball = new BallState(180, 150, rules.InitialBallSpeedX, 0),
        };
        var controller = new ClassicAiController(rules);

        var axis = controller.Update(state, 1);

        Assert.Equal(1, axis);
    }

    [Fact]
    public void Update_HoldsAxisUntilReactionIntervalElapses()
    {
        var rules = new Classic1972Rules();
        var simulation = new ClassicGameSimulation(rules);
        var controller = new ClassicAiController(rules, reactionIntervalSeconds: 0.2);
        var lowBall = simulation.CreateInitialState() with
        {
            Ball = new BallState(180, 150, rules.InitialBallSpeedX, 0),
        };
        var highBall = lowBall with
        {
            Ball = new BallState(180, 30, rules.InitialBallSpeedX, 0),
        };

        Assert.Equal(1, controller.Update(lowBall, 1));
        Assert.Equal(1, controller.Update(highBall, 0.1));
        Assert.Equal(-1, controller.Update(highBall, 0.1));
    }

    [Fact]
    public void Update_ReturnsTowardCenterWhenBallMovesAway()
    {
        var rules = new Classic1972Rules();
        var simulation = new ClassicGameSimulation(rules);
        var state = simulation.CreateInitialState() with
        {
            RightPaddle = new PaddleState(140),
            Ball = new BallState(180, 150, -rules.InitialBallSpeedX, 0),
        };
        var controller = new ClassicAiController(rules);

        var axis = controller.Update(state, 1);

        Assert.Equal(-1, axis);
    }
}