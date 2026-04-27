#pragma warning disable CA1062 // Public Godot-facing APIs are guarded at call sites or by ThrowIfNull.
using Godot;

namespace TheCanonryGame.UI;

public sealed class ViewSync<TKey, TNode>
    where TKey : notnull
    where TNode : Node
{
    private readonly ViewSyncCore<TKey, TNode> _core = new();

    public IReadOnlyDictionary<TKey, TNode> Nodes => _core.Nodes;

    public bool TryGetNode(TKey key, out TNode node) => _core.TryGetNode(key, out node!);

    public void Clear(Action<TNode>? remove = null)
    {
        _core.Clear(remove ?? QueueFreeNode);
    }

    public void Sync<TData>(
        Node parent,
        IEnumerable<KeyValuePair<TKey, TData>> snapshot,
        Func<TData, TNode> create,
        Action<TNode, TData> update,
        Action<TNode>? remove = null)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(create);
        ArgumentNullException.ThrowIfNull(update);

        _core.Sync(
            snapshot,
            create: data =>
            {
                var node = create(data);
                parent.AddChild(node);
                return node;
            },
            update,
            remove ?? QueueFreeNode);

        var index = 0;
        foreach (var node in _core.OrderedNodes)
        {
            if (node.GetParent() == parent && parent.GetChild(index) != node)
            {
                parent.MoveChild(node, index);
            }

            index++;
        }
    }

    private static void QueueFreeNode(TNode node) => node.QueueFree();
}
