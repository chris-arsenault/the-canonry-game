namespace TheCanonryGame.Game.Tableau;

using Engine.Projection;
using TheCanonryGame.Game.Cards;

public sealed record GameTableauCardState(
    CardInstance Card,
    int GeneratorId,
    bool IsKnown,
    bool IsEnabled,
    int TotalFired,
    int? LastFireTick);

public sealed record GameTableauGeneratorFire(string GeneratorName, int Tick);

public sealed class GameRuntimeTableau
{
    private readonly TableauManager _tableau = new(manaCap: 0f);
    private readonly UiProjectionState _projection;
    private readonly IProjectionSymbolCatalog _symbols;
    private readonly GameIntentPort _intents;

    internal GameRuntimeTableau(
        UiProjectionState projection,
        IProjectionSymbolCatalog symbols,
        GameIntentPort intents)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(symbols);
        ArgumentNullException.ThrowIfNull(intents);
        _projection = projection;
        _symbols = symbols;
        _intents = intents;
        BuildGeneratorCards();
    }

    public IReadOnlyList<CardInstance> Cards => _tableau.ActiveCards;

    public float CurrentMana => _tableau.CurrentMana;

    public float ManaCap => _tableau.ManaCap;

    public int EnabledCardCount => Cards.Count(card => GetCardState(card).IsEnabled);

    public int TotalGeneratorFires => _projection.GeneratorFires.Count;

    public GameTableauCardState GetCardState(CardInstance card)
    {
        ArgumentNullException.ThrowIfNull(card);

        var generatorId = card.Definition.GeneratorId;
        if (generatorId <= 0)
        {
            return new GameTableauCardState(card, 0, false, card.IsActive, card.FireCount, card.LastFiredTick);
        }

        var isKnown = _projection.Generators.TryGetValue(generatorId, out var generator);
        return new GameTableauCardState(
            card,
            generatorId,
            isKnown,
            !isKnown || generator!.IsEnabled,
            generator?.TotalFired ?? 0,
            generator?.LastFire?.Tick);
    }

    public bool RequestSetEnabled(CardInstance card, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(card);

        var state = GetCardState(card);
        if (state.GeneratorId <= 0)
        {
            return false;
        }

        return _intents.RequestGeneratorEnabled(state.GeneratorId, enabled);
    }

    public bool RequestToggle(CardInstance card)
    {
        ArgumentNullException.ThrowIfNull(card);

        var state = GetCardState(card);
        return state.GeneratorId > 0 && RequestSetEnabled(card, !state.IsEnabled);
    }

    public IReadOnlyList<GameTableauGeneratorFire> RecentGeneratorFires(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        return _projection.GeneratorFires
            .TakeLast(count)
            .Select(fire => new GameTableauGeneratorFire(
                _symbols.ResolveDisplayName(
                    ProjectionSymbolKind.Generator,
                    fire.GeneratorId,
                    $"Generator {fire.GeneratorId}"),
                fire.Tick))
            .ToArray();
    }

    private void BuildGeneratorCards()
    {
        foreach (var symbol in _symbols.Symbols.Values
            .Where(symbol => symbol.Kind == ProjectionSymbolKind.Generator)
            .OrderBy(symbol => symbol.DisplayName, StringComparer.Ordinal))
        {
            var card = new CardDefinition(
                $"generator:{symbol.StableName}",
                symbol.DisplayName,
                CardType.Generator,
                AlwaysCardCondition.Instance,
                manaCost: 0,
                cooldown: 0,
                cadence: 0,
                lifespan: int.MaxValue,
                generatorId: symbol.Id)
            {
                Description = symbol.Description ?? string.Empty,
            };
            card.Tags.Add("generator");
            _tableau.PlaceCard(card, currentTick: 0);
        }
    }
}
