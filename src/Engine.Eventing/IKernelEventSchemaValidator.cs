namespace Engine.Eventing;

public interface IKernelEventSchemaValidator
{
    bool IsKnown(KernelEventType eventType);

    bool IsValid(in KernelEvent kernelEvent);
}
