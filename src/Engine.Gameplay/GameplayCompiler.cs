namespace Engine.Gameplay;

using Engine.Constants;

public static class GameplayCompiler
{
    public static GameplayCompileResult CompileGameplay(
        GameplayDefinition definition,
        IGameplayCompilationContext context)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);
        var validation = GameplayValidator.Validate(definition);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.Errors[0].Message);
        }

        return new GameplayCompileResult(
            definition.Modules.Count,
            new GameplayKernelProgramSet
            {
                LowAltitudeVerbs = CompileLowAltitudeVerbProgram(definition, context),
                MapActivity = new MapActivityProgram(),
                MapSignals = CompileMapSignalsProgram(definition, context),
                Effects = CompileEffectsProgram(definition, context),
            });
    }

    private static LowAltitudeVerbProgram CompileLowAltitudeVerbProgram(
        GameplayDefinition definition,
        IGameplayCompilationContext context)
    {
        var rules = new List<LowAltitudeVerbRule>();
        foreach (var verb in definition.LowAltitudeVerbs)
        {
            var shape = LowAltitudeVerbShape.For(verb.Operation);
            var foreignTargeting = CompileForeignInfluenceTargeting(verb);
            var ownerPlaneCode = verb.Plane is null
                ? context.PrimaryPlaneCodeForKind(verb.Kind, $"Low-altitude verb '{verb.Id}' kind")
                : PlaneCode(verb.Plane);
            rules.Add(new LowAltitudeVerbRule(
                context.ResolveKindId(verb.Kind, $"Low-altitude verb '{verb.Id}' kind"),
                context.ResolveVerbId(verb.Id),
                ownerPlaneCode,
                shape.OperationCode,
                TargetModeCode(verb.TargetMode),
                verb.TargetKind is null
                    ? 0
                    : context.ResolveKindId(verb.TargetKind, $"Low-altitude verb '{verb.Id}' target kind"),
                verb.RelationshipKind is null ? 0 : context.ResolveRelationshipKindId(verb.RelationshipKind),
                RelationshipDirectionCode(verb.RelationshipDirection),
                CompileTerrainIds(verb.Terrains, context),
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
                foreignTargeting.Enabled,
                foreignTargeting.BlockPositiveRelationships,
                foreignTargeting.AllowNegativeRelationships,
                foreignTargeting.MinimumRelationshipStrength,
                foreignTargeting.AllowUnrelatedInfluenceAtOrAbove,
                verb.AffinityRelationshipKind is null ? 0 : context.ResolveRelationshipKindId(verb.AffinityRelationshipKind),
                RelationshipDirectionCode(verb.AffinityRelationshipDirection),
                verb.RelationshipAffinityWeight,
                verb.InfluenceDelta,
                verb.InfluenceRadius,
                verb.CoverageGrowthChance,
                verb.HeadquartersMoveChance,
                verb.RequiredInfluence,
                verb.RelationshipStrengthDelta,
                shape.PrimitiveSteps));
        }

        return new LowAltitudeVerbProgram
        {
            SemanticBounds = CompileSemanticBounds(definition.SemanticPlane),
            Verbs = rules,
            InitialInfluence = CompileInitialInfluenceRules(definition, context),
            GeneratorAffordances = CompileGeneratorAffordances(context),
            TraitWeightModifiers = CompileTraitLowAltitudeWeightModifiers(context),
        };
    }

    private static CompiledForeignInfluenceTargeting CompileForeignInfluenceTargeting(LowAltitudeVerbConfig verb)
    {
        if (verb.ForeignInfluenceTargeting is not { } targeting)
        {
            return new CompiledForeignInfluenceTargeting(false, false, false, 0f, -1f);
        }

        return new CompiledForeignInfluenceTargeting(
            true,
            targeting.BlockPositiveRelationships,
            targeting.AllowNegativeRelationships,
            targeting.MinimumRelationshipStrength,
            targeting.AllowUnrelatedInfluenceAtOrAbove);
    }

    private static CompiledSemanticPlaneBounds CompileSemanticBounds(SemanticPlaneConfig semanticPlane) =>
        new(semanticPlane.MinQ, semanticPlane.MaxQ, semanticPlane.MinR, semanticPlane.MaxR);

    private static List<CompiledInitialInfluenceRule> CompileInitialInfluenceRules(
        GameplayDefinition definition,
        IGameplayCompilationContext context)
    {
        var rules = new List<CompiledInitialInfluenceRule>();
        foreach (var rule in definition.InitialInfluence)
        {
            rules.Add(new CompiledInitialInfluenceRule(
                context.ResolveKindId(rule.Kind, "Initial influence kind"),
                PlaneCode(rule.Plane),
                rule.InfluenceValue));
        }

        return rules;
    }

    private static List<CompiledGeneratorAffordanceRule> CompileGeneratorAffordances(
        IGameplayCompilationContext context)
    {
        var rules = new List<CompiledGeneratorAffordanceRule>();
        foreach (var affordance in context.GeneratorAffordances())
        {
            rules.Add(new CompiledGeneratorAffordanceRule(
                context.ResolveKindId(affordance.ActorKind, "Generator affordance actor kind"),
                context.ResolveKindId(affordance.CandidateKind, "Generator affordance candidate kind"),
                context.ResolveKindId(affordance.BindingKind, "Generator affordance binding kind"),
                1f));
        }

        return rules;
    }

    private static List<CompiledTraitLowAltitudeWeightModifierRule> CompileTraitLowAltitudeWeightModifiers(
        IGameplayCompilationContext context)
    {
        var rules = new List<CompiledTraitLowAltitudeWeightModifierRule>();
        foreach (var definition in context.TraitOperations())
        {
            if (definition.Operation is not TraitLowAltitudeWeightOperationConfig operation)
            {
                continue;
            }

            rules.Add(new CompiledTraitLowAltitudeWeightModifierRule(
                context.ResolveKindId(definition.OwnerKind, $"Trait '{definition.Trait}' owner kind"),
                context.ResolveTraitId(definition.Trait),
                operation.Verb is null ? 0 : context.ResolveVerbId(operation.Verb),
                operation.WeightDelta,
                operation.ExploreWeightDelta,
                operation.StalenessWeightDelta,
                operation.HotspotWeightDelta,
                operation.CrowdingWeightDelta,
                operation.GeneratorAffordanceWeightDelta,
                operation.RelationshipAffinityWeightDelta));
        }

        return rules;
    }


    private static MapSignalsProgram CompileMapSignalsProgram(
        GameplayDefinition definition,
        IGameplayCompilationContext context)
    {
        var mapSignals = definition.MapSignals;
        return new MapSignalsProgram
        {
            SemanticBaseCapacity = mapSignals.SemanticBaseCapacity,
            CrowdingRadius = mapSignals.CrowdingRadius,
            Terrains = CompileTerrainMapSignals(mapSignals, context),
            Kinds = CompileKindMapSignals(mapSignals, context),
            Relationships = CompileRelationshipMapSignals(mapSignals, context),
        };
    }

    private static List<TerrainMapSignalRule> CompileTerrainMapSignals(
        MapSignalsConfig mapSignals,
        IGameplayCompilationContext context)
    {
        var rules = new List<TerrainMapSignalRule>();
        foreach (var terrain in mapSignals.Terrains)
        {
            if (!context.TerrainExists(terrain.Terrain))
            {
                throw new InvalidOperationException(
                    $"Map signal terrain references unknown terrain '{terrain.Terrain}'.");
            }

            rules.Add(new TerrainMapSignalRule(
                context.ResolveTerrainId(terrain.Terrain),
                terrain.BaseCapacity));
        }

        return rules;
    }

    private static List<KindMapSignalRule> CompileKindMapSignals(
        MapSignalsConfig mapSignals,
        IGameplayCompilationContext context)
    {
        var rules = new List<KindMapSignalRule>();
        foreach (var kind in mapSignals.Kinds)
        {
            rules.Add(new KindMapSignalRule(
                context.ResolveKindId(kind.Kind, "Map signal kind"),
                kind.SignalRadius,
                kind.CapacityEmission,
                kind.SupportEmission,
                kind.ThreatEmission,
                kind.BaseAttractorMass,
                kind.CoverageAttractorScale,
                kind.InfluenceAttractorScale,
                kind.ActivityAttractorScale,
                kind.AttractorRadius,
                CompileMapSignalScalarInputs(kind, context)));
        }

        return rules;
    }

    private static List<MapSignalScalarInputRule> CompileMapSignalScalarInputs(
        KindMapSignalConfig kind,
        IGameplayCompilationContext context)
    {
        var scalarInputs = new List<MapSignalScalarInputRule>();
        foreach (var scalar in kind.AttractorScalarInputs)
        {
            if (!context.KindHasAttribute(kind.Kind, scalar.Attribute))
            {
                throw new InvalidOperationException(
                    $"Map signal scalar input references unknown attribute '{scalar.Attribute}'.");
            }

            scalarInputs.Add(new MapSignalScalarInputRule(
                context.ResolveAttributeId(scalar.Attribute),
                scalar.AttractorScale));
        }

        return scalarInputs;
    }

    private static List<RelationshipMapSignalRule> CompileRelationshipMapSignals(
        MapSignalsConfig mapSignals,
        IGameplayCompilationContext context)
    {
        var rules = new List<RelationshipMapSignalRule>();
        foreach (var relationship in mapSignals.Relationships)
        {
            if (!context.RelationshipExists(relationship.RelationshipKind))
            {
                throw new InvalidOperationException(
                    $"Map signal relationship references unknown relationship '{relationship.RelationshipKind}'.");
            }

            rules.Add(new RelationshipMapSignalRule(
                context.ResolveRelationshipKindId(relationship.RelationshipKind),
                relationship.SignalRadius,
                relationship.SupportEmission,
                relationship.ThreatEmission));
        }

        return rules;
    }

    private static EffectsProgram CompileEffectsProgram(
        GameplayDefinition definition,
        IGameplayCompilationContext context)
    {
        var rules = new List<AttributeDeltaEffectRule>();
        foreach (var effect in definition.Effects.AttributeDeltaEffects)
        {
            if (!context.KindHasAttribute(effect.Kind, effect.Attribute))
            {
                throw new InvalidOperationException(
                    $"Attribute delta effect for kind '{effect.Kind}' references unknown attribute '{effect.Attribute}'.");
            }

            if (EffectRequiresRelationship(effect.Condition) &&
                (effect.RelationshipKind is null || !context.RelationshipExists(effect.RelationshipKind)))
            {
                throw new InvalidOperationException(
                    $"Attribute delta effect for kind '{effect.Kind}' references unknown relationship '{effect.RelationshipKind}'.");
            }

            rules.Add(new AttributeDeltaEffectRule(
                context.ResolveKindId(effect.Kind, "Attribute delta effect kind"),
                context.ResolveAttributeId(effect.Attribute),
                ConditionCode(effect.Condition),
                effect.RelationshipKind is null ? 0 : context.ResolveRelationshipKindId(effect.RelationshipKind),
                RelationshipDirectionCode(effect.RelationshipDirection),
                PlaneCode(effect.Plane),
                effect.MaxDistance,
                effect.Threshold,
                effect.MinimumRelationshipStrength,
                effect.DeltaPerTick,
                RequiredTargetTraitId: 0,
                RequiredRelatedTraitId: 0));
        }

        rules.AddRange(CompileTraitAttributeDeltaEffects(context));
        return new EffectsProgram { AttributeDeltaEffects = rules };
    }

    private static List<AttributeDeltaEffectRule> CompileTraitAttributeDeltaEffects(
        IGameplayCompilationContext context)
    {
        var rules = new List<AttributeDeltaEffectRule>();
        foreach (var definition in context.TraitOperations())
        {
            if (definition.Operation is not TraitAttributeDeltaOperationConfig operation)
            {
                continue;
            }

            ValidateTraitAttributeDelta(definition, operation, context);
            var traitId = context.ResolveTraitId(definition.Trait);
            rules.Add(new AttributeDeltaEffectRule(
                context.ResolveKindId(operation.TargetKind, $"Trait '{definition.Trait}' attribute effect target kind"),
                context.ResolveAttributeId(operation.Attribute),
                ConditionCode(operation.Condition),
                operation.RelationshipKind is null ? 0 : context.ResolveRelationshipKindId(operation.RelationshipKind),
                RelationshipDirectionCode(operation.RelationshipDirection),
                PlaneCode(operation.Plane),
                operation.MaxDistance,
                operation.Threshold,
                operation.MinimumRelationshipStrength,
                operation.DeltaPerTick,
                RequiredTargetTraitId: operation.TraitBearer == TraitEffectBearer.Target ? traitId : 0,
                RequiredRelatedTraitId: operation.TraitBearer == TraitEffectBearer.RelatedEntity ? traitId : 0));
        }

        return rules;
    }

    private static void ValidateTraitAttributeDelta(
        TraitOperationDefinition definition,
        TraitAttributeDeltaOperationConfig operation,
        IGameplayCompilationContext context)
    {
        if (!context.KindHasAttribute(operation.TargetKind, operation.Attribute))
        {
            throw new InvalidOperationException(
                $"Trait '{definition.Trait}' attribute delta effect for kind '{operation.TargetKind}' references unknown attribute '{operation.Attribute}'.");
        }

        if (EffectRequiresRelationship(operation.Condition) &&
            (operation.RelationshipKind is null || !context.RelationshipExists(operation.RelationshipKind)))
        {
            throw new InvalidOperationException(
                $"Trait '{definition.Trait}' attribute delta effect for kind '{operation.TargetKind}' references unknown relationship '{operation.RelationshipKind}'.");
        }

        if (!EffectRequiresRelationship(operation.Condition) &&
            operation.TraitBearer == TraitEffectBearer.RelatedEntity)
        {
            throw new InvalidOperationException(
                $"Trait '{definition.Trait}' uses related-entity bearer without a relationship effect condition.");
        }

        if (operation.TraitBearer is TraitEffectBearer.Actor or TraitEffectBearer.GambitTarget)
        {
            throw new InvalidOperationException(
                $"Trait '{definition.Trait}' uses a gambit-only bearer for an attribute delta effect.");
        }
    }

    private static List<int> CompileTerrainIds(IReadOnlySet<string>? terrains, IGameplayCompilationContext context)
    {
        if (terrains is null || terrains.Count == 0)
        {
            return [];
        }

        var terrainIds = new List<int>(terrains.Count);
        foreach (var terrain in terrains)
        {
            terrainIds.Add(context.ResolveTerrainId(terrain));
        }

        return terrainIds;
    }

    private static int TargetModeCode(LowAltitudeVerbTargetMode mode) => mode switch
    {
        LowAltitudeVerbTargetMode.Explore => TargetSelectorCodes.Explore,
        LowAltitudeVerbTargetMode.EntityKind => TargetSelectorCodes.EntityKind,
        LowAltitudeVerbTargetMode.RelationshipTarget => TargetSelectorCodes.RelationshipTarget,
        LowAltitudeVerbTargetMode.Terrain => TargetSelectorCodes.Terrain,
        LowAltitudeVerbTargetMode.OwnInfluence => TargetSelectorCodes.SelfMapValue,
        LowAltitudeVerbTargetMode.ForeignInfluence => TargetSelectorCodes.OtherMapValue,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    private static int RelationshipDirectionCode(LowAltitudeVerbRelationshipDirection direction) => direction switch
    {
        LowAltitudeVerbRelationshipDirection.Outgoing => RelationshipMatchDirectionCodes.Outgoing,
        LowAltitudeVerbRelationshipDirection.Incoming => RelationshipMatchDirectionCodes.Incoming,
        LowAltitudeVerbRelationshipDirection.Either => RelationshipMatchDirectionCodes.Either,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    private static int RelationshipDirectionCode(EffectRelationshipDirection direction) => direction switch
    {
        EffectRelationshipDirection.Outgoing => RelationshipMatchDirectionCodes.Outgoing,
        EffectRelationshipDirection.Incoming => RelationshipMatchDirectionCodes.Incoming,
        EffectRelationshipDirection.Either => RelationshipMatchDirectionCodes.Either,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    private static int ConditionCode(EffectCondition condition) => condition switch
    {
        EffectCondition.RelatedInfluenceAtLocation => EffectConditionCodes.RelationshipMapValueAtLocation,
        EffectCondition.RelatedEntityDistance => EffectConditionCodes.RelatedEntityDistance,
        EffectCondition.MapSupportAtLocation => EffectConditionCodes.LocalPositiveSignal,
        EffectCondition.MapThreatAtLocation => EffectConditionCodes.LocalNegativeSignal,
        EffectCondition.MapCrowdingLoad => EffectConditionCodes.LocalCrowdingLoadSignal,
        EffectCondition.MapCrowdingComfort => EffectConditionCodes.LocalComfortSignal,
        _ => throw new ArgumentOutOfRangeException(nameof(condition), condition, null),
    };

    private static int PlaneCode(string plane) =>
        string.Equals(plane, "semantic", StringComparison.OrdinalIgnoreCase)
            ? PlaneCodes.Semantic
            : PlaneCodes.Physical;

    private static bool EffectRequiresRelationship(EffectCondition condition) =>
        condition is EffectCondition.RelatedInfluenceAtLocation
            or EffectCondition.RelatedEntityDistance;

    private sealed record CompiledForeignInfluenceTargeting(
        bool Enabled,
        bool BlockPositiveRelationships,
        bool AllowNegativeRelationships,
        float MinimumRelationshipStrength,
        float AllowUnrelatedInfluenceAtOrAbove);
}

public interface IGameplayCompilationContext
{
    int ResolveKindId(string kind, string context);
    int PrimaryPlaneCodeForKind(string kind, string context);
    int ResolveVerbId(string verb);
    int ResolveTerrainId(string terrain);
    int ResolveRelationshipKindId(string relationship);
    int ResolveAttributeId(string attribute);
    bool TerrainExists(string terrain);
    bool RelationshipExists(string relationship);
    int ResolveTraitId(string trait);
    bool KindHasAttribute(string kind, string attribute);
    IReadOnlyList<GeneratorAffordanceDefinition> GeneratorAffordances();
    IReadOnlyList<TraitOperationDefinition> TraitOperations();
}

/// <summary>
/// Adapter boundary normally backed by the domain's DomainIdRegistry.
/// </summary>
public sealed record GeneratorAffordanceDefinition(
    string ActorKind,
    string CandidateKind,
    string BindingKind);

/// <summary>
/// Gameplay-owned KernelPrograms payload compiled down for host installation.
/// </summary>
public sealed class GameplayKernelProgramSet
{
    public required LowAltitudeVerbProgram LowAltitudeVerbs { get; init; }
    public required MapActivityProgram MapActivity { get; init; }
    public required MapSignalsProgram MapSignals { get; init; }
    public required EffectsProgram Effects { get; init; }
}

public sealed record GameplayCompileResult(int ModuleCount, GameplayKernelProgramSet Programs);
