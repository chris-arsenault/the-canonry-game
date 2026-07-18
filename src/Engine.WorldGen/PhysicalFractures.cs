namespace Engine.WorldGen;

internal static class PhysicalFractures
{
    public static void Generate(
        PhysicalGeographyField field,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var x = field.Grid.TriangleX(triangleIndex);
            var y = field.Grid.TriangleY(triangleIndex);
            var independent = PhysicalNoise.UnitNoise(x, y, seed ^ 0x1d73, 15d, 3);
            var branching = BranchingConductance(x, y, seed, profile);
            var ridgeSeed = field.RidgeInfluence[triangleIndex] * 0.28d;
            field.FractureConductance[triangleIndex] = Math.Clamp(
                profile.FractureConductanceFloor +
                    (int)Math.Round(
                        (independent * 0.22d + branching + ridgeSeed) * 850d,
                        MidpointRounding.AwayFromZero),
                0,
                1000);
            field.HeatSourceDensity[triangleIndex] = Math.Clamp(
                (field.Geothermal[triangleIndex] * 2 + field.FractureConductance[triangleIndex]) / 3,
                0,
                1000);
        }
    }

    private static double BranchingConductance(
        double x,
        double y,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        var strongest = 0d;
        for (var branch = 0; branch < profile.FractureBranchCount; branch++)
        {
            var angle = PhysicalNoise.HashUnit(seed ^ 0x2b91, branch, 0) * Math.PI * 2d;
            var centerRadius = PhysicalNoise.HashUnit(seed ^ 0x3da7, branch, 1) * 150d;
            var centerAngle = PhysicalNoise.HashUnit(seed ^ 0x4f39, branch, 2) * Math.PI * 2d;
            var cx = Math.Cos(centerAngle) * centerRadius;
            var cy = Math.Sin(centerAngle) * centerRadius;
            var wavelength = 13d + (PhysicalNoise.HashUnit(seed ^ 0x5ac1, branch, 3) * 23d);
            var across = Math.Abs((-Math.Sin(angle) * (x - cx)) + (Math.Cos(angle) * (y - cy)));
            var along = (Math.Cos(angle) * (x - cx)) + (Math.Sin(angle) * (y - cy));
            var wobble = Math.Sin(along / wavelength) * (2.5d + (branch % 4));
            var distance = Math.Abs(across - wobble);
            var width = 0.8d + (PhysicalNoise.HashUnit(seed ^ 0x6c53, branch, 4) * 1.8d);
            strongest = Math.Max(strongest, Math.Clamp(1d - (distance / width), 0d, 1d));
        }

        return strongest * 0.72d;
    }
}
