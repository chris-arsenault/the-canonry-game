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
                new LowAltitudeVerbConfig
                {
                    Id = "bad_semantic_terrain",
                    Kind = "actor",
                    Plane = "semantic",
                    Operation = LowAltitudeVerbOperation.Move,
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
                new LowAltitudeVerbConfig
                {
                    Id = "bad_semantic_claim",
                    Kind = "actor",
                    Plane = "semantic",
                    Operation = LowAltitudeVerbOperation.ExpandCoverage,
                    TargetMode = LowAltitudeVerbTargetMode.OwnInfluence,
                    InfluenceDelta = 1f,
                    CoverageGrowthChance = 1f,
                    RequiredInfluence = 50f,
                },
            ],
        });

        Assert.Contains(result.Errors, error => error.Code == "InvalidVerbPlane");
    }

    [Fact]
    public void CompileGameplay_UsesShapeTablePrimitiveStepsForCoverage()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition
        {
            LowAltitudeVerbs =
            [
                new LowAltitudeVerbConfig
                {
                    Id = "claim",
                    Kind = "actor",
                    Plane = "physical",
                    Operation = LowAltitudeVerbOperation.ExpandCoverage,
                    TargetMode = LowAltitudeVerbTargetMode.OwnInfluence,
                    InfluenceDelta = 1f,
                    CoverageGrowthChance = 1f,
                    RequiredInfluence = 50f,
                },
            ],
        }, new TestGameplayContext());

        var verb = Assert.Single(result.Programs.LowAltitudeVerbs.Verbs);
        Assert.Equal(ProgramOpcodeCodes.TryClaimCell, verb.OperationCode);
        Assert.Equal(
            [ProgramOpcodeCodes.AddOwnedCellScalar, ProgramOpcodeCodes.TryClaimCell],
            verb.PrimitiveSteps.Select(step => step.Opcode).ToArray());
    }

    private sealed class EmptyGameplayContext : IGameplayCompilationContext
    {
        public int ResolveKindId(string kind, string context) => throw new NotSupportedException();
        public int PrimaryPlaneCodeForKind(string kind, string context) => throw new NotSupportedException();
        public int ResolveVerbId(string verb) => throw new NotSupportedException();
        public int ResolveTerrainId(string terrain) => throw new NotSupportedException();
        public int ResolveRelationshipKindId(string relationship) => throw new NotSupportedException();
        public int ResolveAttributeId(string attribute) => throw new NotSupportedException();
        public bool TerrainExists(string terrain) => throw new NotSupportedException();
        public bool RelationshipExists(string relationship) => throw new NotSupportedException();
        public bool KindHasAttribute(string kind, string attribute) => throw new NotSupportedException();
        public string? HealthAttributeForKind(string kind, string context) => throw new NotSupportedException();
        public IReadOnlyList<GeneratorAffordanceDefinition> GeneratorAffordances() => [];
    }

    private sealed class TestGameplayContext : IGameplayCompilationContext
    {
        public int ResolveKindId(string kind, string context) => 1;
        public int PrimaryPlaneCodeForKind(string kind, string context) => PlaneCodes.Physical;
        public int ResolveVerbId(string verb) => 10;
        public int ResolveTerrainId(string terrain) => 20;
        public int ResolveRelationshipKindId(string relationship) => 30;
        public int ResolveAttributeId(string attribute) => 40;
        public bool TerrainExists(string terrain) => true;
        public bool RelationshipExists(string relationship) => true;
        public bool KindHasAttribute(string kind, string attribute) => true;
        public string? HealthAttributeForKind(string kind, string context) => null;
        public IReadOnlyList<GeneratorAffordanceDefinition> GeneratorAffordances() => [];
    }
}
