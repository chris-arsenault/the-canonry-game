namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Kinds;

using Attr = IceRemembers.IceRemembersAttributeId;
using K = IceRemembers.IceRemembersKinds;
using Res = IceRemembers.IceRemembersResource;

public static class IceRemembersSemanticDefinition
{
    public static SemanticDomainConfig Create() => new()
    {
        RingOrder =
        [
            K.Penguin,
            K.Settlement,
            K.Faction,
            K.Region,
            K.Artifact,
        ],
        Stocks = CreateStocks(),
        Generators = IceRemembersGeneratorDefinition.Create(),
    };

    private static string A(Attr attribute) => attribute.ToDomainName();
    private static string R(Res resource) => resource.ToDomainName();

    private static IReadOnlyList<SemanticStockConfig> CreateStocks() =>
    [
        Stock(K.Artifact, Attr.Condition, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100", "anchor", "Health (integrity). Dies at 0."),
        Stock(K.Artifact, Attr.Power, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Capacity - tangible working stock spent on outward actions."),
        Stock(K.Artifact, Attr.Resonance, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Drive - diffuse resonance; bleeds into the world, accumulates from compromised land, and consecrates settlements."),

        Stock(K.Faction, Attr.Cohesion, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100", "anchor", "Health (integrity). Dies at 0."),
        Stock(K.Faction, Attr.Reach, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Projection - factional reach: symbolic, reputational, political, and logistical."),
        Stock(K.Faction, Attr.Provisions, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Need - stockpile of material resources; deficit drives seeking."),

        Stock(K.Penguin, Attr.Vitality, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100", "anchor", "Health (integrity). Dies at 0."),
        Stock(K.Penguin, Attr.Echo, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Capacity - what others can draw on from the penguin for outward tasks."),
        Stock(K.Penguin, Attr.Warmth, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Need - warmth deficit drives seeking food, shelter, and survivable company."),

        Stock(K.Region, Attr.Stability, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100", "anchor", "Health (integrity). Dies at 0."),
        Stock(K.Region, Attr.Holding, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Capacity - the land's holding: how well the world keeps its cover, thresholds, and protections intact."),
        Stock(K.Region, Attr.Pristiness, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Need - unspoiled quality; low values mark breached, opened, or compromised ground."),

        Stock(K.Settlement, Attr.TempHp, SemanticStockClass.Anchor, SemanticFeedbackClass.Anchor,
            "0..100", "anchor", "Temporary health. Dies at 0."),
        Stock(K.Settlement, Attr.Dominion, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Projection - broad civic and territorial reach: political, cultural, mercantile, military."),
        Stock(K.Settlement, Attr.Shelter, SemanticStockClass.Free, SemanticFeedbackClass.Balancing,
            "0..100", "free", "Need - shelter, warmth, and civic habitability; deficit drives corrective behaviors."),

        ResourceStock(Res.Food, "Ambient hex food abundance."),
        ResourceStock(Res.Wealth, "Ambient hex wealth."),
        ResourceStock(Res.Population, "Ambient hex population."),
    ];

    private static SemanticAttributeStockConfig Stock(
        EntityKind kind,
        Attr attribute,
        SemanticStockClass stockClass,
        SemanticFeedbackClass feedbackClass,
        string boundsLabel,
        string conservationLabel,
        string notes) =>
        new()
        {
            Kind = kind,
            AttributeName = A(attribute),
            StockClass = stockClass,
            FeedbackClass = feedbackClass,
            RequiredFlowCoverage = SemanticFlowCoverageRequirement.None,
            BoundsLabel = boundsLabel,
            ConservationLabel = conservationLabel,
            Notes = notes,
        };

    private static SemanticResourceStockConfig ResourceStock(Res resource, string notes) =>
        new()
        {
            ResourceName = R(resource),
            StockClass = SemanticStockClass.Free,
            FeedbackClass = SemanticFeedbackClass.Unknown,
            RequiredFlowCoverage = SemanticFlowCoverageRequirement.None,
            Notes = notes,
        };
}
