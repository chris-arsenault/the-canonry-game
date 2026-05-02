namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class GeneratorProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<CompiledGeneratorRule> Generators { get; init; } = [];
}

public sealed record CompiledGeneratorRule(
    int GeneratorId,
    int CadenceTicks,
    int TargetKindId,
    IReadOnlyList<int> TargetSubtypeIds,
    bool TargetAllowWilderness,
    GeneratorTargetRelationshipRequirements TargetRelationshipRequirements,
    int PulseWeight,
    int MaxRuns,
    GeneratorTargetSaturationRule? TargetSaturation,
    IReadOnlyList<GeneratorTriggerRule> Triggers,
    IReadOnlyList<GeneratorBindingRule> SecondaryBindings,
    IReadOnlyList<GeneratorProducedEntityRule> ProducedEntities,
    IReadOnlyList<GeneratorRelationshipRule> Relationships,
    IReadOnlyList<GeneratorEffectRule> Effects);

public sealed record GeneratorEffectRule(
    int EffectTypeCode,
    int RefOrKindId);

public sealed record GeneratorTargetSaturationRule(
    int RelationshipKindId,
    int RequiredFactId,
    GeneratorProgramSaturationDirection Direction,
    int MaxExisting);

public sealed record GeneratorTargetRelationshipRequirements(
    IReadOnlyList<int> RequiredOutgoingRelationshipKindIds,
    IReadOnlyList<int> RequiredIncomingRelationshipKindIds,
    IReadOnlyList<int> RequiredRelationshipKindIds);

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
    GeneratorProgramPlacement SemanticPlacement,
    int PlacementRefCode,
    int CultureRefCode,
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
    bool ReplaceExistingOutgoing,
    IReadOnlyList<int> FactIds);

public enum GeneratorProgramSaturationDirection
{
    OutgoingFromTarget,
    IncomingToTarget,
    Symmetric,
}

public enum GeneratorProgramPlacement
{
    On,
    Near,
    Across,
    InCoverageLocality,
}

public enum GeneratorProgramCultureMatch
{
    Any,
    SameAsTarget,
    DifferentFromTarget,
}

public enum GeneratorProgramCoverageMode
{
    None = 0,
    CreateNew = 1,
    SplitFromExisting = 2,
}
