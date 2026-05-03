namespace TheCanonryGame.Game.Chronicle;

using Engine.Projection;

using TheCanonryGame.Game.Domain;
using TheCanonryGame.Game.Runtime;

internal sealed class GameRuntimeChronicle : IGameChronicleFeed
{
    private const int MaxLines = 160;
    private const string TickColor = "6F7684";
    private const string ChainColor = "D7B968";
    private const string EntityColor = "E7E3D5";
    private const string ConnectorColor = "B8B5A6";

    private readonly UiChronicleProjection _baseChronicle;
    private readonly ProvenanceProjection _provenance;
    private readonly IProjectionSymbolCatalog _symbols;
    private readonly GameDomainKnowledge _knowledge;
    private readonly HashSet<int> _emittedChainInvocationIds = [];
    private readonly List<string> _chainLines = [];

    internal GameRuntimeChronicle(
        UiChronicleProjection baseChronicle,
        ProvenanceProjection provenance,
        IProjectionSymbolCatalog symbols,
        GameDomainKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(baseChronicle);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(symbols);
        ArgumentNullException.ThrowIfNull(knowledge);

        _baseChronicle = baseChronicle;
        _provenance = provenance;
        _symbols = symbols;
        _knowledge = knowledge;
    }

    public IReadOnlyList<string> Lines
    {
        get
        {
            RefreshChains();
            return CombinedLines();
        }
    }

    private void RefreshChains()
    {
        if (_knowledge.ChronicleQueries.Count == 0)
        {
            return;
        }

        foreach (var invocation in _provenance.Invocations.Values.OrderBy(static invocation => invocation.Sequence))
        {
            if (_emittedChainInvocationIds.Contains(invocation.InvocationId))
            {
                continue;
            }

            foreach (var query in _knowledge.ChronicleQueries)
            {
                if (TryAppendChain(query, invocation))
                {
                    break;
                }
            }
        }
    }

    private bool TryAppendChain(
        GameChronicleQuery query,
        ProvenanceInvocationProjection child)
    {
        if (!MatchesProducer(child, query.ChildProducerKindCode, query.ChildProducerId) ||
            !_provenance.TryGetParentInvocation(child.InvocationId, out var parent) ||
            !MatchesProducer(parent, query.ParentProducerKindCode, query.ParentProducerId) ||
            !_provenance.TryGetRelationshipTrigger(child.InvocationId, out var trigger) ||
            trigger.CreatedByInvocationId != parent.InvocationId ||
            trigger.RelationshipKindId != query.TriggerRelationshipKindId)
        {
            return false;
        }

        _emittedChainInvocationIds.Add(child.InvocationId);
        _chainLines.Add(FormatChainLine(child, parent, trigger));
        TrimChainLines();
        return true;
    }

    private string FormatChainLine(
        ProvenanceInvocationProjection child,
        ProvenanceInvocationProjection parent,
        ProvenanceRelationshipTriggerProjection trigger)
    {
        var parentName = ResolveProducer(parent.ProducerKindCode, parent.ProducerId);
        var childName = ResolveProducer(child.ProducerKindCode, child.ProducerId);
        var relationshipName = _symbols.ResolveDisplayName(
            ProjectionSymbolKind.RelationshipKind,
            trigger.RelationshipKindId,
            $"relationship {trigger.RelationshipKindId}");

        return $"{TickPrefix(child.Tick)}[b][color=#{ChainColor}]history chain[/color][/b] " +
            $"[color=#{EntityColor}]{parentName}[/color] [color=#{ConnectorColor}]->[/color] " +
            $"[color=#{EntityColor}]{childName}[/color] [color=#{ConnectorColor}]via[/color] " +
            $"[color=#{EntityColor}]{relationshipName}[/color]";
    }

    private static bool MatchesProducer(
        ProvenanceInvocationProjection invocation,
        int producerKindCode,
        int producerId) =>
        invocation.ProducerKindCode == producerKindCode &&
        invocation.ProducerId == producerId;

    private string ResolveProducer(int producerKindCode, int producerId)
    {
        var symbolKind = producerKindCode switch
        {
            ProvenanceProducerKindCodes.Gambit => ProjectionSymbolKind.Gambit,
            ProvenanceProducerKindCodes.Generator => ProjectionSymbolKind.Generator,
            _ => ProjectionSymbolKind.None,
        };

        return symbolKind == ProjectionSymbolKind.None
            ? $"producer {producerKindCode}:{producerId}"
            : _symbols.ResolveDisplayName(symbolKind, producerId, $"{symbolKind} {producerId}");
    }

    private static string TickPrefix(int tick) =>
        $"[color=#{TickColor}]t{tick,4}[/color]  ";

    private void TrimChainLines()
    {
        if (_chainLines.Count > MaxLines)
        {
            _chainLines.RemoveRange(0, _chainLines.Count - MaxLines);
        }
    }

    private IReadOnlyList<string> CombinedLines()
    {
        var totalCount = _baseChronicle.Lines.Count + _chainLines.Count;
        if (totalCount <= MaxLines)
        {
            return _baseChronicle.Lines.Concat(_chainLines).ToArray();
        }

        var skip = totalCount - MaxLines;
        var lines = new List<string>(MaxLines);
        foreach (var line in _baseChronicle.Lines.Concat(_chainLines).Skip(skip))
        {
            lines.Add(line);
        }

        return lines;
    }
}
