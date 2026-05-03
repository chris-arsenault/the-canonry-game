namespace TheCanonryGame.Game.Runtime;

using Engine.Host;
using Engine.Projection;

using TheCanonryGame.Game.Chronicle;
using TheCanonryGame.Game.Domain;

internal interface IGameChronicleFeed
{
    IReadOnlyList<string> Lines { get; }
}

internal sealed class GameRuntimeReadModels
{
    internal GameRuntimeReadModels(KernelUiRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        Map = runtime.Map;
        Entities = runtime.Ui;
        Symbols = runtime.Symbols;
        Knowledge = GameDomainKnowledge.FromCompiledDomain(runtime.CompiledDomain, runtime.Symbols);
        Chronicle = new GameRuntimeChronicle(runtime.Chronicle, runtime.Provenance, runtime.Symbols, Knowledge);
    }

    public HexMapProjection Map { get; }

    public UiProjectionState Entities { get; }

    public IGameChronicleFeed Chronicle { get; }

    public IProjectionSymbolCatalog Symbols { get; }

    public GameDomainKnowledge Knowledge { get; }
}
