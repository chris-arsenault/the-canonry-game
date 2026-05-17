#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

public partial class EntityCardLayer : CanvasLayer
{
    private const float NavigationOffset = 24f;

    private readonly ViewSync<int, EntityCard> _openCards = new();
    private readonly Dictionary<int, Vector2> _openCardPositions = [];
    private readonly List<int> _openCardOrder = [];

    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private Godot.Theme? _theme;
    private PackedScene? _entityCardScene;

    public long Version { get; private set; }

    public override void _Ready()
    {
        _entityCardScene = GD.Load<PackedScene>("res://scenes/ui/EntityCard.tscn");
    }

    public void Initialize(UiProjectionState? projection, IProjectionSymbolCatalog? symbols, Godot.Theme? theme)
    {
        _projection = projection;
        _symbols = symbols;
        _theme = theme;
        Version++;
        Refresh();
    }

    public void Open(int entityId, Vector2 position)
    {
        if (_projection is null)
        {
            return;
        }

        if (_openCardPositions.ContainsKey(entityId))
        {
            Promote(entityId);
            Version++;
            Refresh();
            return;
        }

        _openCardPositions[entityId] = position;
        _openCardOrder.Add(entityId);
        Version++;
        Refresh();
    }

    public void CloseAll()
    {
        _openCardPositions.Clear();
        _openCardOrder.Clear();
        Version++;
        _openCards.Clear(static card => card.QueueFree());
    }

    public void Refresh()
    {
        if (_entityCardScene is null)
        {
            return;
        }

        var snapshot = _openCardOrder
            .Where(_openCardPositions.ContainsKey)
            .Select(entityId => KeyValuePair.Create(
                entityId,
                new OpenCardData(entityId, _openCardPositions[entityId])))
            .ToArray();

        _openCards.Sync(
            this,
            snapshot,
            CreateEntityCard,
            UpdateEntityCard,
            static card => card.FadeOutAndQueueFree());
    }

    private void Promote(int entityId)
    {
        _openCardOrder.Remove(entityId);
        _openCardOrder.Add(entityId);
    }

    private EntityCard CreateEntityCard(OpenCardData data)
    {
        _ = data;
        var card = _entityCardScene?.Instantiate<EntityCard>()
            ?? throw new InvalidOperationException("EntityCard scene is not loaded.");
        card.Theme = _theme;
        card.CardClosed += OnCardClosed;
        card.EntitySelected += OnCardEntitySelected;
        return card;
    }

    private void OnCardEntitySelected(int entityIdValue)
    {
        if (entityIdValue <= 0)
        {
            return;
        }

        var nextPosition = NextNavigationPosition();
        Open(entityIdValue, nextPosition);
    }

    private Vector2 NextNavigationPosition()
    {
        if (_openCardOrder.Count == 0)
        {
            return new Vector2(NavigationOffset, NavigationOffset);
        }

        var lastId = _openCardOrder[^1];
        if (!_openCardPositions.TryGetValue(lastId, out var anchor))
        {
            return new Vector2(NavigationOffset, NavigationOffset);
        }

        return anchor + new Vector2(NavigationOffset, NavigationOffset);
    }

    private void UpdateEntityCard(EntityCard card, OpenCardData data)
    {
        card.BindTo(_projection, _symbols, data.EntityId, data.Position);
    }

    private void OnCardClosed(EntityCard card)
    {
        _openCardPositions.Remove(card.EntityId);
        _openCardOrder.Remove(card.EntityId);
        Version++;
        Refresh();
    }

    private readonly record struct OpenCardData(int EntityId, Vector2 Position);
}
