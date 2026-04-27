namespace TheCanonryGame.UI.Tests;

using TheCanonryGame.UI;

public sealed class ViewSyncCoreTests
{
    [Fact]
    public void SyncAddsUpdatesAndRemovesNodes()
    {
        var sync = new ViewSyncCore<int, FakeNode>();
        var created = new List<FakeNode>();
        var removed = new List<FakeNode>();

        sync.Sync(
            Snapshot((1, "one"), (2, "two")),
            create: data =>
            {
                var node = new FakeNode();
                created.Add(node);
                return node;
            },
            update: static (node, data) => node.Value = data,
            remove: removed.Add);

        var firstNode = sync.Nodes[1];
        var secondNode = sync.Nodes[2];

        sync.Sync(
            Snapshot((2, "two updated"), (3, "three")),
            create: data =>
            {
                var node = new FakeNode();
                created.Add(node);
                return node;
            },
            update: static (node, data) => node.Value = data,
            remove: removed.Add);

        Assert.Equal(3, created.Count);
        Assert.Single(removed);
        Assert.Same(firstNode, removed[0]);
        Assert.Same(secondNode, sync.Nodes[2]);
        Assert.Equal("two updated", secondNode.Value);
        Assert.Equal([secondNode, sync.Nodes[3]], sync.OrderedNodes);
    }

    [Fact]
    public void SyncRejectsDuplicateKeys()
    {
        var sync = new ViewSyncCore<int, FakeNode>();

        var error = Assert.Throws<InvalidOperationException>(() =>
            sync.Sync(
                Snapshot((1, "one"), (1, "again")),
                create: static _ => new FakeNode(),
                update: static (node, data) => node.Value = data,
                remove: static _ => { }));

        Assert.Contains("duplicate key", error.Message, StringComparison.Ordinal);
    }

    private static KeyValuePair<int, string>[] Snapshot(params (int Key, string Value)[] values) =>
        values.Select(value => KeyValuePair.Create(value.Key, value.Value)).ToArray();

    private sealed class FakeNode
    {
        public string? Value { get; set; }
    }
}
