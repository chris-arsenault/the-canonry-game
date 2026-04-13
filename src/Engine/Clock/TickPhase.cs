namespace Engine.Clock;

/// <summary>
/// Execution phases within a single tick. Systems execute in strict order:
/// results from phase N are visible to phase N+1 but NOT to phase N.
/// </summary>
public enum TickPhase
{
    /// <summary>Low-altitude systems: entity behaviors, relationships, lifecycle, resources.</summary>
    LowAltitude = 0,

    /// <summary>Pressure recomputation from low-altitude outputs.</summary>
    Pressure = 1,

    /// <summary>Card precondition evaluation against pressure and world state.</summary>
    CardPreconditions = 2,

    /// <summary>Card execution: generators, systems, actions that fired.</summary>
    CardExecution = 3,

    /// <summary>Event notification flush.</summary>
    EventNotification = 4,
}
