namespace PaddleDuel.Core;

public sealed class FixedStepGameLoop
{
    private const long MaximumElapsedTicks = TimeSpan.TicksPerSecond / 4;
    private readonly ClassicGameSimulation simulation;
    private long scaledTickAccumulator;

    public FixedStepGameLoop(ClassicGameSimulation simulation, GameState initialState)
    {
        this.simulation = simulation;
        State = initialState;
    }

    public GameState State { get; private set; }

    public void Advance(TimeSpan elapsed, PaddleInput input)
    {
        var elapsedTicks = Math.Clamp(elapsed.Ticks, 0, MaximumElapsedTicks);
        scaledTickAccumulator += elapsedTicks * simulation.Rules.TickRate;

        while (scaledTickAccumulator >= TimeSpan.TicksPerSecond)
        {
            State = simulation.Step(State, input, 1d / simulation.Rules.TickRate);
            scaledTickAccumulator -= TimeSpan.TicksPerSecond;
        }
    }
}