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
        ConfigureLocality(context, programs.Locality);
        ConfigureLowAltitudeVerbs(context, programs.LowAltitudeVerbs);
        ConfigureGameplayEffects(context, programs.Effects);
    }

    private static void ConfigureLocality(KernelConfigurationContext context, LocalityRules rules)
    {
        context.ConfigureLocality(rules.SemanticBaseCapacity, rules.CrowdingRadius);
        if (rules.PhysicalCapacityFormula is { } formula)
        {
            context.SetLocalityCapacityFormula(
                formula.ColdFloor,
                formula.WarmPeak,
                formula.GeothermalBlend,
                formula.FreshwaterWeight,
                formula.FertileWeight,
                formula.ReliefComfort,
                formula.ReliefPenalty,
                formula.RoughnessPenalty,
                formula.LandScale,
                formula.MarineScale);
        }

        foreach (var terrain in rules.Terrains)
        {
            context.RegisterTerrainLocality(terrain.TerrainId, terrain.BaseCapacity);
        }

        foreach (var kind in rules.Kinds)
        {
            context.RegisterKindLocality(
                kind.KindId,
                kind.LocalityRadius,
                kind.CapacityEmission,
                kind.SupportEmission,
                kind.ThreatEmission,
                kind.BaseAttractorMass,
                kind.CoverageAttractorScale,
                kind.InfluenceAttractorScale,
                kind.AttractorRadius);
            AddKindLocalityAttractorScalars(context, kind);
        }

        foreach (var relationship in rules.Relationships)
        {
            context.RegisterRelationshipLocality(
                relationship.RelationshipKindId,
                relationship.SupportEmission,
                relationship.ThreatEmission);
        }
    }

    private static void AddKindLocalityAttractorScalars(
        KernelConfigurationContext context,
        KindLocalityRule kind)
    {
        foreach (var scalarInput in kind.AttractorScalarInputs)
        {
            context.AddKindLocalityAttractorScalar(
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

        foreach (var rule in program.DistrictBonuses)
        {
            context.RegisterDistrictBonus(rule.SubtypeId, rule.BonusKindCode, rule.Magnitude);
        }

        foreach (var rule in program.StageRules)
        {
            context.RegisterStageProperties(
                rule.KindId,
                rule.StageIndex,
                rule.BandMultiplier,
                rule.WorkerBonus,
                rule.AmbientEmission,
                rule.DistrictCapacity);
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
            verb.RelationshipStrengthDelta,
            verb.InfluenceTargetingCode);
        foreach (var terrainId in verb.TerrainIds)
        {
            context.AddLowAltitudeVerbTerrainFilter(slot, terrainId);
        }

        if (verb.MinTemperature > 0 || verb.SiteFlags != 0 || verb.RequireFogAdjacent)
        {
            context.SetLowAltitudeVerbGeographyFilter(
                slot,
                verb.MinTemperature,
                verb.SiteFlags,
                verb.RequireFogAdjacent ? 1 : 0);
        }

        if (verb.RestAtDestination)
        {
            context.SetLowAltitudeVerbRestAtDestination(slot);
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
                step.ScalarArgument);
        }
    }

    private static void ConfigureGameplayEffects(KernelConfigurationContext context, EffectsProgram program)
    {
        foreach (var rule in program.RelationshipTagRules)
        {
            context.RegisterRelationshipTagRule(
                rule.KindId,
                rule.TagId,
                rule.RelationshipKindId,
                rule.RelationshipDirectionCode,
                rule.SetAtOrAbove,
                rule.ClearBelow,
                rule.InsideCoverageOwnerKindId,
                rule.InsideCoverageOwnerSubtypeId,
                rule.InsideCoveragePlaneCode);
        }

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
                effect.CultureSlotPolarityCode,
                effect.SupportScale,
                effect.CoverageOwnerKindId,
                effect.CoverageOwnerSubtypeId);
        }
    }
}
