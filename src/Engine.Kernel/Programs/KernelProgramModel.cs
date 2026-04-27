namespace Engine.Kernel;

public interface IKernelProgramData
{
    int ProgramId { get; }

    int SchemaVersion { get; }
}

public interface IKernelProgramState
{
    int ProgramId { get; }

    int SchemaVersion { get; }
}

public interface IKernelProgramRuntime<out TData, out TState> : ISimulationSystem
    where TData : IKernelProgramData
    where TState : IKernelProgramState
{
    TData Data { get; }

    TState State { get; }
}
