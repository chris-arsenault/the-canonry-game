namespace Engine.Gameplay;

/// <summary>
/// The reusable gameplay surfaces that a claimed knowledge site can modify.
/// Targets use stable domain names for authored rules, or primitive codes for
/// engine-owned concepts such as planes and minor-actor purposes.
/// </summary>
public enum KnowledgeGrantDomain
{
    None = 0,
    CultureProduction = 1,
    CulturePersistence = 2,
    VerbWeight = 3,
    GeneratorPulseWeight = 4,
    GambitScore = 5,
    GambitSuccess = 6,
    MinorActorWork = 7,
    MovementStepRate = 8,
    RoadWork = 9,
    RelationshipMaintenance = 10,
    CarryingCapacity = 11,
    Attraction = 12,
    FrontierThawThreshold = 13,
    FrontierReclaimThreshold = 14,
    DecayTideResistance = 15,
}

public sealed record KnowledgeGrantConfig(
    KnowledgeGrantDomain Domain,
    float Multiplier,
    string? Target = null,
    int TargetCode = 0);
