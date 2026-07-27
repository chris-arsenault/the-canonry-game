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
    private int _districtHighWatermark;
    private bool[] _districtActive = [];
    private int[] _activeDistrictSlots = [];
    private int[] _activeDistrictPositions = [];
    private int[] _freeDistrictSlots = [];
    private int _freeDistrictSlotCount;
    private int[] _districtArtifactKindIds = [];
    private int[] _districtArtifactSubtypeIds = [];
    private int[] _districtArtifactCultureIds = [];
    private int[] _districtHostOccurrenceHandles = [];
    private int[] _districtCellIds = [];
    private int[] _firstDistrictByHost = [];
    private int[] _nextDistrictByHost = [];
    private int[] _previousDistrictByHost = [];
    private int[] _districtBonusKindCodes = [];
    private int[] _districtMagnitudes = [];
    private int[] _districtRelationshipKindIds = [];
    private int[] _districtRelationshipPolarityCodes = [];
    private int[] _districtRelationshipStrengths = [];
    private int[] _districtHealingMagnitudesByHost = [];
    private int _districtCultureModifierCount;
    private int[] _districtCultureModifierCultureIds = [];
    private int[] _districtCultureModifierCellIds = [];
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

    internal int DistrictHostId(int district)
    {
        if (!TryResolveEntityOccurrence(_districtHostOccurrenceHandles[district], out var hostId))
        {
            throw new global::System.InvalidOperationException();
        }

        return hostId;
    }

    internal int DistrictQ(int district) => _cells.Q(_districtCellIds[district]);

    internal int DistrictR(int district) => _cells.R(_districtCellIds[district]);

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

    internal int DistrictCultureModifierQ(int modifier) =>
        _cells.Q(_districtCultureModifierCellIds[modifier]);

    internal int DistrictCultureModifierR(int modifier) =>
        _cells.R(_districtCultureModifierCellIds[modifier]);

    internal FixedScalar DistrictCultureModifierMagnitude(int modifier) =>
        KernelFixedPoint.CultureMassScalar(_districtCultureModifierMagnitudes[modifier]);

    internal int DistrictCultureModifierMagnitudeUnits(int modifier) =>
        _districtCultureModifierMagnitudes[modifier];

    private void InitializeDistrictStorage(SimulationOptions options)
    {
        _districtBonusSubtypeIds = new int[65];
        _districtBonusKindRuleCodes = new int[65];
        _districtBonusRuleMagnitudes = new FixedScalar[65];
        _districtActive = new bool[options.DistrictCapacity + 1];
        _activeDistrictSlots = new int[options.DistrictCapacity];
        _activeDistrictPositions = new int[options.DistrictCapacity + 1];
        _freeDistrictSlots = new int[options.DistrictCapacity];
        _districtArtifactKindIds = new int[options.DistrictCapacity + 1];
        _districtArtifactSubtypeIds = new int[options.DistrictCapacity + 1];
        _districtArtifactCultureIds = new int[options.DistrictCapacity + 1];
        _districtHostOccurrenceHandles = new int[options.DistrictCapacity + 1];
        _districtCellIds = new int[options.DistrictCapacity + 1];
        _firstDistrictByHost = new int[options.EntityCapacity + 1];
        _nextDistrictByHost = new int[options.DistrictCapacity + 1];
        _previousDistrictByHost = new int[options.DistrictCapacity + 1];
        _districtBonusKindCodes = new int[options.DistrictCapacity + 1];
        _districtMagnitudes = new int[options.DistrictCapacity + 1];
        _districtRelationshipKindIds = new int[options.DistrictCapacity + 1];
        _districtRelationshipPolarityCodes = new int[options.DistrictCapacity + 1];
        _districtRelationshipStrengths = new int[options.DistrictCapacity + 1];
        _districtHealingMagnitudesByHost = new int[options.EntityCapacity + 1];
        _districtCultureModifierCultureIds = new int[options.DistrictCapacity + 1];
        _districtCultureModifierCellIds = new int[options.DistrictCapacity + 1];
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
        EnsureEntity(hostId);
        var district = AllocateDistrictSlot();
        _districtActive[district] = true;
        _districtArtifactKindIds[district] = artifactKindId;
        _districtArtifactSubtypeIds[district] = artifactSubtypeId;
        _districtArtifactCultureIds[district] = artifactCultureId;
        _districtHostOccurrenceHandles[district] = EntityOccurrenceHandle(hostId);
        _districtCellIds[district] = RequireCellId(PlaneCodes.Physical, q, r, z: 0);
        _districtBonusKindCodes[district] = bonusKindCode;
        _districtMagnitudes[district] = DistrictMagnitudeUnits(bonusKindCode, magnitude);
        _districtRelationshipKindIds[district] = relationshipKindId;
        _districtRelationshipPolarityCodes[district] = relationshipPolarityCode;
        _districtRelationshipStrengths[district] =
            KernelFixedPoint.RelationshipStrengthUnits(relationshipStrength);
        InsertActiveDistrict(district);
        InsertDistrictHostIndex(district, hostId);
        AddDistrictModifier(district);
        return district;
    }

    internal bool TryRemoveNewestDistrictOf(int hostId, out DistrictReawakening template)
    {
        var district = _firstDistrictByHost[hostId];
        if (TryReadDistrictReawakening(district, out template))
        {
            RemoveDistrictAt(district, hostId);
            return true;
        }

        return false;
    }

    internal bool TryPeekNewestDistrictOf(
        int hostId,
        out DistrictReawakening template) =>
        TryReadDistrictReawakening(
            _firstDistrictByHost[hostId],
            out template);

    private bool TryReadDistrictReawakening(
        int district,
        out DistrictReawakening template)
    {
        if (district <= 0)
        {
            template = default;
            return false;
        }

        template = new DistrictReawakening(
            _districtArtifactKindIds[district],
            _districtArtifactSubtypeIds[district],
            _districtArtifactCultureIds[district],
            DistrictQ(district),
            DistrictR(district),
            _districtRelationshipKindIds[district],
            _districtRelationshipPolarityCodes[district],
            KernelFixedPoint.RelationshipStrengthScalar(
                _districtRelationshipStrengths[district]));
        return true;
    }

    internal int ActiveDistrictCount(int hostId)
    {
        var count = 0;
        for (var district = _firstDistrictByHost[hostId]; district > 0; district = _nextDistrictByHost[district])
        {
            count++;
        }

        return count;
    }

    internal bool HasActiveDistrict(int hostId)
    {
        return _firstDistrictByHost[hostId] > 0;
    }

    internal void RemoveDistrictsOfHost(int hostId)
    {
        while (_firstDistrictByHost[hostId] > 0)
        {
            RemoveDistrictAt(_firstDistrictByHost[hostId], hostId);
        }
    }

    private void RemoveDistrictAt(int district, int hostId)
    {
        RemoveDistrictModifier(district);
        RemoveDistrictHostIndex(district, hostId);
        RemoveActiveDistrict(district);
        ClearDistrictSlot(district);
        _freeDistrictSlots[_freeDistrictSlotCount++] = district;
    }

    private int AllocateDistrictSlot()
    {
        if (_freeDistrictSlotCount > 0)
        {
            return _freeDistrictSlots[--_freeDistrictSlotCount];
        }

        if (_districtHighWatermark >= _districtActive.Length - 1)
        {
            throw new global::System.InvalidOperationException();
        }

        return ++_districtHighWatermark;
    }

    private void InsertActiveDistrict(int district)
    {
        _activeDistrictSlots[_districtCount] = district;
        _activeDistrictPositions[district] = _districtCount + 1;
        _districtCount++;
    }

    private void RemoveActiveDistrict(int district)
    {
        var position = _activeDistrictPositions[district] - 1;
        var lastPosition = --_districtCount;
        var movedDistrict = _activeDistrictSlots[lastPosition];
        _activeDistrictSlots[position] = movedDistrict;
        _activeDistrictPositions[movedDistrict] = position + 1;
        _activeDistrictSlots[lastPosition] = 0;
        _activeDistrictPositions[district] = 0;
        _districtActive[district] = false;
    }

    private void InsertDistrictHostIndex(int district, int hostId)
    {
        var current = _firstDistrictByHost[hostId];
        _nextDistrictByHost[district] = current;
        if (current > 0)
        {
            _previousDistrictByHost[current] = district;
        }

        _firstDistrictByHost[hostId] = district;
    }

    private void RemoveDistrictHostIndex(int district, int hostId)
    {
        var previous = _previousDistrictByHost[district];
        var next = _nextDistrictByHost[district];
        if (previous > 0)
        {
            _nextDistrictByHost[previous] = next;
        }
        else
        {
            _firstDistrictByHost[hostId] = next;
        }

        if (next > 0)
        {
            _previousDistrictByHost[next] = previous;
        }

        _previousDistrictByHost[district] = 0;
        _nextDistrictByHost[district] = 0;
    }

    private void ClearDistrictSlot(int district)
    {
        _districtArtifactKindIds[district] = 0;
        _districtArtifactSubtypeIds[district] = 0;
        _districtArtifactCultureIds[district] = 0;
        _districtHostOccurrenceHandles[district] = 0;
        _districtCellIds[district] = 0;
        _districtBonusKindCodes[district] = 0;
        _districtMagnitudes[district] = 0;
        _districtRelationshipKindIds[district] = 0;
        _districtRelationshipPolarityCodes[district] = 0;
        _districtRelationshipStrengths[district] = 0;
    }

    private void AddDistrictModifier(int district)
    {
        var magnitude = _districtMagnitudes[district];
        switch (_districtBonusKindCodes[district])
        {
            case DistrictBonusKindCodes.Healing:
                var hostId = DistrictHostId(district);
                _districtHealingMagnitudesByHost[hostId] = KernelFixedPoint.SaturatingAdd(
                    _districtHealingMagnitudesByHost[hostId],
                    magnitude,
                    KernelFixedPoint.AttributeMinimumUnits,
                    KernelFixedPoint.AttributeMaximumUnits);
                break;
            case DistrictBonusKindCodes.Culture:
                AddCultureDistrictModifier(
                    _districtArtifactCultureIds[district],
                    _districtCellIds[district],
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
                var hostId = DistrictHostId(district);
                _districtHealingMagnitudesByHost[hostId] = global::System.Math.Max(
                    0,
                    _districtHealingMagnitudesByHost[hostId] - magnitude);
                break;
            case DistrictBonusKindCodes.Culture:
                RemoveCultureDistrictModifier(
                    _districtArtifactCultureIds[district],
                    _districtCellIds[district],
                    magnitude);
                break;
        }
    }

    private void AddCultureDistrictModifier(int cultureId, int cellId, int magnitude)
    {
        for (var modifier = 1; modifier <= _districtCultureModifierCount; modifier++)
        {
            if (_districtCultureModifierCultureIds[modifier] == cultureId &&
                _districtCultureModifierCellIds[modifier] == cellId)
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
        _districtCultureModifierCellIds[added] = cellId;
        _districtCultureModifierMagnitudes[added] = magnitude;
    }

    private void RemoveCultureDistrictModifier(int cultureId, int cellId, int magnitude)
    {
        for (var modifier = 1; modifier <= _districtCultureModifierCount; modifier++)
        {
            if (_districtCultureModifierCultureIds[modifier] != cultureId ||
                _districtCultureModifierCellIds[modifier] != cellId)
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
            _districtCultureModifierCellIds[modifier] = _districtCultureModifierCellIds[last];
            _districtCultureModifierMagnitudes[modifier] = _districtCultureModifierMagnitudes[last];
            _districtCultureModifierCultureIds[last] = 0;
            _districtCultureModifierCellIds[last] = 0;
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
