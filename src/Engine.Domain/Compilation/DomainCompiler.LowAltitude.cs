namespace Engine.Domain.Compilation;

using Engine.Constants;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;

public static partial class DomainCompiler
{
    private static MovementProgram BuildMovementProgram(DomainConfig config, DomainIdRegistry ids)
    {
        var rules = new List<MovementRule>();
        foreach (var (kind, kindConfig) in config.Kinds)
        {
            var planeCode = kindConfig.PrimaryPlane == PrimaryPlane.Physical
                ? PlaneCodes.Physical
                : PlaneCodes.Semantic;
            var moveChance = planeCode == PlaneCodes.Physical ? 0.45f : 0.20f;
            rules.Add(new MovementRule(
                ids.KindIdOf(kind).Value,
                planeCode,
                moveChance,
                ExploreWeight: 2.0f,
                StalenessWeight: 1.0f,
                CrowdingWeight: 0.75f));
        }

        return new MovementProgram { EnabledByDefault = false, Rules = rules };
    }

    private static LowAltitudeVerbProgram BuildLowAltitudeVerbProgram(DomainConfig config, DomainIdRegistry ids)
    {
        var rules = new List<LowAltitudeVerbRule>();
        foreach (var verb in config.LowAltitudeVerbs)
        {
            var kindId = ids.KindIdOf(verb.Kind).Value;
            var ownerPlane = verb.Plane ?? config.Kinds[verb.Kind].PrimaryPlane;
            rules.Add(new LowAltitudeVerbRule(
                kindId,
                ids.VerbIdOf(verb.Id).Value,
                LowAltitudePlaneCode(ownerPlane),
                OperationCode(verb.Operation),
                TargetModeCode(verb.TargetMode),
                verb.TargetKind is null ? 0 : ids.KindIdOf(verb.TargetKind.Value).Value,
                verb.RelationshipKind is null ? 0 : ids.RelationshipKindIdOf(verb.RelationshipKind).Value,
                RelationshipDirectionCode(verb.RelationshipDirection),
                CompileTerrainIds(verb.Terrains, ids),
                verb.MinCommitmentTicks,
                verb.MaxCommitmentTicks,
                verb.DwellTicks,
                verb.TargetRadius,
                verb.CandidateSampleCount,
                verb.Weight,
                verb.ExploreWeight,
                verb.StalenessWeight,
                verb.HotspotWeight,
                verb.CrowdingWeight,
                verb.GeneratorAffordanceWeight,
                verb.AffinityRelationshipKind is null ? 0 : ids.RelationshipKindIdOf(verb.AffinityRelationshipKind).Value,
                RelationshipDirectionCode(verb.AffinityRelationshipDirection),
                verb.RelationshipAffinityWeight,
                verb.InfluenceDelta,
                verb.InfluenceRadius,
                verb.CoverageGrowthChance,
                verb.HeadquartersMoveChance,
                verb.RequiredInfluence));
        }

        return new LowAltitudeVerbProgram { Verbs = rules };
    }

    private static MapActivityProgram BuildMapActivityProgram() => new();

    private static MapSignalsProgram BuildMapSignalsProgram(DomainConfig config, DomainIdRegistry ids)
    {
        return new MapSignalsProgram
        {
            SemanticBaseCapacity = config.MapSignals.SemanticBaseCapacity,
            CrowdingRadius = config.MapSignals.CrowdingRadius,
            Terrains = CompileTerrainMapSignals(config, ids),
            Kinds = CompileKindMapSignals(config, ids),
            Relationships = CompileRelationshipMapSignals(config, ids),
        };
    }

    private static List<TerrainMapSignalRule> CompileTerrainMapSignals(DomainConfig config, DomainIdRegistry ids)
    {
        var terrainRules = new List<TerrainMapSignalRule>();
        foreach (var terrain in config.MapSignals.Terrains)
        {
            if (!config.Terrains.Any(candidate => string.Equals(candidate.Name, terrain.Terrain, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    $"Map signal terrain references unknown terrain '{terrain.Terrain}'.");
            }

            terrainRules.Add(new TerrainMapSignalRule(
                ids.TerrainIdOf(terrain.Terrain).Value,
                terrain.BaseCapacity));
        }

        return terrainRules;
    }

    private static List<KindMapSignalRule> CompileKindMapSignals(DomainConfig config, DomainIdRegistry ids)
    {
        var kindRules = new List<KindMapSignalRule>();
        foreach (var kind in config.MapSignals.Kinds)
        {
            EnsureKind(config, kind.Kind, "Map signal kind");
            var healthAttribute = kind.HealthAttribute ?? config.Kinds[kind.Kind].HealthAttribute;
            var healthAttributeId = 0;
            if (!string.IsNullOrWhiteSpace(healthAttribute))
            {
                EnsureKindAttribute(config, kind.Kind, healthAttribute, "Map signal health attribute");
                healthAttributeId = ids.AttributeIdOf(healthAttribute).Value;
            }

            kindRules.Add(new KindMapSignalRule(
                ids.KindIdOf(kind.Kind).Value,
                healthAttributeId,
                kind.SignalRadius,
                kind.CapacityEmission,
                kind.SupportEmission,
                kind.ThreatEmission,
                kind.BaseAttractorMass,
                kind.CoverageAttractorScale,
                kind.InfluenceAttractorScale,
                kind.ActivityAttractorScale,
                kind.HealthAttractorScale,
                kind.AttractorRadius));
        }

        return kindRules;
    }

    private static List<RelationshipMapSignalRule> CompileRelationshipMapSignals(DomainConfig config, DomainIdRegistry ids)
    {
        var relationshipRules = new List<RelationshipMapSignalRule>();
        foreach (var relationship in config.MapSignals.Relationships)
        {
            if (!config.Relationships.Any(candidate =>
                    string.Equals(candidate.Name, relationship.RelationshipKind, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    $"Map signal relationship references unknown relationship '{relationship.RelationshipKind}'.");
            }

            relationshipRules.Add(new RelationshipMapSignalRule(
                ids.RelationshipKindIdOf(relationship.RelationshipKind).Value,
                relationship.SignalRadius,
                relationship.SupportEmission,
                relationship.ThreatEmission));
        }

        return relationshipRules;
    }

    private static List<int> CompileTerrainIds(IReadOnlySet<string>? terrains, DomainIdRegistry ids)
    {
        if (terrains is null || terrains.Count == 0)
        {
            return [];
        }

        var terrainIds = new List<int>(terrains.Count);
        foreach (var terrain in terrains)
        {
            terrainIds.Add(ids.TerrainIdOf(terrain).Value);
        }

        return terrainIds;
    }

    private static int LowAltitudePlaneCode(PrimaryPlane plane) => plane == PrimaryPlane.Physical
        ? PlaneCodes.Physical
        : PlaneCodes.Semantic;

    private static int OperationCode(LowAltitudeVerbOperation operation) => operation switch
    {
        LowAltitudeVerbOperation.Move => LowAltitudeVerbOperationCodes.Move,
        LowAltitudeVerbOperation.GrowInfluence => LowAltitudeVerbOperationCodes.GrowInfluence,
        LowAltitudeVerbOperation.ConsolidateInfluence => LowAltitudeVerbOperationCodes.ConsolidateInfluence,
        LowAltitudeVerbOperation.ReduceInfluence => LowAltitudeVerbOperationCodes.ReduceInfluence,
        LowAltitudeVerbOperation.ExpandCoverage => LowAltitudeVerbOperationCodes.ExpandCoverage,
        LowAltitudeVerbOperation.MoveHeadquarters => LowAltitudeVerbOperationCodes.MoveHeadquarters,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
    };

    private static int TargetModeCode(LowAltitudeVerbTargetMode mode) => mode switch
    {
        LowAltitudeVerbTargetMode.Explore => LowAltitudeVerbTargetModeCodes.Explore,
        LowAltitudeVerbTargetMode.EntityKind => LowAltitudeVerbTargetModeCodes.EntityKind,
        LowAltitudeVerbTargetMode.RelationshipTarget => LowAltitudeVerbTargetModeCodes.RelationshipTarget,
        LowAltitudeVerbTargetMode.Terrain => LowAltitudeVerbTargetModeCodes.Terrain,
        LowAltitudeVerbTargetMode.OwnInfluence => LowAltitudeVerbTargetModeCodes.OwnInfluence,
        LowAltitudeVerbTargetMode.ForeignInfluence => LowAltitudeVerbTargetModeCodes.ForeignInfluence,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    private static int RelationshipDirectionCode(LowAltitudeVerbRelationshipDirection direction) => direction switch
    {
        LowAltitudeVerbRelationshipDirection.Outgoing => LowAltitudeVerbRelationshipDirectionCodes.Outgoing,
        LowAltitudeVerbRelationshipDirection.Incoming => LowAltitudeVerbRelationshipDirectionCodes.Incoming,
        LowAltitudeVerbRelationshipDirection.Either => LowAltitudeVerbRelationshipDirectionCodes.Either,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };
}
