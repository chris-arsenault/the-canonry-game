using Engine.Constants;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public sealed partial class PhysicalMicrogeographyPainter
{
    private static Color ElevationColor(float elevation, float minimum, float maximum)
    {
        if (elevation < 0f)
        {
            var depth = MathF.Pow(
                Math.Clamp(-elevation / Math.Max(1f, -minimum), 0f, 1f),
                0.60f);
            return new Color(0.24f, 0.43f, 0.44f).Lerp(
                new Color(0.035f, 0.10f, 0.22f),
                depth);
        }

        var normalized = MathF.Pow(
            Math.Clamp(elevation / Math.Max(1f, maximum), 0f, 1f),
            0.55f);
        if (normalized < 0.58f)
        {
            return new Color(0.31f, 0.48f, 0.38f).Lerp(
                new Color(0.62f, 0.57f, 0.45f),
                normalized / 0.58f);
        }

        return new Color(0.62f, 0.57f, 0.45f).Lerp(
            new Color(0.95f, 0.94f, 0.90f),
            (normalized - 0.58f) / 0.42f);
    }

    private static Color TemperatureColor(int temperature) =>
        new Color(0.18f, 0.38f, 0.68f).Lerp(
            new Color(0.88f, 0.38f, 0.18f),
            Math.Clamp(temperature / 1000f, 0f, 1f));

    private static Color MoistureColor(int moisture) =>
        new Color(0.54f, 0.42f, 0.25f).Lerp(
            new Color(0.18f, 0.62f, 0.68f),
            Math.Clamp(moisture / 1000f, 0f, 1f));

    private static Color GeothermalColor(int geothermal) =>
        new Color(0.17f, 0.18f, 0.23f).Lerp(
            new Color(0.92f, 0.30f, 0.12f),
            Math.Clamp(geothermal / 1000f, 0f, 1f));

    private static Color OceanCurrentColor(int strength, int heat)
    {
        var thermal = new Color(0.08f, 0.20f, 0.38f).Lerp(
            new Color(0.20f, 0.74f, 0.66f),
            Math.Clamp(heat / 1000f, 0f, 1f));
        return new Color(0.06f, 0.09f, 0.14f).Lerp(
            thermal,
            Math.Clamp(strength / 1000f, 0f, 1f));
    }

    private static Color WaveExposureColor(int exposure) =>
        new Color(0.05f, 0.13f, 0.22f).Lerp(
            new Color(0.68f, 0.90f, 0.96f),
            Math.Clamp(exposure / 1000f, 0f, 1f));

    private static Color PrecipitationColor(int rainfall, int snowfall)
    {
        var total = Math.Clamp((rainfall + snowfall) / 1000f, 0f, 1f);
        var snowShare = snowfall / (float)Math.Max(1, rainfall + snowfall);
        var wet = new Color(0.15f, 0.42f, 0.72f).Lerp(
            new Color(0.88f, 0.95f, 0.98f),
            snowShare);
        return new Color(0.18f, 0.14f, 0.12f).Lerp(wet, total);
    }

    private static Color SeaIceSuitabilityColor(int suitability) =>
        new Color(0.08f, 0.16f, 0.28f).Lerp(
            new Color(0.88f, 0.97f, 1f),
            Math.Clamp(suitability / 1000f, 0f, 1f));

    private static Color SedimentColor(int thickness, int deposition)
    {
        var amount = Math.Clamp((thickness + (deposition / 2f)) / 180f, 0f, 1f);
        return new Color(0.13f, 0.14f, 0.16f).Lerp(
            new Color(0.73f, 0.58f, 0.35f),
            amount);
    }

    private static Color ErosionHistoryColor(int fluvial, int glacial, int coastal)
    {
        var maximum = Math.Max(1f, Math.Max(fluvial, Math.Max(glacial, coastal)));
        var intensity = Math.Clamp((fluvial + glacial + coastal) / 240f, 0f, 1f);
        var process = new Color(
            0.18f + (0.22f * coastal / maximum),
            0.16f + (0.55f * fluvial / maximum),
            0.20f + (0.66f * glacial / maximum));
        return new Color(0.08f, 0.09f, 0.11f).Lerp(process, intensity);
    }

    private static Color MaximumGlaciationColor(int thickness) =>
        new Color(0.16f, 0.12f, 0.18f).Lerp(
            new Color(0.78f, 0.94f, 1f),
            Math.Clamp(thickness / 1000f, 0f, 1f));

    private static Color IceColor(int iceThickness) =>
        new Color(0.13f, 0.18f, 0.24f).Lerp(
            new Color(0.82f, 0.94f, 0.98f),
            Math.Clamp(iceThickness / 1000f, 0f, 1f));

    private static Color FractureColor(int conductance) =>
        new Color(0.10f, 0.10f, 0.14f).Lerp(
            new Color(0.96f, 0.30f, 0.12f),
            Math.Clamp(conductance / 1000f, 0f, 1f));

    private static Color TectonicProvinceColor(int provinceId) => provinceId <= 0
        ? UiTokens.Ink
        : Color.FromHsv((provinceId * 0.618034f) % 1f, 0.58f, 0.84f);

    private static Color CrustTypeColor(int crustType) => crustType switch
    {
        1 => new Color(0.12f, 0.30f, 0.48f),
        2 => new Color(0.66f, 0.51f, 0.30f),
        _ => UiTokens.Ink,
    };

    private static Color TectonicBoundaryColor(int boundaryType) => boundaryType switch
    {
        1 => new Color(0.95f, 0.30f, 0.16f),
        2 => new Color(0.20f, 0.65f, 0.92f),
        3 => new Color(0.94f, 0.70f, 0.22f),
        _ => UiTokens.Ink,
    };

    private static Color TectonicStrainColor(int strain) =>
        new Color(0.08f, 0.10f, 0.16f).Lerp(
            new Color(0.94f, 0.36f, 0.18f),
            Math.Clamp(strain / 1000f, 0f, 1f));

    private static Color FireColor(int intensity) =>
        new Color(0.10f, 0.08f, 0.10f).Lerp(
            new Color(1.00f, 0.28f, 0.05f),
            Math.Clamp(intensity / 1000f, 0f, 1f));

    private static Color AuroraColor(int intensity) =>
        new Color(0.08f, 0.10f, 0.17f).Lerp(
            new Color(0.26f, 0.96f, 0.73f),
            Math.Clamp(intensity / 1000f, 0f, 1f));

    private static Color PressureColor(int intensity) =>
        new Color(0.07f, 0.09f, 0.15f).Lerp(
            new Color(0.70f, 0.20f, 0.78f),
            Math.Clamp(intensity / 1000f, 0f, 1f));

    private static Color MagicCompositeColor(int fire, int aurora, int pressure)
    {
        var fireStrength = Math.Clamp(fire / 1000f, 0f, 1f);
        var auroraStrength = Math.Clamp(aurora / 1000f, 0f, 1f);
        var pressureStrength = Math.Clamp(pressure / 1000f, 0f, 1f);
        return new Color(
            Math.Clamp(0.07f + (0.82f * fireStrength) + (0.38f * pressureStrength), 0f, 1f),
            Math.Clamp(0.08f + (0.76f * auroraStrength) + (0.12f * fireStrength), 0f, 1f),
            Math.Clamp(0.13f + (0.56f * auroraStrength) + (0.68f * pressureStrength), 0f, 1f));
    }

    private static Color MagicHistoryColor(
        int fire,
        int aurora,
        int pressure,
        int coastalPressure,
        int firstEpoch,
        int epochCount)
    {
        if (firstEpoch < 0)
        {
            return new Color(0.07f, 0.08f, 0.11f);
        }

        var history = MagicCompositeColor(fire, aurora, pressure);
        history = history.Lerp(
            new Color(0.70f, 0.48f, 0.84f),
            Math.Clamp(coastalPressure / 1000f, 0f, 1f) * 0.42f);
        var ancient = 1f - Math.Clamp(firstEpoch / (float)Math.Max(1, epochCount - 1), 0f, 1f);
        return history.Lerp(new Color(0.94f, 0.78f, 0.42f), ancient * 0.24f);
    }

    private static Color LandformColor(int landformCode)
    {
        if (landformCode >= PhysicalLandformCodes.OutwashPlain)
        {
            return HistoricalLandformColor(landformCode);
        }

        return landformCode >= PhysicalLandformCodes.UValley
            ? GlacialLandformColor(landformCode)
            : StandardLandformColor(landformCode);
    }

    private static Color StandardLandformColor(int landformCode) => landformCode switch
    {
        PhysicalLandformCodes.Plain => new Color(0.42f, 0.52f, 0.42f),
        PhysicalLandformCodes.Slope => new Color(0.52f, 0.49f, 0.40f),
        PhysicalLandformCodes.Valley => new Color(0.28f, 0.48f, 0.42f),
        PhysicalLandformCodes.Basin => new Color(0.30f, 0.36f, 0.44f),
        PhysicalLandformCodes.Ridge => new Color(0.67f, 0.62f, 0.54f),
        PhysicalLandformCodes.Peak => new Color(0.90f, 0.88f, 0.82f),
        PhysicalLandformCodes.Pass => new Color(0.82f, 0.57f, 0.28f),
        PhysicalLandformCodes.Seabed => new Color(0.12f, 0.23f, 0.34f),
        _ => UiTokens.Whisper,
    };

    private static Color HistoricalLandformColor(int landformCode) => landformCode switch
    {
        PhysicalLandformCodes.OutwashPlain => new Color(0.48f, 0.55f, 0.47f),
        PhysicalLandformCodes.ImpactBasin => new Color(0.34f, 0.28f, 0.30f),
        PhysicalLandformCodes.ImpactRim => new Color(0.70f, 0.42f, 0.31f),
        _ => UiTokens.Whisper,
    };

    private static Color GlacialLandformColor(int landformCode) => landformCode switch
    {
        PhysicalLandformCodes.UValley => new Color(0.32f, 0.52f, 0.50f),
        PhysicalLandformCodes.Cirque => new Color(0.58f, 0.72f, 0.76f),
        PhysicalLandformCodes.Fjord => new Color(0.10f, 0.31f, 0.43f),
        PhysicalLandformCodes.Moraine => new Color(0.44f, 0.39f, 0.33f),
        _ => UiTokens.Whisper,
    };
}
