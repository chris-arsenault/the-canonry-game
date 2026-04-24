namespace Engine.Tests;

using Engine.Kinds;

internal static class TestKinds
{
    public static readonly KindId Region = KindId.Register("region");
    public static readonly KindId Settlement = KindId.Register("settlement");
    public static readonly KindId Faction = KindId.Register("faction");
    public static readonly KindId Penguin = KindId.Register("penguin");
    public static readonly KindId Artifact = KindId.Register("artifact");
    public static readonly KindId Monster = KindId.Register("monster");
}
