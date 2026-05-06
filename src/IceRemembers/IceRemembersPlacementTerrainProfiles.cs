namespace IceRemembers;

using K = IceRemembers.IceRemembersKinds;
using Terrain = IceRemembers.IceRemembersTerrain;

public sealed record IceRemembersPlacementTerrainProfile(
    IReadOnlySet<string> PreferredTerrains,
    IReadOnlyList<string> PreferredTerrainOrder,
    IReadOnlySet<string> AvoidedTerrains,
    string? FallbackTerrain);

public static class IceRemembersPlacementTerrainProfiles
{
    private static readonly IReadOnlySet<string> SemanticHostileTerrains = TerrainSet(
        Terrain.Severance,
        Terrain.TabooRidge,
        Terrain.CavePocket,
        Terrain.Silence);

    public static IceRemembersPlacementTerrainProfile Physical(
        EntityKind kind,
        string? subtype,
        string? culture = null)
    {
        var preferredTerrains = PhysicalPreferences(kind, subtype, culture);
        return new(
            TerrainSet(preferredTerrains),
            TerrainNames(preferredTerrains),
            PhysicalAvoidedTerrains(kind, subtype, culture),
            Terrain.Tundra.ToDomainName());
    }

    public static IceRemembersPlacementTerrainProfile Semantic(
        EntityKind kind,
        string? subtype,
        string? culture = null)
    {
        var preferredTerrains = SemanticPreferenceTerrains(kind, subtype, culture);
        return new(
            TerrainSet(preferredTerrains),
            TerrainNames(preferredTerrains),
            SemanticHostileTerrains,
            Terrain.OpenField.ToDomainName());
    }

    public static IReadOnlySet<string> SemanticPreferences(
        EntityKind kind,
        string? subtype,
        string? culture = null) =>
        TerrainSet(SemanticPreferenceTerrains(kind, subtype, culture));

    public static IReadOnlyList<Terrain> SemanticPreferenceTerrains(
        EntityKind kind,
        string? subtype,
        string? culture = null)
    {
        if (kind == K.Settlement)
        {
            return SettlementSemanticPreferences(subtype, culture);
        }

        if (kind == K.Faction)
        {
            return FactionSemanticPreferences(subtype, culture);
        }

        if (kind == K.Penguin)
        {
            return PenguinSemanticPreferences(subtype);
        }

        if (kind == K.Artifact)
        {
            return ArtifactSemanticPreferences(subtype);
        }

        if (kind == K.PointOfInterest)
        {
            return PointOfInterestSemanticPreferences(subtype);
        }

        if (kind == K.Ability)
        {
            return AbilitySemanticPreferences(culture);
        }

        return GeneralSemanticPreferences(kind);
    }

    private static IReadOnlyList<Terrain> PhysicalPreferences(
        EntityKind kind,
        string? subtype,
        string? culture)
    {
        if (kind == K.Region)
        {
            return RegionPhysicalPreferences(subtype);
        }

        if (kind == K.Settlement)
        {
            return SettlementPhysicalPreferences(subtype, culture);
        }

        if (kind == K.Faction)
        {
            return FactionPhysicalPreferences(subtype, culture);
        }

        if (kind == K.PointOfInterest)
        {
            return PointOfInterestPhysicalPreferences(subtype);
        }

        return GeneralPhysicalPreferences(kind);
    }

    private static IReadOnlyList<Terrain> RegionPhysicalPreferences(string? subtype) =>
        subtype switch
        {
            "face" => [Terrain.Ice, Terrain.Tundra, Terrain.Shelf],
            "reach" => [Terrain.Tundra, Terrain.HotSprings, Terrain.Wastes],
            "shelf" => [Terrain.Shelf, Terrain.Reef, Terrain.Ocean, Terrain.Tundra],
            "sea" => [Terrain.Ocean, Terrain.Reef, Terrain.Shelf],
            "scar" => [Terrain.Wastes, Terrain.Crags, Terrain.Tundra],
            _ => [Terrain.Tundra, Terrain.Ice, Terrain.Shelf],
        };

    private static IReadOnlyList<Terrain> SettlementPhysicalPreferences(string? subtype, string? culture) =>
        subtype switch
        {
            "harbor" or "reefhold" => [Terrain.Shelf, Terrain.Reef, Terrain.Ocean],
            "holdfast" => [Terrain.Shelf, Terrain.Reef, Terrain.Ocean, Terrain.Crags, Terrain.Tundra],
            "sanctuary" => [Terrain.HotSprings, Terrain.Tundra, Terrain.Ice],
            _ => culture == "nightshelf"
                ? [Terrain.Ice, Terrain.Shelf, Terrain.Tundra]
                : [Terrain.Tundra, Terrain.Shelf, Terrain.Ice],
        };

    private static IReadOnlyList<Terrain> FactionPhysicalPreferences(string? subtype, string? culture)
    {
        if (subtype == "choir" || culture == "wake_singers")
        {
            return [Terrain.Ocean, Terrain.Reef, Terrain.Shelf];
        }

        if (subtype == "order" || culture == "nightshelf")
        {
            return [Terrain.Shelf, Terrain.Ice, Terrain.Tundra];
        }

        return [Terrain.Tundra, Terrain.Ice, Terrain.Shelf];
    }

    private static IReadOnlyList<Terrain> PointOfInterestPhysicalPreferences(string? subtype) =>
        subtype switch
        {
            "vent" => [Terrain.HotSprings, Terrain.Crags, Terrain.Wastes],
            "shoal" => [Terrain.Reef, Terrain.Shelf, Terrain.Ocean],
            "bloom" => [Terrain.Ocean, Terrain.Reef, Terrain.Wastes],
            _ => [Terrain.Tundra, Terrain.Crags, Terrain.Ice],
        };

    private static IReadOnlyList<Terrain> GeneralSemanticPreferences(EntityKind kind)
    {
        if (kind == K.Region)
        {
            return [Terrain.OpenField, Terrain.Lineage, Terrain.Witness];
        }

        if (kind == K.Monster)
        {
            return [Terrain.Silence, Terrain.Severance, Terrain.CavePocket];
        }

        if (kind == K.Transport)
        {
            return [Terrain.Breakaway, Terrain.OpenField, Terrain.Witness];
        }

        if (kind == K.Occurrence)
        {
            return [Terrain.Witness, Terrain.Severance, Terrain.OpenField];
        }

        if (kind == K.Orca)
        {
            return [Terrain.Breakaway, Terrain.Lineage, Terrain.OpenField];
        }

        return [Terrain.OpenField];
    }

    private static IReadOnlyList<Terrain> GeneralPhysicalPreferences(EntityKind kind)
    {
        if (kind == K.Orca || kind == K.Transport)
        {
            return [Terrain.Ocean, Terrain.Reef, Terrain.Shelf];
        }

        if (kind == K.Artifact)
        {
            return [Terrain.Crags, Terrain.Wastes, Terrain.Tundra];
        }

        if (kind == K.Monster)
        {
            return [Terrain.Wastes, Terrain.Crags, Terrain.Ice];
        }

        return [Terrain.Tundra, Terrain.Ice, Terrain.Shelf];
    }

    private static HashSet<string> PhysicalAvoidedTerrains(EntityKind kind, string? subtype, string? culture)
    {
        if (PhysicallyAquaticOrMaritime(kind, subtype, culture))
        {
            return TerrainSet(Terrain.Wastes, Terrain.Crags);
        }

        return TerrainSet(Terrain.Ocean, Terrain.Reef);
    }

    private static bool PhysicallyAquaticOrMaritime(EntityKind kind, string? subtype, string? culture)
    {
        if (AlwaysUsesMaritimePhysicalTerrain(kind))
        {
            return true;
        }

        if (RegionUsesMaritimePhysicalTerrain(kind, subtype))
        {
            return true;
        }

        if (SettlementUsesMaritimePhysicalTerrain(kind, subtype))
        {
            return true;
        }

        if (FactionUsesMaritimePhysicalTerrain(kind, subtype, culture))
        {
            return true;
        }

        return PointOfInterestUsesMaritimePhysicalTerrain(kind, subtype);
    }

    private static bool AlwaysUsesMaritimePhysicalTerrain(EntityKind kind) =>
        kind == K.Orca || kind == K.Transport;

    private static bool RegionUsesMaritimePhysicalTerrain(EntityKind kind, string? subtype) =>
        kind == K.Region && subtype is ("sea" or "shelf");

    private static bool SettlementUsesMaritimePhysicalTerrain(EntityKind kind, string? subtype) =>
        kind == K.Settlement && subtype is ("harbor" or "reefhold" or "holdfast");

    private static bool FactionUsesMaritimePhysicalTerrain(EntityKind kind, string? subtype, string? culture) =>
        kind == K.Faction && (subtype == "choir" || culture == "wake_singers");

    private static bool PointOfInterestUsesMaritimePhysicalTerrain(EntityKind kind, string? subtype) =>
        kind == K.PointOfInterest && subtype is ("shoal" or "bloom");

    private static IReadOnlyList<Terrain> SettlementSemanticPreferences(string? subtype, string? culture)
    {
        if (subtype == "sanctuary")
        {
            return [Terrain.Witness, Terrain.BeaconSpire, Terrain.Lineage, Terrain.OpenField];
        }

        if (subtype is "harbor" or "reefhold")
        {
            return [Terrain.Breakaway, Terrain.Witness, Terrain.OpenField];
        }

        return culture switch
        {
            "aurora_stack" => [Terrain.Witness, Terrain.Lineage, Terrain.BeaconSpire, Terrain.OpenField],
            "nightshelf" => [Terrain.Silence, Terrain.CavePocket, Terrain.Yoke, Terrain.OpenField],
            _ => [Terrain.Lineage, Terrain.OpenField, Terrain.Witness],
        };
    }

    private static IReadOnlyList<Terrain> FactionSemanticPreferences(string? subtype, string? culture) =>
        subtype switch
        {
            "order" => culture == "nightshelf"
                ? [Terrain.Silence, Terrain.Yoke, Terrain.CavePocket]
                : [Terrain.Yoke, Terrain.Witness, Terrain.Lineage],
            "choir" => [Terrain.Lineage, Terrain.Witness, Terrain.OpenField],
            "cell" => [Terrain.CavePocket, Terrain.Silence, Terrain.Breakaway],
            "compact" => [Terrain.Yoke, Terrain.Lineage, Terrain.Witness],
            _ => [Terrain.Yoke, Terrain.Lineage, Terrain.OpenField],
        };

    private static IReadOnlyList<Terrain> PenguinSemanticPreferences(string? subtype) =>
        subtype switch
        {
            "witness" => [Terrain.Witness, Terrain.Lineage, Terrain.BeaconSpire],
            "broker" => [Terrain.Breakaway, Terrain.OpenField, Terrain.Witness],
            "warden" => [Terrain.Yoke, Terrain.Lineage, Terrain.TabooRidge],
            "exile" => [Terrain.Severance, Terrain.Breakaway, Terrain.Silence],
            _ => [Terrain.Lineage, Terrain.OpenField, Terrain.Witness],
        };

    private static IReadOnlyList<Terrain> ArtifactSemanticPreferences(string? subtype) =>
        subtype switch
        {
            "weapon" => [Terrain.Severance, Terrain.TabooRidge, Terrain.Yoke],
            "seal" => [Terrain.TabooRidge, Terrain.Yoke, Terrain.Silence],
            "instrument" => [Terrain.BeaconSpire, Terrain.Witness, Terrain.Breakaway],
            "relic" => [Terrain.Lineage, Terrain.Witness, Terrain.CavePocket],
            "tome" => [Terrain.Lineage, Terrain.Silence, Terrain.Witness],
            _ => [Terrain.Silence, Terrain.CavePocket, Terrain.OpenField],
        };

    private static IReadOnlyList<Terrain> PointOfInterestSemanticPreferences(string? subtype) =>
        subtype switch
        {
            "vent" => [Terrain.BeaconSpire, Terrain.Witness, Terrain.OpenField],
            "bloom" => [Terrain.Severance, Terrain.Silence, Terrain.TabooRidge],
            "shoal" => [Terrain.Breakaway, Terrain.OpenField, Terrain.Witness],
            _ => [Terrain.Witness, Terrain.OpenField, Terrain.Lineage],
        };

    private static IReadOnlyList<Terrain> AbilitySemanticPreferences(string? culture) =>
        culture switch
        {
            "aurora_stack" => [Terrain.Witness, Terrain.Lineage, Terrain.BeaconSpire],
            "nightshelf" => [Terrain.Silence, Terrain.CavePocket, Terrain.Yoke],
            "wake_singers" => [Terrain.Lineage, Terrain.Breakaway, Terrain.Witness],
            _ => [Terrain.Lineage, Terrain.Witness, Terrain.OpenField],
        };

    private static HashSet<string> TerrainSet(IEnumerable<Terrain> terrains)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var terrain in terrains)
        {
            result.Add(terrain.ToDomainName());
        }

        return result;
    }

    private static List<string> TerrainNames(IEnumerable<Terrain> terrains)
    {
        var result = new List<string>();
        foreach (var terrain in terrains)
        {
            result.Add(terrain.ToDomainName());
        }

        return result;
    }

    private static HashSet<string> TerrainSet(params Terrain[] terrains)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var terrain in terrains)
        {
            result.Add(terrain.ToDomainName());
        }

        return result;
    }
}
