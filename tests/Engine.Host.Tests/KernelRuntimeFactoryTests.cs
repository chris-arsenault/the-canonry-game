namespace Engine.Host.Tests;

using Engine.Domain.Config;
using Engine.Domain.Startup;
using Engine.Host;
using Engine.Kernel;

using IceRemembers;

// Slow: builds a full startup seed and kernel runtime; excluded from the fast loop.
[Trait("Category", "Slow")]
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
    public void Start_UsesMatchingPrebuiltStartupSeedWithoutCallingDomainBuilder()
    {
        var domain = new CountingDomainModule();
        var factory = KernelRuntimeFactory.ForDomains(domain);
        var prebuilt = new DomainStartupSeedResult(
            DomainStartupSeed.Empty(mapRadius: 1),
            new StartupSeedIdentity(baseSeed: 100, attempt: 3));

        using var runtime = factory.Start(new KernelStartRequest(
            domain.Name,
            StartupSeedPolicy.Requested(100),
            KernelFunctionProfile.Batch,
            startupSeed: prebuilt));

        Assert.Equal(0, domain.BuildStartupSeedCalls);
        Assert.Equal(3, runtime.SeedIdentity.Attempt);
    }

    [Fact]
    public void Start_RejectsPrebuiltStartupSeedForDifferentBaseSeed()
    {
        var domain = new CountingDomainModule();
        var factory = KernelRuntimeFactory.ForDomains(domain);
        var prebuilt = new DomainStartupSeedResult(
            DomainStartupSeed.Empty(mapRadius: 1),
            new StartupSeedIdentity(baseSeed: 101, attempt: 0));

        var exception = Assert.Throws<InvalidOperationException>(() => factory.Start(new KernelStartRequest(
            domain.Name,
            StartupSeedPolicy.Requested(100),
            KernelFunctionProfile.Batch,
            startupSeed: prebuilt)));

        Assert.Contains("does not match", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, domain.BuildStartupSeedCalls);
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
                  "minorActorSubstepsPerPulse": 7,
                  "cultureInfluenceActionScale": 0.25,
                  "lowAltitudeVerbs": {
                    "enabled": false,
                    "opportunityRate": 0.35,
                    "opportunityBonus": 1.75,
                    "recentTargetCooldownTicks": 77,
                    "recentTargetRingSize": 2,
                    "cellSaturationFactor": 0.5,
                    "exploreDistanceScale": 4.5,
                    "terrainDistanceScale": 5.5,
                    "entityKindDistanceScale": 6.5,
                    "relationshipTargetDistanceScale": 7.5,
                    "ownInfluenceDistanceScale": 8.5,
                    "foreignInfluenceDistanceScale": 9.5
                  },
                  "generators": {
                    "retryFireTargets": false,
                    "pulseWidthTicks": 17,
                    "firesPerPulse": 2
                  },
                  "gambits": {
                    "enabled": true
                  },
                  "lifecycle": {
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
    public void Start_LoadsKernelTuningFromTomlConfigFile()
    {
        var path = WriteTempConfig(
            ".toml",
            """
            [kernel_tuning.programs.generators]
            retry_fire_targets = false
            pulse_width_ticks = 17
            fires_per_pulse = 2

            [kernel_tuning.programs]
            minor_actor_main_tick_ratio = 7
            culture_influence_action_scale = 0.25

            [kernel_tuning.programs.movement]
            enabled = false

            [kernel_tuning.programs.low_altitude_verbs]
            enabled = false
            opportunity_rate = 0.35
            opportunity_bonus = 1.75
            recent_target_cooldown_ticks = 77
            recent_target_ring_size = 2
            cell_saturation_factor = 0.5
            explore_distance_scale = 4.5
            terrain_distance_scale = 5.5
            entity_kind_distance_scale = 6.5
            relationship_target_distance_scale = 7.5
            own_influence_distance_scale = 8.5
            foreign_influence_distance_scale = 9.5

            [kernel_tuning.programs.gambits]
            enabled = true

            [kernel_tuning.programs.lifecycle]
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
                  "gambits": {
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
            Assert.True(runtime.Host.ProgramIsEnabled(KernelProgramCodes.Gambits));
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
        Assert.Equal(7, options.KernelTuning.Programs.MinorActorSubstepsPerPulse);
        Assert.Equal(FixedScalar.FromSingle(0.25f), options.KernelTuning.Programs.CultureInfluenceActionScale);
        Assert.Equal(17, options.KernelTuning.Programs.Generators.PulseWidthTicks);
        Assert.Equal(2, options.KernelTuning.Programs.Generators.FiresPerPulse);
        Assert.Equal(ProgramEnabledOverrideCodes.Disabled, options.KernelTuning.Programs.Enabled.LowAltitudeVerbs);
        Assert.Equal(ProgramEnabledOverrideCodes.Enabled, options.KernelTuning.Programs.Enabled.Gambits);
        Assert.Equal(LifecycleAgingModelCodes.LateLife, options.KernelTuning.Programs.Lifecycle.AgingModelCode);
        Assert.Equal(FixedScalar.FromSingle(0.35f), options.KernelTuning.Programs.LowAltitudeVerbs.OpportunityRate);
        Assert.Equal(FixedScalar.FromSingle(1.75f), options.KernelTuning.Programs.LowAltitudeVerbs.OpportunityBonus);
        Assert.Equal(77, options.KernelTuning.Programs.LowAltitudeVerbs.RecentTargetCooldownTicks);
        Assert.Equal(2, options.KernelTuning.Programs.LowAltitudeVerbs.RecentTargetRingSize);
        Assert.Equal(FixedScalar.FromSingle(0.5f), options.KernelTuning.Programs.LowAltitudeVerbs.CellSaturationFactor);
        Assert.Equal(FixedScalar.FromSingle(4.5f), options.KernelTuning.Programs.LowAltitudeVerbs.ExploreDistanceScale);
        Assert.Equal(FixedScalar.FromSingle(5.5f), options.KernelTuning.Programs.LowAltitudeVerbs.TerrainDistanceScale);
        Assert.Equal(FixedScalar.FromSingle(6.5f), options.KernelTuning.Programs.LowAltitudeVerbs.EntityKindDistanceScale);
        Assert.Equal(FixedScalar.FromSingle(7.5f), options.KernelTuning.Programs.LowAltitudeVerbs.RelationshipTargetDistanceScale);
        Assert.Equal(FixedScalar.FromSingle(8.5f), options.KernelTuning.Programs.LowAltitudeVerbs.OwnInfluenceDistanceScale);
        Assert.Equal(FixedScalar.FromSingle(9.5f), options.KernelTuning.Programs.LowAltitudeVerbs.ForeignInfluenceDistanceScale);
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
