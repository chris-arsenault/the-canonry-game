namespace Engine.Host;

using Engine.Domain;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Gameplay;
using Engine.Kernel;

internal static partial class KernelProgramInstaller
{
    // Installs the GameplayCompiler output as kernel primitive tables; the host does not compile gameplay.
    private static void InstallGameplay(KernelConfigurationContext context, KernelProgramSet programs)
    {
        ConfigureMapActivity(context, programs.MapActivity);
        ConfigureMapSignals(context, programs.MapSignals);
        ConfigureLowAltitudeVerbs(context, programs.LowAltitudeVerbs);
        ConfigureGameplayEffects(context, programs.Effects);
    }

    private static void ConfigureMapActivity(KernelConfigurationContext context, MapActivityProgram program)
    {
        context.ConfigureMapActivity(program.PresenceHeat, program.HeatDecayPerTick, program.HeatEmaAlpha);
    }

    private static void ConfigureMapSignals(KernelConfigurationContext context, MapSignalsProgram program)
    {
        context.ConfigureMapSignals(program.SemanticBaseCapacity, program.CrowdingRadius);
        foreach (var terrain in program.Terrains)
        {
            context.RegisterTerrainMapSignal(terrain.TerrainId, terrain.BaseCapacity);
        }

        foreach (var kind in program.Kinds)
        {
            context.RegisterKindMapSignal(
                kind.KindId,
                kind.SignalRadius,
                kind.CapacityEmission,
                kind.SupportEmission,
                kind.ThreatEmission,
                kind.BaseAttractorMass,
                kind.CoverageAttractorScale,
                kind.InfluenceAttractorScale,
                kind.ActivityAttractorScale,
                kind.AttractorRadius);
            AddKindMapSignalAttractorScalars(context, kind);
        }

        foreach (var relationship in program.Relationships)
        {
            context.RegisterRelationshipMapSignal(
                relationship.RelationshipKindId,
                relationship.SignalRadius,
                relationship.SupportEmission,
                relationship.ThreatEmission);
        }
    }

    private static void AddKindMapSignalAttractorScalars(
        KernelConfigurationContext context,
        KindMapSignalRule kind)
    {
        foreach (var scalarInput in kind.AttractorScalarInputs)
        {
            context.AddKindMapSignalAttractorScalar(
                kind.KindId,
                scalarInput.AttributeId,
                scalarInput.AttractorScale);
        }
    }

    private static void ConfigureLowAltitudeVerbs(KernelConfigurationContext context, LowAltitudeVerbProgram program)
    {
        foreach (var verb in program.Verbs)
        {
            RegisterLowAltitudeVerb(context, verb);
        }

        foreach (var rule in program.InitialInfluence)
        {
            context.RegisterLowAltitudeInitialInfluence(
                rule.KindId,
                rule.PlaneCode,
                rule.InfluenceValue);
        }

        foreach (var rule in program.Affordances)
        {
            context.RegisterLowAltitudeAffordance(
                rule.SourceKindCode,
                rule.SourceId,
                rule.ShapeCode,
                rule.ActorKindId,
                rule.PlaneCode,
                rule.CandidateKindId,
                rule.RelationshipKindId,
                rule.RelationshipDirectionCode,
                rule.OperationCode,
                rule.PolarityCode,
                rule.Threshold,
                rule.Weight);
        }

        foreach (var modifier in program.TraitWeightModifiers)
        {
            context.RegisterLowAltitudeTraitWeightModifier(
                modifier.KindId,
                modifier.TraitId,
                modifier.VerbId,
                modifier.WeightDelta,
                modifier.OpportunityBonusDelta,
                modifier.FitnessWeightDelta);
        }
    }

    private static void RegisterLowAltitudeVerb(KernelConfigurationContext context, LowAltitudeVerbRule verb)
    {
        var slot = context.RegisterLowAltitudeVerb(
            verb.KindId,
            verb.VerbId,
            verb.PlaneCode,
            verb.OperationCode,
            verb.TargetModeCode,
            verb.TargetKindId,
            verb.RelationshipKindId,
            verb.RelationshipDirectionCode,
            verb.MinCommitmentTicks,
            verb.MaxCommitmentTicks,
            verb.DwellTicks,
            verb.TargetRadius,
            verb.CandidateSampleCount,
            verb.Weight,
            verb.OpportunityBonusOverride,
            verb.FitnessWeight,
            verb.DistanceScaleOverride,
            verb.FitnessRelationshipKindId,
            verb.FitnessRelationshipDirectionCode,
            verb.FitnessPolarityCode,
            verb.FitnessThreshold,
            verb.InfluenceDelta,
            verb.InfluenceRadius,
            verb.CoverageGrowthChance,
            verb.HeadquartersMoveChance,
            verb.RequiredInfluence,
            verb.ForeignInfluenceFilterEnabled ? 1 : 0,
            verb.ForeignInfluenceBlockPositiveRelationships ? 1 : 0,
            verb.ForeignInfluenceAllowNegativeRelationships ? 1 : 0,
            verb.ForeignInfluenceMinimumRelationshipStrength,
            verb.ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove,
            verb.RelationshipStrengthDelta);
        foreach (var terrainId in verb.TerrainIds)
        {
            context.AddLowAltitudeVerbTerrainFilter(slot, terrainId);
        }

        AddLowAltitudeVerbPrimitiveSteps(context, slot, verb.PrimitiveSteps);
    }

    private static void AddLowAltitudeVerbPrimitiveSteps(
        KernelConfigurationContext context,
        int slot,
        IReadOnlyList<CompiledProgramStep> steps)
    {
        foreach (var step in steps)
        {
            context.AddLowAltitudeVerbPrimitiveStep(
                slot,
                step.Opcode,
                step.IntArgument,
                step.FloatArgument);
        }
    }

    private static void ConfigureGameplayEffects(KernelConfigurationContext context, EffectsProgram program)
    {
        foreach (var effect in program.AttributeDeltaEffects)
        {
            context.RegisterAttributeDeltaEffect(
                effect.KindId,
                new AttributeRuntimeId(effect.AttributeId),
                effect.ConditionCode,
                effect.RelationshipKindId,
                effect.RelationshipDirectionCode,
                effect.PlaneCode,
                effect.MaxDistance,
                effect.Threshold,
                effect.MinimumRelationshipStrength,
                effect.DeltaPerTick,
                effect.RequiredTargetTraitId,
                effect.RequiredRelatedTraitId,
                effect.CultureId,
                effect.CultureSlotId,
                effect.CultureSlotPolarityCode);
        }
    }
}
