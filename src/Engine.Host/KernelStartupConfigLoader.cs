namespace Engine.Host;

using System.Globalization;
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
        new("KERNELTUNING.PROGRAMS.GAMBITS", KernelProgramCodes.Gambits),
        new("KERNELTUNING.PROGRAMS.RELATIONSHIPS", KernelProgramCodes.Relationships),
        new("KERNELTUNING.PROGRAMS.GENERATORS", KernelProgramCodes.Generators),
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
        tuning = ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.Gambits, "gambits");
        tuning = ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.Relationships, "relationships");
        return ApplyJsonProgramEnabledValue(programs, tuning, KernelProgramCodes.Generators, "generators");
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

    private static KernelTuningOptions ApplyJsonGeneratorTuning(JsonElement programs, KernelTuningOptions tuning)
    {
        if (!TryGetObject(programs, out var generators, "generators"))
        {
            return tuning;
        }

        if (TryGetBool(generators, out var retry, "retryFireTargets", "retry_fire_targets"))
        {
            tuning = WithRetryFireTargets(tuning, retry);
        }

        if (TryGetInt(generators, out var pulseWidthTicks, "pulseWidthTicks", "pulse_width_ticks"))
        {
            tuning = WithGeneratorPulseWidth(tuning, pulseWidthTicks);
        }

        if (TryGetInt(generators, out var firesPerPulse, "firesPerPulse", "fires_per_pulse"))
        {
            tuning = WithGeneratorFiresPerPulse(tuning, firesPerPulse);
        }

        return tuning;
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

        var scalar = ReadBareTomlValue(value);
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
            KernelProgramCodes.Generators => options with { Generators = code },
            _ => options,
        };
    }

    private static KernelTuningOptions ApplyTomlGeneratorValue(
        string key,
        string value,
        KernelTuningOptions tuning)
    {
        var scalar = ReadBareTomlValue(value);
        if (string.Equals(key, "RETRYFIRETARGETS", StringComparison.Ordinal) &&
            bool.TryParse(scalar, out var retry))
        {
            return WithRetryFireTargets(tuning, retry);
        }

        if (string.Equals(key, "PULSEWIDTHTICKS", StringComparison.Ordinal) &&
            int.TryParse(scalar, CultureInfo.InvariantCulture, out var pulseWidthTicks))
        {
            return WithGeneratorPulseWidth(tuning, pulseWidthTicks);
        }

        if (string.Equals(key, "FIRESPERPULSE", StringComparison.Ordinal) &&
            int.TryParse(scalar, CultureInfo.InvariantCulture, out var firesPerPulse))
        {
            return WithGeneratorFiresPerPulse(tuning, firesPerPulse);
        }

        return tuning;
    }

    private static KernelTuningOptions ApplyTomlUpkeepValue(
        string value,
        KernelTuningOptions tuning)
    {
        return TryParseAgingModel(value, out var agingModelCode)
            ? WithAgingModel(tuning, agingModelCode)
            : tuning;
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

    private static KernelTuningOptions WithGeneratorPulseWidth(KernelTuningOptions tuning, int pulseWidthTicks)
    {
        return tuning with
        {
            Programs = tuning.Programs with
            {
                Generators = tuning.Programs.Generators with
                {
                    PulseWidthTicks = pulseWidthTicks,
                },
            },
        };
    }

    private static KernelTuningOptions WithGeneratorFiresPerPulse(KernelTuningOptions tuning, int firesPerPulse)
    {
        return tuning with
        {
            Programs = tuning.Programs with
            {
                Generators = tuning.Programs.Generators with
                {
                    FiresPerPulse = firesPerPulse,
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
