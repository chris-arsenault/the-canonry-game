namespace Engine.Kernel.Driver.Tests;

using Engine.Eventing;
using Engine.Kernel;
using Engine.Kernel.Driver;

public sealed class KernelCommandTests
{
    [Fact]
    public void CommandRingIsBoundedFifoAndAssignsCommandIds()
    {
        var commands = new KernelCommandRing(capacity: 2);
        var first = KernelCommand.From(new NoOpKernelCommand(IssueVersion: 0, SubmitterId: 7, SubmitterSequence: 1));
        var second = KernelCommand.From(new NoOpKernelCommand(IssueVersion: 0, SubmitterId: 7, SubmitterSequence: 2));
        var third = KernelCommand.From(new NoOpKernelCommand(IssueVersion: 0, SubmitterId: 7, SubmitterSequence: 3));

        Assert.True(commands.TryEnqueue(first, out var firstId));
        Assert.True(commands.TryEnqueue(second, out var secondId));
        Assert.False(commands.TryEnqueue(third, out var rejectedId));

        Assert.Equal(1, firstId);
        Assert.Equal(2, secondId);
        Assert.Equal(0, rejectedId);
        Assert.Equal(1, commands.RejectedWriteCount);

        Assert.True(commands.TryRead(out var firstRead));
        Assert.True(commands.TryRead(out var secondRead));
        Assert.False(commands.TryRead(out _));

        Assert.Equal(firstId, firstRead.CommandId);
        Assert.Equal(secondId, secondRead.CommandId);
        Assert.Equal(1, firstRead.SubmitterSequence);
        Assert.Equal(2, secondRead.SubmitterSequence);
    }

    [Fact]
    public async Task NoOpCommandEmitsAppliedResultAndCompletesHandle()
    {
        var simulation = new Simulation(new SimulationOptions { CommandCapacity = 4 });
        var completionTable = new KernelCommandCompletionTable(capacity: 4);
        var submitter = new KernelCommandSubmitter(simulation.Commands, completionTable);
        using var completionPump = new KernelCommandCompletionPump(simulation.Events, completionTable);
        var command = KernelCommand.From(new NoOpKernelCommand(
            IssueVersion: (int)simulation.CurrentVersion,
            SubmitterId: 1,
            SubmitterSequence: 1));

        Assert.True(submitter.TryEnqueue(command, out var handle));

        var advanced = simulation.AdvanceTicks(1);
        completionPump.DrainAvailable();
        var result = await handle.WaitAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(1, advanced);
        Assert.Equal(handle.CommandId, result.CommandId);
        Assert.Equal(KernelCommandTypeCodes.NoOp, result.CommandType);
        Assert.True(result.Applied);
        Assert.Equal(KernelCommandRejectReasonCodes.None, result.ReasonCodeId);
    }

    [Fact]
    public void StaleCommandEmitsRejectedResult()
    {
        var simulation = new Simulation(new SimulationOptions { CommandCapacity = 4 });
        var command = KernelCommand.From(new NoOpKernelCommand(IssueVersion: 999));

        Assert.True(simulation.Commands.TryEnqueue(command, out var commandId));

        _ = simulation.AdvanceTicks(1);
        var rejection = ReadSingleCommandRejected(simulation.Events);

        Assert.Equal(commandId, rejection.CommandId);
        Assert.Equal(KernelCommandTypeCodes.NoOp, rejection.CommandType);
        Assert.Equal(KernelCommandRejectReasonCodes.StaleVersion, rejection.ReasonCodeId);
    }

    [Fact]
    public void UnsupportedCommandEmitsRejectedResult()
    {
        var simulation = new Simulation(new SimulationOptions { CommandCapacity = 4 });
        var command = new KernelCommand(commandType: 777, issueVersion: (int)simulation.CurrentVersion);

        Assert.True(simulation.Commands.TryEnqueue(command, out var commandId));

        _ = simulation.AdvanceTicks(1);
        var rejection = ReadSingleCommandRejected(simulation.Events);

        Assert.Equal(commandId, rejection.CommandId);
        Assert.Equal(777, rejection.CommandType);
        Assert.Equal(KernelCommandRejectReasonCodes.UnsupportedCommand, rejection.ReasonCodeId);
    }

    [Fact]
    public void SetGeneratorEnabledCommandAppliesByRuntimeIdAndEmitsProjectedEvent()
    {
        var simulation = new Simulation(new SimulationOptions { CommandCapacity = 4 });
        _ = simulation.Generators.RegisterGenerator(
            generatorId: 91,
            cadence: 1,
            targetKindId: 1,
            pulseWeight: 1,
            maxRuns: 0,
            targetAllowWilderness: false);
        var command = KernelCommand.From(new SetGeneratorEnabledKernelCommand(
            IssueVersion: 999,
            GeneratorId: 91,
            Enabled: 0));

        Assert.True(simulation.Commands.TryEnqueue(command, out var commandId));

        _ = simulation.AdvanceTicks(1);

        Assert.True(simulation.Generators.TryGetGeneratorEnabledById(91, out var enabled));
        Assert.False(enabled);
        var enabledChanged = ReadSingleGeneratorEnabledChanged(simulation.Events);
        Assert.Equal(91, enabledChanged.GeneratorId);
        Assert.Equal(0, enabledChanged.Enabled);
        var applied = ReadSingleCommandApplied(simulation.Events);
        Assert.Equal(commandId, applied.CommandId);
        Assert.Equal(KernelCommandTypeCodes.SetGeneratorEnabled, applied.CommandType);
    }

    [Fact]
    public void InvalidSetGeneratorEnabledCommandEmitsRejectedResult()
    {
        var simulation = new Simulation(new SimulationOptions { CommandCapacity = 4 });
        var command = KernelCommand.From(new SetGeneratorEnabledKernelCommand(
            IssueVersion: (int)simulation.CurrentVersion,
            GeneratorId: 404,
            Enabled: 1));

        Assert.True(simulation.Commands.TryEnqueue(command, out var commandId));

        _ = simulation.AdvanceTicks(1);
        var rejection = ReadSingleCommandRejected(simulation.Events);

        Assert.Equal(commandId, rejection.CommandId);
        Assert.Equal(KernelCommandTypeCodes.SetGeneratorEnabled, rejection.CommandType);
        Assert.Equal(KernelCommandRejectReasonCodes.InvalidCommand, rejection.ReasonCodeId);
    }

    private static CommandRejectedKernelEvent ReadSingleCommandRejected(KernelEventLog events)
    {
        var cursor = events.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        try
        {
            Assert.True(events.TryRead(cursor, out var sequenced));
            Assert.Equal(KernelEventType.CommandRejected, sequenced.Event.EventType);
            return sequenced.Event.AsCommandRejected();
        }
        finally
        {
            cursor.Dispose();
        }
    }

    private static CommandAppliedKernelEvent ReadSingleCommandApplied(KernelEventLog events)
    {
        var cursor = events.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        try
        {
            while (events.TryRead(cursor, out var sequenced))
            {
                if (sequenced.Event.EventType == KernelEventType.CommandApplied)
                {
                    return sequenced.Event.AsCommandApplied();
                }
            }

            throw new InvalidOperationException();
        }
        finally
        {
            cursor.Dispose();
        }
    }

    private static GeneratorEnabledChangedKernelEvent ReadSingleGeneratorEnabledChanged(KernelEventLog events)
    {
        var cursor = events.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        try
        {
            while (events.TryRead(cursor, out var sequenced))
            {
                if (sequenced.Event.EventType == KernelEventType.GeneratorEnabledChanged)
                {
                    return sequenced.Event.AsGeneratorEnabledChanged();
                }
            }

            throw new InvalidOperationException();
        }
        finally
        {
            cursor.Dispose();
        }
    }
}
