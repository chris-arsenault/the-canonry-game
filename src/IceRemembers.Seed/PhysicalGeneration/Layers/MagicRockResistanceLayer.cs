namespace IceRemembers.Seed;

using Engine.Constants;

using IceRemembers;

/// <summary>
/// What the magic does to the rock itself.
///
/// The engine's erosion knows only that rock has a resistance; it cannot see
/// magic, and should not. This layer supplies the domain's contribution to
/// that number, so magic shows up in the shape of the land rather than only
/// in its colour: fire-worked ground bakes hard and stands as ridges and
/// waterfall lips long after softer rock around it has gone, aurora ground is
/// friable and washes into wide hollows, and pressure barely touches
/// durability at all.
///
/// Applied on top of whatever geology already wrote, so an impact ring under
/// an aurora field is still harder than the plain beside it.
/// </summary>
internal sealed class MagicRockResistanceLayer : IIceRemembersMicrogeographyLayer
{
    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "magic-rock-resistance",
        0,
        IceRemembersMicrogeographyFacts.None,
        IceRemembersMicrogeographyFacts.MagicHistory,
        IceRemembersMicrogeographyFacts.Validated);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        var profile = context.Profile;
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            var fire = map.FireIntensity[triangleIndex] / 1000d;
            var aurora = map.AuroraIntensity[triangleIndex] / 1000d;
            var delta = (fire * profile.FireRockHardening) -
                (aurora * profile.AuroraRockWeakening);
            if (delta == 0d)
            {
                continue;
            }

            map.Physical.RockResistance[triangleIndex] = PhysicalRock.Clamp(
                map.Physical.RockResistance[triangleIndex] +
                (int)Math.Round(delta, MidpointRounding.AwayFromZero));
        }
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = context;
        foreach (var resistance in map.Physical.RockResistance)
        {
            if (resistance < PhysicalRock.MinimumResistance ||
                resistance > PhysicalRock.MaximumResistance)
            {
                throw new InvalidOperationException("Rock resistance left its legal range.");
            }
        }
    }
}
