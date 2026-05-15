#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// Left sidebar listing all entities. Click an entity to open its floating card.
/// </summary>
public partial class EntityBrowser : PanelContainer
{
    private const float HeaderHeight = 24f;
    private const float HeaderLeadingGapHeight = 12f;
    private const float EntityRowHeight = 46f;
    private const int OverscanItemCount = 4;
    private const int AllGeneratorFilterId = 0;
    private const int SeedGeneratorFilterId = -1;

    private readonly List<RegistryItemData> _items = [];
    private readonly List<float> _itemOffsets = [];
    private readonly Dictionary<RegistryItemKey, Control> _visibleNodes = [];
    private readonly Stack<EntityRow> _rowPool = [];
    private readonly Stack<EntityKindHeader> _headerPool = [];
    private readonly List<RegistryItemKey> _releaseKeys = [];
    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private ScrollContainer? _scroll;
    private VBoxContainer? _listContainer;
    private VBoxContainer? _visibleContainer;
    private Control? _topSpacer;
    private Control? _bottomSpacer;
    private OptionButton? _generatorFilterOption;
    private CheckBox? _filterMapCheckBox;
    private Label? _filterSummaryLabel;
    private PackedScene? _entityRowScene;
    private PackedScene? _kindHeaderScene;
    private float _totalHeight;
    private bool _visibleRefreshQueued;
    private bool _updatingFilterControls;
    private int _selectedGeneratorFilterId = AllGeneratorFilterId;

    [Signal]
    public delegate void EntitySelectedEventHandler(int entityIdValue);

    [Signal]
    public delegate void EntityFilterChangedEventHandler();

    public long FilterVersion { get; private set; }

    public bool FilterMapToBrowser => _filterMapCheckBox?.ButtonPressed == true;

    public int SelectedGeneratorFilterId => _selectedGeneratorFilterId;

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
        AddVirtualizedList(vbox);
        Refresh();
    }

    private void AddHeader(VBoxContainer vbox)
    {
        vbox.AddChild(CreateHeaderLabel("Registry", UiTokens.SizeTitleMedium, UiTokens.Vellum));
        vbox.AddChild(CreateHeaderLabel("ACTIVE ENTITIES", UiTokens.SizeMicro, UiTokens.Whisper));
        AddFilterControls(vbox);
        vbox.AddChild(new ColorRect
        {
            Color = UiTokens.Hairline,
            CustomMinimumSize = new Vector2(0, 1),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
    }

    private static Label CreateHeaderLabel(string text, int size, Color color)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        label.AddThemeFontOverride("font", size == UiTokens.SizeTitleMedium ? UiTheme.DisplayFont : UiTheme.BodyFont);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private void AddFilterControls(VBoxContainer vbox)
    {
        var controls = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        controls.AddThemeConstantOverride("separation", UiTokens.Space1);
        controls.AddChild(CreateGeneratorFilterRow());
        controls.AddChild(CreateMapFilterRow());
        vbox.AddChild(controls);
    }

    private HBoxContainer CreateGeneratorFilterRow()
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", UiTokens.Space1);

        var label = new Label
        {
            Text = "Generator",
            CustomMinimumSize = new Vector2(72, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.AddThemeFontOverride("font", UiTheme.BodyFont);
        label.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        label.AddThemeColorOverride("font_color", UiTokens.Whisper);
        row.AddChild(label);

        _generatorFilterOption = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 28),
            TooltipText = "Filter entities by the generator that spawned them.",
        };
        _generatorFilterOption.ItemSelected += OnGeneratorFilterSelected;
        row.AddChild(_generatorFilterOption);
        return row;
    }

    private HBoxContainer CreateMapFilterRow()
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", UiTokens.Space1);

        _filterMapCheckBox = new CheckBox
        {
            Text = "Filter map",
            TooltipText = "Only draw browser-matched entities on the map.",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _filterMapCheckBox.Toggled += OnFilterMapToggled;
        row.AddChild(_filterMapCheckBox);

        _filterSummaryLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(64, 0),
        };
        _filterSummaryLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _filterSummaryLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _filterSummaryLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);
        row.AddChild(_filterSummaryLabel);
        return row;
    }

    private void AddVirtualizedList(VBoxContainer vbox)
    {
        _scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        vbox.AddChild(_scroll);

        _listContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _listContainer.AddThemeConstantOverride("separation", 0);
        _scroll.AddChild(_listContainer);

        _topSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _visibleContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _visibleContainer.AddThemeConstantOverride("separation", 2);
        _bottomSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _listContainer.AddChild(_topSpacer);
        _listContainer.AddChild(_visibleContainer);
        _listContainer.AddChild(_bottomSpacer);

        _scroll.GetVScrollBar().ValueChanged += OnScrollValueChanged;
        _scroll.Resized += QueueVisibleRefresh;
        Resized += QueueVisibleRefresh;
    }

    public void Refresh()
    {
        if (_projection is null || _listContainer is null || _visibleContainer is null)
        {
            ClearVirtualizedItems();
            return;
        }

        RebuildGeneratorFilterOptions();
        RebuildSnapshot();
        PresentVisibleItems();
        QueueVisibleRefresh();
    }

    public HashSet<int>? VisibleEntityIdsForMap()
    {
        if (!FilterMapToBrowser || _projection is null || _selectedGeneratorFilterId == AllGeneratorFilterId)
        {
            return null;
        }

        return FilteredActiveEntities().Select(static entity => entity.EntityId).ToHashSet();
    }

    private void RebuildSnapshot()
    {
        _items.Clear();
        _itemOffsets.Clear();
        _totalHeight = 0f;
        var firstKind = true;

        foreach (var group in FilteredActiveEntities()
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
            AddSnapshotItem(RegistryItemData.Header(
                new RegistryItemKey(RegistryItemType.KindHeader, group.Key, 0),
                displayName,
                accent,
                entities.Length,
                hasLeadingGap: !firstKind));
            firstKind = false;

            foreach (var entity in entities)
            {
                AddSnapshotItem(RegistryItemData.ForEntity(
                    new RegistryItemKey(RegistryItemType.EntityRow, entity.KindId, entity.EntityId),
                    entity));
            }
        }
    }

    private IEnumerable<UiEntityProjection> FilteredActiveEntities() =>
        _projection!.Entities.Values
            .Where(entity => entity.StatusCode == EntityStatusCodes.Active && EntityMatchesGeneratorFilter(entity));

    private bool EntityMatchesGeneratorFilter(UiEntityProjection entity) =>
        _selectedGeneratorFilterId switch
        {
            AllGeneratorFilterId => true,
            SeedGeneratorFilterId => entity.CreatedByGeneratorId is null,
            _ => entity.CreatedByGeneratorId == _selectedGeneratorFilterId,
        };

    private void RebuildGeneratorFilterOptions()
    {
        if (_generatorFilterOption is null || _projection is null)
        {
            return;
        }

        var activeEntities = _projection.Entities.Values
            .Where(static entity => entity.StatusCode == EntityStatusCodes.Active)
            .ToArray();
        var countsByGenerator = activeEntities
            .Where(static entity => entity.CreatedByGeneratorId is not null)
            .GroupBy(static entity => entity.CreatedByGeneratorId!.Value)
            .ToDictionary(static group => group.Key, static group => group.Count());
        var seedCount = activeEntities.Count(static entity => entity.CreatedByGeneratorId is null);
        var generatorOptions = countsByGenerator.Keys
            .Select(generatorId => new
            {
                GeneratorId = generatorId,
                Name = ProjectionDisplay.DisplayName(
                    _symbols,
                    ProjectionSymbolKind.Generator,
                    generatorId,
                    $"Generator {generatorId}"),
                Count = countsByGenerator[generatorId],
            })
            .OrderBy(option => option.Name, StringComparer.Ordinal)
            .ToArray();

        if (_selectedGeneratorFilterId > 0 && !countsByGenerator.ContainsKey(_selectedGeneratorFilterId))
        {
            _selectedGeneratorFilterId = AllGeneratorFilterId;
        }

        _updatingFilterControls = true;
        _generatorFilterOption.Clear();
        _generatorFilterOption.AddItem($"All generators ({activeEntities.Length})", AllGeneratorFilterId);
        _generatorFilterOption.AddItem($"Seeded / untraced ({seedCount})", SeedGeneratorFilterId);
        foreach (var option in generatorOptions)
        {
            _generatorFilterOption.AddItem($"{option.Name} ({option.Count})", option.GeneratorId);
        }

        var selectedIndex = FindOptionIndexById(_generatorFilterOption, _selectedGeneratorFilterId);
        if (selectedIndex < 0)
        {
            _selectedGeneratorFilterId = AllGeneratorFilterId;
            selectedIndex = FindOptionIndexById(_generatorFilterOption, AllGeneratorFilterId);
        }

        if (selectedIndex >= 0)
        {
            _generatorFilterOption.Select(selectedIndex);
        }

        _updatingFilterControls = false;
        UpdateFilterSummary();
    }

    private void UpdateFilterSummary()
    {
        if (_filterSummaryLabel is null || _projection is null)
        {
            return;
        }

        _filterSummaryLabel.Text = $"{FilteredActiveEntities().Count()} shown";
    }

    private static int FindOptionIndexById(OptionButton option, int id)
    {
        for (var i = 0; i < option.ItemCount; i++)
        {
            if (option.GetItemId(i) == id)
            {
                return i;
            }
        }

        return -1;
    }

    private void AddSnapshotItem(RegistryItemData data)
    {
        _itemOffsets.Add(_totalHeight);
        _items.Add(data);
        _totalHeight += data.Height;
    }

    private void PresentVisibleItems()
    {
        if (_scroll is null ||
            _visibleContainer is null ||
            _topSpacer is null ||
            _bottomSpacer is null)
        {
            return;
        }

        if (_items.Count == 0)
        {
            ReleaseAllVisibleNodes();
            SetSpacerHeights(0f, 0f);
            return;
        }

        var range = VisibleRange();
        ReleaseNodesOutside(range.First, range.Last);
        SetSpacerHeights(_itemOffsets[range.First], BottomSpacerHeight(range.Last));
        PresentRange(range.First, range.Last);
    }

    private (int First, int Last) VisibleRange()
    {
        var viewportHeight = Math.Max(1f, _scroll?.Size.Y ?? 1f);
        var scrollTop = Math.Max(0f, _scroll?.ScrollVertical ?? 0);
        var overscan = EntityRowHeight * OverscanItemCount;
        var first = FindFirstVisibleIndex(Math.Max(0f, scrollTop - overscan));
        var endOffset = scrollTop + viewportHeight + overscan;
        var last = first;
        while (last < _items.Count && _itemOffsets[last] < endOffset)
        {
            last++;
        }

        return (first, Math.Max(first + 1, last));
    }

    private int FindFirstVisibleIndex(float offset)
    {
        var low = 0;
        var high = _items.Count - 1;
        var result = _items.Count - 1;
        while (low <= high)
        {
            var mid = low + ((high - low) / 2);
            if (_itemOffsets[mid] + _items[mid].Height >= offset)
            {
                result = mid;
                high = mid - 1;
                continue;
            }

            low = mid + 1;
        }

        return result;
    }

    private void PresentRange(int first, int last)
    {
        for (var i = first; i < last; i++)
        {
            var data = _items[i];
            var node = GetOrCreateVisibleNode(data);
            BindNode(node, data);
            node.CustomMinimumSize = new Vector2(0, data.Height);
            if (node.GetParent() != _visibleContainer)
            {
                _visibleContainer!.AddChild(node);
            }

            if (_visibleContainer!.GetChild(i - first) != node)
            {
                _visibleContainer.MoveChild(node, i - first);
            }
        }
    }

    private Control GetOrCreateVisibleNode(RegistryItemData data)
    {
        if (_visibleNodes.TryGetValue(data.Key, out var existing))
        {
            return existing;
        }

        var node = data.ItemType == RegistryItemType.KindHeader
            ? (Control)RentHeader()
            : RentRow();
        node.Visible = true;
        _visibleNodes[data.Key] = node;
        return node;
    }

    private EntityKindHeader RentHeader() =>
        _headerPool.Count > 0
            ? _headerPool.Pop()
            : _kindHeaderScene?.Instantiate<EntityKindHeader>()
                ?? throw new InvalidOperationException("EntityKindHeader scene is not loaded.");

    private EntityRow RentRow()
    {
        if (_rowPool.Count > 0)
        {
            return _rowPool.Pop();
        }

        var row = _entityRowScene?.Instantiate<EntityRow>()
            ?? throw new InvalidOperationException("EntityRow scene is not loaded.");
        row.EntitySelected += OnRowEntitySelected;
        return row;
    }

    private void BindNode(Control node, RegistryItemData data)
    {
        if (data.ItemType == RegistryItemType.KindHeader && node is EntityKindHeader header)
        {
            header.BindTo(data.DisplayName, data.Accent, data.Count, data.HasLeadingGap);
        }
        else if (data.Entity is not null && node is EntityRow row)
        {
            row.BindTo(data.Entity, _symbols);
        }
    }

    private void ReleaseNodesOutside(int first, int last)
    {
        _releaseKeys.Clear();
        foreach (var key in _visibleNodes.Keys)
        {
            if (!IsInVisibleRange(key, first, last))
            {
                _releaseKeys.Add(key);
            }
        }

        foreach (var key in _releaseKeys)
        {
            ReleaseVisibleNode(key);
        }

        _releaseKeys.Clear();
    }

    private bool IsInVisibleRange(RegistryItemKey key, int first, int last)
    {
        for (var i = first; i < last; i++)
        {
            if (_items[i].Key == key)
            {
                return true;
            }
        }

        return false;
    }

    private void ReleaseAllVisibleNodes()
    {
        foreach (var key in _visibleNodes.Keys.ToArray())
        {
            ReleaseVisibleNode(key);
        }
    }

    private void ReleaseVisibleNode(RegistryItemKey key)
    {
        if (!_visibleNodes.TryGetValue(key, out var node))
        {
            return;
        }

        _visibleNodes.Remove(key);
        if (node.GetParent() == _visibleContainer)
        {
            _visibleContainer?.RemoveChild(node);
        }

        node.Visible = false;
        if (node is EntityRow row)
        {
            _rowPool.Push(row);
        }
        else if (node is EntityKindHeader header)
        {
            _headerPool.Push(header);
        }
    }

    private void ClearVirtualizedItems()
    {
        ReleaseAllVisibleNodes();
        FreePool(_rowPool);
        FreePool(_headerPool);
        _items.Clear();
        _itemOffsets.Clear();
        _totalHeight = 0f;
        SetSpacerHeights(0f, 0f);
    }

    private static void FreePool<TNode>(Stack<TNode> pool)
        where TNode : Node
    {
        while (pool.Count > 0)
        {
            pool.Pop().QueueFree();
        }
    }

    private float BottomSpacerHeight(int last)
    {
        var end = _itemOffsets[last - 1] + _items[last - 1].Height;
        return Math.Max(0f, _totalHeight - end);
    }

    private void SetSpacerHeights(float top, float bottom)
    {
        if (_topSpacer is not null)
        {
            _topSpacer.CustomMinimumSize = new Vector2(0, top);
        }

        if (_bottomSpacer is not null)
        {
            _bottomSpacer.CustomMinimumSize = new Vector2(0, bottom);
        }
    }

    private void QueueVisibleRefresh()
    {
        if (_visibleRefreshQueued)
        {
            return;
        }

        _visibleRefreshQueued = true;
        CallDeferred(nameof(PresentVisibleItemsDeferred));
    }

    private void PresentVisibleItemsDeferred()
    {
        _visibleRefreshQueued = false;
        PresentVisibleItems();
    }

    private void OnScrollValueChanged(double value)
    {
        _ = value;
        PresentVisibleItems();
    }

    private void OnRowEntitySelected(int entityIdValue) =>
        EmitSignal(SignalName.EntitySelected, entityIdValue);

    private void OnGeneratorFilterSelected(long selectedIndex)
    {
        if (_updatingFilterControls || _generatorFilterOption is null)
        {
            return;
        }

        var filterId = _generatorFilterOption.GetItemId((int)selectedIndex);
        if (_selectedGeneratorFilterId == filterId)
        {
            return;
        }

        _selectedGeneratorFilterId = filterId;
        ApplyFilterChange(resetScroll: true);
    }

    private void OnFilterMapToggled(bool pressed)
    {
        _ = pressed;
        if (_updatingFilterControls)
        {
            return;
        }

        ApplyFilterChange(resetScroll: false);
    }

    private void ApplyFilterChange(bool resetScroll)
    {
        FilterVersion++;
        if (resetScroll && _scroll is not null)
        {
            _scroll.ScrollVertical = 0;
        }

        Refresh();
        EmitSignal(SignalName.EntityFilterChanged);
    }

    private readonly record struct RegistryItemKey(RegistryItemType ItemType, int KindId, int EntityId);

    private enum RegistryItemType
    {
        KindHeader,
        EntityRow,
    }

    private sealed record RegistryItemData(
        RegistryItemKey Key,
        RegistryItemType ItemType,
        UiEntityProjection? Entity,
        string DisplayName,
        Color Accent,
        int Count,
        bool HasLeadingGap,
        float Height)
    {
        public static RegistryItemData Header(
            RegistryItemKey key,
            string displayName,
            Color accent,
            int count,
            bool hasLeadingGap) =>
            new(
                key,
                RegistryItemType.KindHeader,
                null,
                displayName,
                accent,
                count,
                hasLeadingGap,
                HeaderHeight + (hasLeadingGap ? HeaderLeadingGapHeight : 0f));

        public static RegistryItemData ForEntity(RegistryItemKey key, UiEntityProjection entity) =>
            new(key, RegistryItemType.EntityRow, entity, string.Empty, default, 0, false, EntityRowHeight);
    }
}
