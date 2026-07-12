#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class LegendPanel : PanelContainer
{
    private const long MinimumRefreshIntervalMs = 500;
    private const int MinorActorActivityWindowTicks = 160;
    private const int NationTrendSampleTicks = 500;

    private readonly Dictionary<(int CultureId, int PlaneCode), NationTrendTracker> _nationTrends = [];
    private IReadOnlyList<CulturePowerSummary>? _culturePowers;
    private VBoxContainer? _column;
    private LegendState? _lastState;
    private HexMapProjection? _lastMap;
    private long _lastRenderedAtMs;
    private int _mapGeneration;

    public override void _Ready()
    {
        AssetLibrary.Load();
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeStyleboxOverride("panel", UiTheme.OverlayPanelStyle());
        _column = GetNode<VBoxContainer>("Column");
        _column.AddThemeConstantOverride("separation", UiTokens.Space1);
        Render(null, null, null, PlaneMode.Physical);
    }

    public bool BindCultureOverview(
        HexMapProjection? map,
        UiProjectionState? projection,
        IProjectionSymbolCatalog? symbols,
        PlaneMode plane,
        IReadOnlyList<CulturePowerSummary>? culturePowers = null)
    {
        _culturePowers = culturePowers;
        if (!ReferenceEquals(_lastMap, map))
        {
            _lastMap = map;
            _mapGeneration++;
        }

        var state = CreateState(map, projection, plane);
        if (!ShouldRender(state))
        {
            return false;
        }

        _lastState = state;
        Render(map, projection, symbols, plane);
        return true;
    }

    private LegendState CreateState(HexMapProjection? map, UiProjectionState? projection, PlaneMode plane)
    {
        var cellVersion = map is null ? -1 : map.CellVersion;
        var entityMapVersion = projection is null ? -1 : projection.EntityMapVersion;
        var minorActorActivityVersion = projection is null ? -1 : projection.MinorActorActivityVersion;
        var minorActorActivityBucket = projection is null ? -1 : projection.CurrentTick / 10;
        return new LegendState(
            _mapGeneration,
            cellVersion,
            entityMapVersion,
            minorActorActivityVersion,
            minorActorActivityBucket,
            plane);
    }

    private bool ShouldRender(LegendState state)
    {
        if (_lastState == state)
        {
            return false;
        }

        var now = System.Environment.TickCount64;
        if (IsRefreshThrottled(state, now))
        {
            return false;
        }

        _lastRenderedAtMs = now;
        return true;
    }

    private bool IsRefreshThrottled(LegendState state, long now) =>
        _lastState is { } last &&
        last.MapGeneration == state.MapGeneration &&
        last.Plane == state.Plane &&
        now - _lastRenderedAtMs < MinimumRefreshIntervalMs;

    private void Render(
        HexMapProjection? map,
        UiProjectionState? projection,
        IProjectionSymbolCatalog? symbols,
        PlaneMode plane)
    {
        if (_column is null)
        {
            return;
        }

        ClearColumn();
        AddTitle("CULTURES");
        if (map is not null && projection is not null)
        {
            AddCultureRows(map, projection, symbols, plane);
            AddTheaterRows(map, projection, symbols, plane);
        }
        else
        {
            AddMutedLine("waiting for map");
        }

        AddDivider();
        AddTitle("LEGEND");
        AddLegendRow("penguin", UiTokens.KindPenguin, "penguin");
        AddLegendRow("settlement", UiTokens.KindSettlement, "settlement");
        AddLegendRow("faction", UiTokens.KindFaction, "faction");
        AddLegendRow("artifact", UiTokens.KindArtifact, "artifact");
        AddLegendRow("orca", UiTokens.KindOrca, "orca");
    }

    private void AddCultureRows(
        HexMapProjection map,
        UiProjectionState projection,
        IProjectionSymbolCatalog? symbols,
        PlaneMode plane)
    {
        var planeCode = plane == PlaneMode.Physical ? PlaneCodes.Physical : PlaneCodes.Semantic;
        var cells = map.Hexes(planeCode).ToArray();
        var cultures = CultureSymbols(symbols).ToArray();
        if (cultures.Length == 0)
        {
            AddMutedLine("no cultures");
            return;
        }

        foreach (var culture in cultures)
        {
            var summary = CultureSummary.For(culture.Id, cells, projection);
            AddCultureRow(
                culture.DisplayName,
                UiTokens.CultureColor(culture.StableName),
                $"{summary.DominantCells} cells  {summary.ActiveEntities} entities",
                summary.ControlledCells > 0
                    ? $"{summary.ControlledCells} held / {summary.MismatchedControl} contested  avg {summary.AverageShare:0}%"
                    : $"avg {summary.AverageShare:0}%");
            AddNationRow(culture.Id, cells, projection.CurrentTick, planeCode);
            AddCulturePowerRow(culture.Id);
            AddCultureSlotRows(culture.Id, projection, symbols);
            AddMinorActorActivityRow(culture.Id, projection, planeCode);
        }
    }

    private void AddNationRow(int cultureId, HexProjection[] cells, int currentTick, int planeCode)
    {
        var nation = NationSummary.For(cultureId, cells);
        if (nation.InfluencedCells == 0)
        {
            return;
        }

        var key = (cultureId, planeCode);
        if (!_nationTrends.TryGetValue(key, out var tracker))
        {
            tracker = new NationTrendTracker();
            _nationTrends[key] = tracker;
        }

        var trend = tracker.Update(currentTick, NationTrendSampleTicks, nation);
        AddMutedLine(
            $"territory {nation.InfluencedCells}{trend.Territory}  " +
            $"heartland {nation.LargestComponentShare:0}%{trend.Heartland}  " +
            $"nexus {nation.NexusCells}{trend.Nexus}  " +
            $"contested {nation.ContestedCells}{trend.Contested}");
    }

    private void AddCulturePowerRow(int cultureId)
    {
        if (_culturePowers is not { Count: > 0 } powers)
        {
            return;
        }

        foreach (var power in powers)
        {
            if (power.CultureId != cultureId)
            {
                continue;
            }

            AddMutedLine(
                $"witness {power.WitnessShare:0}%  " +
                $"shadow {power.ShadowShare:0}%  " +
                $"wake {power.WakeShare:0}%");
            return;
        }
    }

    private void AddCultureSlotRows(
        int cultureId,
        UiProjectionState projection,
        IProjectionSymbolCatalog? symbols)
    {
        var slots = projection.CultureSlotDefinitions.Values
            .Where(slot => slot.CultureId == cultureId)
            .OrderBy(slot => slot.SlotId)
            .ToArray();
        foreach (var slot in slots)
        {
            projection.CultureSlots.TryGetValue(slot.Key, out var occupancy);
            AddCultureSlotRow(
                slot,
                occupancy is { CurrentEntityId: > 0 }
                    ? ProjectionDisplay.EntityName(symbols, projection, occupancy.CurrentEntityId)
                    : "unfilled",
                occupancy?.CurrentScore);
        }
    }

    private void AddTheaterRows(
        HexMapProjection map,
        UiProjectionState projection,
        IProjectionSymbolCatalog? symbols,
        PlaneMode plane)
    {
        var planeCode = plane == PlaneMode.Physical ? PlaneCodes.Physical : PlaneCodes.Semantic;
        var theaters = map.CultureTheaters(planeCode, ownerId => OwnerCultureId(projection, ownerId))
            .Take(3)
            .ToArray();
        if (theaters.Length == 0)
        {
            AddMutedLine("no contested theaters");
            return;
        }

        AddMutedLine("theaters");
        foreach (var theater in theaters)
        {
            var dominant = CultureName(symbols, theater.DominantCultureId);
            var rival = theater.RivalCultureId > 0 ? CultureName(symbols, theater.RivalCultureId) : "unopposed";
            AddMutedLine(
                $"{dominant} / {rival}: {theater.CellCount} cells, {theater.ContestedCellCount} contested, {theater.ControlMismatchCellCount} held against influence");
        }
    }

    private void AddMinorActorActivityRow(int cultureId, UiProjectionState projection, int planeCode)
    {
        var summary = MinorActorActivitySummary.For(cultureId, projection, planeCode);
        if (summary.TotalActions == 0)
        {
            return;
        }

        AddMutedLine(
            $"labor: {summary.CultureActions} culture / {summary.RoadActions} road  latest {summary.LatestAge(projection.CurrentTick)}t");
    }

    private void AddTitle(string text)
    {
        var title = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontOverride("font", UiTheme.BodyFont);
        title.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        title.AddThemeColorOverride("font_color", UiTokens.Parchment);
        _column!.AddChild(title);
    }

    private void AddCultureRow(string labelText, Color color, string primary, string secondary)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", UiTokens.Space2);
        _column!.AddChild(row);

        row.AddChild(new ColorRect
        {
            Color = color,
            CustomMinimumSize = new Vector2(18, 18),
            MouseFilter = MouseFilterEnum.Ignore,
        });

        var text = new Label
        {
            Text = $"{labelText}\n{primary}\n{secondary}",
        };
        text.AddThemeFontOverride("font", UiTheme.BodyFont);
        text.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        text.AddThemeColorOverride("font_color", UiTokens.Vellum);
        row.AddChild(text);
    }

    private void AddCultureSlotRow(
        UiCultureSlotDefinitionProjection slot,
        string ownerName,
        float? score)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", UiTokens.Space1);
        _column!.AddChild(row);

        row.AddChild(new Control
        {
            CustomMinimumSize = new Vector2(20, 1),
            MouseFilter = MouseFilterEnum.Ignore,
        });

        row.AddChild(new ColorRect
        {
            Color = SlotPolarityColor(slot.PolarityCode),
            CustomMinimumSize = new Vector2(8, 8),
            MouseFilter = MouseFilterEnum.Ignore,
        });

        var text = new Label
        {
            Text = score is { } value
                ? $"{slot.DisplayName}: {ownerName}  {value:0.0}"
                : $"{slot.DisplayName}: {ownerName}",
        };
        text.AddThemeFontOverride("font", UiTheme.BodyFont);
        text.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        text.AddThemeColorOverride("font_color", UiTokens.Parchment);
        row.AddChild(text);
    }

    private void AddLegendRow(string kindId, Color accent, string labelText)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", UiTokens.Space2);
        _column!.AddChild(row);

        Control glyph;
        var sprite = AssetLibrary.KindMapIcon(kindId)
            ?? AssetLibrary.KindSprite(kindId);
        if (sprite is not null)
        {
            glyph = new TextureRect
            {
                Texture = sprite,
                CustomMinimumSize = new Vector2(22, 22),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
            };
        }
        else
        {
            glyph = new KindGlyphRect
            {
                KindId = kindId,
                Accent = accent,
                CustomMinimumSize = new Vector2(18, 18),
            };
        }

        row.AddChild(glyph);

        var label = new Label { Text = labelText };
        label.AddThemeFontOverride("font", UiTheme.BodyFont);
        label.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        label.AddThemeColorOverride("font_color", UiTokens.Parchment);
        row.AddChild(label);
    }

    private void AddMutedLine(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontOverride("font", UiTheme.BodyFont);
        label.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        label.AddThemeColorOverride("font_color", UiTokens.Whisper);
        _column!.AddChild(label);
    }

    private void AddDivider()
    {
        _column!.AddChild(new ColorRect
        {
            Color = UiTokens.Hairline,
            CustomMinimumSize = new Vector2(0, 1),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
    }

    private void ClearColumn()
    {
        foreach (var child in _column!.GetChildren())
        {
            _column.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static IEnumerable<ProjectionSymbol> CultureSymbols(IProjectionSymbolCatalog? symbols)
    {
        if (symbols is null)
        {
            return [];
        }

        return symbols.Symbols.Values
            .Where(symbol => symbol.Kind == ProjectionSymbolKind.Culture && symbol.Id > 0)
            .OrderBy(symbol => symbol.DisplayName, StringComparer.Ordinal);
    }

    private static int DominantCultureId(HexProjection cell) =>
        cell.CultureInfluences.Count == 0 ? 0 : cell.CultureInfluences[0].CultureId;

    private static int OwnerCultureId(UiProjectionState projection, int ownerEntityId) =>
        ownerEntityId > 0 && projection.Entities.TryGetValue(ownerEntityId, out var owner)
            ? owner.CultureId
            : 0;

    private static string CultureName(IProjectionSymbolCatalog? symbols, int cultureId) =>
        ProjectionDisplay.DisplayName(
            symbols,
            ProjectionSymbolKind.Culture,
            cultureId,
            cultureId > 0 ? $"Culture {cultureId}" : "none");

    private static Color SlotPolarityColor(int polarityCode) => polarityCode switch
    {
        RelationshipPolarityCodes.Positive => UiTokens.Aurora,
        RelationshipPolarityCodes.Negative => UiTokens.Blood,
        RelationshipPolarityCodes.Neutral => UiTokens.Gilt,
        _ => UiTokens.Parchment,
    };

    private readonly record struct CultureSummary(
        int DominantCells,
        int ControlledCells,
        int MismatchedControl,
        int ActiveEntities,
        float AverageShare)
    {
        public static CultureSummary For(
            int cultureId,
            HexProjection[] cells,
            UiProjectionState projection)
        {
            var accumulator = new CultureSummaryAccumulator(cultureId, projection);
            foreach (var cell in cells)
            {
                accumulator.Add(cell);
            }

            return accumulator.ToSummary(cells.Length);
        }
    }

    private struct CultureSummaryAccumulator
    {
        private readonly int _cultureId;
        private readonly UiProjectionState _projection;
        private int _dominantCells;
        private int _controlledCells;
        private int _mismatchedControl;
        private float _shareTotal;

        public CultureSummaryAccumulator(int cultureId, UiProjectionState projection)
        {
            _cultureId = cultureId;
            _projection = projection;
        }

        public void Add(HexProjection cell)
        {
            var dominantCultureId = DominantCultureId(cell);
            _dominantCells += dominantCultureId == _cultureId ? 1 : 0;
            _shareTotal += CultureShare(cell, _cultureId);
            if (OwnerCultureId(_projection, cell.OwnerEntityId) == _cultureId)
            {
                _controlledCells++;
                _mismatchedControl += dominantCultureId > 0 && dominantCultureId != _cultureId ? 1 : 0;
            }
        }

        public readonly CultureSummary ToSummary(int cellCount)
        {
            var cultureId = _cultureId;
            var activeEntities = _projection.Entities.Values.Count(entity =>
                entity.StatusCode == EntityStatusCodes.Active &&
                entity.CultureId == cultureId);

            return new(
                _dominantCells,
                _controlledCells,
                _mismatchedControl,
                activeEntities,
                cellCount == 0 ? 0f : _shareTotal / cellCount);
        }
    }

    private static float CultureShare(HexProjection cell, int cultureId) =>
        cell.CultureInfluences.FirstOrDefault(influence => influence.CultureId == cultureId)?.Share ?? 0f;

    private readonly record struct LegendState(
        int MapGeneration,
        long CellVersion,
        long EntityMapVersion,
        long MinorActorActivityVersion,
        int MinorActorActivityBucket,
        PlaneMode Plane);

    private readonly record struct NationSummary(
        int InfluencedCells,
        int NexusCells,
        int ContestedCells,
        int LargestComponentCells)
    {
        public float LargestComponentShare =>
            InfluencedCells == 0 ? 0f : LargestComponentCells * 100f / InfluencedCells;

        public static NationSummary For(int cultureId, HexProjection[] cells)
        {
            var influenced = 0;
            var nexus = 0;
            var contested = 0;
            var largestComponent = 0;
            foreach (var cell in cells)
            {
                foreach (var influence in cell.CultureInfluences)
                {
                    if (influence.CultureId != cultureId || influence.RawMass <= 0f)
                    {
                        continue;
                    }

                    influenced++;
                    nexus += influence.StrengthBandCode >= CultureFieldStrengthBandCodes.Nexus ? 1 : 0;
                    contested += influence.CellKindCode == CultureFieldCellKindCodes.Contested ? 1 : 0;
                    largestComponent = Math.Max(largestComponent, influence.ComponentCellCount);
                }
            }

            return new NationSummary(influenced, nexus, contested, largestComponent);
        }
    }

    private readonly record struct NationTrend(
        string Territory,
        string Heartland,
        string Nexus,
        string Contested);

    private sealed class NationTrendTracker
    {
        private int _sampleTick = -1;
        private NationSummary _sample;
        private NationTrend _trend = new(string.Empty, string.Empty, string.Empty, string.Empty);

        public NationTrend Update(int currentTick, int sampleWindowTicks, in NationSummary current)
        {
            if (_sampleTick < 0)
            {
                _sampleTick = currentTick;
                _sample = current;
                return _trend;
            }

            if (currentTick - _sampleTick >= sampleWindowTicks)
            {
                _trend = new NationTrend(
                    Arrow(current.InfluencedCells - _sample.InfluencedCells),
                    Arrow((int)(current.LargestComponentShare - _sample.LargestComponentShare)),
                    Arrow(current.NexusCells - _sample.NexusCells),
                    Arrow(current.ContestedCells - _sample.ContestedCells));
                _sampleTick = currentTick;
                _sample = current;
            }

            return _trend;
        }

        private static string Arrow(int delta) => delta switch
        {
            > 0 => "▴",
            < 0 => "▾",
            _ => string.Empty,
        };
    }

    private readonly record struct MinorActorActivitySummary(
        int CultureActions,
        int RoadActions,
        int LatestTick)
    {
        public int TotalActions => CultureActions + RoadActions;

        public int LatestAge(int currentTick) => Math.Max(0, currentTick - LatestTick);

        public static MinorActorActivitySummary For(int cultureId, UiProjectionState projection, int planeCode)
        {
            var cultureActions = 0;
            var roadActions = 0;
            var latestTick = 0;
            foreach (var activity in projection.MinorActorActivities)
            {
                if (activity.StatusCode != TraceStatusCodes.Selected ||
                    activity.CultureId != cultureId ||
                    activity.PlaneCode != planeCode ||
                    projection.CurrentTick - activity.Tick > MinorActorActivityWindowTicks)
                {
                    continue;
                }

                latestTick = Math.Max(latestTick, activity.Tick);
                if (activity.ActionCode == MinorActorActionCodes.RoadMaintenance)
                {
                    roadActions++;
                    continue;
                }

                cultureActions++;
            }

            return new MinorActorActivitySummary(cultureActions, roadActions, latestTick);
        }
    }
}
