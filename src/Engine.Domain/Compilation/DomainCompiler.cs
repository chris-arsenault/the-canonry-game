namespace Engine.Domain.Compilation;

using Engine.Domain.Compilation.KernelData;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Gambits;
using Engine.Kinds;
using Engine.Processes;
using Engine.Resources;
using Engine.Wiring;
using Engine.WorldGen;

public static partial class DomainCompiler
{
    public static CompiledDomain Compile(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var ids = BuildCatalog(config);
        var graph = DefinitionGraphBuilder.Build(config, ids);

        return new CompiledDomain
        {
            Config = config,
            Ids = ids,
            DefinitionGraph = graph,
            RuntimeCodes = new DomainRuntimeCodes(
                ids.EventCodeIdOf(DomainRuntimeNames.RelationshipCreatedCode),
                ids.EventCodeIdOf(DomainRuntimeNames.RelationshipCulledCode),
                ids.EventCodeIdOf(DomainRuntimeNames.RelationshipStrengthChangedCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.ProcessSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.GambitSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.UpkeepSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.StartupSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.RelationshipSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.GeneratorSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.PressureSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.DegeneracySourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.PlanningSourceCode)),
            KernelData = BuildKernelData(config, ids),
            Programs = BuildPrograms(config, ids, graph),
            Symbols = BuildSymbols(config, ids),
        };
    }

    private static CompiledEntityData BuildEntityData(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var attributes = new List<CompiledEntityAttributeData>();
        foreach (var (kind, kindConfig) in config.Kinds)
        {
            var kindId = ids.KindIdOf(kind).Value;
            foreach (var attribute in kindConfig.Attributes)
            {
                attributes.Add(new CompiledEntityAttributeData(
                    kindId,
                    ids.AttributeIdOf(attribute.Name).Value,
                    attribute.Min,
                    attribute.Max,
                    attribute.Default,
                    attribute.DefaultVariance));
            }
        }

        return new CompiledEntityData { Attributes = attributes };
    }

    private static UpkeepProgram BuildUpkeepProgram(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var attributeRules = new List<AttributeUpkeepRule>();
        var lifecycleRules = new List<LifecycleUpkeepRule>();

        foreach (var (kind, kindConfig) in config.Kinds)
        {
            var kindId = ids.KindIdOf(kind).Value;
            foreach (var attribute in kindConfig.Attributes)
            {
                var locationResourceId = attribute.LocationResourceDriftResource is { } resourceType
                    ? ids.ResourceIdOf(resourceType.ToDomainName()).Value
                    : 0;
                var criticalTargetAttributeId = attribute.CriticalEffect is { } criticalEffect
                    ? ids.AttributeIdOf(criticalEffect.TargetAttribute).Value
                    : 0;

                if (attribute.DriftRate == 0f &&
                    locationResourceId == 0 &&
                    criticalTargetAttributeId == 0)
                {
                    continue;
                }

                attributeRules.Add(new AttributeUpkeepRule(
                    kindId,
                    ids.AttributeIdOf(attribute.Name).Value,
                    attribute.DriftRate,
                    locationResourceId,
                    attribute.LocationResourceDriftPerUnit,
                    attribute.LocationResourceDriftMinOffset,
                    attribute.LocationResourceDriftMaxOffset,
                    criticalTargetAttributeId,
                    StandardNeedScale.Critical,
                    attribute.CriticalEffect?.DeltaPerTick ?? 0f));
            }
        }

        foreach (var (kind, lifecycle) in config.Lifecycles)
        {
            lifecycleRules.Add(new LifecycleUpkeepRule(
                ids.KindIdOf(kind).Value,
                ids.AttributeIdOf(lifecycle.HealthAttribute).Value,
                lifecycle.BaseRate,
                lifecycle.AgeCoefficient));
        }

        return new UpkeepProgram
        {
            AttributeRules = attributeRules,
            LifecycleRules = lifecycleRules,
        };
    }

    private static ProcessProgram BuildProcessProgram(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        return new ProcessProgram
        {
            Processes = BuildProcessRules(config, ids),
            Objectives = BuildProcessObjectives(config, ids),
        };
    }

    private static List<CompiledProcessRule> BuildProcessRules(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var processes = new List<CompiledProcessRule>();
        foreach (var process in config.Processes)
        {
            var targetKindId = ResolveTargetKindId(process, ids);
            processes.Add(new CompiledProcessRule(
                ids.KindIdOf(process.TargetKind).Value,
                ids.ProcessIdOf(process.Id).Value,
                process.Cooldown,
                process.EntityCooldown,
                ResolveTargetMode(process, targetKindId),
                targetKindId,
                CompileGoalEffects(process, ids),
                CompileProviderSelectors(process, ids),
                CompileSupportRules(process, ids),
                CompilePreconditions(process.Precondition, ids),
                CompileExpression(process.ScoreExpression, ids),
                CompileExpression(process.RewardExpression, ids),
                CompileSteps(process, ids)));
        }

        return processes;
    }

    private static GambitProgram BuildGambitProgram(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var gambits = new List<CompiledGambitRule>();
        foreach (var gambit in config.Gambits)
        {
            var targetKindId = ResolveTargetKindId(gambit, ids);
            gambits.Add(new CompiledGambitRule(
                ids.KindIdOf(gambit.TargetKind).Value,
                ids.GambitIdOf(gambit.Id).Value,
                gambit.Cooldown,
                gambit.EntityCooldown,
                ResolveTargetMode(gambit, targetKindId),
                targetKindId,
                gambit.SuccessChance,
                CompileGoalEffects(gambit, ids),
                CompileProviderSelectors(gambit, ids),
                CompileSupportRules(gambit, ids),
                CompilePreconditions(gambit.Precondition, ids),
                CompileExpression(gambit.ScoreExpression, ids),
                CompileExpression(gambit.RewardExpression, ids),
                CompileSteps(gambit, ids)));
        }

        return new GambitProgram
        {
            Gambits = gambits,
        };
    }

    private static List<ProcessObjectiveRule> BuildProcessObjectives(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var objectives = new List<ProcessObjectiveRule>();
        foreach (var (kind, kindConfig) in config.Kinds)
        {
            var kindId = ids.KindIdOf(kind).Value;
            foreach (var attribute in kindConfig.Attributes)
            {
                if (!AttributeCreatesObjective(attribute))
                {
                    continue;
                }

                objectives.Add(new ProcessObjectiveRule(
                    kindId,
                    ProcessProgramEffectScope.SelfAttribute,
                    ids.AttributeIdOf(attribute.Name).Value,
                    kindId,
                    ObjectiveUrgency(attribute),
                    ids.AttributeIdOf(attribute.Name).Value,
                    attribute.Min,
                    attribute.Max,
                    attribute.Default,
                    attribute.Baseline,
                    0f));
            }

            foreach (var standingDrive in kindConfig.StandingDrives)
            {
                if (standingDrive.BaseUrgency < 0.01f)
                {
                    continue;
                }

                objectives.Add(new ProcessObjectiveRule(
                    kindId,
                    EffectScope(standingDrive.DesiredEffect),
                    EffectValueId(standingDrive.DesiredEffect, ids),
                    standingDrive.DesiredEffect.Kind is { } effectKind
                        ? ids.KindIdOf(effectKind).Value
                        : 0,
                    ProcessObjectiveUrgency.Fixed,
                    0,
                    0f,
                    0f,
                    0f,
                    0f,
                    standingDrive.BaseUrgency));
            }
        }

        return objectives;
    }

    private static int ResolveTargetKindId(BehaviorDefinition process, DomainIdRegistry ids)
    {
        foreach (var selector in process.ProviderSelectors)
        {
            if (selector.Kind is { } kind)
            {
                return ids.KindIdOf(kind).Value;
            }
        }

        foreach (var effect in process.GoalEffects)
        {
            if (effect.Scope == RefScope.Target && effect.Kind is { } kind)
            {
                return ids.KindIdOf(kind).Value;
            }
        }

        return 0;
    }

    private static ProcessProgramTargetMode ResolveTargetMode(BehaviorDefinition process, int targetKindId)
    {
        if (ContainsPrecondition<TargetIsSelf>(process.Precondition))
        {
            return ProcessProgramTargetMode.Self;
        }

        if (ContainsPrecondition<TargetIsNotSelf>(process.Precondition))
        {
            return targetKindId > 0
                ? ProcessProgramTargetMode.KindOther
                : ProcessProgramTargetMode.AnyOther;
        }

        return ProcessProgramTargetMode.Self;
    }

    private static List<ProcessProgramEffectRule> CompileGoalEffects(
        BehaviorDefinition process,
        DomainIdRegistry ids)
    {
        var effects = new List<ProcessProgramEffectRule>();
        foreach (var effect in process.GoalEffects)
        {
            effects.Add(new ProcessProgramEffectRule(
                EffectScope(effect),
                EffectValueId(effect, ids),
                effect.Kind is { } kind ? ids.KindIdOf(kind).Value : 0));
        }

        return effects;
    }

    private static List<ProcessProgramProviderSelectorRule> CompileProviderSelectors(
        BehaviorDefinition process,
        DomainIdRegistry ids)
    {
        var selectors = new List<ProcessProgramProviderSelectorRule>();
        foreach (var selector in process.ProviderSelectors)
        {
            selectors.Add(new ProcessProgramProviderSelectorRule(
                ProviderSelectorCode(selector),
                selector.Kind is { } kind ? ids.KindIdOf(kind).Value : 0,
                ProviderSelectorValueId(selector, ids),
                ProviderSelectorRelationshipKindId(selector, ids),
                ProviderSelectorFactId(selector, ids),
                0f));
        }

        return selectors;
    }

    private static List<ProcessProgramSupportRule> CompileSupportRules(
        BehaviorDefinition process,
        DomainIdRegistry ids)
    {
        var supportRules = new List<ProcessProgramSupportRule>();
        foreach (var rule in process.SupportRules)
        {
            supportRules.Add(new ProcessProgramSupportRule(
                EffectScope(rule.DesiredEffect),
                EffectValueId(rule.DesiredEffect, ids),
                rule.DesiredEffect.Kind is { } kind ? ids.KindIdOf(kind).Value : 0,
                rule.DesiredMinimum,
                rule.UrgencyScale,
                CompileExpression(rule.CurrentValueExpression, ids)));
        }

        return supportRules;
    }

    private static ProcessProgramProviderSelector ProviderSelectorCode(AttributeRef selector) => selector.Scope switch
    {
        RefScope.Target => ProcessProgramProviderSelector.TargetAttribute,
        RefScope.Structural => ProcessProgramProviderSelector.Structural,
        RefScope.Resource => ProcessProgramProviderSelector.Resource,
        RefScope.Location => ProcessProgramProviderSelector.LocationAttribute,
        RefScope.Related or RefScope.Relational => ProcessProgramProviderSelector.OutgoingRelationship,
        _ => throw new InvalidOperationException($"Unsupported kernel provider selector scope '{selector.Scope}'."),
    };

    private static int ProviderSelectorValueId(AttributeRef selector, DomainIdRegistry ids) => selector.Scope switch
    {
        RefScope.Target or RefScope.Location or RefScope.Related => ids.AttributeIdOf(selector.Name).Value,
        RefScope.Structural => ids.StructuralFactIdOf(selector.Name).Value,
        RefScope.Resource => ids.ResourceIdOf(selector.Name).Value,
        _ => 0,
    };

    private static int ProviderSelectorRelationshipKindId(AttributeRef selector, DomainIdRegistry ids) =>
        selector.Scope is RefScope.Related or RefScope.Relational
            ? ids.RelationshipKindIdOf(selector.RelationshipKind ?? selector.Name).Value
            : 0;

    private static int ProviderSelectorFactId(AttributeRef selector, DomainIdRegistry ids)
    {
        if (selector.Scope != RefScope.Relational ||
            string.IsNullOrWhiteSpace(selector.Name) ||
            string.Equals(selector.Name, "matched", StringComparison.Ordinal))
        {
            return 0;
        }

        return ids.RelationshipFactIdOf(selector.Name).Value;
    }

    private static List<ProcessProgramPreconditionOp> CompilePreconditions(
        IPrecondition precondition,
        DomainIdRegistry ids)
    {
        var ops = new List<ProcessProgramPreconditionOp>();
        CompilePreconditionExpression(ops, precondition, ids);
        return ops;
    }

    private static void CompilePreconditionExpression(
        List<ProcessProgramPreconditionOp> ops,
        IPrecondition precondition,
        DomainIdRegistry ids)
    {
        if (TryCompileCompositePrecondition(ops, precondition, ids) ||
            TryCompileSimplePrecondition(ops, precondition) ||
            TryCompileRangePrecondition(ops, precondition, ids) ||
            TryCompileRelationshipPrecondition(ops, precondition, ids))
        {
            return;
        }

        throw new InvalidOperationException($"Unsupported kernel process precondition '{precondition.GetType().Name}'.");
    }

    private static bool TryCompileCompositePrecondition(
        List<ProcessProgramPreconditionOp> ops,
        IPrecondition precondition,
        DomainIdRegistry ids)
    {
        switch (precondition)
        {
            case AndPrecondition and:
                CompilePreconditionChildren(ops, and.Children, ids, ProcessProgramPrecondition.And);
                return true;
            case OrPrecondition or:
                CompilePreconditionChildren(ops, or.Children, ids, ProcessProgramPrecondition.Or);
                return true;
            case NotPrecondition not:
                CompilePreconditionExpression(ops, not.Inner, ids);
                ops.Add(new ProcessProgramPreconditionOp(ProcessProgramPrecondition.Not));
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileSimplePrecondition(
        List<ProcessProgramPreconditionOp> ops,
        IPrecondition precondition)
    {
        switch (precondition)
        {
            case AlwaysTrue:
                ops.Add(new ProcessProgramPreconditionOp(ProcessProgramPrecondition.Always));
                return true;
            case TargetIsSelf:
                ops.Add(new ProcessProgramPreconditionOp(ProcessProgramPrecondition.TargetIsSelf));
                return true;
            case TargetIsNotSelf:
                ops.Add(new ProcessProgramPreconditionOp(ProcessProgramPrecondition.TargetIsNotSelf));
                return true;
            case HasColocatedNamedEntityPrecondition:
                ops.Add(new ProcessProgramPreconditionOp(ProcessProgramPrecondition.HasColocatedEntity));
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileRangePrecondition(
        List<ProcessProgramPreconditionOp> ops,
        IPrecondition precondition,
        DomainIdRegistry ids)
    {
        switch (precondition)
        {
            case SelfAttributeThreshold self:
                AddAttributeRangePrecondition(ops, self.AttributeName, self.Min, self.Max, ids, ProcessProgramPrecondition.SelfAttributeRange);
                return true;
            case TargetAttributeThreshold target:
                AddAttributeRangePrecondition(ops, target.AttributeName, target.Min, target.Max, ids, ProcessProgramPrecondition.TargetAttributeRange);
                return true;
            case WithinPlaneDistancePrecondition distance:
                AddPlaneDistancePrecondition(ops, distance, ids);
                return true;
            case LocationResourceThreshold resource:
                AddLocationResourcePrecondition(ops, resource, ids);
                return true;
            case PressureThreshold pressure:
                AddPressurePrecondition(ops, pressure, ids);
                return true;
            case CooldownCheck cooldown:
                ops.Add(new ProcessProgramPreconditionOp(ProcessProgramPrecondition.CooldownElapsed, cooldown.MinimumTicksSinceFire));
                return true;
            case EntityCountCheck count:
                ops.Add(new ProcessProgramPreconditionOp(ProcessProgramPrecondition.EntityCountAtLeast, ids.KindIdOf(count.Kind).Value, count.Min));
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileRelationshipPrecondition(
        List<ProcessProgramPreconditionOp> ops,
        IPrecondition precondition,
        DomainIdRegistry ids)
    {
        switch (precondition)
        {
            case SelfHasRelationshipToTarget relationship:
                AddRelationshipStrengthPrecondition(ops, relationship.RelationshipKind, relationship.MinStrength, ids, ProcessProgramPrecondition.SelfHasRelationshipToTarget);
                return true;
            case TargetHasRelationshipToSelf relationship:
                AddRelationshipStrengthPrecondition(ops, relationship.RelationshipKind, relationship.MinStrength, ids, ProcessProgramPrecondition.TargetHasRelationshipToSelf);
                return true;
            case RelatedEntityMatchesTarget relationship:
                ops.Add(new ProcessProgramPreconditionOp(
                    ProcessProgramPrecondition.RelatedEntityMatchesTarget,
                    ids.RelationshipKindIdOf(relationship.RelationshipKind).Value));
                return true;
            case RelationshipExists relationship:
                AddRelationshipExistsPrecondition(ops, relationship, ids);
                return true;
            default:
                return false;
        }
    }

    private static void AddAttributeRangePrecondition(
        List<ProcessProgramPreconditionOp> ops,
        string attributeName,
        float? min,
        float? max,
        DomainIdRegistry ids,
        ProcessProgramPrecondition operatorCode)
    {
        ops.Add(new ProcessProgramPreconditionOp(
            operatorCode,
            ids.AttributeIdOf(attributeName).Value,
            min.HasValue ? 1 : 0,
            max.HasValue ? 1 : 0,
            X: min ?? 0f,
            Y: max ?? 0f));
    }

    private static void AddPlaneDistancePrecondition(
        List<ProcessProgramPreconditionOp> ops,
        WithinPlaneDistancePrecondition distance,
        DomainIdRegistry ids)
    {
        ops.Add(new ProcessProgramPreconditionOp(
            ProcessProgramPrecondition.PlaneDistance,
            distance.MaxDistance,
            PlaneCode(distance.Plane),
            ids.StructuralFactIdOf(distance.InRangeFactName).Value));
    }

    private static void AddLocationResourcePrecondition(
        List<ProcessProgramPreconditionOp> ops,
        LocationResourceThreshold resource,
        DomainIdRegistry ids)
    {
        ops.Add(new ProcessProgramPreconditionOp(
            ProcessProgramPrecondition.LocationResourceRange,
            ids.ResourceIdOf(resource.ResourceType.ToDomainName()).Value,
            resource.TargetLocation ? 1 : 0,
            resource.Min.HasValue ? 1 : 0,
            resource.Max.HasValue ? 1 : 0,
            resource.Min ?? 0f,
            resource.Max ?? 0f));
    }

    private static void AddPressurePrecondition(
        List<ProcessProgramPreconditionOp> ops,
        PressureThreshold pressure,
        DomainIdRegistry ids)
    {
        ops.Add(new ProcessProgramPreconditionOp(
            ProcessProgramPrecondition.PressureRange,
            ids.PressureIdOf(pressure.PressureName).Value,
            pressure.ScopeId.Value,
            pressure.Min.HasValue ? 1 : 0,
            pressure.Max.HasValue ? 1 : 0,
            pressure.Min ?? 0f,
            pressure.Max ?? 0f));
    }

    private static void AddRelationshipStrengthPrecondition(
        List<ProcessProgramPreconditionOp> ops,
        string relationshipKind,
        float? minStrength,
        DomainIdRegistry ids,
        ProcessProgramPrecondition operatorCode)
    {
        ops.Add(new ProcessProgramPreconditionOp(
            operatorCode,
            ids.RelationshipKindIdOf(relationshipKind).Value,
            minStrength.HasValue ? 1 : 0,
            X: minStrength ?? 0f));
    }

    private static void AddRelationshipExistsPrecondition(
        List<ProcessProgramPreconditionOp> ops,
        RelationshipExists relationship,
        DomainIdRegistry ids)
    {
        ops.Add(new ProcessProgramPreconditionOp(
            ProcessProgramPrecondition.RelationshipExists,
            ids.RelationshipKindIdOf(relationship.RelationshipKind).Value,
            relationship.MinStrength.HasValue ? 1 : 0,
            relationship.SourceId.Value,
            relationship.TargetId.Value,
            relationship.MinStrength ?? 0f));
    }

    private static void CompilePreconditionChildren(
        List<ProcessProgramPreconditionOp> ops,
        IReadOnlyList<IPrecondition> children,
        DomainIdRegistry ids,
        ProcessProgramPrecondition operatorCode)
    {
        if (children.Count == 0)
        {
            ops.Add(new ProcessProgramPreconditionOp(ProcessProgramPrecondition.Always));
            return;
        }

        CompilePreconditionExpression(ops, children[0], ids);
        for (var i = 1; i < children.Count; i++)
        {
            CompilePreconditionExpression(ops, children[i], ids);
            ops.Add(new ProcessProgramPreconditionOp(operatorCode));
        }
    }

    private static List<ProcessProgramExpressionOp> CompileExpression(
        IExpression expression,
        DomainIdRegistry ids)
    {
        var ops = new List<ProcessProgramExpressionOp>();
        CompileExpression(ops, expression, ids);
        return ops;
    }

    private static void CompileExpression(
        List<ProcessProgramExpressionOp> ops,
        IExpression expression,
        DomainIdRegistry ids)
    {
        if (TryCompileTerminalExpression(ops, expression, ids) ||
            TryCompileBinaryExpression(ops, expression, ids) ||
            TryCompileUnaryExpression(ops, expression, ids))
        {
            return;
        }

        if (expression is Jitter)
        {
            throw new InvalidOperationException("Jitter expressions are not supported by compiled kernel process programs.");
        }

        throw new InvalidOperationException($"Unsupported kernel process expression '{expression.GetType().Name}'.");
    }

    private static bool TryCompileTerminalExpression(
        List<ProcessProgramExpressionOp> ops,
        IExpression expression,
        DomainIdRegistry ids)
    {
        switch (expression)
        {
            case Literal literal:
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Literal, X: literal.Value));
                return true;
            case AttributePath attr:
                AddAttributeExpression(ops, attr, ids);
                return true;
            case VariableRef variable:
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Variable, StableRuntimeCode(variable.VariableName)));
                return true;
            case LocationResourcePath resource:
                AddLocationResourceExpression(ops, resource, ids);
                return true;
            case Distance:
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Distance));
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileBinaryExpression(
        List<ProcessProgramExpressionOp> ops,
        IExpression expression,
        DomainIdRegistry ids)
    {
        switch (expression)
        {
            case Add add:
                AddBinaryExpression(ops, add.Left, add.Right, ids, ProcessProgramExpression.Add);
                return true;
            case Subtract subtract:
                AddBinaryExpression(ops, subtract.Left, subtract.Right, ids, ProcessProgramExpression.Subtract);
                return true;
            case Multiply multiply:
                AddBinaryExpression(ops, multiply.Left, multiply.Right, ids, ProcessProgramExpression.Multiply);
                return true;
            case Divide divide:
                AddBinaryExpression(ops, divide.Left, divide.Right, ids, ProcessProgramExpression.Divide);
                return true;
            case Min min:
                AddBinaryExpression(ops, min.A, min.B, ids, ProcessProgramExpression.Min);
                return true;
            case Max max:
                AddBinaryExpression(ops, max.A, max.B, ids, ProcessProgramExpression.Max);
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileUnaryExpression(
        List<ProcessProgramExpressionOp> ops,
        IExpression expression,
        DomainIdRegistry ids)
    {
        switch (expression)
        {
            case Clamp clamp:
                CompileExpression(ops, clamp.Value, ids);
                CompileExpression(ops, clamp.Min, ids);
                CompileExpression(ops, clamp.Max, ids);
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Clamp));
                return true;
            case Abs abs:
                CompileExpression(ops, abs.Value, ids);
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Abs));
                return true;
            default:
                return false;
        }
    }

    private static void AddAttributeExpression(
        List<ProcessProgramExpressionOp> ops,
        AttributePath attr,
        DomainIdRegistry ids)
    {
        ops.Add(new ProcessProgramExpressionOp(
            attr.EntityRef == "target"
                ? ProcessProgramExpression.TargetAttribute
                : ProcessProgramExpression.SelfAttribute,
            ids.AttributeIdOf(attr.AttributeName).Value));
    }

    private static void AddLocationResourceExpression(
        List<ProcessProgramExpressionOp> ops,
        LocationResourcePath resource,
        DomainIdRegistry ids)
    {
        ops.Add(new ProcessProgramExpressionOp(
            resource.EntityRef == "target"
                ? ProcessProgramExpression.TargetLocationResource
                : ProcessProgramExpression.SelfLocationResource,
            ids.ResourceIdOf(resource.ResourceType.ToDomainName()).Value));
    }

    private static void AddBinaryExpression(
        List<ProcessProgramExpressionOp> ops,
        IExpression left,
        IExpression right,
        DomainIdRegistry ids,
        ProcessProgramExpression operatorCode)
    {
        CompileExpression(ops, left, ids);
        CompileExpression(ops, right, ids);
        ops.Add(new ProcessProgramExpressionOp(operatorCode));
    }

    private static List<ProcessProgramStepRule> CompileSteps(
        BehaviorDefinition process,
        DomainIdRegistry ids)
    {
        var steps = new List<ProcessProgramStepRule>();
        foreach (var mutation in process.Mutations)
        {
            CompileStep(steps, mutation, ids);
        }

        return steps;
    }

    private static void CompileStep(
        List<ProcessProgramStepRule> steps,
        IMutation mutation,
        DomainIdRegistry ids)
    {
        if (TryCompileCreateStep(steps, mutation, ids) ||
            TryCompileAttributeStep(steps, mutation, ids) ||
            TryCompileResourceStep(steps, mutation, ids) ||
            TryCompileVariableStep(steps, mutation, ids) ||
            TryCompileMovementStep(steps, mutation))
        {
            return;
        }

        throw new InvalidOperationException($"Unsupported kernel process mutation '{mutation.GetType().Name}'.");
    }

    private static bool TryCompileCreateStep(
        List<ProcessProgramStepRule> steps,
        IMutation mutation,
        DomainIdRegistry ids)
    {
        switch (mutation)
        {
            case CreateEntityMutation createEntity:
                AddCreateEntityStep(steps, createEntity, ids);
                return true;
            case CreateRelationshipMutation createRelationship:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.CreateFixedRelationship,
                    ids.RelationshipKindIdOf(createRelationship.RelationshipKind).Value,
                    createRelationship.SourceId.Value,
                    createRelationship.TargetId.Value,
                    (int)createRelationship.Polarity,
                    createRelationship.Strength));
                return true;
            case SetHistoricalMutation historical:
                steps.Add(new ProcessProgramStepRule(ProcessProgramOperation.SetHistorical, historical.EntityId.Value));
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileAttributeStep(
        List<ProcessProgramStepRule> steps,
        IMutation mutation,
        DomainIdRegistry ids)
    {
        switch (mutation)
        {
            case ModifySelfAttributeMutation modify:
                AddAttributeStep(steps, ProcessProgramOperation.AddAttribute, modify.AttributeName, modify.Delta, ids);
                return true;
            case ModifyTargetAttributeMutation modify:
                AddAttributeStep(steps, ProcessProgramOperation.AddTargetAttribute, modify.AttributeName, modify.Delta, ids);
                return true;
            case TransferAttributeMutation transfer:
                AddTransferAttributeStep(steps, transfer, ids);
                return true;
            case ModifyRelatedEntitiesAttributeMutation related:
                AddRelatedEntitiesAttributeStep(steps, related, ids);
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileResourceStep(
        List<ProcessProgramStepRule> steps,
        IMutation mutation,
        DomainIdRegistry ids)
    {
        switch (mutation)
        {
            case ModifyPressureMutation modify:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.AddPressure,
                    ids.PressureIdOf(modify.PressureName).Value,
                    modify.ScopeId.Value,
                    X: modify.Delta));
                return true;
            case ModifyLocationResourceMutation resource:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.AddLocationResource,
                    ids.ResourceIdOf(resource.ResourceType.ToDomainName()).Value,
                    resource.TargetLocation ? 1 : 0,
                    X: resource.Delta));
                return true;
            case HexToHealthMutation hex:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.HexToHealth,
                    ids.AttributeIdOf(hex.AttributeName).Value,
                    ids.ResourceIdOf(hex.Resource.ToDomainName()).Value,
                    hex.TargetLocation ? 1 : 0,
                    X: hex.RatePerHexValue));
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileVariableStep(
        List<ProcessProgramStepRule> steps,
        IMutation mutation,
        DomainIdRegistry ids)
    {
        switch (mutation)
        {
            case CalculateMutation calculate:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.CalculateVariable,
                    StableRuntimeCode(calculate.VariableName),
                    Expression: CompileExpression(calculate.Expression, ids)));
                return true;
            case ModifySelfByVariableMutation modify:
                AddVariableAttributeStep(steps, ProcessProgramOperation.AddAttributeFromVariable, modify.AttributeName, modify.VariableName, ids);
                return true;
            case ModifyTargetByVariableMutation modify:
                AddVariableAttributeStep(steps, ProcessProgramOperation.AddTargetAttributeFromVariable, modify.AttributeName, modify.VariableName, ids);
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileMovementStep(List<ProcessProgramStepRule> steps, IMutation mutation)
    {
        switch (mutation)
        {
            case MoveTowardTargetMutation:
                steps.Add(new ProcessProgramStepRule(ProcessProgramOperation.MoveTowardTargetPhysical));
                return true;
            case WanderMutation:
                steps.Add(new ProcessProgramStepRule(ProcessProgramOperation.WanderPhysical));
                return true;
            case MoveTowardTargetOnSemanticPlaneMutation:
                steps.Add(new ProcessProgramStepRule(ProcessProgramOperation.MoveTowardTargetSemantic));
                return true;
            case WanderOnSemanticPlaneMutation:
                steps.Add(new ProcessProgramStepRule(ProcessProgramOperation.WanderSemantic));
                return true;
            default:
                return false;
        }
    }

    private static void AddCreateEntityStep(
        List<ProcessProgramStepRule> steps,
        CreateEntityMutation createEntity,
        DomainIdRegistry ids)
    {
        steps.Add(new ProcessProgramStepRule(
            ProcessProgramOperation.CreateEntity,
            ids.KindIdOf(createEntity.Kind).Value,
            ids.SubtypeIdOf(createEntity.Subtype).Value,
            ids.CultureIdOf(createEntity.Culture).Value,
            createEntity.Coordinates.HasValue ? 1 : 0,
            createEntity.Coordinates?.Q ?? 0,
            createEntity.Coordinates?.R ?? 0));
    }

    private static void AddAttributeStep(
        List<ProcessProgramStepRule> steps,
        ProcessProgramOperation operation,
        string attributeName,
        float delta,
        DomainIdRegistry ids)
    {
        steps.Add(new ProcessProgramStepRule(
            operation,
            ids.AttributeIdOf(attributeName).Value,
            X: delta));
    }

    private static void AddTransferAttributeStep(
        List<ProcessProgramStepRule> steps,
        TransferAttributeMutation transfer,
        DomainIdRegistry ids)
    {
        steps.Add(new ProcessProgramStepRule(
            ProcessProgramOperation.TransferAttribute,
            (int)EndpointCode(transfer.Source),
            ids.AttributeIdOf(transfer.SourceAttribute).Value,
            (int)EndpointCode(transfer.Destination),
            ids.AttributeIdOf(transfer.DestinationAttribute).Value,
            transfer.Amount));
    }

    private static void AddRelatedEntitiesAttributeStep(
        List<ProcessProgramStepRule> steps,
        ModifyRelatedEntitiesAttributeMutation related,
        DomainIdRegistry ids)
    {
        steps.Add(new ProcessProgramStepRule(
            ProcessProgramOperation.AddRelatedEntitiesAttribute,
            ids.RelationshipKindIdOf(related.RelationshipKind).Value,
            ids.AttributeIdOf(related.AttributeName).Value,
            related.Direction == RelationshipTraversalDirection.Incoming
                ? (int)ProcessProgramRelationshipTraversalDirection.Incoming
                : (int)ProcessProgramRelationshipTraversalDirection.Outgoing,
            related.UseTargetEntity ? 1 : 0,
            related.Delta));
    }

    private static void AddVariableAttributeStep(
        List<ProcessProgramStepRule> steps,
        ProcessProgramOperation operation,
        string attributeName,
        string variableName,
        DomainIdRegistry ids)
    {
        steps.Add(new ProcessProgramStepRule(
            operation,
            ids.AttributeIdOf(attributeName).Value,
            StableRuntimeCode(variableName),
            X: 1f));
    }

}
