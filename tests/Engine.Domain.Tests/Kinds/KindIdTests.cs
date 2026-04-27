namespace Engine.Domain.Tests.Kinds;

using Engine.Domain;
using Engine.Kinds;

public sealed class KindIdTests
{
    [Fact]
    public void DomainRegistryAssignsPrimitiveKindId()
    {
        var ids = new DomainIdRegistry();
        var kind = KindId.Register("kind_id_test_registered");
        var runtimeId = ids.RegisterKind(kind);

        Assert.True(runtimeId.Value > 0);
        Assert.Equal(kind, KindId.From("kind_id_test_registered"));
        Assert.Equal(runtimeId, ids.KindIdOf(KindId.From("kind_id_test_registered")));
    }
}
