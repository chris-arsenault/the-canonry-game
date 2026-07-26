namespace Engine.Kernel;

internal sealed partial class Simulation
{
    private const int BlessCellCultureMaxRadius = 2;
    private static readonly FixedScalar BlessCellCultureMaxAmount = 500;
    private static readonly FixedScalar FavorEntityMaxMultiplierDelta = 4;
    private const int FavorEntityMaxDurationTicks = 50000;

    private void DrainCommands()
    {
        while (Commands.TryRead(out var command))
        {
            if (command.IssueVersion != CurrentVersion && !CommandIsVersionTolerant(command.CommandType))
            {
                WriteCommandRejected(command, KernelCommandRejectReasonCodes.StaleVersion);
                continue;
            }

            var result = ApplyCommand(in command);
            if (result.Applied)
            {
                WriteCommandApplied(command);
            }
            else
            {
                WriteCommandRejected(command, result.ReasonCode);
            }
        }
    }

    private KernelCommandApplyResult ApplyCommand(in KernelCommand command) =>
        command.CommandType switch
        {
            KernelCommandTypeCodes.NoOp => KernelCommandApplyResult.Success,
            KernelCommandTypeCodes.SetGeneratorEnabled => ApplySetGeneratorEnabled(command.AsSetGeneratorEnabled()),
            KernelCommandTypeCodes.SetGambitEnabled => ApplySetGambitEnabled(command.AsSetGambitEnabled()),
            KernelCommandTypeCodes.BlessCellCulture => ApplyBlessCellCulture(command.AsBlessCellCulture()),
            KernelCommandTypeCodes.FavorEntity => ApplyFavorEntity(command.AsFavorEntity()),
            _ => new KernelCommandApplyResult(false, KernelCommandRejectReasonCodes.UnsupportedCommand),
        };

    private KernelCommandApplyResult ApplySetGeneratorEnabled(in SetGeneratorEnabledKernelCommand payload)
    {
        if (payload.GeneratorId <= 0 ||
            (uint)payload.Enabled > 1u ||
            !Generators.TrySetGeneratorEnabledById(payload.GeneratorId, payload.Enabled != 0))
        {
            return new KernelCommandApplyResult(false, KernelCommandRejectReasonCodes.InvalidCommand);
        }

        WriteGeneratorEnabledChanged(payload.GeneratorId, payload.Enabled);
        return KernelCommandApplyResult.Success;
    }

    private KernelCommandApplyResult ApplySetGambitEnabled(in SetGambitEnabledKernelCommand payload)
    {
        if (payload.GambitId <= 0 ||
            (uint)payload.Enabled > 1u ||
            !Gambits.TrySetGambitEnabledById(payload.GambitId, payload.Enabled != 0))
        {
            return new KernelCommandApplyResult(false, KernelCommandRejectReasonCodes.InvalidCommand);
        }

        WriteGambitEnabledChanged(payload.GambitId, payload.Enabled);
        return KernelCommandApplyResult.Success;
    }

    private KernelCommandApplyResult ApplyBlessCellCulture(in BlessCellCultureKernelCommand payload)
    {
        if (payload.CultureId <= 0 ||
            (payload.PlaneCode != PlaneCodes.Physical && payload.PlaneCode != PlaneCodes.Semantic) ||
            (uint)payload.Radius > BlessCellCultureMaxRadius ||
            payload.Amount <= 0 ||
            payload.Amount > BlessCellCultureMaxAmount)
        {
            return new KernelCommandApplyResult(false, KernelCommandRejectReasonCodes.InvalidCommand);
        }

        using var scope = EnterExecutionContext(PlayerSourceCodeId, programId: 0);
        for (var dq = -payload.Radius; dq <= payload.Radius; dq++)
        {
            var minDr = global::System.Math.Max(-payload.Radius, -dq - payload.Radius);
            var maxDr = global::System.Math.Min(payload.Radius, -dq + payload.Radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                if (!World.CellIsKnown(payload.PlaneCode, payload.Q + dq, payload.R + dr))
                {
                    continue;
                }

                _ = ModifyCellCultureInfluenceRaw(
                    payload.CultureId,
                    payload.PlaneCode,
                    payload.Q + dq,
                    payload.R + dr,
                    payload.Amount,
                    CurrentTick);
            }
        }

        return KernelCommandApplyResult.Success;
    }

    private KernelCommandApplyResult ApplyFavorEntity(in FavorEntityKernelCommand payload)
    {
        if (payload.EntityId <= 0 ||
            !World.TryResolveEntityOccurrence(payload.EntityId, out var entityId) ||
            payload.MultiplierDelta <= 0 ||
            payload.MultiplierDelta > FavorEntityMaxMultiplierDelta ||
            payload.DurationTicks <= 0 ||
            payload.DurationTicks > FavorEntityMaxDurationTicks)
        {
            return new KernelCommandApplyResult(false, KernelCommandRejectReasonCodes.InvalidCommand);
        }

        World.SetEntityFavor(
            entityId,
            payload.MultiplierDelta,
            CurrentTick + payload.DurationTicks);
        return KernelCommandApplyResult.Success;
    }

    private static bool CommandIsVersionTolerant(int commandType)
    {
        return commandType is KernelCommandTypeCodes.SetGeneratorEnabled
            or KernelCommandTypeCodes.SetGambitEnabled
            or KernelCommandTypeCodes.BlessCellCulture
            or KernelCommandTypeCodes.FavorEntity;
    }

    private readonly record struct KernelCommandApplyResult(bool Applied, int ReasonCode)
    {
        public static KernelCommandApplyResult Success => new(true, KernelCommandRejectReasonCodes.None);
    }
}
