namespace Engine.Kernel;

internal delegate int QueryEntityWorkTierReader(int entityId);

internal delegate FixedScalar QueryTravelCostReader(int fromEntityId, int toEntityId, int planeCode, int tick);

internal delegate bool QueryKnowledgeClaimReader(int cultureId, int techId);

internal readonly struct QueryBinding
{
    public QueryBinding(
        int actorId,
        int targetId,
        int tick = 0,
        int actionId = 0,
        int targetOriginQ = 0,
        int targetOriginR = 0,
        int[]? refEntityIds = null,
        int refCount = 0,
        QueryEntityWorkTierReader? entityWorkTierReader = null,
        QueryTravelCostReader? travelCostReader = null,
        QueryKnowledgeClaimReader? knowledgeClaimReader = null)
    {
        ActorId = actorId;
        TargetId = targetId;
        Tick = tick;
        ActionId = actionId;
        TargetOriginQ = targetOriginQ;
        TargetOriginR = targetOriginR;
        RefEntityIds = refEntityIds;
        RefCount = refCount;
        EntityWorkTierReader = entityWorkTierReader;
        TravelCostReader = travelCostReader;
        KnowledgeClaimReader = knowledgeClaimReader;
    }

    public int ActorId { get; }

    public int TargetId { get; }

    public int Tick { get; }

    public int ActionId { get; }

    public int TargetOriginQ { get; }

    public int TargetOriginR { get; }

    public int[]? RefEntityIds { get; }

    public int RefCount { get; }

    public QueryEntityWorkTierReader? EntityWorkTierReader { get; }

    public QueryTravelCostReader? TravelCostReader { get; }

    public QueryKnowledgeClaimReader? KnowledgeClaimReader { get; }

    public bool ReferenceAlreadyBound(int entityId)
    {
        if (RefEntityIds is null)
        {
            return false;
        }

        for (var i = 0; i < RefCount; i++)
        {
            if (RefEntityIds[i] == entityId)
            {
                return true;
            }
        }

        return false;
    }

    public int EntityWorkTier(int entityId) =>
        EntityWorkTierReader is null ? 0 : EntityWorkTierReader(entityId);

    // No reader means reach cannot be evaluated here; a travel-reach gate
    // conservatively fails rather than passing an unbounded route.
    public FixedScalar TravelCost(int fromEntityId, int toEntityId, int planeCode, int tick) =>
        TravelCostReader is null
            ? FixedScalar.PositiveInfinity
            : TravelCostReader(fromEntityId, toEntityId, planeCode, tick);

    // No reader means claims cannot be evaluated here; an enablement gate
    // conservatively stays locked rather than opening unverified.
    public bool CultureHoldsTech(int cultureId, int techId) =>
        KnowledgeClaimReader is not null && KnowledgeClaimReader(cultureId, techId);
}

internal readonly struct QueryPredicate
{
    public QueryPredicate(int code, int a = 0, int b = 0, int c = 0, int d = 0, FixedScalar x = default, FixedScalar y = default)
    {
        Code = code;
        A = a;
        B = b;
        C = c;
        D = d;
        X = x;
        Y = y;
    }

    public int Code { get; }

    public int A { get; }

    public int B { get; }

    public int C { get; }

    public int D { get; }

    public FixedScalar X { get; }

    public FixedScalar Y { get; }
}

internal readonly struct SharedQuery
{
    public SharedQuery(
        int kindId,
        int[]? subtypeIds = null,
        int subtypeStart = 0,
        int subtypeCount = 0,
        int adjacentToTarget = 0,
        int cultureMatchCode = 0,
        int[]? relationshipKindFilterIds = null,
        int withoutOutgoingStart = 0,
        int withoutOutgoingCount = 0,
        int withoutIncomingStart = 0,
        int withoutIncomingCount = 0,
        int requiredTagBits = 0,
        int withoutTagBits = 0,
        int cultureId = 0,
        int relatedToTargetKindId = 0,
        int relatedToTargetIncoming = 0,
        FixedScalar relatedToTargetMinStrength = default)
    {
        KindId = kindId;
        SubtypeIds = subtypeIds;
        SubtypeStart = subtypeStart;
        SubtypeCount = subtypeCount;
        AdjacentToTarget = adjacentToTarget;
        CultureMatchCode = cultureMatchCode;
        RelationshipKindFilterIds = relationshipKindFilterIds;
        WithoutOutgoingStart = withoutOutgoingStart;
        WithoutOutgoingCount = withoutOutgoingCount;
        WithoutIncomingStart = withoutIncomingStart;
        WithoutIncomingCount = withoutIncomingCount;
        RequiredTagBits = requiredTagBits;
        WithoutTagBits = withoutTagBits;
        CultureId = cultureId;
        RelatedToTargetKindId = relatedToTargetKindId;
        RelatedToTargetIncoming = relatedToTargetIncoming;
        RelatedToTargetMinStrength = relatedToTargetMinStrength;
    }

    public int KindId { get; }

    public int[]? SubtypeIds { get; }

    public int SubtypeStart { get; }

    public int SubtypeCount { get; }

    public int AdjacentToTarget { get; }

    public int CultureMatchCode { get; }

    public int[]? RelationshipKindFilterIds { get; }

    public int WithoutOutgoingStart { get; }

    public int WithoutOutgoingCount { get; }

    public int WithoutIncomingStart { get; }

    public int WithoutIncomingCount { get; }

    public int RequiredTagBits { get; }

    public int WithoutTagBits { get; }

    public int CultureId { get; }

    public int RelatedToTargetKindId { get; }

    public int RelatedToTargetIncoming { get; }

    public FixedScalar RelatedToTargetMinStrength { get; }
}

internal readonly struct QueryEvaluation
{
    public QueryEvaluation(int count, int selectedEntityId)
    {
        Count = count;
        SelectedEntityId = selectedEntityId;
    }

    public int Count { get; }

    public int SelectedEntityId { get; }
}
