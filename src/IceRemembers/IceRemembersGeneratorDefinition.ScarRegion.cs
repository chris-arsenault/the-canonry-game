namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Domain.Spatial;
using Engine.Kinds;
using Engine.WorldGen;

using K = IceRemembers.IceRemembersKinds;
using Terrain = IceRemembers.IceRemembersTerrain;

public static partial class IceRemembersGeneratorDefinition
{
    private static SemanticProducedEntity ScarRegion(string @ref, string cultureRef)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(K.Region, "scar");
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(K.Region, "scar");
        return new()
        {
            Ref = @ref,
            Kind = K.Region,
            Subtype = "scar",
            Role = "scar",
            PhysicalPlacement = SemanticPlacement.Near,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            PhysicalPlacementPreferredSites = physicalProfile.PreferredSites,
            PhysicalPlacementWarmthAffinity = physicalProfile.WarmthAffinity,
            PhysicalPlacementFireAffinity = physicalProfile.FireAffinity,
            PhysicalPlacementAuroraAffinity = physicalProfile.AuroraAffinity,
            PhysicalPlacementPressureAffinity = physicalProfile.PressureAffinity,
            SemanticPlacement = SemanticPlacement.Near,
            SemanticPlacementPreferredTerrains = semanticProfile.PreferredTerrains,
            SemanticPlacementAvoidedTerrains = semanticProfile.AvoidedTerrains,
            SemanticPlacementFallbackTerrain = semanticProfile.FallbackTerrain,
            PlacementRef = "$target",
            InheritCultureFrom = cultureRef,
            CulturePlacement = GeneratorCulturePlacement.CultureFrontier,
            Coverage = new SemanticRegionCoverage
            {
                Mode = CoveragePlacementMode.CreateNew,
                DesiredCellCount = 8,
                MinimumCellCount = 4,
                TransformTerrain = Terrain.Wastes.ToDomainName(),
            },
        };
    }

    private static SemanticProducedEntity HarborAcross(string @ref, string placementRef)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(K.Settlement, "harbor");
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(K.Settlement, "harbor");
        return new()
        {
            Ref = @ref,
            Kind = K.Settlement,
            Subtype = "harbor",
            Role = "harbor",
            PhysicalPlacement = SemanticPlacement.Across,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            PhysicalPlacementPreferredSites = physicalProfile.PreferredSites,
            PhysicalPlacementWarmthAffinity = physicalProfile.WarmthAffinity,
            PhysicalPlacementFireAffinity = physicalProfile.FireAffinity,
            PhysicalPlacementAuroraAffinity = physicalProfile.AuroraAffinity,
            PhysicalPlacementPressureAffinity = physicalProfile.PressureAffinity,
            SemanticPlacement = SemanticPlacement.Near,
            SemanticPlacementPreferredTerrains = semanticProfile.PreferredTerrains,
            SemanticPlacementAvoidedTerrains = semanticProfile.AvoidedTerrains,
            SemanticPlacementFallbackTerrain = semanticProfile.FallbackTerrain,
            PlacementRef = placementRef,
            InheritCultureFrom = placementRef,
            CulturePlacement = GeneratorCulturePlacement.CultureRoute,
        };
    }

    private static GeneratorCulturePlacement CulturePlacementFor(EntityKind kind)
    {
        if (kind == K.Settlement ||
            kind == K.Faction ||
            kind == K.PointOfInterest ||
            kind == K.Region)
        {
            return GeneratorCulturePlacement.CultureInterior;
        }

        return GeneratorCulturePlacement.PreferInheritedCulture;
    }
}
