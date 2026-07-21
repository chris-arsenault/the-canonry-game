namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.Domain.Config;
using Engine.Domain.Startup;
using Engine.Gameplay;
using Engine.Kinds;
using Engine.WorldGen;

using IceRemembers;

/// <summary>
/// Seed-time placement taste: the same kind and culture weights the kernel
/// scores at runtime, evaluated against the blueprint's hex facts before
/// the kernel exists. Axis values mirror the kernel's normalization
/// (quantized byte facts over 255, capacity from the authored formula,
/// coast from the land/water divide) so a hex the seed likes is a hex the
/// running world likes.
/// </summary>
internal sealed class IceRemembersSeedTaste
{
    private const float CapacityNormal = 4f;
    private const float ScoreScale = 60f;

    private static readonly int[] AxisFactCodes =
    [
        -1,
        PhysicalHexFactCodes.Temperature,
        PhysicalHexFactCodes.Moisture,
        PhysicalHexFactCodes.Elevation,
        PhysicalHexFactCodes.Relief,
        PhysicalHexFactCodes.IceThickness,
        PhysicalHexFactCodes.RiverCoverage,
        PhysicalHexFactCodes.FireMean,
        PhysicalHexFactCodes.AuroraMean,
        PhysicalHexFactCodes.PressureMean,
        -1,
        -1,
    ];

    private readonly Dictionary<HexCoord, byte[]> _facts = [];
    private readonly Dictionary<HexCoord, bool> _water = [];
    private readonly Dictionary<(string Kind, string Culture), float[]> _weights = [];
    private readonly byte _coldFloor;
    private readonly byte _warmPeak;
    private readonly PhysicalCapacityFormulaConfig _formula;

    public IceRemembersSeedTaste(IReadOnlyList<DomainPhysicalGeographyCell> cells)
    {
        _formula = IceRemembersDefinition.PhysicalCapacityFormula;
        _coldFloor = PhysicalHexFactCodes.FromIntensity(_formula.ColdFloorTemperature);
        _warmPeak = PhysicalHexFactCodes.FromIntensity(_formula.WarmPeakTemperature);
        var waterTerrains = WaterTerrainNames();
        foreach (var cell in cells)
        {
            var coord = new HexCoord(cell.Q, cell.R);
            var facts = new byte[PhysicalHexFactCodes.Count];
            DomainPhysicalGeographyFacts.Quantize(cell, facts);
            _facts[coord] = facts;
            _water[coord] = waterTerrains.Contains(cell.BaseTerrain);
        }
    }

    /// <summary>
    /// The taste score of a hex for a kind placed under a culture: each
    /// signed weight times its normalized axis value, summed and scaled to
    /// the seed placement score range.
    /// </summary>
    public float Score(EntityKind kind, string? culture, HexCoord coord)
    {
        if (!_facts.TryGetValue(coord, out var facts))
        {
            return 0f;
        }

        var weights = CombinedWeights(kind, culture);
        var score = 0f;
        for (var axis = 0; axis < PlacementTasteAxisCodes.Count; axis++)
        {
            var weight = weights[axis];
            if (weight != 0f)
            {
                score += weight * AxisValue(axis, coord, facts);
            }
        }

        return score * ScoreScale;
    }

    /// <summary>
    /// The derived carrying capacity of a hex (authored formula over its
    /// quantized facts), for validators and instruments that ask whether
    /// ground is livable rather than whether a kind likes it.
    /// </summary>
    public float Capacity(HexCoord coord) =>
        _facts.TryGetValue(coord, out var facts) ? RawCapacity(facts) : 0f;

    private float AxisValue(int axis, HexCoord coord, byte[] facts)
    {
        var factCode = AxisFactCodes[axis];
        if (factCode >= 0)
        {
            return facts[factCode] / 255f;
        }

        if (axis == PlacementTasteAxisCodes.Capacity)
        {
            return CapacityValue(facts);
        }

        return axis == PlacementTasteAxisCodes.WaterFraction
            ? 1f - (facts[PhysicalHexFactCodes.LandFraction] / 255f)
            : CoastValue(coord);
    }

    private float CapacityValue(byte[] facts) =>
        Math.Clamp(RawCapacity(facts) / CapacityNormal, 0f, 1f);

    private float RawCapacity(byte[] facts)
    {
        var warmth = facts[PhysicalHexFactCodes.Temperature] +
            (facts[PhysicalHexFactCodes.Geothermal] * _formula.GeothermalBlend);
        var suitability = Math.Clamp((warmth - _coldFloor) / (float)(_warmPeak - _coldFloor), 0f, 1f);
        var land = facts[PhysicalHexFactCodes.LandFraction] / 255f;
        var fresh = 1f +
            (_formula.FreshwaterWeight * facts[PhysicalHexFactCodes.RiverCoverage] / 255f) +
            (_formula.FertileWeight * facts[PhysicalHexFactCodes.FloodplainCoverage] / 255f);
        var reliefExcess = Math.Max(
            0f,
            (facts[PhysicalHexFactCodes.Relief] / 255f) - _formula.ReliefComfort);
        var shelter = Math.Clamp(
            1f -
            (_formula.ReliefPenalty * reliefExcess) -
            (_formula.RoughnessPenalty * facts[PhysicalHexFactCodes.Roughness] / 255f),
            0f,
            1f);
        return (_formula.LandScale * suitability * land * fresh * shelter) +
            (_formula.MarineScale * suitability * (1f - land));
    }

    private float CoastValue(HexCoord coord)
    {
        if (!_water.TryGetValue(coord, out var water))
        {
            return 0f;
        }

        foreach (var neighbor in coord.Neighbors())
        {
            if (_water.TryGetValue(neighbor, out var neighborWater) && neighborWater != water)
            {
                return 1f;
            }
        }

        return 0f;
    }

    private float[] CombinedWeights(EntityKind kind, string? culture)
    {
        var key = (kind.Value, culture ?? string.Empty);
        if (_weights.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var weights = new float[PlacementTasteAxisCodes.Count];
        Accumulate(weights, IceRemembersEntities.Entity(kind).PlacementTaste);
        Accumulate(weights, IceRemembersPlacementTastes.ForCultureName(culture));
        _weights[key] = weights;
        return weights;
    }

    private static void Accumulate(float[] weights, IReadOnlyList<PlacementTasteWeightConfig> taste)
    {
        foreach (var weight in taste)
        {
            weights[PlacementTasteAxisNames.CodeOf(weight.Axis)] += weight.Weight;
        }
    }

    private static HashSet<string> WaterTerrainNames()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var terrain in IceRemembersTerrainCatalog.DomainTerrains)
        {
            if (terrain.IsWater)
            {
                names.Add(terrain.Name);
            }
        }

        return names;
    }

}
