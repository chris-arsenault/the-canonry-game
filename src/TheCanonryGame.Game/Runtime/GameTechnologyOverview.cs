namespace TheCanonryGame.Game.Runtime;

using Engine.Gameplay;
using Engine.Host;
using Engine.Projection;

using IceRemembers;

internal sealed record GameTechnologyOverview(
    long Version,
    IReadOnlyList<GameCultureTechnologyOverview> Cultures);

internal sealed record GameCultureTechnologyOverview(
    int CultureId,
    string StableName,
    string DisplayName,
    int ClaimedTechs,
    int MaxTier,
    IReadOnlyList<GameClaimedTechnology> Technologies,
    IReadOnlyList<GameTechnologyBonus> Bonuses);

internal sealed record GameClaimedTechnology(
    string TechId,
    string Name,
    int Tier,
    string Discipline,
    string GrantDescription);

internal sealed record GameTechnologyBonus(
    string Label,
    float Multiplier);

internal sealed class GameTechnologyOverviewReadModel
{
    private static readonly Dictionary<string, KnowledgeTech> TechsById =
        IceRemembersKnowledge.Techs.ToDictionary(static tech => tech.Id, StringComparer.Ordinal);

    private static readonly Dictionary<KnowledgeGrantDomain, string> BonusSuffixes = new()
    {
        [KnowledgeGrantDomain.CultureProduction] = "culture production",
        [KnowledgeGrantDomain.CulturePersistence] = "culture decay",
        [KnowledgeGrantDomain.VerbWeight] = "activity weight",
        [KnowledgeGrantDomain.GeneratorFireWeight] = "event weight",
        [KnowledgeGrantDomain.GambitScore] = "score",
        [KnowledgeGrantDomain.GambitSuccess] = "success chance",
        [KnowledgeGrantDomain.MinorActorWork] = "labour effectiveness",
        [KnowledgeGrantDomain.MovementStepRate] = "travel cost",
        [KnowledgeGrantDomain.RoadWork] = "road labour",
        [KnowledgeGrantDomain.RelationshipMaintenance] = "maintenance",
        [KnowledgeGrantDomain.CarryingCapacity] = "carrying capacity",
        [KnowledgeGrantDomain.Attraction] = "attraction",
        [KnowledgeGrantDomain.DecayTideResistance] = "excess decay tide",
    };

    private readonly KernelUiRuntime _runtime;
    private readonly IProjectionSymbolCatalog _symbols;
    private GameTechnologyOverview? _current;

    internal GameTechnologyOverviewReadModel(
        KernelUiRuntime runtime,
        IProjectionSymbolCatalog symbols)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(symbols);
        _runtime = runtime;
        _symbols = symbols;
    }

    internal GameTechnologyOverview Current()
    {
        var snapshot = _runtime.KnowledgeOverview();
        if (_current is null || _current.Version != snapshot.Revision)
        {
            _current = Build(snapshot, _symbols);
        }

        return _current;
    }

    internal static GameTechnologyOverview Build(
        KnowledgeOverviewSnapshot snapshot,
        IProjectionSymbolCatalog symbols)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(symbols);

        var cultures = snapshot.Cultures
            .Select(culture => BuildCulture(culture, symbols))
            .OrderBy(static culture => culture.CultureId)
            .ToArray();
        return new GameTechnologyOverview(snapshot.Revision, cultures);
    }

    private static GameCultureTechnologyOverview BuildCulture(
        KnowledgeCultureSnapshot culture,
        IProjectionSymbolCatalog symbols)
    {
        var stableName = $"culture_{culture.CultureId}";
        var displayName = $"Culture {culture.CultureId}";
        if (symbols.TryResolve(ProjectionSymbolKind.Culture, culture.CultureId, out var symbol))
        {
            stableName = symbol.StableName;
            displayName = symbol.DisplayName;
        }

        var technologies = culture.Claims
            .Select(BuildTechnology)
            .OrderBy(static tech => tech.Tier)
            .ThenBy(static tech => tech.Discipline, StringComparer.Ordinal)
            .ThenBy(static tech => tech.Name, StringComparer.Ordinal)
            .ToArray();
        var bonuses = culture.Modifiers
            .Select(modifier => new GameTechnologyBonus(
                BonusLabel(modifier, symbols),
                modifier.Multiplier))
            .OrderBy(static bonus => bonus.Label, StringComparer.Ordinal)
            .ToArray();

        return new GameCultureTechnologyOverview(
            culture.CultureId,
            stableName,
            displayName,
            culture.ClaimedTechs,
            culture.MaxTier,
            technologies,
            bonuses);
    }

    private static GameClaimedTechnology BuildTechnology(KnowledgeClaimSnapshot claim)
    {
        if (TechsById.TryGetValue(claim.TechId, out var tech))
        {
            return new GameClaimedTechnology(
                tech.Id,
                tech.Name,
                tech.Tier,
                tech.Discipline.ToString(),
                tech.Grant);
        }

        return new GameClaimedTechnology(
            claim.TechId,
            ProjectionSymbolCatalog.ToDisplayName(claim.TechId),
            claim.Tier,
            "Unknown discipline",
            "No authored grant description is available.");
    }

    private static string BonusLabel(
        KnowledgeModifierSnapshot modifier,
        IProjectionSymbolCatalog symbols)
    {
        if (modifier.Domain == KnowledgeGrantDomain.FrontierThawThreshold)
        {
            return "Thaw frontier threshold";
        }

        if (modifier.Domain == KnowledgeGrantDomain.FrontierReclaimThreshold)
        {
            return "Refreeze frontier threshold";
        }

        return BonusSuffixes.TryGetValue(modifier.Domain, out var suffix)
            ? $"{TargetLabel(modifier, symbols)} {suffix}"
            : ProjectionSymbolCatalog.ToDisplayName(modifier.Domain.ToString());
    }

    private static string TargetLabel(
        KnowledgeModifierSnapshot modifier,
        IProjectionSymbolCatalog symbols)
    {
        if (modifier.TargetStableName is { Length: > 0 } stableName)
        {
            return NamedTargetLabel(modifier, symbols, stableName);
        }

        if (modifier.TargetId == 0)
        {
            return GlobalTargetLabel(modifier.Domain);
        }

        if (modifier.Domain == KnowledgeGrantDomain.MinorActorWork)
        {
            return MinorActorRoleLabel(modifier.AuthoredTargetCode);
        }

        return PlaneTargetLabel(modifier.AuthoredTargetCode, modifier.TargetId);
    }

    private static string NamedTargetLabel(
        KnowledgeModifierSnapshot modifier,
        IProjectionSymbolCatalog symbols,
        string stableName)
    {
        var symbolKind = TargetSymbolKind(modifier.Domain);
        var fallback = ProjectionSymbolCatalog.ToDisplayName(stableName);
        return symbolKind == ProjectionSymbolKind.None
            ? fallback
            : symbols.ResolveDisplayName(symbolKind, modifier.TargetId, fallback);
    }

    private static ProjectionSymbolKind TargetSymbolKind(KnowledgeGrantDomain domain) => domain switch
    {
        KnowledgeGrantDomain.VerbWeight => ProjectionSymbolKind.Verb,
        KnowledgeGrantDomain.GeneratorFireWeight => ProjectionSymbolKind.Generator,
        KnowledgeGrantDomain.GambitScore or KnowledgeGrantDomain.GambitSuccess => ProjectionSymbolKind.Gambit,
        KnowledgeGrantDomain.RelationshipMaintenance => ProjectionSymbolKind.RelationshipKind,
        KnowledgeGrantDomain.MovementStepRate or KnowledgeGrantDomain.RoadWork => ProjectionSymbolKind.Kind,
        _ => ProjectionSymbolKind.None,
    };

    private static string GlobalTargetLabel(KnowledgeGrantDomain domain) => domain switch
    {
        KnowledgeGrantDomain.MinorActorWork => "All minor-actor",
        KnowledgeGrantDomain.MovementStepRate => "All physical",
        _ => "All",
    };

    private static string MinorActorRoleLabel(int roleCode) => roleCode switch
    {
        MinorActorRoleCodes.CulturalFront => "Cultural front",
        MinorActorRoleCodes.RoadCrew => "Road crew",
        MinorActorRoleCodes.CampaignColumn => "Campaign column",
        _ => $"Role {roleCode}",
    };

    private static string PlaneTargetLabel(int planeCode, int targetId) => planeCode switch
    {
        PlaneCodes.Physical => "Physical",
        PlaneCodes.Semantic => "Semantic",
        _ => $"Target {targetId}",
    };
}
