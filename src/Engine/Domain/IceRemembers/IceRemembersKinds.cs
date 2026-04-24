namespace Engine.Domain.IceRemembers;

using Engine.Kinds;

public static class IceRemembersKinds
{
    public static readonly KindId Region = KindId.Register("region");
    public static readonly KindId Settlement = KindId.Register("settlement");
    public static readonly KindId Faction = KindId.Register("faction");
    public static readonly KindId Penguin = KindId.Register("penguin");
    public static readonly KindId Artifact = KindId.Register("artifact");

    public static IReadOnlyList<KindId> All { get; } =
    [
        Region,
        Settlement,
        Faction,
        Penguin,
        Artifact,
    ];
}
