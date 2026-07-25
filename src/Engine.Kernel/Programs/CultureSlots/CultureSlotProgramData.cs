namespace Engine.Kernel;

using Engine.Constants;
using Engine.Kernel.Programs;

internal sealed partial class CultureSlotProgramData : IKernelProgramData
{
    private readonly int[] _cultureIds;
    private readonly int[] _slotIds;
    private readonly int[] _polarityCodes;
    private readonly int[] _allowedKindStarts;
    private readonly int[] _allowedKindCounts;
    private readonly int[] _allowedKindIds;
    private readonly int[] _ruleStarts;
    private readonly int[] _ruleCounts;
    private readonly int[] _ruleSignalCodes;
    private readonly int[] _ruleRelationshipKindIds;
    private readonly FixedScalar[] _ruleWeights;
    private readonly FixedScalar[] _minimumScores;
    private readonly int[] _minimumTenureTicks;
    private readonly FixedScalar[] _displacementMargins;
    private readonly FixedScalar[] _entityInfluenceMultiplierDeltas;
    private readonly FixedScalar[] _cultureInfluenceMultiplierDeltas;
    private int _slotCount;
    private int _allowedKindCount;
    private int _ruleCount;

    public CultureSlotProgramData(
        int slotCapacity,
        int allowedKindCapacity,
        int ruleCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(slotCapacity);
        NumericGuard.ThrowIfNegativeOrZero(allowedKindCapacity);
        NumericGuard.ThrowIfNegativeOrZero(ruleCapacity);
        _cultureIds = new int[slotCapacity + 1];
        _slotIds = new int[slotCapacity + 1];
        _polarityCodes = new int[slotCapacity + 1];
        _allowedKindStarts = new int[slotCapacity + 1];
        _allowedKindCounts = new int[slotCapacity + 1];
        _allowedKindIds = new int[allowedKindCapacity + 1];
        _ruleStarts = new int[slotCapacity + 1];
        _ruleCounts = new int[slotCapacity + 1];
        _ruleSignalCodes = new int[ruleCapacity + 1];
        _ruleRelationshipKindIds = new int[ruleCapacity + 1];
        _ruleWeights = new FixedScalar[ruleCapacity + 1];
        _minimumScores = new FixedScalar[slotCapacity + 1];
        _minimumTenureTicks = new int[slotCapacity + 1];
        _displacementMargins = new FixedScalar[slotCapacity + 1];
        _entityInfluenceMultiplierDeltas = new FixedScalar[slotCapacity + 1];
        _cultureInfluenceMultiplierDeltas = new FixedScalar[slotCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.CultureSlots;

    public int SchemaVersion => 1;

    public int SlotCount => _slotCount;

    internal int SlotCapacity => _cultureIds.Length - 1;

    public int RegisterSlot(
        int cultureId,
        int slotId,
        int polarityCode,
        FixedScalar minimumScore,
        int minimumTenureTicks,
        FixedScalar displacementMargin,
        FixedScalar entityInfluenceMultiplierDelta,
        FixedScalar cultureInfluenceMultiplierDelta)
    {
        ValidatePositive(cultureId);
        ValidatePositive(slotId);
        ValidatePolarity(polarityCode);
        NumericGuard.ThrowIfNegative(minimumScore);
        NumericGuard.ThrowIfNegative(minimumTenureTicks);
        NumericGuard.ThrowIfNegative(displacementMargin);
        if (_slotCount + 1 >= _cultureIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _slotCount++;
        var slot = _slotCount;
        _cultureIds[slot] = cultureId;
        _slotIds[slot] = slotId;
        _polarityCodes[slot] = polarityCode;
        _minimumScores[slot] = minimumScore;
        _minimumTenureTicks[slot] = minimumTenureTicks;
        _displacementMargins[slot] = displacementMargin;
        _entityInfluenceMultiplierDeltas[slot] = entityInfluenceMultiplierDelta;
        _cultureInfluenceMultiplierDeltas[slot] = cultureInfluenceMultiplierDelta;
        return slot;
    }

    public void AddAllowedKind(int slot, int kindId)
    {
        EnsureSlot(slot);
        ValidatePositive(kindId);
        if (_allowedKindCount + 1 >= _allowedKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        if (_allowedKindCounts[slot] == 0)
        {
            _allowedKindStarts[slot] = _allowedKindCount + 1;
        }

        _allowedKindCount++;
        _allowedKindIds[_allowedKindCount] = kindId;
        _allowedKindCounts[slot]++;
    }

    public void AddScoreRule(
        int slot,
        int signalCode,
        int relationshipKindId,
        FixedScalar weight)
    {
        EnsureSlot(slot);
        ValidateScoreSignal(signalCode);
        if (RequiresRelationship(signalCode))
        {
            ValidatePositive(relationshipKindId);
        }

        if (_ruleCount + 1 >= _ruleSignalCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        if (_ruleCounts[slot] == 0)
        {
            _ruleStarts[slot] = _ruleCount + 1;
        }

        _ruleCount++;
        _ruleSignalCodes[_ruleCount] = signalCode;
        _ruleRelationshipKindIds[_ruleCount] = relationshipKindId;
        _ruleWeights[_ruleCount] = weight;
        _ruleCounts[slot]++;
    }

    public int CultureId(int slot)
    {
        EnsureSlot(slot);
        return _cultureIds[slot];
    }

    public int SlotId(int slot)
    {
        EnsureSlot(slot);
        return _slotIds[slot];
    }

    public int PolarityCode(int slot)
    {
        EnsureSlot(slot);
        return _polarityCodes[slot];
    }

    public FixedScalar MinimumScore(int slot)
    {
        EnsureSlot(slot);
        return _minimumScores[slot];
    }

    public int MinimumTenureTicks(int slot)
    {
        EnsureSlot(slot);
        return _minimumTenureTicks[slot];
    }

    public FixedScalar DisplacementMargin(int slot)
    {
        EnsureSlot(slot);
        return _displacementMargins[slot];
    }

    public FixedScalar EntityInfluenceMultiplierDelta(int slot)
    {
        EnsureSlot(slot);
        return _entityInfluenceMultiplierDeltas[slot];
    }

    public FixedScalar CultureInfluenceMultiplierDelta(int slot)
    {
        EnsureSlot(slot);
        return _cultureInfluenceMultiplierDeltas[slot];
    }

    public bool KindAllowed(int slot, int kindId)
    {
        EnsureSlot(slot);
        var count = _allowedKindCounts[slot];
        if (count == 0)
        {
            return true;
        }

        var start = _allowedKindStarts[slot];
        for (var i = 0; i < count; i++)
        {
            if (_allowedKindIds[start + i] == kindId)
            {
                return true;
            }
        }

        return false;
    }

    public int AllowedKindCount(int slot)
    {
        EnsureSlot(slot);
        return _allowedKindCounts[slot];
    }

    public int AllowedKindId(int slot, int offset)
    {
        EnsureSlot(slot);
        if ((uint)offset >= (uint)_allowedKindCounts[slot])
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(offset));
        }

        return _allowedKindIds[_allowedKindStarts[slot] + offset];
    }

    public int RuleCount(int slot)
    {
        EnsureSlot(slot);
        return _ruleCounts[slot];
    }

    public int RuleIndex(int slot, int offset)
    {
        EnsureSlot(slot);
        if ((uint)offset >= (uint)_ruleCounts[slot])
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(offset));
        }

        return _ruleStarts[slot] + offset;
    }

    public int RuleSignalCode(int rule) => _ruleSignalCodes[rule];

    public int RuleRelationshipKindId(int rule) => _ruleRelationshipKindIds[rule];

    public FixedScalar RuleWeight(int rule) => _ruleWeights[rule];

    private void EnsureSlot(int slot)
    {
        if ((uint)slot == 0u || slot > _slotCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static bool RequiresRelationship(int signalCode) =>
        signalCode is CultureSlotScoreSignalCodes.OutgoingRelationshipStrength or
            CultureSlotScoreSignalCodes.IncomingRelationshipStrength;

    private static void ValidatePositive(int value)
    {
        NumericGuard.ThrowIfNegativeOrZero(value);
    }

    private static void ValidatePolarity(int polarityCode)
    {
        if (polarityCode is RelationshipPolarityCodes.Positive or
            RelationshipPolarityCodes.Neutral or
            RelationshipPolarityCodes.Negative)
        {
            return;
        }

        throw new global::System.ArgumentOutOfRangeException(nameof(polarityCode));
    }

    private static void ValidateScoreSignal(int signalCode)
    {
        if (signalCode is >= CultureSlotScoreSignalCodes.ActiveEntity and <= CultureSlotScoreSignalCodes.SemanticInfluenceProduction)
        {
            return;
        }

        throw new global::System.ArgumentOutOfRangeException(nameof(signalCode));
    }
}
