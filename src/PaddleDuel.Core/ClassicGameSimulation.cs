namespace PaddleDuel.Core;

public sealed class ClassicGameSimulation(Classic1972Rules rules)
{
    public Classic1972Rules Rules { get; } = rules;

    public GameState CreateInitialState(PlayerSide serveToward = PlayerSide.Right)
    {
        var paddleY = (Rules.FieldHeight - Rules.PaddleHeight) / 2;
        var velocityX = serveToward == PlayerSide.Right
            ? Rules.InitialBallSpeedX
            : -Rules.InitialBallSpeedX;

        return new GameState(
            new BallState(
                Rules.FieldWidth / 2,
                Rules.FieldHeight / 2,
                velocityX,
                Rules.InitialBallSpeedY),
            new PaddleState(paddleY),
            new PaddleState(paddleY),
            0,
            0,
            0,
            MatchPhase.Playing,
            null);
    }

    public GameState Step(GameState state, PaddleInput input, double elapsedSeconds)
    {
        if (state.Phase != MatchPhase.Playing || elapsedSeconds <= 0)
        {
            return state;
        }

        var leftPaddle = MovePaddle(state.LeftPaddle, input.LeftAxis, elapsedSeconds);
        var rightPaddle = MovePaddle(state.RightPaddle, input.RightAxis, elapsedSeconds);
        var ball = state.Ball with
        {
            X = state.Ball.X + (state.Ball.VelocityX * elapsedSeconds),
            Y = state.Ball.Y + (state.Ball.VelocityY * elapsedSeconds),
        };

        ball = BounceOffWalls(ball);

        var rallyHits = state.RallyHits;
        if (HitsLeftPaddle(ball, leftPaddle))
        {
            rallyHits++;
            ball = BounceOffPaddle(ball, leftPaddle, PlayerSide.Left, rallyHits);
        }
        else if (HitsRightPaddle(ball, rightPaddle))
        {
            rallyHits++;
            ball = BounceOffPaddle(ball, rightPaddle, PlayerSide.Right, rallyHits);
        }

        var updated = state with
        {
            Ball = ball,
            LeftPaddle = leftPaddle,
            RightPaddle = rightPaddle,
            RallyHits = rallyHits,
        };

        var radius = Rules.BallSize / 2;
        if (ball.X + radius < 0)
        {
            return ScorePoint(updated, PlayerSide.Right);
        }

        if (ball.X - radius > Rules.FieldWidth)
        {
            return ScorePoint(updated, PlayerSide.Left);
        }

        return updated;
    }

    private PaddleState MovePaddle(PaddleState paddle, double axis, double elapsedSeconds)
    {
        var minimumY = Rules.PaddleTopDeadZone;
        var maximumY = Rules.FieldHeight - Rules.PaddleHeight;
        var nextY = paddle.Y + (Math.Clamp(axis, -1, 1) * Rules.PaddleSpeed * elapsedSeconds);
        return new PaddleState(Math.Clamp(nextY, minimumY, maximumY));
    }

    private BallState BounceOffWalls(BallState ball)
    {
        var radius = Rules.BallSize / 2;
        if (ball.Y - radius < 0)
        {
            return ball with { Y = radius, VelocityY = Math.Abs(ball.VelocityY) };
        }

        if (ball.Y + radius > Rules.FieldHeight)
        {
            return ball with
            {
                Y = Rules.FieldHeight - radius,
                VelocityY = -Math.Abs(ball.VelocityY),
            };
        }

        return ball;
    }

    private bool HitsLeftPaddle(BallState ball, PaddleState paddle)
    {
        var radius = Rules.BallSize / 2;
        var paddleRight = Rules.PaddleInset + Rules.PaddleWidth;
        return ball.VelocityX < 0
            && ball.X - radius <= paddleRight
            && ball.X + radius >= Rules.PaddleInset
            && OverlapsVertically(ball, paddle);
    }

    private bool HitsRightPaddle(BallState ball, PaddleState paddle)
    {
        var radius = Rules.BallSize / 2;
        var paddleLeft = Rules.FieldWidth - Rules.PaddleInset - Rules.PaddleWidth;
        return ball.VelocityX > 0
            && ball.X + radius >= paddleLeft
            && ball.X - radius <= paddleLeft + Rules.PaddleWidth
            && OverlapsVertically(ball, paddle);
    }

    private bool OverlapsVertically(BallState ball, PaddleState paddle)
    {
        var radius = Rules.BallSize / 2;
        return ball.Y + radius >= paddle.Y
            && ball.Y - radius <= paddle.Y + Rules.PaddleHeight;
    }

    private BallState BounceOffPaddle(
        BallState ball,
        PaddleState paddle,
        PlayerSide side,
        int rallyHits)
    {
        var relativeHit = Math.Clamp((ball.Y - paddle.Y) / Rules.PaddleHeight, 0, 0.999999);
        var segment = (int)(relativeHit * 8);
        var speedX = Math.Min(
            Rules.InitialBallSpeedX + (Rules.SpeedIncreasePerHit * rallyHits),
            Rules.MaximumBallSpeedX);
        var velocityX = side == PlayerSide.Left ? speedX : -speedX;
        var contactX = side == PlayerSide.Left
            ? Rules.PaddleInset + Rules.PaddleWidth + (Rules.BallSize / 2)
            : Rules.FieldWidth - Rules.PaddleInset - Rules.PaddleWidth - (Rules.BallSize / 2);

        return ball with
        {
            X = contactX,
            VelocityX = velocityX,
            VelocityY = Rules.GetBounceFactor(segment) * Rules.BounceSpeedY,
        };
    }

    private GameState ScorePoint(GameState state, PlayerSide scorer)
    {
        var leftScore = state.LeftScore + (scorer == PlayerSide.Left ? 1 : 0);
        var rightScore = state.RightScore + (scorer == PlayerSide.Right ? 1 : 0);
        var winner = leftScore >= Rules.WinningScore
            ? PlayerSide.Left
            : rightScore >= Rules.WinningScore
                ? PlayerSide.Right
                : (PlayerSide?)null;

        if (winner is not null)
        {
            return state with
            {
                LeftScore = leftScore,
                RightScore = rightScore,
                RallyHits = 0,
                Phase = MatchPhase.Finished,
                Winner = winner,
            };
        }

        var reset = CreateInitialState(scorer == PlayerSide.Left ? PlayerSide.Right : PlayerSide.Left);
        return reset with { LeftScore = leftScore, RightScore = rightScore };
    }
}