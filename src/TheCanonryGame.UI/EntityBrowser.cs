#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Kinds;
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// Left sidebar registry. Renders active entities as a nested tree grouped by
/// realm and place affiliation. Realms appear at the top level, their controlled
/// places nest below them, and the lives anchored to each place nest beneath
/// it. Entities with no affiliation roll up into a "Wild" section at the bottom.
/// Clicking a row opens that entity's floating card.
/// </summary>
public partial class EntityBrowser : PanelContainer
{
    private const int IndentStepPx = 14;
    private const string WildSectionLabel = "Wild";
    private const string UnaffiliatedPlacesLabel = "Unbound Places";
    private const string UntieredSectionLabel = "Untiered";

    private readonly List<EntityRow> _rowPool = [];
    private readonly List<EntityKindHeader> _headerPool = [];
    private readonly List<Label> _labelPool = [];
    private readonly List<MarginContainer> _activeWrappers = [];

    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private VBoxContainer? _listContainer;
    private PackedScene? _entityRowScene;
    private PackedScene? _kindHeaderScene;

    [Signal]
    public delegate void EntitySelectedEventHandler(int entityIdValue);

    [Signal]
    public delegate void EntityFilterChangedEventHandler();

    /// <summary>Legacy filter API — the registry no longer filters; always 0.</summary>
    public long FilterVersion => 0;

    /// <summary>Legacy filter API — the registry no longer filters; always false.</summary>
    public bool FilterMapToBrowser => false;

    /// <summary>Legacy filter API — the registry no longer filters; always 0.</summary>
    public int SelectedGeneratorFilterId => 0;

    /// <summary>Legacy filter API — the registry no longer filters; always null.</summary>
    public HashSet<int>? VisibleEntityIdsForMap() => null;

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

        AddHeader(vbox);
        AddScrollList(vbox);
        Refresh();
    }

    public void Refresh()
    {
        if (_projection is null || _listContainer is null)
        {
            return;
        }

        ReleaseActiveWrappers();
        var tree = BuildTree();
        RenderTree(tree);
    }

    private void AddHeader(VBoxContainer vbox)
    {
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
            Text = "BY AFFILIATION",
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
    }

    private void AddScrollList(VBoxContainer vbox)
    {
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        vbox.AddChild(scroll);

        _listContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _listContainer.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(_listContainer);
    }

    private RegistryTree BuildTree()
    {
        var tree = new RegistryTree();
        if (_projection is null)
        {
            return tree;
        }

        var byTier = _projection.Entities.Values
            .Where(e => e.StatusCode == EntityStatusCodes.Active)
            .GroupBy(e => _projection.EntityTierOf(e.EntityId))
            .ToDictionary(g => g.Key, g => g.ToList());

        tree.Realms = byTier.GetValueOrDefault(EntityTier.Realm, []);
        tree.Untiered = byTier.GetValueOrDefault(EntityTier.None, []);
        var places = byTier.GetValueOrDefault(EntityTier.Place, []);
        var lives = byTier.GetValueOrDefault(EntityTier.Life, []);
        var realmIds = tree.Realms.Select(r => r.EntityId).ToHashSet();
        var placeIds = places.Select(p => p.EntityId).ToHashSet();

        AssignPlacesToRealms(places, realmIds, tree);
        AssignLivesToPlacesAndRealms(lives, placeIds, realmIds, tree);
        return tree;
    }

    private void AssignPlacesToRealms(
        List<UiEntityProjection> places,
        HashSet<int> realmIds,
        RegistryTree tree)
    {
        foreach (var place in places)
        {
            var realmId = FirstAffiliationTarget(place.EntityId, realmIds);
            if (realmId is { } id)
            {
                AppendChild(tree.PlacesByRealm, id, place);
            }
            else
            {
                tree.OrphanPlaces.Add(place);
            }
        }
    }

    private void AssignLivesToPlacesAndRealms(
        List<UiEntityProjection> lives,
        HashSet<int> placeIds,
        HashSet<int> realmIds,
        RegistryTree tree)
    {
        foreach (var life in lives)
        {
            var placeId = FirstAffiliationTarget(life.EntityId, placeIds);
            if (placeId is { } pid)
            {
                AppendChild(tree.LivesByPlace, pid, life);
                continue;
            }

            var realmId = FirstAffiliationTarget(life.EntityId, realmIds);
            if (realmId is { } rid)
            {
                AppendChild(tree.LivesByRealm, rid, life);
                continue;
            }

            tree.OrphanLives.Add(life);
        }
    }

    private static void AppendChild(
        Dictionary<int, List<UiEntityProjection>> map,
        int key,
        UiEntityProjection entity)
    {
        if (!map.TryGetValue(key, out var list))
        {
            list = [];
            map[key] = list;
        }

        list.Add(entity);
    }

    private int? FirstAffiliationTarget(int entityId, HashSet<int> candidateTargets)
    {
        foreach (var rel in _projection!.ActiveOutgoingRelationships(entityId))
        {
            if (!_projection.IsAffiliationRelationship(rel.Key.RelationshipKindId))
            {
                continue;
            }

            if (candidateTargets.Contains(rel.Key.TargetEntityId))
            {
                return rel.Key.TargetEntityId;
            }
        }

        return null;
    }

    private void RenderTree(RegistryTree tree)
    {
        foreach (var realm in tree.Realms.OrderBy(NameOf, StringComparer.Ordinal))
        {
            RenderRealmSubtree(realm, tree);
        }

        RenderOrphanSection(UnaffiliatedPlacesLabel, tree.OrphanPlaces, tree, includeLivesUnderPlaces: true);
        RenderOrphanLives(tree.OrphanLives);
        RenderUntiered(tree.Untiered);
    }

    private void RenderRealmSubtree(UiEntityProjection realm, RegistryTree tree)
    {
        AddEntityRow(realm, indentLevel: 0);

        foreach (var place in tree.PlacesByRealm.GetValueOrDefault(realm.EntityId, []).OrderBy(NameOf, StringComparer.Ordinal))
        {
            AddEntityRow(place, indentLevel: 1);
            RenderLivesInPlace(place, tree);
        }

        foreach (var life in tree.LivesByRealm.GetValueOrDefault(realm.EntityId, []).OrderBy(NameOf, StringComparer.Ordinal))
        {
            AddEntityRow(life, indentLevel: 1);
        }
    }

    private void RenderLivesInPlace(UiEntityProjection place, RegistryTree tree)
    {
        foreach (var life in tree.LivesByPlace.GetValueOrDefault(place.EntityId, []).OrderBy(NameOf, StringComparer.Ordinal))
        {
            AddEntityRow(life, indentLevel: 2);
        }
    }

    private void RenderOrphanSection(
        string label,
        List<UiEntityProjection> orphanPlaces,
        RegistryTree tree,
        bool includeLivesUnderPlaces)
    {
        if (orphanPlaces.Count == 0)
        {
            return;
        }

        AddSectionHeader(label, leadingGap: true);
        foreach (var place in orphanPlaces.OrderBy(NameOf, StringComparer.Ordinal))
        {
            AddEntityRow(place, indentLevel: 1);
            if (includeLivesUnderPlaces)
            {
                RenderLivesInPlace(place, tree);
            }
        }
    }

    private void RenderOrphanLives(List<UiEntityProjection> orphanLives)
    {
        if (orphanLives.Count == 0)
        {
            return;
        }

        AddSectionHeader(WildSectionLabel, leadingGap: true);
        foreach (var life in orphanLives.OrderBy(NameOf, StringComparer.Ordinal))
        {
            AddEntityRow(life, indentLevel: 1);
        }
    }

    private void RenderUntiered(List<UiEntityProjection> untiered)
    {
        if (untiered.Count == 0)
        {
            return;
        }

        AddSectionHeader(UntieredSectionLabel, leadingGap: true);
        foreach (var entity in untiered.OrderBy(NameOf, StringComparer.Ordinal))
        {
            AddEntityRow(entity, indentLevel: 1);
        }
    }

    private string NameOf(UiEntityProjection entity) =>
        ProjectionDisplay.EntityName(_symbols, entity);

    private void AddSectionHeader(string text, bool leadingGap)
    {
        var header = RentHeader();
        var wrapper = WrapWithIndent(header, indentLevel: 0);
        _listContainer!.AddChild(wrapper);
        header.BindTo(text, UiTokens.Parchment, count: 0, hasLeadingGap: leadingGap);
    }

    private void AddEntityRow(UiEntityProjection entity, int indentLevel)
    {
        var row = RentRow();
        var wrapper = WrapWithIndent(row, indentLevel);
        _listContainer!.AddChild(wrapper);
        row.BindTo(entity, _symbols);
    }

    private MarginContainer WrapWithIndent(Control content, int indentLevel)
    {
        var wrapper = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        wrapper.AddThemeConstantOverride("margin_left", indentLevel * IndentStepPx);
        wrapper.AddChild(content);
        _activeWrappers.Add(wrapper);
        return wrapper;
    }

    private void ReleaseActiveWrappers()
    {
        if (_listContainer is null)
        {
            return;
        }

        foreach (var wrapper in _activeWrappers)
        {
            if (wrapper.GetChildCount() > 0)
            {
                var content = wrapper.GetChild<Control>(0);
                wrapper.RemoveChild(content);
                ReturnControlToPool(content);
            }

            if (wrapper.GetParent() == _listContainer)
            {
                _listContainer.RemoveChild(wrapper);
            }

            wrapper.QueueFree();
        }

        _activeWrappers.Clear();
    }

    private void ReturnControlToPool(Control control)
    {
        switch (control)
        {
            case EntityRow row:
                _rowPool.Add(row);
                break;
            case EntityKindHeader header:
                _headerPool.Add(header);
                break;
            case Label label:
                _labelPool.Add(label);
                break;
        }
    }

    private EntityRow RentRow()
    {
        if (_rowPool.Count > 0)
        {
            var pooled = _rowPool[^1];
            _rowPool.RemoveAt(_rowPool.Count - 1);
            return pooled;
        }

        var row = _entityRowScene?.Instantiate<EntityRow>()
            ?? throw new InvalidOperationException("EntityRow scene is not loaded.");
        row.EntitySelected += OnRowEntitySelected;
        return row;
    }

    private EntityKindHeader RentHeader()
    {
        if (_headerPool.Count > 0)
        {
            var pooled = _headerPool[^1];
            _headerPool.RemoveAt(_headerPool.Count - 1);
            return pooled;
        }

        return _kindHeaderScene?.Instantiate<EntityKindHeader>()
            ?? throw new InvalidOperationException("EntityKindHeader scene is not loaded.");
    }

    private void OnRowEntitySelected(int entityIdValue) =>
        EmitSignal(SignalName.EntitySelected, entityIdValue);

    private sealed class RegistryTree
    {
        public List<UiEntityProjection> Realms { get; set; } = [];
        public Dictionary<int, List<UiEntityProjection>> PlacesByRealm { get; set; } = [];
        public Dictionary<int, List<UiEntityProjection>> LivesByPlace { get; set; } = [];
        public Dictionary<int, List<UiEntityProjection>> LivesByRealm { get; set; } = [];
        public List<UiEntityProjection> OrphanPlaces { get; set; } = [];
        public List<UiEntityProjection> OrphanLives { get; set; } = [];
        public List<UiEntityProjection> Untiered { get; set; } = [];
    }
}
