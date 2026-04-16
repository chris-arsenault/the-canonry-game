namespace Engine.Domain.Graph;

public readonly record struct DefinitionAttributeId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct DefinitionResourceId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct DefinitionRelationshipId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct DefinitionPressureId(string Value)
{
    public override string ToString() => Value;
}

public enum DefinitionAttributeShape
{
    Scalar,
    Plane2D,
}

public enum DefinitionConservationClass
{
    Unspecified,
    Creatable,
    Destroyable,
    Transferable,
    Hybrid,
}
