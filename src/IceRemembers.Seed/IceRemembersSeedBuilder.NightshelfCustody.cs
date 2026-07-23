namespace IceRemembers.Seed;

using Engine.Domain.Spatial;
using Engine.Domain.Startup;
using Engine.Graph;

using IceRemembers;

using K = IceRemembers.IceRemembersKinds;

internal static partial class IceRemembersSeedBuilder
{
    /// <summary>
    /// Nightshelf's start carries its identity in custody rather than surface
    /// footprint: held artifacts with strong custody edges feeding the Shadow
    /// ledger, matched to the brokers seeded in the base penguin plans.
    /// </summary>
    private static void CreateNightshelfCustodyAssets(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic)
    {
        var roum = FindEntityByName(seed, "Factor Roum☽-vel");
        var tutris = FindEntityByName(seed, "Veilmonger Tutris☽");
        var drownedLedge = FindEntityByName(seed, "Codex∴lockhold");
        var nightfallShelf = FindEntityByName(seed, "Nightshelf");

        CreateNightshelfCustodyArtifact(
            seed, random, takenPhysical, takenSemantic,
            "seal", "Smoke-held∴chain", "Chain", roum, drownedLedge);
        CreateNightshelfCustodyArtifact(
            seed, random, takenPhysical, takenSemantic,
            "instrument", "Hushed∴shadow-glass", "Shadow-glass", tutris, nightfallShelf);
    }

    private static void CreateNightshelfCustodyArtifact(
        SeedAssembly seed,
        Random random,
        HashSet<HexCoord> takenPhysical,
        HashSet<SemanticHexCoord> takenSemantic,
        string subtype,
        string name,
        string referenceName,
        SeedEntity holder,
        SeedEntity anchorSettlement)
    {
        var coord = ChoosePhysicalCoordinate(
            seed,
            random,
            takenPhysical,
            K.Artifact,
            subtype,
            C(IceRemembersCulture.Nightshelf),
            minimumDistance: 0,
            origin: anchorSettlement.Coordinates,
            searchRadius: 4);
        var artifact = CreateEntity(
            seed,
            K.Artifact, subtype, name, referenceName, C(IceRemembersCulture.Nightshelf),
            coordinates: (coord.Q, coord.R));
        takenPhysical.Add(coord);
        SetSemanticNaturalNear(seed, artifact, anchorSettlement, random, takenSemantic, searchRadius: 10);
        CreateRelationship(seed, "anchored", RelationshipPolarity.Neutral, artifact.SeedEntityId, anchorSettlement.SeedEntityId, 0.9f);
        CreateRelationship(seed, "holds", RelationshipPolarity.Neutral, holder.SeedEntityId, artifact.SeedEntityId, 1f);
    }
}
