namespace Engine.Kernel;

internal sealed partial class Simulation
{
    private const int BlessCellCultureMaxRadius = 2;
    private static readonly FixedScalar BlessCellCultureMaxAmount = 500;
    private static readonly FixedScalar FavorEntityMaxMultiplierDelta = 4;
    private const int FavorEntityMaxDurationTicks = 10000;

    private void RouteCommandsToOwners()
    {
        while (Commands.TryRead(out var command))
        {
            _continuousOwners.EnqueueCommand(
                CommandOwner(command),
                command);
        }
    }

    internal KernelCommandApplyResult ApplyOwnedCommand(
        in KernelCommand command,
        int ownerTick,
        long admissionVersion)
    {
        if (command.IssueVersion != admissionVersion &&
            !CommandIsVersionTolerant(command.CommandType))
        {
            return new KernelCommandApplyResult(
                false,
                KernelCommandRejectReasonCodes.StaleVersion);
        }

        return ApplyCommand(command, ownerTick);
    }

    private KernelCommandApplyResult ApplyCommand(
        in KernelCommand command,
        int ownerTick) =>
        command.CommandType switch
        {
            KernelCommandTypeCodes.NoOp => KernelCommandApplyResult.Success,
            KernelCommandTypeCodes.SetGeneratorEnabled => ApplySetGeneratorEnabled(command.AsSetGeneratorEnabled()),
            KernelCommandTypeCodes.SetGambitEnabled => ApplySetGambitEnabled(command.AsSetGambitEnabled()),
            KernelCommandTypeCodes.BlessCellCulture => ApplyBlessCellCulture(command.AsBlessCellCulture(), ownerTick),
            KernelCommandTypeCodes.FavorEntity => ApplyFavorEntity(command.AsFavorEntity(), ownerTick),
            _ => new KernelCommandApplyResult(false, KernelCommandRejectReasonCodes.UnsupportedCommand),
        };

    private int CommandOwner(in KernelCommand command) =>
        command.CommandType switch
        {
            KernelCommandTypeCodes.SetGeneratorEnabled =>
                _ownerTopology.ProgramOwner(
                    KernelProgramCodes.Generators),
            KernelCommandTypeCodes.SetGambitEnabled =>
                _ownerTopology.ProgramOwner(
                    KernelProgramCodes.Gambits),
            KernelCommandTypeCodes.BlessCellCulture =>
                BlessCommandOwner(command.AsBlessCellCulture()),
            KernelCommandTypeCodes.FavorEntity =>
                EntityCommandOwner(
                    command.AsFavorEntity().EntityId,
                    command.CommandType),
            _ => _ownerTopology.ProgramOwner(
                global::System.Math.Max(1, command.CommandType)),
        };

    private int EntityCommandOwner(int entityOccurrence, int fallbackId) =>
        entityOccurrence > 0
            ? _ownerTopology.EntityOccurrenceOwner(entityOccurrence)
            : _ownerTopology.ProgramOwner(
                global::System.Math.Max(1, fallbackId));

    private int BlessCommandOwner(
        in BlessCellCultureKernelCommand payload) =>
        _ownerTopology.CellOwner(
            payload.PlaneCode,
            payload.Q,
            payload.R);

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

    private KernelCommandApplyResult ApplyBlessCellCulture(
        in BlessCellCultureKernelCommand payload,
        int ownerTick)
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
                    ownerTick);
            }
        }

        return KernelCommandApplyResult.Success;
    }

    private KernelCommandApplyResult ApplyFavorEntity(
        in FavorEntityKernelCommand payload,
        int ownerTick)
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
            ownerTick + payload.DurationTicks);
        return KernelCommandApplyResult.Success;
    }

    private static bool CommandIsVersionTolerant(int commandType)
    {
        return commandType is KernelCommandTypeCodes.SetGeneratorEnabled
            or KernelCommandTypeCodes.SetGambitEnabled
            or KernelCommandTypeCodes.BlessCellCulture
            or KernelCommandTypeCodes.FavorEntity;
    }

    internal readonly record struct KernelCommandApplyResult(
        bool Applied,
        int ReasonCode)
    {
        public static KernelCommandApplyResult Success => new(true, KernelCommandRejectReasonCodes.None);
    }
}
