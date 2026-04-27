namespace Engine.Kernel;

internal interface IKernelProgramData
{
    int ProgramId { get; }

    int SchemaVersion { get; }
}

internal interface IKernelProgramState
{
    int ProgramId { get; }

    int SchemaVersion { get; }
}

internal interface IKernelProgramRuntime<out TData, out TState> : ISimulationSystem
    where TData : IKernelProgramData
    where TState : IKernelProgramState
{
    TData Data { get; }

    TState State { get; }
}
