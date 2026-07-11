namespace TheCanonryGame.Game.Tests;

using Engine.Kernel.Driver;

using TheCanonryGame.Game.Runtime;

public sealed class GameIntentPortTests
{
    [Fact]
    public void RequestGeneratorEnabled_SubmitsClampedWorldVersionAndMonotonicSequence()
    {
        var worldVersion = (long)int.MaxValue + 10;
        var commands = new RecordingCommandSubmitter { NextResult = false };
        var intents = new GameIntentPort(commands, () => worldVersion);

        var firstResult = intents.RequestGeneratorEnabled(generatorId: 7, enabled: false);

        Assert.False(firstResult);
        Assert.Collection(
            commands.Commands,
            command =>
            {
                Assert.Equal(int.MaxValue, command.IssueVersion);
                Assert.Equal(7, command.GeneratorId);
                Assert.False(command.Enabled);
                Assert.Equal(1, command.SubmitterId);
                Assert.Equal(1, command.SubmitterSequence);
            });

        worldVersion = 42;
        commands.NextResult = true;

        var secondResult = intents.RequestGeneratorEnabled(generatorId: 8, enabled: true);

        Assert.True(secondResult);
        Assert.Equal(2, commands.Commands.Count);
        Assert.Equal(42, commands.Commands[1].IssueVersion);
        Assert.Equal(8, commands.Commands[1].GeneratorId);
        Assert.True(commands.Commands[1].Enabled);
        Assert.Equal(2, commands.Commands[1].SubmitterSequence);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RequestGeneratorEnabled_RejectsInvalidGeneratorIdWithoutSubmitting(int generatorId)
    {
        var commands = new RecordingCommandSubmitter();
        var intents = new GameIntentPort(commands, () => 10);

        Assert.Throws<ArgumentOutOfRangeException>(() => intents.RequestGeneratorEnabled(generatorId, enabled: true));
        Assert.Empty(commands.Commands);
    }

    private sealed class RecordingCommandSubmitter : ICommandSubmitter
    {
        public List<SetGeneratorEnabledCommand> Commands { get; } = [];

        public List<SetGambitEnabledCommand> GambitCommands { get; } = [];

        public bool NextResult { get; set; } = true;

        public bool TrySetGeneratorEnabled(in SetGeneratorEnabledCommand command, out CommandHandle handle)
        {
            Commands.Add(command);
            handle = default;
            return NextResult;
        }

        public bool TrySetGambitEnabled(in SetGambitEnabledCommand command, out CommandHandle handle)
        {
            GambitCommands.Add(command);
            handle = default;
            return NextResult;
        }

        public bool TryBlessCellCulture(in BlessCellCultureCommand command, out CommandHandle handle)
        {
            BlessCommands.Add(command);
            handle = default;
            return NextResult;
        }

        public bool TryFavorEntity(in FavorEntityCommand command, out CommandHandle handle)
        {
            FavorCommands.Add(command);
            handle = default;
            return NextResult;
        }

        public List<BlessCellCultureCommand> BlessCommands { get; } = [];

        public List<FavorEntityCommand> FavorCommands { get; } = [];
    }
}
