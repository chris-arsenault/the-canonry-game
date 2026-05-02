namespace TheCanonryGame.Game.Tests;

using System.Reflection;

public sealed class GameAssemblySmokeTests
{
    [Fact]
    public void GameAssembly_LoadsRuntimeFacade()
    {
        var assembly = Assembly.Load(new AssemblyName("TheCanonryGame.Game"));

        Assert.NotNull(assembly.GetType("TheCanonryGame.Game.CanonryGameRuntime"));
    }
}
