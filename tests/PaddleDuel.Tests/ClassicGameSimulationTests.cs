using PaddleDuel.Core;

namespace PaddleDuel.Tests;

public sealed class ClassicGameSimulationTests
{
    private readonly Classic1972Rules rules = new();

    [Fact]
    public void Step_BouncesFromEachPaddleSegment()
    {
        var simulation = new ClassicGameSimulation(rules);
        var initial = simulation.CreateInitialState();

        for (var segment = 0; segment < 8; segment++)
        {
            var hitY = initial.LeftPaddle.Y + ((segment + 0.5) * rules.PaddleHeight / 8);
            var state = initial with
            {
                Ball = new BallState(
                    rules.PaddleInset + rules.PaddleWidth + (rules.BallSize / 2),
                    hitY,
                    -rules.InitialBallSpeedX,
                    0),
            };

            var result = simulation.Step(state, default, 1d / rules.TickRate);

            Assert.True(result.Ball.VelocityX > 0);
            Assert.Equal(rules.GetBounceFactor(segment) * rules.BounceSpeedY, result.Ball.VelocityY, 8);
        }
    }

    [Fact]
    public void Step_IncreasesHorizontalSpeedAfterAHit()
    {
        var simulation = new ClassicGameSimulation(rules);
        var initial = simulation.CreateInitialState();
        var state = initial with
        {
            Ball = new BallState(
                rules.PaddleInset + rules.PaddleWidth + (rules.BallSize / 2),
                initial.LeftPaddle.Y + (rules.PaddleHeight / 2),
                -rules.InitialBallSpeedX,
                0),
        };

        var result = simulation.Step(state, default, 1d / rules.TickRate);

        Assert.Equal(1, result.RallyHits);
        Assert.Equal(rules.InitialBallSpeedX + rules.SpeedIncreasePerHit, result.Ball.VelocityX, 8);
    }

    [Fact]
    public void Step_EndsMatchWhenAPlayerReachesEleven()
    {
        var simulation = new ClassicGameSimulation(rules);
        var initial = simulation.CreateInitialState();
        var state = initial with
        {
            LeftScore = rules.WinningScore - 1,
            Ball = new BallState(rules.FieldWidth + rules.BallSize, 80, rules.InitialBallSpeedX, 0),
        };

        var result = simulation.Step(state, default, 1d / rules.TickRate);

        Assert.Equal(rules.WinningScore, result.LeftScore);
        Assert.Equal(MatchPhase.Finished, result.Phase);
        Assert.Equal(PlayerSide.Left, result.Winner);
    }

    [Fact]
    public void Step_ResetsRallySpeedAfterAPoint()
    {
        var simulation = new ClassicGameSimulation(rules);
        var state = simulation.CreateInitialState() with
        {
            RallyHits = 7,
            Ball = new BallState(-rules.BallSize, 80, -rules.MaximumBallSpeedX, 0),
        };

        var result = simulation.Step(state, default, 1d / rules.TickRate);

        Assert.Equal(0, result.RallyHits);
        Assert.Equal(rules.InitialBallSpeedX, Math.Abs(result.Ball.VelocityX), 8);
    }

    [Fact]
    public void FixedStepLoop_ProducesSameStateForDifferentRenderCadences()
    {
        var simulation = new ClassicGameSimulation(rules);
        var sixtyHertz = new FixedStepGameLoop(simulation, simulation.CreateInitialState());
        var oneHundredTwentyHertz = new FixedStepGameLoop(simulation, simulation.CreateInitialState());

        for (var frame = 0; frame < 60; frame++)
        {
            sixtyHertz.Advance(TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60), default);
        }

        for (var frame = 0; frame < 120; frame++)
        {
            oneHundredTwentyHertz.Advance(TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 120), default);
        }

        Assert.Equal(sixtyHertz.State, oneHundredTwentyHertz.State);
    }
}