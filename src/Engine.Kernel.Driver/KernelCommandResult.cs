namespace Engine.Kernel.Driver;

public readonly record struct KernelCommandResult(
    int CommandId,
    int CommandType,
    bool Applied,
    int ReasonCodeId);
