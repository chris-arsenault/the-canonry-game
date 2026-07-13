namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class ParallelKernelRandomTests
{
    [Fact]
    public void SameCoordinatesProduceSameStream()
    {
        var first = KernelRandomCursor.Create(77, 100, KernelPhase.ActorDecision, 12, 44, 3);
        var second = KernelRandomCursor.Create(77, 100, KernelPhase.ActorDecision, 12, 44, 3);

        for (var draw = 0; draw < 32; draw++)
        {
            Assert.Equal(first.NextFloat(), second.NextFloat());
        }
    }

    [Fact]
    public void SubjectAndPurposeSelectIndependentStreams()
    {
        var first = KernelRandomCursor.Create(77, 100, KernelPhase.ActorDecision, 12, 44, 3);
        var otherSubject = KernelRandomCursor.Create(77, 100, KernelPhase.ActorDecision, 12, 45, 3);
        var otherPurpose = KernelRandomCursor.Create(77, 100, KernelPhase.ActorDecision, 12, 44, 4);

        Assert.NotEqual(first.NextFloat(), otherSubject.NextFloat());
        Assert.NotEqual(first.NextFloat(), otherPurpose.NextFloat());
    }

    [Fact]
    public void IntegerDrawsStayInsideBounds()
    {
        var random = KernelRandomCursor.Create(5, 6, 7, 8, 9, 10);

        for (var draw = 0; draw < 1000; draw++)
        {
            Assert.InRange(random.NextInt(17), 0, 16);
            Assert.InRange(random.NextInclusive(-4, 7), -4, 7);
        }
    }
}
