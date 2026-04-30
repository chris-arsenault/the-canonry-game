namespace Engine.Host.Tests;

using Engine.Domain.Config;
using Engine.Domain.Startup;
using Engine.Host;
using Engine.Kernel;

using IceRemembers;

public sealed class KernelRuntimeFactoryTests
{
    [Fact]
    public void Start_ReusesCompiledDomainAcrossDifferentRequestedSeeds()
    {
        var domain = new CountingDomainModule();
        var factory = KernelRuntimeFactory.ForDomains(domain);

        using var first = factory.Start(Request(domain.Name, seed: 100));
        using var second = factory.Start(Request(domain.Name, seed: 101));

        Assert.Equal(1, domain.CreateDomainConfigCalls);
        Assert.Equal(2, domain.BuildStartupSeedCalls);
        Assert.Equal(100, first.SeedIdentity.BaseSeed);
        Assert.Equal(101, second.SeedIdentity.BaseSeed);
    }

    [Fact]
    public void Start_DoesNotShareRuntimeDomainIdsAcrossCachedCompiledDomainStarts()
    {
        var domain = new CountingDomainModule();
        var factory = KernelRuntimeFactory.ForDomains(domain);

        using var first = factory.Start(Request(domain.Name, seed: 100));
        var settlementId = first.Host.DomainIds.KindIdOf(IceRemembersKinds.Settlement);
        first.Host.DomainIds.RegisterKind("runtime_only_kind");

        using var second = factory.Start(Request(domain.Name, seed: 101));

        Assert.Equal(settlementId, second.Host.DomainIds.KindIdOf(IceRemembersKinds.Settlement));
        Assert.Throws<KeyNotFoundException>(() => second.Host.DomainIds.KindIdOf("runtime_only_kind"));
    }

    [Fact]
    public void Start_LoadsKernelTuningFromJsonConfigFile()
    {
        var path = WriteTempConfig(
            ".json",
            """
            {
              "kernelTuning": {
                "programs": {
                  "lowAltitudeVerbs": {
                    "enabled": false
                  },
                  "generators": {
                    "retryFireTargets": false
                  },
                  "processes": {
                    "enabled": true,
                    "workCommitments": "pilot"
                  },
                  "gambits": {
                    "enabled": true
                  },
                  "planning": {
                    "defaultPlanner": "opportunity",
                    "goapTargetReservations": true,
                    "opportunityTargetCapacity": 3,
                    "kindPlannerOverrides": [
                      { "kindId": 1, "planner": "goap" }
                    ],
                    "entityPlannerOverrides": [
                      { "entityId": 2, "planner": "opportunity" }
                    ]
                  },
                  "pressures": {
                    "enabled": false
                  },
                  "upkeep": {
                    "agingModel": "late_life"
                  }
                }
              }
            }
            """);

        try
        {
            var options = StartAndReadSavedOptions(path);

            AssertConfiguredTuningOptions(options);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Start_UsesPressureDisabledKernelTuningByDefault()
    {
        var domain = new CountingDomainModule();
        var factory = KernelRuntimeFactory.ForDomains(domain);
        using var runtime = factory.Start(Request(domain.Name, seed: 100));
        using var stream = new MemoryStream();

        runtime.Host.SaveKernelState(stream);
        stream.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(stream);

        Assert.False(loaded.Options.KernelTuning.Programs.Pressures.Enabled);
    }

    [Fact]
    public void Start_CanEnablePressureTuningFromJsonConfigFile()
    {
        var path = WriteTempConfig(
            ".json",
            """
            {
              "kernelTuning": {
                "programs": {
                  "pressures": {
                    "enabled": true
                  }
                }
              }
            }
            """);

        try
        {
            var options = StartAndReadSavedOptions(path);

            Assert.True(options.KernelTuning.Programs.Pressures.Enabled);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Start_LoadsKernelTuningFromTomlConfigFile()
    {
        var path = WriteTempConfig(
            ".toml",
            """
            [kernel_tuning.programs.generators]
            retry_fire_targets = false

            [kernel_tuning.programs.movement]
            enabled = false

            [kernel_tuning.programs.low_altitude_verbs]
            enabled = false

            [kernel_tuning.programs.processes]
            enabled = true
            work_commitments = "pilot"

            [kernel_tuning.programs.gambits]
            enabled = true

            [kernel_tuning.programs.planning]
            default_planner = "opportunity"
            goap_target_reservations = true
            opportunity_target_capacity = 3

            [kernel_tuning.programs.planning.kind.1]
            planner = "goap"

            [kernel_tuning.programs.planning.entity.2]
            planner = "opportunity"

            [kernel_tuning.programs.pressures]
            enabled = false

            [kernel_tuning.programs.upkeep]
            aging_model = "late_life"
            """);

        try
        {
            var options = StartAndReadSavedOptions(path);

            AssertConfiguredTuningOptions(options);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(".yaml")]
    [InlineData(".yml")]
    public void Start_RejectsYamlKernelConfigFile(string extension)
    {
        var path = WriteTempConfig(extension, "kernel_tuning: {}\n");

        try
        {
            var domain = new CountingDomainModule();
            var factory = KernelRuntimeFactory.ForDomains(domain);
            var request = new KernelStartRequest(
                domain.Name,
                StartupSeedPolicy.Requested(100),
                KernelFunctionProfile.Batch,
                configFilePath: path);

            Assert.Throws<InvalidOperationException>(() => factory.Start(request));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Start_AppliesProgramEnabledOverridesFromKernelConfig()
    {
        var path = WriteTempConfig(
            ".json",
            """
            {
              "kernelTuning": {
                "programs": {
                  "lowAltitudeVerbs": {
                    "enabled": false
                  },
                  "processes": {
                    "enabled": true
                  }
                }
              }
            }
            """);

        try
        {
            var domain = new CountingDomainModule();
            var factory = KernelRuntimeFactory.ForDomains(domain);
            using var runtime = factory.Start(new KernelStartRequest(
                domain.Name,
                StartupSeedPolicy.Requested(100),
                KernelFunctionProfile.Batch,
                configFilePath: path));

            Assert.False(runtime.Host.ProgramIsEnabled(KernelProgramCodes.LowAltitudeVerbs));
            Assert.True(runtime.Host.ProgramIsEnabled(KernelProgramCodes.Processes));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static KernelStartRequest Request(string domainName, int seed) =>
        new(domainName, StartupSeedPolicy.Requested(seed), KernelFunctionProfile.Batch);

    private static void AssertConfiguredTuningOptions(SimulationOptions options)
    {
        Assert.False(options.KernelTuning.Programs.Generators.RetryFireTargets);
        Assert.Equal(ProgramEnabledOverrideCodes.Disabled, options.KernelTuning.Programs.Enabled.LowAltitudeVerbs);
        Assert.Equal(ProgramEnabledOverrideCodes.Enabled, options.KernelTuning.Programs.Enabled.Processes);
        Assert.Equal(ProgramEnabledOverrideCodes.Enabled, options.KernelTuning.Programs.Enabled.Gambits);
        Assert.Equal(ProcessWorkCommitmentCodes.Pilot, options.KernelTuning.Programs.Processes.WorkCommitmentCode);
        Assert.Equal(PlannerKindCodes.Opportunity, options.KernelTuning.Programs.Planning.DefaultPlannerKindCode);
        Assert.True(options.KernelTuning.Programs.Planning.GoapTargetReservationsEnabled);
        Assert.Equal(3, options.KernelTuning.Programs.Planning.OpportunityTargetCapacity);
        Assert.Contains(options.KernelTuning.Programs.Planning.KindPlannerOverrides, item =>
            item.KindId == 1 && item.PlannerKindCode == PlannerKindCodes.Goap);
        Assert.Contains(options.KernelTuning.Programs.Planning.EntityPlannerOverrides, item =>
            item.EntityId == 2 && item.PlannerKindCode == PlannerKindCodes.Opportunity);
        Assert.False(options.KernelTuning.Programs.Pressures.Enabled);
        Assert.Equal(UpkeepAgingModelCodes.LateLife, options.KernelTuning.Programs.Upkeep.AgingModelCode);
    }

    private static SimulationOptions StartAndReadSavedOptions(string configFilePath)
    {
        var domain = new CountingDomainModule();
        var factory = KernelRuntimeFactory.ForDomains(domain);
        using var runtime = factory.Start(new KernelStartRequest(
            domain.Name,
            StartupSeedPolicy.Requested(100),
            KernelFunctionProfile.Batch,
            configFilePath: configFilePath));
        using var stream = new MemoryStream();
        runtime.Host.SaveKernelState(stream);
        stream.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(stream);
        return loaded.Options;
    }

    private static string WriteTempConfig(string extension, string text)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
        File.WriteAllText(path, text);
        return path;
    }

    private sealed class CountingDomainModule : IKernelDomainModule
    {
        public string Name => "counting-domain";

        public int CreateDomainConfigCalls { get; private set; }

        public int BuildStartupSeedCalls { get; private set; }

        public DomainConfig CreateDomainConfig()
        {
            CreateDomainConfigCalls++;
            return IceRemembersDefinition.Create();
        }

        public DomainStartupSeedResult BuildStartupSeed(DomainStartupSeedRequest request)
        {
            BuildStartupSeedCalls++;
            return new DomainStartupSeedResult(
                DomainStartupSeed.Empty(mapRadius: 1),
                new StartupSeedIdentity(request.BaseSeed, attempt: 0));
        }

        public void ConfigureProjectionSymbols(KernelSymbolContext symbols)
        {
            ArgumentNullException.ThrowIfNull(symbols);
        }
    }
}
