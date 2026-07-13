namespace IceRemembers;

using Engine.Domain.Config;

public static class IceRemembersSemanticDefinition
{
    public static SemanticDomainConfig Create() => new()
    {
        Stocks = IceRemembersEntities.CreateSemanticStocks(),
        Generators = IceRemembersGeneratorDefinition.Create(),
        StoryArchetypes = IceRemembersStoryArchetypes.Create(),
    };
}
