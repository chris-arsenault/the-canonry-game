namespace IceRemembers;

using Engine.Kinds;

public static class IceRemembersKinds
{
    public static readonly KindId Region = KindId.Register("region");
    public static readonly KindId Settlement = KindId.Register("settlement");
    public static readonly KindId Faction = KindId.Register("faction");
    public static readonly KindId Penguin = KindId.Register("penguin");
    public static readonly KindId Artifact = KindId.Register("artifact");
    public static readonly KindId Orca = KindId.Register("orca");
    public static readonly KindId PointOfInterest = KindId.Register("point_of_interest");
    public static readonly KindId Ability = KindId.Register("ability");
    public static readonly KindId Monster = KindId.Register("monster");
    public static readonly KindId Transport = KindId.Register("transport");
    public static readonly KindId Occurrence = KindId.Register("occurrence");

    public static IReadOnlyList<KindId> All { get; } =
    [
        Region,
        Settlement,
        Faction,
        Penguin,
        Artifact,
        Orca,
        PointOfInterest,
        Ability,
        Monster,
        Transport,
        Occurrence,
    ];
}
