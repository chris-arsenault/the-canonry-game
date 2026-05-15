namespace IceRemembers;

using System.Globalization;
using System.Text.Json;

using Engine.Constants;
using Engine.Projection;

using NameForge.Runtime;

public sealed class IceRemembersEntityNameSynthesizer : IProjectionEntityNameSynthesizer
{
    private const string NameForgeResourceName = "IceRemembers.Names.ice-remembers.nameforge.json";

    private static readonly Lazy<NameForgeGenerator?> NameForge = new(LoadNameForgeGenerator);

    private static readonly HashSet<string> NameForgeKinds = new(StringComparer.Ordinal)
    {
        "region",
        "settlement",
        "faction",
        "penguin",
        "artifact",
        "orca",
        "point_of_interest",
        "ability",
        "monster",
        "transport",
        "occurrence",
    };

    private static readonly string[] NpcSyllables =
    [
        "bren",
        "fel",
        "sor",
        "ka",
        "mir",
        "dal",
        "tho",
        "ren",
    ];

    private static readonly string[] SettlementSyllables =
    [
        "iron",
        "frost",
        "sun",
        "stone",
        "wave",
        "high",
        "low",
        "south",
        "north",
        "old",
        "new",
        "bright",
    ];

    private static readonly string[] SettlementSuffixes =
    [
        "hold",
        "haven",
        "reach",
        "watch",
        "port",
        "gate",
        "fall",
    ];

    private static readonly string[] FactionAdjectives =
    [
        "Red",
        "Iron",
        "Silent",
        "Hungry",
        "Golden",
        "Forgotten",
        "Ember",
        "Silver",
    ];

    private static readonly string[] FactionNouns =
    [
        "Compact",
        "League",
        "Covenant",
        "Host",
        "Kin",
        "Assembly",
        "Circle",
        "Order",
    ];

    private static readonly string[] ArtifactPrefixes =
    [
        "storm",
        "night",
        "sun",
        "iron",
        "ember",
        "thorn",
        "frost",
        "dawn",
    ];

    private static readonly string[] ArtifactSuffixes =
    [
        "cleaver",
        "breaker",
        "shield",
        "crown",
        "song",
        "tear",
        "rune",
        "chain",
    ];

    private static readonly string[] RegionDescriptors =
    [
        "hush",
        "ember",
        "pale",
        "broken",
        "deep",
        "old",
        "black",
        "white",
        "salt",
        "cold",
    ];

    private static readonly string[] SeaSuffixes =
    [
        "sea",
        "reach",
        "sound",
        "water",
    ];

    private static readonly string[] ShelfSuffixes =
    [
        "shelf",
        "ledge",
        "face",
        "wall",
    ];

    private static readonly string[] ScarSuffixes =
    [
        "scar",
        "rent",
        "breach",
        "wound",
    ];

    private static readonly string[] GenericRegionSuffixes =
    [
        "march",
        "waste",
        "expanse",
        "reach",
    ];

    private static readonly string[] HeroPrefixes = ["Captain", "Sir"];
    private static readonly string[] ScholarPrefixes = ["Elder", "Master"];
    private static readonly string[] WarriorPrefixes = ["Captain", "Warden"];

    public static IceRemembersEntityNameSynthesizer Instance { get; } = new();

    private IceRemembersEntityNameSynthesizer()
    {
    }

    public bool TrySynthesizeName(ProjectionEntityNameContext context, out string displayName)
    {
        var kind = context.Kind?.StableName;
        if (string.IsNullOrWhiteSpace(kind))
        {
            displayName = string.Empty;
            return false;
        }

        if (NameForgeKinds.Contains(kind) && TrySynthesizeNameForge(context, kind, out displayName))
        {
            return true;
        }

        var subtype = context.Subtype?.StableName ?? string.Empty;
        var rng = CreateRandom(context.Key);
        var fallbackBuilder = ResolveFallbackBuilder(kind);
        if (fallbackBuilder is null)
        {
            displayName = string.Empty;
            return false;
        }

        displayName = fallbackBuilder(subtype, rng);
        return true;
    }

    private static Func<string, Random, string>? ResolveFallbackBuilder(string kind) =>
        kind switch
        {
            var value when IsKind(value, IceRemembersKinds.Penguin.Value) => BuildPenguinName,
            var value when IsKind(value, IceRemembersKinds.Settlement.Value) => static (_, rng) => BuildSettlementName(rng),
            var value when IsKind(value, IceRemembersKinds.Faction.Value) => static (_, rng) => $"The {Pick(FactionAdjectives, rng)} {Pick(FactionNouns, rng)}",
            var value when IsKind(value, IceRemembersKinds.Artifact.Value) => static (_, rng) => BuildArtifactName(rng),
            var value when IsKind(value, IceRemembersKinds.Region.Value) => BuildRegionName,
            _ => null,
        };

    private static bool TrySynthesizeNameForge(
        ProjectionEntityNameContext context,
        string kind,
        out string displayName)
    {
        displayName = string.Empty;
        var generator = NameForge.Value;
        if (generator is null ||
            !TryResolveNameForgeCulture(context.Culture?.StableName, out var cultureId))
        {
            return false;
        }

        var request = new NameForgeGenerateRequest
        {
            CultureId = cultureId,
            Kind = kind,
            Subtype = context.Subtype?.StableName ?? string.Empty,
            Prominence = "recognized",
            Context = BuildNameForgeContext(context),
            Seed = string.Create(
                CultureInfo.InvariantCulture,
                $"{context.Key.NameSeed}:{context.Key.EntityId}:{context.Key.KindId}:{context.Key.SubtypeId}:{context.Key.CultureId}"),
        };

        return generator.TryGenerateOne(request, out displayName);
    }

    private static Dictionary<string, string> BuildNameForgeContext(ProjectionEntityNameContext context)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var provenance = context.Provenance;
        if (provenance is null)
        {
            return values;
        }

        AddInvocation(values, string.Empty, provenance.Invocation, provenance.Producer);
        Add(values, "primary_entity_name", provenance.PrimaryEntityName);
        Add(values, "secondary_entity_name", provenance.SecondaryEntityName);
        Add(values, "selected", provenance.PrimaryEntityName);
        Add(values, "created_by_invocation_id", provenance.Invocation.InvocationId);
        Add(values, "parent_chain_depth", provenance.ParentChain.Count);
        Add(values, "output_count", provenance.Outputs.Count);

        if (provenance.ParentInvocation is { } parentInvocation)
        {
            AddInvocation(values, "parent_", parentInvocation, provenance.ParentProducer);
            Add(values, "parent_primary_entity_name", provenance.ParentPrimaryEntityName);
            Add(values, "parent_secondary_entity_name", provenance.ParentSecondaryEntityName);
        }

        if (provenance.RelationshipTrigger is { } relationshipTrigger)
        {
            Add(values, "trigger_relationship_id", relationshipTrigger.RelationshipId);
            Add(values, "trigger_relationship_kind_id", relationshipTrigger.RelationshipKindId);
            Add(values, "trigger_relationship_source_entity_id", relationshipTrigger.SourceEntityId);
            Add(values, "trigger_relationship_target_entity_id", relationshipTrigger.TargetEntityId);
            Add(values, "trigger_relationship_created_by_invocation_id", relationshipTrigger.CreatedByInvocationId);
            Add(values, "trigger_relationship_last_changed_by_invocation_id", relationshipTrigger.LastChangedByInvocationId);
            Add(values, "trigger_relationship_kind", provenance.RelationshipTriggerKind?.StableName);
            Add(values, "trigger_relationship_kind_display_name", provenance.RelationshipTriggerKind?.DisplayName);
            Add(values, "trigger_relationship_source_entity_name", provenance.RelationshipTriggerSourceEntityName);
            Add(values, "trigger_relationship_target_entity_name", provenance.RelationshipTriggerTargetEntityName);
        }

        return values;
    }

    private static void AddInvocation(
        Dictionary<string, string> values,
        string prefix,
        ProvenanceInvocationProjection invocation,
        ProjectionSymbol? producer)
    {
        Add(values, $"{prefix}invocation_id", invocation.InvocationId);
        Add(values, $"{prefix}tick", invocation.Tick);
        Add(values, $"{prefix}producer_kind_code", invocation.ProducerKindCode);
        Add(values, $"{prefix}producer_kind", ProducerKindName(invocation.ProducerKindCode));
        Add(values, $"{prefix}producer_id", invocation.ProducerId);
        Add(values, $"{prefix}producer", producer?.StableName);
        Add(values, $"{prefix}producer_display_name", producer?.DisplayName);
        Add(values, $"{prefix}primary_entity_id", invocation.PrimaryEntityId);
        Add(values, $"{prefix}secondary_entity_id", invocation.SecondaryEntityId);
        Add(values, $"{prefix}parent_invocation_id", invocation.ParentInvocationId);
        Add(values, $"{prefix}trigger_kind_code", invocation.TriggerKindCode);
        Add(values, $"{prefix}trigger_kind", TriggerKindName(invocation.TriggerKindCode));
        Add(values, $"{prefix}trigger_id", invocation.TriggerId);
        Add(values, $"{prefix}declared_output_count", invocation.OutputCount);
    }

    private static void Add(Dictionary<string, string> values, string key, int value)
    {
        if (value > 0)
        {
            values[key] = value.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static void Add(Dictionary<string, string> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values[key] = value;
        }
    }

    private static string ProducerKindName(int producerKindCode) =>
        producerKindCode switch
        {
            ProvenanceProducerKindCodes.Generator => "generator",
            ProvenanceProducerKindCodes.Gambit => "gambit",
            ProvenanceProducerKindCodes.Action => "action",
            ProvenanceProducerKindCodes.System => "system",
            _ => string.Empty,
        };

    private static string TriggerKindName(int triggerKindCode) =>
        triggerKindCode switch
        {
            ProvenanceTriggerKindCodes.Entity => "entity",
            ProvenanceTriggerKindCodes.Relationship => "relationship",
            ProvenanceTriggerKindCodes.Invocation => "invocation",
            ProvenanceTriggerKindCodes.Attribute => "attribute",
            _ => string.Empty,
        };

    private static bool TryResolveNameForgeCulture(string? culture, out string cultureId)
    {
        cultureId = culture switch
        {
            "aurora_stack" or "aurora-stack" => "aurora_stack",
            "nightshelf" => "nightshelf",
            "wake_singers" or "orca" => "wake_singers",
            "world" => "world",
            _ => string.Empty,
        };

        return cultureId.Length > 0;
    }

    private static NameForgeGenerator? LoadNameForgeGenerator()
    {
        var assembly = typeof(IceRemembersEntityNameSynthesizer).Assembly;
        using var stream = assembly.GetManifestResourceStream(NameForgeResourceName);
        if (stream is null)
        {
            return null;
        }

        try
        {
            return new NameForgeGenerator(NameForgeProject.Load(stream));
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static string BuildPenguinName(string subtype, Random rng)
    {
        var core = Capitalize(Pick(NpcSyllables, rng) + Pick(NpcSyllables, rng));
        var prefix = subtype switch
        {
            "warden" => Pick(HeroPrefixes, rng),
            "scholar" => Pick(ScholarPrefixes, rng),
            "warrior" => Pick(WarriorPrefixes, rng),
            "merchant" => null,
            _ => null,
        };

        return prefix is null ? core : $"{prefix} {core}";
    }

    private static string BuildSettlementName(Random rng)
    {
        var syllableCount = rng.Next(2, 4);
        var parts = new List<string>(capacity: syllableCount);
        for (var i = 0; i < syllableCount - 1; i++)
        {
            parts.Add(Pick(SettlementSyllables, rng));
        }

        parts.Add(Pick(SettlementSuffixes, rng));
        return Capitalize(string.Concat(parts));
    }

    private static string BuildArtifactName(Random rng)
    {
        var prefixCount = rng.Next(1, 3);
        var parts = new List<string>(capacity: prefixCount + 1);
        for (var i = 0; i < prefixCount; i++)
        {
            parts.Add(Pick(ArtifactPrefixes, rng));
        }

        parts.Add(Pick(ArtifactSuffixes, rng));
        return Capitalize(string.Concat(parts));
    }

    private static string BuildRegionName(string subtype, Random rng)
    {
        var descriptor = Capitalize(Pick(RegionDescriptors, rng));
        var suffixes = subtype switch
        {
            "sea" => SeaSuffixes,
            "shelf" => ShelfSuffixes,
            "scar" => ScarSuffixes,
            _ => GenericRegionSuffixes,
        };

        return $"{descriptor} {Capitalize(Pick(suffixes, rng))}";
    }

    private static string Pick(string[] values, Random rng) =>
        values[rng.Next(values.Length)];

    private static bool IsKind(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.Ordinal);

    private static Random CreateRandom(ProjectionEntityNameKey key)
    {
        var seed = key.NameSeed != 0 ? key.NameSeed : key.EntityId;
        return new Random(Mix(seed, key.EntityId, key.KindId, key.SubtypeId, key.CultureId));
    }

    private static int Mix(int a, int b, int c, int d, int e)
    {
        unchecked
        {
            var hash = 2166136261u;
            hash = (hash ^ (uint)a) * 16777619u;
            hash = (hash ^ (uint)b) * 16777619u;
            hash = (hash ^ (uint)c) * 16777619u;
            hash = (hash ^ (uint)d) * 16777619u;
            hash = (hash ^ (uint)e) * 16777619u;
            return (int)(hash & 0x7fffffffu);
        }
    }

    private static string Capitalize(string value) =>
        string.IsNullOrEmpty(value)
            ? value
            : char.ToUpperInvariant(value[0]) + value[1..];
}
