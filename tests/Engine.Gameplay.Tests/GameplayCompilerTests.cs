namespace Engine.Gameplay.Tests;

using Engine.Gameplay;

public sealed class GameplayCompilerTests
{
    [Fact]
    public void CompileGameplay_ReturnsResultForEmptyDefinition()
    {
        var result = GameplayCompiler.CompileGameplay(new GameplayDefinition(), new EmptyGameplayContext());

        Assert.Equal(0, result.ModuleCount);
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
}
