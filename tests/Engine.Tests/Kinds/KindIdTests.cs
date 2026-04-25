namespace Engine.Tests.Kinds;

using Engine.Kinds;

public sealed class KindIdTests
{
    [Fact]
    public void RegisteredKindHasPrimitiveId()
    {
        var kind = KindId.Register("kind_id_test_registered");

        Assert.True(kind.Id > 0);
        Assert.Equal(kind, KindId.From("kind_id_test_registered"));
        Assert.Equal(kind.Id, KindId.From("kind_id_test_registered").Id);
    }
}
