namespace Engine.Host;

using Engine.Constants;
using Engine.Domain.Compilation;
using Engine.Kernel;
using Engine.Observability;
using Engine.Projection;

public sealed partial class KernelHost
{
    private const float WitnessHeightWeight = 0.25f;

    private CulturePowerVocabulary? _culturePowerVocabulary;

    private CulturePowerVocabulary CulturePowerVocabularyInstance =>
        _culturePowerVocabulary ??= CulturePowerVocabulary.Resolve(CompiledDomain);

    /// <summary>
    /// Computes the culture power ledgers (Witness/Shadow/Wake) from current
    /// world state. Shares are 0-100 percentages of world totals; axis
    /// components whose world total is zero are dropped from their mean. Wake
    /// counts claims, war presence, and devotion flow: offering-relationship
    /// strength attributed to the receiving site's culture.
    /// </summary>
    public IReadOnlyList<CulturePowerSummary> ComputeCulturePower()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var world = _kernel.World;
        var accumulators = new CulturePowerAccumulator[world.CultureHighWatermark + 1];
        AccumulateFieldMass(world, accumulators);
        AccumulateRelationshipStrengths(world, accumulators);
        return BuildCulturePowerSummaries(accumulators);
    }

    public void RecordCulturePowerTelemetry(TelemetryBatch batch, int runId, int tick)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);

        foreach (var summary in ComputeCulturePower())
        {
            batch.CulturePowerSnapshots.Add(new CulturePowerSnapshotRow(
                runId,
                tick,
                summary.CultureId,
                summary.WitnessShare,
                summary.ShadowShare,
                summary.WakeShare,
                summary.WitnessMass,
                summary.HiddenMass,
                summary.SemanticMass,
                summary.CustodyStrength,
                summary.ClaimStrength,
                summary.WarStrength,
                summary.OfferingStrength));
        }
    }

    private static void AccumulateFieldMass(WorldState world, CulturePowerAccumulator[] accumulators)
    {
        for (var cultureId = 1; cultureId < accumulators.Length; cultureId++)
        {
            ref var accumulator = ref accumulators[cultureId];
            var physical = world.RawCultureInfluenceCellsOf(cultureId, PlaneCodes.Physical);
            while (world.MoveNextRawCultureInfluenceCell(ref physical, out _, out _, out var z, out var raw))
            {
                if (z >= 0)
                {
                    accumulator.WitnessMass += raw * (1f + (WitnessHeightWeight * z));
                }
                else
                {
                    accumulator.HiddenMass += raw;
                }
            }

            var semantic = world.RawCultureInfluenceCellsOf(cultureId, PlaneCodes.Semantic);
            while (world.MoveNextRawCultureInfluenceCell(ref semantic, out _, out _, out var semanticRaw))
            {
                accumulator.SemanticMass += semanticRaw;
            }
        }
    }

    private void AccumulateRelationshipStrengths(WorldState world, CulturePowerAccumulator[] accumulators)
    {
        var vocabulary = CulturePowerVocabularyInstance;
        if (!vocabulary.HasRelationshipVocabulary)
        {
            return;
        }

        for (var relationshipId = 1; relationshipId <= world.RelationshipHighWatermark; relationshipId++)
        {
            if (!world.RelationshipIsActive(relationshipId))
            {
                continue;
            }

            var strength = world.RelationshipStrength(relationshipId);
            if (strength <= 0f)
            {
                continue;
            }

            AccumulateRelationship(world, accumulators, vocabulary, relationshipId, strength);
        }
    }

    private static void AccumulateRelationship(
        WorldState world,
        CulturePowerAccumulator[] accumulators,
        CulturePowerVocabulary vocabulary,
        int relationshipId,
        float strength)
    {
        var kindId = world.RelationshipKindId(relationshipId);
        if (vocabulary.IsCustodyRelationship(kindId))
        {
            AccumulateCustody(world, accumulators, vocabulary, relationshipId, strength);
        }

        if (kindId == vocabulary.ClaimRelationshipKindId)
        {
            var cultureId = ActiveEntityCultureId(world, world.RelationshipTargetId(relationshipId));
            if (cultureId > 0)
            {
                accumulators[cultureId].ClaimStrength += strength;
            }
        }

        if (kindId == vocabulary.WarRelationshipKindId)
        {
            AccumulateWar(world, accumulators, relationshipId, strength);
        }

        if (vocabulary.IsOfferingRelationship(kindId))
        {
            var siteCultureId = ActiveEntityCultureId(world, world.RelationshipTargetId(relationshipId));
            if (siteCultureId > 0)
            {
                accumulators[siteCultureId].OfferingStrength += strength;
            }
        }
    }

    private static void AccumulateCustody(
        WorldState world,
        CulturePowerAccumulator[] accumulators,
        CulturePowerVocabulary vocabulary,
        int relationshipId,
        float strength)
    {
        AccumulateCustodyEndpoint(world, accumulators, vocabulary, world.RelationshipSourceId(relationshipId), strength);
        AccumulateCustodyEndpoint(world, accumulators, vocabulary, world.RelationshipTargetId(relationshipId), strength);
    }

    private static void AccumulateCustodyEndpoint(
        WorldState world,
        CulturePowerAccumulator[] accumulators,
        CulturePowerVocabulary vocabulary,
        int entityId,
        float strength)
    {
        if (!world.EntityIsActive(entityId) || !vocabulary.IsCustodyKind(world.EntityKindId(entityId)))
        {
            return;
        }

        var cultureId = world.EntityCultureId(entityId);
        if (cultureId > 0 && cultureId < accumulators.Length)
        {
            accumulators[cultureId].CustodyStrength += strength;
        }
    }

    private static void AccumulateWar(
        WorldState world,
        CulturePowerAccumulator[] accumulators,
        int relationshipId,
        float strength)
    {
        var sourceCultureId = ActiveEntityCultureId(world, world.RelationshipSourceId(relationshipId));
        var targetCultureId = ActiveEntityCultureId(world, world.RelationshipTargetId(relationshipId));
        if (sourceCultureId > 0)
        {
            accumulators[sourceCultureId].WarStrength += strength;
        }

        if (targetCultureId > 0 && targetCultureId != sourceCultureId)
        {
            accumulators[targetCultureId].WarStrength += strength;
        }
    }

    private static int ActiveEntityCultureId(WorldState world, int entityId) =>
        world.EntityIsActive(entityId) ? world.EntityCultureId(entityId) : 0;

    private static CulturePowerSummary[] BuildCulturePowerSummaries(CulturePowerAccumulator[] accumulators)
    {
        var totals = default(CulturePowerAccumulator);
        for (var cultureId = 1; cultureId < accumulators.Length; cultureId++)
        {
            totals.AddFrom(accumulators[cultureId]);
        }

        var summaries = new CulturePowerSummary[Math.Max(0, accumulators.Length - 1)];
        for (var cultureId = 1; cultureId < accumulators.Length; cultureId++)
        {
            summaries[cultureId - 1] = BuildCulturePowerSummary(cultureId, accumulators[cultureId], totals);
        }

        return summaries;
    }

    private static CulturePowerSummary BuildCulturePowerSummary(
        int cultureId,
        in CulturePowerAccumulator accumulator,
        in CulturePowerAccumulator totals)
    {
        var shadow = default(ComponentMean);
        shadow.Add(accumulator.SemanticMass, totals.SemanticMass);
        shadow.Add(accumulator.HiddenMass, totals.HiddenMass);
        shadow.Add(accumulator.CustodyStrength, totals.CustodyStrength);

        var wake = default(ComponentMean);
        wake.Add(accumulator.ClaimStrength, totals.ClaimStrength);
        wake.Add(accumulator.WarStrength, totals.WarStrength);
        wake.Add(accumulator.OfferingStrength, totals.OfferingStrength);

        return new CulturePowerSummary(
            cultureId,
            Share(accumulator.WitnessMass, totals.WitnessMass),
            shadow.Mean,
            wake.Mean,
            accumulator.WitnessMass,
            accumulator.HiddenMass,
            accumulator.SemanticMass,
            accumulator.CustodyStrength,
            accumulator.ClaimStrength,
            accumulator.WarStrength,
            accumulator.OfferingStrength);
    }

    private static float Share(float value, float total) =>
        total <= 0f ? 0f : value * 100f / total;

    private struct CulturePowerAccumulator
    {
        public float WitnessMass;
        public float HiddenMass;
        public float SemanticMass;
        public float CustodyStrength;
        public float ClaimStrength;
        public float WarStrength;
        public float OfferingStrength;

        public void AddFrom(in CulturePowerAccumulator other)
        {
            WitnessMass += other.WitnessMass;
            HiddenMass += other.HiddenMass;
            SemanticMass += other.SemanticMass;
            CustodyStrength += other.CustodyStrength;
            ClaimStrength += other.ClaimStrength;
            WarStrength += other.WarStrength;
            OfferingStrength += other.OfferingStrength;
        }
    }

    private struct ComponentMean
    {
        private float _sum;
        private int _count;

        public float Mean => _count == 0 ? 0f : _sum / _count;

        public void Add(float value, float total)
        {
            if (total <= 0f)
            {
                return;
            }

            _sum += Share(value, total);
            _count++;
        }
    }

    private sealed class CulturePowerVocabulary
    {
        private static readonly CulturePowerVocabulary Empty = new([], [], 0, 0, []);

        private readonly HashSet<int> _custodyKindIds;
        private readonly HashSet<int> _custodyRelationshipKindIds;
        private readonly HashSet<int> _offeringRelationshipKindIds;

        private CulturePowerVocabulary(
            HashSet<int> custodyKindIds,
            HashSet<int> custodyRelationshipKindIds,
            int claimRelationshipKindId,
            int warRelationshipKindId,
            HashSet<int> offeringRelationshipKindIds)
        {
            _custodyKindIds = custodyKindIds;
            _custodyRelationshipKindIds = custodyRelationshipKindIds;
            ClaimRelationshipKindId = claimRelationshipKindId;
            WarRelationshipKindId = warRelationshipKindId;
            _offeringRelationshipKindIds = offeringRelationshipKindIds;
        }

        public int ClaimRelationshipKindId { get; }

        public int WarRelationshipKindId { get; }

        public bool HasRelationshipVocabulary =>
            _custodyRelationshipKindIds.Count > 0 ||
            ClaimRelationshipKindId > 0 ||
            WarRelationshipKindId > 0 ||
            _offeringRelationshipKindIds.Count > 0;

        public bool IsCustodyKind(int kindId) => _custodyKindIds.Contains(kindId);

        public bool IsCustodyRelationship(int relationshipKindId) =>
            _custodyRelationshipKindIds.Contains(relationshipKindId);

        public bool IsOfferingRelationship(int relationshipKindId) =>
            _offeringRelationshipKindIds.Contains(relationshipKindId);

        public static CulturePowerVocabulary Resolve(CompiledDomain? domain)
        {
            if (domain is null)
            {
                return Empty;
            }

            var config = domain.Config.CulturePower;
            var custodyKindIds = new HashSet<int>();
            foreach (var kindName in config.CustodyKinds)
            {
                custodyKindIds.Add(domain.Ids.KindIdOf(kindName).Value);
            }

            var custodyRelationshipKindIds = new HashSet<int>();
            foreach (var relationshipName in config.CustodyRelationships)
            {
                custodyRelationshipKindIds.Add(domain.Ids.RelationshipKindIdOf(relationshipName).Value);
            }

            var offeringRelationshipKindIds = new HashSet<int>();
            foreach (var relationshipName in config.OfferingRelationships)
            {
                offeringRelationshipKindIds.Add(domain.Ids.RelationshipKindIdOf(relationshipName).Value);
            }

            return new CulturePowerVocabulary(
                custodyKindIds,
                custodyRelationshipKindIds,
                config.ClaimRelationship is { } claim ? domain.Ids.RelationshipKindIdOf(claim).Value : 0,
                config.WarRelationship is { } war ? domain.Ids.RelationshipKindIdOf(war).Value : 0,
                offeringRelationshipKindIds);
        }
    }
}
