namespace TheCanonryGame.Game.Runtime;

using Engine.Kernel.Driver;

internal sealed class GameIntentPort
{
    private readonly ICommandSubmitter _commands;
    private readonly Func<long> _worldVersion;
    private int _submitterSequence;

    internal GameIntentPort(
        ICommandSubmitter commands,
        Func<long> worldVersion)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(worldVersion);
        _commands = commands;
        _worldVersion = worldVersion;
    }

    public bool RequestGeneratorEnabled(int generatorId, bool enabled)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(generatorId);

        var version = _worldVersion();
        var issueVersion = version > int.MaxValue ? int.MaxValue : (int)version;
        var command = new SetGeneratorEnabledCommand(
            issueVersion,
            generatorId,
            enabled,
            SubmitterId: 1,
            SubmitterSequence: ++_submitterSequence);
        return _commands.TrySetGeneratorEnabled(command, out _);
    }
}
