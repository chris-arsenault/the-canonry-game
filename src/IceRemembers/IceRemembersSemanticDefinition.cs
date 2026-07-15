namespace IceRemembers;

using Engine.Domain.Config;

public static class IceRemembersSemanticDefinition
{
    public static SemanticDomainConfig Create() => new()
    {
        Stocks = IceRemembersEntities.CreateSemanticStocks(),
        Generators = IceRemembersGeneratorDefinition.Create(),
        ChronicleArchetypes = IceRemembersChronicleArchetypes.Create(),
        ChronicleSharedBeats = IceRemembersChronicleArchetypes.CreateSharedBeats(),
        WorldBeatAges = IceRemembersAges.Create(),
    };
}
