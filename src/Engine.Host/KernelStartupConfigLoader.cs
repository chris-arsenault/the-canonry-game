namespace Engine.Host;

using System.Text.Json;

using Engine.Kernel;

internal static class KernelStartupConfigLoader
{
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
        return ApplyJsonGeneratorTuning(programs, tuning);
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

        if (TryGetString(processes, out var resourceSupplyModel, "resourceSupplyModel", "resource_supply_model") &&
            TryParseResourceSupplyModel(resourceSupplyModel, out var resourceSupplyModelCode))
        {
            tuning = WithResourceSupplyModel(tuning, resourceSupplyModelCode);
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
            return ApplyTomlGeneratorValue(key, value, tuning);
        }

        if (string.Equals(section, "KERNELTUNING.PROGRAMS.PRESSURES", StringComparison.Ordinal))
        {
            return ApplyTomlPressureValue(key, value, tuning);
        }

        var scalar = ReadScalarValue(value);
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
            return ApplyTomlProcessValue(key, scalar, tuning);
        }

        if (string.Equals(section, "KERNELTUNING.PROGRAMS.UPKEEP", StringComparison.Ordinal) &&
            string.Equals(key, "AGINGMODEL", StringComparison.Ordinal))
        {
            return ApplyTomlUpkeepValue(scalar, tuning);
        }

        return tuning;
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
        if (string.Equals(key, "RESOURCESUPPLYMODEL", StringComparison.Ordinal) &&
            TryParseResourceSupplyModel(value, out var resourceSupplyModelCode))
        {
            return WithResourceSupplyModel(tuning, resourceSupplyModelCode);
        }

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

    private static KernelTuningOptions WithResourceSupplyModel(KernelTuningOptions tuning, int code)
    {
        return tuning with
        {
            Programs = tuning.Programs with
            {
                Processes = tuning.Programs.Processes with
                {
                    ResourceSupplyModelCode = code,
                },
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

    private static bool TryParseResourceSupplyModel(string value, out int code)
    {
        var normalized = Normalize(value);
        if (string.Equals(normalized, "AMBIENTHEX", StringComparison.Ordinal))
        {
            code = ProcessResourceSupplyModelCodes.AmbientHex;
            return true;
        }

        if (string.Equals(normalized, "NAMEDPROVIDERNOHEX", StringComparison.Ordinal))
        {
            code = ProcessResourceSupplyModelCodes.NamedProviderNoHex;
            return true;
        }

        code = 0;
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

    private static string ReadScalarValue(string value)
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
