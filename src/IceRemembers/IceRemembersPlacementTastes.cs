namespace IceRemembers;

using Engine.Domain.Config;

using Axis = Engine.Domain.Config.PlacementTasteAxisNames;
using Culture = IceRemembers.IceRemembersCulture;

/// <summary>
/// Placement taste tables: each culture's relationship to the land, stated
/// once as signed weights over the geography fact axes. Kinds author their
/// own taste on their entity definitions; both feed the same scorer, at
/// seed time and in the kernel.
/// </summary>
public static class IceRemembersPlacementTastes
{
    /// <summary>Shorthand for one signed axis weight.</summary>
    public static PlacementTasteWeightConfig W(string axis, float weight) =>
        new() { Axis = axis, Weight = weight };

    private static readonly Dictionary<Culture, IReadOnlyList<PlacementTasteWeightConfig>> ByCulture =
        new()
        {
            // Aurora Stack reads the sky: it settles under the lights and
            // keeps to the milder ground where its courts stay open.
            [Culture.AuroraStack] = [W(Axis.Aurora, 0.8f), W(Axis.Temperature, 0.2f)],

            // Nightshelf lives in the frozen margins: thick ice overhead,
            // the shore at its back, and no love for the bright sky.
            [Culture.Nightshelf] = [W(Axis.Ice, 0.6f), W(Axis.Coast, 0.3f), W(Axis.Aurora, -0.4f)],

            // Wake-Singers belong to the water: open sea, the deep pressure
            // roads, and the coasts where song meets shore.
            [Culture.WakeSingers] = [W(Axis.Water, 0.7f), W(Axis.Pressure, 0.5f), W(Axis.Coast, 0.4f)],

            // The Ice is the cold itself: whatever it touches, it wants
            // frozen, and it shuns every warm refuge.
            [Culture.TheIce] = [W(Axis.Ice, 0.9f), W(Axis.Temperature, -0.6f)],
        };

    public static IReadOnlyList<PlacementTasteWeightConfig> ForCulture(Culture culture) =>
        ByCulture.TryGetValue(culture, out var taste) ? taste : [];

    /// <summary>Culture taste by domain name; unknown or absent cultures taste nothing.</summary>
    public static IReadOnlyList<PlacementTasteWeightConfig> ForCultureName(string? cultureName)
    {
        if (string.IsNullOrEmpty(cultureName))
        {
            return [];
        }

        foreach (var (culture, taste) in ByCulture)
        {
            if (string.Equals(culture.ToDomainName(), cultureName, StringComparison.Ordinal))
            {
                return taste;
            }
        }

        return [];
    }
}
