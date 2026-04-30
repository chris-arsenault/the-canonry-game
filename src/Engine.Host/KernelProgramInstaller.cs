namespace Engine.Host;

using Engine.Domain;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Kernel;
using Engine.Kernel.Programs;

internal static class KernelProgramInstaller
{
    public static void Install(
        KernelConfigurationContext context,
        KernelProgramSet programs,
        KernelProgramConfigurationContext programConfig)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(programs);
        ArgumentNullException.ThrowIfNull(programConfig);

        InstallMovement(context, programs.Movement);
        InstallLowAltitudeVerbs(context, programs.LowAltitudeVerbs);
        InstallMapActivity(context, programs.MapActivity);
        InstallEffects(context, programs.Effects);
        InstallUpkeep(context, programs.Upkeep);
        InstallProcesses(context, programs.Processes);
        InstallGambits(context, programs.Gambits);
        InstallRelationships(context, programs.Relationships);
        InstallPressures(context, programs.Pressures);
        InstallGenerators(context, programs.Generators);
        InstallDegeneracy(context, programs.Degeneracy);

        context.SetProgramEnabled(
            KernelProgramCodes.Movement,
            programConfig.IsEnabled(KernelProgramCodes.Movement, programs.Movement.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.LowAltitudeVerbs,
            programConfig.IsEnabled(KernelProgramCodes.LowAltitudeVerbs, programs.LowAltitudeVerbs.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.MapActivity,
            programConfig.IsEnabled(KernelProgramCodes.MapActivity, programs.MapActivity.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Effects,
            programConfig.IsEnabled(KernelProgramCodes.Effects, programs.Effects.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Upkeep,
            programConfig.IsEnabled(KernelProgramCodes.Upkeep, programs.Upkeep.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Processes,
            programConfig.IsEnabled(KernelProgramCodes.Processes, programs.Processes.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Gambits,
            programConfig.IsEnabled(KernelProgramCodes.Gambits, programs.Gambits.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Relationships,
            programConfig.IsEnabled(KernelProgramCodes.Relationships, programs.Relationships.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Pressures,
            programConfig.IsEnabled(KernelProgramCodes.Pressures, programs.Pressures.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Generators,
            programConfig.IsEnabled(KernelProgramCodes.Generators, programs.Generators.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Degeneracy,
            programConfig.IsEnabled(KernelProgramCodes.Degeneracy, programs.Degeneracy.EnabledByDefault));
    }

    private static void InstallMovement(KernelConfigurationContext context, MovementProgram program)
    {
        foreach (var rule in program.Rules)
        {
            context.RegisterMovementRule(
                rule.KindId,
                rule.PlaneCode,
                rule.MoveChance,
                rule.ExploreWeight,
                rule.StalenessWeight,
                rule.CrowdingWeight);
        }
    }

    private static void InstallMapActivity(KernelConfigurationContext context, MapActivityProgram program)
    {
        context.ConfigureMapActivity(program.PresenceHeat);
    }

    private static void InstallLowAltitudeVerbs(KernelConfigurationContext context, LowAltitudeVerbProgram program)
    {
        foreach (var verb in program.Verbs)
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
                verb.ExploreWeight,
                verb.StalenessWeight,
                verb.HotspotWeight,
                verb.CrowdingWeight,
                verb.GeneratorAffordanceWeight,
                verb.AffinityRelationshipKindId,
                verb.AffinityRelationshipDirectionCode,
                verb.RelationshipAffinityWeight,
                verb.InfluenceDelta,
                verb.InfluenceRadius,
                verb.CoverageGrowthChance,
                verb.HeadquartersMoveChance,
                verb.RequiredInfluence);
            foreach (var terrainId in verb.TerrainIds)
            {
                context.AddLowAltitudeVerbTerrainFilter(slot, terrainId);
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

    private static void InstallEffects(KernelConfigurationContext context, EffectsProgram program)
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
                effect.DeltaPerTick);
        }
    }

    private static void InstallProcesses(
        KernelConfigurationContext context,
        ProcessProgram program)
    {
        foreach (var objective in program.Objectives)
        {
            InstallProcessObjective(context, objective);
        }

        foreach (var process in program.Processes)
        {
            InstallProcess(context, process);
        }
    }

    private static void InstallProcessObjective(KernelConfigurationContext context, ProcessObjectiveRule objective)
    {
        if (objective.Urgency == ProcessObjectiveUrgency.Fixed)
        {
            context.RegisterProcessFixedObjective(
                objective.KindId,
                EffectScopeCode(objective.EffectScope),
                objective.EffectValueId,
                objective.EffectKindId,
                objective.BaseUrgency);
            return;
        }

        context.RegisterProcessAttributeObjective(
            objective.KindId,
            new AttributeRuntimeId(objective.AttributeId),
            UrgencyCode(objective.Urgency),
            objective.Minimum,
            objective.Maximum,
            objective.DefaultValue,
            objective.Baseline);
    }

    private static void InstallProcess(KernelConfigurationContext context, CompiledProcessRule process)
    {
        var processSlot = context.RegisterProcess(
            process.KindId,
            process.ProcessId,
            process.CooldownTicks,
            process.EntityCooldownTicks,
            TargetModeCode(process.TargetMode),
            process.TargetKindId,
            process.WorkCommitmentTicks);
        InstallProcessGoalEffects(context, processSlot, process);
        InstallProcessPreconditions(context, processSlot, process);
        InstallProcessExpressions(context, processSlot, process);
        InstallProcessProviderSelectors(context, processSlot, process);
        InstallProcessSupportRules(context, processSlot, process);
        InstallProcessSteps(context, processSlot, process);
    }

    private static void InstallProcessGoalEffects(KernelConfigurationContext context, int processSlot, CompiledProcessRule process)
    {
        foreach (var goalEffect in process.GoalEffects)
        {
            context.AddProcessGoalEffect(
                processSlot,
                EffectScopeCode(goalEffect.Scope),
                goalEffect.ValueId,
                goalEffect.KindId);
        }
    }

    private static void InstallProcessPreconditions(KernelConfigurationContext context, int processSlot, CompiledProcessRule process)
    {
        foreach (var precondition in process.Preconditions)
        {
            context.AddProcessPrecondition(
                processSlot,
                PreconditionCode(precondition.Code),
                precondition.A,
                precondition.B,
                precondition.C,
                precondition.D,
                precondition.X,
                precondition.Y);
        }
    }

    private static void InstallProcessExpressions(KernelConfigurationContext context, int processSlot, CompiledProcessRule process)
    {
        foreach (var expression in process.ScoreExpression)
        {
            context.AddProcessScoreExpressionOp(processSlot, ExpressionCode(expression.Code), expression.A, expression.X);
        }

        foreach (var expression in process.RewardExpression)
        {
            context.AddProcessRewardExpressionOp(processSlot, ExpressionCode(expression.Code), expression.A, expression.X);
        }
    }

    private static void InstallProcessProviderSelectors(KernelConfigurationContext context, int processSlot, CompiledProcessRule process)
    {
        foreach (var selector in process.ProviderSelectors)
        {
            context.AddProcessProviderSelector(
                processSlot,
                ProviderSelectorCode(selector.Selector),
                selector.KindId,
                selector.ValueId,
                selector.RelationshipKindId,
                selector.FactId,
                selector.MinimumStrength);
        }
    }

    private static void InstallProcessSupportRules(KernelConfigurationContext context, int processSlot, CompiledProcessRule process)
    {
        foreach (var support in process.SupportRules)
        {
            var supportSlot = context.AddProcessSupportRule(
                processSlot,
                EffectScopeCode(support.EffectScope),
                support.EffectValueId,
                support.EffectKindId,
                support.DesiredMinimum,
                support.UrgencyScale);
            foreach (var expression in support.CurrentValueExpression)
            {
                context.AddProcessSupportExpressionOp(supportSlot, ExpressionCode(expression.Code), expression.A, expression.X);
            }
        }
    }

    private static void InstallProcessSteps(KernelConfigurationContext context, int processSlot, CompiledProcessRule process)
    {
        foreach (var step in process.Steps)
        {
            var stepSlot = context.AddProcessStep(
                processSlot,
                OperationCode(step.Operation),
                step.A,
                step.B,
                step.C,
                step.D,
                step.X,
                step.Y);
            InstallProcessStepExpression(context, stepSlot, step);
        }
    }

    private static void InstallProcessStepExpression(KernelConfigurationContext context, int stepSlot, ProcessProgramStepRule step)
    {
        if (step.Expression is null)
        {
            return;
        }

        foreach (var expression in step.Expression)
        {
            context.AddProcessStepExpressionOp(stepSlot, ExpressionCode(expression.Code), expression.A, expression.X);
        }
    }

    private static void InstallRelationships(KernelConfigurationContext context, RelationshipProgram program)
    {
        foreach (var relationship in program.Relationships)
        {
            context.RegisterRelationshipKind(
                new RelationshipKindRuntimeId(relationship.RelationshipKindId),
                relationship.CullThreshold);
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
        InstallGambitGoalEffects(context, gambitSlot, gambit);
        InstallGambitPreconditions(context, gambitSlot, gambit);
        InstallGambitExpressions(context, gambitSlot, gambit);
        InstallGambitProviderSelectors(context, gambitSlot, gambit);
        InstallGambitSupportRules(context, gambitSlot, gambit);
        InstallGambitSteps(context, gambitSlot, gambit);
    }

    private static void InstallGambitGoalEffects(KernelConfigurationContext context, int gambitSlot, CompiledGambitRule gambit)
    {
        foreach (var goalEffect in gambit.GoalEffects)
        {
            context.AddGambitGoalEffect(gambitSlot, EffectScopeCode(goalEffect.Scope), goalEffect.ValueId, goalEffect.KindId);
        }
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

        foreach (var expression in gambit.RewardExpression)
        {
            context.AddGambitRewardExpressionOp(gambitSlot, ExpressionCode(expression.Code), expression.A, expression.X);
        }
    }

    private static void InstallGambitProviderSelectors(KernelConfigurationContext context, int gambitSlot, CompiledGambitRule gambit)
    {
        foreach (var selector in gambit.ProviderSelectors)
        {
            context.AddGambitProviderSelector(gambitSlot, ProviderSelectorCode(selector.Selector), selector.KindId, selector.ValueId, selector.RelationshipKindId, selector.FactId, selector.MinimumStrength);
        }
    }

    private static void InstallGambitSupportRules(KernelConfigurationContext context, int gambitSlot, CompiledGambitRule gambit)
    {
        foreach (var support in gambit.SupportRules)
        {
            var supportSlot = context.AddGambitSupportRule(gambitSlot, EffectScopeCode(support.EffectScope), support.EffectValueId, support.EffectKindId, support.DesiredMinimum, support.UrgencyScale);
            foreach (var expression in support.CurrentValueExpression)
            {
                context.AddGambitSupportExpressionOp(supportSlot, ExpressionCode(expression.Code), expression.A, expression.X);
            }
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

    private static void InstallGambitStepExpression(KernelConfigurationContext context, int stepSlot, ProcessProgramStepRule step)
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

    private static void InstallPressures(KernelConfigurationContext context, PressureProgram program)
    {
        foreach (var pressure in program.Pressures)
        {
            context.RegisterPressure(
                new PressureRuntimeId(pressure.PressureId),
                PressureScopeCode(pressure.Scope),
                pressure.Baseline,
                pressure.HomeostasisRate,
                pressure.SmoothingFactor);
        }
    }

    private static void InstallDegeneracy(KernelConfigurationContext context, DegeneracyProgram program)
    {
        foreach (var detector in program.Detectors)
        {
            context.RegisterDegeneracyDetector(
                new DetectorRuntimeId(detector.DetectorId),
                DegeneracyMetricCode(detector.Metric),
                DegeneracyThresholdDirectionCode(detector.ThresholdDirection),
                DegeneracySeverityCode(detector.NoSubjectsSeverity),
                new ReasonCodeId(detector.ReasonCodeId),
                detector.PrimaryKindId,
                detector.SecondaryKindId,
                new AttributeRuntimeId(detector.AttributeId),
                new RelationshipKindRuntimeId(detector.RelationshipKindId),
                DegeneracyRelationshipEndpointCode(detector.DominantRelationshipEndpoint),
                detector.WarningThreshold,
                detector.CriticalThreshold);
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
            generator.CadenceTicks,
            generator.TargetKindId,
            generator.PulseWeight,
            generator.MaxRuns,
            generator.TargetAllowWilderness);
        InstallGeneratorTarget(context, generatorSlot, generator);
        InstallGeneratorBindings(context, generatorSlot, generator);
        InstallGeneratorProducedEntities(context, generatorSlot, generator);
        InstallGeneratorRelationships(context, generatorSlot, generator);
    }

    private static void InstallGeneratorTarget(KernelConfigurationContext context, int generatorSlot, CompiledGeneratorRule generator)
    {
        foreach (var subtypeId in generator.TargetSubtypeIds)
        {
            context.AddGeneratorTargetSubtype(generatorSlot, subtypeId);
        }

        if (generator.TargetSaturation is { } saturation)
        {
            context.SetGeneratorTargetSaturation(
                generatorSlot,
                saturation.RelationshipKindId,
                saturation.RequiredFactId,
                GeneratorSaturationDirectionCode(saturation.Direction),
                saturation.MaxExisting);
        }

        foreach (var trigger in generator.Triggers)
        {
            context.AddGeneratorTrigger(
                generatorSlot,
                trigger.KindId,
                trigger.AttributeId,
                GeneratorTriggerDirectionCode(trigger.Direction),
                trigger.Threshold);
        }
    }

    private static void InstallGeneratorBindings(KernelConfigurationContext context, int generatorSlot, CompiledGeneratorRule generator)
    {
        foreach (var binding in generator.SecondaryBindings)
        {
            var query = binding.Query;
            var bindingSlot = context.AddGeneratorSecondaryBinding(
                generatorSlot,
                binding.RefCode,
                binding.KindId,
                query?.AdjacentToTarget == true,
                GeneratorCultureMatchCode(query?.CultureMatch ?? GeneratorProgramCultureMatch.Any));
            if (query is not null)
            {
                InstallGeneratorBindingQuery(context, bindingSlot, query);
            }
        }
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
        var coverage = produced.Coverage;
        var producedSlot = context.AddGeneratorProduced(
            generatorSlot,
            produced.RefCode,
            produced.KindId,
            produced.SubtypeId,
            produced.CreateIfMissing,
            produced.HasQuery,
            QueryAdjacentToTarget(query),
            QueryCultureMatchCode(query),
            GeneratorPlacementCode(produced.PhysicalPlacement),
            GeneratorPlacementCode(produced.SemanticPlacement),
            produced.PlacementRefCode,
            produced.CultureRefCode,
            produced.CreateChance,
            NullableCoverageModeCode(coverage),
            CoverageSourceRefCode(coverage),
            CoverageDesiredCellCount(coverage),
            CoverageMinimumCellCount(coverage),
            CoverageMinimumSourceRemainingCellCount(coverage),
            CoverageRespectTerrainBoundaries(coverage),
            CoverageTransformTerrainId(coverage));
        InstallGeneratorProducedSubtypeChoices(context, producedSlot, produced);
        if (query is not null)
        {
            InstallGeneratorProducedQuery(context, producedSlot, query);
        }

        if (coverage is not null)
        {
            InstallGeneratorProducedCoverage(context, producedSlot, coverage);
        }
    }

    private static bool QueryAdjacentToTarget(GeneratorEntityQueryRule? query) => query is not null && query.AdjacentToTarget;

    private static int QueryCultureMatchCode(GeneratorEntityQueryRule? query) =>
        GeneratorCultureMatchCode(query?.CultureMatch ?? GeneratorProgramCultureMatch.Any);

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
            var (factId0, factId1, factId2, factId3, factCount) = GeneratorRelationshipFacts(relationship.FactIds);
            context.AddGeneratorRelationship(
                generatorSlot,
                relationship.SourceRefCode,
                relationship.DestinationRefCode,
                relationship.RelationshipKindId,
                relationship.Strength,
                relationship.ReplaceExistingOutgoing,
                factId0,
                factId1,
                factId2,
                factId3,
                factCount);
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

        foreach (var condition in query.AttributeConditions)
        {
            context.AddGeneratorBindingAttributeCondition(
                bindingSlot,
                condition.AttributeId,
                GeneratorTriggerDirectionCode(condition.Direction),
                condition.Threshold);
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

        foreach (var condition in query.AttributeConditions)
        {
            context.AddGeneratorProducedQueryAttributeCondition(
                producedSlot,
                condition.AttributeId,
                GeneratorTriggerDirectionCode(condition.Direction),
                condition.Threshold);
        }
    }

    private static int TargetModeCode(ProcessProgramTargetMode mode) => mode switch
    {
        ProcessProgramTargetMode.Self => ProcessTargetModeCodes.Self,
        ProcessProgramTargetMode.AnyOther => ProcessTargetModeCodes.AnyOther,
        ProcessProgramTargetMode.KindOther => ProcessTargetModeCodes.KindOther,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    private static int OperationCode(ProcessProgramOperation operation)
    {
        if (TryAttributeOperationCode(operation, out var code) ||
            TryMovementOperationCode(operation, out code) ||
            TryEntityOperationCode(operation, out code) ||
            TryContextOperationCode(operation, out code))
        {
            return code;
        }

        throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
    }

    private static bool TryAttributeOperationCode(ProcessProgramOperation operation, out int code)
    {
        code = operation switch
        {
            ProcessProgramOperation.None => 0,
            ProcessProgramOperation.AddAttribute => ProcessOperationCodes.AddAttribute,
            ProcessProgramOperation.SetAttribute => ProcessOperationCodes.SetAttribute,
            ProcessProgramOperation.AddTargetAttribute => ProcessOperationCodes.AddTargetAttribute,
            ProcessProgramOperation.TransferAttribute => ProcessOperationCodes.TransferAttribute,
            ProcessProgramOperation.CalculateVariable => ProcessOperationCodes.CalculateVariable,
            ProcessProgramOperation.AddAttributeFromVariable => ProcessOperationCodes.AddAttributeFromVariable,
            ProcessProgramOperation.AddTargetAttributeFromVariable => ProcessOperationCodes.AddTargetAttributeFromVariable,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryMovementOperationCode(ProcessProgramOperation operation, out int code)
    {
        code = operation switch
        {
            ProcessProgramOperation.MovePhysical => ProcessOperationCodes.MovePhysical,
            ProcessProgramOperation.MoveSemantic => ProcessOperationCodes.MoveSemantic,
            ProcessProgramOperation.MoveTowardTargetPhysical => ProcessOperationCodes.MoveTowardTargetPhysical,
            ProcessProgramOperation.WanderPhysical => ProcessOperationCodes.WanderPhysical,
            ProcessProgramOperation.MoveTowardTargetSemantic => ProcessOperationCodes.MoveTowardTargetSemantic,
            ProcessProgramOperation.WanderSemantic => ProcessOperationCodes.WanderSemantic,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryEntityOperationCode(ProcessProgramOperation operation, out int code)
    {
        code = operation switch
        {
            ProcessProgramOperation.SetStatus => ProcessOperationCodes.SetStatus,
            ProcessProgramOperation.CreateRelationship => ProcessOperationCodes.CreateRelationship,
            ProcessProgramOperation.SetRelationshipStrength => ProcessOperationCodes.SetRelationshipStrength,
            ProcessProgramOperation.CreateEntity => ProcessOperationCodes.CreateEntity,
            ProcessProgramOperation.SetHistorical => ProcessOperationCodes.SetHistorical,
            ProcessProgramOperation.AddRelatedEntitiesAttribute => ProcessOperationCodes.AddRelatedEntitiesAttribute,
            ProcessProgramOperation.CreateFixedRelationship => ProcessOperationCodes.CreateFixedRelationship,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryContextOperationCode(ProcessProgramOperation operation, out int code)
    {
        code = operation switch
        {
            ProcessProgramOperation.AddResource => ProcessOperationCodes.AddResource,
            ProcessProgramOperation.AddLocationResource => ProcessOperationCodes.AddLocationResource,
            ProcessProgramOperation.AddPressure => ProcessOperationCodes.AddPressure,
            _ => -1,
        };
        return code >= 0;
    }

    private static int PreconditionCode(ProcessProgramPrecondition precondition)
    {
        if (TryAttributePreconditionCode(precondition, out var code) ||
            TryRelationshipPreconditionCode(precondition, out code) ||
            TryCompositionPreconditionCode(precondition, out code) ||
            TryContextPreconditionCode(precondition, out code))
        {
            return code;
        }

        throw new ArgumentOutOfRangeException(nameof(precondition), precondition, null);
    }

    private static bool TryAttributePreconditionCode(ProcessProgramPrecondition precondition, out int code)
    {
        code = precondition switch
        {
            ProcessProgramPrecondition.TargetIsSelf => ProcessPreconditionCodes.TargetIsSelf,
            ProcessProgramPrecondition.TargetIsNotSelf => ProcessPreconditionCodes.TargetIsNotSelf,
            ProcessProgramPrecondition.SelfAttributeRange => ProcessPreconditionCodes.SelfAttributeRange,
            ProcessProgramPrecondition.TargetAttributeRange => ProcessPreconditionCodes.TargetAttributeRange,
            ProcessProgramPrecondition.Always => ProcessPreconditionCodes.Always,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryRelationshipPreconditionCode(ProcessProgramPrecondition precondition, out int code)
    {
        code = precondition switch
        {
            ProcessProgramPrecondition.SelfHasRelationshipToTarget => ProcessPreconditionCodes.SelfHasRelationshipToTarget,
            ProcessProgramPrecondition.TargetHasRelationshipToSelf => ProcessPreconditionCodes.TargetHasRelationshipToSelf,
            ProcessProgramPrecondition.RelatedEntityMatchesTarget => ProcessPreconditionCodes.RelatedEntityMatchesTarget,
            ProcessProgramPrecondition.RelationshipExists => ProcessPreconditionCodes.RelationshipExists,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryCompositionPreconditionCode(ProcessProgramPrecondition precondition, out int code)
    {
        code = precondition switch
        {
            ProcessProgramPrecondition.And => ProcessPreconditionCodes.And,
            ProcessProgramPrecondition.Or => ProcessPreconditionCodes.Or,
            ProcessProgramPrecondition.Not => ProcessPreconditionCodes.Not,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryContextPreconditionCode(ProcessProgramPrecondition precondition, out int code)
    {
        code = precondition switch
        {
            ProcessProgramPrecondition.PlaneDistance => ProcessPreconditionCodes.PlaneDistance,
            ProcessProgramPrecondition.LocationResourceRange => ProcessPreconditionCodes.LocationResourceRange,
            ProcessProgramPrecondition.PressureRange => ProcessPreconditionCodes.PressureRange,
            ProcessProgramPrecondition.CooldownElapsed => ProcessPreconditionCodes.CooldownElapsed,
            ProcessProgramPrecondition.EntityCountAtLeast => ProcessPreconditionCodes.EntityCountAtLeast,
            ProcessProgramPrecondition.HasColocatedEntity => ProcessPreconditionCodes.HasColocatedEntity,
            _ => -1,
        };
        return code >= 0;
    }

    private static int ExpressionCode(ProcessProgramExpression expression)
    {
        if (TryValueExpressionCode(expression, out var code) ||
            TryOperatorExpressionCode(expression, out code) ||
            TryReferenceExpressionCode(expression, out code))
        {
            return code;
        }

        throw new ArgumentOutOfRangeException(nameof(expression), expression, null);
    }

    private static bool TryValueExpressionCode(ProcessProgramExpression expression, out int code)
    {
        code = expression switch
        {
            ProcessProgramExpression.Literal => ProcessExpressionCodes.Literal,
            ProcessProgramExpression.SelfAttribute => ProcessExpressionCodes.SelfAttribute,
            ProcessProgramExpression.TargetAttribute => ProcessExpressionCodes.TargetAttribute,
            ProcessProgramExpression.SelfLocationResource => ProcessExpressionCodes.SelfLocationResource,
            ProcessProgramExpression.TargetLocationResource => ProcessExpressionCodes.TargetLocationResource,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryOperatorExpressionCode(ProcessProgramExpression expression, out int code)
    {
        code = expression switch
        {
            ProcessProgramExpression.Add => ProcessExpressionCodes.Add,
            ProcessProgramExpression.Subtract => ProcessExpressionCodes.Subtract,
            ProcessProgramExpression.Multiply => ProcessExpressionCodes.Multiply,
            ProcessProgramExpression.Divide => ProcessExpressionCodes.Divide,
            ProcessProgramExpression.Min => ProcessExpressionCodes.Min,
            ProcessProgramExpression.Max => ProcessExpressionCodes.Max,
            ProcessProgramExpression.Clamp => ProcessExpressionCodes.Clamp,
            ProcessProgramExpression.Abs => ProcessExpressionCodes.Abs,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryReferenceExpressionCode(ProcessProgramExpression expression, out int code)
    {
        code = expression switch
        {
            ProcessProgramExpression.Variable => ProcessExpressionCodes.Variable,
            ProcessProgramExpression.Distance => ProcessExpressionCodes.Distance,
            _ => -1,
        };
        return code >= 0;
    }

    private static int EffectScopeCode(ProcessProgramEffectScope scope) => scope switch
    {
        ProcessProgramEffectScope.SelfAttribute => ProcessEffectScopeCodes.SelfAttribute,
        ProcessProgramEffectScope.TargetAttribute => ProcessEffectScopeCodes.TargetAttribute,
        ProcessProgramEffectScope.Resource => ProcessEffectScopeCodes.Resource,
        ProcessProgramEffectScope.Structural => ProcessEffectScopeCodes.Structural,
        ProcessProgramEffectScope.Relational => ProcessEffectScopeCodes.Relational,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null),
    };

    private static int UrgencyCode(ProcessObjectiveUrgency urgency) => urgency switch
    {
        ProcessObjectiveUrgency.StandardNeed => ProcessObjectiveUrgencyCodes.StandardNeed,
        ProcessObjectiveUrgency.Baseline => ProcessObjectiveUrgencyCodes.Baseline,
        ProcessObjectiveUrgency.RangeDefault => ProcessObjectiveUrgencyCodes.RangeDefault,
        _ => throw new ArgumentOutOfRangeException(nameof(urgency), urgency, null),
    };

    private static int ProviderSelectorCode(ProcessProgramProviderSelector selector) => selector switch
    {
        ProcessProgramProviderSelector.TargetAttribute => ProcessProviderSelectorCodes.TargetAttribute,
        ProcessProgramProviderSelector.Structural => ProcessProviderSelectorCodes.Structural,
        ProcessProgramProviderSelector.Resource => ProcessProviderSelectorCodes.Resource,
        ProcessProgramProviderSelector.LocationAttribute => ProcessProviderSelectorCodes.LocationAttribute,
        ProcessProgramProviderSelector.OutgoingRelationship => ProcessProviderSelectorCodes.OutgoingRelationship,
        ProcessProgramProviderSelector.IncomingRelationship => ProcessProviderSelectorCodes.IncomingRelationship,
        _ => throw new ArgumentOutOfRangeException(nameof(selector), selector, null),
    };

    private static int PressureScopeCode(PressureRuntimeScope scope) => scope switch
    {
        PressureRuntimeScope.Coverage => PressureScopeCodes.Coverage,
        PressureRuntimeScope.Entity => PressureScopeCodes.Entity,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null),
    };

    private static int GeneratorSaturationDirectionCode(GeneratorProgramSaturationDirection direction) => direction switch
    {
        GeneratorProgramSaturationDirection.OutgoingFromTarget => GeneratorSaturationDirectionCodes.OutgoingFromTarget,
        GeneratorProgramSaturationDirection.IncomingToTarget => GeneratorSaturationDirectionCodes.IncomingToTarget,
        GeneratorProgramSaturationDirection.Symmetric => GeneratorSaturationDirectionCodes.Symmetric,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    private static int GeneratorPlacementCode(GeneratorProgramPlacement placement) => placement switch
    {
        GeneratorProgramPlacement.On => GeneratorPlacementCodes.On,
        GeneratorProgramPlacement.Near => GeneratorPlacementCodes.Near,
        GeneratorProgramPlacement.Across => GeneratorPlacementCodes.Across,
        GeneratorProgramPlacement.InCoverageLocality => GeneratorPlacementCodes.InCoverageLocality,
        _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, null),
    };

    private static int GeneratorCultureMatchCode(GeneratorProgramCultureMatch cultureMatch) => cultureMatch switch
    {
        GeneratorProgramCultureMatch.Any => GeneratorCultureMatchCodes.Any,
        GeneratorProgramCultureMatch.SameAsTarget => GeneratorCultureMatchCodes.SameAsTarget,
        GeneratorProgramCultureMatch.DifferentFromTarget => GeneratorCultureMatchCodes.DifferentFromTarget,
        _ => throw new ArgumentOutOfRangeException(nameof(cultureMatch), cultureMatch, null),
    };

    private static int GeneratorTriggerDirectionCode(GeneratorProgramTriggerDirection direction) => direction switch
    {
        GeneratorProgramTriggerDirection.High => GeneratorTriggerDirectionCodes.High,
        GeneratorProgramTriggerDirection.Low => GeneratorTriggerDirectionCodes.Low,
        GeneratorProgramTriggerDirection.Critical => GeneratorTriggerDirectionCodes.Critical,
        GeneratorProgramTriggerDirection.Any => GeneratorTriggerDirectionCodes.Any,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    private static int GeneratorCoverageModeCode(GeneratorProgramCoverageMode mode) => mode switch
    {
        GeneratorProgramCoverageMode.None => GeneratorCoverageModeCodes.None,
        GeneratorProgramCoverageMode.CreateNew => GeneratorCoverageModeCodes.CreateNew,
        GeneratorProgramCoverageMode.SplitFromExisting => GeneratorCoverageModeCodes.SplitFromExisting,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    private static int DegeneracyMetricCode(DegeneracyProgramMetric metric) => metric switch
    {
        DegeneracyProgramMetric.AttributeDeclineRatio => DegeneracyMetricCodes.AttributeDeclineRatio,
        DegeneracyProgramMetric.RelationshipEndpointDominanceRatio =>
            DegeneracyMetricCodes.RelationshipEndpointDominanceRatio,
        DegeneracyProgramMetric.ActiveKindCount => DegeneracyMetricCodes.ActiveKindCount,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null),
    };

    private static int DegeneracyThresholdDirectionCode(DegeneracyProgramThresholdDirection direction) =>
        direction switch
        {
            DegeneracyProgramThresholdDirection.HighIsBad => DegeneracyThresholdDirectionCodes.HighIsBad,
            DegeneracyProgramThresholdDirection.LowIsBad => DegeneracyThresholdDirectionCodes.LowIsBad,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };

    private static int DegeneracyRelationshipEndpointCode(DegeneracyProgramRelationshipEndpoint endpoint) =>
        endpoint switch
        {
            DegeneracyProgramRelationshipEndpoint.Source => DegeneracyRelationshipEndpointCodes.Source,
            DegeneracyProgramRelationshipEndpoint.Target => DegeneracyRelationshipEndpointCodes.Target,
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, null),
        };

    private static int DegeneracySeverityCode(DegeneracyProgramSeverity severity) => severity switch
    {
        DegeneracyProgramSeverity.Normal => DegeneracySeverityCodes.Normal,
        DegeneracyProgramSeverity.Warning => DegeneracySeverityCodes.Warning,
        DegeneracyProgramSeverity.Critical => DegeneracySeverityCodes.Critical,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null),
    };

    private static (
        int FactId0,
        int FactId1,
        int FactId2,
        int FactId3,
        int FactCount) GeneratorRelationshipFacts(IReadOnlyList<int> factIds)
    {
        if (factIds.Count > 4)
        {
            throw new InvalidOperationException("Generator relationships support at most four primitive facts.");
        }

        var factId0 = factIds.Count > 0 ? factIds[0] : 0;
        var factId1 = factIds.Count > 1 ? factIds[1] : 0;
        var factId2 = factIds.Count > 2 ? factIds[2] : 0;
        var factId3 = factIds.Count > 3 ? factIds[3] : 0;
        return (factId0, factId1, factId2, factId3, factIds.Count);
    }
}
