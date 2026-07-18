namespace Engine.Constants;

public static class PhysicalCrustTypeCodes
{
    public const int Oceanic = 1;
    public const int Continental = 2;

    public const int Min = Oceanic;
    public const int Max = Continental;
}

public static class PhysicalTectonicBoundaryCodes
{
    public const int None = 0;
    public const int Convergent = 1;
    public const int Divergent = 2;
    public const int Transform = 3;

    public const int Min = None;
    public const int Max = Transform;
}
