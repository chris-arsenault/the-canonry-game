namespace Engine.Host;

using Engine.Domain;
using Engine.Eventing;
using Engine.Projection;

public sealed class KernelHostConsumerContext
{
    internal KernelHostConsumerContext(
        DomainIdRegistry domainIds,
        IKernelEventReader events,
        ProjectionSymbolCatalog symbols,
        KernelConsumerRegistry consumers)
    {
        DomainIds = domainIds;
        Events = events;
        Symbols = symbols;
        Consumers = consumers;
    }

    public DomainIdRegistry DomainIds { get; }

    public IKernelEventReader Events { get; }

    public ProjectionSymbolCatalog Symbols { get; }

    public KernelConsumerRegistry Consumers { get; }

    public KernelConsumerRegistration RegisterProjection(
        string name,
        IKernelEventProjection projection,
        int batchSize = 256) =>
        Consumers.RegisterProjection(name, projection, batchSize);

    public KernelConsumerRegistration RegisterProjections(
        string name,
        IEnumerable<IKernelEventProjection> projections,
        int batchSize = 256) =>
        Consumers.RegisterProjections(name, projections, batchSize);
}
