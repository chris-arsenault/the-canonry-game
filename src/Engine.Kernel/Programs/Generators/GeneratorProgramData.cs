namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class GeneratorProgramData : IKernelProgramData
{
    internal int[] GeneratorIds = [];
    internal int[] TargetKindIds = [];
    internal int[] FireWeights = [];
    internal FixedScalar[] AgeFireWeightMultipliers = [];
    internal int[] CadenceTicks = [];
    internal int[] CadenceJitterTicks = [];
    internal int[] TargetRequiredTagBits = [];
    internal int[] TargetWithoutTagBits = [];
    internal FixedScalar[] TargetChronicleHeatMin = [];
    internal int[] TargetChronicleLegendaryWithinTicks = [];
    internal int[] BindingRequiredTagBits = [];
    internal int[] BindingWithoutTagBits = [];
    internal int[] BindingCultureIds = [];
    internal int[] BindingRelatedToTargetKindIds = [];
    internal int[] BindingRelatedToTargetIncoming = [];
    internal FixedScalar[] BindingRelatedToTargetMinStrengths = [];
    internal int[] ProducedQueryRequiredTagBits = [];
    internal int[] ProducedQueryWithoutTagBits = [];
    internal int[] ProducedQueryCultureIds = [];
    internal int[] ProducedQueryRelatedToTargetKindIds = [];
    internal int[] ProducedQueryRelatedToTargetIncoming = [];
    internal FixedScalar[] ProducedQueryRelatedToTargetMinStrengths = [];
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
    internal int[] SubtypeIds = [];
    internal int[] RelationshipKindFilterIds = [];
    internal FixedScalar[] RelationshipKindFilterMinimumStrengths = [];
    internal int[] RelationshipKindFilterAllowsSlotCausalLimitMultiplier = [];
    internal int[] TerrainFilterIds = [];
    internal int[] TargetTerrainRequirementTerrainIds = [];
    internal int[] TargetTerrainRequirementRadii = [];
    internal int[] TargetTerrainRequirementMaximumExclusive = [];
    internal int[] TriggerKindIds = [];
    internal int[] BindingRefCodes = [];
    internal int[] BindingKindIds = [];
    internal int[] BindingAdjacentToTarget = [];
    internal int[] BindingCultureMatchCodes = [];
    internal int[] BindingRequireTargetCultureRoute = [];
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
    internal int[] ProducedPhysicalPlacementMinRadii = [];
    internal int[] ProducedPhysicalPlacementMaxRadii = [];
    internal int[] ProducedSemanticPlacementMinRadii = [];
    internal int[] ProducedSemanticPlacementMaxRadii = [];
    internal int[] ProducedPlacementPreferredTerrainStarts = [];
    internal int[] ProducedPlacementPreferredTerrainCounts = [];
    internal int[] ProducedPlacementAllowedTerrainStarts = [];
    internal int[] ProducedPlacementAllowedTerrainCounts = [];
    internal int[] ProducedPlacementAvoidedTerrainStarts = [];
    internal int[] ProducedPlacementAvoidedTerrainCounts = [];
    internal int[] ProducedPlacementFallbackTerrainIds = [];
    internal int[] ProducedPlacementSiteFlags = [];
    internal FixedScalar[] ProducedPlacementBandAffinities = [];
    internal int[] ProducedPlacementRefCodes = [];
    internal int[] ProducedCultureRefCodes = [];
    internal int[] ProducedFixedCultureIds = [];
    internal int[] ProducedCulturePlacementCodes = [];
    internal int[] ProducedFeatureKinds = [];

    private void AllocateProducedPlacementArrays(int itemCapacity)
    {
        ProducedCulturePlacementCodes = new int[itemCapacity + 1];
        ProducedFeatureKinds = new int[itemCapacity + 1];
        global::System.Array.Fill(ProducedFeatureKinds, PhysicalGeologicalFeatureKindCodes.Any);
    }
    internal FixedScalar[] ProducedCreateChances = [];
    internal int[] ProducedSubtypeChoiceStarts = [];
    internal int[] ProducedSubtypeChoiceCounts = [];
    internal int[] ProducedQuerySubtypeStarts = [];
    internal int[] ProducedQuerySubtypeCounts = [];
    internal int[] ProducedQueryAdjacentToTarget = [];
    internal int[] ProducedQueryCultureMatchCodes = [];
    internal int[] ProducedQueryRequireTargetCultureRoute = [];
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
    internal FixedScalar[] RelationshipStrengths = [];
    internal int[] EffectTypeCodes = [];
    internal int[] EffectRefOrKindIds = [];
    internal int[] EffectInstanceSelectionCodes = [];
    internal int[] EffectOptionCodes = [];
    internal int[] EffectPlaneCodes = [];
    internal int[] EffectRadii = [];
    internal int[] EffectMaxCellCounts = [];
    internal int[] EffectPreferredTerrainStarts = [];
    internal int[] EffectPreferredTerrainCounts = [];
    internal int[] EffectAllowedTerrainStarts = [];
    internal int[] EffectAllowedTerrainCounts = [];
    internal FixedScalar[] EffectValues = [];
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
    internal int OpenEffectSlot;

    public GeneratorProgramData(int generatorCapacity, int itemCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(generatorCapacity);
        NumericGuard.ThrowIfNegativeOrZero(itemCapacity);
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
        TargetKindIds = new int[generatorCapacity + 1];
        FireWeights = new int[generatorCapacity + 1];
        AgeFireWeightMultipliers = new FixedScalar[generatorCapacity + 1];
        global::System.Array.Fill(AgeFireWeightMultipliers, 1);
        CadenceTicks = new int[generatorCapacity + 1];
        CadenceJitterTicks = new int[generatorCapacity + 1];
        TargetRequiredTagBits = new int[generatorCapacity + 1];
        TargetWithoutTagBits = new int[generatorCapacity + 1];
        TargetChronicleHeatMin = new FixedScalar[generatorCapacity + 1];
        TargetChronicleLegendaryWithinTicks = new int[generatorCapacity + 1];
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
    }

    private void InitializeEffectArrays(int itemCapacity)
    {
        EffectTypeCodes = new int[itemCapacity + 1];
        EffectRefOrKindIds = new int[itemCapacity + 1];
        EffectInstanceSelectionCodes = new int[itemCapacity + 1];
        EffectOptionCodes = new int[itemCapacity + 1];
        EffectPlaneCodes = new int[itemCapacity + 1];
        EffectRadii = new int[itemCapacity + 1];
        EffectMaxCellCounts = new int[itemCapacity + 1];
        EffectPreferredTerrainStarts = new int[itemCapacity + 1];
        EffectPreferredTerrainCounts = new int[itemCapacity + 1];
        EffectAllowedTerrainStarts = new int[itemCapacity + 1];
        EffectAllowedTerrainCounts = new int[itemCapacity + 1];
        EffectValues = new FixedScalar[itemCapacity + 1];
    }

    private void InitializeSharedItemArrays(int itemCapacity)
    {
        SubtypeIds = new int[itemCapacity + 1];
        RelationshipKindFilterIds = new int[itemCapacity + 1];
        RelationshipKindFilterMinimumStrengths = new FixedScalar[itemCapacity + 1];
        RelationshipKindFilterAllowsSlotCausalLimitMultiplier = new int[itemCapacity + 1];
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
        BindingRequireTargetCultureRoute = new int[itemCapacity + 1];
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
        ProducedPhysicalPlacementMinRadii = new int[itemCapacity + 1];
        ProducedPhysicalPlacementMaxRadii = new int[itemCapacity + 1];
        ProducedSemanticPlacementMinRadii = new int[itemCapacity + 1];
        ProducedSemanticPlacementMaxRadii = new int[itemCapacity + 1];
        InitializeProducedPlacementTerrainArrays(itemCapacity);
        ProducedPlacementRefCodes = new int[itemCapacity + 1];
        ProducedCultureRefCodes = new int[itemCapacity + 1];
        ProducedFixedCultureIds = new int[itemCapacity + 1];
        AllocateTagFilters(itemCapacity);

        AllocateProducedPlacementArrays(itemCapacity);
        ProducedCreateChances = new FixedScalar[itemCapacity + 1];
        ProducedSubtypeChoiceStarts = new int[itemCapacity + 1];
        ProducedSubtypeChoiceCounts = new int[itemCapacity + 1];
        ProducedQuerySubtypeStarts = new int[itemCapacity + 1];
        ProducedQuerySubtypeCounts = new int[itemCapacity + 1];
        ProducedQueryAdjacentToTarget = new int[itemCapacity + 1];
        ProducedQueryCultureMatchCodes = new int[itemCapacity + 1];
        ProducedQueryRequireTargetCultureRoute = new int[itemCapacity + 1];
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

    private void InitializeProducedPlacementTerrainArrays(int itemCapacity)
    {
        var planeSlotCapacity = itemCapacity * 2;
        ProducedPlacementPreferredTerrainStarts = new int[planeSlotCapacity + 1];
        ProducedPlacementPreferredTerrainCounts = new int[planeSlotCapacity + 1];
        ProducedPlacementAllowedTerrainStarts = new int[planeSlotCapacity + 1];
        ProducedPlacementAllowedTerrainCounts = new int[planeSlotCapacity + 1];
        ProducedPlacementAvoidedTerrainStarts = new int[planeSlotCapacity + 1];
        ProducedPlacementAvoidedTerrainCounts = new int[planeSlotCapacity + 1];
        ProducedPlacementFallbackTerrainIds = new int[planeSlotCapacity + 1];
        ProducedPlacementSiteFlags = new int[itemCapacity + 1];
        ProducedPlacementBandAffinities = new FixedScalar[(itemCapacity + 1) * PlacementBandAffinityCount];
    }

    internal const int PlacementBandAffinityCount = 4;
    internal const int PlacementWarmthAffinityOffset = 0;
    internal const int PlacementFireAffinityOffset = 1;
    internal const int PlacementAuroraAffinityOffset = 2;
    internal const int PlacementPressureAffinityOffset = 3;

    private void InitializeRelationshipArrays(int itemCapacity)
    {
        RelationshipSourceRefCodes = new int[itemCapacity + 1];
        RelationshipDestinationRefCodes = new int[itemCapacity + 1];
        RelationshipKindIds = new int[itemCapacity + 1];
        RelationshipReplaceExistingOutgoing = new int[itemCapacity + 1];
        RelationshipStrengths = new FixedScalar[itemCapacity + 1];
    }

    private void AllocateTagFilters(int itemCapacity)
    {
        BindingRequiredTagBits = new int[itemCapacity + 1];
        BindingWithoutTagBits = new int[itemCapacity + 1];
        BindingCultureIds = new int[itemCapacity + 1];
        BindingRelatedToTargetKindIds = new int[itemCapacity + 1];
        BindingRelatedToTargetIncoming = new int[itemCapacity + 1];
        BindingRelatedToTargetMinStrengths = new FixedScalar[itemCapacity + 1];
        ProducedQueryRequiredTagBits = new int[itemCapacity + 1];
        ProducedQueryWithoutTagBits = new int[itemCapacity + 1];
        ProducedQueryCultureIds = new int[itemCapacity + 1];
        ProducedQueryRelatedToTargetKindIds = new int[itemCapacity + 1];
        ProducedQueryRelatedToTargetIncoming = new int[itemCapacity + 1];
        ProducedQueryRelatedToTargetMinStrengths = new FixedScalar[itemCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Generators;

    public int SchemaVersion => 19;

}
