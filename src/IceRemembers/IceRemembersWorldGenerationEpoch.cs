namespace IceRemembers;

using Engine.WorldGen;

/// <summary>
/// Ice Remembers authored forcing wrapped around one domain-neutral physical
/// evolution epoch.
/// </summary>
public sealed record IceRemembersWorldGenerationEpoch
{
    public required PhysicalGeographyEpochDefinition Physical { get; init; }

    public int FireStrength { get; init; } = 1000;

    public int AuroraStrength { get; init; } = 1000;

    public int PressureStrength { get; init; } = 1000;

    public void Validate()
    {
        Physical?.Validate();
        if (Physical is null || !IsStrength(FireStrength) || !IsStrength(AuroraStrength) ||
            !IsStrength(PressureStrength))
        {
            throw new InvalidOperationException("Ice Remembers epoch forcing is invalid.");
        }
    }

    private static bool IsStrength(int value) => value is >= 0 and <= 1000;
}
