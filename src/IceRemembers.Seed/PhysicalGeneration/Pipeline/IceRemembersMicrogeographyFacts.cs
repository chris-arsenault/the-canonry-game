namespace IceRemembers.Seed;

[Flags]
internal enum IceRemembersMicrogeographyFacts : ushort
{
    None = 0,
    NaturalSurfaces = 1 << 0,
    FireField = 1 << 1,
    FireClimate = 1 << 2,
    AuroraField = 1 << 3,
    PressureField = 1 << 4,
    FireFeatures = 1 << 5,
    AuroraFeatures = 1 << 6,
    PressureFeatures = 1 << 7,
    Interactions = 1 << 8,
    FinalSurfaces = 1 << 9,
    Validated = 1 << 10,
    MagicHistory = 1 << 11,
    PressureCoastalFeedback = 1 << 12,
}
