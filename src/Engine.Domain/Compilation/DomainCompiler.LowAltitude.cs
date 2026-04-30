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
