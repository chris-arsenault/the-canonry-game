#pragma warning disable CA1303
#pragma warning disable CA1305
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Eventing;
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class ChroniclePanel : PanelContainer
{
    [Signal]
    public delegate void EntitySelectedEventHandler(int entityIdValue);

    [Signal]
    public delegate void JournalRequestedEventHandler();

    private const int AllFilterId = 0;
    private const int EventGambitFilterId = 1;
    private const int EventGeneratorFilterId = 2;
    private const int EventHistoryChainFilterId = 3;
    private const int EventTaleFilterId = 4;
    private const int StateSucceededFilterId = 1;
    private const int StateFailedFilterId = 2;
    private const int TickWindowAllFilterId = 0;
    private const int TickWindow10FilterId = 10;
    private const int TickWindow20FilterId = 20;
    private const int TickWindow50FilterId = 50;
    private const int MaxVisibleEntries = 160;
    private const string HeaderColor = "B8B5A6";

    private readonly List<OptionData> _scratchOptions = [];
    private IProjectionSymbolCatalog? _symbols;
    private IReadOnlyList<UiChronicleEntry> _entries = [];
    private Button? _journalButton;
    private RichTextLabel? _eventLogLabel;
    private LineEdit? _entitySearchEdit;
    private OptionButton? _cultureFilterOption;
    private OptionButton? _kindFilterOption;
    private OptionButton? _producerFilterOption;
    private OptionButton? _eventFilterOption;
    private OptionButton? _stateFilterOption;
    private OptionButton? _tickFilterOption;
    private OptionButton? _sortOption;
    private Label? _filterSummaryLabel;
    private bool _updatingFilterControls;
    private string _entitySearchText = string.Empty;
    private int _selectedCultureFilterId = AllFilterId;
    private int _selectedKindFilterId = AllFilterId;
    private int _selectedProducerFilterId = AllFilterId;
    private int _selectedEventFilterId = AllFilterId;
    private int _selectedStateFilterId = StateSucceededFilterId;
    private int _selectedTickWindowFilterId = TickWindowAllFilterId;
    private ChronicleSortMode _selectedSortMode = ChronicleSortMode.Newest;

    public long FilterVersion { get; private set; }

    public void Initialize(IProjectionSymbolCatalog? symbols)
    {
        _symbols = symbols;
        RenderEntries();
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(360, 0);
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeStyleboxOverride("panel", UiTheme.RailPanelStyle(onLeft: false));

        var outer = GetNode<VBoxContainer>("Outer");
        outer.AddThemeConstantOverride("separation", UiTokens.Space2);

        var title = GetNode<Label>("Outer/TitleLabel");
        title.AddThemeFontOverride("font", UiTheme.DisplayFont);
        title.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        title.AddThemeColorOverride("font_color", UiTokens.Vellum);

        var subtitle = GetNode<Label>("Outer/SubtitleLabel");
        subtitle.AddThemeFontOverride("font", UiTheme.BodyFont);
        subtitle.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        subtitle.AddThemeColorOverride("font_color", UiTokens.Whisper);

        AddFilterControls(outer);
        GetNode<ColorRect>("Outer/Hairline").Color = UiTokens.Hairline;
        AddJournalButton(outer);

        _eventLogLabel = GetNode<RichTextLabel>("Outer/Scroll/EventLogLabel");
        _eventLogLabel.AddThemeFontOverride("normal_font", UiTheme.BodyFont);
        _eventLogLabel.AddThemeFontOverride("mono_font", UiTheme.MonoFont);
        _eventLogLabel.AddThemeFontSizeOverride("normal_font_size", UiTokens.SizeBodySmall);
        _eventLogLabel.AddThemeFontSizeOverride("mono_font_size", UiTokens.SizeBodySmall);
        _eventLogLabel.AddThemeColorOverride("default_color", UiTokens.Vellum);
        RenderEntries();
    }

    public void SetTaleViews(IReadOnlyList<UiChronicleTaleView> taleViews)
    {
        ArgumentNullException.ThrowIfNull(taleViews);
        var unfolding = taleViews.Count(static view => !view.Tale.Resolved);
        if (_journalButton is not null)
        {
            _journalButton.Text = unfolding > 0
                ? $"Chronicle Journal  ({unfolding} unfolding)"
                : "Chronicle Journal";
        }
    }

    private void AddJournalButton(VBoxContainer outer)
    {
        _journalButton = new Button
        {
            Text = "Chronicle Journal",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 28),
            TooltipText = "Open the chronicle journal: the world's unfolding tales and endings.",
        };
        _journalButton.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _journalButton.Pressed += () => EmitSignal(SignalName.JournalRequested);
        outer.AddChild(_journalButton);
        outer.MoveChild(_journalButton, 3);
    }

    public void SetEntries(IReadOnlyList<UiChronicleEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _entries = entries;
        RebuildFilterOptions();
        RenderEntries();
    }

    public void SetLines(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (_eventLogLabel is not null)
        {
            _eventLogLabel.Text = string.Join('\n', lines);
        }
    }

    public void Clear()
    {
        _entries = [];
        if (_eventLogLabel is not null)
        {
            _eventLogLabel.Text = string.Empty;
        }
    }

    private void AddFilterControls(VBoxContainer outer)
    {
        var controls = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        controls.AddThemeConstantOverride("separation", UiTokens.Space1);

        _entitySearchEdit = new LineEdit
        {
            PlaceholderText = "Entity",
            CustomMinimumSize = new Vector2(0, 26),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = "Filter by involved entity name or id.",
        };
        _entitySearchEdit.TextChanged += OnEntitySearchTextChanged;
        controls.AddChild(_entitySearchEdit);

        _cultureFilterOption = CreateOptionButton("Filter by involved entity culture.");
        _kindFilterOption = CreateOptionButton("Filter by involved entity kind.");
        controls.AddChild(CreateOptionPairRow(_cultureFilterOption, _kindFilterOption));

        _producerFilterOption = CreateOptionButton("Filter by producer type.");
        _eventFilterOption = CreateOptionButton("Filter by chronicle event type.");
        controls.AddChild(CreateOptionPairRow(_producerFilterOption, _eventFilterOption));

        _stateFilterOption = CreateOptionButton("Filter by success state.");
        _tickFilterOption = CreateOptionButton("Filter by tick window.");
        controls.AddChild(CreateOptionPairRow(_stateFilterOption, _tickFilterOption));

        _sortOption = CreateOptionButton("Sort or group chronicle entries.");
        _filterSummaryLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(72, 0),
        };
        _filterSummaryLabel.AddThemeFontOverride("font", UiTheme.BodyFont);
        _filterSummaryLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _filterSummaryLabel.AddThemeColorOverride("font_color", UiTokens.Whisper);
        controls.AddChild(CreateOptionPairRow(_sortOption, _filterSummaryLabel));

        _cultureFilterOption.ItemSelected += OnCultureFilterSelected;
        _kindFilterOption.ItemSelected += OnKindFilterSelected;
        _producerFilterOption.ItemSelected += OnProducerFilterSelected;
        _eventFilterOption.ItemSelected += OnEventFilterSelected;
        _stateFilterOption.ItemSelected += OnStateFilterSelected;
        _tickFilterOption.ItemSelected += OnTickFilterSelected;
        _sortOption.ItemSelected += OnSortSelected;

        outer.AddChild(controls);
        outer.MoveChild(controls, 2);
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

    private void RebuildFilterOptions()
    {
        _updatingFilterControls = true;
        RebuildCultureFilterOptions();
        RebuildKindFilterOptions();
        RebuildProducerFilterOptions();
        RebuildEventFilterOptions();
        RebuildStateFilterOptions();
        RebuildTickFilterOptions();
        RebuildSortOptions();
        if (_entitySearchEdit is not null &&
            !string.Equals(_entitySearchEdit.Text, _entitySearchText, StringComparison.Ordinal))
        {
            _entitySearchEdit.Text = _entitySearchText;
        }

        _updatingFilterControls = false;
    }

    private void RebuildCultureFilterOptions()
    {
        if (_cultureFilterOption is null)
        {
            return;
        }

        _scratchOptions.Clear();
        var cultureIds = _entries
            .SelectMany(static entry => entry.Entities)
            .Select(static entity => entity.CultureId)
            .Where(static id => id > 0)
            .Distinct()
            .Order();
        foreach (var cultureId in cultureIds)
        {
            _scratchOptions.Add(new OptionData(
                cultureId,
                $"{ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Culture, cultureId, $"Culture {cultureId}")} ({CountEntriesForCulture(cultureId)})"));
        }

        _scratchOptions.Sort(static (left, right) => string.Compare(left.Text, right.Text, StringComparison.Ordinal));
        _scratchOptions.Insert(0, new OptionData(AllFilterId, $"All cultures ({_entries.Count})"));
        _selectedCultureFilterId = RebuildOption(
            _cultureFilterOption,
            _scratchOptions,
            _selectedCultureFilterId,
            AllFilterId);
    }

    private void RebuildKindFilterOptions()
    {
        if (_kindFilterOption is null)
        {
            return;
        }

        _scratchOptions.Clear();
        var kindIds = _entries
            .SelectMany(static entry => entry.Entities)
            .Select(static entity => entity.KindId)
            .Where(static id => id > 0)
            .Distinct()
            .Order();
        foreach (var kindId in kindIds)
        {
            _scratchOptions.Add(new OptionData(
                kindId,
                $"{ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Kind, kindId, $"Kind {kindId}")} ({CountEntriesForKind(kindId)})"));
        }

        _scratchOptions.Sort(static (left, right) => string.Compare(left.Text, right.Text, StringComparison.Ordinal));
        _scratchOptions.Insert(0, new OptionData(AllFilterId, $"All kinds ({_entries.Count})"));
        _selectedKindFilterId = RebuildOption(_kindFilterOption, _scratchOptions, _selectedKindFilterId, AllFilterId);
    }

    private void RebuildProducerFilterOptions()
    {
        if (_producerFilterOption is null)
        {
            return;
        }

        var gambits = _entries.Count(entry => entry.ProducerKindCode == ProvenanceProducerKindCodes.Gambit);
        var generators = _entries.Count(entry => entry.ProducerKindCode == ProvenanceProducerKindCodes.Generator);
        var options = new List<OptionData>
        {
            new(AllFilterId, $"All producers ({_entries.Count})"),
            new(ProvenanceProducerKindCodes.Gambit, $"Gambits ({gambits})"),
            new(ProvenanceProducerKindCodes.Generator, $"Generators ({generators})"),
        };
        _selectedProducerFilterId = RebuildOption(
            _producerFilterOption,
            options,
            _selectedProducerFilterId,
            AllFilterId);
    }

    private void RebuildEventFilterOptions()
    {
        if (_eventFilterOption is null)
        {
            return;
        }

        var options = new List<OptionData>
        {
            new(AllFilterId, $"All events ({_entries.Count})"),
            new(EventGambitFilterId, $"Gambit ({_entries.Count(static entry => entry.EntryType == UiChronicleEntryType.Gambit)})"),
            new(EventGeneratorFilterId, $"Generator ({_entries.Count(static entry => entry.EntryType == UiChronicleEntryType.Generator)})"),
            new(EventHistoryChainFilterId, $"Chains ({_entries.Count(static entry => entry.EntryType == UiChronicleEntryType.HistoryChain)})"),
            new(EventTaleFilterId, $"Chronicles ({_entries.Count(static entry => entry.EntryType == UiChronicleEntryType.Tale)})"),
        };
        _selectedEventFilterId = RebuildOption(_eventFilterOption, options, _selectedEventFilterId, AllFilterId);
    }

    private void RebuildStateFilterOptions()
    {
        if (_stateFilterOption is null)
        {
            return;
        }

        var succeeded = _entries.Count(EntrySucceeded);
        var failed = _entries.Count(EntryFailed);
        var options = new List<OptionData>
        {
            new(AllFilterId, $"Any state ({_entries.Count})"),
            new(StateSucceededFilterId, $"Succeeded ({succeeded})"),
            new(StateFailedFilterId, $"Failed ({failed})"),
        };
        _selectedStateFilterId = RebuildOption(
            _stateFilterOption,
            options,
            _selectedStateFilterId,
            StateSucceededFilterId);
    }

    private void RebuildTickFilterOptions()
    {
        if (_tickFilterOption is null)
        {
            return;
        }

        var options = new List<OptionData>
        {
            new(TickWindowAllFilterId, "All ticks"),
            new(TickWindow10FilterId, "Last 10t"),
            new(TickWindow20FilterId, "Last 20t"),
            new(TickWindow50FilterId, "Last 50t"),
        };
        _selectedTickWindowFilterId = RebuildOption(
            _tickFilterOption,
            options,
            _selectedTickWindowFilterId,
            TickWindowAllFilterId);
    }

    private void RebuildSortOptions()
    {
        if (_sortOption is null)
        {
            return;
        }

        var options = new List<OptionData>
        {
            new((int)ChronicleSortMode.Newest, "Newest"),
            new((int)ChronicleSortMode.Oldest, "Oldest"),
            new((int)ChronicleSortMode.GroupEntity, "By entity"),
            new((int)ChronicleSortMode.GroupProducer, "By producer"),
        };
        var selected = RebuildOption(_sortOption, options, (int)_selectedSortMode, (int)ChronicleSortMode.Newest);
        _selectedSortMode = (ChronicleSortMode)selected;
    }

    private static int RebuildOption(
        OptionButton option,
        IReadOnlyList<OptionData> options,
        int selectedId,
        int fallbackId)
    {
        var hasSelected = options.Any(candidate => candidate.Id == selectedId);
        var effectiveSelected = hasSelected ? selectedId : fallbackId;
        option.Clear();
        foreach (var candidate in options)
        {
            option.AddItem(candidate.Text, candidate.Id);
        }

        var selectedIndex = FindOptionIndexById(option, effectiveSelected);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
            effectiveSelected = option.ItemCount > 0 ? option.GetItemId(0) : fallbackId;
        }

        if (selectedIndex >= 0)
        {
            option.Select(selectedIndex);
        }

        return effectiveSelected;
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

    private int CountEntriesForCulture(int cultureId) =>
        _entries.Count(entry => entry.Entities.Any(entity => entity.CultureId == cultureId));

    private int CountEntriesForKind(int kindId) =>
        _entries.Count(entry => entry.Entities.Any(entity => entity.KindId == kindId));

    private void RenderEntries()
    {
        if (_eventLogLabel is null)
        {
            return;
        }

        var filtered = FilteredEntries().ToArray();
        UpdateFilterSummary(filtered.Length);
        var lines = _selectedSortMode switch
        {
            ChronicleSortMode.GroupEntity => GroupedLines(filtered, EntityGroupKey),
            ChronicleSortMode.GroupProducer => GroupedLines(filtered, ProducerGroupKey),
            ChronicleSortMode.Oldest => filtered
                .OrderBy(static entry => entry.Tick)
                .ThenBy(static entry => entry.Sequence)
                .Take(MaxVisibleEntries)
                .Select(static entry => entry.Text)
                .ToArray(),
            _ => filtered
                .OrderByDescending(static entry => entry.Tick)
                .ThenByDescending(static entry => entry.Sequence)
                .Take(MaxVisibleEntries)
                .Select(static entry => entry.Text)
                .ToArray(),
        };

        _eventLogLabel.Text = string.Join('\n', lines);
    }

    private IEnumerable<UiChronicleEntry> FilteredEntries()
    {
        var maxTick = _entries.Count == 0 ? 0 : _entries.Max(static entry => entry.Tick);
        var minTick = _selectedTickWindowFilterId <= 0
            ? int.MinValue
            : Math.Max(0, maxTick - _selectedTickWindowFilterId + 1);
        return _entries.Where(entry =>
            entry.Tick >= minTick &&
            EntryMatchesCulture(entry) &&
            EntryMatchesKind(entry) &&
            EntryMatchesProducer(entry) &&
            EntryMatchesEvent(entry) &&
            EntryMatchesState(entry) &&
            EntryMatchesEntitySearch(entry));
    }

    private bool EntryMatchesCulture(UiChronicleEntry entry) =>
        _selectedCultureFilterId == AllFilterId ||
        entry.Entities.Any(entity => entity.CultureId == _selectedCultureFilterId);

    private bool EntryMatchesKind(UiChronicleEntry entry) =>
        _selectedKindFilterId == AllFilterId ||
        entry.Entities.Any(entity => entity.KindId == _selectedKindFilterId);

    private bool EntryMatchesProducer(UiChronicleEntry entry) =>
        _selectedProducerFilterId == AllFilterId ||
        entry.ProducerKindCode == _selectedProducerFilterId;

    private bool EntryMatchesEvent(UiChronicleEntry entry) => _selectedEventFilterId switch
    {
        AllFilterId => true,
        EventGambitFilterId => entry.EntryType == UiChronicleEntryType.Gambit,
        EventGeneratorFilterId => entry.EntryType == UiChronicleEntryType.Generator,
        EventHistoryChainFilterId => entry.EntryType == UiChronicleEntryType.HistoryChain,
        EventTaleFilterId => entry.EntryType == UiChronicleEntryType.Tale,
        _ => true,
    };

    private bool EntryMatchesState(UiChronicleEntry entry) => _selectedStateFilterId switch
    {
        AllFilterId => true,
        StateSucceededFilterId => EntrySucceeded(entry),
        StateFailedFilterId => EntryFailed(entry),
        _ => true,
    };

    private bool EntryMatchesEntitySearch(UiChronicleEntry entry)
    {
        if (string.IsNullOrWhiteSpace(_entitySearchText))
        {
            return true;
        }

        var query = _entitySearchText.Trim();
        return entry.Entities.Any(entity =>
            entity.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            entity.EntityId.ToString(System.Globalization.CultureInfo.InvariantCulture)
                .Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static bool EntrySucceeded(UiChronicleEntry entry) =>
        (entry.State & KernelEventState.Succeeded) != KernelEventState.None;

    private static bool EntryFailed(UiChronicleEntry entry) =>
        (entry.State & KernelEventState.Failed) != KernelEventState.None ||
        (entry.State & KernelEventState.Succeeded) == KernelEventState.None;

    private string[] GroupedLines(
        IReadOnlyList<UiChronicleEntry> entries,
        Func<UiChronicleEntry, GroupKey> keySelector)
    {
        var lines = new List<string>();
        foreach (var group in entries
            .GroupBy(keySelector)
            .OrderBy(static group => group.Key.Text, StringComparer.Ordinal))
        {
            lines.Add($"[color=#{HeaderColor}][b]{group.Key.Text}[/b][/color]");
            foreach (var entry in group
                .OrderByDescending(static entry => entry.Tick)
                .ThenByDescending(static entry => entry.Sequence)
                .Take(MaxVisibleEntries))
            {
                lines.Add(entry.Text);
            }

            if (lines.Count >= MaxVisibleEntries)
            {
                break;
            }
        }

        return lines.Take(MaxVisibleEntries).ToArray();
    }

    private static GroupKey EntityGroupKey(UiChronicleEntry entry)
    {
        var entity = entry.Entities.Count == 0 ? null : entry.Entities[0];
        return entity is null
            ? new GroupKey(0, "No entity")
            : new GroupKey(entity.EntityId, entity.DisplayName);
    }

    private GroupKey ProducerGroupKey(UiChronicleEntry entry) =>
        new(entry.ProducerId, ProducerName(entry));

    private string ProducerName(UiChronicleEntry entry)
    {
        var symbolKind = entry.ProducerKindCode switch
        {
            ProvenanceProducerKindCodes.Gambit => ProjectionSymbolKind.Gambit,
            ProvenanceProducerKindCodes.Generator => ProjectionSymbolKind.Generator,
            _ => ProjectionSymbolKind.None,
        };

        return symbolKind == ProjectionSymbolKind.None
            ? $"producer {entry.ProducerKindCode}:{entry.ProducerId}"
            : ProjectionDisplay.DisplayName(_symbols, symbolKind, entry.ProducerId, $"{symbolKind} {entry.ProducerId}");
    }

    private void UpdateFilterSummary(int shown)
    {
        if (_filterSummaryLabel is not null)
        {
            _filterSummaryLabel.Text = $"{shown} shown";
        }
    }

    private void OnEntitySearchTextChanged(string text)
    {
        if (_updatingFilterControls)
        {
            return;
        }

        _entitySearchText = text;
        ApplyFilterChange();
    }

    private void OnCultureFilterSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_cultureFilterOption, selectedIndex, _selectedCultureFilterId, out var selected))
        {
            _selectedCultureFilterId = selected;
            ApplyFilterChange();
        }
    }

    private void OnKindFilterSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_kindFilterOption, selectedIndex, _selectedKindFilterId, out var selected))
        {
            _selectedKindFilterId = selected;
            ApplyFilterChange();
        }
    }

    private void OnProducerFilterSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_producerFilterOption, selectedIndex, _selectedProducerFilterId, out var selected))
        {
            _selectedProducerFilterId = selected;
            ApplyFilterChange();
        }
    }

    private void OnEventFilterSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_eventFilterOption, selectedIndex, _selectedEventFilterId, out var selected))
        {
            _selectedEventFilterId = selected;
            ApplyFilterChange();
        }
    }

    private void OnStateFilterSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_stateFilterOption, selectedIndex, _selectedStateFilterId, out var selected))
        {
            _selectedStateFilterId = selected;
            ApplyFilterChange();
        }
    }

    private void OnTickFilterSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_tickFilterOption, selectedIndex, _selectedTickWindowFilterId, out var selected))
        {
            _selectedTickWindowFilterId = selected;
            ApplyFilterChange();
        }
    }

    private void OnSortSelected(long selectedIndex)
    {
        if (TryReadSelectedId(_sortOption, selectedIndex, (int)_selectedSortMode, out var selected))
        {
            _selectedSortMode = (ChronicleSortMode)selected;
            ApplyFilterChange();
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

    private void ApplyFilterChange()
    {
        FilterVersion++;
        RenderEntries();
    }

    private readonly record struct OptionData(int Id, string Text);

    private readonly record struct GroupKey(int Id, string Text);

    private enum ChronicleSortMode
    {
        Newest = 1,
        Oldest = 2,
        GroupEntity = 3,
        GroupProducer = 4,
    }
}
