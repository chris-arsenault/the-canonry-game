namespace Engine.Tests.Observability;

using System.IO;
using Engine.Domain.Config;
using Engine.Domain.IceRemembers;
using Engine.Kinds;
using Engine.Observability;
using Engine.Simulation;

public class RunLoggerGeneratorTests
{
    [Fact]
    public void RunLogger_EmitsGeneratorLines_ForShortRun()
    {
        var logPath = Path.Combine(
            Path.GetTempPath(),
            $"canonry-generator-runlogger-test-{System.Guid.NewGuid():N}.log");

        try
        {
            var domain = WithGenerators(CreateAlwaysApplicableGenerator());
            var sim = SimulationRunner.CreateFromDomain(domain);

            using (var logger = new RunLogger(logPath, sim))
            {
                for (var i = 0; i < 50; i++)
                {
                    sim.Tick();
                    logger.OnTickEnded();
                }
            }

            var lines = File.ReadAllLines(logPath);
            var tick25Index = Array.FindIndex(
                lines,
                line => line.StartsWith("=== t=25 ===", StringComparison.Ordinal));

            Assert.True(tick25Index >= 0, "Expected a tick-25 block in the run log.");
            Assert.Contains(
                lines.Skip(tick25Index + 1),
                line => line.StartsWith("==  gen:", StringComparison.Ordinal));
        }
        finally
        {
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }
    }

    private static SemanticGeneratorConfig CreateAlwaysApplicableGenerator() =>
        new()
        {
            Id = "hero_emergence",
            Name = "Hero Emergence",
            Target = new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$target",
                    Kind = "home_base",
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Settlement,
                    Direction = TriggerDirection.Any,
                },
            ],
        };

    private static DomainConfig WithGenerators(params SemanticGeneratorConfig[] generators)
    {
        var config = IceRemembersDefinition.Create();
        var semantics = config.Semantics!;

        return new DomainConfig
        {
            Kinds = config.Kinds,
            Processes = config.Processes,
            Relationships = config.Relationships,
            Pressures = config.Pressures,
            Lifecycles = config.Lifecycles,
            Resources = config.Resources,
            Cultures = config.Cultures,
            Semantics = new SemanticDomainConfig
            {
                Stocks = semantics.Stocks,
                Verbs = semantics.Verbs,
                Flows = semantics.Flows,
                Modifiers = semantics.Modifiers,
                Generators = generators,
            },
        };
    }
}
