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

    public bool RequestGambitEnabled(int gambitId, bool enabled)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gambitId);

        var version = _worldVersion();
        var issueVersion = version > int.MaxValue ? int.MaxValue : (int)version;
        var command = new SetGambitEnabledCommand(
            issueVersion,
            gambitId,
            enabled,
            SubmitterId: 1,
            SubmitterSequence: ++_submitterSequence);
        return _commands.TrySetGambitEnabled(command, out _);
    }

    public bool RequestConsecrate(int cultureId, int planeCode, int q, int r)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cultureId);

        var command = new BlessCellCultureCommand(
            IssueVersionNow(),
            cultureId,
            planeCode,
            q,
            r,
            Radius: 1,
            Amount: 120f,
            SubmitterId: 1,
            SubmitterSequence: ++_submitterSequence);
        return _commands.TryBlessCellCulture(command, out _);
    }

    public bool RequestFavor(int entityId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityId);

        var command = new FavorEntityCommand(
            IssueVersionNow(),
            entityId,
            DurationTicks: 100,
            MultiplierDelta: 1f,
            SubmitterId: 1,
            SubmitterSequence: ++_submitterSequence);
        return _commands.TryFavorEntity(command, out _);
    }

    private int IssueVersionNow()
    {
        var version = _worldVersion();
        return version > int.MaxValue ? int.MaxValue : (int)version;
    }
}
