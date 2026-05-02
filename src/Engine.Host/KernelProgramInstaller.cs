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
        InstallDegeneracy(context, programs.Degeneracy);

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
            KernelProgramCodes.Degeneracy,
            programConfig.IsEnabled(KernelProgramCodes.Degeneracy, programs.Degeneracy.EnabledByDefault));
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
                relationship.TargetFollowsSourcePhysical);
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
        InstallGeneratorEffects(context, generatorSlot, generator);
    }

    private static void InstallGeneratorEffects(KernelConfigurationContext context, int generatorSlot, CompiledGeneratorRule generator)
    {
        foreach (var effect in generator.Effects)
        {
            context.AddGeneratorEffect(generatorSlot, effect.EffectTypeCode, effect.RefOrKindId);
        }
    }

    private static void InstallGeneratorTarget(KernelConfigurationContext context, int generatorSlot, CompiledGeneratorRule generator)
    {
        foreach (var subtypeId in generator.TargetSubtypeIds)
        {
            context.AddGeneratorTargetSubtype(generatorSlot, subtypeId);
        }

        foreach (var relationshipKindId in generator.TargetRelationshipRequirements.RequiredOutgoingRelationshipKindIds)
        {
            context.AddGeneratorTargetRequiredOutgoingRelationship(generatorSlot, relationshipKindId);
        }

        foreach (var relationshipKindId in generator.TargetRelationshipRequirements.RequiredIncomingRelationshipKindIds)
        {
            context.AddGeneratorTargetRequiredIncomingRelationship(generatorSlot, relationshipKindId);
        }

        foreach (var relationshipKindId in generator.TargetRelationshipRequirements.RequiredRelationshipKindIds)
        {
            context.AddGeneratorTargetRequiredRelationship(generatorSlot, relationshipKindId);
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
                trigger.KindId);
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
            TryMapOperationCode(operation, out code) ||
            TryContextOperationCode(operation, out code))
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
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryMapOperationCode(BehaviorProgramOperation operation, out int code)
    {
        code = operation switch
        {
            BehaviorProgramOperation.SetTargetCoverageTerrain => BehaviorOperationCodes.SetTargetCoverageTerrain,
            _ => -1,
        };
        return code >= 0;
    }

    private static bool TryContextOperationCode(BehaviorProgramOperation operation, out int code)
    {
        code = operation switch
        {
            BehaviorProgramOperation.AddResource => BehaviorOperationCodes.AddResource,
            BehaviorProgramOperation.AddLocationResource => BehaviorOperationCodes.AddLocationResource,
            _ => -1,
        };
        return code >= 0;
    }

    private static int PreconditionCode(BehaviorProgramPrecondition precondition)
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
            BehaviorProgramPrecondition.LocationResourceRange => BehaviorPreconditionCodes.LocationResourceRange,
            BehaviorProgramPrecondition.CooldownElapsed => BehaviorPreconditionCodes.CooldownElapsed,
            BehaviorProgramPrecondition.EntityCountAtLeast => BehaviorPreconditionCodes.EntityCountAtLeast,
            BehaviorProgramPrecondition.HasColocatedEntity => BehaviorPreconditionCodes.HasColocatedEntity,
            BehaviorProgramPrecondition.ContestedInfluence => BehaviorPreconditionCodes.ContestedInfluence,
            BehaviorProgramPrecondition.SharedInfluence => BehaviorPreconditionCodes.SharedInfluence,
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
            BehaviorProgramExpression.SelfLocationResource => BehaviorExpressionCodes.SelfLocationResource,
            BehaviorProgramExpression.TargetLocationResource => BehaviorExpressionCodes.TargetLocationResource,
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
            _ => -1,
        };
        return code >= 0;
    }

    private static int EffectScopeCode(BehaviorProgramEffectScope scope) => scope switch
    {
        BehaviorProgramEffectScope.SelfAttribute => BehaviorEffectScopeCodes.SelfAttribute,
        BehaviorProgramEffectScope.TargetAttribute => BehaviorEffectScopeCodes.TargetAttribute,
        BehaviorProgramEffectScope.Resource => BehaviorEffectScopeCodes.Resource,
        BehaviorProgramEffectScope.Structural => BehaviorEffectScopeCodes.Structural,
        BehaviorProgramEffectScope.Relational => BehaviorEffectScopeCodes.Relational,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null),
    };

    private static int ProviderSelectorCode(BehaviorProgramProviderSelector selector) => selector switch
    {
        BehaviorProgramProviderSelector.TargetAttribute => BehaviorProviderSelectorCodes.TargetAttribute,
        BehaviorProgramProviderSelector.Structural => BehaviorProviderSelectorCodes.Structural,
        BehaviorProgramProviderSelector.Resource => BehaviorProviderSelectorCodes.Resource,
        BehaviorProgramProviderSelector.LocationAttribute => BehaviorProviderSelectorCodes.LocationAttribute,
        BehaviorProgramProviderSelector.OutgoingRelationship => BehaviorProviderSelectorCodes.OutgoingRelationship,
        BehaviorProgramProviderSelector.IncomingRelationship => BehaviorProviderSelectorCodes.IncomingRelationship,
        _ => throw new ArgumentOutOfRangeException(nameof(selector), selector, null),
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
