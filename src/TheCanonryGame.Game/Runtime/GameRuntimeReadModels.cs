namespace TheCanonryGame.Game.Runtime;

using Engine.Host;
using Engine.Kinds;
using Engine.Projection;

using TheCanonryGame.Game.Chronicle;
using TheCanonryGame.Game.Domain;

internal interface IGameChronicleFeed
{
    long Version { get; }

    IReadOnlyList<UiChronicleEntry> Entries { get; }

    IReadOnlyList<string> Lines { get; }

    void Refresh();
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
        ConfigureHierarchyTags(runtime);
        ConfigureCultureSlots(runtime);
        ConfigureHealthAttributes(runtime);
    }

    public HexMapProjection Map { get; }

    public UiProjectionState Entities { get; }

    public IGameChronicleFeed Chronicle { get; }

    public IProjectionSymbolCatalog Symbols { get; }

    public GameDomainKnowledge Knowledge { get; }

    private void ConfigureHierarchyTags(KernelUiRuntime runtime)
    {
        var compiled = runtime.CompiledDomain;
        if (compiled is null)
        {
            return;
        }

        var tierByKindId = new Dictionary<int, EntityTier>(compiled.DefinitionGraph.Kinds.Count);
        foreach (var kindNode in compiled.DefinitionGraph.Kinds.Values)
        {
            if (kindNode.Tier != EntityTier.None)
            {
                tierByKindId[kindNode.RuntimeId] = kindNode.Tier;
            }
        }

        var affiliationIds = new HashSet<int>();
        foreach (var relationshipNode in compiled.DefinitionGraph.Relationships.Values)
        {
            if (relationshipNode.SourceConfig.IsAffiliation)
            {
                affiliationIds.Add(relationshipNode.RuntimeId);
            }
        }

        Entities.ConfigureHierarchyTags(tierByKindId, affiliationIds);
    }

    private void ConfigureCultureSlots(KernelUiRuntime runtime)
    {
        var compiled = runtime.CompiledDomain;
        if (compiled is null)
        {
            return;
        }

        var slots = new List<UiCultureSlotDefinitionProjection>();
        foreach (var culture in compiled.DefinitionGraph.Cultures.Values)
        {
            foreach (var slot in culture.Slots.Values)
            {
                slots.Add(new UiCultureSlotDefinitionProjection(
                    new UiCultureSlotKey(culture.RuntimeId, slot.SlotId),
                    culture.RuntimeId,
                    slot.SlotId,
                    $"{culture.Id}.{slot.Id}",
                    slot.DisplayName,
                    slot.Description,
                    slot.PolarityCode));
            }
        }

        Entities.ConfigureCultureSlots(slots);
    }

    private void ConfigureHealthAttributes(KernelUiRuntime runtime)
    {
        var compiled = runtime.CompiledDomain;
        if (compiled is null)
        {
            return;
        }

        var healthAttributes = new List<UiHealthAttributeDefinitionProjection>();
        foreach (var kind in compiled.DefinitionGraph.Kinds.Values)
        {
            if (kind.HealthAttribute is not { } healthAttribute ||
                !kind.Attributes.TryGetValue(healthAttribute, out var attribute))
            {
                continue;
            }

            healthAttributes.Add(new UiHealthAttributeDefinitionProjection(
                kind.RuntimeId,
                compiled.Ids.AttributeIdOf(healthAttribute.Value).Value,
                attribute.SourceConfig.Min,
                attribute.SourceConfig.Max,
                attribute.SourceConfig.Default));
        }

        Entities.ConfigureHealthAttributes(healthAttributes);
    }
}
