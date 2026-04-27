namespace Engine.Kernel.Driver;

public interface ICommandSubmitter
{
    bool TrySetGeneratorEnabled(in SetGeneratorEnabledCommand command, out CommandHandle handle);
}
