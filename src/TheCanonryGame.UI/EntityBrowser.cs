#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using Engine.Projection;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// Left sidebar listing all entities. Click an entity to open its floating card.
/// </summary>
public partial class EntityBrowser : PanelContainer
{
    private readonly ViewSync<RegistryItemKey, Control> _items = new();
    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private VBoxContainer? _listContainer;
    private PackedScene? _entityRowScene;
    private PackedScene? _kindHeaderScene;

    [Signal]
    public delegate void EntitySelectedEventHandler(int entityIdValue);

    public void Initialize(UiProjectionState? projection, IProjectionSymbolCatalog? symbols)
    {
        _projection = projection;
        _symbols = symbols;
        Refresh();
    }

    public override void _Ready()
    {
        _entityRowScene = GD.Load<PackedScene>("res://scenes/ui/EntityRow.tscn");
        _kindHeaderScene = GD.Load<PackedScene>("res://scenes/ui/EntityKindHeader.tscn");

        CustomMinimumSize = new Vector2(260, 0);
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeStyleboxOverride("panel", UiTheme.RailPanelStyle(onLeft: true));

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", UiTokens.Space2);
        AddChild(vbox);

        var title = new Label
        {
            Text = "Registry",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontOverride("font", UiTheme.DisplayFont);
        title.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        title.AddThemeColorOverride("font_color", UiTokens.Vellum);
        vbox.AddChild(title);

        var subtitle = new Label
        {
            Text = "ALL KNOWN ENTITIES",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        subtitle.AddThemeFontOverride("font", UiTheme.BodyFont);
        subtitle.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        subtitle.AddThemeColorOverride("font_color", UiTokens.Whisper);
        vbox.AddChild(subtitle);

        vbox.AddChild(new ColorRect
        {
            Color = UiTokens.Hairline,
            CustomMinimumSize = new Vector2(0, 1),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        vbox.AddChild(scroll);

        _listContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _listContainer.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(_listContainer);

        Refresh();
    }

    public void Refresh()
    {
        if (_projection is null || _listContainer is null)
        {
            return;
        }

        var snapshot = BuildSnapshot();
        _items.Sync(
            _listContainer,
            snapshot,
            CreateItemNode,
            UpdateItemNode);
    }

    private List<KeyValuePair<RegistryItemKey, RegistryItemData>> BuildSnapshot()
    {
        var items = new List<KeyValuePair<RegistryItemKey, RegistryItemData>>();
        var firstKind = true;

        foreach (var group in _projection!.Entities.Values
            .OrderBy(entity => ProjectionDisplay.StableName(_symbols, ProjectionSymbolKind.Kind, entity.KindId, $"kind-{entity.KindId}"), StringComparer.Ordinal)
            .ThenBy(entity => ProjectionDisplay.EntityName(_symbols, entity), StringComparer.Ordinal)
            .GroupBy(entity => entity.KindId))
        {
            var entities = group.ToArray();
            if (entities.Length == 0)
            {
                continue;
            }

            var kindStableName = ProjectionDisplay.StableName(_symbols, ProjectionSymbolKind.Kind, group.Key, $"kind-{group.Key}");
            var displayName = ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Kind, group.Key, kindStableName);
            var accent = UiTokens.KindAccent(kindStableName);
            items.Add(KeyValuePair.Create(
                new RegistryItemKey(RegistryItemType.KindHeader, group.Key, 0),
                RegistryItemData.Header(displayName, accent, entities.Length, hasLeadingGap: !firstKind)));
            firstKind = false;

            foreach (var entity in entities)
            {
                items.Add(KeyValuePair.Create(
                    new RegistryItemKey(RegistryItemType.EntityRow, entity.KindId, entity.EntityId),
                    RegistryItemData.ForEntity(entity)));
            }
        }

        return items;
    }

    private Control CreateItemNode(RegistryItemData data)
    {
        if (data.ItemType == RegistryItemType.KindHeader)
        {
            return _kindHeaderScene?.Instantiate<EntityKindHeader>()
                ?? throw new InvalidOperationException("EntityKindHeader scene is not loaded.");
        }

        var row = _entityRowScene?.Instantiate<EntityRow>()
            ?? throw new InvalidOperationException("EntityRow scene is not loaded.");
        row.EntitySelected += OnRowEntitySelected;
        return row;
    }

    private void UpdateItemNode(Control node, RegistryItemData data)
    {
        if (data.ItemType == RegistryItemType.KindHeader && node is EntityKindHeader header)
        {
            header.BindTo(data.DisplayName, data.Accent, data.Count, data.HasLeadingGap);
            return;
        }

        if (data.Entity is not null && node is EntityRow row)
        {
            row.BindTo(data.Entity, _symbols, _projection?.CurrentTick ?? 0);
        }
    }

    private void OnRowEntitySelected(int entityIdValue) =>
        EmitSignal(SignalName.EntitySelected, entityIdValue);

    private readonly record struct RegistryItemKey(RegistryItemType ItemType, int KindId, int EntityId);

    private enum RegistryItemType
    {
        KindHeader,
        EntityRow,
    }

    private sealed record RegistryItemData(
        RegistryItemType ItemType,
        UiEntityProjection? Entity,
        string DisplayName,
        Color Accent,
        int Count,
        bool HasLeadingGap)
    {
        public static RegistryItemData Header(string displayName, Color accent, int count, bool hasLeadingGap) =>
            new(RegistryItemType.KindHeader, null, displayName, accent, count, hasLeadingGap);

        public static RegistryItemData ForEntity(UiEntityProjection entity) =>
            new(RegistryItemType.EntityRow, entity, string.Empty, default, 0, false);
    }
}
