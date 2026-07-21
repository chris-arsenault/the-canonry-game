namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.WorldGen;

using IceRemembers;

/// <summary>
/// The map-to-hex projection: reduces a hex's 24 microtriangles and its
/// aggregate to the gameplay contract - primary terrain, directional
/// crossing word, band/flag field word, and initial transient cover.
///
/// The terrain ladder is an ordered list of named predicates over the
/// surfaces, landforms, water kinds, and coverages that worldgen concluded.
/// Magic never decides terrain except where it is province-scale (aurora
/// fields); elsewhere magic enters only through bands and flags.
/// </summary>
internal static class IceRemembersHexProjection
{
    // Water partition and water-ladder cuts (counts out of 24 triangles).
    private const int WaterPartitionCoverage = 12;
    private const int FjordCut = 3;
    private const int ReefCut = 3;

    // Land-ladder cuts.
    private const int AuroraProvinceCut = 10;
    private const int HotSpringVentCut = 2;
    private const int HotSpringSurfaceCut = 5;
    private const int BasaltSurfaceCut = 8;
    private const int RiverLandCut = 8;
    private const int FloodplainMaximumRelief = 175;
    private const int FloodplainMaximumRoughness = 62;
    private const int CragPeakCut = 2;
    private const int CragSurfaceCut = 14;
    private const int CragReliefCut = 240;
    private const int CragRoughnessCut = 130;
    private const int IceSurfaceCut = 10;
    private const int IcePermanentCoverageCut = 14;
    private const int VerdantSurfaceCut = 6;
    private const int BarrenSurfaceCut = 8;

    // Initial sea-ice cover: frozen-water surfaces dominating a water hex.
    private const int SeaIceCoverCut = 10;

    // Flag cuts.
    private const int ThermalVentCut = 1;
    private const int ThermalSurfaceCut = 2;
    private const int ThermalGeothermalCut = 400;

    private static readonly ulong AnomalyFeatureMask =
        IceRemembersPhysicalGeography.FeatureFlag(IceRemembersPhysicalFeature.PressureGodLocus) |
        IceRemembersPhysicalGeography.FeatureFlag(IceRemembersPhysicalFeature.RogueImpact) |
        IceRemembersPhysicalGeography.FeatureFlag(IceRemembersPhysicalFeature.InterferenceLattice) |
        IceRemembersPhysicalGeography.FeatureFlag(IceRemembersPhysicalFeature.ThermalWarRim) |
        IceRemembersPhysicalGeography.FeatureFlag(IceRemembersPhysicalFeature.SteamFracture) |
        IceRemembersPhysicalGeography.FeatureFlag(IceRemembersPhysicalFeature.PressureAuroraBreach);

    private static readonly byte[] BaseSurfaceGroups = CreateBaseSurfaceGroups();
    private static readonly byte[] FinalSurfaceGroups = CreateFinalSurfaceGroups();

    /// <summary>
    /// Natural-surface vote groups for the land and water ladders. One row
    /// per surface in <see cref="CreateBaseSurfaceGroups"/> keeps the
    /// affinity table linear in the surface palette.
    /// </summary>
    private enum SurfaceGroup
    {
        None,
        Verdant,
        RiverLand,
        Barren,
        Crag,
        Ice,
        Fjord,
        Reef,
        Shallow,
        Deep,
        FrozenWater,
        Polynya,
    }

    private enum MagicGroup
    {
        None,
        Aurora,
        WarmFire,
        Basalt,
    }

    public readonly record struct Result(
        IceRemembersTerrain Terrain,
        int CrossingWord,
        int FieldWord,
        IceRemembersTerrain? InitialCover);

    public static Result Project(
        PhysicalGeographyField field,
        IceRemembersMicrogeographyMap magic,
        int hexIndex,
        PhysicalHexAggregate aggregate)
    {
        var facts = HexFacts.Create(field, magic, hexIndex, aggregate);
        var terrain = ClassifyTerrain(facts);
        return new Result(
            terrain,
            BuildCrossingWord(aggregate),
            BuildFieldWord(terrain, facts),
            SelectInitialCover(terrain, facts));
    }

    private static IceRemembersTerrain ClassifyTerrain(in HexFacts facts)
    {
        return facts.Aggregate.WaterCoverage >= WaterPartitionCoverage
            ? ClassifyWater(facts)
            : ClassifyLand(facts);
    }

    private static IceRemembersTerrain ClassifyWater(in HexFacts facts)
    {
        if (facts.Aggregate.LakeCoverage >= facts.Aggregate.OceanCoverage)
        {
            return IceRemembersTerrain.Lake;
        }

        var fjordEvidence = Math.Max(facts.FjordCount, facts.Count(SurfaceGroup.Fjord));
        if (fjordEvidence >= FjordCut && facts.Aggregate.CoastEdgeMask != 0)
        {
            return IceRemembersTerrain.Fjord;
        }

        if (facts.Count(SurfaceGroup.Reef) >= ReefCut)
        {
            return IceRemembersTerrain.Reef;
        }

        return facts.Count(SurfaceGroup.Shallow) >= facts.Count(SurfaceGroup.Deep)
            ? IceRemembersTerrain.Shelf
            : IceRemembersTerrain.Ocean;
    }

    private static IceRemembersTerrain ClassifyLand(in HexFacts facts)
    {
        return TryClassifyMagicLand(facts) ??
            TryClassifyReliefLand(facts) ??
            ClassifyOrdinaryLand(facts);
    }

    // Province-scale aurora and dominant geothermal/volcanic ground are the
    // only places magic decides terrain.
    private static IceRemembersTerrain? TryClassifyMagicLand(in HexFacts facts)
    {
        if (facts.MagicCount(MagicGroup.Aurora) >= AuroraProvinceCut)
        {
            return IceRemembersTerrain.AuroraField;
        }

        if (facts.VentCount >= HotSpringVentCut ||
            facts.MagicCount(MagicGroup.WarmFire) >= HotSpringSurfaceCut)
        {
            return IceRemembersTerrain.HotSprings;
        }

        return facts.MagicCount(MagicGroup.Basalt) >= BasaltSurfaceCut
            ? IceRemembersTerrain.Basalt
            : null;
    }

    // River lands beat relief; relief beats flat groups.
    private static IceRemembersTerrain? TryClassifyReliefLand(in HexFacts facts)
    {
        // A floodplain is where a river drops what it carries, which only
        // happens on low ground. Counting river surfaces alone called every
        // steep ravine a floodplain, and once relief arrived that was three
        // quarters of them: channels sit on the steepest ground there is.
        if (facts.Count(SurfaceGroup.RiverLand) >= RiverLandCut &&
            facts.Aggregate.Relief <= FloodplainMaximumRelief &&
            facts.Aggregate.Roughness <= FloodplainMaximumRoughness)
        {
            return IceRemembersTerrain.Floodplain;
        }

        var crags = facts.Aggregate.PeakCount >= CragPeakCut ||
            facts.Count(SurfaceGroup.Crag) >= CragSurfaceCut ||
            (facts.Aggregate.Relief >= CragReliefCut && facts.Aggregate.Roughness >= CragRoughnessCut);
        return crags ? IceRemembersTerrain.Crags : null;
    }

    private static IceRemembersTerrain ClassifyOrdinaryLand(in HexFacts facts)
    {
        if (facts.Count(SurfaceGroup.Ice) >= IceSurfaceCut ||
            facts.Aggregate.PermanentIceCoverage >= IcePermanentCoverageCut)
        {
            return IceRemembersTerrain.Ice;
        }

        if (facts.Count(SurfaceGroup.Verdant) >= VerdantSurfaceCut)
        {
            return IceRemembersTerrain.Verdant;
        }

        return facts.Count(SurfaceGroup.Barren) >= BarrenSurfaceCut
            ? IceRemembersTerrain.Wastes
            : IceRemembersTerrain.Tundra;
    }

    private static IceRemembersTerrain? SelectInitialCover(IceRemembersTerrain terrain, in HexFacts facts)
    {
        var water = terrain is
            IceRemembersTerrain.Ocean or
            IceRemembersTerrain.Shelf or
            IceRemembersTerrain.Reef or
            IceRemembersTerrain.Fjord or
            IceRemembersTerrain.Lake;
        return water && facts.Count(SurfaceGroup.FrozenWater) >= SeaIceCoverCut
            ? IceRemembersTerrain.SeaIce
            : null;
    }

    private static int BuildCrossingWord(in PhysicalHexAggregate aggregate)
    {
        var word = 0;
        for (var direction = 0; direction < PhysicalCrossingCodes.DirectionCount; direction++)
        {
            var crossingClass = CrossingClassFor(aggregate, 1 << direction);
            if (crossingClass != PhysicalCrossingCodes.Open)
            {
                word = PhysicalCrossingCodes.WithClass(word, direction, crossingClass);
            }
        }

        return word;
    }

    // Cliff beats ridge; a pass clears both back to open (that is what a
    // pass is). Trunk rivers survive under passes - a pass over a great
    // river still crosses the river.
    private static int CrossingClassFor(in PhysicalHexAggregate aggregate, int bit)
    {
        var pass = (aggregate.PassEdgeMask & bit) != 0;
        if (!pass && (aggregate.CliffEdgeMask & bit) != 0)
        {
            return PhysicalCrossingCodes.Cliff;
        }

        if (!pass && (aggregate.RidgeEdgeMask & bit) != 0)
        {
            return PhysicalCrossingCodes.Ridge;
        }

        return (aggregate.TrunkRiverEdgeMask & bit) != 0
            ? PhysicalCrossingCodes.River
            : PhysicalCrossingCodes.Open;
    }

    private static int BuildFieldWord(IceRemembersTerrain terrain, in HexFacts facts)
    {
        var aggregate = facts.Aggregate;
        var word = 0;
        if (aggregate.PeakCount > 0)
        {
            word |= PhysicalHexFieldCodes.SummitFlag;
        }

        if (HexIsThermal(terrain, facts))
        {
            word |= PhysicalHexFieldCodes.ThermalFlag;
        }

        if ((aggregate.SignificantFeatureFlags & AnomalyFeatureMask) != 0)
        {
            word |= PhysicalHexFieldCodes.AnomalyFlag;
        }

        if (facts.Count(SurfaceGroup.Polynya) > 0)
        {
            word |= PhysicalHexFieldCodes.PolynyaFlag;
        }

        if (facts.CraterCount > 0)
        {
            word |= PhysicalHexFieldCodes.CraterFlag;
        }

        return word;
    }

    private static bool HexIsThermal(IceRemembersTerrain terrain, in HexFacts facts) =>
        terrain == IceRemembersTerrain.HotSprings ||
        facts.VentCount >= ThermalVentCut ||
        facts.MagicCount(MagicGroup.WarmFire) >= ThermalSurfaceCut ||
        facts.Aggregate.MeanGeothermal >= ThermalGeothermalCut;

    // The surface-to-terrain affinity table: one straight-line assignment
    // per natural surface. Surfaces left at None (tundra, wet tundra, plain
    // magic overlays) vote for no group and fall through to tundra.
    private static byte[] CreateBaseSurfaceGroups()
    {
        var groups = new byte[IceRemembersPhysicalGeography.SurfacePalette.Count];
        groups[(int)IceRemembersPhysicalSurface.OpenField] = (byte)SurfaceGroup.Verdant;
        groups[(int)IceRemembersPhysicalSurface.ColdSteppe] = (byte)SurfaceGroup.Verdant;
        groups[(int)IceRemembersPhysicalSurface.RiverValley] = (byte)SurfaceGroup.RiverLand;
        groups[(int)IceRemembersPhysicalSurface.RiverChannel] = (byte)SurfaceGroup.RiverLand;
        groups[(int)IceRemembersPhysicalSurface.Floodplain] = (byte)SurfaceGroup.RiverLand;
        groups[(int)IceRemembersPhysicalSurface.Delta] = (byte)SurfaceGroup.RiverLand;
        groups[(int)IceRemembersPhysicalSurface.PolarBarrens] = (byte)SurfaceGroup.Barren;
        // A vent field feeds what a reef feeds, so gameplay can treat it as
        // productive water rather than as empty ocean.
        groups[(int)IceRemembersPhysicalSurface.HydrothermalVent] = (byte)SurfaceGroup.Reef;
        groups[(int)IceRemembersPhysicalSurface.Ridge] = (byte)SurfaceGroup.Crag;
        groups[(int)IceRemembersPhysicalSurface.ScreeField] = (byte)SurfaceGroup.Crag;
        groups[(int)IceRemembersPhysicalSurface.Gorge] = (byte)SurfaceGroup.Crag;
        groups[(int)IceRemembersPhysicalSurface.Waterfall] = (byte)SurfaceGroup.Crag;
        groups[(int)IceRemembersPhysicalSurface.Glacier] = (byte)SurfaceGroup.Ice;
        groups[(int)IceRemembersPhysicalSurface.Snowfield] = (byte)SurfaceGroup.Ice;
        groups[(int)IceRemembersPhysicalSurface.Moraine] = (byte)SurfaceGroup.Ice;
        groups[(int)IceRemembersPhysicalSurface.Fjord] = (byte)SurfaceGroup.Fjord;
        groups[(int)IceRemembersPhysicalSurface.Reef] = (byte)SurfaceGroup.Reef;
        groups[(int)IceRemembersPhysicalSurface.Shelf] = (byte)SurfaceGroup.Shallow;
        groups[(int)IceRemembersPhysicalSurface.ExposedShelf] = (byte)SurfaceGroup.Shallow;
        groups[(int)IceRemembersPhysicalSurface.Ocean] = (byte)SurfaceGroup.Deep;
        groups[(int)IceRemembersPhysicalSurface.Trench] = (byte)SurfaceGroup.Deep;
        groups[(int)IceRemembersPhysicalSurface.CurrentChannel] = (byte)SurfaceGroup.Deep;
        groups[(int)IceRemembersPhysicalSurface.PackIce] = (byte)SurfaceGroup.FrozenWater;
        groups[(int)IceRemembersPhysicalSurface.SeaIce] = (byte)SurfaceGroup.FrozenWater;
        groups[(int)IceRemembersPhysicalSurface.IceShelf] = (byte)SurfaceGroup.FrozenWater;
        groups[(int)IceRemembersPhysicalSurface.Polynya] = (byte)SurfaceGroup.Polynya;
        return groups;
    }

    private static byte[] CreateFinalSurfaceGroups()
    {
        var groups = new byte[IceRemembersPhysicalGeography.SurfacePalette.Count];
        groups[(int)IceRemembersPhysicalSurface.AuroraField] = (byte)MagicGroup.Aurora;
        groups[(int)IceRemembersPhysicalSurface.LightWell] = (byte)MagicGroup.Aurora;
        groups[(int)IceRemembersPhysicalSurface.FrostPrism] = (byte)MagicGroup.Aurora;
        groups[(int)IceRemembersPhysicalSurface.MeltLine] = (byte)MagicGroup.Aurora;
        groups[(int)IceRemembersPhysicalSurface.HotSpring] = (byte)MagicGroup.WarmFire;
        groups[(int)IceRemembersPhysicalSurface.ThawedHalo] = (byte)MagicGroup.WarmFire;
        groups[(int)IceRemembersPhysicalSurface.BasaltFlat] = (byte)MagicGroup.Basalt;
        groups[(int)IceRemembersPhysicalSurface.FirecoreVent] = (byte)MagicGroup.Basalt;
        groups[(int)IceRemembersPhysicalSurface.ThermalVein] = (byte)MagicGroup.Basalt;
        return groups;
    }

    private readonly struct HexFacts
    {
        private readonly int[] _groupCounts;
        private readonly int[] _magicCounts;

        private HexFacts(
            PhysicalHexAggregate aggregate,
            int[] groupCounts,
            int[] magicCounts,
            int fjordCount,
            int ventCount,
            int craterCount)
        {
            Aggregate = aggregate;
            _groupCounts = groupCounts;
            _magicCounts = magicCounts;
            FjordCount = fjordCount;
            VentCount = ventCount;
            CraterCount = craterCount;
        }

        public PhysicalHexAggregate Aggregate { get; }

        public int FjordCount { get; }

        public int VentCount { get; }

        public int CraterCount { get; }

        public int Count(SurfaceGroup group) => _groupCounts[(int)group];

        public int MagicCount(MagicGroup group) => _magicCounts[(int)group];

        public static HexFacts Create(
            PhysicalGeographyField field,
            IceRemembersMicrogeographyMap magic,
            int hexIndex,
            PhysicalHexAggregate aggregate)
        {
            var groupCounts = new int[Enum.GetValues<SurfaceGroup>().Length];
            var magicCounts = new int[Enum.GetValues<MagicGroup>().Length];
            var fjordCount = 0;
            var ventCount = 0;
            var craterCount = 0;
            for (var localTriangle = 0;
                localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex;
                localTriangle++)
            {
                var triangleIndex = field.Grid.TriangleIndex(hexIndex, localTriangle);
                groupCounts[BaseSurfaceGroups[magic.BaseSurfaceIndexes[triangleIndex]]]++;
                magicCounts[FinalSurfaceGroups[magic.SurfaceIndexes[triangleIndex]]]++;
                var landform = field.LandformCodes[triangleIndex];
                fjordCount += landform == PhysicalLandformCodes.Fjord ? 1 : 0;
                craterCount += landform is
                    PhysicalLandformCodes.ImpactBasin or
                    PhysicalLandformCodes.ImpactRim ? 1 : 0;
                ventCount += (field.FeatureFlags[triangleIndex] &
                    PhysicalGeographyFeatureCodes.GeothermalVent) != 0 ? 1 : 0;
            }

            return new HexFacts(aggregate, groupCounts, magicCounts, fjordCount, ventCount, craterCount);
        }
    }
}
