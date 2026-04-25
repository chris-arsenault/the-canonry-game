namespace Engine.Domain;

public readonly record struct AttributeRuntimeId(int Value)
{
    public static AttributeRuntimeId None => default;
}

public readonly record struct RelationshipKindRuntimeId(int Value)
{
    public static RelationshipKindRuntimeId None => default;
}

public readonly record struct RelationshipFactRuntimeId(int Value)
{
    public static RelationshipFactRuntimeId None => default;
}

public readonly record struct PressureRuntimeId(int Value)
{
    public static PressureRuntimeId None => default;
}

public readonly record struct ResourceRuntimeId(int Value)
{
    public static ResourceRuntimeId None => default;
}

public readonly record struct TerrainRuntimeId(int Value)
{
    public static TerrainRuntimeId None => default;
}

public readonly record struct SubtypeRuntimeId(int Value)
{
    public static SubtypeRuntimeId None => default;
}

public readonly record struct CultureRuntimeId(int Value)
{
    public static CultureRuntimeId None => default;
}

public readonly record struct GeneratorRuntimeId(int Value)
{
    public static GeneratorRuntimeId None => default;
}

public readonly record struct DetectorRuntimeId(int Value)
{
    public static DetectorRuntimeId None => default;
}

public readonly record struct EventCodeId(int Value)
{
    public static EventCodeId None => default;
}

public readonly record struct ReasonCodeId(int Value)
{
    public static ReasonCodeId None => default;
}

public readonly record struct SourceCodeId(int Value)
{
    public static SourceCodeId None => default;
}
