namespace Engine.WorldGen;

internal static class PhysicalFractures
{
    public static void Generate(
        PhysicalGeographyField field,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        var branches = CreateBranches(seed, profile.FractureBranchCount);
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var x = field.Grid.TriangleX(triangleIndex);
            var y = field.Grid.TriangleY(triangleIndex);
            var independent = PhysicalNoise.UnitNoise(x, y, seed ^ 0x1d73, 15d, 3);
            var branching = BranchingConductance(x, y, branches);
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
        ReadOnlySpan<FractureBranch> branches)
    {
        var strongest = 0d;
        foreach (var branch in branches)
        {
            var offsetX = x - branch.CenterX;
            var offsetY = y - branch.CenterY;
            var across = Math.Abs((-branch.SinAngle * offsetX) + (branch.CosAngle * offsetY));
            var along = (branch.CosAngle * offsetX) + (branch.SinAngle * offsetY);
            var wobble = PeriodicWobble(along / branch.Wavelength) * branch.WobbleStrength;
            var distance = Math.Abs(across - wobble);
            strongest = Math.Max(strongest, Math.Clamp(1d - (distance / branch.Width), 0d, 1d));
        }

        return strongest * 0.72d;
    }

    private static FractureBranch[] CreateBranches(int seed, int count)
    {
        var branches = new FractureBranch[count];
        for (var index = 0; index < branches.Length; index++)
        {
            var angle = PhysicalNoise.HashUnit(seed ^ 0x2b91, index, 0) * Math.PI * 2d;
            var centerRadius = PhysicalNoise.HashUnit(seed ^ 0x3da7, index, 1) * 150d;
            var centerAngle = PhysicalNoise.HashUnit(seed ^ 0x4f39, index, 2) * Math.PI * 2d;
            branches[index] = new FractureBranch(
                Math.Sin(angle),
                Math.Cos(angle),
                Math.Cos(centerAngle) * centerRadius,
                Math.Sin(centerAngle) * centerRadius,
                13d + (PhysicalNoise.HashUnit(seed ^ 0x5ac1, index, 3) * 23d),
                2.5d + (index % 4),
                0.8d + (PhysicalNoise.HashUnit(seed ^ 0x6c53, index, 4) * 1.8d));
        }

        return branches;
    }

    private static double PeriodicWobble(double cycles)
    {
        var phase = cycles - Math.Floor(cycles);
        var triangle = 1d - (4d * Math.Abs(phase - 0.5d));
        return triangle * (1.5d - (0.5d * triangle * triangle));
    }

    private readonly record struct FractureBranch(
        double SinAngle,
        double CosAngle,
        double CenterX,
        double CenterY,
        double Wavelength,
        double WobbleStrength,
        double Width);
}
