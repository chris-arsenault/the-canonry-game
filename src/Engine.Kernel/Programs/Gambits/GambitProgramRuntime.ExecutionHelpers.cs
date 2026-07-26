namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GambitProgramRuntime
{
    private static void WriteGambitEvaluated(
        Simulation simulation,
        int tick,
        int entityOccurrenceHandle,
        int kindId,
        int gambitId,
        int candidateCount,
        KernelEventState state)
    {
        simulation.EmitGambitEvaluated(
            tick,
            entityOccurrenceHandle,
            kindId,
            gambitId,
            candidateCount,
            state);
    }

    private static void WriteGambitExecuted(
        Simulation simulation,
        int tick,
        int entityOccurrenceHandle,
        int targetOccurrenceHandle,
        int kindId,
        int gambitId,
        KernelEventState state)
    {
        simulation.EmitGambitExecuted(
            tick,
            entityOccurrenceHandle,
            targetOccurrenceHandle,
            kindId,
            gambitId,
            state);
    }

    private static void Push(global::System.Span<FixedScalar> stack, ref int stackCount, FixedScalar value)
    {
        if (stackCount >= stack.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        stack[stackCount] = value;
        stackCount++;
    }

    private static FixedScalar Pop(global::System.Span<FixedScalar> stack, ref int stackCount)
    {
        if (stackCount <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        stackCount--;
        return stack[stackCount];
    }

    private static void PushBinary(global::System.Span<FixedScalar> stack, ref int stackCount, FixedScalar value)
    {
        Push(stack, ref stackCount, value);
    }

    private static void Subtract(global::System.Span<FixedScalar> stack, ref int stackCount)
    {
        var right = Pop(stack, ref stackCount);
        var left = Pop(stack, ref stackCount);
        Push(stack, ref stackCount, left - right);
    }

    private static void Divide(global::System.Span<FixedScalar> stack, ref int stackCount)
    {
        var right = Pop(stack, ref stackCount);
        var left = Pop(stack, ref stackCount);
        Push(stack, ref stackCount, right == 0 ? 0 : left / right);
    }

    private static void Min(global::System.Span<FixedScalar> stack, ref int stackCount)
    {
        var right = Pop(stack, ref stackCount);
        var left = Pop(stack, ref stackCount);
        Push(stack, ref stackCount, left < right ? left : right);
    }

    private static void Max(global::System.Span<FixedScalar> stack, ref int stackCount)
    {
        var right = Pop(stack, ref stackCount);
        var left = Pop(stack, ref stackCount);
        Push(stack, ref stackCount, left > right ? left : right);
    }

    private static void Clamp(global::System.Span<FixedScalar> stack, ref int stackCount)
    {
        var max = Pop(stack, ref stackCount);
        var min = Pop(stack, ref stackCount);
        var value = Pop(stack, ref stackCount);
        if (value < min)
        {
            value = min;
        }
        else if (value > max)
        {
            value = max;
        }

        Push(stack, ref stackCount, value);
    }

    private static FixedScalar Abs(FixedScalar value)
    {
        return value < 0 ? -value : value;
    }

    private void EnsureOpenGambit(int gambitSlot)
    {
        EnsureGambit(gambitSlot);
        if (gambitSlot != _openGambitSlot)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureGambit(int gambitSlot)
    {
        if ((uint)gambitSlot == 0u || gambitSlot > _gambitCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidatePositive(int value)
    {
        NumericGuard.ThrowIfNegativeOrZero(value);
    }
}
