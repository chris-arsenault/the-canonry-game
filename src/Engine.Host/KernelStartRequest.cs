namespace Engine.Host;

using System.Collections.ObjectModel;
using System.Security.Cryptography;

using Engine.Domain.Config;
using Engine.Domain.Startup;

public enum StartupSeedPolicyKind
{
    Random = 0,
    Requested = 1,
}

public readonly record struct StartupSeedPolicy
{
    private StartupSeedPolicy(StartupSeedPolicyKind kind, int requestedSeed)
    {
        Kind = kind;
        RequestedSeed = requestedSeed;
    }

    public StartupSeedPolicyKind Kind { get; }

    public int RequestedSeed { get; }

    public static StartupSeedPolicy Random() => new(StartupSeedPolicyKind.Random, 0);

    public static StartupSeedPolicy Requested(int seed)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seed);
        return new StartupSeedPolicy(StartupSeedPolicyKind.Requested, seed);
    }

    internal int ResolveBaseSeed() => Kind switch
    {
        StartupSeedPolicyKind.Random => RandomNumberGenerator.GetInt32(1, int.MaxValue),
        StartupSeedPolicyKind.Requested => RequestedSeed,
        _ => throw new InvalidOperationException($"Unknown startup seed policy '{Kind}'."),
    };
}

public enum KernelFunctionProfile
{
    InteractiveRealtime = 0,
    ManualStep = 1,
    Batch = 2,
}

public sealed record KernelStartRequest
{
    public KernelStartRequest(
        string domainName,
        StartupSeedPolicy seedPolicy,
        KernelFunctionProfile functionProfile,
        bool traceEnabled = false,
        string? configFilePath = null,
        DomainStartupSeedResult? startupSeed = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainName);
        if (configFilePath is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(configFilePath);
        }

        DomainName = domainName;
        SeedPolicy = seedPolicy;
        FunctionProfile = functionProfile;
        TraceEnabled = traceEnabled;
        ConfigFilePath = configFilePath;
        StartupSeed = startupSeed;
    }

    public string DomainName { get; }

    public StartupSeedPolicy SeedPolicy { get; }

    public KernelFunctionProfile FunctionProfile { get; }

    public bool TraceEnabled { get; }

    public string? ConfigFilePath { get; }

    /// <summary>
    /// Optional prebuilt domain seed. Interactive shells use this to restore a
    /// versioned world-generation cache without moving persistence into the
    /// deterministic kernel runtime.
    /// </summary>
    public DomainStartupSeedResult? StartupSeed { get; }
}

public readonly record struct DomainStartupSeedRequest
{
    public DomainStartupSeedRequest(int baseSeed)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseSeed);
        BaseSeed = baseSeed;
    }

    public int BaseSeed { get; }
}

public sealed record StartupSeedIdentity
{
    public StartupSeedIdentity(
        int baseSeed,
        int attempt,
        IReadOnlyDictionary<string, int>? components = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseSeed);
        ArgumentOutOfRangeException.ThrowIfNegative(attempt);

        BaseSeed = baseSeed;
        Attempt = attempt;
        Components = new ReadOnlyDictionary<string, int>(
            components is null
                ? new Dictionary<string, int>(StringComparer.Ordinal)
                : new Dictionary<string, int>(components, StringComparer.Ordinal));
    }

    public int BaseSeed { get; }

    public int Attempt { get; }

    public IReadOnlyDictionary<string, int> Components { get; }

    public bool TryGetComponent(string name, out int value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Components.TryGetValue(name, out value);
    }
}

public sealed record DomainStartupSeedResult
{
    public DomainStartupSeedResult(
        DomainStartupSeed seed,
        StartupSeedIdentity identity,
        IReadOnlyList<string>? rejectedAttempts = null)
    {
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(identity);
        Seed = seed;
        Identity = identity;
        RejectedAttempts = rejectedAttempts ?? [];
    }

    public DomainStartupSeed Seed { get; }

    public StartupSeedIdentity Identity { get; }

    /// <summary>
    /// Why the attempts before the accepted one were turned down, in order.
    /// A domain that rejects seeds silently makes a change that degrades the
    /// world indistinguishable from one that merely reshuffles it.
    /// </summary>
    public IReadOnlyList<string> RejectedAttempts { get; }
}

public interface IKernelDomainModule
{
    string Name { get; }

    DomainConfig CreateDomainConfig();

    DomainStartupSeedResult BuildStartupSeed(DomainStartupSeedRequest request);

    void ConfigureProjectionSymbols(KernelSymbolContext symbols);
}
