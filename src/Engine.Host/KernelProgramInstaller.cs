namespace Engine.Host;

using Engine.Domain;
using Engine.Domain.Compilation;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Kernel;
using Engine.Kernel.Programs;

internal static class KernelProgramInstaller
{
    public static void Install(
        KernelConfigurationContext context,
        CompiledDomain compiledDomain,
        KernelProgramConfigurationContext programConfig)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(compiledDomain);
        ArgumentNullException.ThrowIfNull(programConfig);

        InstallEntityData(context, compiledDomain.Programs.EntityData);
        InstallUpkeep(context, compiledDomain.Programs.Upkeep);
        InstallProcesses(context, compiledDomain.Programs.Processes);
        InstallRelationships(context, compiledDomain.Programs.Relationships);
        InstallTerrain(context, compiledDomain.Programs.Terrain);
        InstallPressures(context, compiledDomain.Programs.Pressures);
        InstallGenerators(context, compiledDomain.Programs.Generators);
        InstallDegeneracy(context, compiledDomain.Programs.Degeneracy);

        context.SetProgramEnabled(
            KernelProgramCodes.Upkeep,
            programConfig.IsEnabled(KernelProgramCodes.Upkeep, compiledDomain.Programs.Upkeep.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Processes,
            programConfig.IsEnabled(KernelProgramCodes.Processes, compiledDomain.Programs.Processes.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Relationships,
            programConfig.IsEnabled(KernelProgramCodes.Relationships, compiledDomain.Programs.Relationships.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Pressures,
            programConfig.IsEnabled(KernelProgramCodes.Pressures, compiledDomain.Programs.Pressures.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Generators,
            programConfig.IsEnabled(KernelProgramCodes.Generators, compiledDomain.Programs.Generators.EnabledByDefault));
        context.SetProgramEnabled(
            KernelProgramCodes.Degeneracy,
            programConfig.IsEnabled(KernelProgramCodes.Degeneracy, compiledDomain.Programs.Degeneracy.EnabledByDefault));
    }

    private static void InstallEntityData(KernelConfigurationContext context, EntityDataProgram program)
    {
        foreach (var attribute in program.Attributes)
        {
            context.RegisterAttribute(
                attribute.KindId,
                new AttributeRuntimeId(attribute.AttributeId),
                attribute.Minimum,
                attribute.Maximum,
                attribute.DefaultValue,
                attribute.DefaultVariance);
        }
    }

    private static void InstallUpkeep(KernelConfigurationContext context, UpkeepProgram program)
    {
        foreach (var rule in program.AttributeRules)
        {
            context.RegisterAttributeUpkeep(
                rule.KindId,
                new AttributeRuntimeId(rule.AttributeId),
                rule.DriftRate,
                rule.LocationResourceId,
                rule.LocationResourceDriftPerUnit,
                rule.LocationResourceDriftMinOffset,
                rule.LocationResourceDriftMaxOffset,
                rule.CriticalTargetAttributeId,
                rule.CriticalThreshold,
                rule.CriticalDelta);
        }

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

    private static void InstallProcesses(
        KernelConfigurationContext context,
        ProcessProgram program)
    {
        foreach (var objective in program.Objectives)
        {
            if (objective.Urgency == ProcessObjectiveUrgency.Fixed)
            {
                context.RegisterProcessFixedObjective(
                    objective.KindId,
                    EffectScopeCode(objective.EffectScope),
                    objective.EffectValueId,
                    objective.EffectKindId,
                    objective.BaseUrgency);
                continue;
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

        foreach (var process in program.Processes)
        {
            var processSlot = context.RegisterProcess(
                process.KindId,
                process.ProcessId,
                process.CooldownTicks,
                process.EntityCooldownTicks,
                TargetModeCode(process.TargetMode),
                process.TargetKindId);

            foreach (var goalEffect in process.GoalEffects)
            {
                context.AddProcessGoalEffect(
                    processSlot,
                    EffectScopeCode(goalEffect.Scope),
                    goalEffect.ValueId,
                    goalEffect.KindId);
            }

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

            foreach (var expression in process.ScoreExpression)
            {
                context.AddProcessScoreExpressionOp(
                    processSlot,
                    ExpressionCode(expression.Code),
                    expression.A,
                    expression.X);
            }

            foreach (var expression in process.RewardExpression)
            {
                context.AddProcessRewardExpressionOp(
                    processSlot,
                    ExpressionCode(expression.Code),
                    expression.A,
                    expression.X);
            }

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
                    context.AddProcessSupportExpressionOp(
                        supportSlot,
                        ExpressionCode(expression.Code),
                        expression.A,
                        expression.X);
                }
            }

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
                if (step.Expression is null)
                {
                    continue;
                }

                foreach (var expression in step.Expression)
                {
                    context.AddProcessStepExpressionOp(
                        stepSlot,
                        ExpressionCode(expression.Code),
                        expression.A,
                        expression.X);
                }
            }
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
    }

    private static void InstallTerrain(KernelConfigurationContext context, TerrainProgram program)
    {
        foreach (var terrain in program.Terrains)
        {
            context.RegisterTerrain(
                new TerrainRuntimeId(terrain.TerrainId),
                terrain.IsPassable,
                terrain.BoundaryGroupId);
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
            var generatorSlot = context.RegisterGenerator(
                generator.GeneratorId,
                generator.CadenceTicks,
                generator.TargetKindId,
                generator.PulseWeight,
                generator.MaxRuns,
                generator.TargetAllowWilderness);

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

            foreach (var produced in generator.ProducedEntities)
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
                    query?.AdjacentToTarget == true,
                    GeneratorCultureMatchCode(query?.CultureMatch ?? GeneratorProgramCultureMatch.Any),
                    GeneratorPlacementCode(produced.PhysicalPlacement),
                    GeneratorPlacementCode(produced.SemanticPlacement),
                    produced.PlacementRefCode,
                    produced.CultureRefCode,
                    produced.CreateChance,
                    GeneratorCoverageModeCode(coverage?.Mode ?? GeneratorProgramCoverageMode.None),
                    coverage?.SourceRefCode ?? 0,
                    coverage?.DesiredCellCount ?? 0,
                    coverage?.MinimumCellCount ?? 0,
                    coverage?.MinimumSourceRemainingCellCount ?? 0,
                    coverage?.RespectTerrainBoundaries ?? true,
                    coverage?.TransformTerrainId ?? 0,
                    coverage?.TransformPassable ?? false);

                foreach (var subtypeId in produced.SubtypeChoiceIds)
                {
                    context.AddGeneratorProducedSubtypeChoice(producedSlot, subtypeId);
                }

                if (query is not null)
                {
                    InstallGeneratorProducedQuery(context, producedSlot, query);
                }

                if (coverage is not null)
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
            }

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

    private static int OperationCode(ProcessProgramOperation operation) => operation switch
    {
        ProcessProgramOperation.None => 0,
        ProcessProgramOperation.AddAttribute => ProcessOperationCodes.AddAttribute,
        ProcessProgramOperation.SetAttribute => ProcessOperationCodes.SetAttribute,
        ProcessProgramOperation.MovePhysical => ProcessOperationCodes.MovePhysical,
        ProcessProgramOperation.MoveSemantic => ProcessOperationCodes.MoveSemantic,
        ProcessProgramOperation.AddResource => ProcessOperationCodes.AddResource,
        ProcessProgramOperation.SetStatus => ProcessOperationCodes.SetStatus,
        ProcessProgramOperation.CreateRelationship => ProcessOperationCodes.CreateRelationship,
        ProcessProgramOperation.SetRelationshipStrength => ProcessOperationCodes.SetRelationshipStrength,
        ProcessProgramOperation.AddTargetAttribute => ProcessOperationCodes.AddTargetAttribute,
        ProcessProgramOperation.TransferAttribute => ProcessOperationCodes.TransferAttribute,
        ProcessProgramOperation.AddLocationResource => ProcessOperationCodes.AddLocationResource,
        ProcessProgramOperation.HexToHealth => ProcessOperationCodes.HexToHealth,
        ProcessProgramOperation.MoveTowardTargetPhysical => ProcessOperationCodes.MoveTowardTargetPhysical,
        ProcessProgramOperation.WanderPhysical => ProcessOperationCodes.WanderPhysical,
        ProcessProgramOperation.MoveTowardTargetSemantic => ProcessOperationCodes.MoveTowardTargetSemantic,
        ProcessProgramOperation.WanderSemantic => ProcessOperationCodes.WanderSemantic,
        ProcessProgramOperation.CreateEntity => ProcessOperationCodes.CreateEntity,
        ProcessProgramOperation.AddPressure => ProcessOperationCodes.AddPressure,
        ProcessProgramOperation.SetEntityTag => ProcessOperationCodes.SetEntityTag,
        ProcessProgramOperation.SetHistorical => ProcessOperationCodes.SetHistorical,
        ProcessProgramOperation.AddRelatedEntitiesAttribute => ProcessOperationCodes.AddRelatedEntitiesAttribute,
        ProcessProgramOperation.CalculateVariable => ProcessOperationCodes.CalculateVariable,
        ProcessProgramOperation.AddAttributeFromVariable => ProcessOperationCodes.AddAttributeFromVariable,
        ProcessProgramOperation.AddTargetAttributeFromVariable => ProcessOperationCodes.AddTargetAttributeFromVariable,
        ProcessProgramOperation.CreateFixedRelationship => ProcessOperationCodes.CreateFixedRelationship,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
    };

    private static int PreconditionCode(ProcessProgramPrecondition precondition) => precondition switch
    {
        ProcessProgramPrecondition.TargetIsSelf => ProcessPreconditionCodes.TargetIsSelf,
        ProcessProgramPrecondition.TargetIsNotSelf => ProcessPreconditionCodes.TargetIsNotSelf,
        ProcessProgramPrecondition.SelfAttributeRange => ProcessPreconditionCodes.SelfAttributeRange,
        ProcessProgramPrecondition.TargetAttributeRange => ProcessPreconditionCodes.TargetAttributeRange,
        ProcessProgramPrecondition.PlaneDistance => ProcessPreconditionCodes.PlaneDistance,
        ProcessProgramPrecondition.LocationResourceRange => ProcessPreconditionCodes.LocationResourceRange,
        ProcessProgramPrecondition.Always => ProcessPreconditionCodes.Always,
        ProcessProgramPrecondition.SelfHasRelationshipToTarget => ProcessPreconditionCodes.SelfHasRelationshipToTarget,
        ProcessProgramPrecondition.TargetHasRelationshipToSelf => ProcessPreconditionCodes.TargetHasRelationshipToSelf,
        ProcessProgramPrecondition.RelatedEntityMatchesTarget => ProcessPreconditionCodes.RelatedEntityMatchesTarget,
        ProcessProgramPrecondition.RelationshipExists => ProcessPreconditionCodes.RelationshipExists,
        ProcessProgramPrecondition.PressureRange => ProcessPreconditionCodes.PressureRange,
        ProcessProgramPrecondition.CooldownElapsed => ProcessPreconditionCodes.CooldownElapsed,
        ProcessProgramPrecondition.And => ProcessPreconditionCodes.And,
        ProcessProgramPrecondition.Or => ProcessPreconditionCodes.Or,
        ProcessProgramPrecondition.Not => ProcessPreconditionCodes.Not,
        ProcessProgramPrecondition.EntityCountAtLeast => ProcessPreconditionCodes.EntityCountAtLeast,
        ProcessProgramPrecondition.HasColocatedEntity => ProcessPreconditionCodes.HasColocatedEntity,
        _ => throw new ArgumentOutOfRangeException(nameof(precondition), precondition, null),
    };

    private static int ExpressionCode(ProcessProgramExpression expression) => expression switch
    {
        ProcessProgramExpression.Literal => ProcessExpressionCodes.Literal,
        ProcessProgramExpression.SelfAttribute => ProcessExpressionCodes.SelfAttribute,
        ProcessProgramExpression.TargetAttribute => ProcessExpressionCodes.TargetAttribute,
        ProcessProgramExpression.SelfLocationResource => ProcessExpressionCodes.SelfLocationResource,
        ProcessProgramExpression.TargetLocationResource => ProcessExpressionCodes.TargetLocationResource,
        ProcessProgramExpression.Add => ProcessExpressionCodes.Add,
        ProcessProgramExpression.Subtract => ProcessExpressionCodes.Subtract,
        ProcessProgramExpression.Multiply => ProcessExpressionCodes.Multiply,
        ProcessProgramExpression.Divide => ProcessExpressionCodes.Divide,
        ProcessProgramExpression.Min => ProcessExpressionCodes.Min,
        ProcessProgramExpression.Max => ProcessExpressionCodes.Max,
        ProcessProgramExpression.Clamp => ProcessExpressionCodes.Clamp,
        ProcessProgramExpression.Abs => ProcessExpressionCodes.Abs,
        ProcessProgramExpression.Variable => ProcessExpressionCodes.Variable,
        ProcessProgramExpression.Distance => ProcessExpressionCodes.Distance,
        _ => throw new ArgumentOutOfRangeException(nameof(expression), expression, null),
    };

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
