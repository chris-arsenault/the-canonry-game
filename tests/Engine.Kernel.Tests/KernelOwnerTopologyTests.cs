namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class KernelOwnerTopologyTests
{
    [Fact]
    public void LogicalDestinationOwnerDoesNotDependOnPhysicalWorkerCount()
    {
        var topology = new KernelOwnerTopology(new SimulationOptions
        {
            EntityCapacity = 32,
            RelationshipCapacity = 64,
            Execution = new KernelExecutionOptions
            {
                OwnerPartitionCount = 11,
            },
        });

        for (var entityId = 1; entityId <= 32; entityId++)
        {
            var owner = topology.EntityOwner(entityId);
            AssertOwnedByOneWorkerAtEveryTopology(topology, owner);
        }

        for (var relationshipId = 1;
             relationshipId <= 64;
             relationshipId++)
        {
            var owner = topology.RelationshipOwner(relationshipId);
            AssertOwnedByOneWorkerAtEveryTopology(topology, owner);
        }

        for (var q = -8; q <= 8; q++)
        {
            var owner = topology.CellOwner(
                PlaneCodes.Physical,
                q,
                r: 3);
            AssertOwnedByOneWorkerAtEveryTopology(topology, owner);
        }
    }

    private static void AssertOwnedByOneWorkerAtEveryTopology(
        KernelOwnerTopology topology,
        int owner)
    {
        foreach (var workerCount in new[] { 1, 2, 4, 8 })
        {
            var matchingWorkers = 0;
            for (var workerIndex = 0;
                 workerIndex < workerCount;
                 workerIndex++)
            {
                if (owner >= topology.FirstOwnerForWorker(
                        workerIndex,
                        workerCount) &&
                    owner < topology.OwnerLimitForWorker(
                        workerIndex,
                        workerCount))
                {
                    matchingWorkers++;
                }
            }

            Assert.Equal(1, matchingWorkers);
        }
    }
}
