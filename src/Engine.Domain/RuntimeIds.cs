namespace Engine.Domain;

public readonly record struct KindRuntimeId(int Value)
{
    public static KindRuntimeId None => default;
}

public readonly record struct AttributeRuntimeId(int Value)
{
    public static AttributeRuntimeId None => default;
}

public readonly record struct TraitRuntimeId(int Value)
{
    public static TraitRuntimeId None => default;
}

public readonly record struct RelationshipKindRuntimeId(int Value)
{
    public static RelationshipKindRuntimeId None => default;
}

public readonly record struct StructuralFactRuntimeId(int Value)
{
    public static StructuralFactRuntimeId None => default;
}

public readonly record struct TerrainRuntimeId(int Value)
{
    public static TerrainRuntimeId None => default;
}

public readonly record struct TerrainBoundaryGroupRuntimeId(int Value)
{
    public static TerrainBoundaryGroupRuntimeId None => default;
}

public readonly record struct TravelStructureRuntimeId(int Value)
{
    public static TravelStructureRuntimeId None => default;
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

public readonly record struct VerbRuntimeId(int Value)
{
    public static VerbRuntimeId None => default;
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

public readonly record struct TagRuntimeId(int Value)
{
    public static TagRuntimeId None => default;
}
