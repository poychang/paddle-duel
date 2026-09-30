using Arcade1972.Core;

namespace Arcade1972.Tests;

public sealed class Classic1972GoldenRegressionTests
{
    private readonly Classic1972Rules rules = new();

    [Fact]
    public void CreateInitialState_MatchesGoldenBaseline()
    {
        var simulation = new ClassicGameSimulation(rules);

        var state = simulation.CreateInitialState();

        Assert.Equal(new BallState(128, 96, 90, 30), state.Ball);
        Assert.Equal(new PaddleState(80), state.LeftPaddle);
        Assert.Equal(new PaddleState(80), state.RightPaddle);
        Assert.Equal(0, state.LeftScore);
        Assert.Equal(0, state.RightScore);
        Assert.Equal(0, state.RallyHits);
        Assert.Equal(MatchPhase.Playing, state.Phase);
    }

    [Fact]
    public void Step_ClampsPaddlesToTheHistoricalPlayfieldBounds()
    {
        var simulation = new ClassicGameSimulation(rules);
        var initial = simulation.CreateInitialState() with
        {
            Ball = new BallState(128, 96, 0, 0),
        };

        var movedUp = simulation.Step(initial, new PaddleInput(-1, -1), 10);
        var movedDown = simulation.Step(initial, new PaddleInput(1, 1), 10);

        Assert.Equal(rules.PaddleTopDeadZone, movedUp.LeftPaddle.Y);
        Assert.Equal(rules.PaddleTopDeadZone, movedUp.RightPaddle.Y);
        Assert.Equal(rules.FieldHeight - rules.PaddleHeight, movedDown.LeftPaddle.Y);
        Assert.Equal(rules.FieldHeight - rules.PaddleHeight, movedDown.RightPaddle.Y);
    }

    [Fact]
    public void Step_BouncesAtTheTopAndBottomFieldEdges()
    {
        var simulation = new ClassicGameSimulation(rules);
        var initial = simulation.CreateInitialState();
        var radius = rules.BallSize / 2;

        var top = initial with
        {
            Ball = new BallState(128, radius, 0, -20),
        };
        var bottom = initial with
        {
            Ball = new BallState(128, rules.FieldHeight - radius, 0, 20),
        };

        var topResult = simulation.Step(top, default, 1d / rules.TickRate);
        var bottomResult = simulation.Step(bottom, default, 1d / rules.TickRate);

        Assert.Equal(radius, topResult.Ball.Y, 8);
        Assert.Equal(20, topResult.Ball.VelocityY, 8);
        Assert.Equal(rules.FieldHeight - radius, bottomResult.Ball.Y, 8);
        Assert.Equal(-20, bottomResult.Ball.VelocityY, 8);
    }

    [Fact]
    public void Step_AwardingPointResetsServeDirectionAndRallySpeed()
    {
        var simulation = new ClassicGameSimulation(rules);
        var state = simulation.CreateInitialState() with
        {
            RallyHits = 4,
            Ball = new BallState(-rules.BallSize, 96, -rules.MaximumBallSpeedX, 20),
        };

        var result = simulation.Step(state, default, 1d / rules.TickRate);

        Assert.Equal(0, result.LeftScore);
        Assert.Equal(1, result.RightScore);
        Assert.Equal(0, result.RallyHits);
        Assert.Equal(-rules.InitialBallSpeedX, result.Ball.VelocityX, 8);
        Assert.Equal(rules.InitialBallSpeedY, result.Ball.VelocityY, 8);
    }
}