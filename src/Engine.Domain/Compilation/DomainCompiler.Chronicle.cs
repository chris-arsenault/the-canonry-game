namespace Engine.Domain.Compilation;

using Engine.Constants;
using Engine.Domain.Config;

public static partial class DomainCompiler
{
    private static List<CompiledChronicleQuery> BuildChronicleQueries(
        DomainConfig config,
        DomainIdRegistry ids)
    {
        var queries = new List<CompiledChronicleQuery>();
        foreach (var query in config.Chronicle.Queries)
        {
            if (string.IsNullOrWhiteSpace(query.Id))
            {
                throw new InvalidOperationException("Chronicle query IDs must be non-empty.");
            }

            queries.Add(new CompiledChronicleQuery(
                query.Id,
                CompileChronicleProducer(query.Parent, ids),
                ids.RelationshipKindIdOf(query.TriggerRelationshipKind).Value,
                CompileChronicleProducer(query.Child, ids)));
        }

        return queries;
    }

    private static CompiledChronicleProducer CompileChronicleProducer(
        ChronicleProducerRef producer,
        DomainIdRegistry ids)
    {
        ArgumentNullException.ThrowIfNull(producer);
        var producerKindCode = ChronicleProducerKindCode(producer.Kind);
        var producerId = producer.Kind switch
        {
            ChronicleProducerKind.Gambit => ids.GambitIdOf(producer.StableName).Value,
            ChronicleProducerKind.Generator => ids.GeneratorIdOf(producer.StableName).Value,
            _ => throw new ArgumentOutOfRangeException(
                nameof(producer),
                producer.Kind,
                "Unsupported chronicle producer kind."),
        };

        return new CompiledChronicleProducer(producerKindCode, producerId);
    }

    private static int ChronicleProducerKindCode(ChronicleProducerKind kind) => kind switch
    {
        ChronicleProducerKind.Gambit => ProvenanceProducerKindCodes.Gambit,
        ChronicleProducerKind.Generator => ProvenanceProducerKindCodes.Generator,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported chronicle producer kind."),
    };
}
