namespace IceRemembers;

using Engine.Domain.Config;

public enum IceRemembersMovementProfile
{
    Trail,
    Shore,
    Wake,
    Commons,
    Mandate,
    Veil,
    Rift,
}

public static class IceRemembersMovementProfileExtensions
{
    private static readonly string[] DomainNames =
    [
        "trail",
        "shore",
        "wake",
        "commons",
        "mandate",
        "veil",
        "rift",
    ];

    public static string ToDomainName(this IceRemembersMovementProfile profile)
    {
        var index = (int)profile;
        return (uint)index < (uint)DomainNames.Length
            ? DomainNames[index]
            : throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown Ice Remembers movement profile");
    }
}

public static class IceRemembersMovementProfiles
{
    public static IReadOnlyList<MovementProfileConfig> DomainProfiles { get; } =
    [
        Profile(IceRemembersMovementProfile.Trail, "Trail"),
        Profile(IceRemembersMovementProfile.Shore, "Shore"),
        Profile(IceRemembersMovementProfile.Wake, "Wake"),
        Profile(IceRemembersMovementProfile.Commons, "Commons"),
        Profile(IceRemembersMovementProfile.Mandate, "Mandate"),
        Profile(IceRemembersMovementProfile.Veil, "Veil"),
        Profile(IceRemembersMovementProfile.Rift, "Rift"),
    ];

    private static MovementProfileConfig Profile(
        IceRemembersMovementProfile profile,
        string displayName) =>
        new()
        {
            Name = profile.ToDomainName(),
            DisplayName = displayName,
        };
}
