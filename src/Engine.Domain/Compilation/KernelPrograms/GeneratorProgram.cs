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
    int PulseWeight,
    int MaxRuns,
    IReadOnlyList<GeneratorTriggerRule> Triggers,
    IReadOnlyList<GeneratorBindingRule> SecondaryBindings,
    IReadOnlyList<GeneratorProducedEntityRule> ProducedEntities,
    IReadOnlyList<GeneratorRelationshipRule> Relationships,
    IReadOnlyList<GeneratorEffectRule> Effects);

public sealed record GeneratorEffectRule(
    int EffectTypeCode,
    int RefOrKindId,
    int InstanceSelectionCode = GeneratorRelationshipInstanceSelectionCodes.AllTargetRelationships,
    int OptionCode = 0,
    float Value = 0f,
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

public sealed record GeneratorRelationshipRequirementRule(int RelationshipKindId, float MinimumStrength);

public sealed record GeneratorTerrainCountRequirementRule(int TerrainId, int Radius, int MaximumExclusive);

public sealed record GeneratorTriggerRule(int KindId);

public sealed record GeneratorBindingRule(
    int RefCode,
    int KindId,
    GeneratorEntityQueryRule? Query);

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
    GeneratorProgramCulturePlacement CulturePlacement,
    float CreateChance,
    GeneratorCoverageRule? Coverage);

public sealed record GeneratorEntityQueryRule(
    IReadOnlyList<int> SubtypeIds,
    bool AdjacentToTarget,
    GeneratorProgramCultureMatch CultureMatch,
    IReadOnlyList<int> WithoutOutgoingRelationshipKindIds,
    IReadOnlyList<int> WithoutIncomingRelationshipKindIds);

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
    float Strength,
    bool ReplaceExistingOutgoing);

public enum GeneratorProgramPlacement
{
    Area,
    Locality,
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
}

public enum GeneratorProgramCoverageMode
{
    None = 0,
    CreateNew = 1,
    SplitFromExisting = 2,
}
