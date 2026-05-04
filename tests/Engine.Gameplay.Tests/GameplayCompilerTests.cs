namespace Engine.Gameplay.Tests;

using Engine.Constants;
using Engine.Gameplay;

public sealed class GameplayCompilerTests
{
    [Fact]
    public void CompileGameplay_ReturnsResultForEmptyDefinition()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition(), new EmptyGameplayContext());

        Assert.Equal(0, result.ModuleCount);
    }

    [Fact]
    public void Validate_RejectsSemanticTerrainTargetsBeforeRuntime()
    {
        var result = GameplayValidator.Validate(new GameplayDefinition
        {
            LowAltitudeVerbs =
            [
                new MovementLowAltitudeVerbConfig
                {
                    Id = "bad_semantic_terrain",
                    Kind = "actor",
                    Plane = "semantic",
                    TargetMode = LowAltitudeVerbTargetMode.Terrain,
                    Terrains = new HashSet<string>(StringComparer.Ordinal) { "ocean" },
                },
            ],
        });

        Assert.Contains(result.Errors, error => error.Code == "InvalidTerrainPlane");
    }

    [Fact]
    public void Validate_RejectsPhysicalCoverageOperationsOnSemanticPlane()
    {
        var result = GameplayValidator.Validate(new GameplayDefinition
        {
            LowAltitudeVerbs =
            [
                new CoverageExpansionLowAltitudeVerbConfig
                {
                    Id = "bad_semantic_claim",
                    Kind = "actor",
                    Plane = "semantic",
                    TargetMode = LowAltitudeVerbTargetMode.OwnInfluence,
                    Delta = 1f,
                    GrowthChance = 1f,
                    RequiredMapValue = 50f,
                },
            ],
        });

        Assert.Contains(result.Errors, error => error.Code == "InvalidVerbPlane");
    }

    [Fact]
    public void Validate_RejectsInvalidSemanticPlaneBounds()
    {
        var result = GameplayValidator.Validate(new GameplayDefinition
        {
            SemanticPlane = new SemanticPlaneConfig
            {
                MinQ = 4,
                MaxQ = -4,
                MinR = -4,
                MaxR = 4,
            },
        });

        Assert.Contains(result.Errors, error => error.Code == "InvalidSemanticPlaneBounds");
    }

    [Fact]
    public void Validate_RejectsInfluenceShapeUsingMovementOperation()
    {
        var result = GameplayValidator.Validate(new GameplayDefinition
        {
            LowAltitudeVerbs =
            [
                new InfluenceLowAltitudeVerbConfig
                {
                    Id = "bad_influence_shape",
                    Kind = "actor",
                    InfluenceOperation = LowAltitudeVerbOperation.Move,
                    TargetMode = LowAltitudeVerbTargetMode.OwnInfluence,
                    Delta = 1f,
                },
            ],
        });

        Assert.Contains(result.Errors, error => error.Code == "InvalidInfluenceShapeOperation");
    }

    [Fact]
    public void ValidateRelationshipMaintenance_RejectsInfluenceRuleWithoutPlane()
    {
        var errors = GameplayValidator.ValidateRelationshipMaintenance(
            "claims",
            [
                new RelationshipMaintenanceRuleConfig
                {
                    Condition = RelationshipMaintenanceCondition.TargetInfluenceAtSource,
                    Threshold = 50f,
                    Delta = 0.01f,
                },
            ]);

        Assert.Contains(errors, error => error.Code == "MissingRelationshipMaintenancePlane");
    }

    [Fact]
    public void ValidateRelationshipMaintenance_RejectsPlaneOnPlaneFreeRule()
    {
        var errors = GameplayValidator.ValidateRelationshipMaintenance(
            "kinship",
            [
                new RelationshipMaintenanceRuleConfig
                {
                    Condition = RelationshipMaintenanceCondition.SameCulture,
                    Plane = "physical",
                    Delta = 0.01f,
                },
            ]);

        Assert.Contains(errors, error => error.Code == "InvalidRelationshipMaintenancePlane");
    }

    [Fact]
    public void ValidateRelationshipMaintenance_AcceptsExplicitSemanticContactPlane()
    {
        var errors = GameplayValidator.ValidateRelationshipMaintenance(
            "echoes",
            [
                new RelationshipMaintenanceRuleConfig
                {
                    Condition = RelationshipMaintenanceCondition.ContactMemory,
                    Plane = "semantic",
                    Threshold = 2f,
                    Delta = 0.01f,
                },
            ]);

        Assert.Empty(errors);
    }

    [Fact]
    public void CompileGameplay_UsesShapeTablePrimitiveStepsForCoverage()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition
        {
            LowAltitudeVerbs =
            [
                new CoverageExpansionLowAltitudeVerbConfig
                {
                    Id = "claim",
                    Kind = "actor",
                    Plane = "physical",
                    TargetMode = LowAltitudeVerbTargetMode.OwnInfluence,
                    Delta = 1f,
                    GrowthChance = 1f,
                    RequiredMapValue = 50f,
                },
            ],
        }, new TestGameplayContext());

        var verb = Assert.Single(result.Programs.LowAltitudeVerbs.Verbs);
        Assert.Equal(ProgramOpcodeCodes.TryClaimCell, verb.OperationCode);
        Assert.Equal(
            [ProgramOpcodeCodes.AddOwnedCellScalar, ProgramOpcodeCodes.TryClaimCell],
            verb.PrimitiveSteps.Select(step => step.Opcode).ToArray());
    }

    [Fact]
    public void CompileGameplay_PropagatesSemanticPlaneBounds()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition
        {
            SemanticPlane = new SemanticPlaneConfig
            {
                MinQ = -7,
                MaxQ = 9,
                MinR = -5,
                MaxR = 11,
            },
        }, new TestGameplayContext());

        Assert.Equal(-7, result.Programs.LowAltitudeVerbs.SemanticBounds.MinQ);
        Assert.Equal(9, result.Programs.LowAltitudeVerbs.SemanticBounds.MaxQ);
        Assert.Equal(-5, result.Programs.LowAltitudeVerbs.SemanticBounds.MinR);
        Assert.Equal(11, result.Programs.LowAltitudeVerbs.SemanticBounds.MaxR);
    }

    [Fact]
    public void Validate_RejectsForeignInfluenceTargetingOutsideForeignInfluenceReducers()
    {
        var result = GameplayValidator.Validate(new GameplayDefinition
        {
            LowAltitudeVerbs =
            [
                new InfluenceLowAltitudeVerbConfig
                {
                    Id = "bad_filter",
                    Kind = "actor",
                    InfluenceOperation = LowAltitudeVerbOperation.GrowInfluence,
                    TargetMode = LowAltitudeVerbTargetMode.OwnInfluence,
                    Delta = 1f,
                    ForeignInfluenceTargeting = new ForeignInfluenceTargetingConfig
                    {
                        BlockPositiveRelationships = true,
                    },
                },
            ],
        });

        Assert.Contains(result.Errors, error => error.Code == "InvalidForeignInfluenceTargeting");
    }

    [Fact]
    public void CompileGameplay_CompilesForeignInfluenceTargeting()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition
        {
            LowAltitudeVerbs =
            [
                new InfluenceLowAltitudeVerbConfig
                {
                    Id = "reduce",
                    Kind = "actor",
                    InfluenceOperation = LowAltitudeVerbOperation.ReduceInfluence,
                    TargetMode = LowAltitudeVerbTargetMode.ForeignInfluence,
                    Delta = 1f,
                    ForeignInfluenceTargeting = new ForeignInfluenceTargetingConfig
                    {
                        BlockPositiveRelationships = true,
                        AllowNegativeRelationships = true,
                        MinimumRelationshipStrength = 0.2f,
                        AllowUnrelatedInfluenceAtOrAbove = 50f,
                    },
                },
            ],
        }, new TestGameplayContext());

        var verb = Assert.Single(result.Programs.LowAltitudeVerbs.Verbs);
        Assert.True(verb.ForeignInfluenceFilterEnabled);
        Assert.True(verb.ForeignInfluenceBlockPositiveRelationships);
        Assert.True(verb.ForeignInfluenceAllowNegativeRelationships);
        Assert.Equal(0.2f, verb.ForeignInfluenceMinimumRelationshipStrength);
        Assert.Equal(50f, verb.ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove);
    }

    [Fact]
    public void CompileGameplay_CompilesMapSignalScalarInputs()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition
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
                                Attribute = "health",
                                AttractorScale = 0.25f,
                            },
                        ],
                    },
                ],
            },
        }, new TestGameplayContext());

        var kindSignal = Assert.Single(result.Programs.MapSignals.Kinds);
        var scalarInput = Assert.Single(kindSignal.AttractorScalarInputs);
        Assert.Equal(40, scalarInput.AttributeId);
        Assert.Equal(0.25f, scalarInput.AttractorScale);
    }

    [Fact]
    public void CompileGameplay_CompilesTraitOwnedOperations()
    {
        var result = GameplayCompiler.CompileGameplay(
            new GameplayDefinition(),
            new TestGameplayContext(
            [
                new TraitOperationDefinition(
                    "actor",
                    "ranging",
                    new TraitLowAltitudeWeightOperationConfig
                    {
                        Verb = "explore",
                        ExploreWeightDelta = 1.25f,
                    }),
                new TraitOperationDefinition(
                    "artifact",
                    "restorative",
                    new TraitAttributeDeltaOperationConfig
                    {
                        TargetKind = "actor",
                        Attribute = "health",
                        Condition = EffectCondition.RelatedEntityDistance,
                        TraitBearer = TraitEffectBearer.RelatedEntity,
                        RelationshipKind = "holds",
                        RelationshipDirection = EffectRelationshipDirection.Outgoing,
                        DeltaPerTick = 0.04f,
                    }),
            ]));

        var modifier = Assert.Single(result.Programs.LowAltitudeVerbs.TraitWeightModifiers);
        Assert.Equal(50, modifier.TraitId);
        Assert.Equal(10, modifier.VerbId);
        Assert.Equal(1.25f, modifier.ExploreWeightDelta);

        var effect = Assert.Single(result.Programs.Effects.AttributeDeltaEffects);
        Assert.Equal(0, effect.RequiredTargetTraitId);
        Assert.Equal(50, effect.RequiredRelatedTraitId);
        Assert.Equal(EffectConditionCodes.RelatedEntityDistance, effect.ConditionCode);
    }

    private sealed class EmptyGameplayContext : IGameplayCompilationContext
    {
        public int ResolveKindId(string kind, string context) => throw new NotSupportedException();
        public int PrimaryPlaneCodeForKind(string kind, string context) => throw new NotSupportedException();
        public int ResolveVerbId(string verb) => throw new NotSupportedException();
        public int ResolveGeneratorId(string generator) => throw new NotSupportedException();
        public int ResolveGambitId(string gambit, string context) => throw new NotSupportedException();
        public int ResolveTerrainId(string terrain) => throw new NotSupportedException();
        public int ResolveRelationshipKindId(string relationship) => throw new NotSupportedException();
        public int ResolveAttributeId(string attribute) => throw new NotSupportedException();
        public int ResolveTraitId(string trait) => throw new NotSupportedException();
        public bool TerrainExists(string terrain) => throw new NotSupportedException();
        public bool RelationshipExists(string relationship) => throw new NotSupportedException();
        public bool KindHasAttribute(string kind, string attribute) => throw new NotSupportedException();
        public IReadOnlyList<AffordanceDefinition> Affordances() => [];
        public IReadOnlyList<TraitOperationDefinition> TraitOperations() => [];
    }

    private sealed class TestGameplayContext : IGameplayCompilationContext
    {
        private readonly IReadOnlyList<TraitOperationDefinition> _traitOperations;

        public TestGameplayContext(IReadOnlyList<TraitOperationDefinition>? traitOperations = null)
        {
            _traitOperations = traitOperations ?? [];
        }

        public int ResolveKindId(string kind, string context) => 1;
        public int PrimaryPlaneCodeForKind(string kind, string context) => PlaneCodes.Physical;
        public int ResolveVerbId(string verb) => 10;
        public int ResolveGeneratorId(string generator) => 60;
        public int ResolveGambitId(string gambit, string context) => 70;
        public int ResolveTerrainId(string terrain) => 20;
        public int ResolveRelationshipKindId(string relationship) => 30;
        public int ResolveAttributeId(string attribute) => 40;
        public int ResolveTraitId(string trait) => 50;
        public bool TerrainExists(string terrain) => true;
        public bool RelationshipExists(string relationship) => true;
        public bool KindHasAttribute(string kind, string attribute) => true;
        public IReadOnlyList<AffordanceDefinition> Affordances() => [];
        public IReadOnlyList<TraitOperationDefinition> TraitOperations() => _traitOperations;
    }
}
