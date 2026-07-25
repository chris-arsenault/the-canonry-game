namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class Simulation
{
    internal void RegisterDistrictBonus(int subtypeId, int bonusKindCode, FixedScalar magnitude) =>
        World.RegisterDistrictBonus(subtypeId, bonusKindCode, magnitude);
}

/// <summary>
/// Installed artifact districts are compact active templates. They retain the
/// authored identity needed to create a fresh artifact and relationship when
/// consumed, never the retired source occurrence.
/// </summary>
internal sealed partial class WorldState
{
    private int _districtCount;
    private int[] _districtArtifactKindIds = [];
    private int[] _districtArtifactSubtypeIds = [];
    private int[] _districtArtifactCultureIds = [];
    private int[] _districtHostIds = [];
    private int[] _districtQ = [];
    private int[] _districtR = [];
    private int[] _districtBonusKindCodes = [];
    private int[] _districtMagnitudes = [];
    private int[] _districtRelationshipKindIds = [];
    private int[] _districtRelationshipPolarityCodes = [];
    private int[] _districtRelationshipStrengths = [];
    private int[] _districtHealingMagnitudesByHost = [];
    private int _districtCultureModifierCount;
    private int[] _districtCultureModifierCultureIds = [];
    private int[] _districtCultureModifierQ = [];
    private int[] _districtCultureModifierR = [];
    private int[] _districtCultureModifierMagnitudes = [];

    private int _districtBonusRuleCount;
    private int[] _districtBonusSubtypeIds = [];
    private int[] _districtBonusKindRuleCodes = [];
    private FixedScalar[] _districtBonusRuleMagnitudes = [];

    internal int DistrictCount => _districtCount;

    internal void RegisterDistrictBonus(int subtypeId, int bonusKindCode, FixedScalar magnitude)
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

    internal bool TryGetDistrictBonus(int subtypeId, out int bonusKindCode, out FixedScalar magnitude)
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
        magnitude = 0;
        return false;
    }

    internal int DistrictHostId(int district) => _districtHostIds[district];

    internal int DistrictQ(int district) => _districtQ[district];

    internal int DistrictR(int district) => _districtR[district];

    internal int DistrictBonusKindCode(int district) => _districtBonusKindCodes[district];

    internal FixedScalar DistrictMagnitude(int district) =>
        DistrictMagnitudeValue(_districtBonusKindCodes[district], _districtMagnitudes[district]);

    internal FixedScalar DistrictHealingMagnitude(int hostId) =>
        (uint)hostId < (uint)_districtHealingMagnitudesByHost.Length
            ? KernelFixedPoint.AttributeScalar(_districtHealingMagnitudesByHost[hostId])
            : 0;

    internal int DistrictHealingMagnitudeUnits(int hostId) =>
        (uint)hostId < (uint)_districtHealingMagnitudesByHost.Length
            ? _districtHealingMagnitudesByHost[hostId]
            : 0;

    internal int DistrictCultureModifierCount => _districtCultureModifierCount;

    internal int DistrictCultureModifierCultureId(int modifier) =>
        _districtCultureModifierCultureIds[modifier];

    internal int DistrictCultureModifierQ(int modifier) => _districtCultureModifierQ[modifier];

    internal int DistrictCultureModifierR(int modifier) => _districtCultureModifierR[modifier];

    internal FixedScalar DistrictCultureModifierMagnitude(int modifier) =>
        KernelFixedPoint.CultureMassScalar(_districtCultureModifierMagnitudes[modifier]);

    internal int DistrictCultureModifierMagnitudeUnits(int modifier) =>
        _districtCultureModifierMagnitudes[modifier];

    private void InitializeDistrictStorage(SimulationOptions options)
    {
        _districtBonusSubtypeIds = new int[65];
        _districtBonusKindRuleCodes = new int[65];
        _districtBonusRuleMagnitudes = new FixedScalar[65];
        _districtArtifactKindIds = new int[options.DistrictCapacity + 1];
        _districtArtifactSubtypeIds = new int[options.DistrictCapacity + 1];
        _districtArtifactCultureIds = new int[options.DistrictCapacity + 1];
        _districtHostIds = new int[options.DistrictCapacity + 1];
        _districtQ = new int[options.DistrictCapacity + 1];
        _districtR = new int[options.DistrictCapacity + 1];
        _districtBonusKindCodes = new int[options.DistrictCapacity + 1];
        _districtMagnitudes = new int[options.DistrictCapacity + 1];
        _districtRelationshipKindIds = new int[options.DistrictCapacity + 1];
        _districtRelationshipPolarityCodes = new int[options.DistrictCapacity + 1];
        _districtRelationshipStrengths = new int[options.DistrictCapacity + 1];
        _districtHealingMagnitudesByHost = new int[options.EntityCapacity + 1];
        _districtCultureModifierCultureIds = new int[options.DistrictCapacity + 1];
        _districtCultureModifierQ = new int[options.DistrictCapacity + 1];
        _districtCultureModifierR = new int[options.DistrictCapacity + 1];
        _districtCultureModifierMagnitudes = new int[options.DistrictCapacity + 1];
    }

    internal int InstallDistrict(
        int artifactKindId,
        int artifactSubtypeId,
        int artifactCultureId,
        int hostId,
        int q,
        int r,
        int bonusKindCode,
        FixedScalar magnitude,
        int relationshipKindId,
        int relationshipPolarityCode,
        FixedScalar relationshipStrength)
    {
        if (_districtCount + 1 >= _districtHostIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _districtCount++;
        var district = _districtCount;
        _districtArtifactKindIds[district] = artifactKindId;
        _districtArtifactSubtypeIds[district] = artifactSubtypeId;
        _districtArtifactCultureIds[district] = artifactCultureId;
        _districtHostIds[district] = hostId;
        _districtQ[district] = q;
        _districtR[district] = r;
        _districtBonusKindCodes[district] = bonusKindCode;
        _districtMagnitudes[district] = DistrictMagnitudeUnits(bonusKindCode, magnitude);
        _districtRelationshipKindIds[district] = relationshipKindId;
        _districtRelationshipPolarityCodes[district] = relationshipPolarityCode;
        _districtRelationshipStrengths[district] =
            KernelFixedPoint.RelationshipStrengthUnits(relationshipStrength);
        AddDistrictModifier(district);
        return district;
    }

    internal bool TryRemoveNewestDistrictOf(int hostId, out DistrictReawakening template)
    {
        for (var district = _districtCount; district >= 1; district--)
        {
            if (_districtHostIds[district] == hostId)
            {
                template = new DistrictReawakening(
                    _districtArtifactKindIds[district],
                    _districtArtifactSubtypeIds[district],
                    _districtArtifactCultureIds[district],
                    _districtQ[district],
                    _districtR[district],
                    _districtRelationshipKindIds[district],
                    _districtRelationshipPolarityCodes[district],
                    KernelFixedPoint.RelationshipStrengthScalar(_districtRelationshipStrengths[district]));
                RemoveDistrictAt(district);
                return true;
            }
        }

        template = default;
        return false;
    }

    internal int ActiveDistrictCount(int hostId)
    {
        var count = 0;
        for (var district = 1; district <= _districtCount; district++)
        {
            if (_districtHostIds[district] == hostId)
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
            if (_districtHostIds[district] == hostId)
            {
                return true;
            }
        }

        return false;
    }

    internal void RemoveDistrictsOfHost(int hostId)
    {
        for (var district = _districtCount; district >= 1; district--)
        {
            if (_districtHostIds[district] == hostId)
            {
                RemoveDistrictAt(district);
            }
        }
    }

    private void RemoveDistrictAt(int district)
    {
        RemoveDistrictModifier(district);
        for (var row = district; row < _districtCount; row++)
        {
            _districtArtifactKindIds[row] = _districtArtifactKindIds[row + 1];
            _districtArtifactSubtypeIds[row] = _districtArtifactSubtypeIds[row + 1];
            _districtArtifactCultureIds[row] = _districtArtifactCultureIds[row + 1];
            _districtHostIds[row] = _districtHostIds[row + 1];
            _districtQ[row] = _districtQ[row + 1];
            _districtR[row] = _districtR[row + 1];
            _districtBonusKindCodes[row] = _districtBonusKindCodes[row + 1];
            _districtMagnitudes[row] = _districtMagnitudes[row + 1];
            _districtRelationshipKindIds[row] = _districtRelationshipKindIds[row + 1];
            _districtRelationshipPolarityCodes[row] = _districtRelationshipPolarityCodes[row + 1];
            _districtRelationshipStrengths[row] = _districtRelationshipStrengths[row + 1];
        }

        _districtArtifactKindIds[_districtCount] = 0;
        _districtArtifactSubtypeIds[_districtCount] = 0;
        _districtArtifactCultureIds[_districtCount] = 0;
        _districtHostIds[_districtCount] = 0;
        _districtQ[_districtCount] = 0;
        _districtR[_districtCount] = 0;
        _districtBonusKindCodes[_districtCount] = 0;
        _districtMagnitudes[_districtCount] = 0;
        _districtRelationshipKindIds[_districtCount] = 0;
        _districtRelationshipPolarityCodes[_districtCount] = 0;
        _districtRelationshipStrengths[_districtCount] = 0;
        _districtCount--;
    }

    private void AddDistrictModifier(int district)
    {
        var magnitude = _districtMagnitudes[district];
        switch (_districtBonusKindCodes[district])
        {
            case DistrictBonusKindCodes.Healing:
                var hostId = _districtHostIds[district];
                _districtHealingMagnitudesByHost[hostId] = KernelFixedPoint.SaturatingAdd(
                    _districtHealingMagnitudesByHost[hostId],
                    magnitude,
                    KernelFixedPoint.AttributeMinimumUnits,
                    KernelFixedPoint.AttributeMaximumUnits);
                break;
            case DistrictBonusKindCodes.Culture:
                AddCultureDistrictModifier(
                    _districtArtifactCultureIds[district],
                    _districtQ[district],
                    _districtR[district],
                    magnitude);
                break;
        }
    }

    private void RemoveDistrictModifier(int district)
    {
        var magnitude = _districtMagnitudes[district];
        switch (_districtBonusKindCodes[district])
        {
            case DistrictBonusKindCodes.Healing:
                var hostId = _districtHostIds[district];
                _districtHealingMagnitudesByHost[hostId] = global::System.Math.Max(
                    0,
                    _districtHealingMagnitudesByHost[hostId] - magnitude);
                break;
            case DistrictBonusKindCodes.Culture:
                RemoveCultureDistrictModifier(
                    _districtArtifactCultureIds[district],
                    _districtQ[district],
                    _districtR[district],
                    magnitude);
                break;
        }
    }

    private void AddCultureDistrictModifier(int cultureId, int q, int r, int magnitude)
    {
        for (var modifier = 1; modifier <= _districtCultureModifierCount; modifier++)
        {
            if (_districtCultureModifierCultureIds[modifier] == cultureId &&
                _districtCultureModifierQ[modifier] == q &&
                _districtCultureModifierR[modifier] == r)
            {
                _districtCultureModifierMagnitudes[modifier] = KernelFixedPoint.SaturatingAdd(
                    _districtCultureModifierMagnitudes[modifier],
                    magnitude,
                    0,
                    KernelFixedPoint.CultureMassMaximumUnits);
                return;
            }
        }

        if (_districtCultureModifierCount + 1 >= _districtCultureModifierCultureIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var added = ++_districtCultureModifierCount;
        _districtCultureModifierCultureIds[added] = cultureId;
        _districtCultureModifierQ[added] = q;
        _districtCultureModifierR[added] = r;
        _districtCultureModifierMagnitudes[added] = magnitude;
    }

    private void RemoveCultureDistrictModifier(int cultureId, int q, int r, int magnitude)
    {
        for (var modifier = 1; modifier <= _districtCultureModifierCount; modifier++)
        {
            if (_districtCultureModifierCultureIds[modifier] != cultureId ||
                _districtCultureModifierQ[modifier] != q ||
                _districtCultureModifierR[modifier] != r)
            {
                continue;
            }

            var nextMagnitude = global::System.Math.Max(
                0,
                _districtCultureModifierMagnitudes[modifier] - magnitude);
            if (nextMagnitude > 0)
            {
                _districtCultureModifierMagnitudes[modifier] = nextMagnitude;
                return;
            }

            var last = _districtCultureModifierCount;
            _districtCultureModifierCultureIds[modifier] = _districtCultureModifierCultureIds[last];
            _districtCultureModifierQ[modifier] = _districtCultureModifierQ[last];
            _districtCultureModifierR[modifier] = _districtCultureModifierR[last];
            _districtCultureModifierMagnitudes[modifier] = _districtCultureModifierMagnitudes[last];
            _districtCultureModifierCultureIds[last] = 0;
            _districtCultureModifierQ[last] = 0;
            _districtCultureModifierR[last] = 0;
            _districtCultureModifierMagnitudes[last] = 0;
            _districtCultureModifierCount--;
            return;
        }
    }

    private static int DistrictMagnitudeUnits(int bonusKindCode, FixedScalar magnitude) =>
        bonusKindCode == DistrictBonusKindCodes.Culture
            ? KernelFixedPoint.CultureMassUnits(magnitude)
            : KernelFixedPoint.AttributeUnits(magnitude);

    private static FixedScalar DistrictMagnitudeValue(int bonusKindCode, int magnitudeUnits) =>
        bonusKindCode == DistrictBonusKindCodes.Culture
            ? KernelFixedPoint.CultureMassScalar(magnitudeUnits)
            : KernelFixedPoint.AttributeScalar(magnitudeUnits);
}

internal readonly record struct DistrictReawakening(
    int ArtifactKindId,
    int ArtifactSubtypeId,
    int ArtifactCultureId,
    int Q,
    int R,
    int RelationshipKindId,
    int RelationshipPolarityCode,
    FixedScalar RelationshipStrength);
