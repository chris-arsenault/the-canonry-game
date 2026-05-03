namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class GeneratorProgramData : IKernelProgramData
{
    internal int[] GeneratorIds = [];
    internal int[] Cadences = [];
    internal int[] TargetKindIds = [];
    internal int[] TargetAllowWilderness = [];
    internal int[] PulseWeights = [];
    internal int[] MaxRuns = [];
    internal int[] TargetSubtypeStarts = [];
    internal int[] TargetSubtypeCounts = [];
    internal int[] TargetRequiredOutgoingStarts = [];
    internal int[] TargetRequiredOutgoingCounts = [];
    internal int[] TargetRequiredIncomingStarts = [];
    internal int[] TargetRequiredIncomingCounts = [];
    internal int[] TargetRequiredAnyStarts = [];
    internal int[] TargetRequiredAnyCounts = [];
    internal int[] TargetWithoutOutgoingStarts = [];
    internal int[] TargetWithoutOutgoingCounts = [];
    internal int[] TargetWithoutIncomingStarts = [];
    internal int[] TargetWithoutIncomingCounts = [];
    internal int[] TargetTerrainRequirementStarts = [];
    internal int[] TargetTerrainRequirementCounts = [];
    internal int[] TriggerStarts = [];
    internal int[] TriggerCounts = [];
    internal int[] BindingStarts = [];
    internal int[] BindingCounts = [];
    internal int[] ProducedStarts = [];
    internal int[] ProducedCounts = [];
    internal int[] RelationshipStarts = [];
    internal int[] RelationshipCounts = [];
    internal int[] EffectStarts = [];
    internal int[] EffectCounts = [];
    internal int[] SaturationRelationshipKindIds = [];
    internal int[] SaturationFactIds = [];
    internal int[] SaturationDirectionCodes = [];
    internal int[] SaturationMaxExisting = [];
    internal int[] SubtypeIds = [];
    internal int[] RelationshipKindFilterIds = [];
    internal float[] RelationshipKindFilterMinimumStrengths = [];
    internal int[] TerrainFilterIds = [];
    internal int[] TargetTerrainRequirementTerrainIds = [];
    internal int[] TargetTerrainRequirementRadii = [];
    internal int[] TargetTerrainRequirementMaximumExclusive = [];
    internal int[] TriggerKindIds = [];
    internal int[] BindingRefCodes = [];
    internal int[] BindingKindIds = [];
    internal int[] BindingAdjacentToTarget = [];
    internal int[] BindingCultureMatchCodes = [];
    internal int[] BindingSubtypeStarts = [];
    internal int[] BindingSubtypeCounts = [];
    internal int[] BindingWithoutOutgoingStarts = [];
    internal int[] BindingWithoutOutgoingCounts = [];
    internal int[] BindingWithoutIncomingStarts = [];
    internal int[] BindingWithoutIncomingCounts = [];
    internal int[] ProducedRefCodes = [];
    internal int[] ProducedKindIds = [];
    internal int[] ProducedSubtypeIds = [];
    internal int[] ProducedCreateIfMissing = [];
    internal int[] ProducedHasQuery = [];
    internal int[] ProducedPhysicalPlacementCodes = [];
    internal int[] ProducedSemanticPlacementCodes = [];
    internal int[] ProducedPlacementRefCodes = [];
    internal int[] ProducedCultureRefCodes = [];
    internal float[] ProducedCreateChances = [];
    internal int[] ProducedSubtypeChoiceStarts = [];
    internal int[] ProducedSubtypeChoiceCounts = [];
    internal int[] ProducedQuerySubtypeStarts = [];
    internal int[] ProducedQuerySubtypeCounts = [];
    internal int[] ProducedQueryAdjacentToTarget = [];
    internal int[] ProducedQueryCultureMatchCodes = [];
    internal int[] ProducedQueryWithoutOutgoingStarts = [];
    internal int[] ProducedQueryWithoutOutgoingCounts = [];
    internal int[] ProducedQueryWithoutIncomingStarts = [];
    internal int[] ProducedQueryWithoutIncomingCounts = [];
    internal int[] ProducedCoverageModeCodes = [];
    internal int[] ProducedCoverageSourceRefCodes = [];
    internal int[] ProducedCoverageDesiredCellCounts = [];
    internal int[] ProducedCoverageMinimumCellCounts = [];
    internal int[] ProducedCoverageMinimumSourceRemainingCellCounts = [];
    internal int[] ProducedCoverageRespectTerrainBoundaries = [];
    internal int[] ProducedCoverageTransformTerrainIds = [];
    internal int[] ProducedCoveragePreferredTerrainStarts = [];
    internal int[] ProducedCoveragePreferredTerrainCounts = [];
    internal int[] ProducedCoverageAllowedTerrainStarts = [];
    internal int[] ProducedCoverageAllowedTerrainCounts = [];
    internal int[] RelationshipSourceRefCodes = [];
    internal int[] RelationshipDestinationRefCodes = [];
    internal int[] RelationshipKindIds = [];
    internal int[] RelationshipReplaceExistingOutgoing = [];
    internal int[] RelationshipFactId0 = [];
    internal int[] RelationshipFactId1 = [];
    internal int[] RelationshipFactId2 = [];
    internal int[] RelationshipFactId3 = [];
    internal int[] RelationshipFactCounts = [];
    internal float[] RelationshipStrengths = [];
    internal int[] EffectTypeCodes = [];
    internal int[] EffectRefOrKindIds = [];
    internal int[] EffectInstanceSelectionCodes = [];
    internal int[] EffectOptionCodes = [];
    internal float[] EffectValues = [];
    internal int SemanticMinQ = -24;
    internal int SemanticMaxQ = 24;
    internal int SemanticMinR = -24;
    internal int SemanticMaxR = 24;
    internal int GeneratorCount;
    internal int SubtypeCount;
    internal int RelationshipKindFilterCount;
    internal int TerrainFilterCount;
    internal int TargetTerrainRequirementCount;
    internal int TriggerCount;
    internal int BindingCount;
    internal int ProducedCount;
    internal int RelationshipCount;
    internal int EffectCount;
    internal int OpenGeneratorSlot;
    internal int OpenBindingSlot;
    internal int OpenProducedSlot;

    public GeneratorProgramData(int generatorCapacity, int itemCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(generatorCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(itemCapacity);
        InitializeGeneratorArrays(generatorCapacity);
        InitializeSharedItemArrays(itemCapacity);
        InitializeTriggerArrays(itemCapacity);
        InitializeBindingArrays(itemCapacity);
        InitializeProducedArrays(itemCapacity);
        InitializeRelationshipArrays(itemCapacity);
        InitializeEffectArrays(itemCapacity);
    }

    private void InitializeGeneratorArrays(int generatorCapacity)
    {
        GeneratorIds = new int[generatorCapacity + 1];
        Cadences = new int[generatorCapacity + 1];
        TargetKindIds = new int[generatorCapacity + 1];
        TargetAllowWilderness = new int[generatorCapacity + 1];
        PulseWeights = new int[generatorCapacity + 1];
        MaxRuns = new int[generatorCapacity + 1];
        TargetSubtypeStarts = new int[generatorCapacity + 1];
        TargetSubtypeCounts = new int[generatorCapacity + 1];
        TargetRequiredOutgoingStarts = new int[generatorCapacity + 1];
        TargetRequiredOutgoingCounts = new int[generatorCapacity + 1];
        TargetRequiredIncomingStarts = new int[generatorCapacity + 1];
        TargetRequiredIncomingCounts = new int[generatorCapacity + 1];
        TargetRequiredAnyStarts = new int[generatorCapacity + 1];
        TargetRequiredAnyCounts = new int[generatorCapacity + 1];
        TargetWithoutOutgoingStarts = new int[generatorCapacity + 1];
        TargetWithoutOutgoingCounts = new int[generatorCapacity + 1];
        TargetWithoutIncomingStarts = new int[generatorCapacity + 1];
        TargetWithoutIncomingCounts = new int[generatorCapacity + 1];
        TargetTerrainRequirementStarts = new int[generatorCapacity + 1];
        TargetTerrainRequirementCounts = new int[generatorCapacity + 1];
        TriggerStarts = new int[generatorCapacity + 1];
        TriggerCounts = new int[generatorCapacity + 1];
        BindingStarts = new int[generatorCapacity + 1];
        BindingCounts = new int[generatorCapacity + 1];
        ProducedStarts = new int[generatorCapacity + 1];
        ProducedCounts = new int[generatorCapacity + 1];
        RelationshipStarts = new int[generatorCapacity + 1];
        RelationshipCounts = new int[generatorCapacity + 1];
        EffectStarts = new int[generatorCapacity + 1];
        EffectCounts = new int[generatorCapacity + 1];
        SaturationRelationshipKindIds = new int[generatorCapacity + 1];
        SaturationFactIds = new int[generatorCapacity + 1];
        SaturationDirectionCodes = new int[generatorCapacity + 1];
        SaturationMaxExisting = new int[generatorCapacity + 1];
    }

    private void InitializeEffectArrays(int itemCapacity)
    {
        EffectTypeCodes = new int[itemCapacity + 1];
        EffectRefOrKindIds = new int[itemCapacity + 1];
        EffectInstanceSelectionCodes = new int[itemCapacity + 1];
        EffectOptionCodes = new int[itemCapacity + 1];
        EffectValues = new float[itemCapacity + 1];
    }

    private void InitializeSharedItemArrays(int itemCapacity)
    {
        SubtypeIds = new int[itemCapacity + 1];
        RelationshipKindFilterIds = new int[itemCapacity + 1];
        RelationshipKindFilterMinimumStrengths = new float[itemCapacity + 1];
        TerrainFilterIds = new int[itemCapacity + 1];
        TargetTerrainRequirementTerrainIds = new int[itemCapacity + 1];
        TargetTerrainRequirementRadii = new int[itemCapacity + 1];
        TargetTerrainRequirementMaximumExclusive = new int[itemCapacity + 1];
    }

    private void InitializeTriggerArrays(int itemCapacity)
    {
        TriggerKindIds = new int[itemCapacity + 1];
    }

    private void InitializeBindingArrays(int itemCapacity)
    {
        BindingRefCodes = new int[itemCapacity + 1];
        BindingKindIds = new int[itemCapacity + 1];
        BindingAdjacentToTarget = new int[itemCapacity + 1];
        BindingCultureMatchCodes = new int[itemCapacity + 1];
        BindingSubtypeStarts = new int[itemCapacity + 1];
        BindingSubtypeCounts = new int[itemCapacity + 1];
        BindingWithoutOutgoingStarts = new int[itemCapacity + 1];
        BindingWithoutOutgoingCounts = new int[itemCapacity + 1];
        BindingWithoutIncomingStarts = new int[itemCapacity + 1];
        BindingWithoutIncomingCounts = new int[itemCapacity + 1];
    }

    private void InitializeProducedArrays(int itemCapacity)
    {
        ProducedRefCodes = new int[itemCapacity + 1];
        ProducedKindIds = new int[itemCapacity + 1];
        ProducedSubtypeIds = new int[itemCapacity + 1];
        ProducedCreateIfMissing = new int[itemCapacity + 1];
        ProducedHasQuery = new int[itemCapacity + 1];
        ProducedPhysicalPlacementCodes = new int[itemCapacity + 1];
        ProducedSemanticPlacementCodes = new int[itemCapacity + 1];
        ProducedPlacementRefCodes = new int[itemCapacity + 1];
        ProducedCultureRefCodes = new int[itemCapacity + 1];
        ProducedCreateChances = new float[itemCapacity + 1];
        ProducedSubtypeChoiceStarts = new int[itemCapacity + 1];
        ProducedSubtypeChoiceCounts = new int[itemCapacity + 1];
        ProducedQuerySubtypeStarts = new int[itemCapacity + 1];
        ProducedQuerySubtypeCounts = new int[itemCapacity + 1];
        ProducedQueryAdjacentToTarget = new int[itemCapacity + 1];
        ProducedQueryCultureMatchCodes = new int[itemCapacity + 1];
        ProducedQueryWithoutOutgoingStarts = new int[itemCapacity + 1];
        ProducedQueryWithoutOutgoingCounts = new int[itemCapacity + 1];
        ProducedQueryWithoutIncomingStarts = new int[itemCapacity + 1];
        ProducedQueryWithoutIncomingCounts = new int[itemCapacity + 1];
        ProducedCoverageModeCodes = new int[itemCapacity + 1];
        ProducedCoverageSourceRefCodes = new int[itemCapacity + 1];
        ProducedCoverageDesiredCellCounts = new int[itemCapacity + 1];
        ProducedCoverageMinimumCellCounts = new int[itemCapacity + 1];
        ProducedCoverageMinimumSourceRemainingCellCounts = new int[itemCapacity + 1];
        ProducedCoverageRespectTerrainBoundaries = new int[itemCapacity + 1];
        ProducedCoverageTransformTerrainIds = new int[itemCapacity + 1];
        ProducedCoveragePreferredTerrainStarts = new int[itemCapacity + 1];
        ProducedCoveragePreferredTerrainCounts = new int[itemCapacity + 1];
        ProducedCoverageAllowedTerrainStarts = new int[itemCapacity + 1];
        ProducedCoverageAllowedTerrainCounts = new int[itemCapacity + 1];
    }

    private void InitializeRelationshipArrays(int itemCapacity)
    {
        RelationshipSourceRefCodes = new int[itemCapacity + 1];
        RelationshipDestinationRefCodes = new int[itemCapacity + 1];
        RelationshipKindIds = new int[itemCapacity + 1];
        RelationshipReplaceExistingOutgoing = new int[itemCapacity + 1];
        RelationshipFactId0 = new int[itemCapacity + 1];
        RelationshipFactId1 = new int[itemCapacity + 1];
        RelationshipFactId2 = new int[itemCapacity + 1];
        RelationshipFactId3 = new int[itemCapacity + 1];
        RelationshipFactCounts = new int[itemCapacity + 1];
        RelationshipStrengths = new float[itemCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Generators;

    public int SchemaVersion => 7;

    public void ConfigureSemanticBounds(int minQ, int maxQ, int minR, int maxR)
    {
        if (minQ > maxQ || minR > maxR)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(minQ));
        }

        SemanticMinQ = minQ;
        SemanticMaxQ = maxQ;
        SemanticMinR = minR;
        SemanticMaxR = maxR;
    }
}
