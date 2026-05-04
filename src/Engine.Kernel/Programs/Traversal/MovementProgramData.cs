namespace Engine.Kernel;

internal sealed partial class MovementProgramData : IKernelProgramData
{
    public MovementProgramData(
        float roadBuildPerTravel = 1f,
        float roadDecayPerTick = 0.9995f)
    {
        RoadBuildPerTravel = roadBuildPerTravel;
        RoadDecayPerTick = roadDecayPerTick;
    }

    public int ProgramId => KernelProgramCodes.Movement;

    public int SchemaVersion => 1;

    public float RoadBuildPerTravel { get; set; }

    public float RoadDecayPerTick { get; set; }
}
