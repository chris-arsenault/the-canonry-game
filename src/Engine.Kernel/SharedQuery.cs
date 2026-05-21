namespace Engine.Kernel;

internal delegate int QueryRecentActionCounter(int actorId, int actionId, int sinceTick);
internal delegate int QueryEntityWorkTierReader(int entityId);

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
        QueryRecentActionCounter? recentActionCounter = null,
        QueryEntityWorkTierReader? entityWorkTierReader = null)
    {
        ActorId = actorId;
        TargetId = targetId;
        Tick = tick;
        ActionId = actionId;
        TargetOriginQ = targetOriginQ;
        TargetOriginR = targetOriginR;
        RefEntityIds = refEntityIds;
        RefCount = refCount;
        RecentActionCounter = recentActionCounter;
        EntityWorkTierReader = entityWorkTierReader;
    }

    public int ActorId { get; }

    public int TargetId { get; }

    public int Tick { get; }

    public int ActionId { get; }

    public int TargetOriginQ { get; }

    public int TargetOriginR { get; }

    public int[]? RefEntityIds { get; }

    public int RefCount { get; }

    public QueryRecentActionCounter? RecentActionCounter { get; }

    public QueryEntityWorkTierReader? EntityWorkTierReader { get; }

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

    public int RecentActionCount(int sinceTick) =>
        RecentActionCounter is null ? 0 : RecentActionCounter(ActorId, ActionId, sinceTick);

    public int EntityWorkTier(int entityId) =>
        EntityWorkTierReader is null ? 0 : EntityWorkTierReader(entityId);
}

internal readonly struct QueryPredicate
{
    public QueryPredicate(int code, int a = 0, int b = 0, int c = 0, int d = 0, float x = 0f, float y = 0f)
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

    public float X { get; }

    public float Y { get; }
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
        int withoutIncomingCount = 0)
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
