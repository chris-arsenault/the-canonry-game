#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

public partial class EntityLayer : Node2D
{
    private readonly ViewSync<int, Entity> _entities = new();
    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private PackedScene? _entityScene;

    [Signal]
    public delegate void EntitySelectedEventHandler(int entityIdValue);

    public float HexSize { get; set; } = 30f;

    public PlaneMode CurrentPlane { get; private set; } = PlaneMode.Physical;

    public bool ShowEntityNames { get; private set; } = true;

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

    public void Refresh()
    {
        if (_projection is null)
        {
            _entities.Clear();
            return;
        }

        var snapshot = BuildSnapshot();
        _entities.Sync(
            this,
            snapshot,
            CreateEntityNode,
            UpdateEntityNode);
    }

    private KeyValuePair<int, EntityLayerItem>[] BuildSnapshot()
    {
        var offsetsByHex = new Dictionary<HexCoord, int>();
        return _projection!.Entities.Values
            .Where(static entity => entity.StatusCode == EntityStatusCodes.Active)
            .OrderBy(entity => entity.EntityId)
            .Select(entity =>
            {
                var coord = EntityCoordForCurrentPlane(entity);
                if (!offsetsByHex.TryGetValue(coord, out var index))
                {
                    index = 0;
                }

                var worldPosition = HexToPixel(coord) + FanOffset(index, HexSize * 0.38f);
                offsetsByHex[coord] = index + 1;
                return KeyValuePair.Create(
                    entity.EntityId,
                    new EntityLayerItem(entity, worldPosition, ShowEntityNames));
            })
            .ToArray();
    }

    private Entity CreateEntityNode(EntityLayerItem data)
    {
        _ = data;
        var entity = _entityScene?.Instantiate<Entity>()
            ?? throw new InvalidOperationException("Entity scene is not loaded.");
        entity.Selected += OnEntitySelected;
        return entity;
    }

    private void UpdateEntityNode(Entity node, EntityLayerItem data)
    {
        node.BindTo(data.Entity, _symbols, data.ShowName);
        node.MoveTo(data.WorldPosition);
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

    private sealed record EntityLayerItem(UiEntityProjection Entity, Vector2 WorldPosition, bool ShowName);
}
