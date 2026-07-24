namespace Engine.Host;

using Engine.Domain;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Kernel;
using Engine.Kernel.Programs;

internal static partial class KernelProgramInstaller
{
    public static void Install(
        KernelConfigurationContext context,
        KernelProgramSet programs,
        KernelProgramConfigurationContext programConfig)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(programs);
        ArgumentNullException.ThrowIfNull(programConfig);

        InstallGameplay(context, programs);
        InstallUpkeep(context, programs.Upkeep);
        InstallGambits(context, programs.Gambits);
        InstallRelationships(context, programs.Relationships);
        InstallGenerators(context, programs.Generators);
        InstallCultureSlots(context, programs.CultureSlots);
        InstallMinorActors(context, programs.MinorActors);
        InstallChronicles(context, programs.Chronicles);
        InstallWorldBeats(context, programs.WorldBeats);
        InstallMapFrontier(context, programs.MapFrontier);

        context.SetProgramEnabled(
            KernelProgramCodes.Movement,
            programConfig.IsEnabled(KernelProgramCodes.Movement, defaultValue: true));
        context.SetProgramEnabled(
            KernelProgramCodes.LowAltitudeVerbs,
            programConfig.IsEnabled(KernelProgramCodes.LowAltitudeVerbs, programs.LowAltitudeVerbs.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.MapActivity,
            programConfig.IsEnabled(KernelProgramCodes.MapActivity, programs.MapActivity.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.MapSignals,
            programConfig.IsEnabled(KernelProgramCodes.MapSignals, programs.MapSignals.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Effects,
            programConfig.IsEnabled(KernelProgramCodes.Effects, programs.Effects.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.CultureSlots,
            programConfig.IsEnabled(KernelProgramCodes.CultureSlots, programs.CultureSlots.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Upkeep,
            programConfig.IsEnabled(KernelProgramCodes.Upkeep, programs.Upkeep.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Gambits,
            programConfig.IsEnabled(KernelProgramCodes.Gambits, programs.Gambits.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Relationships,
            programConfig.IsEnabled(KernelProgramCodes.Relationships, programs.Relationships.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Generators,
            programConfig.IsEnabled(KernelProgramCodes.Generators, programs.Generators.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.MinorActors,
            programConfig.IsEnabled(KernelProgramCodes.MinorActors, programs.MinorActors.EnabledByDefault));
    }

    private static void InstallMinorActors(KernelConfigurationContext context, MinorActorProgram program)
    {
        if (program.CampaignRelationshipKindId > 0)
        {
            context.SetMinorActorCampaignRelationshipKind(program.CampaignRelationshipKindId);
        }

        if (program.CarrierRelationshipKindId > 0)
        {
            context.SetMovementCarrierRelationshipKind(program.CarrierRelationshipKindId);
        }

        foreach (var rule in program.EmitterRules)
        {
            var ruleSlot = context.RegisterMinorActorEmitterRule(
                rule.SourceKindId,
                rule.SourceSubtypeId,
                rule.PurposeCode,
                rule.ActorKindCode,
                rule.ActionCode,
                rule.PlaneCode,
                rule.TtlPulses,
                rule.EmitCadencePulses,
                rule.MaxActivePerSource,
                rule.SpawnRadius,
                rule.MoveChancePermille,
                rule.ActionCadencePulses,
                rule.WorkDelta);

            foreach (var verb in rule.Verbs)
            {
                _ = context.RegisterMinorActorVerb(
                    ruleSlot,
                    verb.VerbId,
                    verb.ActionCode,
                    verb.TargetingCode,
                    verb.PlaneCode,
                    verb.Radius,
                    verb.SampleCount,
                    verb.Weight,
                    verb.WorkDeltaMultiplier);
            }
        }
    }

    private static void InstallCultureSlots(KernelConfigurationContext context, CultureSlotProgram program)
    {
        foreach (var slot in program.Slots)
        {
            var slotHandle = context.RegisterCultureSlot(
                slot.CultureId,
                slot.SlotId,
                slot.PolarityCode,
                slot.MinimumScore,
                slot.MinimumTenureTicks,
                slot.DisplacementMargin,
                slot.EntityInfluenceMultiplierDelta,
                slot.CultureInfluenceMultiplierDelta);
            foreach (var kindId in slot.AllowedKindIds)
            {
                context.AddCultureSlotAllowedKind(slotHandle, kindId);
            }

            foreach (var rule in slot.ScoreRules)
            {
                context.AddCultureSlotScoreRule(
                    slotHandle,
                    rule.SignalCode,
                    rule.RelationshipKindId,
                    rule.Weight);
            }
        }
    }

    private static void InstallUpkeep(KernelConfigurationContext context, UpkeepProgram program)
    {
        foreach (var lifecycle in program.LifecycleRules)
        {
            context.RegisterLifecycleUpkeep(
                lifecycle.KindId,
                new AttributeRuntimeId(lifecycle.HealthAttributeId),
                lifecycle.BaseRate,
                lifecycle.AgeCoefficient,
                lifecycle.Variance);
        }
    }

    private static void InstallRelationships(KernelConfigurationContext context, RelationshipProgram program)
    {
        foreach (var relationship in program.Relationships)
        {
            context.RegisterRelationshipKind(
                new RelationshipKindRuntimeId(relationship.RelationshipKindId),
                relationship.CullThreshold,
                relationship.TargetFollowsSourcePhysical,
                relationship.IsExclusive,
                relationship.IsCultureAnchor);
        }

        foreach (var rule in program.MaintenanceRules)
        {
            context.AddRelationshipMaintenanceRule(
                new RelationshipKindRuntimeId(rule.RelationshipKindId),
                rule.ConditionCode,
                rule.PlaneCode,
                rule.MaxDistance,
                rule.Threshold,
                rule.Delta);
        }
    }

    private static void InstallGambits(
        KernelConfigurationContext context,
        GambitProgram program)
    {
        foreach (var gambit in program.Gambits)
        {
            InstallGambit(context, gambit);
        }

        foreach (var modifier in program.TraitModifiers)
        {
            context.RegisterTraitGambitModifier(
                modifier.GambitId,
                modifier.TraitId,
                modifier.BearerCode,
                modifier.ScoreDelta,
                modifier.SuccessChanceDelta);
        }
    }

    private static void InstallGambit(KernelConfigurationContext context, CompiledGambitRule gambit)
    {
        var gambitSlot = context.RegisterGambit(
            gambit.KindId,
            gambit.GambitId,
            gambit.CooldownTicks,
            gambit.EntityCooldownTicks,
            TargetModeCode(gambit.TargetMode),
            gambit.TargetKindId,
            gambit.SuccessChance);
        if (gambit.ChronicleGranted)
        {
            context.MarkGambitChronicleGranted(gambitSlot);
        }

        InstallGambitPreconditions(context, gambitSlot, gambit);
        InstallGambitExpressions(context, gambitSlot, gambit);
        InstallGambitProviderSelectors(context, gambitSlot, gambit);
        InstallGambitSteps(context, gambitSlot, gambit);
    }

    private static void InstallGambitPreconditions(KernelConfigurationContext context, int gambitSlot, CompiledGambitRule gambit)
    {
        foreach (var precondition in gambit.Preconditions)
        {
            context.AddGambitPrecondition(gambitSlot, PreconditionCode(precondition.Code), precondition.A, precondition.B, precondition.C, precondition.D, precondition.X, precondition.Y);
        }
    }

    private static void InstallGambitExpressions(KernelConfigurationContext context, int gambitSlot, CompiledGambitRule gambit)
    {
        foreach (var expression in gambit.ScoreExpression)
        {
            context.AddGambitScoreExpressionOp(gambitSlot, ExpressionCode(expression.Code), expression.A, expression.X);
        }
    }

    private static void InstallGambitProviderSelectors(KernelConfigurationContext context, int gambitSlot, CompiledGambitRule gambit)
    {
        foreach (var selector in gambit.ProviderSelectors)
        {
            context.AddGambitProviderSelector(gambitSlot, ProviderSelectorCode(selector.Selector), selector.KindId, selector.ValueId, selector.RelationshipKindId, selector.MinimumStrength);
        }
    }

    private static void InstallGambitSteps(KernelConfigurationContext context, int gambitSlot, CompiledGambitRule gambit)
    {
        foreach (var step in gambit.Steps)
        {
            var stepSlot = context.AddGambitStep(gambitSlot, OperationCode(step.Operation), step.A, step.B, step.C, step.D, step.X, step.Y);
            InstallGambitStepExpression(context, stepSlot, step);
        }
    }

    private static void InstallGambitStepExpression(KernelConfigurationContext context, int stepSlot, BehaviorProgramStepRule step)
    {
        if (step.Expression is null)
        {
            return;
        }

        foreach (var expression in step.Expression)
        {
            context.AddGambitStepExpressionOp(stepSlot, ExpressionCode(expression.Code), expression.A, expression.X);
        }
    }


    private static void InstallWorldBeats(KernelConfigurationContext context, WorldBeatProgram program)
    {
        foreach (var age in program.Ages)
        {
            var ageSlot = context.AddWorldBeatAge(
                age.AgeId,
                age.TrackCode,
                age.Priority,
                age.MinOpenTicks,
                age.CooldownTicks);
            foreach (var condition in age.OpenWhen)
            {
                context.AddWorldBeatOpenCondition(
                    ageSlot,
                    condition.FactCode,
                    condition.ComparatorCode,
                    condition.Value,
                    condition.ParamId);
            }

            foreach (var condition in age.CloseWhen)
            {
                context.AddWorldBeatCloseCondition(
                    ageSlot,
                    condition.FactCode,
                    condition.ComparatorCode,
                    condition.Value,
                    condition.ParamId);
            }

            foreach (var charge in age.Charges)
            {
                context.AddWorldBeatCharge(ageSlot, charge.DomainCode, charge.TargetId, charge.Multiplier);
            }
        }
    }

    private static void InstallChronicles(KernelConfigurationContext context, ChronicleProgram program)
    {
        foreach (var archetype in program.Archetypes)
        {
            var archetypeSlot = context.AddChronicleArchetype(
                archetype.ArchetypeId,
                archetype.MaxActive,
                archetype.CastingCadenceTicks,
                archetype.PrimaryKindId,
                archetype.PrimaryRequiredRelKindId,
                archetype.PrimaryRequiredRelMinStrength,
                archetype.FadeOutcomeCodeId,
                archetype.FadeGraceTicks,
                archetype.FadeQuietTicks);
            foreach (var role in archetype.SupportingRoles)
            {
                context.AddChronicleRole(archetypeSlot, role.KindId, role.RelatedKindId, role.RelatedIncoming, role.RelatedMinStrength, role.Open);
            }

            foreach (var beat in archetype.Beats)
            {
                InstallChronicleBeat(context, archetypeSlot, beat);
            }
        }
    }

    private static void InstallChronicleBeat(KernelConfigurationContext context, int archetypeSlot, CompiledChronicleBeat beat)
    {
        var beatSlot = context.AddChronicleBeat(
            archetypeSlot,
            beat.BeatCodeId,
            beat.HeatGain,
            beat.TerminalOutcomeCodeId,
            beat.Open.Kind,
            beat.Open.RoleA,
            beat.Open.RoleB,
            beat.Open.RelOrAttrId,
            beat.Open.Value,
            beat.StrikeKind,
            beat.StrikeActorRole,
            beat.StrikeTargetRole,
            beat.StrikeGambitId,
            beat.StrikeAcceptFailure,
            beat.StrikeJoinRole,
            beat.StrikeFact.Kind,
            beat.StrikeFact.RoleA,
            beat.StrikeFact.RoleB,
            beat.StrikeFact.RelOrAttrId,
            beat.StrikeFact.Value);
        foreach (var charge in beat.Charges)
        {
            context.AddChronicleBeatCharge(beatSlot, charge.ActorRole, charge.TargetRole, charge.GambitId, charge.ScoreBonus, charge.HeatScaled, charge.SuccessBonus);
        }

        foreach (var grant in beat.Grants)
        {
            context.AddChronicleBeatGrant(beatSlot, grant.Role, grant.GambitId);
        }
    }

    private static void InstallGenerators(KernelConfigurationContext context, GeneratorProgram program)
    {
        foreach (var generator in program.Generators)
        {
            InstallGenerator(context, generator);
        }
    }

    private static void InstallGenerator(KernelConfigurationContext context, CompiledGeneratorRule generator)
    {
        var generatorSlot = context.RegisterGenerator(
            generator.GeneratorId,
            generator.TargetKindId,
            generator.PulseWeight,
            generator.MaxRuns,
            generator.CadenceTicks,
            generator.CadenceJitterTicks,
            generator.TargetRequiredTagBits,
            generator.TargetWithoutTagBits);
        if (generator.TargetChronicleHeatMin > 0f || generator.TargetChronicleLegendaryWithinTicks > 0)
        {
            context.SetGeneratorTargetChronicleGates(
                generatorSlot,
                generator.TargetChronicleHeatMin,
                generator.TargetChronicleLegendaryWithinTicks);
        }

        InstallGeneratorTarget(context, generatorSlot, generator);
        InstallGeneratorBindings(context, generatorSlot, generator);
        InstallGeneratorProducedEntities(context, generatorSlot, generator);
        InstallGeneratorRelationships(context, generatorSlot, generator);
        InstallGeneratorEffects(context, generatorSlot, generator);
    }

    private static void InstallGeneratorEffects(KernelConfigurationContext context, int generatorSlot, CompiledGeneratorRule generator)
    {
        foreach (var effect in generator.Effects)
        {
            var effectSlot = context.AddGeneratorEffect(
                generatorSlot,
                effect.EffectTypeCode,
                effect.RefOrKindId,
                effect.InstanceSelectionCode,
                effect.OptionCode,
                effect.Value,
                effect.Radius,
                effect.MaxCellCount,
                effect.PlaneCode);
            foreach (var terrainId in effect.PreferredTerrainIds ?? [])
            {
                context.AddGeneratorEffectPreferredTerrain(effectSlot, terrainId);
            }

            foreach (var terrainId in effect.AllowedTerrainIds ?? [])
            {
                context.AddGeneratorEffectAllowedTerrain(effectSlot, terrainId);
            }
        }
    }

    private static void InstallGeneratorTarget(KernelConfigurationContext context, int generatorSlot, CompiledGeneratorRule generator)
    {
        foreach (var subtypeId in generator.TargetSubtypeIds)
        {
            context.AddGeneratorTargetSubtype(generatorSlot, subtypeId);
        }

        InstallGeneratorTargetRelationships(context, generatorSlot, generator.TargetRelationshipRequirements);
        InstallGeneratorTargetTerrain(context, generatorSlot, generator.TargetRelationshipRequirements);
        InstallGeneratorTriggers(context, generatorSlot, generator);
    }

    private static void InstallGeneratorTargetRelationships(
        KernelConfigurationContext context,
        int generatorSlot,
        GeneratorTargetRelationshipRequirements requirements)
    {
        foreach (var relationship in requirements.RequiredOutgoingRelationships)
        {
            context.AddGeneratorTargetRequiredOutgoingRelationship(
                generatorSlot,
                relationship.RelationshipKindId,
                relationship.MinimumStrength);
        }

        foreach (var relationship in requirements.RequiredIncomingRelationships)
        {
            context.AddGeneratorTargetRequiredIncomingRelationship(
                generatorSlot,
                relationship.RelationshipKindId,
                relationship.MinimumStrength);
        }

        foreach (var relationship in requirements.RequiredRelationships)
        {
            context.AddGeneratorTargetRequiredRelationship(
                generatorSlot,
                relationship.RelationshipKindId,
                relationship.MinimumStrength);
        }

        foreach (var relationshipKindId in requirements.WithoutOutgoingRelationshipKindIds)
        {
            context.AddGeneratorTargetWithoutOutgoingRelationship(generatorSlot, relationshipKindId);
        }

        foreach (var relationshipKindId in requirements.WithoutIncomingRelationshipKindIds)
        {
            context.AddGeneratorTargetWithoutIncomingRelationship(generatorSlot, relationshipKindId);
        }
    }

    private static void InstallGeneratorTargetTerrain(
        KernelConfigurationContext context,
        int generatorSlot,
        GeneratorTargetRelationshipRequirements requirements)
    {
        foreach (var requirement in requirements.TerrainCountRequirements)
        {
            context.AddGeneratorTargetTerrainCountBelow(
                generatorSlot,
                requirement.TerrainId,
                requirement.Radius,
                requirement.MaximumExclusive);
        }
    }

    private static void InstallGeneratorTriggers(
        KernelConfigurationContext context,
        int generatorSlot,
        CompiledGeneratorRule generator)
    {
        foreach (var trigger in generator.Triggers)
        {
            context.AddGeneratorTrigger(
                generatorSlot,
                trigger.KindId);
        }
    }

    private static void InstallGeneratorBindings(KernelConfigurationContext context, int generatorSlot, CompiledGeneratorRule generator)
    {
        foreach (var binding in generator.SecondaryBindings)
        {
            var query = binding.Query;
            var bindingSlot = AddSecondaryBinding(context, generatorSlot, binding, query);
            if (query is not null)
            {
                InstallGeneratorBindingQuery(context, bindingSlot, query);
            }
        }
    }

    private static int AddSecondaryBinding(
        KernelConfigurationContext context,
        int generatorSlot,
        GeneratorBindingRule binding,
        GeneratorEntityQueryRule? query)
    {
        var resolved = query ?? new GeneratorEntityQueryRule([], false, false, GeneratorProgramCultureMatch.Any, [], []);
        return context.AddGeneratorSecondaryBinding(
            generatorSlot,
            binding.RefCode,
            binding.KindId,
            resolved.AdjacentToTarget,
            GeneratorCultureMatchCode(resolved.CultureMatch),
            resolved.SamePhysicalCultureComponentAsTarget,
            resolved.RequiredTagBits,
            resolved.WithoutTagBits,
            resolved.CultureId,
            resolved.RelatedToTargetKindId,
            resolved.RelatedToTargetIncoming,
            resolved.RelatedToTargetMinStrength);
    }

    private static void InstallGeneratorProducedEntities(KernelConfigurationContext context, int generatorSlot, CompiledGeneratorRule generator)
    {
        foreach (var produced in generator.ProducedEntities)
        {
            InstallGeneratorProducedEntity(context, generatorSlot, produced);
        }
    }

    private static void InstallGeneratorProducedEntity(KernelConfigurationContext context, int generatorSlot, GeneratorProducedEntityRule produced)
    {
        var query = produced.Query;
        var resolvedQuery = query ?? new GeneratorEntityQueryRule([], false, false, GeneratorProgramCultureMatch.Any, [], []);
        var coverage = produced.Coverage;
        var producedSlot = context.AddGeneratorProduced(
            generatorSlot,
            produced.RefCode,
            produced.KindId,
            produced.SubtypeId,
            produced.CreateIfMissing,
            produced.HasQuery,
            resolvedQuery.AdjacentToTarget,
            GeneratorCultureMatchCode(resolvedQuery.CultureMatch),
            GeneratorPlacementCode(produced.PhysicalPlacement),
            GeneratorPlacementCode(produced.SemanticPlacement),
            produced.PlacementRefCode,
            produced.CultureRefCode,
            produced.FixedCultureId,
            produced.CreateChance,
            NullableCoverageModeCode(coverage),
            CoverageSourceRefCode(coverage),
            CoverageDesiredCellCount(coverage),
            CoverageMinimumCellCount(coverage),
            CoverageMinimumSourceRemainingCellCount(coverage),
            CoverageRespectTerrainBoundaries(coverage),
            CoverageTransformTerrainId(coverage),
            produced.PhysicalPlacementMinRadius,
            produced.PhysicalPlacementMaxRadius,
            produced.SemanticPlacementMinRadius,
            produced.SemanticPlacementMaxRadius,
            GeneratorCulturePlacementCode(produced.CulturePlacement),
            resolvedQuery.SamePhysicalCultureComponentAsTarget,
            resolvedQuery.RequiredTagBits,
            resolvedQuery.WithoutTagBits,
            resolvedQuery.CultureId,
            resolvedQuery.RelatedToTargetKindId,
            resolvedQuery.RelatedToTargetIncoming,
            resolvedQuery.RelatedToTargetMinStrength);
        InstallGeneratorProducedSubtypeChoices(context, producedSlot, produced);
        InstallGeneratorProducedPlacementTerrains(context, producedSlot, produced);
        if (query is not null)
        {
            InstallGeneratorProducedQuery(context, producedSlot, query);
        }

        if (coverage is not null)
        {
            InstallGeneratorProducedCoverage(context, producedSlot, coverage);
        }
    }

    private static void InstallGeneratorProducedPlacementTerrains(
        KernelConfigurationContext context,
        int producedSlot,
        GeneratorProducedEntityRule produced)
    {
        foreach (var terrainId in produced.PhysicalPlacementPreferredTerrainIds)
        {
            context.AddGeneratorProducedPlacementPreferredTerrain(producedSlot, PlaneCodes.Physical, terrainId);
        }

        foreach (var terrainId in produced.PhysicalPlacementAllowedTerrainIds)
        {
            context.AddGeneratorProducedPlacementAllowedTerrain(producedSlot, PlaneCodes.Physical, terrainId);
        }

        foreach (var terrainId in produced.PhysicalPlacementAvoidedTerrainIds)
        {
            context.AddGeneratorProducedPlacementAvoidedTerrain(producedSlot, PlaneCodes.Physical, terrainId);
        }

        if (produced.PhysicalPlacementFallbackTerrainId > 0)
        {
            context.SetGeneratorProducedPlacementFallbackTerrain(producedSlot, PlaneCodes.Physical, produced.PhysicalPlacementFallbackTerrainId);
        }

        foreach (var terrainId in produced.SemanticPlacementPreferredTerrainIds)
        {
            context.AddGeneratorProducedPlacementPreferredTerrain(producedSlot, PlaneCodes.Semantic, terrainId);
        }

        foreach (var terrainId in produced.SemanticPlacementAllowedTerrainIds)
        {
            context.AddGeneratorProducedPlacementAllowedTerrain(producedSlot, PlaneCodes.Semantic, terrainId);
        }

        foreach (var terrainId in produced.SemanticPlacementAvoidedTerrainIds)
        {
            context.AddGeneratorProducedPlacementAvoidedTerrain(producedSlot, PlaneCodes.Semantic, terrainId);
        }

        if (produced.SemanticPlacementFallbackTerrainId > 0)
        {
            context.SetGeneratorProducedPlacementFallbackTerrain(producedSlot, PlaneCodes.Semantic, produced.SemanticPlacementFallbackTerrainId);
        }

        InstallGeneratorProducedPlacementGeography(context, producedSlot, produced);
        if (produced.PhysicalPlacementFeatureKind >= 0)
        {
            context.SetGeneratorProducedFeatureKind(producedSlot, produced.PhysicalPlacementFeatureKind);
        }
    }

    private static void InstallGeneratorProducedPlacementGeography(
        KernelConfigurationContext context,
        int producedSlot,
        GeneratorProducedEntityRule produced)
    {
        var hasAffinity =
            produced.PhysicalPlacementWarmthAffinity != 0f ||
            produced.PhysicalPlacementFireAffinity != 0f ||
            produced.PhysicalPlacementAuroraAffinity != 0f ||
            produced.PhysicalPlacementPressureAffinity != 0f;
        if (produced.PhysicalPlacementSiteFlags == 0 && !hasAffinity)
        {
            return;
        }

        context.SetGeneratorProducedPlacementGeography(
            producedSlot,
            produced.PhysicalPlacementSiteFlags,
            produced.PhysicalPlacementWarmthAffinity,
            produced.PhysicalPlacementFireAffinity,
            produced.PhysicalPlacementAuroraAffinity,
            produced.PhysicalPlacementPressureAffinity);
    }

    private static int NullableCoverageModeCode(GeneratorCoverageRule? coverage) =>
        GeneratorCoverageModeCode(coverage?.Mode ?? GeneratorProgramCoverageMode.None);

    private static int CoverageSourceRefCode(GeneratorCoverageRule? coverage) => coverage?.SourceRefCode ?? 0;

    private static int CoverageDesiredCellCount(GeneratorCoverageRule? coverage) => coverage?.DesiredCellCount ?? 0;

    private static int CoverageMinimumCellCount(GeneratorCoverageRule? coverage) => coverage?.MinimumCellCount ?? 0;

    private static int CoverageMinimumSourceRemainingCellCount(GeneratorCoverageRule? coverage) =>
        coverage?.MinimumSourceRemainingCellCount ?? 0;

    private static bool CoverageRespectTerrainBoundaries(GeneratorCoverageRule? coverage) =>
        coverage?.RespectTerrainBoundaries ?? true;

    private static int CoverageTransformTerrainId(GeneratorCoverageRule? coverage) => coverage?.TransformTerrainId ?? 0;

    private static void InstallGeneratorProducedSubtypeChoices(KernelConfigurationContext context, int producedSlot, GeneratorProducedEntityRule produced)
    {
        foreach (var subtypeId in produced.SubtypeChoiceIds)
        {
            context.AddGeneratorProducedSubtypeChoice(producedSlot, subtypeId);
        }
    }

    private static void InstallGeneratorProducedCoverage(KernelConfigurationContext context, int producedSlot, GeneratorCoverageRule coverage)
    {
        foreach (var terrainId in coverage.PreferredTerrainIds)
        {
            context.AddGeneratorProducedCoveragePreferredTerrain(producedSlot, terrainId);
        }

        foreach (var terrainId in coverage.AllowedTerrainIds)
        {
            context.AddGeneratorProducedCoverageAllowedTerrain(producedSlot, terrainId);
        }
    }

    private static void InstallGeneratorRelationships(KernelConfigurationContext context, int generatorSlot, CompiledGeneratorRule generator)
    {
        foreach (var relationship in generator.Relationships)
        {
            context.AddGeneratorRelationship(
                generatorSlot,
                relationship.SourceRefCode,
                relationship.DestinationRefCode,
                relationship.RelationshipKindId,
                relationship.Strength,
                relationship.ReplaceExistingOutgoing);
        }
    }

    private static void InstallGeneratorBindingQuery(
        KernelConfigurationContext context,
        int bindingSlot,
        GeneratorEntityQueryRule query)
    {
        foreach (var subtypeId in query.SubtypeIds)
        {
            context.AddGeneratorBindingSubtype(bindingSlot, subtypeId);
        }

        foreach (var relationshipKindId in query.WithoutOutgoingRelationshipKindIds)
        {
            context.AddGeneratorBindingWithoutOutgoingRelationship(bindingSlot, relationshipKindId);
        }

        foreach (var relationshipKindId in query.WithoutIncomingRelationshipKindIds)
        {
            context.AddGeneratorBindingWithoutIncomingRelationship(bindingSlot, relationshipKindId);
        }

    }

    private static void InstallGeneratorProducedQuery(
        KernelConfigurationContext context,
        int producedSlot,
        GeneratorEntityQueryRule query)
    {
        foreach (var subtypeId in query.SubtypeIds)
        {
            context.AddGeneratorProducedQuerySubtype(producedSlot, subtypeId);
        }

        foreach (var relationshipKindId in query.WithoutOutgoingRelationshipKindIds)
        {
            context.AddGeneratorProducedQueryWithoutOutgoingRelationship(producedSlot, relationshipKindId);
        }

        foreach (var relationshipKindId in query.WithoutIncomingRelationshipKindIds)
        {
            context.AddGeneratorProducedQueryWithoutIncomingRelationship(producedSlot, relationshipKindId);
        }

    }

    private static int TargetModeCode(BehaviorProgramTargetMode mode) => mode switch
    {
        BehaviorProgramTargetMode.Self => BehaviorTargetModeCodes.Self,
        BehaviorProgramTargetMode.AnyOther => BehaviorTargetModeCodes.AnyOther,
        BehaviorProgramTargetMode.KindOther => BehaviorTargetModeCodes.KindOther,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    private static int OperationCode(BehaviorProgramOperation operation)
    {
        if (TryAttributeOperationCode(operation, out var code) ||
            TryMovementOperationCode(operation, out code) ||
            TryEntityOperationCode(operation, out code) ||
            TryMapOperationCode(operation, out code))
        {
            return code;
        }

        throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
    }

    private static bool TryAttributeOperationCode(BehaviorProgramOperation operation, out int code)
    {
        code = operation switch
        {
            BehaviorProgramOperation.None => 0,
            BehaviorProgramOperation.AddAttribute => BehaviorOperationCodes.AddAttribute,
            BehaviorProgramOperation.SetAttribute => BehaviorOperationCodes.SetAttribute,
            BehaviorProgramOperation.AddTargetAttribute => BehaviorOperationCodes.AddTargetAttribute,
            BehaviorProgramOperation.TransferAttribute => BehaviorOperationCodes.TransferAttribute,
            BehaviorProgramOperation.CalculateVariable => BehaviorOperationCodes.CalculateVariable,
            BehaviorProgramOperation.AddAttributeFromVariable => BehaviorOperationCodes.AddAttributeFromVariable,
            BehaviorProgramOperation.AddTargetAttributeFromVariable => BehaviorOperationCodes.AddTargetAttributeFromVariable,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryMovementOperationCode(BehaviorProgramOperation operation, out int code)
    {
        code = operation switch
        {
            BehaviorProgramOperation.MovePhysical => BehaviorOperationCodes.MovePhysical,
            BehaviorProgramOperation.MoveSemantic => BehaviorOperationCodes.MoveSemantic,
            BehaviorProgramOperation.MoveTowardTargetPhysical => BehaviorOperationCodes.MoveTowardTargetPhysical,
            BehaviorProgramOperation.WanderPhysical => BehaviorOperationCodes.WanderPhysical,
            BehaviorProgramOperation.MoveTowardTargetSemantic => BehaviorOperationCodes.MoveTowardTargetSemantic,
            BehaviorProgramOperation.WanderSemantic => BehaviorOperationCodes.WanderSemantic,
            BehaviorProgramOperation.MoveTargetToSelfPhysical => BehaviorOperationCodes.MoveTargetToSelfPhysical,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryEntityOperationCode(BehaviorProgramOperation operation, out int code)
    {
        if (TryRelationshipOperationCode(operation, out code))
        {
            return true;
        }

        code = operation switch
        {
            BehaviorProgramOperation.SetStatus => BehaviorOperationCodes.SetStatus,
            BehaviorProgramOperation.CreateEntity => BehaviorOperationCodes.CreateEntity,
            BehaviorProgramOperation.SetHistorical => BehaviorOperationCodes.SetHistorical,
            BehaviorProgramOperation.AddRelatedEntitiesAttribute => BehaviorOperationCodes.AddRelatedEntitiesAttribute,
            BehaviorProgramOperation.SetTargetCultureFromSelf => BehaviorOperationCodes.SetTargetCultureFromSelf,
            BehaviorProgramOperation.IncreaseWorkTier => BehaviorOperationCodes.IncreaseWorkTier,
            BehaviorProgramOperation.ForgeCellVertical => BehaviorOperationCodes.ForgeCellVertical,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryRelationshipOperationCode(BehaviorProgramOperation operation, out int code)
    {
        code = operation switch
        {
            BehaviorProgramOperation.CreateRelationship => BehaviorOperationCodes.CreateRelationship,
            BehaviorProgramOperation.SetRelationshipStrength => BehaviorOperationCodes.SetRelationshipStrength,
            BehaviorProgramOperation.CreateFixedRelationship => BehaviorOperationCodes.CreateFixedRelationship,
            BehaviorProgramOperation.CreateEndpointRelationship => BehaviorOperationCodes.CreateEndpointRelationship,
            BehaviorProgramOperation.SetEndpointRelationshipStrength => BehaviorOperationCodes.SetEndpointRelationshipStrength,
            BehaviorProgramOperation.RemoveEndpointRelationship => BehaviorOperationCodes.RemoveEndpointRelationship,
            BehaviorProgramOperation.CullEndpointIncomingRelationships => BehaviorOperationCodes.CullEndpointIncomingRelationships,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryMapOperationCode(BehaviorProgramOperation operation, out int code)
    {
        code = operation switch
        {
            BehaviorProgramOperation.SetTargetCoverageTerrain => BehaviorOperationCodes.SetTargetCoverageTerrain,
            BehaviorProgramOperation.BuildTravelStructureLine => BehaviorOperationCodes.BuildTravelStructureLine,
            BehaviorProgramOperation.ErodeTargetCulture => BehaviorOperationCodes.ErodeTargetCulture,
            BehaviorProgramOperation.InstallDistrict => BehaviorOperationCodes.InstallDistrict,
            BehaviorProgramOperation.ReawakenDistrict => BehaviorOperationCodes.ReawakenDistrict,
            _ => -1,
        };
        return code >= 0;
    }

    private static int PreconditionCode(BehaviorProgramPrecondition precondition)
    {
        if (TryAttributePreconditionCode(precondition, out var code) ||
            TryRelationshipPreconditionCode(precondition, out code) ||
            TryCompositionPreconditionCode(precondition, out code) ||
            TryContextPreconditionCode(precondition, out code) ||
            TrySpatialPreconditionCode(precondition, out code) ||
            TryKernelStatePreconditionCode(precondition, out code))
        {
            return code;
        }

        throw new ArgumentOutOfRangeException(nameof(precondition), precondition, null);
    }

    private static bool TryAttributePreconditionCode(BehaviorProgramPrecondition precondition, out int code)
    {
        code = precondition switch
        {
            BehaviorProgramPrecondition.TargetIsSelf => BehaviorPreconditionCodes.TargetIsSelf,
            BehaviorProgramPrecondition.TargetIsNotSelf => BehaviorPreconditionCodes.TargetIsNotSelf,
            BehaviorProgramPrecondition.SelfAttributeRange => BehaviorPreconditionCodes.SelfAttributeRange,
            BehaviorProgramPrecondition.TargetAttributeRange => BehaviorPreconditionCodes.TargetAttributeRange,
            BehaviorProgramPrecondition.Always => BehaviorPreconditionCodes.Always,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryRelationshipPreconditionCode(BehaviorProgramPrecondition precondition, out int code)
    {
        code = precondition switch
        {
            BehaviorProgramPrecondition.SelfHasRelationshipToTarget => BehaviorPreconditionCodes.SelfHasRelationshipToTarget,
            BehaviorProgramPrecondition.TargetHasRelationshipToSelf => BehaviorPreconditionCodes.TargetHasRelationshipToSelf,
            BehaviorProgramPrecondition.SelfHasOutgoingRelationship => BehaviorPreconditionCodes.SelfHasOutgoingRelationship,
            BehaviorProgramPrecondition.SelfHasOutgoingRelationshipToKind => BehaviorPreconditionCodes.SelfHasOutgoingRelationshipToKind,
            BehaviorProgramPrecondition.EndpointRelationshipCountBelow => BehaviorPreconditionCodes.EndpointRelationshipCountBelow,
            BehaviorProgramPrecondition.RelatedEntityMatchesTarget => BehaviorPreconditionCodes.RelatedEntityMatchesTarget,
            BehaviorProgramPrecondition.RelationshipExists => BehaviorPreconditionCodes.RelationshipExists,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryCompositionPreconditionCode(BehaviorProgramPrecondition precondition, out int code)
    {
        code = precondition switch
        {
            BehaviorProgramPrecondition.And => BehaviorPreconditionCodes.And,
            BehaviorProgramPrecondition.Or => BehaviorPreconditionCodes.Or,
            BehaviorProgramPrecondition.Not => BehaviorPreconditionCodes.Not,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryContextPreconditionCode(BehaviorProgramPrecondition precondition, out int code)
    {
        code = precondition switch
        {
            BehaviorProgramPrecondition.PlaneDistance => BehaviorPreconditionCodes.PlaneDistance,
            BehaviorProgramPrecondition.CooldownElapsed => BehaviorPreconditionCodes.CooldownElapsed,
            BehaviorProgramPrecondition.EntityCountAtLeast => BehaviorPreconditionCodes.EntityCountAtLeast,
            BehaviorProgramPrecondition.HasColocatedEntity => BehaviorPreconditionCodes.HasColocatedEntity,
            BehaviorProgramPrecondition.ContestedInfluence => BehaviorPreconditionCodes.ContestedInfluence,
            BehaviorProgramPrecondition.SharedInfluence => BehaviorPreconditionCodes.SharedInfluence,
            BehaviorProgramPrecondition.EndpointCultureIs => BehaviorPreconditionCodes.EndpointCultureIs,
            BehaviorProgramPrecondition.EndpointSubtypeIs => BehaviorPreconditionCodes.EndpointSubtypeIs,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TrySpatialPreconditionCode(BehaviorProgramPrecondition precondition, out int code)
    {
        code = precondition switch
        {
            BehaviorProgramPrecondition.WithinTravelReach => BehaviorPreconditionCodes.WithinTravelReach,
            BehaviorProgramPrecondition.SelfHexFactRange => BehaviorPreconditionCodes.SelfHexFactRange,
            BehaviorProgramPrecondition.TargetHexFactRange => BehaviorPreconditionCodes.TargetHexFactRange,
            BehaviorProgramPrecondition.SelfOnTerrainGroup => BehaviorPreconditionCodes.SelfOnTerrainGroup,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryKernelStatePreconditionCode(BehaviorProgramPrecondition precondition, out int code)
    {
        code = precondition switch
        {
            BehaviorProgramPrecondition.SelfCultureMass => BehaviorPreconditionCodes.SelfCultureMass,
            BehaviorProgramPrecondition.SelfWorkTierBelow => BehaviorPreconditionCodes.SelfWorkTierBelow,
            _ => -1,
        };
        return code >= 0;
    }

    private static int ExpressionCode(BehaviorProgramExpression expression)
    {
        if (TryValueExpressionCode(expression, out var code) ||
            TryOperatorExpressionCode(expression, out code) ||
            TryReferenceExpressionCode(expression, out code))
        {
            return code;
        }

        throw new ArgumentOutOfRangeException(nameof(expression), expression, null);
    }

    private static bool TryValueExpressionCode(BehaviorProgramExpression expression, out int code)
    {
        code = expression switch
        {
            BehaviorProgramExpression.Literal => BehaviorExpressionCodes.Literal,
            BehaviorProgramExpression.SelfAttribute => BehaviorExpressionCodes.SelfAttribute,
            BehaviorProgramExpression.TargetAttribute => BehaviorExpressionCodes.TargetAttribute,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryOperatorExpressionCode(BehaviorProgramExpression expression, out int code)
    {
        code = expression switch
        {
            BehaviorProgramExpression.Add => BehaviorExpressionCodes.Add,
            BehaviorProgramExpression.Subtract => BehaviorExpressionCodes.Subtract,
            BehaviorProgramExpression.Multiply => BehaviorExpressionCodes.Multiply,
            BehaviorProgramExpression.Divide => BehaviorExpressionCodes.Divide,
            BehaviorProgramExpression.Min => BehaviorExpressionCodes.Min,
            BehaviorProgramExpression.Max => BehaviorExpressionCodes.Max,
            BehaviorProgramExpression.Clamp => BehaviorExpressionCodes.Clamp,
            BehaviorProgramExpression.Abs => BehaviorExpressionCodes.Abs,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryReferenceExpressionCode(BehaviorProgramExpression expression, out int code)
    {
        code = expression switch
        {
            BehaviorProgramExpression.Variable => BehaviorExpressionCodes.Variable,
            BehaviorProgramExpression.Distance => BehaviorExpressionCodes.Distance,
            BehaviorProgramExpression.SameCulture => BehaviorExpressionCodes.SameCulture,
            _ => -1,
        };
        return code >= 0;
    }

    private static int ProviderSelectorCode(BehaviorProgramProviderSelector selector) => selector switch
    {
        BehaviorProgramProviderSelector.TargetAttribute => BehaviorProviderSelectorCodes.TargetAttribute,
        BehaviorProgramProviderSelector.Structural => BehaviorProviderSelectorCodes.Structural,
        BehaviorProgramProviderSelector.LocationAttribute => BehaviorProviderSelectorCodes.LocationAttribute,
        BehaviorProgramProviderSelector.OutgoingRelationship => BehaviorProviderSelectorCodes.OutgoingRelationship,
        BehaviorProgramProviderSelector.IncomingRelationship => BehaviorProviderSelectorCodes.IncomingRelationship,
        _ => throw new ArgumentOutOfRangeException(nameof(selector), selector, null),
    };

    private static int GeneratorPlacementCode(GeneratorProgramPlacement placement) => placement switch
    {
        GeneratorProgramPlacement.Area => GeneratorPlacementCodes.Area,
        GeneratorProgramPlacement.Locality => GeneratorPlacementCodes.Locality,
        GeneratorProgramPlacement.Frontier => GeneratorPlacementCodes.Frontier,
        GeneratorProgramPlacement.FeatureAnchor => GeneratorPlacementCodes.FeatureAnchor,
        _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, null),
    };

    private static int GeneratorCultureMatchCode(GeneratorProgramCultureMatch cultureMatch) => cultureMatch switch
    {
        GeneratorProgramCultureMatch.Any => GeneratorCultureMatchCodes.Any,
        GeneratorProgramCultureMatch.SameAsTarget => GeneratorCultureMatchCodes.SameAsTarget,
        GeneratorProgramCultureMatch.DifferentFromTarget => GeneratorCultureMatchCodes.DifferentFromTarget,
        _ => throw new ArgumentOutOfRangeException(nameof(cultureMatch), cultureMatch, null),
    };

    private static int GeneratorCulturePlacementCode(GeneratorProgramCulturePlacement culturePlacement) => culturePlacement switch
    {
        GeneratorProgramCulturePlacement.Ignore => GeneratorCulturePlacementCodes.Ignore,
        GeneratorProgramCulturePlacement.PreferInheritedCulture => GeneratorCulturePlacementCodes.PreferInheritedCulture,
        GeneratorProgramCulturePlacement.HostileIntrusion => GeneratorCulturePlacementCodes.HostileIntrusion,
        GeneratorProgramCulturePlacement.CultureInterior => GeneratorCulturePlacementCodes.CultureInterior,
        GeneratorProgramCulturePlacement.CultureFrontier => GeneratorCulturePlacementCodes.CultureFrontier,
        GeneratorProgramCulturePlacement.CultureRoute => GeneratorCulturePlacementCodes.CultureRoute,
        GeneratorProgramCulturePlacement.Wilderness => GeneratorCulturePlacementCodes.Wilderness,
        _ => throw new ArgumentOutOfRangeException(nameof(culturePlacement), culturePlacement, null),
    };

    private static int GeneratorCoverageModeCode(GeneratorProgramCoverageMode mode) => mode switch
    {
        GeneratorProgramCoverageMode.None => GeneratorCoverageModeCodes.None,
        GeneratorProgramCoverageMode.CreateNew => GeneratorCoverageModeCodes.CreateNew,
        GeneratorProgramCoverageMode.SplitFromExisting => GeneratorCoverageModeCodes.SplitFromExisting,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

}
