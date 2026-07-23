namespace IceRemembers.Seed;

using Engine.Domain.Spatial;
using Engine.Domain.Startup;
using Engine.Graph;

using IceRemembers;

using K = IceRemembers.IceRemembersKinds;

internal static partial class IceRemembersSeedBuilder
{
    private const int AccordCoverageRadius = 3;
    private const int TabooCoverageRadius = 2;

    /// <summary>
    /// Canonical laws seeded as semantic coverage: the Flipper Accord (the
    /// mutual hostage arrangement that ended the faction wars) and the
    /// Unspoken Silence (Nightshelf's prohibition on speaking of what waits
    /// in the deep). Their coverage patches are the first named jurisdictions
    /// of idea-space; entities whose semantic position falls inside are
    /// living under them.
    /// </summary>
    private static void CreateSeedCanons(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        (SeedEntity Council, SeedEntity Exchange, SeedEntity Claws, SeedEntity WakeSingers) factions)
    {
        var accord = CreateEntity(
            seed, K.Canon, "accord", "The Flipper Accord", "Flipper Accord", C(IceRemembersCulture.TheIce));
        SetPhysicalNaturalNear(seed, accord, factions.Council.Coordinates, random, takenPhysical, searchRadius: 10);
        SetSemanticNatural(seed, accord, random, takenSemantic);
        ClaimSemanticCoverage(seed, accord, AccordCoverageRadius);
        CreateRelationship(seed, "claimed_by", RelationshipPolarity.Neutral, accord.SeedEntityId, factions.Council.SeedEntityId, 0.6f);
        CreateRelationship(seed, "claimed_by", RelationshipPolarity.Neutral, accord.SeedEntityId, factions.WakeSingers.SeedEntityId, 0.4f);

        var silence = CreateEntity(
            seed, K.Canon, "taboo", "The Unspoken Silence", "The Silence", C(IceRemembersCulture.Nightshelf));
        SetPhysicalNaturalNear(seed, silence, factions.Claws.Coordinates, random, takenPhysical, searchRadius: 10);
        SetSemanticNaturalNear(seed, silence, factions.Claws, random, takenSemantic, searchRadius: 10);
        ClaimSemanticCoverage(seed, silence, TabooCoverageRadius);
        CreateRelationship(seed, "claimed_by", RelationshipPolarity.Neutral, silence.SeedEntityId, factions.Claws.SeedEntityId, 0.8f);
    }

    private static void ClaimSemanticCoverage(SeedAssembly seed, SeedEntity canon, int radius)
    {
        var center = canon.SemanticPosition;
        foreach (var (coord, cell) in seed.SemanticCells.ToArray())
        {
            if (SemanticDistance(coord, center) > radius || cell.OwnerSeedEntityId is not null)
            {
                continue;
            }

            seed.SetSemanticCell(coord, cell.Terrain, canon.SeedEntityId, cell.SuperregionId, cell.ContinentId);
        }
    }

    private static int SemanticDistance(SemanticHexCoord a, SemanticHexCoord b)
    {
        var dq = a.Q - b.Q;
        var dr = a.R - b.R;
        var ds = (-a.Q - a.R) - (-b.Q - b.R);
        return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(ds)) / 2;
    }
}
