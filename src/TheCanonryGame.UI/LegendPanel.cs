#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class LegendPanel : PanelContainer
{
    private const ulong MinimumRefreshIntervalMs = 500;

    private VBoxContainer? _column;
    private LegendState? _lastState;
    private ulong _lastRenderedAtMs;

    public override void _Ready()
    {
        AssetLibrary.Load();
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeStyleboxOverride("panel", UiTheme.OverlayPanelStyle());
        _column = GetNode<VBoxContainer>("Column");
        _column.AddThemeConstantOverride("separation", UiTokens.Space1);
        Render(null, null, null, PlaneMode.Physical);
    }

    public void BindCultureOverview(
        HexMapProjection? map,
        UiProjectionState? projection,
        IProjectionSymbolCatalog? symbols,
        PlaneMode plane)
    {
        var state = new LegendState(
            map is null ? 0 : global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(map),
            map?.CellVersion ?? -1,
            projection?.EntityMapVersion ?? -1,
            plane);
        if (!ShouldRender(state, Time.GetTicksMsec()))
        {
            return;
        }

        _lastState = state;
        Render(map, projection, symbols, plane);
    }

    private bool ShouldRender(LegendState state, ulong now)
    {
        if (_lastState == state || IsRefreshThrottled(state, now))
        {
            return false;
        }

        _lastRenderedAtMs = now;
        return true;
    }

    private bool IsRefreshThrottled(LegendState state, ulong now) =>
        _lastState is { } last &&
        last.MapIdentity == state.MapIdentity &&
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
            AddCultureSlotRows(culture.Id, projection, symbols);
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
        int MapIdentity,
        long CellVersion,
        long EntityMapVersion,
        PlaneMode Plane);
}
