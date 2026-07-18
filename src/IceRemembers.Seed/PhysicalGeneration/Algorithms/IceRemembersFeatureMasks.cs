namespace IceRemembers.Seed;

using IceRemembers;

internal static class IceRemembersFeatureMasks
{
    public static ulong Of(params IceRemembersPhysicalFeature[] features)
    {
        var result = 0UL;
        foreach (var feature in features)
        {
            result |= IceRemembersPhysicalGeography.FeatureFlag(feature);
        }

        return result;
    }

    public static void Add(ref ulong flags, bool condition, IceRemembersPhysicalFeature feature)
    {
        if (condition)
        {
            flags |= IceRemembersPhysicalGeography.FeatureFlag(feature);
        }
    }

    public static bool Has(ulong flags, IceRemembersPhysicalFeature feature) =>
        (flags & IceRemembersPhysicalGeography.FeatureFlag(feature)) != 0;
}
