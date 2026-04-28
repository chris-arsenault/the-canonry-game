namespace Engine.Kernel;

internal sealed partial class ProcessProgramRuntime
{
    private float EvaluateScore(WorldState world, int processSlot, int entityId, int targetId)
    {
        return EvaluateExpression(
            world,
            entityId,
            targetId,
            _processExpressionStarts[processSlot],
            _processExpressionCounts[processSlot],
            _expressionCodes,
            _expressionA,
            _expressionX,
            defaultValue: 1f,
            processSlot);
    }

    private float EvaluateReward(WorldState world, int processSlot, int entityId, int targetId)
    {
        return EvaluateExpression(
            world,
            entityId,
            targetId,
            _processRewardExpressionStarts[processSlot],
            _processRewardExpressionCounts[processSlot],
            _rewardExpressionCodes,
            _rewardExpressionA,
            _rewardExpressionX,
            defaultValue: 1f,
            processSlot);
    }

    private float EvaluateExpression(
        WorldState world,
        int entityId,
        int targetId,
        int first,
        int count,
        int[] codes,
        int[] a,
        float[] x,
        float defaultValue,
        int processSlot = 0)
    {
        return EvaluateExpression(
            world,
            entityId,
            targetId,
            first,
            count,
            codes,
            a,
            x,
            defaultValue,
            processSlot,
            default,
            default,
            0);
    }

    private float EvaluateExpression(
        WorldState world,
        int entityId,
        int targetId,
        int first,
        int count,
        int[] codes,
        int[] a,
        float[] x,
        float defaultValue,
        int processSlot,
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
        int variableCount)
    {
        if (count == 0)
        {
            return defaultValue;
        }

        global::System.Span<float> stack = _expressionStack;
        var stackCount = 0;
        var end = first + count;
        for (var op = first; op < end; op++)
        {
            ExecuteExpressionOp(
                world,
                codes[op],
                a[op],
                x[op],
                entityId,
                targetId,
                processSlot,
                stack,
                ref stackCount,
                variableIds,
                variableValues,
                variableCount);
        }

        return stackCount == 0 ? 0f : stack[stackCount - 1];
    }

    private void ExecuteExpressionOp(
        WorldState world,
        int code,
        int a,
        float x,
        int entityId,
        int targetId,
        int processSlot,
        global::System.Span<float> stack,
        ref int stackCount,
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
        int variableCount)
    {
        if (TryExecuteValueExpressionOp(
                world,
                code,
                a,
                x,
                entityId,
                targetId,
                processSlot,
                stack,
                ref stackCount,
                variableIds,
                variableValues,
                variableCount) ||
            TryExecuteOperatorExpressionOp(code, stack, ref stackCount))
        {
            return;
        }

        throw new global::System.InvalidOperationException();
    }

    private bool TryExecuteValueExpressionOp(
        WorldState world,
        int code,
        int a,
        float x,
        int entityId,
        int targetId,
        int processSlot,
        global::System.Span<float> stack,
        ref int stackCount,
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
        int variableCount)
    {
        if (code == ProcessExpressionCodes.Literal)
        {
            Push(stack, ref stackCount, x);
            return true;
        }

        if (code == ProcessExpressionCodes.SelfAttribute)
        {
            Push(stack, ref stackCount, world.GetAttribute(entityId, a));
            return true;
        }

        return TryExecuteContextExpressionOp(world, code, a, entityId, targetId, processSlot, stack, ref stackCount, variableIds, variableValues, variableCount);
    }

    private bool TryExecuteContextExpressionOp(
        WorldState world,
        int code,
        int a,
        int entityId,
        int targetId,
        int processSlot,
        global::System.Span<float> stack,
        ref int stackCount,
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
        int variableCount)
    {
        if (code == ProcessExpressionCodes.TargetAttribute)
        {
            Push(stack, ref stackCount, world.GetAttribute(targetId, a));
            return true;
        }

        if (code == ProcessExpressionCodes.SelfLocationResource)
        {
            Push(stack, ref stackCount, ResourceAtEntity(world, entityId, a));
            return true;
        }

        return TryExecuteReferenceExpressionOp(world, code, a, entityId, targetId, processSlot, stack, ref stackCount, variableIds, variableValues, variableCount);
    }

    private bool TryExecuteReferenceExpressionOp(
        WorldState world,
        int code,
        int a,
        int entityId,
        int targetId,
        int processSlot,
        global::System.Span<float> stack,
        ref int stackCount,
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
        int variableCount)
    {
        if (code == ProcessExpressionCodes.TargetLocationResource)
        {
            if (NamedProviderRecoveryActive(processSlot))
            {
                Push(stack, ref stackCount, world.GetAttribute(targetId, _processNamedProviderAttributeIds[processSlot]));
                return true;
            }

            Push(stack, ref stackCount, ResourceAtEntity(world, targetId, a));
            return true;
        }

        if (code == ProcessExpressionCodes.Variable)
        {
            Push(stack, ref stackCount, VariableValue(variableIds, variableValues, variableCount, a));
            return true;
        }

        if (code != ProcessExpressionCodes.Distance)
        {
            return false;
        }

        Push(stack, ref stackCount, PlaneDistance(world, entityId, targetId, PlaneCodes.Physical));
        return true;
    }

    private static bool TryExecuteOperatorExpressionOp(
        int code,
        global::System.Span<float> stack,
        ref int stackCount)
    {
        if (code == ProcessExpressionCodes.Add)
        {
            PushBinary(stack, ref stackCount, Pop(stack, ref stackCount) + Pop(stack, ref stackCount));
            return true;
        }

        if (code == ProcessExpressionCodes.Subtract)
        {
            Subtract(stack, ref stackCount);
            return true;
        }

        return TryExecuteOtherOperatorExpressionOp(code, stack, ref stackCount);
    }

    private static bool TryExecuteOtherOperatorExpressionOp(
        int code,
        global::System.Span<float> stack,
        ref int stackCount)
    {
        if (code == ProcessExpressionCodes.Multiply)
        {
            PushBinary(stack, ref stackCount, Pop(stack, ref stackCount) * Pop(stack, ref stackCount));
            return true;
        }

        if (code == ProcessExpressionCodes.Divide)
        {
            Divide(stack, ref stackCount);
            return true;
        }

        return TryExecuteFinalOperatorExpressionOp(code, stack, ref stackCount);
    }

    private static bool TryExecuteFinalOperatorExpressionOp(
        int code,
        global::System.Span<float> stack,
        ref int stackCount)
    {
        if (code == ProcessExpressionCodes.Min)
        {
            Min(stack, ref stackCount);
            return true;
        }

        if (code == ProcessExpressionCodes.Max)
        {
            Max(stack, ref stackCount);
            return true;
        }

        if (code == ProcessExpressionCodes.Clamp)
        {
            Clamp(stack, ref stackCount);
            return true;
        }

        if (code != ProcessExpressionCodes.Abs)
        {
            return false;
        }

        Push(stack, ref stackCount, Abs(Pop(stack, ref stackCount)));
        return true;
    }

}
