namespace Engine.Gameplay.Tests;

using Engine.Constants;
using Engine.Gameplay;

public sealed class GameplayCompilerProgramTests
{
    [Fact]
    public void CompileGameplay_CompilesInitialInfluenceAndGeneratorAffordances()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition
        {
            Modules = [new GameplayModule("core")],
            InitialInfluence =
            [
                new InitialInfluenceRule("actor", "semantic", 0.75f),
            ],
        }, new ConfigurableGameplayContext(
            generatorAffordances:
            [
                new GeneratorAffordanceDefinition("actor", "candidate", "binding"),
            ]));

        Assert.Equal(1, result.ModuleCount);
        var initialInfluence = Assert.Single(result.Programs.LowAltitudeVerbs.InitialInfluence);
        Assert.Equal(1, initialInfluence.KindId);
        Assert.Equal(PlaneCodes.Semantic, initialInfluence.PlaneCode);
        Assert.Equal(0.75f, initialInfluence.InfluenceValue);

        var affordance = Assert.Single(result.Programs.LowAltitudeVerbs.GeneratorAffordances);
        Assert.Equal(1, affordance.ActorKindId);
        Assert.Equal(5, affordance.CandidateKindId);
        Assert.Equal(4, affordance.BindingKindId);
        Assert.Equal(1f, affordance.AffordanceScore);
    }

    [Fact]
    public void CompileGameplay_UsesPrimaryPlaneWhenVerbPlaneIsOmitted()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition
        {
            LowAltitudeVerbs =
            [
                new MovementLowAltitudeVerbConfig
                {
                    Id = "visit",
                    Kind = "actor",
                    TargetMode = LowAltitudeVerbTargetMode.Explore,
                },
            ],
        }, new ConfigurableGameplayContext(primaryPlaneCode: PlaneCodes.Semantic));

        var verb = Assert.Single(result.Programs.LowAltitudeVerbs.Verbs);
        Assert.Equal(PlaneCodes.Semantic, verb.PlaneCode);
    }

    [Fact]
    public void CompileGameplay_CompilesRelationshipAndTerrainTargetVerbs()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition
        {
            LowAltitudeVerbs =
            [
                new MovementLowAltitudeVerbConfig
                {
                    Id = "visit",
                    Kind = "actor",
                    Plane = "physical",
                    TargetMode = LowAltitudeVerbTargetMode.RelationshipTarget,
                    RelationshipKind = "allies",
                    RelationshipDirection = LowAltitudeVerbRelationshipDirection.Outgoing,
                    AffinityRelationshipKind = "claims",
                    AffinityRelationshipDirection = LowAltitudeVerbRelationshipDirection.Incoming,
                    RelationshipAffinityWeight = 2.5f,
                    TargetRadius = 4,
                    CandidateSampleCount = 7,
                    Weight = 1.2f,
                    ExploreWeight = 0.8f,
                },
                new MovementLowAltitudeVerbConfig
                {
                    Id = "explore",
                    Kind = "actor",
                    Plane = "physical",
                    TargetMode = LowAltitudeVerbTargetMode.Terrain,
                    Terrains = new SortedSet<string>(StringComparer.Ordinal)
                    {
                        "ocean",
                        "tundra",
                    },
                },
            ],
        }, new ConfigurableGameplayContext());

        var relationshipVerb = result.Programs.LowAltitudeVerbs.Verbs[0];
        Assert.Equal(TargetSelectorCodes.RelationshipTarget, relationshipVerb.TargetModeCode);
        Assert.Equal(31, relationshipVerb.RelationshipKindId);
        Assert.Equal(RelationshipMatchDirectionCodes.Outgoing, relationshipVerb.RelationshipDirectionCode);
        Assert.Equal(32, relationshipVerb.AffinityRelationshipKindId);
        Assert.Equal(RelationshipMatchDirectionCodes.Incoming, relationshipVerb.AffinityRelationshipDirectionCode);
        Assert.Equal(2.5f, relationshipVerb.RelationshipAffinityWeight);
        Assert.Equal(4, relationshipVerb.TargetRadius);
        Assert.Equal(7, relationshipVerb.CandidateSampleCount);
        Assert.Equal(1.2f, relationshipVerb.Weight);
        Assert.Equal(0.8f, relationshipVerb.ExploreWeight);

        var terrainVerb = result.Programs.LowAltitudeVerbs.Verbs[1];
        Assert.Equal(TargetSelectorCodes.Terrain, terrainVerb.TargetModeCode);
        Assert.Equal([20, 21], terrainVerb.TerrainIds);
    }

    [Fact]
    public void CompileGameplay_CompilesAnchorRelocationPrimitiveStepsAndInputs()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition
        {
            LowAltitudeVerbs =
            [
                new AnchorRelocationLowAltitudeVerbConfig
                {
                    Id = "relocate",
                    Kind = "actor",
                    Plane = "physical",
                    TargetMode = LowAltitudeVerbTargetMode.OwnInfluence,
                    Delta = 0.5f,
                    Radius = 2,
                    RelocationChance = 0.25f,
                    RequiredMapValue = 55f,
                },
            ],
        }, new ConfigurableGameplayContext());

        var verb = Assert.Single(result.Programs.LowAltitudeVerbs.Verbs);
        Assert.Equal(ProgramOpcodeCodes.TryRelocateEntity, verb.OperationCode);
        Assert.Equal(
            [ProgramOpcodeCodes.AddOwnedCellScalar, ProgramOpcodeCodes.TryRelocateEntity],
            verb.PrimitiveSteps.Select(step => step.Opcode).ToArray());
        Assert.Equal(0.5f, verb.InfluenceDelta);
        Assert.Equal(2, verb.InfluenceRadius);
        Assert.Equal(0.25f, verb.HeadquartersMoveChance);
        Assert.Equal(55f, verb.RequiredInfluence);
    }

    [Fact]
    public void CompileGameplay_CompilesRelationshipStrengthPrimitiveStepAndInputs()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition
        {
            LowAltitudeVerbs =
            [
                new RelationshipStrengthLowAltitudeVerbConfig
                {
                    Id = "tend_alliance",
                    Kind = "actor",
                    Plane = "semantic",
                    TargetMode = LowAltitudeVerbTargetMode.RelationshipTarget,
                    RelationshipKind = "allies",
                    RelationshipDirection = LowAltitudeVerbRelationshipDirection.Incoming,
                    Delta = 0.075f,
                },
            ],
        }, new ConfigurableGameplayContext());

        var verb = Assert.Single(result.Programs.LowAltitudeVerbs.Verbs);
        Assert.Equal(ProgramOpcodeCodes.AdjustRelationshipStrength, verb.OperationCode);
        Assert.Equal(TargetSelectorCodes.RelationshipTarget, verb.TargetModeCode);
        Assert.Equal(31, verb.RelationshipKindId);
        Assert.Equal(RelationshipMatchDirectionCodes.Incoming, verb.RelationshipDirectionCode);
        Assert.Equal(0.075f, verb.RelationshipStrengthDelta);
        Assert.Equal(
            [ProgramOpcodeCodes.AdjustRelationshipStrength],
            verb.PrimitiveSteps.Select(step => step.Opcode).ToArray());
    }

    [Fact]
    public void CompileGameplay_CompilesTerrainAndRelationshipMapSignals()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition
        {
            MapSignals = new MapSignalsConfig
            {
                SemanticBaseCapacity = 3.5f,
                CrowdingRadius = 4,
                Terrains =
                [
                    new TerrainMapSignalConfig
                    {
                        Terrain = "ocean",
                        BaseCapacity = 8f,
                    },
                ],
                Relationships =
                [
                    new RelationshipMapSignalConfig
                    {
                        RelationshipKind = "allies",
                        SignalRadius = 6,
                        SupportEmission = 0.4f,
                        ThreatEmission = 0.2f,
                    },
                ],
            },
        }, new ConfigurableGameplayContext());

        Assert.Equal(3.5f, result.Programs.MapSignals.SemanticBaseCapacity);
        Assert.Equal(4, result.Programs.MapSignals.CrowdingRadius);

        var terrain = Assert.Single(result.Programs.MapSignals.Terrains);
        Assert.Equal(20, terrain.TerrainId);
        Assert.Equal(8f, terrain.BaseCapacity);

        var relationship = Assert.Single(result.Programs.MapSignals.Relationships);
        Assert.Equal(31, relationship.RelationshipKindId);
        Assert.Equal(6, relationship.SignalRadius);
        Assert.Equal(0.4f, relationship.SupportEmission);
        Assert.Equal(0.2f, relationship.ThreatEmission);
    }

    [Fact]
    public void CompileGameplay_RejectsUnknownTerrainMapSignal()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            GameplayCompiler.CompileGameplay(new GameplayDefinition
            {
                MapSignals = new MapSignalsConfig
                {
                    Terrains =
                    [
                        new TerrainMapSignalConfig
                        {
                            Terrain = "unknown",
                            BaseCapacity = 1f,
                        },
                    ],
                },
            }, new ConfigurableGameplayContext(terrainExists: false)));

        Assert.Contains("unknown terrain", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompileGameplay_RejectsUnknownRelationshipMapSignal()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            GameplayCompiler.CompileGameplay(new GameplayDefinition
            {
                MapSignals = new MapSignalsConfig
                {
                    Relationships =
                    [
                        new RelationshipMapSignalConfig
                        {
                            RelationshipKind = "unknown",
                            SupportEmission = 1f,
                        },
                    ],
                },
            }, new ConfigurableGameplayContext(relationshipExists: false)));

        Assert.Contains("unknown relationship", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompileGameplay_RejectsUnknownMapSignalScalarAttribute()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            GameplayCompiler.CompileGameplay(new GameplayDefinition
            {
                MapSignals = new MapSignalsConfig
                {
                    Kinds =
                    [
                        new KindMapSignalConfig
                        {
                            Kind = "actor",
                            AttractorScalarInputs =
                            [
                                new MapSignalScalarInputConfig
                                {
                                    Attribute = "missing",
                                    AttractorScale = 1f,
                                },
                            ],
                        },
                    ],
                },
            }, new ConfigurableGameplayContext(kindHasAttribute: false)));

        Assert.Contains("unknown attribute", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompileGameplay_CompilesAttributeEffectsWithRelationshipAndPlaneCodes()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition
        {
            Effects = new EffectsConfig
            {
                AttributeDeltaEffects =
                [
                    new AttributeDeltaEffectConfig
                    {
                        Kind = "actor",
                        Attribute = "health",
                        Condition = EffectCondition.RelatedEntityDistance,
                        RelationshipKind = "allies",
                        RelationshipDirection = EffectRelationshipDirection.Incoming,
                        Plane = "semantic",
                        MaxDistance = 5,
                        Threshold = 2f,
                        MinimumRelationshipStrength = 0.3f,
                        DeltaPerTick = -0.04f,
                    },
                    new AttributeDeltaEffectConfig
                    {
                        Kind = "actor",
                        Attribute = "warmth",
                        Condition = EffectCondition.MapThreatAtLocation,
                        Plane = "physical",
                        Threshold = 0.6f,
                        DeltaPerTick = -0.02f,
                    },
                ],
            },
        }, new ConfigurableGameplayContext());

        var relationshipEffect = result.Programs.Effects.AttributeDeltaEffects[0];
        Assert.Equal(1, relationshipEffect.KindId);
        Assert.Equal(40, relationshipEffect.AttributeId);
        Assert.Equal(EffectConditionCodes.RelatedEntityDistance, relationshipEffect.ConditionCode);
        Assert.Equal(31, relationshipEffect.RelationshipKindId);
        Assert.Equal(RelationshipMatchDirectionCodes.Incoming, relationshipEffect.RelationshipDirectionCode);
        Assert.Equal(PlaneCodes.Semantic, relationshipEffect.PlaneCode);
        Assert.Equal(5, relationshipEffect.MaxDistance);
        Assert.Equal(2f, relationshipEffect.Threshold);
        Assert.Equal(0.3f, relationshipEffect.MinimumRelationshipStrength);
        Assert.Equal(-0.04f, relationshipEffect.DeltaPerTick);

        var mapEffect = result.Programs.Effects.AttributeDeltaEffects[1];
        Assert.Equal(41, mapEffect.AttributeId);
        Assert.Equal(EffectConditionCodes.LocalNegativeSignal, mapEffect.ConditionCode);
        Assert.Equal(0, mapEffect.RelationshipKindId);
        Assert.Equal(PlaneCodes.Physical, mapEffect.PlaneCode);
    }

    [Fact]
    public void CompileGameplay_RejectsEffectUnknownAttribute()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            GameplayCompiler.CompileGameplay(new GameplayDefinition
            {
                Effects = new EffectsConfig
                {
                    AttributeDeltaEffects =
                    [
                        new AttributeDeltaEffectConfig
                        {
                            Kind = "actor",
                            Attribute = "missing",
                            Condition = EffectCondition.MapSupportAtLocation,
                            DeltaPerTick = 0.1f,
                        },
                    ],
                },
            }, new ConfigurableGameplayContext(kindHasAttribute: false)));

        Assert.Contains("unknown attribute", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompileGameplay_RejectsRelationshipEffectWithoutKnownRelationship()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            GameplayCompiler.CompileGameplay(new GameplayDefinition
            {
                Effects = new EffectsConfig
                {
                    AttributeDeltaEffects =
                    [
                        new AttributeDeltaEffectConfig
                        {
                            Kind = "actor",
                            Attribute = "health",
                            Condition = EffectCondition.RelatedInfluenceAtLocation,
                            DeltaPerTick = -0.1f,
                        },
                    ],
                },
            }, new ConfigurableGameplayContext()));

        Assert.Contains("unknown relationship", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompileGameplay_CompilesTraitTargetBearerAttributeEffect()
    {
        var result = GameplayCompiler.CompileGameplay(
            new GameplayDefinition(),
            new ConfigurableGameplayContext(
                traitOperations:
                [
                    new TraitOperationDefinition(
                        "actor",
                        "restorative",
                        new TraitAttributeDeltaOperationConfig
                        {
                            TargetKind = "actor",
                            Attribute = "health",
                            Condition = EffectCondition.MapSupportAtLocation,
                            TraitBearer = TraitEffectBearer.Target,
                            DeltaPerTick = 0.05f,
                        }),
                ]));

        var effect = Assert.Single(result.Programs.Effects.AttributeDeltaEffects);
        Assert.Equal(50, effect.RequiredTargetTraitId);
        Assert.Equal(0, effect.RequiredRelatedTraitId);
        Assert.Equal(EffectConditionCodes.LocalPositiveSignal, effect.ConditionCode);
    }

    [Fact]
    public void CompileGameplay_RejectsTraitAttributeDeltaWithUnknownAttribute()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            GameplayCompiler.CompileGameplay(
                new GameplayDefinition(),
                new ConfigurableGameplayContext(
                    kindHasAttribute: false,
                    traitOperations:
                    [
                        new TraitOperationDefinition(
                            "actor",
                            "restorative",
                            new TraitAttributeDeltaOperationConfig
                            {
                                TargetKind = "actor",
                                Attribute = "missing",
                                Condition = EffectCondition.MapSupportAtLocation,
                                DeltaPerTick = 0.05f,
                            }),
                    ])));

        Assert.Contains("unknown attribute", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompileGameplay_RejectsTraitRelatedBearerWithoutRelationshipCondition()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            GameplayCompiler.CompileGameplay(
                new GameplayDefinition(),
                new ConfigurableGameplayContext(
                    traitOperations:
                    [
                        new TraitOperationDefinition(
                            "actor",
                            "restorative",
                            new TraitAttributeDeltaOperationConfig
                            {
                                TargetKind = "actor",
                                Attribute = "health",
                                Condition = EffectCondition.MapSupportAtLocation,
                                TraitBearer = TraitEffectBearer.RelatedEntity,
                                DeltaPerTick = 0.05f,
                            }),
                    ])));

        Assert.Contains("related-entity bearer", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompileGameplay_RejectsTraitAttributeDeltaWithGambitOnlyBearer()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            GameplayCompiler.CompileGameplay(
                new GameplayDefinition(),
                new ConfigurableGameplayContext(
                    traitOperations:
                    [
                        new TraitOperationDefinition(
                            "actor",
                            "restorative",
                            new TraitAttributeDeltaOperationConfig
                            {
                                TargetKind = "actor",
                                Attribute = "health",
                                Condition = EffectCondition.MapSupportAtLocation,
                                TraitBearer = TraitEffectBearer.Actor,
                                DeltaPerTick = 0.05f,
                            }),
                    ])));

        Assert.Contains("gambit-only bearer", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompileGameplay_CompilesTraitWeightModifierWithoutVerbAsWildcard()
    {
        var result = GameplayCompiler.CompileGameplay(
            new GameplayDefinition(),
            new ConfigurableGameplayContext(
                traitOperations:
                [
                    new TraitOperationDefinition(
                        "actor",
                        "ranging",
                        new TraitLowAltitudeWeightOperationConfig
                        {
                            WeightDelta = 0.2f,
                            ExploreWeightDelta = 0.3f,
                            StalenessWeightDelta = 0.4f,
                            HotspotWeightDelta = 0.5f,
                            CrowdingWeightDelta = 0.6f,
                            GeneratorAffordanceWeightDelta = 0.7f,
                            RelationshipAffinityWeightDelta = 0.8f,
                        }),
                ]));

        var modifier = Assert.Single(result.Programs.LowAltitudeVerbs.TraitWeightModifiers);
        Assert.Equal(1, modifier.KindId);
        Assert.Equal(50, modifier.TraitId);
        Assert.Equal(0, modifier.VerbId);
        Assert.Equal(0.2f, modifier.WeightDelta);
        Assert.Equal(0.8f, modifier.RelationshipAffinityWeightDelta);
    }

    private sealed class ConfigurableGameplayContext : IGameplayCompilationContext
    {
        private readonly IReadOnlyList<GeneratorAffordanceDefinition> _generatorAffordances;
        private readonly IReadOnlyList<TraitOperationDefinition> _traitOperations;
        private readonly int _primaryPlaneCode;
        private readonly bool _terrainExists;
        private readonly bool _relationshipExists;
        private readonly bool _kindHasAttribute;

        public ConfigurableGameplayContext(
            IReadOnlyList<GeneratorAffordanceDefinition>? generatorAffordances = null,
            IReadOnlyList<TraitOperationDefinition>? traitOperations = null,
            int primaryPlaneCode = PlaneCodes.Physical,
            bool terrainExists = true,
            bool relationshipExists = true,
            bool kindHasAttribute = true)
        {
            _generatorAffordances = generatorAffordances ?? [];
            _traitOperations = traitOperations ?? [];
            _primaryPlaneCode = primaryPlaneCode;
            _terrainExists = terrainExists;
            _relationshipExists = relationshipExists;
            _kindHasAttribute = kindHasAttribute;
        }

        public int ResolveKindId(string kind, string context) => kind switch
        {
            "actor" => 1,
            "artifact" => 2,
            "settlement" => 3,
            "binding" => 4,
            "candidate" => 5,
            _ => 99,
        };

        public int PrimaryPlaneCodeForKind(string kind, string context) => _primaryPlaneCode;

        public int ResolveVerbId(string verb) => verb switch
        {
            "explore" => 10,
            "visit" => 11,
            "relocate" => 12,
            "claim" => 13,
            _ => 19,
        };

        public int ResolveTerrainId(string terrain) => terrain switch
        {
            "ocean" => 20,
            "tundra" => 21,
            "crags" => 22,
            _ => 29,
        };

        public int ResolveRelationshipKindId(string relationship) => relationship switch
        {
            "holds" => 30,
            "allies" => 31,
            "claims" => 32,
            _ => 39,
        };

        public int ResolveAttributeId(string attribute) => attribute switch
        {
            "health" => 40,
            "warmth" => 41,
            _ => 49,
        };

        public int ResolveTraitId(string trait) => trait switch
        {
            "ranging" => 50,
            "restorative" => 50,
            _ => 59,
        };

        public bool TerrainExists(string terrain) => _terrainExists;
        public bool RelationshipExists(string relationship) => _relationshipExists;
        public bool KindHasAttribute(string kind, string attribute) => _kindHasAttribute;
        public IReadOnlyList<GeneratorAffordanceDefinition> GeneratorAffordances() => _generatorAffordances;
        public IReadOnlyList<TraitOperationDefinition> TraitOperations() => _traitOperations;
    }
}
