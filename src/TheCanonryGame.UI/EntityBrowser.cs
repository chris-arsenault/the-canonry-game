#pragma warning disable CA1303
#pragma warning disable CA1305
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Kinds;
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// Left sidebar listing entities in the flat registry model. The default view
/// matches the pre-tree browser: active entities grouped by kind, virtualized
/// for long runs, with controls for narrowing and sorting the list.
/// </summary>
public partial class EntityBrowser : PanelContainer
{
    private const float HeaderHeight = 24f;
    private const float HeaderLeadingGapHeight = 12f;
    private const float EntityRowHeight = 46f;
    private const int OverscanItemCount = 4;
    private const int AllFilterId = 0;
    private const int AllGeneratorFilterId = 0;
    private const int SeedGeneratorFilterId = -1;
    private const int AnyWorkTierFilterId = -1;
    private const int StatusActiveFilterId = 1;
    private const int StatusHistoricalFilterId = 2;
    private const int SlotOccupiedFilterId = 1;
    private const int SlotEmptyFilterId = 2;
    private const int SlotPositiveFilterId = 3;
    private const int SlotNeutralFilterId = 4;
    private const int SlotNegativeFilterId = 5;

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
    private LineEdit? _searchEdit;
    private OptionButton? _kindFilterOption;
    private OptionButton? _cultureFilterOption;
    private OptionButton? _statusFilterOption;
    private OptionButton? _slotFilterOption;
    private OptionButton? _workTierFilterOption;
    private OptionButton? _sortOption;
    private OptionButton? _generatorFilterOption;
    private CheckBox? _filterMapCheckBox;
    private Label? _filterSummaryLabel;
    private PackedScene? _entityRowScene;
    private PackedScene? _kindHeaderScene;
    private float _totalHeight;
    private bool _visibleRefreshQueued;
    private bool _updatingFilterControls;
    private int _selectedKindFilterId = AllFilterId;
    private int _selectedCultureFilterId = AllFilterId;
    private int _selectedStatusFilterId = StatusActiveFilterId;
    private int _selectedSlotFilterId = AllFilterId;
    private int _selectedWorkTierFilterId = AnyWorkTierFilterId;
    private int _selectedGeneratorFilterId = AllGeneratorFilterId;
    private RegistrySortMode _selectedSortMode = RegistrySortMode.Kind;
    private string _searchText = string.Empty;

    private static readonly Dictionary<RegistrySortMode, Func<EntityBrowser, IEnumerable<UiEntityProjection>, IOrderedEnumerable<UiEntityProjection>>> Sorters =
        new Dictionary<RegistrySortMode, Func<EntityBrowser, IEnumerable<UiEntityProjection>, IOrderedEnumerable<UiEntityProjection>>>
        {
            [RegistrySortMode.Name] = static (browser, entities) => browser.SortByName(entities),
            [RegistrySortMode.Culture] = static (browser, entities) => browser.SortByCulture(entities),
            [RegistrySortMode.HealthLow] = static (browser, entities) => browser.SortByHealthLow(entities),
            [RegistrySortMode.HealthHigh] = static (browser, entities) => browser.SortByHealthHigh(entities),
            [RegistrySortMode.AgeOldest] = static (browser, entities) => browser.SortByAgeOldest(entities),
            [RegistrySortMode.AgeYoungest] = static (browser, entities) => browser.SortByAgeYoungest(entities),
            [RegistrySortMode.WorkTier] = static (browser, entities) => browser.SortByWorkTier(entities),
            [RegistrySortMode.RecentActivity] = static (browser, entities) => browser.SortByRecentActivity(entities),
        };

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
        Refresh(rebuildFilterOptions: true);
    }

    public override void _Ready()
    {
        _entityRowScene = GD.Load<PackedScene>("res://scenes/ui/EntityRow.tscn");
        _kindHeaderScene = GD.Load<PackedScene>("res://scenes/ui/EntityKindHeader.tscn");
        CustomMinimumSize = new Vector2(280, 0);
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeStyleboxOverride("panel", UiTheme.RailPanelStyle(onLeft: true));

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", UiTokens.Space2);
        AddChild(vbox);

        AddHeader(vbox);
        AddVirtualizedList(vbox);
        Refresh(rebuildFilterOptions: true);
    }

    public void Refresh(bool rebuildFilterOptions = true)
    {
        if (_projection is null || _listContainer is null || _visibleContainer is null)
        {
            ClearVirtualizedItems();
            return;
        }

        if (rebuildFilterOptions)
        {
            RebuildFilterOptions();
        }

        RebuildSnapshot();
        UpdateFilterSummary();
        PresentVisibleItems();
        QueueVisibleRefresh();
    }

    public void RefreshDirtyRows(IReadOnlyCollection<int> entityIds)
    {
        ArgumentNullException.ThrowIfNull(entityIds);
        if (_projection is null || entityIds.Count == 0)
        {
            return;
        }

        if (_selectedSortMode == RegistrySortMode.RecentActivity)
        {
            Refresh(rebuildFilterOptions: false);
            return;
        }

        var dirtyEntityIds = entityIds as IReadOnlySet<int> ?? entityIds.ToHashSet();
        foreach (var pair in _visibleNodes)
        {
            if (pair.Key.ItemType != RegistryItemType.EntityRow ||
                !dirtyEntityIds.Contains(pair.Key.EntityId) ||
                pair.Value is not EntityRow row ||
                !_projection.Entities.TryGetValue(pair.Key.EntityId, out var entity))
            {
                continue;
            }

            row.BindTo(entity, _symbols);
        }
    }

    public HashSet<int>? VisibleEntityIdsForMap()
    {
        if (!FilterMapToBrowser || _projection is null)
        {
            return null;
        }

        return FilteredEntities()
            .Where(static entity => entity.StatusCode == EntityStatusCodes.Active)
            .Select(static entity => entity.EntityId)
            .ToHashSet();
    }

    private void AddHeader(VBoxContainer vbox)
    {
        vbox.AddChild(CreateHeaderLabel("Registry", UiTokens.SizeTitleMedium, UiTokens.Vellum));
        vbox.AddChild(CreateHeaderLabel("FLAT ENTITY LIST", UiTokens.SizeMicro, UiTokens.Whisper));
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
        AddSearchControl(controls);
        AddFilterOptionRows(controls);
        AddMapFilterRow(controls);
        WireFilterEvents();
        vbox.AddChild(controls);
    }

    private void AddSearchControl(VBoxContainer controls)
    {
        _searchEdit = new LineEdit
        {
            PlaceholderText = "Search",
            CustomMinimumSize = new Vector2(0, 26),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = "Filter by entity, kind, subtype, culture, or id.",
        };
        _searchEdit.TextChanged += OnSearchTextChanged;
        controls.AddChild(_searchEdit);
    }

    private void AddFilterOptionRows(VBoxContainer controls)
    {
        _kindFilterOption = CreateOptionButton("Filter by kind.");
        _cultureFilterOption = CreateOptionButton("Filter by culture.");
        controls.AddChild(CreateOptionPairRow(_kindFilterOption, _cultureFilterOption));

        _statusFilterOption = CreateOptionButton("Filter active or historical entities.");
        _slotFilterOption = CreateOptionButton("Filter culture slot occupancy.");
        controls.AddChild(CreateOptionPairRow(_statusFilterOption, _slotFilterOption));

        _workTierFilterOption = CreateOptionButton("Filter by worker tier.");
        _sortOption = CreateOptionButton("Sort the registry.");
        controls.AddChild(CreateOptionPairRow(_workTierFilterOption, _sortOption));

        _generatorFilterOption = CreateOptionButton("Filter entities by the generator that spawned them.");
        controls.AddChild(_generatorFilterOption);
    }

    private void AddMapFilterRow(VBoxContainer controls)
    {
        _filterMapCheckBox = new CheckBox
        {
            Text = "Filter map",
            TooltipText = "Only draw browser-matched active entities on the map.",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _filterMapCheckBox.Toggled += OnFilterMapToggled;

        _filterSummaryLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(72, 0),
        };
        _filterSummaryLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _filterSummaryLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _filterSummaryLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);
        var mapRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        mapRow.AddThemeConstantOverride("separation", UiTokens.Space1);
        mapRow.AddChild(_filterMapCheckBox);
        mapRow.AddChild(_filterSummaryLabel);
        controls.AddChild(mapRow);
    }

    private void WireFilterEvents()
    {
        _kindFilterOption!.ItemSelected += OnKindFilterSelected;
        _cultureFilterOption!.ItemSelected += OnCultureFilterSelected;
        _statusFilterOption!.ItemSelected += OnStatusFilterSelected;
        _slotFilterOption!.ItemSelected += OnSlotFilterSelected;
        _workTierFilterOption!.ItemSelected += OnWorkTierFilterSelected;
        _sortOption!.ItemSelected += OnSortSelected;
        _generatorFilterOption!.ItemSelected += OnGeneratorFilterSelected;
    }

    private static OptionButton CreateOptionButton(string tooltip) => new()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
        CustomMinimumSize = new Vector2(0, 26),
        TooltipText = tooltip,
    };

    private static HBoxContainer CreateOptionPairRow(Control left, Control right)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", UiTokens.Space1);
        row.AddChild(left);
        row.AddChild(right);
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

    private void RebuildSnapshot()
    {
        _items.Clear();
        _itemOffsets.Clear();
        _totalHeight = 0f;

        var entities = SortedEntities(FilteredEntities()).ToArray();
        if (_selectedSortMode == RegistrySortMode.Kind)
        {
            AddKindGroupedSnapshot(entities);
            return;
        }

        foreach (var entity in entities)
        {
            AddSnapshotItem(RegistryItemData.ForEntity(
                new RegistryItemKey(RegistryItemType.EntityRow, entity.KindId, entity.EntityId),
                entity));
        }
    }

    private void AddKindGroupedSnapshot(IReadOnlyList<UiEntityProjection> entities)
    {
        var firstKind = true;
        foreach (var group in entities.GroupBy(static entity => entity.KindId))
        {
            var kindStableName = ProjectionDisplay.StableName(_symbols, ProjectionSymbolKind.Kind, group.Key, $"kind-{group.Key}");
            var displayName = ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Kind, group.Key, kindStableName);
            var groupEntities = group.ToArray();
            AddSnapshotItem(RegistryItemData.Header(
                new RegistryItemKey(RegistryItemType.KindHeader, group.Key, 0),
                displayName,
                UiTokens.KindAccent(kindStableName),
                groupEntities.Length,
                hasLeadingGap: !firstKind));
            firstKind = false;

            foreach (var entity in groupEntities)
            {
                AddSnapshotItem(RegistryItemData.ForEntity(
                    new RegistryItemKey(RegistryItemType.EntityRow, entity.KindId, entity.EntityId),
                    entity));
            }
        }
    }

    private IEnumerable<UiEntityProjection> FilteredEntities() =>
        _projection!.Entities.Values.Where(EntityMatchesFilters);

    private bool EntityMatchesFilters(UiEntityProjection entity) =>
        EntityMatchesStatusFilter(entity) &&
        (_selectedKindFilterId == AllFilterId || entity.KindId == _selectedKindFilterId) &&
        (_selectedCultureFilterId == AllFilterId || entity.CultureId == _selectedCultureFilterId) &&
        EntityMatchesSlotFilter(entity) &&
        (_selectedWorkTierFilterId == AnyWorkTierFilterId || entity.WorkTier == _selectedWorkTierFilterId) &&
        EntityMatchesGeneratorFilter(entity) &&
        EntityMatchesSearch(entity);

    private bool EntityMatchesStatusFilter(UiEntityProjection entity) => _selectedStatusFilterId switch
    {
        AllFilterId => true,
        StatusActiveFilterId => entity.StatusCode == EntityStatusCodes.Active,
        StatusHistoricalFilterId => entity.StatusCode != EntityStatusCodes.Active,
        _ => true,
    };

    private bool EntityMatchesSlotFilter(UiEntityProjection entity) => _selectedSlotFilterId switch
    {
        AllFilterId => true,
        SlotOccupiedFilterId => entity.CultureSlotCount > 0,
        SlotEmptyFilterId => entity.CultureSlotCount == 0,
        SlotPositiveFilterId => entity.CultureSlotPolarityCode == RelationshipPolarityCodes.Positive,
        SlotNeutralFilterId => entity.CultureSlotPolarityCode == RelationshipPolarityCodes.Neutral,
        SlotNegativeFilterId => entity.CultureSlotPolarityCode == RelationshipPolarityCodes.Negative,
        _ => true,
    };

    private bool EntityMatchesGeneratorFilter(UiEntityProjection entity) =>
        _selectedGeneratorFilterId switch
        {
            AllGeneratorFilterId => true,
            SeedGeneratorFilterId => entity.CreatedByGeneratorId is null,
            _ => entity.CreatedByGeneratorId == _selectedGeneratorFilterId,
        };

    private bool EntityMatchesSearch(UiEntityProjection entity)
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            return true;
        }

        var query = _searchText.Trim();
        return ProjectionDisplay.EntityName(_symbols, entity).Contains(query, StringComparison.OrdinalIgnoreCase) ||
            ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Kind, entity.KindId, $"Kind {entity.KindId}")
                .Contains(query, StringComparison.OrdinalIgnoreCase) ||
            ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Subtype, entity.SubtypeId, $"Subtype {entity.SubtypeId}")
                .Contains(query, StringComparison.OrdinalIgnoreCase) ||
            ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Culture, entity.CultureId, $"Culture {entity.CultureId}")
                .Contains(query, StringComparison.OrdinalIgnoreCase) ||
            entity.EntityId.ToString(System.Globalization.CultureInfo.InvariantCulture)
                .Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private IOrderedEnumerable<UiEntityProjection> SortedEntities(IEnumerable<UiEntityProjection> entities) =>
        Sorters.TryGetValue(_selectedSortMode, out var sorter)
            ? sorter(this, entities)
            : SortByKind(entities);

    private IOrderedEnumerable<UiEntityProjection> SortByKind(IEnumerable<UiEntityProjection> entities) =>
        entities
            .OrderBy(KindName, StringComparer.Ordinal)
            .ThenBy(EntityName, StringComparer.Ordinal)
            .ThenBy(static entity => entity.EntityId);

    private IOrderedEnumerable<UiEntityProjection> SortByName(IEnumerable<UiEntityProjection> entities) =>
        entities
            .OrderBy(EntityName, StringComparer.Ordinal)
            .ThenBy(static entity => entity.EntityId);

    private IOrderedEnumerable<UiEntityProjection> SortByCulture(IEnumerable<UiEntityProjection> entities) =>
        entities
            .OrderBy(CultureName, StringComparer.Ordinal)
            .ThenBy(EntityName, StringComparer.Ordinal)
            .ThenBy(static entity => entity.EntityId);

    private IOrderedEnumerable<UiEntityProjection> SortByHealthLow(IEnumerable<UiEntityProjection> entities) =>
        entities
            .OrderBy(HealthSortLow)
            .ThenBy(EntityName, StringComparer.Ordinal)
            .ThenBy(static entity => entity.EntityId);

    private IOrderedEnumerable<UiEntityProjection> SortByHealthHigh(IEnumerable<UiEntityProjection> entities) =>
        entities
            .OrderByDescending(HealthSortHigh)
            .ThenBy(EntityName, StringComparer.Ordinal)
            .ThenBy(static entity => entity.EntityId);

    private IOrderedEnumerable<UiEntityProjection> SortByAgeOldest(IEnumerable<UiEntityProjection> entities) =>
        entities
            .OrderBy(static entity => entity.CreatedTick ?? int.MaxValue)
            .ThenBy(EntityName, StringComparer.Ordinal)
            .ThenBy(static entity => entity.EntityId);

    private IOrderedEnumerable<UiEntityProjection> SortByAgeYoungest(IEnumerable<UiEntityProjection> entities) =>
        entities
            .OrderByDescending(static entity => entity.CreatedTick ?? int.MinValue)
            .ThenBy(EntityName, StringComparer.Ordinal)
            .ThenBy(static entity => entity.EntityId);

    private IOrderedEnumerable<UiEntityProjection> SortByWorkTier(IEnumerable<UiEntityProjection> entities) =>
        entities
            .OrderByDescending(static entity => entity.WorkTier)
            .ThenBy(EntityName, StringComparer.Ordinal)
            .ThenBy(static entity => entity.EntityId);

    private IOrderedEnumerable<UiEntityProjection> SortByRecentActivity(IEnumerable<UiEntityProjection> entities) =>
        entities
            .OrderByDescending(RecentActivityTick)
            .ThenBy(EntityName, StringComparer.Ordinal)
            .ThenBy(static entity => entity.EntityId);

    private string EntityName(UiEntityProjection entity) =>
        ProjectionDisplay.EntityName(_symbols, entity);

    private string KindName(UiEntityProjection entity) =>
        ProjectionDisplay.StableName(_symbols, ProjectionSymbolKind.Kind, entity.KindId, $"kind-{entity.KindId}");

    private string CultureName(UiEntityProjection entity) =>
        ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Culture, entity.CultureId, $"Culture {entity.CultureId}");

    private float HealthSortLow(UiEntityProjection entity) =>
        _projection?.HealthRatioOf(entity) ?? float.PositiveInfinity;

    private float HealthSortHigh(UiEntityProjection entity) =>
        _projection?.HealthRatioOf(entity) ?? float.NegativeInfinity;

    private static int RecentActivityTick(UiEntityProjection entity)
    {
        var last = entity.LastUpdatedTick;
        if (entity.LastMove is { } move)
        {
            last = Math.Max(last, move.Tick);
        }

        if (entity.LastGambitExecution is { } gambit)
        {
            last = Math.Max(last, gambit.Tick);
        }

        if (entity.CreatedByGeneratorTick is { } generatorTick)
        {
            last = Math.Max(last, generatorTick);
        }

        return last;
    }

    private void AddSnapshotItem(RegistryItemData data)
    {
        _itemOffsets.Add(_totalHeight);
        _items.Add(data);
        _totalHeight += data.Height;
    }

    private void UpdateFilterSummary()
    {
        if (_filterSummaryLabel is null || _projection is null)
        {
            return;
        }

        _filterSummaryLabel.Text = $"{FilteredEntities().Count()} shown";
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

    private void OnSearchTextChanged(string text)
    {
        if (_updatingFilterControls)
        {
            return;
        }

        _searchText = text;
        ApplyFilterChange(resetScroll: true);
    }

    private void OnKindFilterSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_kindFilterOption, selectedIndex, _selectedKindFilterId, out var selected))
        {
            _selectedKindFilterId = selected;
            ApplyFilterChange(resetScroll: true);
        }
    }

    private void OnCultureFilterSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_cultureFilterOption, selectedIndex, _selectedCultureFilterId, out var selected))
        {
            _selectedCultureFilterId = selected;
            ApplyFilterChange(resetScroll: true);
        }
    }

    private void OnStatusFilterSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_statusFilterOption, selectedIndex, _selectedStatusFilterId, out var selected))
        {
            _selectedStatusFilterId = selected;
            ApplyFilterChange(resetScroll: true);
        }
    }

    private void OnSlotFilterSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_slotFilterOption, selectedIndex, _selectedSlotFilterId, out var selected))
        {
            _selectedSlotFilterId = selected;
            ApplyFilterChange(resetScroll: true);
        }
    }

    private void OnWorkTierFilterSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_workTierFilterOption, selectedIndex, _selectedWorkTierFilterId, out var selected))
        {
            _selectedWorkTierFilterId = selected;
            ApplyFilterChange(resetScroll: true);
        }
    }

    private void OnSortSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_sortOption, selectedIndex, (int)_selectedSortMode, out var selected))
        {
            _selectedSortMode = (RegistrySortMode)selected;
            ApplyFilterChange(resetScroll: true);
        }
    }

    private void OnGeneratorFilterSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_generatorFilterOption, selectedIndex, _selectedGeneratorFilterId, out var selected))
        {
            _selectedGeneratorFilterId = selected;
            ApplyFilterChange(resetScroll: true);
        }
    }

    private bool TryReadSelectedId(OptionButton? option, long selectedIndex, int current, out int selected)
    {
        selected = current;
        if (_updatingFilterControls || option is null)
        {
            return false;
        }

        var next = option.GetItemId((int)selectedIndex);
        if (next == current)
        {
            return false;
        }

        selected = next;
        return true;
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

        Refresh(rebuildFilterOptions: false);
        EmitSignal(SignalName.EntityFilterChanged);
    }

    private readonly record struct OptionData(int Id, string Text);

    private readonly record struct RegistryItemKey(RegistryItemType ItemType, int KindId, int EntityId);

    private enum RegistryItemType
    {
        KindHeader,
        EntityRow,
    }

    private enum RegistrySortMode
    {
        Kind = 1,
        Name = 2,
        Culture = 3,
        HealthLow = 4,
        HealthHigh = 5,
        AgeOldest = 6,
        AgeYoungest = 7,
        WorkTier = 8,
        RecentActivity = 9,
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
