namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class TectonicUpliftLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "tectonic-uplift",
        0x4f2b17,
        PhysicalGeographyFacts.TectonicProvinces |
        PhysicalGeographyFacts.TectonicMargins |
        PhysicalGeographyFacts.ContinentalDomains,
        PhysicalGeographyFacts.Elevation,
        PhysicalGeographyFacts.VolcanicConstruction |
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Bathymetry |
        PhysicalGeographyFacts.Landmasses |
        PhysicalGeographyFacts.Fractures |
        PhysicalGeographyFacts.Geothermal |
        PhysicalGeographyFacts.OceanCurrents |
        PhysicalGeographyFacts.Warmth |
        PhysicalGeographyFacts.Temperature |
        PhysicalGeographyFacts.AtmosphericFlow |
        PhysicalGeographyFacts.Moisture |
        PhysicalGeographyFacts.Precipitation |
        PhysicalGeographyFacts.Runoff |
        PhysicalGeographyFacts.DrainageConditioning |
        PhysicalGeographyFacts.Lakes |
        PhysicalGeographyFacts.FlowRouting |
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.LandIce |
        PhysicalGeographyFacts.IceFlow |
        PhysicalGeographyFacts.SeaIce |
        PhysicalGeographyFacts.GlacialMeltwater |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.RiverGeometry |
        PhysicalGeographyFacts.Floodplains |
        PhysicalGeographyFacts.Deltas |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var x = map.Grid.TriangleX(triangleIndex);
            var y = map.Grid.TriangleY(triangleIndex);
            var potential = map.ContinentalPotential[triangleIndex] / 1000d;
            var strain = map.TectonicStrain[triangleIndex] / 1000d;
            var boundaryRelief = BoundaryRelief(map, triangleIndex, strain, context);
            var assemblySeparation = map.CrustAssemblySeparation[triangleIndex] / 1000d;
            var variation = PhysicalNoise.FractalNoise(
                x,
                y,
                context.Seed ^ 0x52c9,
                context.Profile.ElevationVariationScale,
                3) * context.Profile.ElevationVariationStrength;
            InteriorRelief(map, context, triangleIndex, out var interiorRelief, out var belt);

            // Height is what the crust floats to, not a value painted from the
            // continental mask. The mask saturates by design - it has to, or
            // coastlines go mushy - and a saturated term contributes no
            // gradient across the interior it just defined.
            var thicknessUnits = CrustThicknessUnits(
                map,
                context,
                triangleIndex,
                potential,
                boundaryRelief + variation + interiorRelief - assemblySeparation);
            map.CrustThickness[triangleIndex] = PhysicalElevation.FromUnits(thicknessUnits);
            var baseElevation =
                PhysicalCrust.HeightUnits(thicknessUnits) / context.Profile.ElevationScale;
            var elevation = baseElevation +
                MarginProfile(map, context.Profile, triangleIndex, baseElevation);
            if (context.IsEvolutionEpoch)
            {
                ApplyIncrementalUplift(map, context, triangleIndex, boundaryRelief, belt);
            }
            else
            {
                map.Elevation[triangleIndex] = PhysicalElevation.FromScaled(
                    elevation,
                    context.Profile.ElevationScale);
            }

            map.Uplift[triangleIndex] = (float)Math.Clamp(
                Math.Max(0d, boundaryRelief) + (belt * 0.55d) + (potential * 0.48d) +
                    InheritedRelief(map, triangleIndex, context.Profile) * 0.20d,
                0d,
                1.5d);
            map.RidgeInfluence[triangleIndex] = (float)Math.Clamp(
                Math.Max(
                    Math.Max(
                        Math.Max(0d, boundaryRelief) / Math.Max(0.001d, context.Profile.RidgeUplift),
                        belt / Math.Max(0.001d, context.Profile.InteriorBeltStrength) * 0.8d),
                    InheritedRelief(map, triangleIndex, context.Profile) * 0.52d),
                0d,
                1d);
        }
    }

    /// <summary>
    /// Crustal thickness, from which height follows by isostasy.
    ///
    /// The continental mask decides continental against oceanic, which is the
    /// one job a saturating field does well. Everything that varies inland has
    /// to come from somewhere that does not clamp, so province identity
    /// carries terrane thickness and the relief terms are converted into
    /// thickness rather than added to height.
    /// </summary>
    private static double CrustThicknessUnits(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        int triangleIndex,
        double potential,
        double reliefContribution)
    {
        var mask = PhysicalCrust.OceanicThicknessUnits +
            ((PhysicalCrust.ContinentalThicknessUnits - PhysicalCrust.OceanicThicknessUnits) *
                potential);
        var buoyancy = PhysicalCrust.BuoyancyPerMille / 1000d;
        var reliefUnits = reliefContribution * context.Profile.ElevationScale / buoyancy;
        return PhysicalCrust.ClampThicknessUnits(
            mask + (ProvinceThicknessOffsetUnits(map, context, triangleIndex) * potential) +
                reliefUnits);
    }

    /// <summary>
    /// Per-terrane thickness. Scaled by continental potential so it fades out
    /// over ocean, where crust is oceanic regardless of which province it
    /// belongs to.
    /// </summary>
    private static double ProvinceThicknessOffsetUnits(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        int triangleIndex)
    {
        var province = map.CrustProvinceIds[triangleIndex];
        if (province < 0)
        {
            return 0d;
        }

        var hash = PhysicalNoise.HashUnit(context.Seed ^ 0x3d7b, province, 0x51a9);
        return (hash - 0.5d) * 2d * PhysicalCrust.ProvinceThicknessRangeUnits;
    }

    /// <summary>
    /// Obduction at convergent margins.
    ///
    /// Ordinary boundary uplift is far too gentle to raise ocean floor to land
    /// within the epoch schedule, so seafloor that drifts into a collision
    /// would simply stay seafloor and the history it carries would never
    /// surface. Accretion lifts arriving low crust hard, which is what strands
    /// former seabed — and the ocean-born pressure record written into it —
    /// on top of a new range.
    /// </summary>
    private static long AccretionUplift(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        int triangleIndex)
    {
        if (map.MarginCodes[triangleIndex] != PhysicalMarginCodes.Convergent)
        {
            return 0;
        }

        var depth = map.EffectiveSeaLevel - map.BedrockElevation[triangleIndex];
        if (depth <= 0)
        {
            return 0;
        }

        // Deeper crust is driven up harder, so a whole slab surfaces together
        // rather than only its shallowest edge.
        var submergence = Math.Clamp(
            PhysicalElevation.ToUnits(depth) / Math.Max(1d, context.Profile.AccretionDepthScale),
            0d,
            1d);
        var strain = map.TectonicStrain[triangleIndex] / 1000d;
        return PhysicalElevation.FromUnits(
            submergence * strain * context.Profile.AccretionUpliftStep *
                context.TectonicStrength / 1000d);
    }

    /// <summary>
    /// Shapes the land-to-ocean transition by margin type, which is what makes
    /// coasts differ in kind rather than only in raggedness.
    ///
    /// Oceanic crust subsides as it ages away from its spreading centre, so
    /// young ridge flanks stay shallow and old crust reaches abyssal depth.
    /// Near sea level, passive margins flatten into a broad platform: the same
    /// horizontal noise then moves the shoreline much further, which is how
    /// real platform peninsulas and offshore archipelagos arise. Convergent
    /// and transform margins keep a steep, narrow transition instead.
    /// </summary>
    private static double MarginProfile(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int triangleIndex,
        double baseElevation)
    {
        var adjustment = 0d;
        if (map.CrustTypeCodes[triangleIndex] == PhysicalCrustTypeCodes.Oceanic)
        {
            adjustment -= map.CrustAge[triangleIndex] / 1000d *
                (profile.OceanicSubsidenceDepth / 1000d);
        }

        var flattening = map.MarginCodes[triangleIndex] switch
        {
            PhysicalMarginCodes.Passive => profile.PassiveMarginFlattening / 1000d,
            PhysicalMarginCodes.Rifted => profile.RiftedMarginFlattening / 1000d,
            _ => 0d,
        };

        if (flattening <= 0d)
        {
            return adjustment;
        }

        // Pull elevation toward the shoreline datum inside a band around it.
        // The closer the crust already is to sea level, the harder it is
        // pulled, which widens the near-shore platform without moving
        // continental interiors or deep basins.
        var band = Math.Max(0.001d, profile.MarginPlatformBand / 1000d);
        var proximity = Math.Clamp(1d - (Math.Abs(baseElevation) / band), 0d, 1d);
        return adjustment - (baseElevation * proximity * proximity * flattening);
    }

    /// <summary>
    /// Persistent interior height structure on continental crust: broad
    /// highlands/basins plus elongated mountain belts along the zero contours
    /// of an epoch-stable belt field. Keeps a strong differential everywhere so
    /// downstream processes never operate on a flat plateau.
    /// </summary>
    private static void InteriorRelief(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        int triangleIndex,
        out double relief,
        out double belt)
    {
        var profile = context.Profile;
        var continentality = Math.Clamp(
            ((map.ContinentalPotential[triangleIndex] / 1000d) - 0.45d) / 0.25d,
            0d,
            1d);
        if (continentality <= 0d)
        {
            relief = 0d;
            belt = 0d;
            return;
        }

        var x = map.Grid.TriangleX(triangleIndex);
        var y = map.Grid.TriangleY(triangleIndex);
        var macro = PhysicalNoise.FractalNoise(
            x,
            y,
            context.EpochStableSeed ^ 0x5ee1,
            profile.InteriorReliefScale,
            4) * profile.InteriorReliefStrength;
        if (macro < 0d)
        {
            // Basins stay but shallower than highlands are tall: the land
            // skews vertical without drowning half the interior in lakes.
            macro *= profile.InteriorBasinDepthShare / 1000d;
        }
        var beltField = PhysicalNoise.FractalNoise(
            x,
            y,
            context.EpochStableSeed ^ 0x77a3,
            profile.InteriorBeltScale * 2.6d,
            3);
        var beltMask = Math.Clamp(
            (profile.InteriorBeltCoverage - Math.Abs(beltField)) / 0.18d,
            0d,
            1d);
        var ridged = 1d - Math.Abs(PhysicalNoise.FractalNoise(
            x,
            y,
            context.EpochStableSeed ^ 0x3b59,
            profile.InteriorBeltScale,
            4));
        belt = continentality * beltMask * Math.Pow(ridged, 1.6d) * profile.InteriorBeltStrength;
        relief = (continentality * macro) + belt;
    }

    private static void ApplyIncrementalUplift(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        int triangleIndex,
        double boundaryRelief,
        double belt)
    {
        // Tectonics act on the crust, not on the surface. Routing the epoch's
        // work through thickness is what lets it compound: convergence that
        // thickens the same ground in three epochs stacks, where three
        // repaintings of a height field would not. It also means crustal
        // limits bound the result rather than an elevation cap.
        var beltUplift = belt * context.Profile.EpochInteriorUpliftShare / 1000d;
        var heightDrive = PhysicalElevation.FromUnits(
            (boundaryRelief + beltUplift) * context.Profile.EpochTectonicElevationStep *
                context.TectonicStrength / 1000d) +
            AccretionUplift(map, context, triangleIndex);
        var delta = ApplyThicknessChange(map, triangleIndex, heightDrive);
        var maximumBedrock = PhysicalElevation.FromUnits(2000) - map.SedimentThickness[triangleIndex];
        var nextBedrock = Math.Clamp(
            map.BedrockElevation[triangleIndex] + delta,
            PhysicalElevation.FromUnits(-2000),
            maximumBedrock);
        var applied = nextBedrock - map.BedrockElevation[triangleIndex];
        map.BedrockElevation[triangleIndex] = nextBedrock;
        LandscapeSurface.Rebuild(map, triangleIndex);
        map.TectonicActivityHistory[triangleIndex] = checked(
            map.TectonicActivityHistory[triangleIndex] + PhysicalElevation.ToUnits(Math.Abs(applied)));
    }

    /// <summary>
    /// Turns an intended height change into the thickness change that would
    /// produce it, applies that to the crust, and reports the height the crust
    /// actually gained. They differ where the crust hits its thickness limits,
    /// which is the point: a range stops growing because the crust cannot
    /// thicken further, not because a height cap intervened.
    /// </summary>
    private static long ApplyThicknessChange(
        PhysicalGeographyField map,
        int triangleIndex,
        long heightDrive)
    {
        var buoyancy = PhysicalCrust.BuoyancyPerMille / 1000d;
        var currentUnits = PhysicalElevation.ToUnits(map.CrustThickness[triangleIndex]);
        var nextUnits = PhysicalCrust.ClampThicknessUnits(
            currentUnits + (PhysicalElevation.ToUnits(heightDrive) / buoyancy));
        map.CrustThickness[triangleIndex] = PhysicalElevation.FromUnits(nextUnits);
        return PhysicalElevation.FromUnits(
            PhysicalCrust.HeightUnits(nextUnits) - PhysicalCrust.HeightUnits(currentUnits));
    }

    private static double InheritedRelief(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile) =>
        Math.Clamp(
            map.TectonicActivityHistory[triangleIndex] /
                Math.Max(1d, profile.EpochTectonicElevationStep * 1.8d),
            0d,
            1d);

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        var convergentRidge = false;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.RidgeInfluence[triangleIndex] is < 0f or > 1f)
            {
                throw new InvalidOperationException("Tectonic ridge influence is outside its scalar range.");
            }

            convergentRidge |=
                map.TectonicBoundaryCodes[triangleIndex] == PhysicalTectonicBoundaryCodes.Convergent &&
                map.RidgeInfluence[triangleIndex] > 0.15f;
        }

        if (!convergentRidge)
        {
            throw new InvalidOperationException("Tectonic uplift produced no convergent mountain system.");
        }
    }

    private static double BoundaryRelief(
        PhysicalGeographyField map,
        int triangleIndex,
        double strain,
        PhysicalGeographyGenerationContext context)
    {
        var profile = context.Profile;
        var shaped = strain * strain;
        return map.TectonicBoundaryCodes[triangleIndex] switch
        {
            PhysicalTectonicBoundaryCodes.Convergent => shaped * profile.RidgeUplift *
                ConvergentCrustScale(map, triangleIndex, context) *
                RangeConcentration(map, context, triangleIndex),
            PhysicalTectonicBoundaryCodes.Divergent => -shaped * profile.RiftDepth,
            PhysicalTectonicBoundaryCodes.Transform =>
                shaped * profile.RidgeUplift *
                (map.CrustTypeCodes[triangleIndex] == PhysicalCrustTypeCodes.Continental ? 0.16d : 0.05d),
            _ => 0d,
        };
    }

    /// <summary>
    /// Gathers uplift onto narrow crests instead of spreading it evenly across
    /// a tectonic zone. Strain is a smooth field, so squaring it still yields a
    /// smear; a ridged field at range wavelength turns that smear into crests
    /// with flanks steep enough for drainage to organise around. Mean
    /// preserved, so this redistributes uplift rather than adding any.
    /// </summary>
    private static double RangeConcentration(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        int triangleIndex)
    {
        var noise = PhysicalNoise.FractalNoise(
            map.Grid.TriangleX(triangleIndex),
            map.Grid.TriangleY(triangleIndex),
            context.Seed ^ 0x6b1d,
            context.Profile.RangeConcentrationScale,
            3);
        var crest = 1d - Math.Abs(noise);
        return 0.30d + (context.Profile.RangeConcentrationSharpness * crest * crest * crest);
    }

    private static double ConvergentCrustScale(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationContext context)
    {
        if (map.CrustTypeCodes[triangleIndex] == PhysicalCrustTypeCodes.Continental)
        {
            return 1d;
        }

        var arcNoise = PhysicalNoise.UnitNoise(
            map.Grid.TriangleX(triangleIndex),
            map.Grid.TriangleY(triangleIndex),
            context.Seed ^ 0x39d1,
            Math.Max(3d, context.Profile.RidgeWidth * 0.62d),
            2);
        var exposedArc = Math.Pow(Math.Clamp((arcNoise - 0.56d) / 0.44d, 0d, 1d), 2d);
        return 0.08d + (exposedArc * 0.30d);
    }

}
