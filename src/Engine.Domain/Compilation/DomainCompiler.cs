namespace Engine.Domain.Compilation;

using Engine.Behaviors;
using Engine.Constants;
using Engine.Domain.Compilation.KernelData;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Spatial;
using Engine.Gambits;
using Engine.Gameplay;
using Engine.Kinds;
using Engine.Wiring;

public static partial class DomainCompiler
{
    public static CompiledDomain Compile(DomainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var ids = BuildCatalog(config);
        var graph = DefinitionGraphBuilder.Build(config, ids);
        var diagnostics = new List<DomainCompileDiagnostic>();
        var kernelData = BuildKernelData(config, ids);
        var programs = BuildPrograms(config, ids, graph, diagnostics);
        var symbols = BuildSymbols(config, ids);
        var chronicleQueries = BuildChronicleQueries(config, ids);

        return new CompiledDomain
        {
            Config = config,
            Ids = ids,
            DefinitionGraph = graph,
            RuntimeCodes = new DomainRuntimeCodes(
                ids.EventCodeIdOf(DomainRuntimeNames.RelationshipCreatedCode),
                ids.EventCodeIdOf(DomainRuntimeNames.RelationshipCulledCode),
                ids.EventCodeIdOf(DomainRuntimeNames.RelationshipStrengthChangedCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.GambitSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.StartupSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.RelationshipSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.GeneratorSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.MapSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.LowAltitudeVerbSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.EffectsSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.PlayerSourceCode),
                ids.SourceCodeIdOf(DomainRuntimeNames.ChronicleSourceCode)),
            KernelData = kernelData,
            Programs = programs,
            Symbols = symbols,
            ChronicleQueries = chronicleQueries,
            Diagnostics = diagnostics,
        };
    }

    private static CompiledEntityData BuildEntityData(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var attributes = new List<CompiledEntityAttributeData>();
        var traits = new List<CompiledEntityTraitData>();
        foreach (var (kind, kindConfig) in config.Kinds)
        {
            var kindId = ids.KindIdOf(kind).Value;
            var health = kindConfig.Health;
            attributes.Add(new CompiledEntityAttributeData(
                kindId,
                ids.AttributeIdOf(health.Name).Value,
                KernelFixedPoint.AttributeScalar(KernelFixedPoint.AttributeUnits(health.Min)),
                KernelFixedPoint.AttributeScalar(KernelFixedPoint.AttributeUnits(health.Max)),
                KernelFixedPoint.AttributeScalar(KernelFixedPoint.AttributeUnits(health.Default)),
                FixedScalar.FromSingle(health.DefaultVariance)));

            foreach (var trait in kindConfig.Traits)
            {
                traits.Add(new CompiledEntityTraitData(
                    kindId,
                    ids.TraitIdOf(trait.Name).Value,
                    FixedScalar.FromSingle(trait.AssignmentChance)));
            }
        }

        return new CompiledEntityData
        {
            Attributes = attributes,
            Traits = traits,
        };
    }

    private static CompiledMovementData BuildMovementData(
        DomainConfig config,
        DomainIdRegistry ids,
        Dictionary<string, int> movementProfileIds)
    {
        var defaults = new List<CompiledMovementProfileDefault>();
        var subtypeRules = new List<CompiledMovementProfileSubtypeRule>();
        var traitRules = new List<CompiledMovementProfileTraitRule>();
        foreach (var (kind, kindConfig) in config.Kinds)
        {
            var kindId = ids.KindIdOf(kind).Value;
            AddMovementProfileDefault(defaults, kindId, PlaneCodes.Physical, kindConfig.MovementProfiles.PhysicalDefault, movementProfileIds);
            AddMovementProfileDefault(defaults, kindId, PlaneCodes.Semantic, kindConfig.MovementProfiles.SemanticDefault, movementProfileIds);
            AddMovementProfileSubtypeRules(subtypeRules, kindId, kindConfig.MovementProfiles.SubtypeRules, ids, movementProfileIds);
            AddMovementProfileTraitRules(traitRules, kindId, kindConfig.MovementProfiles.TraitRules, ids, movementProfileIds);
        }

        return new CompiledMovementData
        {
            Defaults = defaults,
            SubtypeRules = subtypeRules,
            TraitRules = traitRules,
            TravelStructures = BuildTravelStructureData(config, ids, movementProfileIds),
            RoughnessCostWeight = FixedScalar.FromSingle(
                config.MovementRoughnessCostWeight < 0f
                    ? 0f
                    : config.MovementRoughnessCostWeight),
        };
    }

    private static List<CompiledTravelStructureDefinition> BuildTravelStructureData(
        DomainConfig config,
        DomainIdRegistry ids,
        Dictionary<string, int> movementProfileIds)
    {
        var structures = new List<CompiledTravelStructureDefinition>(config.TravelStructures.Count);
        foreach (var structure in config.TravelStructures)
        {
            if (structure.PlaneCode is not PlaneCodes.Physical and not PlaneCodes.Semantic)
            {
                throw new InvalidOperationException($"Travel structure '{structure.Name}' uses unsupported plane code {structure.PlaneCode}.");
            }

            var structureId = ids.TravelStructureIdOf(structure.Name).Value;
            structures.Add(new CompiledTravelStructureDefinition(
                structureId,
                structure.PlaneCode,
                CompileTravelStructureMovementProfiles(structureId, structure.MovementProfiles, movementProfileIds)));
        }

        return structures;
    }

    private static List<CompiledTravelStructureMovementProfileDefinition> CompileTravelStructureMovementProfiles(
        int structureId,
        IReadOnlyList<TravelStructureMovementProfileConfig> profiles,
        Dictionary<string, int> movementProfileIds)
    {
        var compiled = new List<CompiledTravelStructureMovementProfileDefinition>(profiles.Count);
        foreach (var profile in profiles)
        {
            compiled.Add(new CompiledTravelStructureMovementProfileDefinition(
                structureId,
                ResolveMovementProfileId(profile.Profile, movementProfileIds),
                FixedScalar.FromSingle(profile.MovementCost < 0f ? 0f : profile.MovementCost)));
        }

        return compiled;
    }

    private static void AddMovementProfileDefault(
        List<CompiledMovementProfileDefault> defaults,
        int kindId,
        int planeCode,
        string? profile,
        Dictionary<string, int> movementProfileIds)
    {
        if (string.IsNullOrWhiteSpace(profile))
        {
            return;
        }

        defaults.Add(new CompiledMovementProfileDefault(
            kindId,
            planeCode,
            ResolveMovementProfileId(profile, movementProfileIds)));
    }

    private static void AddMovementProfileSubtypeRules(
        List<CompiledMovementProfileSubtypeRule> rules,
        int kindId,
        IReadOnlyList<MovementProfileSubtypeRuleConfig> source,
        DomainIdRegistry ids,
        Dictionary<string, int> movementProfileIds)
    {
        foreach (var rule in source)
        {
            var subtypeId = ids.SubtypeIdOf(rule.Subtype).Value;
            AddMovementProfileSubtypeRule(rules, kindId, subtypeId, PlaneCodes.Physical, rule.PhysicalProfile, movementProfileIds);
            AddMovementProfileSubtypeRule(rules, kindId, subtypeId, PlaneCodes.Semantic, rule.SemanticProfile, movementProfileIds);
        }
    }

    private static void AddMovementProfileSubtypeRule(
        List<CompiledMovementProfileSubtypeRule> rules,
        int kindId,
        int subtypeId,
        int planeCode,
        string? profile,
        Dictionary<string, int> movementProfileIds)
    {
        if (string.IsNullOrWhiteSpace(profile))
        {
            return;
        }

        rules.Add(new CompiledMovementProfileSubtypeRule(
            kindId,
            subtypeId,
            planeCode,
            ResolveMovementProfileId(profile, movementProfileIds)));
    }

    private static void AddMovementProfileTraitRules(
        List<CompiledMovementProfileTraitRule> rules,
        int kindId,
        IReadOnlyList<MovementProfileTraitRuleConfig> source,
        DomainIdRegistry ids,
        Dictionary<string, int> movementProfileIds)
    {
        foreach (var rule in source)
        {
            var traitId = ids.TraitIdOf(rule.Trait).Value;
            AddMovementProfileTraitRule(rules, kindId, traitId, PlaneCodes.Physical, rule.PhysicalProfile, movementProfileIds);
            AddMovementProfileTraitRule(rules, kindId, traitId, PlaneCodes.Semantic, rule.SemanticProfile, movementProfileIds);
        }
    }

    private static void AddMovementProfileTraitRule(
        List<CompiledMovementProfileTraitRule> rules,
        int kindId,
        int traitId,
        int planeCode,
        string? profile,
        Dictionary<string, int> movementProfileIds)
    {
        if (string.IsNullOrWhiteSpace(profile))
        {
            return;
        }

        rules.Add(new CompiledMovementProfileTraitRule(
            kindId,
            traitId,
            planeCode,
            ResolveMovementProfileId(profile, movementProfileIds)));
    }

    private static int ResolveMovementProfileId(string profile, Dictionary<string, int> movementProfileIds)
    {
        if (movementProfileIds.TryGetValue(profile, out var profileId))
        {
            return profileId;
        }

        throw new InvalidOperationException($"Unknown movement profile '{profile}'.");
    }

    private static LifecycleProgram BuildLifecycleProgram(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var lifecycleRules = new List<LifecycleRule>();

        foreach (var (kind, lifecycle) in config.Lifecycles)
        {
            var kindId = ids.KindIdOf(kind).Value;
            var healthAttributeId = ids.AttributeIdOf(lifecycle.Health).Value;
            lifecycleRules.Add(new LifecycleRule(
                kindId,
                healthAttributeId,
                KernelFixedPoint.AttributeScalar(KernelFixedPoint.AttributeUnits(lifecycle.BaseRate)),
                KernelFixedPoint.AttributeScalar(KernelFixedPoint.AttributeUnits(lifecycle.AgeCoefficient)),
                FixedScalar.FromUnits(200_000L)));
        }

        return new LifecycleProgram
        {
            Rules = lifecycleRules,
        };
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
                FixedScalar.FromSingle(gambit.SuccessChance),
                gambit.ChronicleGranted,
                CompileProviderSelectors(gambit, ids),
                CompilePreconditions(gambit.Precondition, ids),
                CompileExpression(gambit.ScoreExpression, ids),
                CompileSteps(gambit, ids)));
        }

        return new GambitProgram
        {
            EnabledByDefault = gambits.Count > 0,
            Gambits = gambits,
            TraitModifiers = CompileTraitGambitModifiers(config, ids),
        };
    }

    private static List<CompiledTraitGambitModifierRule> CompileTraitGambitModifiers(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var modifiers = new List<CompiledTraitGambitModifierRule>();
        foreach (var (_, kindConfig) in config.Kinds)
        {
            foreach (var trait in kindConfig.Traits)
            {
                foreach (var operation in trait.Operations)
                {
                    if (operation is not TraitGambitModifierOperationConfig modifier)
                    {
                        continue;
                    }

                    modifiers.Add(new CompiledTraitGambitModifierRule(
                        ids.GambitIdOf(modifier.Gambit).Value,
                        ids.TraitIdOf(trait.Name).Value,
                        TraitEffectBearerCode(modifier.TraitBearer),
                        FixedScalar.FromSingle(modifier.ScoreDelta),
                        FixedScalar.FromSingle(modifier.SuccessChanceDelta)));
                }
            }
        }

        return modifiers;
    }

    private static int TraitEffectBearerCode(TraitEffectBearer bearer) => bearer switch
    {
        TraitEffectBearer.Actor => TraitEffectBearerCodes.Actor,
        TraitEffectBearer.GambitTarget => TraitEffectBearerCodes.GambitTarget,
        _ => throw new ArgumentOutOfRangeException(nameof(bearer), bearer, "Unsupported gambit trait bearer."),
    };

    private static int ResolveTargetKindId(BehaviorDefinition behavior, DomainIdRegistry ids)
    {
        foreach (var selector in behavior.ProviderSelectors)
        {
            if (selector.Kind is { } kind)
            {
                return ids.KindIdOf(kind).Value;
            }
        }

        return 0;
    }

    private static BehaviorProgramTargetMode ResolveTargetMode(BehaviorDefinition behavior, int targetKindId)
    {
        if (ContainsPrecondition<TargetIsSelf>(behavior.Precondition))
        {
            return BehaviorProgramTargetMode.Self;
        }

        if (ContainsPrecondition<TargetIsNotSelf>(behavior.Precondition))
        {
            return targetKindId > 0
                ? BehaviorProgramTargetMode.KindOther
                : BehaviorProgramTargetMode.AnyOther;
        }

        return BehaviorProgramTargetMode.Self;
    }

    private static List<BehaviorProgramProviderSelectorRule> CompileProviderSelectors(
        BehaviorDefinition behavior,
        DomainIdRegistry ids)
    {
        var selectors = new List<BehaviorProgramProviderSelectorRule>();
        foreach (var selector in behavior.ProviderSelectors)
        {
            selectors.Add(new BehaviorProgramProviderSelectorRule(
                ProviderSelectorCode(selector),
                selector.Kind is { } kind ? ids.KindIdOf(kind).Value : 0,
                ProviderSelectorValueId(selector, ids),
                ProviderSelectorRelationshipKindId(selector, ids),
                FixedScalar.Zero));
        }

        return selectors;
    }

    private static BehaviorProgramProviderSelector ProviderSelectorCode(AttributeRef selector) => selector.Scope switch
    {
        RefScope.Target => BehaviorProgramProviderSelector.TargetAttribute,
        RefScope.Structural => BehaviorProgramProviderSelector.Structural,
        RefScope.Location => BehaviorProgramProviderSelector.LocationAttribute,
        RefScope.Related or RefScope.Relational => BehaviorProgramProviderSelector.OutgoingRelationship,
        _ => throw new InvalidOperationException($"Unsupported kernel provider selector scope '{selector.Scope}'."),
    };

    private static int ProviderSelectorValueId(AttributeRef selector, DomainIdRegistry ids) => selector.Scope switch
    {
        RefScope.Target or RefScope.Location or RefScope.Related => ids.AttributeIdOf(selector.Name).Value,
        RefScope.Structural => ids.StructuralFactIdOf(selector.Name).Value,
        _ => 0,
    };

    private static int ProviderSelectorRelationshipKindId(AttributeRef selector, DomainIdRegistry ids) =>
        selector.Scope is RefScope.Related or RefScope.Relational
            ? ids.RelationshipKindIdOf(selector.RelationshipKind ?? selector.Name).Value
            : 0;

    private static List<BehaviorProgramPreconditionOp> CompilePreconditions(
        IPrecondition precondition,
        DomainIdRegistry ids)
    {
        var ops = new List<BehaviorProgramPreconditionOp>();
        CompilePreconditionExpression(ops, precondition, ids);
        return ops;
    }

    private static void CompilePreconditionExpression(
        List<BehaviorProgramPreconditionOp> ops,
        IPrecondition precondition,
        DomainIdRegistry ids)
    {
        if (TryCompileCompositePrecondition(ops, precondition, ids) ||
            TryCompileSimplePrecondition(ops, precondition) ||
            TryCompileRangePrecondition(ops, precondition, ids) ||
            TryCompileRelationshipPrecondition(ops, precondition, ids) ||
            TryCompileCulturePrecondition(ops, precondition, ids) ||
            TryCompileGeographyPrecondition(ops, precondition, ids) ||
            TryCompileKernelStatePrecondition(ops, precondition))
        {
            return;
        }

        throw new InvalidOperationException($"Unsupported kernel behavior precondition '{precondition.GetType().Name}'.");
    }

    private static bool TryCompileCompositePrecondition(
        List<BehaviorProgramPreconditionOp> ops,
        IPrecondition precondition,
        DomainIdRegistry ids)
    {
        switch (precondition)
        {
            case AndPrecondition and:
                CompilePreconditionChildren(ops, and.Children, ids, BehaviorProgramPrecondition.And);
                return true;
            case OrPrecondition or:
                CompilePreconditionChildren(ops, or.Children, ids, BehaviorProgramPrecondition.Or);
                return true;
            case NotPrecondition not:
                CompilePreconditionExpression(ops, not.Inner, ids);
                ops.Add(new BehaviorProgramPreconditionOp(BehaviorProgramPrecondition.Not));
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileSimplePrecondition(
        List<BehaviorProgramPreconditionOp> ops,
        IPrecondition precondition)
    {
        switch (precondition)
        {
            case AlwaysTrue:
                ops.Add(new BehaviorProgramPreconditionOp(BehaviorProgramPrecondition.Always));
                return true;
            case TargetIsSelf:
                ops.Add(new BehaviorProgramPreconditionOp(BehaviorProgramPrecondition.TargetIsSelf));
                return true;
            case TargetIsNotSelf:
                ops.Add(new BehaviorProgramPreconditionOp(BehaviorProgramPrecondition.TargetIsNotSelf));
                return true;
            case HasColocatedNamedEntityPrecondition:
                ops.Add(new BehaviorProgramPreconditionOp(BehaviorProgramPrecondition.HasColocatedEntity));
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileRangePrecondition(
        List<BehaviorProgramPreconditionOp> ops,
        IPrecondition precondition,
        DomainIdRegistry ids)
    {
        switch (precondition)
        {
            case SelfAttributeThreshold self:
                AddAttributeRangePrecondition(ops, self.AttributeName, self.Min, self.Max, ids, BehaviorProgramPrecondition.SelfAttributeRange);
                return true;
            case TargetAttributeThreshold target:
                AddAttributeRangePrecondition(ops, target.AttributeName, target.Min, target.Max, ids, BehaviorProgramPrecondition.TargetAttributeRange);
                return true;
            case WithinPlaneDistancePrecondition distance:
                AddPlaneDistancePrecondition(ops, distance, ids);
                return true;
            case WithinTravelReachPrecondition reach:
                ops.Add(new BehaviorProgramPreconditionOp(
                    BehaviorProgramPrecondition.WithinTravelReach,
                    B: PlaneCode(reach.Plane),
                    X: FixedScalar.FromSingle(reach.MaxCost)));
                return true;
            case ContestedInfluencePrecondition contested:
                ops.Add(new BehaviorProgramPreconditionOp(
                    BehaviorProgramPrecondition.ContestedInfluence,
                    PlaneCode(contested.Plane),
                    X: KernelFixedPoint.CultureMassScalar(
                        KernelFixedPoint.CultureMassUnits(contested.SelfMinimum)),
                    Y: KernelFixedPoint.CultureMassScalar(
                        KernelFixedPoint.CultureMassUnits(contested.TargetMinimum))));
                return true;
            case SharedInfluencePrecondition shared:
                ops.Add(new BehaviorProgramPreconditionOp(
                    BehaviorProgramPrecondition.SharedInfluence,
                    PlaneCode(shared.Plane),
                    X: KernelFixedPoint.CultureMassScalar(
                        KernelFixedPoint.CultureMassUnits(shared.SelfMinimum)),
                    Y: KernelFixedPoint.CultureMassScalar(
                        KernelFixedPoint.CultureMassUnits(shared.TargetMinimum))));
                return true;
            case EntityCountCheck count:
                ops.Add(new BehaviorProgramPreconditionOp(BehaviorProgramPrecondition.EntityCountAtLeast, ids.KindIdOf(count.Kind).Value, count.Min));
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileRelationshipPrecondition(
        List<BehaviorProgramPreconditionOp> ops,
        IPrecondition precondition,
        DomainIdRegistry ids)
    {
        switch (precondition)
        {
            case SelfHasRelationshipToTarget relationship:
                AddRelationshipStrengthPrecondition(ops, relationship.RelationshipKind, relationship.MinStrength, ids, BehaviorProgramPrecondition.SelfHasRelationshipToTarget);
                return true;
            case TargetHasRelationshipToSelf relationship:
                AddRelationshipStrengthPrecondition(ops, relationship.RelationshipKind, relationship.MinStrength, ids, BehaviorProgramPrecondition.TargetHasRelationshipToSelf);
                return true;
            case SelfHasOutgoingRelationship relationship:
                AddRelationshipStrengthPrecondition(ops, relationship.RelationshipKind, relationship.MinStrength, ids, BehaviorProgramPrecondition.SelfHasOutgoingRelationship);
                return true;
            case SelfHasRelationshipToEntityOfSubtype relationship:
                // B packs two flags: bit 0 = has minimum strength, bit 1 = scan
                // incoming edges instead of outgoing.
                ops.Add(new BehaviorProgramPreconditionOp(
                    BehaviorProgramPrecondition.SelfHasOutgoingRelationshipToKind,
                    ids.RelationshipKindIdOf(relationship.RelationshipKind).Value,
                    (relationship.MinStrength.HasValue ? 1 : 0) | (relationship.Incoming ? 2 : 0),
                    ids.KindIdOf(relationship.TargetKind.Value).Value,
                    relationship.TargetSubtype is null ? 0 : ids.SubtypeIdOf(relationship.TargetSubtype).Value,
                    X: relationship.MinStrength.HasValue
                        ? KernelFixedPoint.RelationshipStrengthScalar(KernelFixedPoint.RelationshipStrengthUnits(relationship.MinStrength.Value))
                        : FixedScalar.Zero));
                return true;
            case EndpointRelationshipCountBelow relationship:
                ops.Add(new BehaviorProgramPreconditionOp(
                    BehaviorProgramPrecondition.EndpointRelationshipCountBelow,
                    ids.RelationshipKindIdOf(relationship.RelationshipKind).Value,
                    (int)EndpointCode(relationship.Endpoint),
                    relationship.MaximumExclusive,
                    relationship.MinStrength.HasValue ? 1 : 0,
                    X: relationship.MinStrength.HasValue
                        ? KernelFixedPoint.RelationshipStrengthScalar(KernelFixedPoint.RelationshipStrengthUnits(relationship.MinStrength.Value))
                        : FixedScalar.Zero));
                return true;
            case RelatedEntityMatchesTarget relationship:
                ops.Add(new BehaviorProgramPreconditionOp(
                    BehaviorProgramPrecondition.RelatedEntityMatchesTarget,
                    ids.RelationshipKindIdOf(relationship.RelationshipKind).Value));
                return true;
            case RelationshipExists relationship:
                AddRelationshipExistsPrecondition(ops, relationship, ids);
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileCulturePrecondition(
        List<BehaviorProgramPreconditionOp> ops,
        IPrecondition precondition,
        DomainIdRegistry ids)
    {
        switch (precondition)
        {
            case EndpointCultureIs culture:
                ops.Add(new BehaviorProgramPreconditionOp(BehaviorProgramPrecondition.EndpointCultureIs, (int)EndpointCode(culture.Endpoint), ids.CultureIdOf(culture.Culture).Value));
                return true;
            case EndpointSubtypeIs subtype:
                ops.Add(new BehaviorProgramPreconditionOp(BehaviorProgramPrecondition.EndpointSubtypeIs, (int)EndpointCode(subtype.Endpoint), ids.SubtypeIdOf(subtype.Subtype).Value));
                return true;
            default:
                return false;
        }
    }

    private static void CompilePreconditionChildren(
        List<BehaviorProgramPreconditionOp> ops,
        IReadOnlyList<IPrecondition> children,
        DomainIdRegistry ids,
        BehaviorProgramPrecondition operatorCode)
    {
        if (children.Count == 0)
        {
            ops.Add(new BehaviorProgramPreconditionOp(BehaviorProgramPrecondition.Always));
            return;
        }

        CompilePreconditionExpression(ops, children[0], ids);
        for (var i = 1; i < children.Count; i++)
        {
            CompilePreconditionExpression(ops, children[i], ids);
            ops.Add(new BehaviorProgramPreconditionOp(operatorCode));
        }
    }

    private static List<BehaviorProgramExpressionOp> CompileExpression(
        IExpression expression,
        DomainIdRegistry ids)
    {
        var ops = new List<BehaviorProgramExpressionOp>();
        CompileExpression(ops, expression, ids);
        return ops;
    }

    private static void CompileExpression(
        List<BehaviorProgramExpressionOp> ops,
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
            throw new InvalidOperationException("Jitter expressions are not supported by compiled kernel behavior programs.");
        }

        throw new InvalidOperationException($"Unsupported kernel behavior expression '{expression.GetType().Name}'.");
    }

    private static bool TryCompileTerminalExpression(
        List<BehaviorProgramExpressionOp> ops,
        IExpression expression,
        DomainIdRegistry ids)
    {
        switch (expression)
        {
            case Literal literal:
                ops.Add(new BehaviorProgramExpressionOp(
                    BehaviorProgramExpression.Literal,
                    X: FixedScalar.FromSingle(literal.Value)));
                return true;
            case AttributePath attr:
                AddAttributeExpression(ops, attr, ids);
                return true;
            case VariableRef variable:
                ops.Add(new BehaviorProgramExpressionOp(BehaviorProgramExpression.Variable, StableRuntimeCode(variable.VariableName)));
                return true;
            case Distance:
                ops.Add(new BehaviorProgramExpressionOp(BehaviorProgramExpression.Distance));
                return true;
            case SameCulture:
                ops.Add(new BehaviorProgramExpressionOp(BehaviorProgramExpression.SameCulture));
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileBinaryExpression(
        List<BehaviorProgramExpressionOp> ops,
        IExpression expression,
        DomainIdRegistry ids)
    {
        switch (expression)
        {
            case Add add:
                AddBinaryExpression(ops, add.Left, add.Right, ids, BehaviorProgramExpression.Add);
                return true;
            case Subtract subtract:
                AddBinaryExpression(ops, subtract.Left, subtract.Right, ids, BehaviorProgramExpression.Subtract);
                return true;
            case Multiply multiply:
                AddBinaryExpression(ops, multiply.Left, multiply.Right, ids, BehaviorProgramExpression.Multiply);
                return true;
            case Divide divide:
                AddBinaryExpression(ops, divide.Left, divide.Right, ids, BehaviorProgramExpression.Divide);
                return true;
            case Min min:
                AddBinaryExpression(ops, min.A, min.B, ids, BehaviorProgramExpression.Min);
                return true;
            case Max max:
                AddBinaryExpression(ops, max.A, max.B, ids, BehaviorProgramExpression.Max);
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileUnaryExpression(
        List<BehaviorProgramExpressionOp> ops,
        IExpression expression,
        DomainIdRegistry ids)
    {
        switch (expression)
        {
            case Clamp clamp:
                CompileExpression(ops, clamp.Value, ids);
                CompileExpression(ops, clamp.Min, ids);
                CompileExpression(ops, clamp.Max, ids);
                ops.Add(new BehaviorProgramExpressionOp(BehaviorProgramExpression.Clamp));
                return true;
            case Abs abs:
                CompileExpression(ops, abs.Value, ids);
                ops.Add(new BehaviorProgramExpressionOp(BehaviorProgramExpression.Abs));
                return true;
            default:
                return false;
        }
    }

    private static void AddAttributeExpression(
        List<BehaviorProgramExpressionOp> ops,
        AttributePath attr,
        DomainIdRegistry ids)
    {
        ops.Add(new BehaviorProgramExpressionOp(
            attr.EntityRef == "target"
                ? BehaviorProgramExpression.TargetAttribute
                : BehaviorProgramExpression.SelfAttribute,
            ids.AttributeIdOf(attr.AttributeName).Value));
    }

    private static void AddBinaryExpression(
        List<BehaviorProgramExpressionOp> ops,
        IExpression left,
        IExpression right,
        DomainIdRegistry ids,
        BehaviorProgramExpression operatorCode)
    {
        CompileExpression(ops, left, ids);
        CompileExpression(ops, right, ids);
        ops.Add(new BehaviorProgramExpressionOp(operatorCode));
    }

    private static List<BehaviorProgramStepRule> CompileSteps(
        BehaviorDefinition behavior,
        DomainIdRegistry ids)
    {
        var steps = new List<BehaviorProgramStepRule>();
        foreach (var mutation in behavior.Mutations)
        {
            CompileStep(steps, mutation, ids);
        }

        return steps;
    }

    private static void CompileStep(
        List<BehaviorProgramStepRule> steps,
        IMutation mutation,
        DomainIdRegistry ids)
    {
        if (TryCompileCreateStep(steps, mutation, ids) ||
            TryCompileAttributeStep(steps, mutation, ids) ||
            TryCompileVariableStep(steps, mutation, ids) ||
            TryCompileKernelStateStep(steps, mutation) ||
            TryCompileMapStep(steps, mutation, ids) ||
            TryCompileMovementStep(steps, mutation))
        {
            return;
        }

        throw new InvalidOperationException($"Unsupported kernel behavior mutation '{mutation.GetType().Name}'.");
    }

    private static bool TryCompileCreateStep(
        List<BehaviorProgramStepRule> steps,
        IMutation mutation,
        DomainIdRegistry ids)
    {
        switch (mutation)
        {
            case CreateEntityMutation createEntity:
                AddCreateEntityStep(steps, createEntity, ids);
                return true;
            case CreateRelationshipMutation createRelationship:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.CreateFixedRelationship,
                    ids.RelationshipKindIdOf(createRelationship.RelationshipKind).Value,
                    createRelationship.SourceId.Value,
                    createRelationship.TargetId.Value,
                    (int)createRelationship.Polarity,
                    KernelFixedPoint.RelationshipStrengthScalar(KernelFixedPoint.RelationshipStrengthUnits(createRelationship.Strength))));
                return true;
            case CreateContextRelationshipMutation createRelationship:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.CreateEndpointRelationship,
                    ids.RelationshipKindIdOf(createRelationship.RelationshipKind).Value,
                    (int)EndpointCode(createRelationship.Source),
                    (int)EndpointCode(createRelationship.Target),
                    (int)createRelationship.Polarity,
                    KernelFixedPoint.RelationshipStrengthScalar(KernelFixedPoint.RelationshipStrengthUnits(createRelationship.Strength))));
                return true;
            case SetContextRelationshipStrengthMutation relationshipStrength:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.SetEndpointRelationshipStrength,
                    ids.RelationshipKindIdOf(relationshipStrength.RelationshipKind).Value,
                    (int)EndpointCode(relationshipStrength.Source),
                    (int)EndpointCode(relationshipStrength.Target),
                    X: KernelFixedPoint.RelationshipStrengthScalar(KernelFixedPoint.RelationshipStrengthUnits(relationshipStrength.Strength))));
                return true;
            case CullEndpointIncomingRelationshipsMutation cullIncoming:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.CullEndpointIncomingRelationships,
                    ids.RelationshipKindIdOf(cullIncoming.RelationshipKind).Value,
                    (int)EndpointCode(cullIncoming.Endpoint)));
                return true;
            case RemoveContextRelationshipMutation removeRelationship:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.RemoveEndpointRelationship,
                    ids.RelationshipKindIdOf(removeRelationship.RelationshipKind).Value,
                    (int)EndpointCode(removeRelationship.Source),
                    (int)EndpointCode(removeRelationship.Target)));
                return true;
            case SetTargetCultureFromSelfMutation:
                steps.Add(new BehaviorProgramStepRule(BehaviorProgramOperation.SetTargetCultureFromSelf));
                return true;
            default:
                return TryCompileHistoricalStep(steps, mutation, ids);
        }
    }

    private static bool TryCompileHistoricalStep(
        List<BehaviorProgramStepRule> steps,
        IMutation mutation,
        DomainIdRegistry ids)
    {
        switch (mutation)
        {
            case SetHistoricalMutation historical:
                steps.Add(new BehaviorProgramStepRule(BehaviorProgramOperation.SetHistorical, historical.EntityId.Value));
                return true;
            case SetTargetHistoricalMutation:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.SetHistorical,
                    B: (int)BehaviorProgramEndpoint.Target));
                return true;
            case SetSelfHistoricalMutation:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.SetHistorical,
                    B: (int)BehaviorProgramEndpoint.Self));
                return true;
            case InstallDistrictMutation installDistrict:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.InstallDistrict,
                    ids.RelationshipKindIdOf(installDistrict.ReawakenedRelationshipKind).Value,
                    C: (int)installDistrict.ReawakenedRelationshipPolarity,
                    X: KernelFixedPoint.RelationshipStrengthScalar(
                        KernelFixedPoint.RelationshipStrengthUnits(
                            installDistrict.ReawakenedRelationshipStrength))));
                return true;
            case ReawakenDistrictMutation:
                steps.Add(new BehaviorProgramStepRule(BehaviorProgramOperation.ReawakenDistrict));
                return true;
            case DemoteSelfStageMutation:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.DemoteStage,
                    B: (int)BehaviorProgramEndpoint.Self));
                return true;
            case DemoteTargetStageMutation:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.DemoteStage,
                    B: (int)BehaviorProgramEndpoint.Target));
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileAttributeStep(
        List<BehaviorProgramStepRule> steps,
        IMutation mutation,
        DomainIdRegistry ids)
    {
        switch (mutation)
        {
            case ModifySelfAttributeMutation modify:
                AddAttributeStep(steps, BehaviorProgramOperation.AddAttribute, modify.AttributeName, modify.Delta, ids);
                return true;
            case ModifyTargetAttributeMutation modify:
                AddAttributeStep(steps, BehaviorProgramOperation.AddTargetAttribute, modify.AttributeName, modify.Delta, ids);
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

    private static bool TryCompileVariableStep(
        List<BehaviorProgramStepRule> steps,
        IMutation mutation,
        DomainIdRegistry ids)
    {
        switch (mutation)
        {
            case CalculateMutation calculate:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.CalculateVariable,
                    StableRuntimeCode(calculate.VariableName),
                    Expression: CompileExpression(calculate.Expression, ids)));
                return true;
            case ModifySelfByVariableMutation modify:
                AddVariableAttributeStep(steps, BehaviorProgramOperation.AddAttributeFromVariable, modify.AttributeName, modify.VariableName, ids);
                return true;
            case ModifyTargetByVariableMutation modify:
                AddVariableAttributeStep(steps, BehaviorProgramOperation.AddTargetAttributeFromVariable, modify.AttributeName, modify.VariableName, ids);
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileMapStep(
        List<BehaviorProgramStepRule> steps,
        IMutation mutation,
        DomainIdRegistry ids)
    {
        switch (mutation)
        {
            case SetTargetCoverageTerrainMutation setTerrain:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.SetTargetCoverageTerrain,
                    ids.TerrainIdOf(setTerrain.TerrainName).Value));
                return true;
            case ForgeCellVerticalMutation forge:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.ForgeCellVertical,
                    forge.Direction,
                    ids.TerrainIdOf(forge.TerrainName).Value));
                return true;
            case BuildTravelStructureLineMutation buildStructure:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.BuildTravelStructureLine,
                    ids.TravelStructureIdOf(buildStructure.StructureName).Value,
                    buildStructure.PlaneCode,
                    buildStructure.MaxDistance));
                return true;
            case ErodeTargetCultureMutation erode:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.ErodeTargetCulture,
                    erode.Radius,
                    X: KernelFixedPoint.CultureMassScalar(
                        KernelFixedPoint.CultureMassUnits(erode.AmountPerCell))));
                return true;
            default:
                return false;
        }
    }

    private static bool TryCompileMovementStep(List<BehaviorProgramStepRule> steps, IMutation mutation)
    {
        switch (mutation)
        {
            case MoveTowardTargetMutation:
                steps.Add(new BehaviorProgramStepRule(BehaviorProgramOperation.MoveTowardTargetPhysical));
                return true;
            case WanderMutation:
                steps.Add(new BehaviorProgramStepRule(BehaviorProgramOperation.WanderPhysical));
                return true;
            case MoveTowardTargetOnSemanticPlaneMutation:
                steps.Add(new BehaviorProgramStepRule(BehaviorProgramOperation.MoveTowardTargetSemantic));
                return true;
            case MoveTargetToSelfPhysicalMutation:
                steps.Add(new BehaviorProgramStepRule(BehaviorProgramOperation.MoveTargetToSelfPhysical));
                return true;
            case WanderOnSemanticPlaneMutation:
                steps.Add(new BehaviorProgramStepRule(BehaviorProgramOperation.WanderSemantic));
                return true;
            default:
                return false;
        }
    }

    private static void AddCreateEntityStep(
        List<BehaviorProgramStepRule> steps,
        CreateEntityMutation createEntity,
        DomainIdRegistry ids)
    {
        steps.Add(new BehaviorProgramStepRule(
            BehaviorProgramOperation.CreateEntity,
            ids.KindIdOf(createEntity.Kind).Value,
            ids.SubtypeIdOf(createEntity.Subtype).Value,
            ids.CultureIdOf(createEntity.Culture).Value,
            createEntity.Coordinates.HasValue ? 1 : 0,
            createEntity.Coordinates?.Q ?? 0,
            createEntity.Coordinates?.R ?? 0));
    }

    private static void AddAttributeStep(
        List<BehaviorProgramStepRule> steps,
        BehaviorProgramOperation operation,
        string attributeName,
        float delta,
        DomainIdRegistry ids)
    {
        steps.Add(new BehaviorProgramStepRule(
            operation,
            ids.AttributeIdOf(attributeName).Value,
            X: KernelFixedPoint.AttributeScalar(KernelFixedPoint.AttributeUnits(delta))));
    }

    private static void AddTransferAttributeStep(
        List<BehaviorProgramStepRule> steps,
        TransferAttributeMutation transfer,
        DomainIdRegistry ids)
    {
        steps.Add(new BehaviorProgramStepRule(
            BehaviorProgramOperation.TransferAttribute,
            (int)EndpointCode(transfer.Source),
            ids.AttributeIdOf(transfer.SourceAttribute).Value,
            (int)EndpointCode(transfer.Destination),
            ids.AttributeIdOf(transfer.DestinationAttribute).Value,
            KernelFixedPoint.AttributeScalar(KernelFixedPoint.AttributeUnits(transfer.Amount))));
    }

    private static void AddRelatedEntitiesAttributeStep(
        List<BehaviorProgramStepRule> steps,
        ModifyRelatedEntitiesAttributeMutation related,
        DomainIdRegistry ids)
    {
        steps.Add(new BehaviorProgramStepRule(
            BehaviorProgramOperation.AddRelatedEntitiesAttribute,
            ids.RelationshipKindIdOf(related.RelationshipKind).Value,
            ids.AttributeIdOf(related.AttributeName).Value,
            related.Direction == RelationshipTraversalDirection.Incoming
                ? (int)BehaviorProgramRelationshipTraversalDirection.Incoming
                : (int)BehaviorProgramRelationshipTraversalDirection.Outgoing,
            related.UseTargetEntity ? 1 : 0,
            KernelFixedPoint.AttributeScalar(KernelFixedPoint.AttributeUnits(related.Delta))));
    }

    private static void AddVariableAttributeStep(
        List<BehaviorProgramStepRule> steps,
        BehaviorProgramOperation operation,
        string attributeName,
        string variableName,
        DomainIdRegistry ids)
    {
        steps.Add(new BehaviorProgramStepRule(
            operation,
            ids.AttributeIdOf(attributeName).Value,
            StableRuntimeCode(variableName),
            X: FixedScalar.One));
    }

}
