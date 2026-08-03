namespace Engine.Domain.Compilation.KernelPrograms;

using Engine.Constants;

public sealed class GeneratorProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<CompiledGeneratorRule> Generators { get; init; } = [];
}

public sealed record CompiledGeneratorRule(
    int GeneratorId,
    int TargetKindId,
    IReadOnlyList<int> TargetSubtypeIds,
    GeneratorTargetRelationshipRequirements TargetRelationshipRequirements,
    int FireWeight,
    int MaxRuns,
    FixedScalar DesperationRatio,
    int DesperationCultureId,
    int TargetRequiredTagBits,
    int TargetWithoutTagBits,
    FixedScalar TargetChronicleHeatMin,
    int TargetChronicleLegendaryWithinTicks,
    IReadOnlyList<GeneratorTriggerRule> Triggers,
    IReadOnlyList<GeneratorBindingRule> SecondaryBindings,
    IReadOnlyList<GeneratorProducedEntityRule> ProducedEntities,
    IReadOnlyList<GeneratorRelationshipRule> Relationships,
    IReadOnlyList<GeneratorEffectRule> Effects,
    int TargetKindIdB = 0);

public sealed record GeneratorEffectRule(
    int EffectTypeCode,
    int RefOrKindId,
    int InstanceSelectionCode = GeneratorRelationshipInstanceSelectionCodes.AllTargetRelationships,
    int OptionCode = 0,
    FixedScalar Value = default,
    int Radius = 0,
    int MaxCellCount = 0,
    IReadOnlyList<int>? PreferredTerrainIds = null,
    IReadOnlyList<int>? AllowedTerrainIds = null,
    int PlaneCode = PlaneCodes.Physical);

public sealed record GeneratorTargetRelationshipRequirements(
    IReadOnlyList<GeneratorRelationshipRequirementRule> RequiredOutgoingRelationships,
    IReadOnlyList<GeneratorRelationshipRequirementRule> RequiredIncomingRelationships,
    IReadOnlyList<GeneratorRelationshipRequirementRule> RequiredRelationships,
    IReadOnlyList<GeneratorTerrainCountRequirementRule> TerrainCountRequirements,
    IReadOnlyList<int> WithoutOutgoingRelationshipKindIds,
    IReadOnlyList<int> WithoutIncomingRelationshipKindIds);

public sealed record GeneratorRelationshipRequirementRule(int RelationshipKindId, FixedScalar MinimumStrength);

public sealed record GeneratorTerrainCountRequirementRule(int TerrainId, int Radius, int MaximumExclusive);

public sealed record GeneratorTriggerRule(int KindId);

public sealed record GeneratorBindingRule(
    int RefCode,
    int KindId,
    GeneratorEntityQueryRule? Query,
    int KindIdB = 0);

public sealed record GeneratorProducedEntityRule(
    int RefCode,
    int KindId,
    int SubtypeId,
    IReadOnlyList<int> SubtypeChoiceIds,
    bool CreateIfMissing,
    bool HasQuery,
    GeneratorEntityQueryRule? Query,
    GeneratorProgramPlacement PhysicalPlacement,
    int PhysicalPlacementMinRadius,
    int PhysicalPlacementMaxRadius,
    IReadOnlyList<int> PhysicalPlacementPreferredTerrainIds,
    IReadOnlyList<int> PhysicalPlacementAllowedTerrainIds,
    IReadOnlyList<int> PhysicalPlacementAvoidedTerrainIds,
    int PhysicalPlacementFallbackTerrainId,
    GeneratorProgramPlacement SemanticPlacement,
    int SemanticPlacementMinRadius,
    int SemanticPlacementMaxRadius,
    IReadOnlyList<int> SemanticPlacementPreferredTerrainIds,
    IReadOnlyList<int> SemanticPlacementAllowedTerrainIds,
    IReadOnlyList<int> SemanticPlacementAvoidedTerrainIds,
    int SemanticPlacementFallbackTerrainId,
    int PlacementRefCode,
    int CultureRefCode,
    int FixedCultureId,
    GeneratorProgramCulturePlacement CulturePlacement,
    FixedScalar CreateChance,
    GeneratorCoverageRule? Coverage,
    int PhysicalPlacementSiteFlags = 0,
    FixedScalar PhysicalPlacementWarmthAffinity = default,
    FixedScalar PhysicalPlacementFireAffinity = default,
    FixedScalar PhysicalPlacementAuroraAffinity = default,
    FixedScalar PhysicalPlacementPressureAffinity = default,
    int PhysicalPlacementFeatureKind = -1,
    int CultureModeCode = 0,
    int OnlyWhenTargetKindId = 0);

public sealed record GeneratorEntityQueryRule(
    IReadOnlyList<int> SubtypeIds,
    bool AdjacentToTarget,
    bool RequireTargetCultureRoute,
    GeneratorProgramCultureMatch CultureMatch,
    IReadOnlyList<int> WithoutOutgoingRelationshipKindIds,
    IReadOnlyList<int> WithoutIncomingRelationshipKindIds,
    int RequiredTagBits = 0,
    int WithoutTagBits = 0,
    int CultureId = 0,
    int RelatedToTargetKindId = 0,
    bool RelatedToTargetIncoming = false,
    FixedScalar RelatedToTargetMinStrength = default);

public sealed record GeneratorCoverageRule(
    GeneratorProgramCoverageMode Mode,
    int SourceRefCode,
    int DesiredCellCount,
    int MinimumCellCount,
    int MinimumSourceRemainingCellCount,
    bool RespectTerrainBoundaries,
    int TransformTerrainId,
    IReadOnlyList<int> PreferredTerrainIds,
    IReadOnlyList<int> AllowedTerrainIds);

public sealed record GeneratorRelationshipRule(
    int SourceRefCode,
    int DestinationRefCode,
    int RelationshipKindId,
    FixedScalar Strength,
    bool ReplaceExistingOutgoing);

public enum GeneratorProgramPlacement
{
    Area,
    Locality,
    Frontier,
    FeatureAnchor,
}

public enum GeneratorProgramCultureMatch
{
    Any,
    SameAsTarget,
    DifferentFromTarget,
}

public enum GeneratorProgramCulturePlacement
{
    Ignore,
    PreferInheritedCulture,
    HostileIntrusion,
    CultureInterior,
    CultureFrontier,
    CultureRoute,
    Wilderness,
}

public enum GeneratorProgramCoverageMode
{
    None = 0,
    CreateNew = 1,
    SplitFromExisting = 2,
}
