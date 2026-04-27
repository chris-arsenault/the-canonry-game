namespace Engine.Domain.Compilation;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Compilation.KernelData;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Gambits;
using Engine.Kinds;
using Engine.Processes;
using Engine.Resources;
using Engine.WorldGen;
using Engine.Wiring;

public static class DomainCompiler
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
                ids.SourceCodeIdOf(DomainRuntimeNames.GeneratorSourceCode)),
            KernelData = BuildKernelData(config, ids),
            Programs = BuildPrograms(config, ids, graph),
            Symbols = BuildSymbols(config, ids),
        };
    }

    public static DomainIdRegistry BuildCatalog(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var registry = new DomainIdRegistry();
        var kinds = new HashSet<string>(StringComparer.Ordinal);
        var attributes = new HashSet<string>(StringComparer.Ordinal);
        var processes = new HashSet<string>(StringComparer.Ordinal);
        var structuralFacts = new HashSet<string>(StringComparer.Ordinal);
        var relationshipKinds = new HashSet<string>(StringComparer.Ordinal);
        var relationshipFacts = new HashSet<string>(StringComparer.Ordinal);
        var pressures = new HashSet<string>(StringComparer.Ordinal);
        var resources = new HashSet<string>(StringComparer.Ordinal);
        var terrains = new HashSet<string>(StringComparer.Ordinal);
        var subtypes = new HashSet<string>(StringComparer.Ordinal);
        var cultures = new HashSet<string>(StringComparer.Ordinal);
        var generators = new HashSet<string>(StringComparer.Ordinal);
        var gambits = new HashSet<string>(StringComparer.Ordinal);
        var detectors = new HashSet<string>(StringComparer.Ordinal);
        var eventCodes = new HashSet<string>(StringComparer.Ordinal);
        var sourceCodes = new HashSet<string>(StringComparer.Ordinal);
        var reasonCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (kind, kindConfig) in config.Kinds)
        {
            RegisterOnceStrict(kinds, kind.Value, registry.RegisterKind, "kind");

            foreach (var subtype in kindConfig.Subtypes)
            {
                RegisterOnce(subtypes, subtype, registry.RegisterSubtype);
            }

            foreach (var attribute in kindConfig.Attributes)
            {
                RegisterOnce(attributes, attribute.Name, registry.RegisterAttribute);
            }
        }

        foreach (var relationship in config.Relationships)
        {
            RegisterOnceStrict(relationshipKinds, relationship.Name, registry.RegisterRelationshipKind, "relationship kind");
        }

        foreach (var fact in config.RelationshipFacts)
        {
            RegisterOnceStrict(relationshipFacts, fact.Name, registry.RegisterRelationshipFact, "relationship fact");
        }

        foreach (var pressure in config.Pressures)
        {
            RegisterOnceStrict(pressures, pressure.Name, registry.RegisterPressure, "pressure");
        }

        foreach (var resource in config.Resources)
        {
            RegisterOnceStrict(resources, resource.Name, registry.RegisterResource, "resource");
        }

        RegisterOnce(resources, ResourceType.Food.ToDomainName(), registry.RegisterResource);
        RegisterOnce(resources, ResourceType.Wealth.ToDomainName(), registry.RegisterResource);
        RegisterOnce(resources, ResourceType.Population.ToDomainName(), registry.RegisterResource);

        foreach (var terrain in config.Terrains)
        {
            RegisterOnceStrict(terrains, terrain.Name, registry.RegisterTerrain, "terrain");
        }

        foreach (var culture in config.Cultures)
        {
            RegisterOnceStrict(cultures, culture.Id, registry.RegisterCulture, "culture");
        }

        RegisterOnce(cultures, DomainRuntimeNames.NoneCultureId, registry.RegisterCulture);

        if (config.Semantics is not null)
        {
            foreach (var generator in config.Semantics.Generators)
            {
                RegisterOnceStrict(generators, generator.Id, registry.RegisterGenerator, "generator");
                RegisterGeneratorSubtypes(generator, subtypes, registry);
            }
        }

        foreach (var process in config.Processes)
        {
            RegisterOnceStrict(processes, process.Id, registry.RegisterProcess, "process");
            RegisterProcessStructuralFacts(process, structuralFacts, registry);
        }

        foreach (var gambit in config.Gambits)
        {
            RegisterOnceStrict(gambits, gambit.Id, registry.RegisterGambit, "gambit");
            RegisterProcessStructuralFacts(gambit, structuralFacts, registry);
        }

        foreach (var (_, kindConfig) in config.Kinds)
        {
            foreach (var standingDrive in kindConfig.StandingDrives)
            {
                RegisterStructuralFactRef(standingDrive.DesiredEffect, structuralFacts, registry);
            }
        }

        RegisterOnceStrict(eventCodes, DomainRuntimeNames.RelationshipCreatedCode, registry.RegisterEventCode, "event code");
        RegisterOnceStrict(eventCodes, DomainRuntimeNames.RelationshipCulledCode, registry.RegisterEventCode, "event code");
        RegisterOnceStrict(
            eventCodes,
            DomainRuntimeNames.RelationshipStrengthChangedCode,
            registry.RegisterEventCode,
            "event code");
        RegisterOnceStrict(sourceCodes, DomainRuntimeNames.ProcessSourceCode, registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(sourceCodes, DomainRuntimeNames.GambitSourceCode, registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(sourceCodes, DomainRuntimeNames.UpkeepSourceCode, registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(sourceCodes, DomainRuntimeNames.StartupSourceCode, registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(sourceCodes, DomainRuntimeNames.RelationshipSourceCode, registry.RegisterSourceCode, "source code");
        RegisterOnceStrict(sourceCodes, DomainRuntimeNames.GeneratorSourceCode, registry.RegisterSourceCode, "source code");
        foreach (var detector in config.Degeneracy.Detectors)
        {
            RegisterOnceStrict(detectors, detector.Id, registry.RegisterDetector, "detector");
            RegisterOnceStrict(reasonCodes, detector.ReasonCode, registry.RegisterReasonCode, "reason code");
        }

        return registry;
    }

    private static KernelProgramSet BuildPrograms(
        DomainConfig config,
        DomainIdRegistry ids,
        DefinitionGraph graph)
    {
        return new KernelProgramSet
        {
            Upkeep = BuildUpkeepProgram(config, ids),
            Processes = BuildProcessProgram(config, ids),
            Gambits = BuildGambitProgram(config, ids),
            Relationships = BuildRelationshipProgram(config, ids),
            Pressures = BuildPressureProgram(config, ids),
            Generators = BuildGeneratorProgram(config, ids, graph),
            Degeneracy = BuildDegeneracyProgram(config, ids),
        };
    }

    private static CompiledKernelDataSet BuildKernelData(DomainConfig config, DomainIdRegistry ids)
    {
        return new CompiledKernelDataSet
        {
            Entities = BuildEntityData(config, ids),
            Terrain = BuildTerrainData(config, ids),
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
        return ContainsPrecondition<TargetIsSelf>(process.Precondition)
            ? ProcessProgramTargetMode.Self
            : ContainsPrecondition<TargetIsNotSelf>(process.Precondition)
                ? targetKindId > 0
                    ? ProcessProgramTargetMode.KindOther
                    : ProcessProgramTargetMode.AnyOther
                : ProcessProgramTargetMode.Self;
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
        switch (precondition)
        {
            case AndPrecondition and:
                CompilePreconditionChildren(ops, and.Children, ids, ProcessProgramPrecondition.And);
                break;
            case OrPrecondition or:
                CompilePreconditionChildren(ops, or.Children, ids, ProcessProgramPrecondition.Or);
                break;
            case NotPrecondition not:
                CompilePreconditionExpression(ops, not.Inner, ids);
                ops.Add(new ProcessProgramPreconditionOp(ProcessProgramPrecondition.Not));
                break;
            case AlwaysTrue:
                ops.Add(new ProcessProgramPreconditionOp(ProcessProgramPrecondition.Always));
                break;
            case TargetIsSelf:
                ops.Add(new ProcessProgramPreconditionOp(ProcessProgramPrecondition.TargetIsSelf));
                break;
            case TargetIsNotSelf:
                ops.Add(new ProcessProgramPreconditionOp(ProcessProgramPrecondition.TargetIsNotSelf));
                break;
            case SelfAttributeThreshold self:
                ops.Add(new ProcessProgramPreconditionOp(
                    ProcessProgramPrecondition.SelfAttributeRange,
                    ids.AttributeIdOf(self.AttributeName).Value,
                    self.Min.HasValue ? 1 : 0,
                    self.Max.HasValue ? 1 : 0,
                    X: self.Min ?? 0f,
                    Y: self.Max ?? 0f));
                break;
            case TargetAttributeThreshold target:
                ops.Add(new ProcessProgramPreconditionOp(
                    ProcessProgramPrecondition.TargetAttributeRange,
                    ids.AttributeIdOf(target.AttributeName).Value,
                    target.Min.HasValue ? 1 : 0,
                    target.Max.HasValue ? 1 : 0,
                    X: target.Min ?? 0f,
                    Y: target.Max ?? 0f));
                break;
            case WithinPlaneDistancePrecondition distance:
                ops.Add(new ProcessProgramPreconditionOp(
                    ProcessProgramPrecondition.PlaneDistance,
                    distance.MaxDistance,
                    PlaneCode(distance.Plane),
                    ids.StructuralFactIdOf(distance.InRangeFactName).Value));
                break;
            case LocationResourceThreshold resource:
                ops.Add(new ProcessProgramPreconditionOp(
                    ProcessProgramPrecondition.LocationResourceRange,
                    ids.ResourceIdOf(resource.ResourceType.ToDomainName()).Value,
                    resource.TargetLocation ? 1 : 0,
                    resource.Min.HasValue ? 1 : 0,
                    resource.Max.HasValue ? 1 : 0,
                    resource.Min ?? 0f,
                    resource.Max ?? 0f));
                break;
            case SelfHasRelationshipToTarget relationship:
                ops.Add(new ProcessProgramPreconditionOp(
                    ProcessProgramPrecondition.SelfHasRelationshipToTarget,
                    ids.RelationshipKindIdOf(relationship.RelationshipKind).Value,
                    relationship.MinStrength.HasValue ? 1 : 0,
                    X: relationship.MinStrength ?? 0f));
                break;
            case TargetHasRelationshipToSelf relationship:
                ops.Add(new ProcessProgramPreconditionOp(
                    ProcessProgramPrecondition.TargetHasRelationshipToSelf,
                    ids.RelationshipKindIdOf(relationship.RelationshipKind).Value,
                    relationship.MinStrength.HasValue ? 1 : 0,
                    X: relationship.MinStrength ?? 0f));
                break;
            case RelatedEntityMatchesTarget relationship:
                ops.Add(new ProcessProgramPreconditionOp(
                    ProcessProgramPrecondition.RelatedEntityMatchesTarget,
                    ids.RelationshipKindIdOf(relationship.RelationshipKind).Value));
                break;
            case RelationshipExists relationship:
                ops.Add(new ProcessProgramPreconditionOp(
                    ProcessProgramPrecondition.RelationshipExists,
                    ids.RelationshipKindIdOf(relationship.RelationshipKind).Value,
                    relationship.MinStrength.HasValue ? 1 : 0,
                    relationship.SourceId.Value,
                    relationship.TargetId.Value,
                    relationship.MinStrength ?? 0f));
                break;
            case PressureThreshold pressure:
                ops.Add(new ProcessProgramPreconditionOp(
                    ProcessProgramPrecondition.PressureRange,
                    ids.PressureIdOf(pressure.PressureName).Value,
                    pressure.ScopeId.Value,
                    pressure.Min.HasValue ? 1 : 0,
                    pressure.Max.HasValue ? 1 : 0,
                    pressure.Min ?? 0f,
                    pressure.Max ?? 0f));
                break;
            case CooldownCheck cooldown:
                ops.Add(new ProcessProgramPreconditionOp(
                    ProcessProgramPrecondition.CooldownElapsed,
                    cooldown.MinimumTicksSinceFire));
                break;
            case EntityCountCheck count:
                ops.Add(new ProcessProgramPreconditionOp(
                    ProcessProgramPrecondition.EntityCountAtLeast,
                    ids.KindIdOf(count.Kind).Value,
                    count.Min));
                break;
            case HasColocatedNamedEntityPrecondition:
                ops.Add(new ProcessProgramPreconditionOp(ProcessProgramPrecondition.HasColocatedEntity));
                break;
            default:
                throw new InvalidOperationException($"Unsupported kernel process precondition '{precondition.GetType().Name}'.");
        }
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
        switch (expression)
        {
            case Literal literal:
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Literal, X: literal.Value));
                break;
            case AttributePath attr:
                ops.Add(new ProcessProgramExpressionOp(
                    attr.EntityRef == "target"
                        ? ProcessProgramExpression.TargetAttribute
                        : ProcessProgramExpression.SelfAttribute,
                    ids.AttributeIdOf(attr.AttributeName).Value));
                break;
            case VariableRef variable:
                ops.Add(new ProcessProgramExpressionOp(
                    ProcessProgramExpression.Variable,
                    StableRuntimeCode(variable.VariableName)));
                break;
            case LocationResourcePath resource:
                ops.Add(new ProcessProgramExpressionOp(
                    resource.EntityRef == "target"
                        ? ProcessProgramExpression.TargetLocationResource
                        : ProcessProgramExpression.SelfLocationResource,
                    ids.ResourceIdOf(resource.ResourceType.ToDomainName()).Value));
                break;
            case Distance:
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Distance));
                break;
            case Add add:
                CompileExpression(ops, add.Left, ids);
                CompileExpression(ops, add.Right, ids);
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Add));
                break;
            case Subtract subtract:
                CompileExpression(ops, subtract.Left, ids);
                CompileExpression(ops, subtract.Right, ids);
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Subtract));
                break;
            case Multiply multiply:
                CompileExpression(ops, multiply.Left, ids);
                CompileExpression(ops, multiply.Right, ids);
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Multiply));
                break;
            case Divide divide:
                CompileExpression(ops, divide.Left, ids);
                CompileExpression(ops, divide.Right, ids);
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Divide));
                break;
            case Min min:
                CompileExpression(ops, min.A, ids);
                CompileExpression(ops, min.B, ids);
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Min));
                break;
            case Max max:
                CompileExpression(ops, max.A, ids);
                CompileExpression(ops, max.B, ids);
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Max));
                break;
            case Clamp clamp:
                CompileExpression(ops, clamp.Value, ids);
                CompileExpression(ops, clamp.Min, ids);
                CompileExpression(ops, clamp.Max, ids);
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Clamp));
                break;
            case Abs abs:
                CompileExpression(ops, abs.Value, ids);
                ops.Add(new ProcessProgramExpressionOp(ProcessProgramExpression.Abs));
                break;
            case Jitter:
                throw new InvalidOperationException("Jitter expressions are not supported by compiled kernel process programs.");
            default:
                throw new InvalidOperationException($"Unsupported kernel process expression '{expression.GetType().Name}'.");
        }
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
        switch (mutation)
        {
            case CreateEntityMutation createEntity:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.CreateEntity,
                    ids.KindIdOf(createEntity.Kind).Value,
                    ids.SubtypeIdOf(createEntity.Subtype).Value,
                    ids.CultureIdOf(createEntity.Culture).Value,
                    createEntity.Coordinates.HasValue ? 1 : 0,
                    createEntity.Coordinates?.Q ?? 0,
                    createEntity.Coordinates?.R ?? 0));
                break;
            case CreateRelationshipMutation createRelationship:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.CreateFixedRelationship,
                    ids.RelationshipKindIdOf(createRelationship.RelationshipKind).Value,
                    createRelationship.SourceId.Value,
                    createRelationship.TargetId.Value,
                    (int)createRelationship.Polarity,
                    createRelationship.Strength));
                break;
            case ModifySelfAttributeMutation modify:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.AddAttribute,
                    ids.AttributeIdOf(modify.AttributeName).Value,
                    X: modify.Delta));
                break;
            case ModifyTargetAttributeMutation modify:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.AddTargetAttribute,
                    ids.AttributeIdOf(modify.AttributeName).Value,
                    X: modify.Delta));
                break;
            case ModifyPressureMutation modify:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.AddPressure,
                    ids.PressureIdOf(modify.PressureName).Value,
                    modify.ScopeId.Value,
                    X: modify.Delta));
                break;
            case SetHistoricalMutation historical:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.SetHistorical,
                    historical.EntityId.Value));
                break;
            case TransferAttributeMutation transfer:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.TransferAttribute,
                    (int)EndpointCode(transfer.Source),
                    ids.AttributeIdOf(transfer.SourceAttribute).Value,
                    (int)EndpointCode(transfer.Destination),
                    ids.AttributeIdOf(transfer.DestinationAttribute).Value,
                    transfer.Amount));
                break;
            case ModifyLocationResourceMutation resource:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.AddLocationResource,
                    ids.ResourceIdOf(resource.ResourceType.ToDomainName()).Value,
                    resource.TargetLocation ? 1 : 0,
                    X: resource.Delta));
                break;
            case HexToHealthMutation hex:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.HexToHealth,
                    ids.AttributeIdOf(hex.AttributeName).Value,
                    ids.ResourceIdOf(hex.Resource.ToDomainName()).Value,
                    hex.TargetLocation ? 1 : 0,
                    X: hex.RatePerHexValue));
                break;
            case ModifyRelatedEntitiesAttributeMutation related:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.AddRelatedEntitiesAttribute,
                    ids.RelationshipKindIdOf(related.RelationshipKind).Value,
                    ids.AttributeIdOf(related.AttributeName).Value,
                    related.Direction == RelationshipTraversalDirection.Incoming
                        ? (int)ProcessProgramRelationshipTraversalDirection.Incoming
                        : (int)ProcessProgramRelationshipTraversalDirection.Outgoing,
                    related.UseTargetEntity ? 1 : 0,
                    related.Delta));
                break;
            case CalculateMutation calculate:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.CalculateVariable,
                    StableRuntimeCode(calculate.VariableName),
                    Expression: CompileExpression(calculate.Expression, ids)));
                break;
            case ModifySelfByVariableMutation modify:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.AddAttributeFromVariable,
                    ids.AttributeIdOf(modify.AttributeName).Value,
                    StableRuntimeCode(modify.VariableName),
                    X: 1f));
                break;
            case ModifyTargetByVariableMutation modify:
                steps.Add(new ProcessProgramStepRule(
                    ProcessProgramOperation.AddTargetAttributeFromVariable,
                    ids.AttributeIdOf(modify.AttributeName).Value,
                    StableRuntimeCode(modify.VariableName),
                    X: 1f));
                break;
            case MoveTowardTargetMutation:
                steps.Add(new ProcessProgramStepRule(ProcessProgramOperation.MoveTowardTargetPhysical));
                break;
            case WanderMutation:
                steps.Add(new ProcessProgramStepRule(ProcessProgramOperation.WanderPhysical));
                break;
            case MoveTowardTargetOnSemanticPlaneMutation:
                steps.Add(new ProcessProgramStepRule(ProcessProgramOperation.MoveTowardTargetSemantic));
                break;
            case WanderOnSemanticPlaneMutation:
                steps.Add(new ProcessProgramStepRule(ProcessProgramOperation.WanderSemantic));
                break;
            default:
                throw new InvalidOperationException($"Unsupported kernel process mutation '{mutation.GetType().Name}'.");
        }
    }

    private static ProcessProgramEndpoint EndpointCode(TransferEndpoint endpoint) => endpoint switch
    {
        TransferEndpoint.Target => ProcessProgramEndpoint.Target,
        _ => ProcessProgramEndpoint.Self,
    };

    private static int PlaneCode(PrimaryPlane plane) =>
        plane == PrimaryPlane.Semantic ? 2 : 1;

    private static RelationshipProgram BuildRelationshipProgram(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var relationships = new List<RelationshipRuntimeRule>();
        foreach (var relationship in config.Relationships)
        {
            relationships.Add(new RelationshipRuntimeRule(
                ids.RelationshipKindIdOf(relationship.Name).Value,
                relationship.CullThreshold));
        }

        return new RelationshipProgram { Relationships = relationships };
    }

    private static CompiledTerrainData BuildTerrainData(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var terrains = new List<CompiledTerrainDefinition>();
        var boundaryGroups = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var terrain in config.Terrains)
        {
            terrains.Add(new CompiledTerrainDefinition(
                ids.TerrainIdOf(terrain.Name).Value,
                BoundaryGroupId(boundaryGroups, terrain.BoundaryGroup)));
        }

        return new CompiledTerrainData { Terrains = terrains };
    }

    private static PressureProgram BuildPressureProgram(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var pressures = new List<PressureRuntimeRule>();
        foreach (var pressure in config.Pressures)
        {
            pressures.Add(new PressureRuntimeRule(
                ids.PressureIdOf(pressure.Name).Value,
                PressureScope(pressure.Scope),
                pressure.Baseline,
                pressure.HomeostasisRate,
                pressure.SmoothingFactor));
        }

        return new PressureProgram { Pressures = pressures };
    }

    private static DegeneracyProgram BuildDegeneracyProgram(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var detectors = new List<CompiledDegeneracyDetectorRule>();
        foreach (var detector in config.Degeneracy.Detectors)
        {
            detectors.Add(CompileDegeneracyDetector(config, ids, detector));
        }

        return new DegeneracyProgram { Detectors = detectors };
    }

    private static CompiledDegeneracyDetectorRule CompileDegeneracyDetector(
        DomainConfig config,
        DomainIdRegistry ids,
        DegeneracyDetectorConfig detector)
    {
        EnsureKind(config, detector.PrimaryKind, $"Degeneracy detector '{detector.Id}' primary kind");
        ValidateDegeneracyThresholds(detector);

        var secondaryKindId = 0;
        var attributeId = 0;
        var relationshipKindId = 0;

        switch (detector.Metric)
        {
            case DegeneracyMetric.AttributeDeclineRatio:
                attributeId = ResolveDegeneracyAttributeId(config, ids, detector.PrimaryKind, detector.Attribute, detector.Id);
                EnsureNoSecondaryKind(detector);
                EnsureNoRelationshipKind(detector);
                break;
            case DegeneracyMetric.RelationshipEndpointDominanceRatio:
                secondaryKindId = ResolveDegeneracySecondaryKindId(config, ids, detector);
                relationshipKindId = ResolveDegeneracyRelationshipKindId(config, ids, detector.RelationshipKind, detector.Id);
                EnsureNoAttribute(detector);
                break;
            case DegeneracyMetric.ActiveKindCount:
                EnsureNoSecondaryKind(detector);
                EnsureNoAttribute(detector);
                EnsureNoRelationshipKind(detector);
                break;
            default:
                throw new InvalidOperationException(
                    $"Degeneracy detector '{detector.Id}' uses unsupported metric '{detector.Metric}'.");
        }

        return new CompiledDegeneracyDetectorRule(
            ids.DetectorIdOf(detector.Id).Value,
            ToDegeneracyProgramMetric(detector.Metric),
            ToDegeneracyProgramThresholdDirection(detector.ThresholdDirection),
            ToDegeneracyProgramSeverity(detector.NoSubjectsSeverity),
            ids.ReasonCodeIdOf(detector.ReasonCode).Value,
            ids.KindIdOf(detector.PrimaryKind).Value,
            secondaryKindId,
            attributeId,
            relationshipKindId,
            ToDegeneracyProgramRelationshipEndpoint(detector.DominantRelationshipEndpoint),
            detector.WarningThreshold,
            detector.CriticalThreshold);
    }

    private static int ResolveDegeneracySecondaryKindId(
        DomainConfig config,
        DomainIdRegistry ids,
        DegeneracyDetectorConfig detector)
    {
        if (detector.SecondaryKind is not { } secondaryKind)
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' requires SecondaryKind for metric '{detector.Metric}'.");
        }

        EnsureKind(config, secondaryKind, $"Degeneracy detector '{detector.Id}' secondary kind");
        return ids.KindIdOf(secondaryKind).Value;
    }

    private static int ResolveDegeneracyAttributeId(
        DomainConfig config,
        DomainIdRegistry ids,
        EntityKind kind,
        string? attributeName,
        string detectorId)
    {
        if (string.IsNullOrWhiteSpace(attributeName))
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detectorId}' requires Attribute for attribute decline metrics.");
        }

        if (!config.Kinds[kind].Attributes.Any(attribute => string.Equals(attribute.Name, attributeName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detectorId}' references unknown attribute '{attributeName}' on kind '{kind}'.");
        }

        return ids.AttributeIdOf(attributeName).Value;
    }

    private static int ResolveDegeneracyRelationshipKindId(
        DomainConfig config,
        DomainIdRegistry ids,
        string? relationshipKind,
        string detectorId)
    {
        if (string.IsNullOrWhiteSpace(relationshipKind))
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detectorId}' requires RelationshipKind for relationship dominance metrics.");
        }

        if (!config.Relationships.Any(relationship => string.Equals(relationship.Name, relationshipKind, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detectorId}' references unknown relationship kind '{relationshipKind}'.");
        }

        return ids.RelationshipKindIdOf(relationshipKind).Value;
    }

    private static void EnsureKind(DomainConfig config, EntityKind kind, string context)
    {
        if (!config.Kinds.ContainsKey(kind))
        {
            throw new InvalidOperationException($"{context} references unknown kind '{kind}'.");
        }
    }

    private static void EnsureNoSecondaryKind(DegeneracyDetectorConfig detector)
    {
        if (detector.SecondaryKind is not null)
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' does not support SecondaryKind for metric '{detector.Metric}'.");
        }
    }

    private static void EnsureNoAttribute(DegeneracyDetectorConfig detector)
    {
        if (!string.IsNullOrWhiteSpace(detector.Attribute))
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' does not support Attribute for metric '{detector.Metric}'.");
        }
    }

    private static void EnsureNoRelationshipKind(DegeneracyDetectorConfig detector)
    {
        if (!string.IsNullOrWhiteSpace(detector.RelationshipKind))
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' does not support RelationshipKind for metric '{detector.Metric}'.");
        }
    }

    private static void ValidateDegeneracyThresholds(DegeneracyDetectorConfig detector)
    {
        if (detector.WarningThreshold < 0f || detector.CriticalThreshold < 0f)
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' thresholds must be non-negative.");
        }

        if (detector.ThresholdDirection == DegeneracyThresholdDirection.HighIsBad &&
            detector.CriticalThreshold < detector.WarningThreshold)
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' critical threshold must be greater than or equal to warning threshold.");
        }

        if (detector.ThresholdDirection == DegeneracyThresholdDirection.LowIsBad &&
            detector.CriticalThreshold > detector.WarningThreshold)
        {
            throw new InvalidOperationException(
                $"Degeneracy detector '{detector.Id}' critical threshold must be less than or equal to warning threshold.");
        }
    }

    private static GeneratorProgram BuildGeneratorProgram(
        DomainConfig config,
        DomainIdRegistry ids,
        DefinitionGraph graph)
    {
        var terrainByName = config.Terrains.ToDictionary(
            static terrain => terrain.Name,
            StringComparer.Ordinal);
        var generators = new List<CompiledGeneratorRule>();

        foreach (var generator in graph.Generators.Values)
        {
            var refCodes = BuildGeneratorRefCodes(generator);
            generators.Add(new CompiledGeneratorRule(
                generator.RuntimeId,
                GeneratorProgram.DefaultCadenceTicks,
                ids.KindIdOf(generator.Target.Kind).Value,
                CompileGeneratorSubtypeIds(generator.Target.Subtypes, ids),
                generator.Target.AllowWilderness,
                Math.Max(generator.PulseWeight, 0),
                generator.MaxRunsPerSim ?? 0,
                CompileGeneratorTargetSaturation(generator.TargetSaturation),
                CompileGeneratorTriggers(generator.Triggers, ids),
                CompileGeneratorBindings(generator.SecondaryTargets, ids, refCodes),
                CompileGeneratorProducedEntities(generator.Produces, ids, terrainByName, refCodes),
                CompileGeneratorRelationships(generator.Relationships, graph, refCodes)));
        }

        return new GeneratorProgram { Generators = generators };
    }

    private static Dictionary<string, int> BuildGeneratorRefCodes(DefinitionGeneratorNode generator)
    {
        var refCodes = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["$target"] = GeneratorProgram.TargetReferenceCode,
        };
        var next = GeneratorProgram.TargetReferenceCode + 1;

        foreach (var binding in generator.SecondaryTargets)
        {
            refCodes.Add(binding.Ref, next++);
        }

        foreach (var produced in generator.Produces)
        {
            refCodes.Add(produced.Ref, next++);
        }

        return refCodes;
    }

    private static GeneratorTargetSaturationRule? CompileGeneratorTargetSaturation(
        DefinitionTargetSaturation? saturation)
    {
        return saturation is null
            ? null
            : new GeneratorTargetSaturationRule(
                saturation.RelationshipRuntimeId,
                saturation.RequiredFactRuntimeId,
                ToGeneratorSaturationDirection(saturation.Direction),
                saturation.MaxExisting);
    }

    private static List<GeneratorTriggerRule> CompileGeneratorTriggers(
        IReadOnlyList<DefinitionGeneratorTrigger> triggers,
        DomainIdRegistry ids)
    {
        var compiled = new List<GeneratorTriggerRule>();
        foreach (var trigger in triggers)
        {
            var direction = ToGeneratorTriggerDirection(trigger.Direction);
            compiled.Add(new GeneratorTriggerRule(
                ids.KindIdOf(trigger.OnKind).Value,
                trigger.AttributeId is { } attributeId ? ids.AttributeIdOf(attributeId.Value).Value : 0,
                direction,
                GeneratorTriggerThreshold(direction)));
        }

        return compiled;
    }

    private static List<GeneratorBindingRule> CompileGeneratorBindings(
        IReadOnlyList<DefinitionGeneratorBindingTarget> bindings,
        DomainIdRegistry ids,
        Dictionary<string, int> refCodes)
    {
        var compiled = new List<GeneratorBindingRule>();
        foreach (var binding in bindings)
        {
            compiled.Add(new GeneratorBindingRule(
                refCodes[binding.Ref],
                ids.KindIdOf(binding.Kind).Value,
                CompileGeneratorQuery(binding.Query, ids)));
        }

        return compiled;
    }

    private static List<GeneratorProducedEntityRule> CompileGeneratorProducedEntities(
        IReadOnlyList<DefinitionProducedEntity> producedEntities,
        DomainIdRegistry ids,
        Dictionary<string, TerrainConfig> terrainByName,
        Dictionary<string, int> refCodes)
    {
        var compiled = new List<GeneratorProducedEntityRule>();
        foreach (var produced in producedEntities)
        {
            compiled.Add(new GeneratorProducedEntityRule(
                refCodes[produced.Ref],
                ids.KindIdOf(produced.Kind).Value,
                !string.IsNullOrWhiteSpace(produced.Subtype)
                    ? ids.SubtypeIdOf(produced.Subtype).Value
                    : 0,
                CompileGeneratorSubtypeIds(produced.Subtypes, ids),
                produced.CreateIfMissing,
                produced.Query is not null,
                produced.Query is null
                    ? null
                    : CompileGeneratorQuery(produced.Query, ids, ProducedQuerySubtypes(produced)),
                ToGeneratorPlacement(produced.PhysicalPlacement),
                ToGeneratorPlacement(produced.SemanticPlacement),
                refCodes[produced.PlacementRef],
                refCodes[produced.InheritCultureFrom],
                produced.CreateChance,
                CompileGeneratorCoverage(produced.Coverage, terrainByName, refCodes, ids)));
        }

        return compiled;
    }

    private static GeneratorEntityQueryRule? CompileGeneratorQuery(
        DefinitionEntityQuery? query,
        DomainIdRegistry ids,
        IEnumerable<string>? subtypeSource = null)
    {
        if (query is null)
        {
            return null;
        }

        return new GeneratorEntityQueryRule(
            CompileGeneratorSubtypeIds(subtypeSource ?? query.Subtypes, ids),
            query.AdjacentToTarget,
            ToGeneratorCultureMatch(query.TargetCultureMatch),
            CompileRelationshipKindIds(query.WithoutOutgoingRelationships, ids),
            CompileRelationshipKindIds(query.WithoutIncomingRelationships, ids),
            CompileGeneratorAttributeConditions(query.AttributeConditions, ids));
    }

    private static List<GeneratorAttributeConditionRule> CompileGeneratorAttributeConditions(
        IReadOnlyList<DefinitionQueryAttributeCondition> conditions,
        DomainIdRegistry ids)
    {
        var compiled = new List<GeneratorAttributeConditionRule>();
        foreach (var condition in conditions)
        {
            var direction = ToGeneratorTriggerDirection(condition.Direction);
            compiled.Add(new GeneratorAttributeConditionRule(
                ids.AttributeIdOf(condition.AttributeId.Value).Value,
                direction,
                GeneratorTriggerThreshold(direction)));
        }

        return compiled;
    }

    private static GeneratorCoverageRule? CompileGeneratorCoverage(
        DefinitionRegionCoverage? coverage,
        Dictionary<string, TerrainConfig> terrainByName,
        Dictionary<string, int> refCodes,
        DomainIdRegistry ids)
    {
        if (coverage is null)
        {
            return null;
        }

        TerrainConfig? transformTerrain = null;
        if (!string.IsNullOrWhiteSpace(coverage.TransformTerrain))
        {
            transformTerrain = ResolveTerrain(terrainByName, coverage.TransformTerrain);
        }

        return new GeneratorCoverageRule(
            ToGeneratorCoverageMode(coverage.Mode),
            !string.IsNullOrWhiteSpace(coverage.SourceRef)
                ? refCodes[coverage.SourceRef]
                : 0,
            coverage.DesiredCellCount,
            coverage.MinimumCellCount,
            coverage.MinimumSourceRemainingCellCount,
            coverage.RespectTerrainBoundaries,
            transformTerrain is null ? 0 : ids.TerrainIdOf(transformTerrain.Name).Value,
            CompileTerrainIds(coverage.PreferredTerrains, terrainByName, ids),
            CompileTerrainIds(coverage.AllowedTerrains, terrainByName, ids));
    }

    private static List<GeneratorRelationshipRule> CompileGeneratorRelationships(
        IReadOnlyList<DefinitionGeneratorRelationship> relationships,
        DefinitionGraph graph,
        Dictionary<string, int> refCodes)
    {
        var compiled = new List<GeneratorRelationshipRule>();
        foreach (var relationship in relationships)
        {
            compiled.Add(new GeneratorRelationshipRule(
                refCodes[relationship.SourceRef],
                refCodes[relationship.DestinationRef],
                relationship.RelationshipRuntimeId,
                relationship.Strength,
                relationship.ReplaceExistingOutgoingOfSameKind ||
                graph.Relationships[relationship.RelationshipId].SourceConfig.IsExclusive,
                relationship.FactRuntimeIds.OrderBy(static factId => factId).ToList()));
        }

        return compiled;
    }

    private static List<int> CompileGeneratorSubtypeIds(
        IEnumerable<string>? subtypes,
        DomainIdRegistry ids)
    {
        var compiled = new List<int>();
        if (subtypes is null)
        {
            return compiled;
        }

        foreach (var subtype in subtypes)
        {
            compiled.Add(ids.SubtypeIdOf(subtype).Value);
        }

        return compiled;
    }

    private static List<int> CompileRelationshipKindIds(
        IEnumerable<string>? relationshipKinds,
        DomainIdRegistry ids)
    {
        var compiled = new List<int>();
        if (relationshipKinds is null)
        {
            return compiled;
        }

        foreach (var relationshipKind in relationshipKinds)
        {
            compiled.Add(ids.RelationshipKindIdOf(relationshipKind).Value);
        }

        return compiled;
    }

    private static List<int> CompileTerrainIds(
        IEnumerable<string>? terrains,
        Dictionary<string, TerrainConfig> terrainByName,
        DomainIdRegistry ids)
    {
        var compiled = new List<int>();
        if (terrains is null)
        {
            return compiled;
        }

        foreach (var terrain in terrains)
        {
            compiled.Add(ids.TerrainIdOf(ResolveTerrain(terrainByName, terrain).Name).Value);
        }

        return compiled;
    }

    private static TerrainConfig ResolveTerrain(
        Dictionary<string, TerrainConfig> terrainByName,
        string terrainName)
    {
        if (!terrainByName.TryGetValue(terrainName, out var terrain))
        {
            throw new InvalidOperationException($"Generator coverage references undeclared terrain '{terrainName}'.");
        }

        return terrain;
    }

    private static IEnumerable<string>? ProducedQuerySubtypes(DefinitionProducedEntity produced)
    {
        if (produced.Query?.Subtypes is { Count: > 0 } querySubtypes)
        {
            return querySubtypes;
        }

        if (!string.IsNullOrWhiteSpace(produced.Subtype))
        {
            return [produced.Subtype];
        }

        return produced.Subtypes;
    }

    private static GeneratorProgramSaturationDirection ToGeneratorSaturationDirection(
        TargetSaturationDirection direction) => direction switch
    {
        TargetSaturationDirection.OutgoingFromTarget => GeneratorProgramSaturationDirection.OutgoingFromTarget,
        TargetSaturationDirection.IncomingToTarget => GeneratorProgramSaturationDirection.IncomingToTarget,
        TargetSaturationDirection.Symmetric => GeneratorProgramSaturationDirection.Symmetric,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    private static GeneratorProgramPlacement ToGeneratorPlacement(SemanticPlacement placement) => placement switch
    {
        SemanticPlacement.On => GeneratorProgramPlacement.On,
        SemanticPlacement.Near => GeneratorProgramPlacement.Near,
        SemanticPlacement.Across => GeneratorProgramPlacement.Across,
        SemanticPlacement.InRegionLocality => GeneratorProgramPlacement.InCoverageLocality,
        _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, null),
    };

    private static GeneratorProgramCultureMatch ToGeneratorCultureMatch(
        QueryTargetCultureMatch cultureMatch) => cultureMatch switch
    {
        QueryTargetCultureMatch.Any => GeneratorProgramCultureMatch.Any,
        QueryTargetCultureMatch.SameAsTarget => GeneratorProgramCultureMatch.SameAsTarget,
        QueryTargetCultureMatch.DifferentFromTarget => GeneratorProgramCultureMatch.DifferentFromTarget,
        _ => throw new ArgumentOutOfRangeException(nameof(cultureMatch), cultureMatch, null),
    };

    private static GeneratorProgramTriggerDirection ToGeneratorTriggerDirection(
        TriggerDirection direction) => direction switch
    {
        TriggerDirection.High => GeneratorProgramTriggerDirection.High,
        TriggerDirection.Low => GeneratorProgramTriggerDirection.Low,
        TriggerDirection.Critical => GeneratorProgramTriggerDirection.Critical,
        TriggerDirection.Any => GeneratorProgramTriggerDirection.Any,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    private static GeneratorProgramCoverageMode ToGeneratorCoverageMode(CoveragePlacementMode mode) => mode switch
    {
        CoveragePlacementMode.CreateNew => GeneratorProgramCoverageMode.CreateNew,
        CoveragePlacementMode.SplitFromExisting => GeneratorProgramCoverageMode.SplitFromExisting,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    private static float GeneratorTriggerThreshold(GeneratorProgramTriggerDirection direction) => direction switch
    {
        GeneratorProgramTriggerDirection.High => StandardNeedScale.Comfortable,
        GeneratorProgramTriggerDirection.Low => StandardNeedScale.Concern,
        GeneratorProgramTriggerDirection.Critical => StandardNeedScale.Critical,
        _ => 0f,
    };

    private static DegeneracyProgramMetric ToDegeneracyProgramMetric(DegeneracyMetric metric) => metric switch
    {
        DegeneracyMetric.AttributeDeclineRatio => DegeneracyProgramMetric.AttributeDeclineRatio,
        DegeneracyMetric.RelationshipEndpointDominanceRatio => DegeneracyProgramMetric.RelationshipEndpointDominanceRatio,
        DegeneracyMetric.ActiveKindCount => DegeneracyProgramMetric.ActiveKindCount,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null),
    };

    private static DegeneracyProgramThresholdDirection ToDegeneracyProgramThresholdDirection(
        DegeneracyThresholdDirection direction) => direction switch
    {
        DegeneracyThresholdDirection.HighIsBad => DegeneracyProgramThresholdDirection.HighIsBad,
        DegeneracyThresholdDirection.LowIsBad => DegeneracyProgramThresholdDirection.LowIsBad,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    private static DegeneracyProgramRelationshipEndpoint ToDegeneracyProgramRelationshipEndpoint(
        DegeneracyRelationshipEndpoint endpoint) => endpoint switch
    {
        DegeneracyRelationshipEndpoint.Source => DegeneracyProgramRelationshipEndpoint.Source,
        DegeneracyRelationshipEndpoint.Target => DegeneracyProgramRelationshipEndpoint.Target,
        _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, null),
    };

    private static DegeneracyProgramSeverity ToDegeneracyProgramSeverity(DegeneracySeverity severity) => severity switch
    {
        DegeneracySeverity.Normal => DegeneracyProgramSeverity.Normal,
        DegeneracySeverity.Warning => DegeneracyProgramSeverity.Warning,
        DegeneracySeverity.Critical => DegeneracyProgramSeverity.Critical,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null),
    };

    private static bool AttributeCreatesObjective(AttributeConfig attribute) =>
        attribute.Category is AttributeCategory.Health or AttributeCategory.Need or AttributeCategory.Drive;

    private static ProcessObjectiveUrgency ObjectiveUrgency(AttributeConfig attribute) =>
        StandardNeedScale.AppliesTo(attribute)
            ? ProcessObjectiveUrgency.StandardNeed
            : attribute.Baseline > 0f
                ? ProcessObjectiveUrgency.Baseline
                : ProcessObjectiveUrgency.RangeDefault;

    private static ProcessProgramEffectScope EffectScope(AttributeRef reference) => reference.Scope switch
    {
        RefScope.Self => ProcessProgramEffectScope.SelfAttribute,
        RefScope.Target => ProcessProgramEffectScope.TargetAttribute,
        RefScope.Resource => ProcessProgramEffectScope.Resource,
        RefScope.Structural => ProcessProgramEffectScope.Structural,
        RefScope.Relational => ProcessProgramEffectScope.Relational,
        _ => throw new InvalidOperationException($"Unsupported process effect scope '{reference.Scope}'."),
    };

    private static int EffectValueId(AttributeRef reference, DomainIdRegistry ids) => reference.Scope switch
    {
        RefScope.Self or RefScope.Target => ids.AttributeIdOf(reference.Name).Value,
        RefScope.Resource => ids.ResourceIdOf(reference.Name).Value,
        RefScope.Structural => ids.StructuralFactIdOf(reference.Name).Value,
        RefScope.Relational => ids.RelationshipKindIdOf(reference.RelationshipKind ?? reference.Name).Value,
        _ => throw new InvalidOperationException($"Unsupported process effect scope '{reference.Scope}'."),
    };

    private static int BoundaryGroupId(Dictionary<string, int> boundaryGroups, string stableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stableName);
        if (boundaryGroups.TryGetValue(stableName, out var id))
        {
            return id;
        }

        id = boundaryGroups.Count + 1;
        boundaryGroups.Add(stableName, id);
        return id;
    }

    private static PressureRuntimeScope PressureScope(Engine.Pressure.PressureScope scope) => scope switch
    {
        Engine.Pressure.PressureScope.Regional => PressureRuntimeScope.Coverage,
        Engine.Pressure.PressureScope.Entity => PressureRuntimeScope.Entity,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null),
    };

    private static int StableRuntimeCode(string stableName)
    {
        unchecked
        {
            var hash = 23;
            foreach (var ch in stableName)
            {
                hash = (hash * 31) + ch;
            }

            return hash == int.MinValue ? int.MaxValue : Math.Abs(hash);
        }
    }

    private static List<DomainSymbolSeed> BuildSymbols(DomainConfig config, DomainIdRegistry ids)
    {
        var symbols = new List<DomainSymbolSeed>();
        var seen = new HashSet<(DomainSymbolKind Kind, int Id)>();

        void Add(
            DomainSymbolKind kind,
            int id,
            string stableName,
            string? displayName = null,
            string? description = null,
            string? iconKey = null,
            string? colorKey = null)
        {
            if (id <= 0 || !seen.Add((kind, id)))
            {
                return;
            }

            symbols.Add(new DomainSymbolSeed(kind, id, stableName, displayName, description, iconKey, colorKey));
        }

        foreach (var (kind, kindConfig) in config.Kinds)
        {
            Add(DomainSymbolKind.Kind, ids.KindIdOf(kind).Value, kind.Value, description: kindConfig.Description);
            foreach (var attribute in kindConfig.Attributes)
            {
                Add(
                    DomainSymbolKind.Attribute,
                    ids.AttributeIdOf(attribute.Name).Value,
                    attribute.Name,
                    attribute.DisplayName);
            }
        }

        foreach (var relationship in config.Relationships)
        {
            Add(
                DomainSymbolKind.RelationshipKind,
                ids.RelationshipKindIdOf(relationship.Name).Value,
                relationship.Name);
        }

        foreach (var fact in config.RelationshipFacts)
        {
            Add(
                DomainSymbolKind.RelationshipFact,
                ids.RelationshipFactIdOf(fact.Name).Value,
                fact.Name,
                fact.DisplayName,
                fact.Description);
        }

        foreach (var pressure in config.Pressures)
        {
            Add(DomainSymbolKind.Pressure, ids.PressureIdOf(pressure.Name).Value, pressure.Name);
        }

        foreach (var resource in config.Resources)
        {
            Add(DomainSymbolKind.Resource, ids.ResourceIdOf(resource.Name).Value, resource.Name, resource.DisplayName);
        }

        foreach (var terrain in config.Terrains)
        {
            Add(DomainSymbolKind.Terrain, ids.TerrainIdOf(terrain.Name).Value, terrain.Name, terrain.DisplayName);
        }

        foreach (var culture in config.Cultures)
        {
            Add(DomainSymbolKind.Culture, ids.CultureIdOf(culture.Id).Value, culture.Id, culture.DisplayName);
        }

        foreach (var detector in config.Degeneracy.Detectors)
        {
            Add(
                DomainSymbolKind.Detector,
                ids.DetectorIdOf(detector.Id).Value,
                detector.Id,
                detector.DisplayName,
                detector.Description);
            Add(DomainSymbolKind.ReasonCode, ids.ReasonCodeIdOf(detector.ReasonCode).Value, detector.ReasonCode);
        }

        if (config.Semantics is not null)
        {
            foreach (var generator in config.Semantics.Generators)
            {
                Add(
                    DomainSymbolKind.Generator,
                    ids.GeneratorIdOf(generator.Id).Value,
                    generator.Id,
                    generator.Name,
                    generator.Description);
            }
        }

        foreach (var process in config.Processes)
        {
            Add(
                DomainSymbolKind.Process,
                ids.ProcessIdOf(process.Id).Value,
                process.Id,
                process.Name);
        }

        foreach (var gambit in config.Gambits)
        {
            Add(
                DomainSymbolKind.Gambit,
                ids.GambitIdOf(gambit.Id).Value,
                gambit.Id,
                gambit.Name);
        }

        foreach (var entry in ids.Entries())
        {
            var kind = ToSymbolKind(entry.Category);
            if (kind is not null)
            {
                Add(kind.Value, entry.Id, entry.StableName);
            }
        }

        return symbols;
    }

    private static DomainSymbolKind? ToSymbolKind(DomainIdCategory category) => category switch
    {
        DomainIdCategory.Kind => DomainSymbolKind.Kind,
        DomainIdCategory.Attribute => DomainSymbolKind.Attribute,
        DomainIdCategory.Process => DomainSymbolKind.Process,
        DomainIdCategory.Gambit => DomainSymbolKind.Gambit,
        DomainIdCategory.StructuralFact => DomainSymbolKind.StructuralFact,
        DomainIdCategory.RelationshipKind => DomainSymbolKind.RelationshipKind,
        DomainIdCategory.RelationshipFact => DomainSymbolKind.RelationshipFact,
        DomainIdCategory.Pressure => DomainSymbolKind.Pressure,
        DomainIdCategory.Resource => DomainSymbolKind.Resource,
        DomainIdCategory.Terrain => DomainSymbolKind.Terrain,
        DomainIdCategory.Subtype => DomainSymbolKind.Subtype,
        DomainIdCategory.Culture => DomainSymbolKind.Culture,
        DomainIdCategory.Generator => DomainSymbolKind.Generator,
        DomainIdCategory.Detector => DomainSymbolKind.Detector,
        DomainIdCategory.EventCode => DomainSymbolKind.EventCode,
        DomainIdCategory.ReasonCode => DomainSymbolKind.ReasonCode,
        DomainIdCategory.SourceCode => DomainSymbolKind.SourceCode,
        _ => null,
    };

    private static bool ContainsPrecondition<T>(IPrecondition precondition)
        where T : IPrecondition
    {
        if (precondition is T)
        {
            return true;
        }

        return precondition switch
        {
            AndPrecondition and => and.Children.Any(ContainsPrecondition<T>),
            OrPrecondition or => or.Children.Any(ContainsPrecondition<T>),
            NotPrecondition not => ContainsPrecondition<T>(not.Inner),
            _ => false,
        };
    }

    private static void RegisterProcessStructuralFacts(
        BehaviorDefinition process,
        HashSet<string> structuralFacts,
        DomainIdRegistry registry)
    {
        RegisterStructuralFacts(process.Reads, structuralFacts, registry);
        RegisterStructuralFacts(process.Writes, structuralFacts, registry);
        RegisterStructuralFacts(process.GoalEffects, structuralFacts, registry);
        RegisterStructuralFacts(process.ProviderSelectors, structuralFacts, registry);
        RegisterPreconditionStructuralFacts(process.Precondition, structuralFacts, registry);
        foreach (var supportRule in process.SupportRules)
        {
            RegisterStructuralFactRef(supportRule.DesiredEffect, structuralFacts, registry);
        }
    }

    private static void RegisterStructuralFacts(
        IEnumerable<AttributeRef> references,
        HashSet<string> structuralFacts,
        DomainIdRegistry registry)
    {
        foreach (var reference in references)
        {
            RegisterStructuralFactRef(reference, structuralFacts, registry);
        }
    }

    private static void RegisterStructuralFactRef(
        AttributeRef reference,
        HashSet<string> structuralFacts,
        DomainIdRegistry registry)
    {
        if (reference.Scope == RefScope.Structural)
        {
            RegisterOnce(structuralFacts, reference.Name, registry.RegisterStructuralFact);
        }
    }

    private static void RegisterPreconditionStructuralFacts(
        IPrecondition precondition,
        HashSet<string> structuralFacts,
        DomainIdRegistry registry)
    {
        switch (precondition)
        {
            case AndPrecondition and:
                foreach (var child in and.Children)
                {
                    RegisterPreconditionStructuralFacts(child, structuralFacts, registry);
                }

                break;
            case OrPrecondition or:
                foreach (var child in or.Children)
                {
                    RegisterPreconditionStructuralFacts(child, structuralFacts, registry);
                }

                break;
            case NotPrecondition not:
                RegisterPreconditionStructuralFacts(not.Inner, structuralFacts, registry);
                break;
            case WithinPlaneDistancePrecondition distance:
                RegisterOnce(structuralFacts, distance.InRangeFactName, registry.RegisterStructuralFact);
                break;
        }
    }

    private static void RegisterGeneratorSubtypes(
        SemanticGeneratorConfig generator,
        HashSet<string> subtypes,
        DomainIdRegistry registry)
    {
        RegisterSubtypeSet(generator.Target.Subtypes, subtypes, registry);
        foreach (var produced in generator.Produces)
        {
            if (!string.IsNullOrWhiteSpace(produced.Subtype))
            {
                RegisterOnce(subtypes, produced.Subtype, registry.RegisterSubtype);
            }

            RegisterSubtypeSet(produced.Subtypes, subtypes, registry);
            RegisterSubtypeSet(produced.Query?.Subtypes, subtypes, registry);
        }

        foreach (var secondaryTarget in generator.SecondaryTargets)
        {
            RegisterSubtypeSet(secondaryTarget.Query?.Subtypes, subtypes, registry);
        }
    }

    private static void RegisterSubtypeSet(
        IReadOnlySet<string>? values,
        HashSet<string> subtypes,
        DomainIdRegistry registry)
    {
        if (values is null)
        {
            return;
        }

        foreach (var subtype in values)
        {
            RegisterOnce(subtypes, subtype, registry.RegisterSubtype);
        }
    }

    private static void RegisterOnce<TId>(
        HashSet<string> seen,
        string stableName,
        Func<string, TId> register)
        where TId : struct
    {
        if (seen.Add(stableName))
        {
            _ = register(stableName);
        }
    }

    private static void RegisterOnceStrict<TId>(
        HashSet<string> seen,
        string stableName,
        Func<string, TId> register,
        string category)
        where TId : struct
    {
        if (!seen.Add(stableName))
        {
            throw new InvalidOperationException($"Duplicate {category} registration '{stableName}'.");
        }

        _ = register(stableName);
    }
}
