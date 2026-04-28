namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class ProcessProgramRuntime
{
    private static void WriteProcessEvaluated(
        Simulation simulation,
        int tick,
        int entityId,
        int kindId,
        int processId,
        int candidateCount,
        KernelEventState state)
    {
        simulation.EmitProcessEvaluated(
            tick,
            entityId,
            kindId,
            0,
            processId,
            candidateCount,
            state);
    }

    private static void WriteProcessExecuted(
        Simulation simulation,
        int tick,
        int entityId,
        int targetId,
        int kindId,
        int processId)
    {
        simulation.EmitProcessExecuted(
            tick,
            entityId,
            targetId,
            kindId,
            processId);
    }

    private static void Push(global::System.Span<float> stack, ref int stackCount, float value)
    {
        if (stackCount >= stack.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        stack[stackCount] = value;
        stackCount++;
    }

    private static float Pop(global::System.Span<float> stack, ref int stackCount)
    {
        if (stackCount <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        stackCount--;
        return stack[stackCount];
    }

    private static void PushBinary(global::System.Span<float> stack, ref int stackCount, float value)
    {
        Push(stack, ref stackCount, value);
    }

    private static void Subtract(global::System.Span<float> stack, ref int stackCount)
    {
        var right = Pop(stack, ref stackCount);
        var left = Pop(stack, ref stackCount);
        Push(stack, ref stackCount, left - right);
    }

    private static void Divide(global::System.Span<float> stack, ref int stackCount)
    {
        var right = Pop(stack, ref stackCount);
        var left = Pop(stack, ref stackCount);
        Push(stack, ref stackCount, right == 0f ? 0f : left / right);
    }

    private static void Min(global::System.Span<float> stack, ref int stackCount)
    {
        var right = Pop(stack, ref stackCount);
        var left = Pop(stack, ref stackCount);
        Push(stack, ref stackCount, left < right ? left : right);
    }

    private static void Max(global::System.Span<float> stack, ref int stackCount)
    {
        var right = Pop(stack, ref stackCount);
        var left = Pop(stack, ref stackCount);
        Push(stack, ref stackCount, left > right ? left : right);
    }

    private static void Clamp(global::System.Span<float> stack, ref int stackCount)
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

    private static void PushBool(global::System.Span<bool> stack, ref int stackCount, bool value)
    {
        if (stackCount >= stack.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        stack[stackCount] = value;
        stackCount++;
    }

    private static bool PopBool(global::System.Span<bool> stack, ref int stackCount)
    {
        if (stackCount <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        stackCount--;
        return stack[stackCount];
    }

    private static float Abs(float value)
    {
        return value < 0f ? -value : value;
    }

    private static int Abs(int value)
    {
        return value < 0 ? -value : value;
    }

    private void EnsureOpenProcess(int processSlot)
    {
        EnsureProcess(processSlot);
        if (processSlot != _openProcessSlot)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureProcess(int processSlot)
    {
        if ((uint)processSlot == 0u || processSlot > _processCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureSupportRule(int supportRuleSlot)
    {
        if ((uint)supportRuleSlot == 0u || supportRuleSlot > _supportRuleCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
