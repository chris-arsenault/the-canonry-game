namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class PrimitiveTableTests
{
    [Fact]
    public void FloatTable_FullTableReadMissReturnsDefaultWhileInsertFails()
    {
        var table = new PrimitiveFloatTable(2);
        table.Set(1, 1, 1f);
        table.Set(2, 2, 2f);

        Assert.Equal(1f, table.GetOrDefault(1, 1));
        Assert.False(table.TryGet(3, 3, out var missing));
        Assert.Equal(0f, missing);
        Assert.Equal(0f, table.GetOrDefault(3, 3));
        Assert.Throws<InvalidOperationException>(() => table.Set(3, 3, 3f));
        Assert.Throws<InvalidOperationException>(() => table.Add(3, 3, 3f));
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
        var floats = new PrimitiveFloatTable(3);
        var ints = new PrimitiveIntTable(3);

        for (var key = 1; key <= 3; key++)
        {
            floats.Set(key, key, key);
            ints.Set(key, key, key);
        }

        Assert.Equal(3f, floats.GetOrDefault(3, 3));
        Assert.Equal(3, ints.GetOrDefault(3, 3));
    }
}
