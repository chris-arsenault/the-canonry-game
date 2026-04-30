namespace Engine.Host;

using System.Text.Json;

using Engine.Kernel;

internal static class KernelStartupConfigLoader
{
    private static readonly ProgramSectionMapping[] ProgramSectionMappings =
    [
        new("KERNELTUNING.PROGRAMS.MOVEMENT", KernelProgramCodes.Movement),
        new("KERNELTUNING.PROGRAMS.LOWALTITUDEVERBS", KernelProgramCodes.LowAltitudeVerbs),
        new("KERNELTUNING.PROGRAMS.LOW_ALTITUDE_VERBS", KernelProgramCodes.LowAltitudeVerbs),
        new("KERNELTUNING.PROGRAMS.MAPACTIVITY", KernelProgramCodes.MapActivity),
        new("KERNELTUNING.PROGRAMS.MAP_ACTIVITY", KernelProgramCodes.MapActivity),
        new("KERNELTUNING.PROGRAMS.MAPSIGNALS", KernelProgramCodes.MapSignals),
        new("KERNELTUNING.PROGRAMS.MAP_SIGNALS", KernelProgramCodes.MapSignals),
        new("KERNELTUNING.PROGRAMS.EFFECTS", KernelProgramCodes.Effects),
        new("KERNELTUNING.PROGRAMS.UPKEEP", KernelProgramCodes.Upkeep),
        new("KERNELTUNING.PROGRAMS.PROCESSES", KernelProgramCodes.Processes),
        new("KERNELTUNING.PROGRAMS.GAMBITS", KernelProgramCodes.Gambits),
        new("KERNELTUNING.PROGRAMS.RELATIONSHIPS", KernelProgramCodes.Relationships),
        new("KERNELTUNING.PROGRAMS.PRESSURES", KernelProgramCodes.Pressures),
        new("KERNELTUNING.PROGRAMS.GENERATORS", KernelProgramCodes.Generators),
        new("KERNELTUNING.PROGRAMS.DEGENERACY", KernelProgramCodes.Degeneracy),
    ];

    public static KernelTuningOptions LoadKernelTuning(string? configFilePath)
    {
        var tuning = new KernelTuningOptions();
        if (configFilePath is null)
        {
            return tuning;
        }

        var extension = Path.GetExtension(configFilePath);
        if (IsJson(extension))
        {
            return ApplyJson(File.ReadAllText(configFilePath), tuning);
        }

        if (IsToml(extension))
        {
            return ApplyToml(File.ReadAllLines(configFilePath), tuning);
        }

        if (IsYaml(extension))
        {
            throw new InvalidOperationException("Kernel startup config does not support YAML. Use JSON or TOML.");
        }

        throw new InvalidOperationException("Kernel startup config must be JSON or TOML.");
    }

    private static KernelTuningOptions ApplyJson(string text, KernelTuningOptions tuning)
    {
        using var document = JsonDocument.Parse(text);
        if (!TryGetObject(document.RootElement, out var root))
        {
            return tuning;
        }

        if (!TryGetObject(root, out var kernelTuning, "kernelTuning", "kernel_tuning"))
        {
            return tuning;
        }

        return ApplyProgramTuning(kernelTuning, tuning);
    }

    private static KernelTuningOptions ApplyProgramTuning(JsonElement kernelTuning, KernelTuningOptions tuning)
    {
        if (!TryGetObject(kernelTuning, out var programs, "programs"))
        {
            return tuning;
        }

        tuning = ApplyJsonProcessTuning(programs, tuning);
        tuning = ApplyJsonPlanningTuning(programs, tuning);
        tuning = ApplyJsonPressureTuning(programs, tuning);
        tuning = ApplyJsonUpkeepTuning(programs, tuning);
        tuning = ApplyJsonGeneratorTuning(programs, tuning);
        return ApplyJsonProgramEnabledTuning(programs, tuning);
    }

    private static KernelTuningOptions ApplyJsonProgramEnabledTuning(JsonElement programs, KernelTuningOptions tuning)
    {
        tuning = ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.Movement, "movement");
        tuning = ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.LowAltitudeVerbs, "lowAltitudeVerbs", "low_altitude_verbs");
        tuning = ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.MapActivity, "mapActivity", "map_activity");
        tuning = ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.MapSignals, "mapSignals", "map_signals");
        tuning = ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.Effects, "effects");
        tuning = ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.Upkeep, "upkeep");
        tuning = ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.Processes, "processes");
        tuning = ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.Gambits, "gambits");
        tuning = ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.Relationships, "relationships");
        tuning = ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.Pressures, "pressures");
        tuning = ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.Generators, "generators");
        return ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.Degeneracy, "degeneracy");
    }

    private static KernelTuningOptions ApplyJsonProgramEnabledValue(
        JsonElement programs,
        KernelTuningOptions tuning,
        int programId,
        params string[] programNames)
    {
        if (!TryGetObject(programs, out var program, programNames) ||
            !TryGetBool(program, out var enabled, "enabled"))
        {
            return tuning;
        }

        return WithProgramEnabled(tuning, programId, enabled);
    }

    private static KernelTuningOptions ApplyJsonPlanningTuning(JsonElement programs, KernelTuningOptions tuning)
    {
        if (!TryGetObject(programs, out var planning, "planning"))
        {
            return tuning;
        }

        if (TryGetString(planning, out var defaultPlanner, "defaultPlanner", "default_planner") &&
            TryParsePlannerKind(defaultPlanner, out var defaultPlannerCode))
        {
            tuning = WithDefaultPlanner(tuning, defaultPlannerCode);
        }

        if (TryGetInt(planning, out var opportunityTargetCapacity, "opportunityTargetCapacity", "opportunity_target_capacity"))
        {
            tuning = WithOpportunityTargetCapacity(tuning, opportunityTargetCapacity);
        }

        if (TryGetBool(planning, out var goapReservations, "goapTargetReservations", "goap_target_reservations"))
        {
            tuning = WithGoapTargetReservations(tuning, goapReservations);
        }

        tuning = ApplyJsonPlanningKindOverrides(planning, tuning);
        return ApplyJsonPlanningEntityOverrides(planning, tuning);
    }

    private static KernelTuningOptions ApplyJsonPlanningKindOverrides(JsonElement planning, KernelTuningOptions tuning)
    {
        if (!TryGetArray(planning, out var overrides, "kindPlannerOverrides", "kind_planner_overrides"))
        {
            return tuning;
        }

        var parsed = new List<PlanningKindPlannerOverride>();
        foreach (var item in overrides.EnumerateArray())
        {
            if (!TryGetInt(item, out var kindId, "kindId", "kind_id") ||
                !TryGetString(item, out var planner, "planner") ||
                !TryParsePlannerKind(planner, out var plannerKindCode))
            {
                continue;
            }

            parsed.Add(new PlanningKindPlannerOverride(kindId, plannerKindCode));
        }

        return parsed.Count == 0 ? tuning : WithKindPlannerOverrides(tuning, parsed.ToArray());
    }

    private static KernelTuningOptions ApplyJsonPlanningEntityOverrides(JsonElement planning, KernelTuningOptions tuning)
    {
        if (!TryGetArray(planning, out var overrides, "entityPlannerOverrides", "entity_planner_overrides"))
        {
            return tuning;
        }

        var parsed = new List<PlanningEntityPlannerOverride>();
        foreach (var item in overrides.EnumerateArray())
        {
            if (!TryGetInt(item, out var entityId, "entityId", "entity_id") ||
                !TryGetString(item, out var planner, "planner") ||
                !TryParsePlannerKind(planner, out var plannerKindCode))
            {
                continue;
            }

            parsed.Add(new PlanningEntityPlannerOverride(entityId, plannerKindCode));
        }

        return parsed.Count == 0 ? tuning : WithEntityPlannerOverrides(tuning, parsed.ToArray());
    }

    private static KernelTuningOptions ApplyJsonGeneratorTuning(JsonElement programs, KernelTuningOptions tuning)
    {
        if (!TryGetObject(programs, out var generators, "generators"))
        {
            return tuning;
        }

        if (!TryGetBool(generators, out var retry, "retryFireTargets", "retry_fire_targets"))
        {
            return tuning;
        }

        return WithRetryFireTargets(tuning, retry);
    }

    private static KernelTuningOptions ApplyJsonProcessTuning(JsonElement programs, KernelTuningOptions tuning)
    {
        if (!TryGetObject(programs, out var processes, "processes"))
        {
            return tuning;
        }

        if (TryGetString(processes, out var workCommitments, "workCommitments", "work_commitments") &&
            TryParseWorkCommitments(workCommitments, out var workCommitmentCode))
        {
            tuning = WithWorkCommitments(tuning, workCommitmentCode);
        }

        return tuning;
    }

    private static KernelTuningOptions ApplyJsonPressureTuning(JsonElement programs, KernelTuningOptions tuning)
    {
        if (!TryGetObject(programs, out var pressures, "pressures") ||
            !TryGetBool(pressures, out var enabled, "enabled"))
        {
            return tuning;
        }

        return WithPressureEnabled(tuning, enabled);
    }

    private static KernelTuningOptions ApplyJsonUpkeepTuning(JsonElement programs, KernelTuningOptions tuning)
    {
        if (!TryGetObject(programs, out var upkeep, "upkeep") ||
            !TryGetString(upkeep, out var agingModel, "agingModel", "aging_model") ||
            !TryParseAgingModel(agingModel, out var agingModelCode))
        {
            return tuning;
        }

        return WithAgingModel(tuning, agingModelCode);
    }

    private static KernelTuningOptions ApplyToml(string[] lines, KernelTuningOptions tuning)
    {
        var section = string.Empty;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = StripComment(lines[i]).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line[0] == '[' && line[^1] == ']')
            {
                section = Normalize(line[1..^1]);
                continue;
            }

            tuning = ApplyTomlAssignment(section, line, tuning);
        }

        return tuning;
    }

    private static KernelTuningOptions ApplyTomlAssignment(
        string section,
        string line,
        KernelTuningOptions tuning)
    {
        var equals = line.IndexOf('=', StringComparison.Ordinal);
        if (equals <= 0)
        {
            return tuning;
        }

        var key = Normalize(line[..equals]);
        var value = line[(equals + 1)..].Trim();
        return ApplyTomlValue(section, key, value, tuning);
    }

    private static KernelTuningOptions ApplyTomlValue(
        string section,
        string key,
        string value,
        KernelTuningOptions tuning)
    {
        if (string.Equals(section, "KERNELTUNING.PROGRAMS.GENERATORS", StringComparison.Ordinal))
        {
            return ApplyTomlProgramEnabledValue(section, key, value, ApplyTomlGeneratorValue(key, value, tuning));
        }

        if (string.Equals(section, "KERNELTUNING.PROGRAMS.PRESSURES", StringComparison.Ordinal))
        {
            return ApplyTomlProgramEnabledValue(section, key, value, ApplyTomlPressureValue(key, value, tuning));
        }

        var scalar = ReadBareTomlValue(value);
        if (string.Equals(section, "KERNELTUNING.PROGRAMS.PLANNING", StringComparison.Ordinal))
        {
            return ApplyTomlPlanningValue(key, scalar, tuning);
        }

        if (section.StartsWith("KERNELTUNING.PROGRAMS.PLANNING.KIND.", StringComparison.Ordinal))
        {
            return ApplyTomlPlanningKindOverride(section, key, scalar, tuning);
        }

        if (section.StartsWith("KERNELTUNING.PROGRAMS.PLANNING.ENTITY.", StringComparison.Ordinal))
        {
            return ApplyTomlPlanningEntityOverride(section, key, scalar, tuning);
        }

        if (string.Equals(section, "KERNELTUNING.PROGRAMS.PROCESSES", StringComparison.Ordinal))
        {
            return ApplyTomlProgramEnabledValue(section, key, value, ApplyTomlProcessValue(key, scalar, tuning));
        }

        if (string.Equals(section, "KERNELTUNING.PROGRAMS.UPKEEP", StringComparison.Ordinal) &&
            string.Equals(key, "AGINGMODEL", StringComparison.Ordinal))
        {
            return ApplyTomlUpkeepValue(scalar, tuning);
        }

        return ApplyTomlProgramEnabledValue(section, key, value, tuning);
    }

    private static KernelTuningOptions ApplyTomlProgramEnabledValue(
        string section,
        string key,
        string value,
        KernelTuningOptions tuning)
    {
        if (!string.Equals(key, "ENABLED", StringComparison.Ordinal) ||
            !bool.TryParse(ReadBareTomlValue(value), out var enabled) ||
            !TryProgramIdFromTomlSection(section, out var programId))
        {
            return tuning;
        }

        return WithProgramEnabled(tuning, programId, enabled);
    }

    private static bool TryProgramIdFromTomlSection(string section, out int programId)
    {
        for (var i = 0; i < ProgramSectionMappings.Length; i++)
        {
            if (string.Equals(section, ProgramSectionMappings[i].Section, StringComparison.Ordinal))
            {
                programId = ProgramSectionMappings[i].ProgramId;
                return true;
            }
        }

        programId = 0;
        return false;
    }

    private readonly record struct ProgramSectionMapping(string Section, int ProgramId);

    private static ProgramEnabledTuningOptions ProgramEnabledOverrideFor(
        ProgramEnabledTuningOptions options,
        int programId,
        int code)
    {
        return TryProgramEnabledOverrideForCore(options, programId, code, out var updated)
            ? updated
            : ProgramEnabledOverrideForWorld(updated, programId, code);
    }

    private static bool TryProgramEnabledOverrideForCore(
        ProgramEnabledTuningOptions options,
        int programId,
        int code,
        out ProgramEnabledTuningOptions updated)
    {
        updated = programId switch
        {
            KernelProgramCodes.Movement => options with { Movement = code },
            KernelProgramCodes.LowAltitudeVerbs => options with { LowAltitudeVerbs = code },
            KernelProgramCodes.MapActivity => options with { MapActivity = code },
            KernelProgramCodes.MapSignals => options with { MapSignals = code },
            KernelProgramCodes.Effects => options with { Effects = code },
            KernelProgramCodes.Upkeep => options with { Upkeep = code },
            KernelProgramCodes.Processes => options with { Processes = code },
            KernelProgramCodes.Gambits => options with { Gambits = code },
            _ => options,
        };
        return !ReferenceEquals(options, updated);
    }

    private static ProgramEnabledTuningOptions ProgramEnabledOverrideForWorld(
        ProgramEnabledTuningOptions options,
        int programId,
        int code)
    {
        return programId switch
        {
            KernelProgramCodes.Relationships => options with { Relationships = code },
            KernelProgramCodes.Pressures => options with { Pressures = code },
            KernelProgramCodes.Generators => options with { Generators = code },
            KernelProgramCodes.Degeneracy => options with { Degeneracy = code },
            _ => options,
        };
    }

    private static KernelTuningOptions ApplyTomlPlanningValue(
        string key,
        string value,
        KernelTuningOptions tuning)
    {
        if (string.Equals(key, "DEFAULTPLANNER", StringComparison.Ordinal) &&
            TryParsePlannerKind(value, out var plannerKindCode))
        {
            return WithDefaultPlanner(tuning, plannerKindCode);
        }

        if (string.Equals(key, "OPPORTUNITYTARGETCAPACITY", StringComparison.Ordinal) &&
            int.TryParse(value, global::System.Globalization.NumberStyles.Integer, global::System.Globalization.CultureInfo.InvariantCulture, out var opportunityTargetCapacity))
        {
            return WithOpportunityTargetCapacity(tuning, opportunityTargetCapacity);
        }

        if (string.Equals(key, "GOAPTARGETRESERVATIONS", StringComparison.Ordinal) &&
            bool.TryParse(value, out var goapReservations))
        {
            return WithGoapTargetReservations(tuning, goapReservations);
        }

        return tuning;
    }

    private static KernelTuningOptions ApplyTomlPlanningKindOverride(
        string section,
        string key,
        string value,
        KernelTuningOptions tuning)
    {
        if (!string.Equals(key, "PLANNER", StringComparison.Ordinal) ||
            !TryParseTomlOverrideId(section, "KERNELTUNING.PROGRAMS.PLANNING.KIND.", out var kindId) ||
            !TryParsePlannerKind(value, out var plannerKindCode))
        {
            return tuning;
        }

        var existing = tuning.Programs.Planning.KindPlannerOverrides;
        var updated = new PlanningKindPlannerOverride[existing.Length + 1];
        Array.Copy(existing, updated, existing.Length);
        updated[^1] = new PlanningKindPlannerOverride(kindId, plannerKindCode);
        return WithKindPlannerOverrides(tuning, updated);
    }

    private static KernelTuningOptions ApplyTomlPlanningEntityOverride(
        string section,
        string key,
        string value,
        KernelTuningOptions tuning)
    {
        if (!string.Equals(key, "PLANNER", StringComparison.Ordinal) ||
            !TryParseTomlOverrideId(section, "KERNELTUNING.PROGRAMS.PLANNING.ENTITY.", out var entityId) ||
            !TryParsePlannerKind(value, out var plannerKindCode))
        {
            return tuning;
        }

        var existing = tuning.Programs.Planning.EntityPlannerOverrides;
        var updated = new PlanningEntityPlannerOverride[existing.Length + 1];
        Array.Copy(existing, updated, existing.Length);
        updated[^1] = new PlanningEntityPlannerOverride(entityId, plannerKindCode);
        return WithEntityPlannerOverrides(tuning, updated);
    }

    private static KernelTuningOptions ApplyTomlGeneratorValue(
        string key,
        string value,
        KernelTuningOptions tuning)
    {
        return string.Equals(key, "RETRYFIRETARGETS", StringComparison.Ordinal) &&
               bool.TryParse(value, out var retry)
            ? WithRetryFireTargets(tuning, retry)
            : tuning;
    }

    private static KernelTuningOptions ApplyTomlPressureValue(
        string key,
        string value,
        KernelTuningOptions tuning)
    {
        return string.Equals(key, "ENABLED", StringComparison.Ordinal) &&
               bool.TryParse(value, out var pressureEnabled)
            ? WithPressureEnabled(tuning, pressureEnabled)
            : tuning;
    }

    private static KernelTuningOptions ApplyTomlUpkeepValue(
        string value,
        KernelTuningOptions tuning)
    {
        return TryParseAgingModel(value, out var agingModelCode)
            ? WithAgingModel(tuning, agingModelCode)
            : tuning;
    }

    private static KernelTuningOptions ApplyTomlProcessValue(
        string key,
        string value,
        KernelTuningOptions tuning)
    {
        if (string.Equals(key, "WORKCOMMITMENTS", StringComparison.Ordinal) &&
            TryParseWorkCommitments(value, out var workCommitmentCode))
        {
            return WithWorkCommitments(tuning, workCommitmentCode);
        }

        return tuning;
    }

    private static KernelTuningOptions WithRetryFireTargets(KernelTuningOptions tuning, bool retry)
    {
        return tuning with
        {
            Programs = tuning.Programs with
            {
                Generators = tuning.Programs.Generators with
                {
                    RetryFireTargets = retry,
                },
            },
        };
    }

    private static KernelTuningOptions WithProgramEnabled(KernelTuningOptions tuning, int programId, bool enabled)
    {
        var code = enabled ? ProgramEnabledOverrideCodes.Enabled : ProgramEnabledOverrideCodes.Disabled;
        return tuning with
        {
            Programs = tuning.Programs with
            {
                Enabled = ProgramEnabledOverrideFor(tuning.Programs.Enabled, programId, code),
            },
        };
    }

    private static KernelTuningOptions WithWorkCommitments(KernelTuningOptions tuning, int code)
    {
        return tuning with
        {
            Programs = tuning.Programs with
            {
                Processes = tuning.Programs.Processes with
                {
                    WorkCommitmentCode = code,
                },
            },
        };
    }

    private static KernelTuningOptions WithDefaultPlanner(KernelTuningOptions tuning, int code)
    {
        return tuning with
        {
            Programs = tuning.Programs with
            {
                Planning = tuning.Programs.Planning with
                {
                    DefaultPlannerKindCode = code,
                },
            },
        };
    }

    private static KernelTuningOptions WithOpportunityTargetCapacity(KernelTuningOptions tuning, int capacity)
    {
        return tuning with
        {
            Programs = tuning.Programs with
            {
                Planning = tuning.Programs.Planning with
                {
                    OpportunityTargetCapacity = capacity,
                },
            },
        };
    }

    private static KernelTuningOptions WithGoapTargetReservations(KernelTuningOptions tuning, bool enabled)
    {
        return tuning with
        {
            Programs = tuning.Programs with
            {
                Planning = tuning.Programs.Planning with
                {
                    GoapTargetReservationsEnabled = enabled,
                },
            },
        };
    }

    private static KernelTuningOptions WithKindPlannerOverrides(KernelTuningOptions tuning, PlanningKindPlannerOverride[] overrides)
    {
        return tuning with
        {
            Programs = tuning.Programs with
            {
                Planning = tuning.Programs.Planning with
                {
                    KindPlannerOverrides = overrides,
                },
            },
        };
    }

    private static KernelTuningOptions WithEntityPlannerOverrides(KernelTuningOptions tuning, PlanningEntityPlannerOverride[] overrides)
    {
        return tuning with
        {
            Programs = tuning.Programs with
            {
                Planning = tuning.Programs.Planning with
                {
                    EntityPlannerOverrides = overrides,
                },
            },
        };
    }

    private static KernelTuningOptions WithPressureEnabled(KernelTuningOptions tuning, bool enabled)
    {
        return tuning with
        {
            Programs = tuning.Programs with
            {
                Pressures = tuning.Programs.Pressures with
                {
                    Enabled = enabled,
                },
            },
        };
    }

    private static KernelTuningOptions WithAgingModel(KernelTuningOptions tuning, int code)
    {
        return tuning with
        {
            Programs = tuning.Programs with
            {
                Upkeep = tuning.Programs.Upkeep with
                {
                    AgingModelCode = code,
                },
            },
        };
    }

    private static bool TryGetObject(JsonElement element, out JsonElement value, params string[] propertyNames)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (propertyNames.Length == 0)
        {
            value = element;
            return true;
        }

        for (var i = 0; i < propertyNames.Length; i++)
        {
            if (element.TryGetProperty(propertyNames[i], out value) &&
                value.ValueKind == JsonValueKind.Object)
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetBool(JsonElement element, out bool value, params string[] propertyNames)
    {
        value = false;
        for (var i = 0; i < propertyNames.Length; i++)
        {
            if (!element.TryGetProperty(propertyNames[i], out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False)
            {
                value = property.GetBoolean();
                return true;
            }
        }

        return false;
    }

    private static bool TryGetInt(JsonElement element, out int value, params string[] propertyNames)
    {
        value = 0;
        for (var i = 0; i < propertyNames.Length; i++)
        {
            if (!element.TryGetProperty(propertyNames[i], out var property) ||
                property.ValueKind != JsonValueKind.Number ||
                !property.TryGetInt32(out value))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool TryGetArray(JsonElement element, out JsonElement value, params string[] propertyNames)
    {
        value = default;
        for (var i = 0; i < propertyNames.Length; i++)
        {
            if (!element.TryGetProperty(propertyNames[i], out value) ||
                value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            return true;
        }

        value = default;
        return false;
    }

    private static bool TryGetString(JsonElement element, out string value, params string[] propertyNames)
    {
        value = string.Empty;
        for (var i = 0; i < propertyNames.Length; i++)
        {
            if (!element.TryGetProperty(propertyNames[i], out var property) ||
                property.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            value = property.GetString() ?? string.Empty;
            return true;
        }

        return false;
    }

    private static bool TryParseWorkCommitments(string value, out int code)
    {
        var normalized = Normalize(value);
        if (string.Equals(normalized, "OFF", StringComparison.Ordinal))
        {
            code = ProcessWorkCommitmentCodes.Off;
            return true;
        }

        if (string.Equals(normalized, "PILOT", StringComparison.Ordinal))
        {
            code = ProcessWorkCommitmentCodes.Pilot;
            return true;
        }

        code = 0;
        return false;
    }

    private static bool TryParseAgingModel(string value, out int code)
    {
        var normalized = Normalize(value);
        if (string.Equals(normalized, "LINEAR", StringComparison.Ordinal))
        {
            code = UpkeepAgingModelCodes.Linear;
            return true;
        }

        if (string.Equals(normalized, "LATELIFE", StringComparison.Ordinal))
        {
            code = UpkeepAgingModelCodes.LateLife;
            return true;
        }

        code = 0;
        return false;
    }

    private static bool TryParsePlannerKind(string value, out int code)
    {
        var normalized = Normalize(value);
        if (string.Equals(normalized, "GOAP", StringComparison.Ordinal))
        {
            code = PlannerKindCodes.Goap;
            return true;
        }

        if (string.Equals(normalized, "HTN", StringComparison.Ordinal))
        {
            code = PlannerKindCodes.Htn;
            return true;
        }

        if (string.Equals(normalized, "OPPORTUNITY", StringComparison.Ordinal))
        {
            code = PlannerKindCodes.Opportunity;
            return true;
        }

        code = 0;
        return false;
    }

    private static bool TryParseTomlOverrideId(string section, string prefix, out int id)
    {
        id = 0;
        return section.Length > prefix.Length &&
               int.TryParse(section[prefix.Length..], global::System.Globalization.NumberStyles.Integer, global::System.Globalization.CultureInfo.InvariantCulture, out id) &&
               id > 0;
    }

    private static string ReadBareTomlValue(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length >= 2 &&
               ((trimmed[0] == '"' && trimmed[^1] == '"') ||
                (trimmed[0] == '\'' && trimmed[^1] == '\''))
            ? trimmed[1..^1]
            : trimmed;
    }

    private static string StripComment(string line)
    {
        var comment = line.IndexOf('#', StringComparison.Ordinal);
        return comment < 0 ? line : line[..comment];
    }

    private static string Normalize(string value) =>
        value
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();

    private static bool IsJson(string extension) =>
        string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase);

    private static bool IsToml(string extension) =>
        string.Equals(extension, ".toml", StringComparison.OrdinalIgnoreCase);

    private static bool IsYaml(string extension) =>
        string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase);
}
