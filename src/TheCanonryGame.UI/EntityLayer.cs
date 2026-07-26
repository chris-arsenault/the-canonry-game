#pragma warning disable CA1303
#pragma warning disable CA2213
using Engine.Kinds;
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>
/// Batched map presentation for active entities. One canvas item owns drawing,
/// animation, and hit testing for the complete visible entity set; individual
/// entities are data records rather than Godot scene/physics subtrees.
/// </summary>
public partial class EntityLayer : Node2D
{
    private const float ExpandedStackZoom = 2.1f;
    private const float MotionDurationSeconds = 0.22f;
    private const int RecentActionTicks = 16;
    private const long StackCycleWindowMs = 850;

    private readonly Dictionary<int, RenderedEntity> _renderedEntities = [];
    private readonly Dictionary<HexCoord, List<int>> _entityIdsByHex = [];
    private readonly SortedSet<HexCoord> _orderedHexes = new(
        Comparer<HexCoord>.Create(static (left, right) =>
        {
            var row = left.R.CompareTo(right.R);
            return row != 0 ? row : left.Q.CompareTo(right.Q);
        }));
    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private HashSet<int>? _visibleEntityIds;
    private int? _highlightedEntityId;
    private bool _showEntityNamesForZoom;
    private float _cameraZoom = 1.5f;
    private int _currentTick;
    private int _latestActionTick = -1;
    private HexCoord? _lastPickedHex;
    private int _lastPickedIndex;
    private long _lastPickedAtMs;

    [Signal]
    public delegate void EntitySelectedEventHandler(int entityIdValue);

    public float HexSize { get; set; } = 30f;

    public PlaneMode CurrentPlane { get; private set; } = PlaneMode.Physical;

    public int CurrentPlaneCode => PlaneCodeForCurrentPlane();

    public bool ShowEntityNames { get; private set; }

    public bool EffectiveShowEntityNames => ShowEntityNames || _showEntityNamesForZoom;

    public int? HighlightedEntityId => _highlightedEntityId;

    private bool StacksExpanded => _cameraZoom >= ExpandedStackZoom;

    public override void _Ready()
    {
        AssetLibrary.Load();
        SetProcess(false);
    }

    public void Initialize(UiProjectionState? projection, IProjectionSymbolCatalog? symbols, float hexSize)
    {
        _projection = projection;
        _symbols = symbols;
        HexSize = hexSize;
        _currentTick = projection?.CurrentTick ?? 0;
        Refresh();
    }

    public bool SetPlane(PlaneMode plane)
    {
        if (CurrentPlane == plane)
        {
            return false;
        }

        CurrentPlane = plane;
        _lastPickedHex = null;
        return true;
    }

    public bool SetShowEntityNames(bool showEntityNames)
    {
        if (ShowEntityNames == showEntityNames)
        {
            return false;
        }

        ShowEntityNames = showEntityNames;
        QueueRedraw();
        return true;
    }

    public bool SetShowEntityNamesForZoom(bool showEntityNamesForZoom)
    {
        if (_showEntityNamesForZoom == showEntityNamesForZoom)
        {
            return false;
        }

        _showEntityNamesForZoom = showEntityNamesForZoom;
        QueueRedraw();
        return true;
    }

    public bool SetCameraZoom(float cameraZoom)
    {
        cameraZoom = Math.Max(0.01f, cameraZoom);
        if (Mathf.IsEqualApprox(_cameraZoom, cameraZoom))
        {
            return false;
        }

        var wasExpanded = StacksExpanded;
        _cameraZoom = cameraZoom;
        if (wasExpanded != StacksExpanded)
        {
            PositionAllStacks(animate: false);
        }

        QueueRedraw();
        return true;
    }

    public void SetCurrentTick(int currentTick)
    {
        if (_currentTick == currentTick)
        {
            return;
        }

        _currentTick = currentTick;
        if (IsRecentAction(_latestActionTick))
        {
            QueueRedraw();
        }
    }

    public void NotifyViewportChanged() => QueueRedraw();

    public bool SetVisibleEntityIds(IReadOnlySet<int>? visibleEntityIds)
    {
        if (visibleEntityIds is null)
        {
            if (_visibleEntityIds is null)
            {
                return false;
            }

            _visibleEntityIds = null;
            return true;
        }

        if (_visibleEntityIds is not null && _visibleEntityIds.SetEquals(visibleEntityIds))
        {
            return false;
        }

        _visibleEntityIds = [.. visibleEntityIds];
        return true;
    }

    public bool SetHighlightedEntity(int? entityId)
    {
        if (entityId is <= 0)
        {
            entityId = null;
        }

        if (_highlightedEntityId == entityId)
        {
            return false;
        }

        _highlightedEntityId = entityId;
        QueueRedraw();
        return true;
    }

    /// <summary>Rendered position in this layer's local space for map overlays.</summary>
    public bool TryGetEntityLocalPosition(int entityId, out Vector2 localPosition)
    {
        if (_renderedEntities.TryGetValue(entityId, out var entity))
        {
            localPosition = StacksExpanded ? entity.Position : HexToPixel(entity.Coord);
            return true;
        }

        localPosition = default;
        return false;
    }

    public bool TryGetEntityWorldPosition(int entityId, out Vector2 worldPosition)
    {
        if (TryGetEntityLocalPosition(entityId, out var localPosition))
        {
            worldPosition = ToGlobal(localPosition);
            return true;
        }

        if (_projection?.Entities.TryGetValue(entityId, out var entity) == true &&
            entity.StatusCode == EntityStatusCodes.Active)
        {
            worldPosition = ToGlobal(HexToPixel(EntityCoordForCurrentPlane(entity)));
            return true;
        }

        worldPosition = default;
        return false;
    }

    public void Refresh()
    {
        if (_projection is null)
        {
            ClearRenderedEntities();
            return;
        }

        var currentIds = new HashSet<int>();
        _entityIdsByHex.Clear();
        _latestActionTick = -1;
        foreach (var entity in ActiveEntitiesInRenderOrder())
        {
            var coord = EntityCoordForCurrentPlane(entity);
            currentIds.Add(entity.EntityId);
            BindEntity(entity, coord);
            EntityIdsAt(coord).Add(entity.EntityId);
        }

        foreach (var entityId in _renderedEntities.Keys.Where(entityId => !currentIds.Contains(entityId)).ToArray())
        {
            _renderedEntities.Remove(entityId);
        }

        RebuildOrderedHexes();
        PositionAllStacks(animate: true);
        QueueRedraw();
    }

    public void RefreshDirty(IReadOnlyCollection<HexCoord> dirtyHexes)
    {
        ArgumentNullException.ThrowIfNull(dirtyHexes);

        if (_projection is null)
        {
            ClearRenderedEntities();
            return;
        }

        if (dirtyHexes.Count == 0)
        {
            return;
        }

        var dirty = dirtyHexes as HashSet<HexCoord> ?? dirtyHexes.ToHashSet();
        var currentIdsInDirtyHexes = BindDirtyHexes(dirty);
        RemoveStaleEntities(dirty, currentIdsInDirtyHexes);
        RebuildDirtyStacks(dirty, currentIdsInDirtyHexes);
        UpdateOrderedHexes(dirty);
        foreach (var coord in dirty)
        {
            PositionStack(coord, animate: true);
        }

        QueueRedraw();
    }

    private HashSet<int> BindDirtyHexes(IReadOnlySet<HexCoord> dirty)
    {
        var currentIds = new HashSet<int>();
        foreach (var coord in dirty)
        {
            BindDirtyHex(coord, currentIds);
        }

        return currentIds;
    }

    private void BindDirtyHex(HexCoord coord, HashSet<int> currentIds)
    {
        foreach (var entityId in _projection!.ActiveOccupants(PlaneCodeForCurrentPlane(), coord))
        {
            if (!_projection.Entities.TryGetValue(entityId, out var entity) ||
                entity.StatusCode != EntityStatusCodes.Active ||
                !EntityPassesVisibleFilter(entity))
            {
                continue;
            }

            currentIds.Add(entityId);
            BindEntity(entity, coord);
        }
    }

    private void RemoveStaleEntities(HashSet<HexCoord> dirty, HashSet<int> currentIds)
    {
        foreach (var coord in dirty)
        {
            if (!_entityIdsByHex.TryGetValue(coord, out var previousIds))
            {
                continue;
            }

            foreach (var entityId in previousIds)
            {
                if (!currentIds.Contains(entityId))
                {
                    _renderedEntities.Remove(entityId);
                }
            }
        }
    }

    private void RebuildDirtyStacks(IReadOnlySet<HexCoord> dirty, IEnumerable<int> currentIds)
    {
        foreach (var coord in dirty)
        {
            _entityIdsByHex.Remove(coord);
        }

        foreach (var entityId in currentIds)
        {
            if (_renderedEntities.TryGetValue(entityId, out var rendered))
            {
                EntityIdsAt(rendered.Coord).Add(entityId);
            }
        }
    }

    public override void _Process(double delta)
    {
        var moving = false;
        foreach (var entity in _renderedEntities.Values)
        {
            if (!entity.IsMoving)
            {
                continue;
            }

            entity.MotionElapsed += (float)Math.Max(0d, delta);
            var progress = Mathf.Clamp(entity.MotionElapsed / MotionDurationSeconds, 0f, 1f);
            var eased = 1f - ((1f - progress) * (1f - progress));
            entity.Position = entity.MotionFrom.Lerp(entity.MotionTarget, eased);
            if (progress >= 1f)
            {
                entity.Position = entity.MotionTarget;
                entity.IsMoving = false;
            }
            else
            {
                moving = true;
            }
        }

        QueueRedraw();
        if (!moving)
        {
            SetProcess(false);
        }
    }

    public override void _Draw()
    {
        if (_renderedEntities.Count == 0)
        {
            return;
        }

        var visibleRect = VisibleLocalRect();
        foreach (var coord in _orderedHexes)
        {
            if (!TryGetVisibleStack(coord, visibleRect, out var entityIds))
            {
                continue;
            }

            DrawStack(entityIds);
        }
    }

    private bool TryGetVisibleStack(HexCoord coord, Rect2 visibleRect, out List<int> entityIds)
    {
        if (_entityIdsByHex.TryGetValue(coord, out entityIds!) &&
            entityIds.Count > 0 &&
            visibleRect.HasPoint(HexToPixel(coord)))
        {
            return true;
        }

        entityIds = [];
        return false;
    }

    private void DrawStack(List<int> entityIds)
    {
        if (StacksExpanded)
        {
            foreach (var entityId in entityIds)
            {
                if (_renderedEntities.TryGetValue(entityId, out var entity))
                {
                    DrawEntityToken(entity, entity.Position, stackCount: 1);
                }
            }

            return;
        }

        var representative = Representative(entityIds);
        if (representative is not null)
        {
            DrawEntityToken(representative, representative.Position, entityIds.Count);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } ||
            GetViewport().GuiGetHoveredControl() is not null)
        {
            return;
        }

        var pickedEntityId = PickEntity(GetLocalMousePosition());
        if (pickedEntityId <= 0)
        {
            return;
        }

        EmitSignal(SignalName.EntitySelected, pickedEntityId);
        GetViewport().SetInputAsHandled();
    }

    /// <summary>Hex center in this layer's local space, for overlay layers.</summary>
    public Vector2 HexCenter(HexCoord coord) => HexToPixel(coord);

    private IEnumerable<UiEntityProjection> ActiveEntitiesInRenderOrder() =>
        _projection!.Entities.Values
            .Where(entity => entity.StatusCode == EntityStatusCodes.Active && EntityPassesVisibleFilter(entity))
            .OrderBy(entity => entity.EntityId);

    private void BindEntity(UiEntityProjection entity, HexCoord coord)
    {
        if (!_renderedEntities.TryGetValue(entity.EntityId, out var rendered))
        {
            var center = HexToPixel(coord);
            rendered = new RenderedEntity(entity.EntityId, coord, center);
            _renderedEntities.Add(entity.EntityId, rendered);
        }

        rendered.Coord = coord;
        rendered.KindStableName = ProjectionDisplay.StableName(
            _symbols,
            ProjectionSymbolKind.Kind,
            entity.KindId,
            $"kind-{entity.KindId}");
        rendered.SubtypeStableName = ProjectionDisplay.NullableStableName(
            _symbols,
            ProjectionSymbolKind.Subtype,
            entity.SubtypeId);
        rendered.Name = ProjectionDisplay.EntityName(_symbols, entity);
        rendered.Accent = UiTokens.KindAccent(rendered.KindStableName);
        rendered.CultureAccent = UiTokens.CultureColor(ProjectionDisplay.StableName(
            _symbols,
            ProjectionSymbolKind.Culture,
            entity.CultureId,
            "none"));
        rendered.GlyphSize = rendered.KindStableName == "settlement" ? 14f : 10f;
        rendered.Texture = AssetLibrary.SubtypeMapIcon(rendered.KindStableName, rendered.SubtypeStableName)
            ?? AssetLibrary.KindSprite(rendered.KindStableName);
        rendered.CultureSlotCount = entity.CultureSlotCount;
        rendered.CultureSlotPolarityCode = entity.CultureSlotPolarityCode;
        rendered.Tier = _projection?.EntityTierOf(entity.EntityId) ?? EntityTier.None;
        rendered.LastActionTick = RecentActionTick(entity);
        _latestActionTick = Math.Max(_latestActionTick, rendered.LastActionTick);
    }

    private List<int> EntityIdsAt(HexCoord coord)
    {
        if (!_entityIdsByHex.TryGetValue(coord, out var entityIds))
        {
            entityIds = [];
            _entityIdsByHex.Add(coord, entityIds);
        }

        return entityIds;
    }

    private void RebuildOrderedHexes()
    {
        foreach (var empty in _entityIdsByHex.Where(entry => entry.Value.Count == 0).Select(entry => entry.Key).ToArray())
        {
            _entityIdsByHex.Remove(empty);
        }

        _orderedHexes.Clear();
        foreach (var coord in _entityIdsByHex.Keys)
        {
            _orderedHexes.Add(coord);
        }
    }

    private void UpdateOrderedHexes(IEnumerable<HexCoord> dirty)
    {
        foreach (var coord in dirty)
        {
            if (_entityIdsByHex.TryGetValue(coord, out var entityIds) && entityIds.Count > 0)
            {
                _orderedHexes.Add(coord);
            }
            else
            {
                _orderedHexes.Remove(coord);
            }
        }
    }

    private void PositionAllStacks(bool animate)
    {
        foreach (var coord in _orderedHexes)
        {
            PositionStack(coord, animate);
        }
    }

    private void PositionStack(HexCoord coord, bool animate)
    {
        if (!_entityIdsByHex.TryGetValue(coord, out var entityIds))
        {
            return;
        }

        entityIds.Sort();
        for (var index = 0; index < entityIds.Count; index++)
        {
            if (!_renderedEntities.TryGetValue(entityIds[index], out var entity))
            {
                continue;
            }

            var offset = StacksExpanded ? FanOffset(index, HexSize * 0.38f) : Vector2.Zero;
            MoveEntity(entity, HexToPixel(coord) + offset, animate);
        }
    }

    private void MoveEntity(RenderedEntity entity, Vector2 target, bool animate)
    {
        if (entity.Position.DistanceSquaredTo(target) < 0.01f)
        {
            entity.Position = target;
            entity.MotionTarget = target;
            entity.IsMoving = false;
            return;
        }

        if (!animate)
        {
            entity.Position = target;
            entity.MotionFrom = target;
            entity.MotionTarget = target;
            entity.MotionElapsed = 0f;
            entity.IsMoving = false;
            return;
        }

        entity.MotionFrom = entity.Position;
        entity.MotionTarget = target;
        entity.MotionElapsed = 0f;
        entity.IsMoving = true;
        SetProcess(true);
    }

    private RenderedEntity? Representative(List<int> entityIds)
    {
        RenderedEntity? best = null;
        foreach (var entityId in entityIds)
        {
            if (!_renderedEntities.TryGetValue(entityId, out var candidate))
            {
                continue;
            }

            if (entityId == _highlightedEntityId)
            {
                return candidate;
            }

            if (best is null || CompareRepresentative(candidate, best) > 0)
            {
                best = candidate;
            }
        }

        return best;
    }

    private static int CompareRepresentative(RenderedEntity left, RenderedEntity right)
    {
        var tier = TierPriority(left.Tier).CompareTo(TierPriority(right.Tier));
        if (tier != 0)
        {
            return tier;
        }

        var action = left.LastActionTick.CompareTo(right.LastActionTick);
        return action != 0 ? action : right.EntityId.CompareTo(left.EntityId);
    }

    private void DrawEntityToken(RenderedEntity entity, Vector2 position, int stackCount)
    {
        var highlighted = entity.EntityId == _highlightedEntityId;
        var tokenRadius = entity.GlyphSize * (highlighted ? 2.45f : 2.02f);
        DrawTokenFrame(entity, position, tokenRadius, highlighted);
        DrawRecentAction(entity, position, tokenRadius);
        DrawEntityGlyph(entity, position, highlighted);
        DrawCultureBadge(entity, position, tokenRadius, highlighted);
        DrawTokenAnnotations(entity, position, tokenRadius, stackCount, highlighted);
    }

    private void DrawTokenFrame(RenderedEntity entity, Vector2 position, float tokenRadius, bool highlighted)
    {
        DrawCircle(
            position,
            tokenRadius,
            new Color(UiTokens.Ink.R, UiTokens.Ink.G, UiTokens.Ink.B, highlighted ? 0.62f : 0.40f));
        DrawArc(
            position,
            tokenRadius,
            0f,
            Mathf.Tau,
            28,
            new Color(entity.CultureAccent, highlighted ? 0.88f : 0.68f),
            highlighted ? 2.3f : 1.5f,
            antialiased: true);

        if (entity.CultureSlotCount > 0)
        {
            DrawSlotPips(entity, position, tokenRadius + (entity.GlyphSize * 0.18f));
        }
    }

    private void DrawRecentAction(RenderedEntity entity, Vector2 position, float tokenRadius)
    {
        var actionIntensity = RecentActionIntensity(entity.LastActionTick);
        if (actionIntensity <= 0f)
        {
            return;
        }

        var actionRadius = tokenRadius + (entity.GlyphSize * 0.32f);
        DrawArc(
            position,
            actionRadius,
            -Mathf.Pi / 2f,
            Mathf.Tau * 0.72f,
            20,
            new Color(UiTokens.Gilt, 0.16f + (actionIntensity * 0.34f)),
            1.8f,
            antialiased: true);
    }

    private void DrawEntityGlyph(RenderedEntity entity, Vector2 position, bool highlighted)
    {
        if (entity.Texture is { } texture)
        {
            var diameter = entity.GlyphSize * (highlighted ? 6.1f : 4.8f);
            DrawTextureRect(
                texture,
                new Rect2(position - new Vector2(diameter * 0.5f, diameter * 0.5f), new Vector2(diameter, diameter)),
                tile: false);
        }
        else
        {
            KindGlyph.Draw(this, entity.KindStableName, position, highlighted ? entity.GlyphSize * 1.28f : entity.GlyphSize, entity.Accent);
        }
    }

    private void DrawTokenAnnotations(
        RenderedEntity entity,
        Vector2 position,
        float tokenRadius,
        int stackCount,
        bool highlighted)
    {
        if (stackCount > 1)
        {
            DrawStackCount(position, tokenRadius, stackCount);
        }

        if (EffectiveShowEntityNames || highlighted)
        {
            DrawEntityName(entity.Name, position, tokenRadius, stackCount);
        }
    }

    private void DrawSlotPips(RenderedEntity entity, Vector2 position, float slotRadius)
    {
        var pipCount = Math.Min(entity.CultureSlotCount, 4);
        var slotColor = SlotPolarityColor(entity.CultureSlotPolarityCode);
        for (var i = 0; i < pipCount; i++)
        {
            var angle = (-Mathf.Pi / 2f) + ((i - ((pipCount - 1) * 0.5f)) * 0.24f);
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * slotRadius;
            DrawCircle(position + offset, 1.95f, UiTokens.Ink);
            DrawCircle(position + offset, 1.25f, slotColor);
        }
    }

    private void DrawCultureBadge(RenderedEntity entity, Vector2 position, float tokenRadius, bool highlighted)
    {
        var offset = new Vector2(tokenRadius * 0.68f, tokenRadius * 0.70f);
        var badgeRadius = highlighted ? 4.6f : 3.6f;
        DrawCircle(position + offset, badgeRadius + 1.2f, UiTokens.Ink);
        DrawCircle(position + offset, badgeRadius, new Color(entity.CultureAccent, 0.94f));
    }

    private void DrawStackCount(Vector2 position, float tokenRadius, int stackCount)
    {
        var center = position + new Vector2(-tokenRadius * 0.72f, tokenRadius * 0.72f);
        DrawCircle(center, 7.4f, UiTokens.Ink);
        DrawCircle(center, 6.1f, UiTokens.Gilt);
        DrawCenteredText(stackCount.ToString(System.Globalization.CultureInfo.InvariantCulture), center, UiTokens.SizeMicro, UiTokens.Ink);
    }

    private void DrawEntityName(string entityName, Vector2 position, float tokenRadius, int stackCount)
    {
        var text = stackCount > 1 ? $"{entityName} +{stackCount - 1}" : entityName;
        var baseline = position + new Vector2(0f, -tokenRadius - 10f);
        DrawCenteredText(text, baseline, UiTokens.SizeCaption, UiTokens.Vellum);
    }

    private void DrawCenteredText(string text, Vector2 center, int fontSize, Color color)
    {
        var size = UiTheme.BodyFont.GetStringSize(text, HorizontalAlignment.Left, -1f, fontSize);
        var baseline = center + new Vector2(-size.X * 0.5f, size.Y * 0.35f);
        DrawString(UiTheme.BodyFont, baseline, text, HorizontalAlignment.Left, -1f, fontSize, color);
    }

    private int PickEntity(Vector2 localPosition)
    {
        return StacksExpanded ? PickExpandedEntity(localPosition) : PickCollapsedEntity(localPosition);
    }

    private int PickExpandedEntity(Vector2 localPosition)
    {
        RenderedEntity? closest = null;
        var closestDistance = float.MaxValue;
        foreach (var entity in _renderedEntities.Values)
        {
            var radius = entity.GlyphSize * 3.2f;
            var distance = entity.Position.DistanceSquaredTo(localPosition);
            if (distance <= radius * radius && distance < closestDistance)
            {
                closest = entity;
                closestDistance = distance;
            }
        }

        return closest?.EntityId ?? 0;
    }

    private int PickCollapsedEntity(Vector2 localPosition)
    {
        foreach (var coord in _orderedHexes)
        {
            if (!_entityIdsByHex.TryGetValue(coord, out var entityIds) || entityIds.Count == 0)
            {
                continue;
            }

            var representative = Representative(entityIds);
            var radius = (representative?.GlyphSize ?? 10f) * 3.2f;
            if (HexToPixel(coord).DistanceSquaredTo(localPosition) <= radius * radius)
            {
                return PickFromStack(coord, entityIds, representative);
            }
        }

        return 0;
    }

    private int PickFromStack(HexCoord coord, List<int> entityIds, RenderedEntity? representative)
    {
        var now = (long)Time.GetTicksMsec();
        var representativeIndex = representative is null
            ? 0
            : FindEntityIndex(entityIds, representative.EntityId);
        if (_lastPickedHex == coord && now - _lastPickedAtMs <= StackCycleWindowMs)
        {
            _lastPickedIndex = (_lastPickedIndex + 1) % entityIds.Count;
        }
        else
        {
            _lastPickedIndex = Math.Max(0, representativeIndex);
        }

        _lastPickedHex = coord;
        _lastPickedAtMs = now;
        return entityIds[_lastPickedIndex];
    }

    private static int FindEntityIndex(List<int> entityIds, int entityId)
    {
        for (var index = 0; index < entityIds.Count; index++)
        {
            if (entityIds[index] == entityId)
            {
                return index;
            }
        }

        return 0;
    }

    private Rect2 VisibleLocalRect()
    {
        var viewport = GetViewport().GetVisibleRect();
        var inverse = GetCanvasTransform().AffineInverse();
        var topLeft = inverse * viewport.Position;
        var topRight = inverse * new Vector2(viewport.End.X, viewport.Position.Y);
        var bottomLeft = inverse * new Vector2(viewport.Position.X, viewport.End.Y);
        var bottomRight = inverse * viewport.End;
        var min = new Vector2(
            Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X)),
            Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y)));
        var max = new Vector2(
            Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X)),
            Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y)));
        return new Rect2(min, max - min).Grow(HexSize * 2f);
    }

    private bool EntityPassesVisibleFilter(UiEntityProjection entity) =>
        _visibleEntityIds is null || _visibleEntityIds.Contains(entity.EntityId);

    private void ClearRenderedEntities()
    {
        _renderedEntities.Clear();
        _entityIdsByHex.Clear();
        _orderedHexes.Clear();
        _latestActionTick = -1;
        SetProcess(false);
        QueueRedraw();
    }

    private HexCoord EntityCoordForCurrentPlane(UiEntityProjection entity) =>
        CurrentPlane == PlaneMode.Physical
            ? new HexCoord(entity.PhysicalQ, entity.PhysicalR)
            : new HexCoord(entity.SemanticQ, entity.SemanticR);

    private Vector2 HexToPixel(HexCoord coord)
    {
        var x = HexSize * (Mathf.Sqrt(3f) * coord.Q + Mathf.Sqrt(3f) / 2f * coord.R);
        var y = HexSize * (1.5f * coord.R);
        return new Vector2(x, y);
    }

    private static Vector2 FanOffset(int index, float radius)
    {
        if (index == 0)
        {
            return Vector2.Zero;
        }

        var ring = ((index - 1) / 6) + 1;
        var spoke = (index - 1) % 6;
        var angle = Mathf.DegToRad(-60f + (60f * spoke));
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * ring;
    }

    private static int RecentActionTick(UiEntityProjection entity)
    {
        var tick = -1;
        if (entity.LastMove is { } move)
        {
            tick = Math.Max(tick, move.Tick);
        }

        if (entity.LastGambitExecution is { } gambit)
        {
            tick = Math.Max(tick, gambit.Tick);
        }

        if (entity.CreatedByGeneratorTick is { } createdTick)
        {
            tick = Math.Max(tick, createdTick);
        }

        return tick;
    }

    private bool IsRecentAction(int lastActionTick) =>
        lastActionTick >= 0 && _currentTick - lastActionTick is >= 0 and <= RecentActionTicks;

    private float RecentActionIntensity(int lastActionTick)
    {
        if (!IsRecentAction(lastActionTick))
        {
            return 0f;
        }

        return 1f - ((_currentTick - lastActionTick) / (float)RecentActionTicks);
    }

    private static int TierPriority(EntityTier tier) => tier switch
    {
        EntityTier.Realm => 4,
        EntityTier.Place => 3,
        EntityTier.Life => 2,
        _ => 1,
    };

    private static Color SlotPolarityColor(int polarityCode) => polarityCode switch
    {
        RelationshipPolarityCodes.Positive => UiTokens.Aurora,
        RelationshipPolarityCodes.Negative => UiTokens.Blood,
        RelationshipPolarityCodes.Neutral => UiTokens.Gilt,
        _ => UiTokens.Parchment,
    };

    private int PlaneCodeForCurrentPlane() =>
        CurrentPlane == PlaneMode.Physical ? PlaneCodes.Physical : PlaneCodes.Semantic;

    private sealed class RenderedEntity(int entityId, HexCoord coord, Vector2 position)
    {
        public int EntityId { get; } = entityId;
        public HexCoord Coord { get; set; } = coord;
        public Vector2 Position { get; set; } = position;
        public Vector2 MotionFrom { get; set; } = position;
        public Vector2 MotionTarget { get; set; } = position;
        public float MotionElapsed { get; set; }
        public bool IsMoving { get; set; }
        public string KindStableName { get; set; } = string.Empty;
        public string? SubtypeStableName { get; set; }
        public string Name { get; set; } = string.Empty;
        public Color Accent { get; set; } = Colors.White;
        public Color CultureAccent { get; set; } = UiTokens.CultureNone;
        public float GlyphSize { get; set; } = 10f;
        public Texture2D? Texture { get; set; }
        public int CultureSlotCount { get; set; }
        public int CultureSlotPolarityCode { get; set; }
        public EntityTier Tier { get; set; }
        public int LastActionTick { get; set; } = -1;
    }
}
