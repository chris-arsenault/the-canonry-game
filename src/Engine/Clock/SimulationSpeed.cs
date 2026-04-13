namespace Engine.Clock;

/// <summary>
/// Simulation speed multiplier. Determines how many ticks are processed per frame.
/// Values are tick counts per frame, not bit flags.
/// </summary>
#pragma warning disable CA1027 // Values are speed multipliers, not flags
public enum SimulationSpeed
#pragma warning restore CA1027
{
    Paused = 0,
    Normal = 1,
    Fast = 2,
    VeryFast = 4,
}
