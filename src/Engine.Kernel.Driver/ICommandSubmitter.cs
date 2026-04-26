namespace Engine.Kernel.Driver;

public interface ICommandSubmitter
{
    bool TryEnqueue(in KernelCommand command, out CommandHandle handle);
}
