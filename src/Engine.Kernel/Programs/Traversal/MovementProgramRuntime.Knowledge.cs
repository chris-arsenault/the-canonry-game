namespace Engine.Kernel;

internal sealed partial class MovementProgramRuntime
{
    public void RegisterTechMovementProfile(
        int kindId,
        int techId,
        int planeCode,
        int movementProfileId) =>
        Data.RegisterTechMovementProfile(kindId, techId, planeCode, movementProfileId);
}
