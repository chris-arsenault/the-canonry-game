namespace Engine.Domain.Tests.Architecture;

public sealed class GameplayBoundaryArchitectureTests
{
    [Fact]
    public void GameplayPackage_ExistsAsDedicatedMiddleCompileLayer()
    {
        var root = FindRepoRoot();
        var gameplayProject = Path.Combine(root.FullName, "src", "Engine.Gameplay", "Engine.Gameplay.csproj");

        Assert.True(
            File.Exists(gameplayProject),
            "Reusable gameplay mechanics need a dedicated Engine.Gameplay package between domain content and kernel primitives.");
    }

    [Fact]
    public void GameplayPackage_IsReferencedByDomainAndHostButNotKernel()
    {
        var root = FindRepoRoot();
        var requiredReferences = new[]
        {
            "src/Engine.Domain/Engine.Domain.csproj",
            "src/Engine.Host/Engine.Host.csproj",
        };
        var missing = new List<string>();
        foreach (var project in requiredReferences)
        {
            var text = ReadSource(root, project);
            if (!text.Contains("Engine.Gameplay.csproj", StringComparison.Ordinal))
            {
                missing.Add(project);
            }
        }

        var kernelProject = ReadSource(root, "src/Engine.Kernel/Engine.Kernel.csproj");

        Assert.Empty(missing);
        Assert.DoesNotContain("Engine.Gameplay.csproj", kernelProject, StringComparison.Ordinal);
    }

    [Fact]
    public void GameplayPackage_HasOwnTestProject()
    {
        var root = FindRepoRoot();
        var testProject = Path.Combine(root.FullName, "tests", "Engine.Gameplay.Tests", "Engine.Gameplay.Tests.csproj");

        Assert.True(
            File.Exists(testProject),
            "Engine.Gameplay needs its own tests for gameplay validation and compile-down behavior.");
    }

    [Fact]
    public void GameplayPackage_DefinesGameplayDefinitionModel()
    {
        var root = FindRepoRoot();
        var gameplaySource = ReadGameplaySourceIfPresent(root);
        var requiredFragments = new[]
        {
            "GameplayDefinition",
            "LowAltitudeVerbDefinition",
            "LocalityConfig",
            "AttributeEffectDefinition",
            "GameplayModule",
        };
        var missing = MissingFragments(gameplaySource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void GameplayPackage_DefinesGameplayCompilerEntryPoint()
    {
        var root = FindRepoRoot();
        var gameplaySource = ReadGameplaySourceIfPresent(root);
        var requiredFragments = new[]
        {
            "GameplayCompiler",
            "CompileGameplay",
            "KernelProgramSet",
            "DomainIdRegistry",
        };
        var missing = MissingFragments(gameplaySource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void GameplayPackage_DefinesGameplayValidationResult()
    {
        var root = FindRepoRoot();
        var gameplaySource = ReadGameplaySourceIfPresent(root);
        var requiredFragments = new[]
        {
            "GameplayValidationResult",
            "GameplayValidationError",
            "Validate",
        };
        var missing = MissingFragments(gameplaySource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void DomainConfig_DoesNotOwnReusableGameplayMechanicTypes()
    {
        var root = FindRepoRoot();
        var domainConfig = ReadSource(root, "src/Engine.Domain/Config/DomainConfig.cs");
        var forbiddenTypes = new[]
        {
            "LowAltitudeVerbConfig",
            "LowAltitudeVerbOperation",
            "LowAltitudeVerbTargetMode",
            "LocalityConfig",
            "KindLocalityConfig",
            "EffectsConfig",
            "AttributeDeltaEffectConfig",
            "EffectCondition",
        };
        var violations = ContainedFragments(domainConfig, forbiddenTypes);

        Assert.Empty(violations);
    }

    [Fact]
    public void DomainConfig_ReferencesGameplayDefinitionsOnlyAsSelectedModules()
    {
        var root = FindRepoRoot();
        var domainConfig = ReadSource(root, "src/Engine.Domain/Config/DomainConfig.cs");
        var requiredFragments = new[]
        {
            "Gameplay",
            "GameplayDefinition",
        };
        var missing = MissingFragments(domainConfig, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void IceRemembers_DoesNotConstructKernelGameplayConfigTypesDirectly()
    {
        var root = FindRepoRoot();
        var iceRemembersSource = ReadAllSourcesUnder(root, "src/IceRemembers");
        var forbiddenFragments = new[]
        {
            "new LowAltitudeVerbConfig",
            "new LocalityConfig",
            "new EffectsConfig",
            "new AttributeDeltaEffectConfig",
            "LowAltitudeVerbOperation.",
            "LowAltitudeVerbTargetMode.",
            "EffectCondition.",
        };
        var violations = ContainedFragments(iceRemembersSource, forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void IceRemembers_SelectsGameplayModulesRatherThanImplementingMechanics()
    {
        var root = FindRepoRoot();
        var iceRemembersSource = ReadAllSourcesUnder(root, "src/IceRemembers");
        var requiredFragments = new[]
        {
            "Gameplay",
            "LowAltitude",
            "Influence",
            "Locality",
        };
        var missing = MissingFragments(iceRemembersSource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void DomainCompiler_DoesNotCompileGameplayProgramsDirectly()
    {
        var root = FindRepoRoot();
        var forbiddenFragments = new[]
        {
            "BuildLowAltitudeVerbProgram",
            "BuildLocalityRules",
            "BuildEffectsProgram",
            "BuildMovementProgram",
            "LowAltitudeVerbRule",
            "KindLocalityRule",
            "AttributeDeltaEffectRule",
        };
        var violations = ContainedFragments(
            ReadAllSourcesUnder(root, "src/Engine.Domain/Compilation"),
            forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void DomainCompiler_DelegatesGameplayCompilationToGameplayPackage()
    {
        var root = FindRepoRoot();
        var compilerSource = ReadAllSourcesUnder(root, "src/Engine.Domain/Compilation");
        var requiredFragments = new[]
        {
            "Engine.Gameplay",
            "GameplayCompiler",
            "CompileGameplay",
        };
        var missing = MissingFragments(compilerSource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void GameplayPackage_OwnsCompilationIntoKernelProgramData()
    {
        var root = FindRepoRoot();
        var gameplaySource = ReadGameplaySourceIfPresent(root);
        var requiredFragments = new[]
        {
            "LowAltitudeVerb",
            "Locality",
            "Effects",
            "Compile",
            "KernelPrograms",
        };
        var missing = MissingFragments(gameplaySource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void GameplayCompiler_CompilesLowAltitudeVerbsIntoPrimitiveKernelRules()
    {
        var root = FindRepoRoot();
        var gameplaySource = ReadGameplaySourceIfPresent(root);
        var requiredFragments = new[]
        {
            "LowAltitudeVerbRule",
            "OperationCode",
            "TargetModeCode",
            "RelationshipDirectionCode",
            "TerrainIds",
        };
        var missing = MissingFragments(gameplaySource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void GameplayCompiler_CompilesLocalityIntoPrimitiveKernelRules()
    {
        var root = FindRepoRoot();
        var gameplaySource = ReadGameplaySourceIfPresent(root);
        var requiredFragments = new[]
        {
            "TerrainLocalityRule",
            "KindLocalityRule",
            "RelationshipLocalityRule",
            "CapacityEmission",
            "Attractor",
        };
        var missing = MissingFragments(gameplaySource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void GameplayCompiler_CompilesEffectsIntoPrimitiveKernelRules()
    {
        var root = FindRepoRoot();
        var gameplaySource = ReadGameplaySourceIfPresent(root);
        var requiredFragments = new[]
        {
            "AttributeDeltaEffectRule",
            "ConditionCode",
            "RelationshipKindId",
            "PlaneCode",
            "DeltaPerTick",
        };
        var missing = MissingFragments(gameplaySource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void GameplayCompiler_DenormalizesAffordancesInsteadOfRuntimePeeking()
    {
        var root = FindRepoRoot();
        var gameplaySource = ReadGameplaySourceIfPresent(root);
        var requiredFragments = new[]
        {
            "AffordanceDefinition",
            "ShapeCode",
            "ActorKind",
            "CandidateKind",
        };
        var missing = MissingFragments(gameplaySource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void GameplayValidator_RejectsIllegalVerbOperationTargetPlaneCombinations()
    {
        var root = FindRepoRoot();
        var gameplaySource = ReadGameplaySourceIfPresent(root);
        var requiredFragments = new[]
        {
            "ValidateLowAltitudeVerb",
            "ExpandCoverage",
            "MoveHeadquarters",
            "Terrain",
            "Semantic",
            "Physical",
        };
        var missing = MissingFragments(gameplaySource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void GameplayValidator_RejectsRuntimeFallthroughAsBehaviorDefinition()
    {
        var root = FindRepoRoot();
        var gameplaySource = ReadGameplaySourceIfPresent(root);
        var requiredFragments = new[]
        {
            "InvalidOperation",
            "Unsupported",
            "NoRuntimeFallthrough",
        };
        var missing = MissingFragments(gameplaySource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void GameplayValidator_ValidatesRelationshipMaintenancePlaneSemantics()
    {
        var root = FindRepoRoot();
        var gameplaySource = ReadGameplaySourceIfPresent(root);
        var requiredFragments = new[]
        {
            "RelationshipMaintenance",
            "SourceInfluenceAtTarget",
            "TargetInfluenceAtSource",
            "ContactMemory",
            "Plane",
        };
        var missing = MissingFragments(gameplaySource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void GameplayDefinitions_RepresentInitialInfluenceExplicitly()
    {
        var root = FindRepoRoot();
        var gameplaySource = ReadGameplaySourceIfPresent(root);
        var requiredFragments = new[]
        {
            "InitialInfluence",
            "InitialInfluenceRule",
            "InfluenceValue",
            "Plane",
        };
        var missing = MissingFragments(gameplaySource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void GameplayDefinitions_RepresentInfluenceAsNamedMapControlSystem()
    {
        var root = FindRepoRoot();
        var gameplaySource = ReadGameplaySourceIfPresent(root);
        var requiredFragments = new[]
        {
            "MapControl",
            "Influence",
            "Coverage",
            "Territory",
        };
        var missing = MissingFragments(gameplaySource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void KernelConstants_DoNotExposeGameplayPolicyVocabulary()
    {
        var root = FindRepoRoot();
        var kernelConstants = ReadSource(root, "src/Engine.Constants/KernelCodes.cs");
        var forbiddenFragments = new[]
        {
            "GrowInfluence",
            "ConsolidateInfluence",
            "ReduceInfluence",
            "ExpandCoverage",
            "MoveHeadquarters",
            "OwnInfluence",
            "ForeignInfluence",
            "RelatedInfluenceAtPhysicalLocation",
            "MapSupportAtLocation",
            "MapThreatAtLocation",
            "MapCrowdingLoad",
            "MapCrowdingComfort",
        };
        var violations = ContainedFragments(kernelConstants, forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void KernelConstants_ExposeOnlyPrimitiveGameplayBytecodeSlots()
    {
        var root = FindRepoRoot();
        var constants = ReadSource(root, "src/Engine.Constants/KernelCodes.cs");
        var requiredFragments = new[]
        {
            "ProgramOpcode",
            "TargetSelectorCode",
            "EffectConditionCode",
        };
        var missing = MissingFragments(constants, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void LowAltitudeKernelRuntime_DoesNotSwitchOnGameplayVerbShapes()
    {
        var root = FindRepoRoot();
        var lowAltitudeRuntime = ReadAllSourcesUnder(root, "src/Engine.Kernel/Programs/LowAltitudeVerbs");
        var forbiddenFragments = new[]
        {
            "case LowAltitudeVerbOperationCodes.GrowInfluence",
            "case LowAltitudeVerbOperationCodes.ConsolidateInfluence",
            "case LowAltitudeVerbOperationCodes.ReduceInfluence",
            "case LowAltitudeVerbOperationCodes.ExpandCoverage",
            "case LowAltitudeVerbOperationCodes.MoveHeadquarters",
            "TryExpandCoverage",
            "TryMoveHeadquarters",
        };
        var violations = ContainedFragments(lowAltitudeRuntime, forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void LowAltitudeKernelRuntime_ExecutesCompiledPrimitiveStepsInsteadOfNamedOperations()
    {
        var root = FindRepoRoot();
        var lowAltitudeRuntime = ReadAllSourcesUnder(root, "src/Engine.Kernel/Programs/LowAltitudeVerbs");
        var requiredFragments = new[]
        {
            "StepCount",
            "StepOpcode",
            "StepArgument",
            "ApplyStep",
        };
        var missing = MissingFragments(lowAltitudeRuntime, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void LowAltitudeVerbProgramData_DoesNotExposeGiantRegisterVerbParameterList()
    {
        var root = FindRepoRoot();
        var lowAltitudeData = ReadSource(root, "src/Engine.Kernel/Programs/LowAltitudeVerbs/LowAltitudeVerbProgramData.cs");
        var forbiddenFragments = new[]
        {
            "int minCommitmentTicks",
            "float affordanceWeight",
            "float coverageGrowthChance",
            "float headquartersMoveChance",
            "float requiredInfluence",
        };
        var violations = ContainedFragments(lowAltitudeData, forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void KernelPrograms_DoNotReadOtherProgramRawDataTables()
    {
        var root = FindRepoRoot();
        var kernelPrograms = ReadAllSourcesUnder(root, "src/Engine.Kernel/Programs");
        var forbiddenFragments = new[]
        {
            "simulation.Generators.Data",
            ".Data.TargetKindIds",
            ".Data.BindingStarts",
            ".Data.BindingCounts",
            ".Data.BindingKindIds",
        };
        var violations = ContainedFragments(kernelPrograms, forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void CrossProgramReads_AreDeclaredThroughProgramDependencyContracts()
    {
        var root = FindRepoRoot();
        var kernelSource = ReadAllSourcesUnder(root, "src/Engine.Kernel");
        var requiredFragments = new[]
        {
            "ProgramReadDependency",
            "ProgramWriteDependency",
            "DependencyLag",
            "SameTick",
            "PreviousTick",
        };
        var missing = MissingFragments(kernelSource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void Affordance_IsProvidedByCompiledGameplaySignalNotGeneratorRuntimeData()
    {
        var root = FindRepoRoot();
        var lowAltitudeRuntime = ReadAllSourcesUnder(root, "src/Engine.Kernel/Programs/LowAltitudeVerbs");
        var requiredFragments = new[]
        {
            "AffordanceCount",
            "AffordanceShapeCode",
        };
        var forbiddenFragments = new[]
        {
            "GeneratorProgramData",
            "GeneratorHasBindingKind",
            "GeneratorConnectsKinds",
        };
        var missing = MissingFragments(lowAltitudeRuntime, requiredFragments);
        var violations = ContainedFragments(lowAltitudeRuntime, forbiddenFragments);

        Assert.Empty(missing);
        Assert.Empty(violations);
    }

    [Fact]
    public void InitialInfluence_IsExplicitGameplayOrSeedDataNotKernelRuntimeMagic()
    {
        var root = FindRepoRoot();
        var lowAltitudeRuntime = ReadAllSourcesUnder(root, "src/Engine.Kernel/Programs/LowAltitudeVerbs");
        var forbiddenFragments = new[]
        {
            "SeedHomeInfluence",
            "HomeInfluenceSeeded",
            "MarkHomeInfluenceSeeded",
            "35f",
        };
        var violations = ContainedFragments(lowAltitudeRuntime, forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void KernelRuntime_DoesNotContainGameplayMagicNumbersForInfluenceDefaults()
    {
        var root = FindRepoRoot();
        var kernelPrograms = ReadAllSourcesUnder(root, "src/Engine.Kernel/Programs");
        var forbiddenFragments = new[]
        {
            "< 35f",
            "35f, tick",
            "InfluenceSeed",
        };
        var violations = ContainedFragments(kernelPrograms, forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void RelationshipMaintenanceCompiler_PreservesPlaneForInfluenceConditions()
    {
        var root = FindRepoRoot();
        var compiler = ReadSource(root, "src/Engine.Domain/Compilation/DomainCompiler.Mapping.cs");

        Assert.DoesNotContain(
            "condition is RelationshipMaintenanceCondition.DistanceWithin",
            compiler,
            StringComparison.Ordinal);
        Assert.Contains(
            "RelationshipMaintenanceCondition.SourceInfluenceAtTarget",
            compiler,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            ": PlaneCodes.None",
            compiler,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RelationshipRuntime_ContactMemoryUsesConfiguredPlaneOnly()
    {
        var root = FindRepoRoot();
        var relationshipRuntime = ReadSource(root, "src/Engine.Kernel/Programs/Relationships/RelationshipProgramRuntime.cs");

        Assert.DoesNotContain(
            "PhysicalDistance(world, sourceId, targetId) <= 1 ||",
            relationshipRuntime,
            StringComparison.Ordinal);
        Assert.Contains(
            "MaintenancePlaneCode",
            relationshipRuntime,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RelationshipRuntime_DoesNotDefaultInfluencePlaneToPhysical()
    {
        var root = FindRepoRoot();
        var relationshipRuntime = ReadSource(root, "src/Engine.Kernel/Programs/Relationships/RelationshipProgramRuntime.cs");

        Assert.DoesNotContain(
            "planeCode == PlaneCodes.None ? PlaneCodes.Physical",
            relationshipRuntime,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DefinitionGraph_OwnsPrimaryGameplayBehaviorSurfaces()
    {
        var root = FindRepoRoot();
        var definitionGraphSource = ReadAllSourcesUnder(root, "src/Engine.Domain/Graph");
        var requiredFragments = new[]
        {
            "LowAltitudeVerb",
            "Locality",
            "AttributeDeltaEffect",
            "Gameplay",
        };
        var missing = MissingFragments(definitionGraphSource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void DefinitionGraph_ModelsGameplayReadsAndWrites()
    {
        var root = FindRepoRoot();
        var definitionGraphSource = ReadAllSourcesUnder(root, "src/Engine.Domain/Graph");
        var requiredFragments = new[]
        {
            "GameplayRead",
            "GameplayWrite",
            "LocalityRead",
            "InfluenceWrite",
            "AttributeDeltaWrite",
        };
        var missing = MissingFragments(definitionGraphSource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void DefinitionGraphValidator_ChecksGameplayBehaviorSemanticDrift()
    {
        var root = FindRepoRoot();
        var definitionGraphSource = ReadAllSourcesUnder(root, "src/Engine.Domain/Graph");
        var requiredFragments = new[]
        {
            "ValidateGameplay",
            "ValidateLowAltitudeVerb",
            "ValidateLocality",
            "ValidateEffects",
        };
        var missing = MissingFragments(definitionGraphSource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void DefinitionGraph_IncludesGameplayProgramNodes()
    {
        var root = FindRepoRoot();
        var definitionGraphSource = ReadAllSourcesUnder(root, "src/Engine.Domain/Graph");
        var requiredFragments = new[]
        {
            "GameplayNodes",
            "DefinitionGameplayNode",
            "DefinitionLowAltitudeVerbNode",
            "DefinitionLocalityNode",
            "DefinitionEffectNode",
        };
        var missing = MissingFragments(definitionGraphSource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void KernelProgramOrder_ValidatesDependenciesAtStartup()
    {
        var root = FindRepoRoot();
        var kernelSource = ReadAllSourcesUnder(root, "src/Engine.Kernel");
        var requiredFragments = new[]
        {
            "ValidateProgramDependencies",
            "ProgramDependencyViolation",
            "RegisterDefaultPrograms",
        };
        var missing = MissingFragments(kernelSource, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void HostInstallsGameplayCompiledProgramsInsteadOfDomainCompiledLowAltitudePrograms()
    {
        var root = FindRepoRoot();
        var hostSource = ReadAllSourcesUnder(root, "src/Engine.Host");
        var requiredFragments = new[]
        {
            "GameplayCompiler",
            "InstallGameplay",
        };
        var forbiddenFragments = new[]
        {
            "InstallLowAltitudeVerbs",
            "InstallLocality",
            "InstallEffects",
        };
        var missing = MissingFragments(hostSource, requiredFragments);
        var violations = ContainedFragments(hostSource, forbiddenFragments);

        Assert.Empty(missing);
        Assert.Empty(violations);
    }

    [Fact]
    public void RuntimeSourceCodes_SeparateGameplaySourceFromDomainContentSource()
    {
        var root = FindRepoRoot();
        var domainRuntimeNames = ReadSource(root, "src/Engine.Domain/Compilation/DomainRuntimeNames.cs");
        var requiredFragments = new[]
        {
            "GameplaySourceCode",
            "MapControlSourceCode",
            "GameplayEffectsSourceCode",
        };
        var missing = MissingFragments(domainRuntimeNames, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void ObsoleteMovementProgram_IsRemovedAfterLowAltitudeGameplayCompilerOwnsMovement()
    {
        var root = FindRepoRoot();
        var movementProgramPath = Path.Combine(root.FullName, "src", "Engine.Kernel", "Programs", "Movement");
        var compilerSource = ReadAllSourcesUnder(root, "src/Engine.Domain/Compilation");

        Assert.False(
            Directory.Exists(movementProgramPath),
            "Legacy MovementProgram should not coexist once low-altitude gameplay owns movement.");
        Assert.DoesNotContain("BuildMovementProgram", compilerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void KernelProgramSet_DoesNotExposeBothMovementAndLowAltitudeVerbMovement()
    {
        var root = FindRepoRoot();
        var programSet = ReadSource(root, "src/Engine.Domain/Compilation/KernelPrograms/KernelProgramSet.cs");

        Assert.DoesNotContain("MovementProgram Movement", programSet, StringComparison.Ordinal);
        Assert.Contains("LowAltitude", programSet, StringComparison.Ordinal);
    }

    [Fact]
    public void StaleDomainMovementProgramSurface_IsNotReintroduced()
    {
        var root = FindRepoRoot();
        var liveSource = string.Concat(
            ReadSource(root, "src/Engine.Domain/Compilation/KernelPrograms/KernelProgramSet.cs"),
            ReadAllSourcesUnder(root, "src/Engine.Domain/Compilation"),
            ReadSource(root, "src/Engine.Kernel/KernelRuntimeCodes.cs"),
            ReadSource(root, "src/Engine.Kernel/SimulationOptions.cs"));
        var forbiddenFragments = new[]
        {
            "MovementSourceCodeId",
            "MovementRuleCapacity",
            "MovementProgram Movement",
            "BuildMovementProgram",
        };
        var violations = ContainedFragments(liveSource, forbiddenFragments);

        Assert.Empty(violations);
    }

    [Fact]
    public void GameplayBoundaryPrinciples_AreTrackedInStableSystemsDocs()
    {
        var root = FindRepoRoot();
        var plan = string.Concat(
            ReadSource(root, "docs/architecture.md"),
            ReadSource(root, "docs/systems/simulation-principles.md"));
        var requiredFragments = new[]
        {
            "Engine.Gameplay",
            "kernel",
            "Low altitude",
            "gameplay",
        };
        var missing = MissingFragments(plan, requiredFragments);

        Assert.Empty(missing);
    }

    [Fact]
    public void KernelPrograms_ModifyEntityCoordinatesOnlyThroughUnifiedMovementApi()
    {
        var root = FindRepoRoot();
        var programDirectory = Path.Combine(root.FullName, "src", "Engine.Kernel", "Programs");
        var traversalDirectory = Path.Combine(programDirectory, "Traversal") + Path.DirectorySeparatorChar;
        var forbiddenCalls = new[]
        {
            "simulation.MoveEntityPhysical(",
            "simulation.MoveEntitySemantic(",
            "MoveEntityPhysicalUnchecked(",
            "MoveEntitySemanticUnchecked(",
            "World.MoveEntityPhysical(",
            "World.MoveEntitySemantic(",
            "world.MoveEntityPhysical(",
            "world.MoveEntitySemantic(",
            "simulation.Movement.TryMovePhysical(",
            "simulation.Movement.TryMoveSemantic(",
        };
        var violations = new List<string>();
        foreach (var file in Directory.GetFiles(programDirectory, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            if (file.StartsWith(traversalDirectory, StringComparison.Ordinal))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            violations.AddRange(ContainedFragments(source, forbiddenCalls).Select(fragment => $"{file}: {fragment}"));
        }

        var effectExecutor = ReadSource(root, "src/Engine.Kernel/KernelEffectExecutor.cs");
        var requiredExecutorFragments = new[]
        {
            "simulation.Movement.Navigate(",
            "simulation.Movement.Place(",
        };
        violations.AddRange(MissingFragments(effectExecutor, requiredExecutorFragments)
            .Select(fragment => $"KernelEffectExecutor missing movement delegation: {fragment}"));

        Assert.Empty(violations);
    }

    private static string ReadGameplaySourceIfPresent(DirectoryInfo root)
    {
        var directory = Path.Combine(root.FullName, "src", "Engine.Gameplay");
        return Directory.Exists(directory)
            ? ReadAllSourcesUnder(root, "src/Engine.Gameplay")
            : string.Empty;
    }

    private static List<string> ContainedFragments(string text, IReadOnlyList<string> fragments)
    {
        var violations = new List<string>();
        foreach (var fragment in fragments)
        {
            if (text.Contains(fragment, StringComparison.Ordinal))
            {
                violations.Add(fragment);
            }
        }

        return violations;
    }

    private static List<string> MissingFragments(string text, IReadOnlyList<string> fragments)
    {
        var violations = new List<string>();
        foreach (var fragment in fragments)
        {
            if (!text.Contains(fragment, StringComparison.Ordinal))
            {
                violations.Add(fragment);
            }
        }

        return violations;
    }

    private static string ReadSource(DirectoryInfo root, string relativePath) =>
        File.ReadAllText(Path.Combine(root.FullName, relativePath));

    private static string ReadAllSourcesUnder(DirectoryInfo root, string relativeDirectory)
    {
        var directory = Path.Combine(root.FullName, relativeDirectory);
        if (!Directory.Exists(directory))
        {
            return string.Empty;
        }

        return string.Join(
            Environment.NewLine,
            Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal)
                .Select(File.ReadAllText));
    }

    private static DirectoryInfo FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TheCanonryGame.csproj")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("Could not find repository root.");
    }
}
