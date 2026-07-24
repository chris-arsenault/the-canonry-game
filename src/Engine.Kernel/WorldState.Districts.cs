namespace Engine.Kernel;

internal sealed partial class Simulation
{
    internal void RegisterDistrictBonus(int subtypeId, int bonusKindCode, float magnitude) =>
        World.RegisterDistrictBonus(subtypeId, bonusKindCode, magnitude);
}

/// <summary>
/// Installed artifact districts: durable landscape state materialized when an
/// artifact installs into a settlement and goes historical. The district tile
/// belongs to the host's coverage; the record carries the bonus until a
/// reawakening returns the artifact to the world (docs/design/
/// artifact-districts.md).
/// </summary>
internal sealed partial class WorldState
{
    private int _districtCount;
    private int[] _districtSourceArtifactIds = [];
    private int[] _districtHostIds = [];
    private int[] _districtQ = [];
    private int[] _districtR = [];
    private int[] _districtBonusKindCodes = [];
    private float[] _districtMagnitudes = [];
    private byte[] _districtActive = [];

    private int _districtBonusRuleCount;
    private int[] _districtBonusSubtypeIds = [];
    private int[] _districtBonusKindRuleCodes = [];
    private float[] _districtBonusRuleMagnitudes = [];

    internal int DistrictCount => _districtCount;

    internal void RegisterDistrictBonus(int subtypeId, int bonusKindCode, float magnitude)
    {
        if (_districtBonusRuleCount + 1 >= _districtBonusSubtypeIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _districtBonusRuleCount++;
        _districtBonusSubtypeIds[_districtBonusRuleCount] = subtypeId;
        _districtBonusKindRuleCodes[_districtBonusRuleCount] = bonusKindCode;
        _districtBonusRuleMagnitudes[_districtBonusRuleCount] = magnitude;
    }

    internal bool TryGetDistrictBonus(int subtypeId, out int bonusKindCode, out float magnitude)
    {
        for (var rule = 1; rule <= _districtBonusRuleCount; rule++)
        {
            if (_districtBonusSubtypeIds[rule] == subtypeId)
            {
                bonusKindCode = _districtBonusKindRuleCodes[rule];
                magnitude = _districtBonusRuleMagnitudes[rule];
                return true;
            }
        }

        bonusKindCode = 0;
        magnitude = 0f;
        return false;
    }

    internal int DistrictSourceArtifactId(int district) => _districtSourceArtifactIds[district];

    internal int DistrictHostId(int district) => _districtHostIds[district];

    internal int DistrictQ(int district) => _districtQ[district];

    internal int DistrictR(int district) => _districtR[district];

    internal int DistrictBonusKindCode(int district) => _districtBonusKindCodes[district];

    internal float DistrictMagnitude(int district) => _districtMagnitudes[district];

    internal bool DistrictIsActive(int district) => _districtActive[district] != 0;

    private void InitializeDistrictStorage(SimulationOptions options)
    {
        _districtBonusSubtypeIds = new int[65];
        _districtBonusKindRuleCodes = new int[65];
        _districtBonusRuleMagnitudes = new float[65];
        _districtSourceArtifactIds = new int[options.DistrictCapacity + 1];
        _districtHostIds = new int[options.DistrictCapacity + 1];
        _districtQ = new int[options.DistrictCapacity + 1];
        _districtR = new int[options.DistrictCapacity + 1];
        _districtBonusKindCodes = new int[options.DistrictCapacity + 1];
        _districtMagnitudes = new float[options.DistrictCapacity + 1];
        _districtActive = new byte[options.DistrictCapacity + 1];
    }

    internal int InstallDistrict(
        int sourceArtifactId,
        int hostId,
        int q,
        int r,
        int bonusKindCode,
        float magnitude)
    {
        if (_districtCount + 1 >= _districtSourceArtifactIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _districtCount++;
        var district = _districtCount;
        _districtSourceArtifactIds[district] = sourceArtifactId;
        _districtHostIds[district] = hostId;
        _districtQ[district] = q;
        _districtR[district] = r;
        _districtBonusKindCodes[district] = bonusKindCode;
        _districtMagnitudes[district] = magnitude;
        _districtActive[district] = 1;
        return district;
    }

    /// <summary>Deactivates the newest active district of the host and
    /// returns its slot, or 0 when the host has none. The tile stays in the
    /// host's coverage; only the bonus and the sleeping artifact leave.</summary>
    internal int DeactivateNewestDistrictOf(int hostId)
    {
        for (var district = _districtCount; district >= 1; district--)
        {
            if (_districtActive[district] != 0 && _districtHostIds[district] == hostId)
            {
                _districtActive[district] = 0;
                return district;
            }
        }

        return 0;
    }

    internal int ActiveDistrictCount(int hostId)
    {
        var count = 0;
        for (var district = 1; district <= _districtCount; district++)
        {
            if (_districtActive[district] != 0 && _districtHostIds[district] == hostId)
            {
                count++;
            }
        }

        return count;
    }

    internal bool HasActiveDistrict(int hostId)
    {
        for (var district = 1; district <= _districtCount; district++)
        {
            if (_districtActive[district] != 0 && _districtHostIds[district] == hostId)
            {
                return true;
            }
        }

        return false;
    }
}
