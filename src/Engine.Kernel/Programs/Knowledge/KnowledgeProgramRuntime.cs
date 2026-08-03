namespace Engine.Kernel;

using Engine.Constants;

internal sealed class KnowledgeProgramRuntime : ISimulationSystem
{
    private static readonly FixedScalar ClaimBand = WorldState.CultureClaimedRawMass;
    private const int ClaimRadius = 2;
    private const int TargetMask = 0x00ffffff;

    private readonly int[] _siteQ;
    private readonly int[] _siteR;
    private readonly int[] _siteTier;
    private readonly int[] _grantDomain;
    private readonly int[] _grantTarget;
    private readonly int[] _grantMultiplier;
    private readonly int[] _firstGrantBySite;
    private readonly int[] _lastGrantBySite;
    private readonly int[] _nextGrant;
    private readonly int[] _modifierKeys;
    private readonly int[] _claimedTechs;
    private readonly int[] _maxTier;
    private readonly byte[] _claimedSites;
    private readonly long[] _seenCultureFieldRevisions;
    private readonly PrimitiveIntTable _modifiers;
    private readonly int _siteCapacity;
    private int _siteCount;
    private int _grantCount;
    private int _modifierKeyCount;
    private long _revision;

    internal KnowledgeProgramRuntime(
        int siteCapacity,
        int grantCapacity,
        int modifierCapacity,
        int cultureIdCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(siteCapacity);
        NumericGuard.ThrowIfNegativeOrZero(grantCapacity);
        NumericGuard.ThrowIfNegativeOrZero(modifierCapacity);
        NumericGuard.ThrowIfNegativeOrZero(cultureIdCapacity);
        _siteQ = new int[siteCapacity];
        _siteR = new int[siteCapacity];
        _siteTier = new int[siteCapacity];
        _grantDomain = new int[grantCapacity];
        _grantTarget = new int[grantCapacity];
        _grantMultiplier = new int[grantCapacity];
        _firstGrantBySite = new int[siteCapacity];
        _lastGrantBySite = new int[siteCapacity];
        _nextGrant = new int[grantCapacity];
        _modifierKeys = new int[grantCapacity];
        _claimedTechs = new int[cultureIdCapacity + 1];
        _maxTier = new int[cultureIdCapacity + 1];
        _claimedSites = new byte[checked((cultureIdCapacity + 1) * siteCapacity)];
        _seenCultureFieldRevisions = new long[cultureIdCapacity + 1];
        _modifiers = new PrimitiveIntTable(modifierCapacity);
        _siteCapacity = siteCapacity;
        global::System.Array.Fill(_maxTier, -1);
        global::System.Array.Fill(_seenCultureFieldRevisions, -1);
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

    internal void RegisterGrant(int site, int domain, int target, FixedScalar multiplier)
    {
        if ((uint)site >= (uint)_siteCount ||
            _grantCount >= _grantDomain.Length ||
            domain <= 0 ||
            target < 0 ||
            target > TargetMask ||
            multiplier <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        var key = ModifierKey(domain, target);
        var grant = _grantCount;
        _grantDomain[grant] = domain;
        _grantTarget[grant] = target;
        _grantMultiplier[grant] = KernelFixedPoint.RatioUnits(multiplier);
        var previous = _lastGrantBySite[site];
        if (previous == 0)
        {
            _firstGrantBySite[site] = grant + 1;
        }
        else
        {
            _nextGrant[previous - 1] = grant + 1;
        }
        _lastGrantBySite[site] = grant + 1;
        RegisterModifierKey(key);
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

    /// <summary>
    /// Gate check by 1-based tech id (catalog order; 0 means none and never
    /// passes). Enablement gates consult this: a gated capability exists only
    /// while the culture's claim holds.
    /// </summary>
    internal bool CultureClaimsTech(int cultureId, int techId) =>
        techId > 0 && CultureClaimsSite(cultureId, techId - 1);

    internal FixedScalar Modifier(int cultureId, int domain, int target = 0)
    {
        if (cultureId <= 0 || domain <= 0 || target < 0 || target > TargetMask)
        {
            return 1;
        }

        var global = _modifiers.GetOrDefault(
            cultureId,
            ModifierKey(domain, 0),
            KernelFixedPoint.RatioScale);
        var modifier = target == 0
            ? global
            : KernelFixedPoint.MultiplyByRatio(
                global,
                _modifiers.GetOrDefault(
                    cultureId,
                    ModifierKey(domain, target),
                    KernelFixedPoint.RatioScale),
                0,
                KernelFixedPoint.RatioMaximumUnits);
        return KernelFixedPoint.RatioScalar(modifier);
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var world = simulation.World;
        var changed = false;
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
        {
            var revision = world.CultureFieldRevision(cultureId);
            if (_seenCultureFieldRevisions[cultureId] == revision)
            {
                continue;
            }

            RecomputeCulture(world, cultureId);
            _seenCultureFieldRevisions[cultureId] = revision;
            changed = true;
        }

        if (changed && _revision < long.MaxValue)
        {
            _revision++;
        }
    }

    private void RecomputeCulture(WorldState world, int cultureId)
    {
        global::System.Array.Clear(
            _claimedSites,
            cultureId * _siteCapacity,
            _siteCapacity);
        _claimedTechs[cultureId] = 0;
        for (var index = 0; index < _modifierKeyCount; index++)
        {
            _modifiers.Set(cultureId, _modifierKeys[index], KernelFixedPoint.RatioScale);
        }

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
        for (var link = _firstGrantBySite[site]; link > 0; link = _nextGrant[link - 1])
        {
            var grant = link - 1;

            var key = ModifierKey(_grantDomain[grant], _grantTarget[grant]);
            var current = _modifiers.GetOrDefault(
                cultureId,
                key,
                KernelFixedPoint.RatioScale);
            _modifiers.Set(
                cultureId,
                key,
                KernelFixedPoint.MultiplyByRatio(
                    current,
                    _grantMultiplier[grant],
                    0,
                    KernelFixedPoint.RatioMaximumUnits));
        }
    }

    private void RegisterModifierKey(int key)
    {
        for (var index = 0; index < _modifierKeyCount; index++)
        {
            if (_modifierKeys[index] == key)
            {
                return;
            }
        }

        if (_modifierKeyCount >= _modifierKeys.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _modifierKeys[_modifierKeyCount] = key;
        _modifierKeyCount++;
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
