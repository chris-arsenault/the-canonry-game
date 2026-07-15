#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

public partial class EntityLayer : Node2D
{
    private readonly Dictionary<int, Entity> _entityNodes = [];
    private readonly Dictionary<int, HexCoord> _renderedCoordsByEntity = [];
    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private HashSet<int>? _visibleEntityIds;
    private PackedScene? _entityScene;
    private int? _highlightedEntityId;
    private bool _showEntityNamesForZoom;

    [Signal]
    public delegate void EntitySelectedEventHandler(int entityIdValue);

    public float HexSize { get; set; } = 30f;

    public PlaneMode CurrentPlane { get; private set; } = PlaneMode.Physical;

    public int CurrentPlaneCode => PlaneCodeForCurrentPlane();

    public bool ShowEntityNames { get; private set; }

    public bool EffectiveShowEntityNames => ShowEntityNames || _showEntityNamesForZoom;

    public int? HighlightedEntityId => _highlightedEntityId;

    public override void _Ready()
    {
        _entityScene = GD.Load<PackedScene>("res://scenes/ui/Entity.tscn");
    }

    public void Initialize(UiProjectionState? projection, IProjectionSymbolCatalog? symbols, float hexSize)
    {
        _projection = projection;
        _symbols = symbols;
        HexSize = hexSize;
        Refresh();
    }

    public bool SetPlane(PlaneMode plane)
    {
        if (CurrentPlane == plane)
        {
            return false;
        }

        CurrentPlane = plane;
        return true;
    }

    public bool SetShowEntityNames(bool showEntityNames)
    {
        if (ShowEntityNames == showEntityNames)
        {
            return false;
        }

        ShowEntityNames = showEntityNames;
        return true;
    }

    public bool SetShowEntityNamesForZoom(bool showEntityNamesForZoom)
    {
        if (_showEntityNamesForZoom == showEntityNamesForZoom)
        {
            return false;
        }

        _showEntityNamesForZoom = showEntityNamesForZoom;
        return true;
    }

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

        var previous = _highlightedEntityId;
        _highlightedEntityId = entityId;
        RefreshEntityAppearance(previous);
        RefreshEntityAppearance(_highlightedEntityId);
        return true;
    }

    /// <summary>Rendered node position in this layer's local space (aura overlays).</summary>
    public bool TryGetEntityLocalPosition(int entityId, out Vector2 localPosition)
    {
        if (_entityNodes.TryGetValue(entityId, out var node))
        {
            localPosition = node.Position;
            return true;
        }

        localPosition = default;
        return false;
    }

    public bool TryGetEntityWorldPosition(int entityId, out Vector2 worldPosition)
    {
        if (_entityNodes.TryGetValue(entityId, out var node))
        {
            worldPosition = node.GlobalPosition;
            return true;
        }

        if (_projection?.Entities.TryGetValue(entityId, out var entity) == true &&
            entity.StatusCode == EntityStatusCodes.Active)
        {
            worldPosition = HexToPixel(EntityCoordForCurrentPlane(entity));
            return true;
        }

        worldPosition = default;
        return false;
    }

    public void Refresh()
    {
        if (_projection is null)
        {
            ClearEntityNodes();
            return;
        }

        var currentIds = new HashSet<int>();
        var offsetsByHex = new Dictionary<HexCoord, int>();
        foreach (var entity in ActiveEntitiesInRenderOrder())
        {
            var coord = EntityCoordForCurrentPlane(entity);
            if (!offsetsByHex.TryGetValue(coord, out var index))
            {
                index = 0;
            }

            currentIds.Add(entity.EntityId);
            UpdateEntity(entity, coord, index);
            offsetsByHex[coord] = index + 1;
        }

        foreach (var entityId in _entityNodes.Keys.Where(entityId => !currentIds.Contains(entityId)).ToArray())
        {
            RemoveEntity(entityId);
        }
    }

    public void RefreshDirty(IReadOnlyCollection<HexCoord> dirtyHexes)
    {
        ArgumentNullException.ThrowIfNull(dirtyHexes);

        if (_projection is null)
        {
            ClearEntityNodes();
            return;
        }

        if (dirtyHexes.Count == 0)
        {
            return;
        }

        var dirty = dirtyHexes.ToHashSet();
        var currentIdsInDirtyHexes = new HashSet<int>();
        foreach (var coord in dirty.OrderBy(coord => coord.Q).ThenBy(coord => coord.R))
        {
            var index = 0;
            foreach (var entityId in _projection.ActiveOccupants(PlaneCodeForCurrentPlane(), coord).Order())
            {
                if (!_projection.Entities.TryGetValue(entityId, out var entity) ||
                    entity.StatusCode != EntityStatusCodes.Active ||
                    !EntityPassesVisibleFilter(entity))
                {
                    continue;
                }

                currentIdsInDirtyHexes.Add(entityId);
                UpdateEntity(entity, coord, index);
                index++;
            }
        }

        foreach (var (entityId, coord) in _renderedCoordsByEntity.ToArray())
        {
            if (dirty.Contains(coord) && !currentIdsInDirtyHexes.Contains(entityId))
            {
                RemoveEntity(entityId);
            }
        }
    }

    private IEnumerable<UiEntityProjection> ActiveEntitiesInRenderOrder() =>
        _projection!.Entities.Values
            .Where(entity => entity.StatusCode == EntityStatusCodes.Active && EntityPassesVisibleFilter(entity))
            .OrderBy(entity => entity.EntityId);

    private void UpdateEntity(UiEntityProjection entity, HexCoord coord, int offsetIndex)
    {
        var worldPosition = HexToPixel(coord) + FanOffset(offsetIndex, HexSize * 0.38f);
        if (!_entityNodes.TryGetValue(entity.EntityId, out var node))
        {
            node = _entityScene?.Instantiate<Entity>()
                ?? throw new InvalidOperationException("Entity scene is not loaded.");
            node.Selected += OnEntitySelected;
            AddChild(node);
            _entityNodes[entity.EntityId] = node;
        }

        _renderedCoordsByEntity[entity.EntityId] = coord;
        node.BindTo(entity, _symbols, EffectiveShowEntityNames, _projection?.CurrentTick ?? 0, entity.EntityId == _highlightedEntityId);
        node.ZIndex = entity.EntityId == _highlightedEntityId ? 20 : 0;
        node.MoveTo(worldPosition);
    }

    private bool EntityPassesVisibleFilter(UiEntityProjection entity) =>
        _visibleEntityIds is null || _visibleEntityIds.Contains(entity.EntityId);

    private void RefreshEntityAppearance(int? entityId)
    {
        if (entityId is not { } id ||
            _projection is null ||
            !_projection.Entities.TryGetValue(id, out var entity) ||
            !_entityNodes.TryGetValue(id, out var node))
        {
            return;
        }

        node.BindTo(entity, _symbols, EffectiveShowEntityNames, _projection.CurrentTick, id == _highlightedEntityId);
        node.ZIndex = id == _highlightedEntityId ? 20 : 0;
    }

    private void RemoveEntity(int entityId)
    {
        _renderedCoordsByEntity.Remove(entityId);
        if (_entityNodes.Remove(entityId, out var node))
        {
            node.QueueFree();
        }
    }

    private void ClearEntityNodes()
    {
        foreach (var node in _entityNodes.Values)
        {
            node.QueueFree();
        }

        _entityNodes.Clear();
        _renderedCoordsByEntity.Clear();
    }

    private void OnEntitySelected(int entityIdValue) =>
        EmitSignal(SignalName.EntitySelected, entityIdValue);

    private HexCoord EntityCoordForCurrentPlane(UiEntityProjection entity)
    {
        return CurrentPlane == PlaneMode.Physical
            ? new HexCoord(entity.PhysicalQ, entity.PhysicalR)
            : new HexCoord(entity.SemanticQ, entity.SemanticR);
    }

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

        var angle = Mathf.DegToRad(-60f + 60f * (index - 1));
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private int PlaneCodeForCurrentPlane() =>
        CurrentPlane == PlaneMode.Physical ? PlaneCodes.Physical : PlaneCodes.Semantic;
}
