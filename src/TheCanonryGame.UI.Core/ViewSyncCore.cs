namespace TheCanonryGame.UI;

internal sealed class ViewSyncCore<TKey, TNode>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TNode> _byKey = [];
    private readonly List<TNode> _orderedNodes = [];

    public IReadOnlyDictionary<TKey, TNode> Nodes => _byKey;

    public IReadOnlyList<TNode> OrderedNodes => _orderedNodes;

    public bool TryGetNode(TKey key, out TNode node) => _byKey.TryGetValue(key, out node!);

    public void Clear(Action<TNode> remove)
    {
        ArgumentNullException.ThrowIfNull(remove);

        foreach (var node in _byKey.Values)
        {
            remove(node);
        }

        _byKey.Clear();
        _orderedNodes.Clear();
    }

    public void Sync<TData>(
        IEnumerable<KeyValuePair<TKey, TData>> snapshot,
        Func<TData, TNode> create,
        Action<TNode, TData> update,
        Action<TNode> remove)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(create);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(remove);

        var seen = new HashSet<TKey>();
        _orderedNodes.Clear();

        foreach (var (key, data) in snapshot)
        {
            if (!seen.Add(key))
            {
                throw new InvalidOperationException($"View snapshot contains duplicate key '{key}'.");
            }

            if (!_byKey.TryGetValue(key, out var node))
            {
                node = create(data);
                _byKey[key] = node;
            }

            update(node, data);
            _orderedNodes.Add(node);
        }

        foreach (var key in _byKey.Keys.Where(key => !seen.Contains(key)).ToArray())
        {
            remove(_byKey[key]);
            _byKey.Remove(key);
        }
    }
}
