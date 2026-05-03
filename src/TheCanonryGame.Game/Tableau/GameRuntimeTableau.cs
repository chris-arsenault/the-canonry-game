namespace TheCanonryGame.Game.Tableau;

using Engine.Projection;

using TheCanonryGame.Game.Cards;

internal sealed record GameTableauCardState(
    CardInstance Card,
    int GeneratorId,
    bool IsKnown,
    bool IsEnabled,
    int TotalFired,
    int? LastFireTick);

internal sealed record GameTableauGeneratorFire(string GeneratorName, int Tick);

internal sealed class GameRuntimeTableau
{
    private readonly TableauManager _tableau = new();
    private readonly UiProjectionState _projection;
    private readonly IProjectionSymbolCatalog _symbols;
    private readonly GameIntentPort _intents;
    private readonly GameDomainKnowledge _knowledge;

    internal GameRuntimeTableau(
        UiProjectionState projection,
        IProjectionSymbolCatalog symbols,
        GameIntentPort intents,
        GameDomainKnowledge? knowledge = null)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(symbols);
        ArgumentNullException.ThrowIfNull(intents);
        _projection = projection;
        _symbols = symbols;
        _intents = intents;
        _knowledge = knowledge ?? GameDomainKnowledge.Empty;
        BuildGeneratorCards();
        BuildGambitCards();
    }

    public IReadOnlyList<CardInstance> Cards => _tableau.ActiveCards;

    public int EnabledCardCount => Cards.Count(card => GetCardState(card).IsEnabled);

    public int TotalGeneratorFires => _projection.GeneratorFires.Count;

    public GameTableauCardState GetCardState(CardInstance card)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (card.Definition.GeneratorId > 0)
        {
            return GetGeneratorCardState(card, card.Definition.GeneratorId);
        }

        if (card.Definition.GambitId > 0)
        {
            return GetGambitCardState(card, card.Definition.GambitId);
        }

        return new GameTableauCardState(card, 0, false, IsEnabled: false, TotalFired: 0, LastFireTick: null);
    }

    private GameTableauCardState GetGeneratorCardState(CardInstance card, int generatorId)
    {
        var isKnown = _projection.Generators.TryGetValue(generatorId, out var generator);
        return new GameTableauCardState(
            card,
            generatorId,
            isKnown,
            !isKnown || generator!.IsEnabled,
            generator?.TotalFired ?? 0,
            generator?.LastFire?.Tick);
    }

    private GameTableauCardState GetGambitCardState(CardInstance card, int gambitId)
    {
        var isKnown = _projection.Gambits.TryGetValue(gambitId, out var gambit);
        return new GameTableauCardState(
            card,
            0,
            isKnown,
            IsEnabled: true,
            gambit?.TotalSucceeded ?? 0,
            gambit?.LastSucceededTick);
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
            _knowledge.GeneratorsById.TryGetValue(symbol.Id, out var capability);
            _tableau.PlaceCard(CreateGeneratorCard(symbol, capability), currentTick: 0);
        }
    }

    private void BuildGambitCards()
    {
        foreach (var symbol in _symbols.Symbols.Values
            .Where(symbol => symbol.Kind == ProjectionSymbolKind.Gambit)
            .OrderBy(symbol => symbol.DisplayName, StringComparer.Ordinal))
        {
            _knowledge.GambitsById.TryGetValue(symbol.Id, out var capability);
            _tableau.PlaceCard(CreateGambitCard(symbol, capability), currentTick: 0);
        }
    }

    private static CardDefinition CreateGeneratorCard(ProjectionSymbol symbol, GameCapabilityKnowledge? capability)
    {
        var card = new CardDefinition(
            $"generator:{symbol.StableName}",
            symbol.DisplayName,
            CardType.Generator,
            generatorId: symbol.Id)
        {
            Description = DescriptionFor(symbol, capability),
            Produces = ProducesFor(capability),
            RelationshipChanges = RelationshipChangesFor(capability),
            TerrainTransforms = TerrainTransformsFor(capability),
            DecisionModifiers = DecisionModifiersFor(capability),
        };
        card.Tags.Add("generator");
        AddKindHintTag(card, GeneratorKindHintFor(symbol, capability));
        return card;
    }

    private static CardDefinition CreateGambitCard(ProjectionSymbol symbol, GameCapabilityKnowledge? capability)
    {
        var card = new CardDefinition(
            $"gambit:{symbol.StableName}",
            symbol.DisplayName,
            CardType.Action,
            gambitId: symbol.Id)
        {
            Description = DescriptionFor(symbol, capability),
            Produces = ProducesFor(capability),
            RelationshipChanges = RelationshipChangesFor(capability),
            TerrainTransforms = TerrainTransformsFor(capability),
            DecisionModifiers = DecisionModifiersFor(capability),
        };
        card.Tags.Add("gambit");
        AddKindHintTag(card, GambitKindHintFor(symbol, capability));
        return card;
    }

    private static string DescriptionFor(ProjectionSymbol symbol, GameCapabilityKnowledge? capability) =>
        capability?.Description ?? symbol.Description ?? string.Empty;

    private static IReadOnlyList<CardProducedEntityDescriptor> ProducesFor(GameCapabilityKnowledge? capability) =>
        capability?.Produces ?? [];

    private static IReadOnlyList<CardRelationshipChangeDescriptor> RelationshipChangesFor(
        GameCapabilityKnowledge? capability) =>
        capability?.RelationshipChanges ?? [];

    private static IReadOnlyList<CardTerrainTransformDescriptor> TerrainTransformsFor(
        GameCapabilityKnowledge? capability) =>
        capability?.TerrainTransforms ?? [];

    private static IReadOnlyList<CardDecisionModifierDescriptor> DecisionModifiersFor(
        GameCapabilityKnowledge? capability) =>
        capability?.DecisionModifiers ?? [];

    private static string? GeneratorKindHintFor(ProjectionSymbol symbol, GameCapabilityKnowledge? capability) =>
        capability?.PrimaryKindStableName ?? GeneratorKindHint(symbol.StableName);

    private static string? GambitKindHintFor(ProjectionSymbol symbol, GameCapabilityKnowledge? capability) =>
        capability?.PrimaryKindStableName ?? GambitKindHint(symbol.StableName);

    private static void AddKindHintTag(CardDefinition card, string? kindHint)
    {
        if (string.IsNullOrEmpty(kindHint))
        {
            return;
        }

        card.Tags.Add(kindHint);
        card.Tags.Add($"kind:{kindHint}");
    }

    private static string? GambitKindHint(string stableName)
    {
        // Gambit ids are actor_kind first: penguin_wields_artifact, faction_declares_*, etc.
        var underscore = stableName.IndexOf('_', StringComparison.Ordinal);
        var head = underscore > 0 ? stableName[..underscore] : stableName;
        return head switch
        {
            "penguin" => "penguin",
            "faction" => "faction",
            "settlement" => "settlement",
            "artifact" => "artifact",
            "region" => "region",
            _ => null,
        };
    }

    private static string? GeneratorKindHint(string stableName) => stableName switch
    {
        "ice_settles_accounts" => "artifact",
        "what_the_spring_remembers" => "region",
        "war_breaks_the_land" => "region",
        "routes_draw_readers" => "settlement",
        "what_the_artifact_knows" => "settlement",
        _ => null,
    };
}
