namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class PrimitiveTableTests
{
    [Fact]
    public void ScalarTable_FullTableReadMissReturnsDefaultWhileInsertFails()
    {
        var table = new PrimitiveScalarTable(2);
        table.Set(1, 1, FixedScalar.One);
        table.Set(2, 2, FixedScalar.FromInt(2));

        Assert.Equal(FixedScalar.One, table.GetOrDefault(1, 1));
        Assert.False(table.TryGet(3, 3, out var missing));
        Assert.Equal(FixedScalar.Zero, missing);
        Assert.Equal(FixedScalar.Zero, table.GetOrDefault(3, 3));
        Assert.Throws<InvalidOperationException>(() => table.Set(3, 3, FixedScalar.FromSingle(3f)));
        Assert.Throws<InvalidOperationException>(() => table.Add(3, 3, FixedScalar.FromSingle(3f)));
    }

    [Fact]
    public void IntTable_FullTableReadMissReturnsDefaultWhileInsertFails()
    {
        var table = new PrimitiveIntTable(2);
        table.Set(1, 1, 1);
        table.Set(2, 2, 2);

        Assert.Equal(1, table.GetOrDefault(1, 1));
        Assert.False(table.TryGet(3, 3, out var missing));
        Assert.Equal(0, missing);
        Assert.Equal(0, table.GetOrDefault(3, 3));
        Assert.Throws<InvalidOperationException>(() => table.Set(3, 3, 3));
        Assert.Throws<InvalidOperationException>(() => table.Add(3, 3, 3));
    }

    [Fact]
    public void TablesSupportNonPowerOfTwoCapacities()
    {
        var scalars = new PrimitiveScalarTable(3);
        var ints = new PrimitiveIntTable(3);

        for (var key = 1; key <= 3; key++)
        {
            scalars.Set(key, key, key);
            ints.Set(key, key, key);
        }

        Assert.Equal(FixedScalar.FromInt(3), scalars.GetOrDefault(3, 3));
        Assert.Equal(3, ints.GetOrDefault(3, 3));
    }
}
