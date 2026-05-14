namespace IceRemembers;

using Engine.Constants;
using Engine.Domain.Config;

using Profile = IceRemembers.IceRemembersMovementProfile;

public enum IceRemembersTravelStructure
{
    IceCauseway,
    DeepCurrent,
}

public static class IceRemembersTravelStructureExtensions
{
    private static readonly string[] DomainNames =
    [
        "ice_causeway",
        "deep_current",
    ];

    public static string ToDomainName(this IceRemembersTravelStructure structure)
    {
        var index = (int)structure;
        return (uint)index < (uint)DomainNames.Length
            ? DomainNames[index]
            : throw new ArgumentOutOfRangeException(nameof(structure), structure, "Unknown Ice Remembers travel structure");
    }
}

public static class IceRemembersTravelStructures
{
    public static IReadOnlyList<TravelStructureConfig> DomainTravelStructures { get; } =
    [
        Structure(
            IceRemembersTravelStructure.IceCauseway,
            "Ice Causeway",
            Move(Profile.Trail, 2.2f),
            Move(Profile.Shore, 2.4f)),
        Structure(
            IceRemembersTravelStructure.DeepCurrent,
            "Deep Current",
            Move(Profile.Wake, 1.2f),
            Move(Profile.Shore, 3.6f)),
    ];

    private static TravelStructureConfig Structure(
        IceRemembersTravelStructure structure,
        string displayName,
        params TravelStructureMovementProfileConfig[] movementProfiles) =>
        new()
        {
            Name = structure.ToDomainName(),
            DisplayName = displayName,
            PlaneCode = PlaneCodes.Physical,
            MovementProfiles = movementProfiles,
        };

    private static TravelStructureMovementProfileConfig Move(Profile profile, float movementCost) =>
        new()
        {
            Profile = profile.ToDomainName(),
            MovementCost = movementCost,
        };
}
