namespace Engine.Kernel;

internal sealed partial class GambitProgramRuntime
{
    private float EvaluateScore(WorldState world, int gambitSlot, int entityId, int targetId)
    {
        return EvaluateExpression(
            world,
            entityId,
            targetId,
            _gambitExpressionStarts[gambitSlot],
            _gambitExpressionCounts[gambitSlot],
            _expressionCodes,
            _expressionA,
            _expressionX,
            defaultValue: 1f);
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
        float defaultValue)
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
        global::System.Span<float> stack,
        ref int stackCount,
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
        int variableCount)
    {
        if (code == BehaviorExpressionCodes.Literal)
        {
            Push(stack, ref stackCount, x);
            return true;
        }

        if (code == BehaviorExpressionCodes.SelfAttribute)
        {
            Push(stack, ref stackCount, world.GetAttribute(entityId, a));
            return true;
        }

        return TryExecuteContextExpressionOp(world, code, a, entityId, targetId, stack, ref stackCount, variableIds, variableValues, variableCount);
    }

    private bool TryExecuteContextExpressionOp(
        WorldState world,
        int code,
        int a,
        int entityId,
        int targetId,
        global::System.Span<float> stack,
        ref int stackCount,
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
        int variableCount)
    {
        if (code == BehaviorExpressionCodes.TargetAttribute)
        {
            Push(stack, ref stackCount, world.GetAttribute(targetId, a));
            return true;
        }

        return TryExecuteReferenceExpressionOp(world, code, a, entityId, targetId, stack, ref stackCount, variableIds, variableValues, variableCount);
    }

    private bool TryExecuteReferenceExpressionOp(
        WorldState world,
        int code,
        int a,
        int entityId,
        int targetId,
        global::System.Span<float> stack,
        ref int stackCount,
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
        int variableCount)
    {
        if (code == BehaviorExpressionCodes.Variable)
        {
            Push(stack, ref stackCount, VariableValue(variableIds, variableValues, variableCount, a));
            return true;
        }

        if (code != BehaviorExpressionCodes.Distance)
        {
            return false;
        }

        Push(stack, ref stackCount, KernelQuery.PlaneDistance(world, entityId, targetId, PlaneCodes.Physical));
        return true;
    }

    private static bool TryExecuteOperatorExpressionOp(
        int code,
        global::System.Span<float> stack,
        ref int stackCount)
    {
        if (code == BehaviorExpressionCodes.Add)
        {
            PushBinary(stack, ref stackCount, Pop(stack, ref stackCount) + Pop(stack, ref stackCount));
            return true;
        }

        if (code == BehaviorExpressionCodes.Subtract)
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
        if (code == BehaviorExpressionCodes.Multiply)
        {
            PushBinary(stack, ref stackCount, Pop(stack, ref stackCount) * Pop(stack, ref stackCount));
            return true;
        }

        if (code == BehaviorExpressionCodes.Divide)
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
        if (code == BehaviorExpressionCodes.Min)
        {
            Min(stack, ref stackCount);
            return true;
        }

        if (code == BehaviorExpressionCodes.Max)
        {
            Max(stack, ref stackCount);
            return true;
        }

        if (code == BehaviorExpressionCodes.Clamp)
        {
            Clamp(stack, ref stackCount);
            return true;
        }

        if (code != BehaviorExpressionCodes.Abs)
        {
            return false;
        }

        Push(stack, ref stackCount, Abs(Pop(stack, ref stackCount)));
        return true;
    }

}
