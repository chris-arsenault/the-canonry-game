#pragma warning disable CA1303
#pragma warning disable CA1305
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using TheCanonryGame.Game.Cards;
using TheCanonryGame.Game.Tableau;
using TheCanonryGame.UI.Theme;

using UiCard = TheCanonryGame.UI.Card;

namespace TheCanonryGame.UI;

public partial class TableauPanel : PanelContainer
{
    private readonly ViewSync<CardRuntimeId, UiCard> _cardSync = new();
    private readonly Dictionary<CardRuntimeId, CardInstance> _cardsById = [];
    private readonly Dictionary<CardRuntimeId, int> _lastFireCounts = [];
    private GameRuntimeTableau? _tableau;
    private VBoxContainer? _rows;
    private Label? _summaryLabel;
    private Label? _recentLabel;
    private PackedScene? _cardScene;

    public void Initialize(GameRuntimeTableau? tableau)
    {
        _tableau = tableau;
        Refresh();
    }

    public override void _Ready()
    {
        _cardScene = GD.Load<PackedScene>("res://scenes/ui/Card.tscn");

        CustomMinimumSize = new Vector2(820, 560);
        AddThemeStyleboxOverride("panel", UiTheme.FloatingPanelStyle());
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;

        GetNode<VBoxContainer>("Outer").AddThemeConstantOverride("separation", UiTokens.Space3);

        var title = GetNode<Label>("Outer/TitleLabel");
        title.AddThemeFontOverride("font", UiTheme.DisplayFont);
        title.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleLarge);
        title.AddThemeColorOverride("font_color", UiTokens.Vellum);

        _summaryLabel = GetNode<Label>("Outer/SummaryLabel");
        StyleBodyLabel(_summaryLabel);

        GetNode<ColorRect>("Outer/TopHairline").Color = UiTokens.Hairline;
        GetNode<ColorRect>("Outer/BottomHairline").Color = UiTokens.Hairline;

        _rows = GetNode<VBoxContainer>("Outer/Scroll/Rows");
        _rows.AddThemeConstantOverride("separation", UiTokens.Space2);

        _recentLabel = GetNode<Label>("Outer/RecentLabel");
        StyleBodyLabel(_recentLabel);
        _recentLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        Refresh();
    }

    public void Refresh()
    {
        if (_rows is null || _summaryLabel is null || _recentLabel is null)
        {
            return;
        }

        if (_tableau is null)
        {
            _summaryLabel.Text = string.Empty;
            _recentLabel.Text = string.Empty;
            _cardsById.Clear();
            _lastFireCounts.Clear();
            _cardSync.Clear();
            return;
        }

        var cards = _tableau.Cards;
        _summaryLabel.Text =
            $"Enabled {_tableau.EnabledCardCount}/{cards.Count}  |  Fired {_tableau.TotalGeneratorFires}";
        _recentLabel.Text = RecentFireText();

        _cardsById.Clear();
        var snapshot = cards
            .OrderBy(card => card.Definition.Name, StringComparer.Ordinal)
            .Select(card =>
            {
                _cardsById[card.Definition.RuntimeId] = card;
                return KeyValuePair.Create(
                    card.Definition.RuntimeId,
                    new CardData(card, _tableau.GetCardState(card)));
            })
            .ToArray();

        _cardSync.Sync(
            _rows,
            snapshot,
            CreateCardNode,
            UpdateCardNode);

        foreach (var removedId in _lastFireCounts.Keys.Where(id => !_cardsById.ContainsKey(id)).ToArray())
        {
            _lastFireCounts.Remove(removedId);
        }
    }

    private UiCard CreateCardNode(CardData data)
    {
        _ = data;
        var card = _cardScene?.Instantiate<UiCard>()
            ?? throw new InvalidOperationException("Card scene is not loaded.");
        card.ToggleRequested += OnCardToggleRequested;
        return card;
    }

    private void UpdateCardNode(UiCard node, CardData data)
    {
        var id = data.Card.Definition.RuntimeId;
        node.BindTo(data.Card, data.State);
        if (_lastFireCounts.TryGetValue(id, out var previous) && data.State.TotalFired > previous)
        {
            node.Flash();
        }

        _lastFireCounts[id] = data.State.TotalFired;
    }

    private void OnCardToggleRequested(int cardRuntimeIdValue)
    {
        var id = new CardRuntimeId(cardRuntimeIdValue);
        if (_cardsById.TryGetValue(id, out var card) && _tableau?.RequestToggle(card) == true)
        {
            Refresh();
        }
    }

    private string RecentFireText()
    {
        if (_tableau is null || _tableau.TotalGeneratorFires == 0)
        {
            return "Recent fires: none";
        }

        var recent = _tableau.RecentGeneratorFires(5)
            .Select(fire => $"{fire.GeneratorName} @ t{fire.Tick}");
        return "Recent fires: " + string.Join("  |  ", recent);
    }

    private static void StyleBodyLabel(Label label)
    {
        label.AddThemeFontOverride("font", UiTheme.BodyFont);
        label.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        label.AddThemeColorOverride("font_color", UiTokens.Parchment);
    }

    private sealed record CardData(CardInstance Card, GameTableauCardState State);
}
