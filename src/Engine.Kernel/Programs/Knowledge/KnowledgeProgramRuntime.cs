namespace Engine.Kernel;

using Engine.Constants;

internal sealed class KnowledgeProgramRuntime : ISimulationSystem
{
    private const float ClaimBand = WorldState.CultureClaimedRawMass;
    private const int ClaimRadius = 2;
    private const int TargetMask = 0x00ffffff;

    private readonly int[] _siteQ;
    private readonly int[] _siteR;
    private readonly int[] _siteTier;
    private readonly int[] _grantSite;
    private readonly int[] _grantDomain;
    private readonly int[] _grantTarget;
    private readonly float[] _grantMultiplier;
    private readonly int[] _claimedTechs;
    private readonly int[] _maxTier;
    private readonly byte[] _claimedSites;
    private readonly PrimitiveFloatTable _modifiers;
    private readonly int _siteCapacity;
    private int _siteCount;
    private int _grantCount;
    private long _revision;

    internal KnowledgeProgramRuntime(
        int siteCapacity,
        int grantCapacity,
        int modifierCapacity,
        int cultureIdCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(siteCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(grantCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(modifierCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cultureIdCapacity);
        _siteQ = new int[siteCapacity];
        _siteR = new int[siteCapacity];
        _siteTier = new int[siteCapacity];
        _grantSite = new int[grantCapacity];
        _grantDomain = new int[grantCapacity];
        _grantTarget = new int[grantCapacity];
        _grantMultiplier = new float[grantCapacity];
        _claimedTechs = new int[cultureIdCapacity + 1];
        _maxTier = new int[cultureIdCapacity + 1];
        _claimedSites = new byte[checked((cultureIdCapacity + 1) * siteCapacity)];
        _modifiers = new PrimitiveFloatTable(modifierCapacity);
        _siteCapacity = siteCapacity;
        global::System.Array.Fill(_maxTier, -1);
    }

    internal int SiteCount => _siteCount;

    internal long Revision => _revision;

    internal int RegisterSite(int q, int r, int tier)
    {
        if (_siteCount >= _siteQ.Length || tier < 0)
        {
            throw new global::System.InvalidOperationException();
        }

        var site = _siteCount;
        _siteQ[site] = q;
        _siteR[site] = r;
        _siteTier[site] = tier;
        _siteCount++;
        return site;
    }

    internal void RegisterGrant(int site, int domain, int target, float multiplier)
    {
        if ((uint)site >= (uint)_siteCount ||
            _grantCount >= _grantSite.Length ||
            domain <= 0 ||
            target < 0 ||
            target > TargetMask ||
            multiplier <= 0f)
        {
            throw new global::System.InvalidOperationException();
        }

        _grantSite[_grantCount] = site;
        _grantDomain[_grantCount] = domain;
        _grantTarget[_grantCount] = target;
        _grantMultiplier[_grantCount] = multiplier;
        _grantCount++;
    }

    internal int ClaimedTechs(int cultureId) =>
        (uint)cultureId < (uint)_claimedTechs.Length ? _claimedTechs[cultureId] : 0;

    internal int MaxTier(int cultureId) =>
        (uint)cultureId < (uint)_maxTier.Length ? _maxTier[cultureId] : -1;

    internal bool CultureClaimsSite(int cultureId, int site) =>
        cultureId > 0 &&
        (uint)cultureId < (uint)_claimedTechs.Length &&
        (uint)site < (uint)_siteCount &&
        _claimedSites[(cultureId * _siteCapacity) + site] != 0;

    internal float Modifier(int cultureId, int domain, int target = 0)
    {
        if (cultureId <= 0 || domain <= 0 || target < 0 || target > TargetMask)
        {
            return 1f;
        }

        var global = _modifiers.GetOrDefault(cultureId, ModifierKey(domain, 0), 1f);
        return target == 0
            ? global
            : global * _modifiers.GetOrDefault(cultureId, ModifierKey(domain, target), 1f);
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        _modifiers.Clear();
        global::System.Array.Clear(_claimedSites);
        global::System.Array.Clear(_claimedTechs);
        global::System.Array.Fill(_maxTier, -1);

        var world = simulation.World;
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
        {
            RecomputeCulture(world, cultureId);
        }

        if (_revision < long.MaxValue)
        {
            _revision++;
        }
    }

    private void RecomputeCulture(WorldState world, int cultureId)
    {
        var maxTier = -1;
        for (var site = 0; site < _siteCount; site++)
        {
            if (!CultureHoldsSite(world, cultureId, site))
            {
                continue;
            }

            _claimedSites[(cultureId * _siteCapacity) + site] = 1;
            _claimedTechs[cultureId]++;
            maxTier = global::System.Math.Max(maxTier, _siteTier[site]);
            ApplySiteGrants(cultureId, site);
        }

        _maxTier[cultureId] = maxTier;
    }

    private void ApplySiteGrants(int cultureId, int site)
    {
        for (var grant = 0; grant < _grantCount; grant++)
        {
            if (_grantSite[grant] != site)
            {
                continue;
            }

            var key = ModifierKey(_grantDomain[grant], _grantTarget[grant]);
            var current = _modifiers.GetOrDefault(cultureId, key, 1f);
            _modifiers.Set(cultureId, key, current * _grantMultiplier[grant]);
        }
    }

    private bool CultureHoldsSite(WorldState world, int cultureId, int index)
    {
        var siteQ = _siteQ[index];
        var siteR = _siteR[index];
        for (var dq = -ClaimRadius; dq <= ClaimRadius; dq++)
        {
            var minR = global::System.Math.Max(-ClaimRadius, -dq - ClaimRadius);
            var maxR = global::System.Math.Min(ClaimRadius, -dq + ClaimRadius);
            for (var dr = minR; dr <= maxR; dr++)
            {
                var q = siteQ + dq;
                var r = siteR + dr;
                if (world.CellIsKnown(PlaneCodes.Semantic, q, r) &&
                    world.GetCellCultureInfluenceRaw(cultureId, PlaneCodes.Semantic, q, r) >= ClaimBand)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int ModifierKey(int domain, int target) =>
        (domain << 24) | (target & TargetMask);
}
