namespace Engine.Projection;

/// <summary>
/// Host-computed culture power ledger snapshot. Shares are 0-100 percentages of
/// the world total for each axis; the raw component values back the shares so
/// telemetry and analysis can recompute or re-weight them.
/// </summary>
public readonly record struct CulturePowerSummary(
    int CultureId,
    float WitnessShare,
    float ShadowShare,
    float WakeShare,
    float WitnessMass,
    float HiddenMass,
    float SemanticMass,
    float CustodyStrength,
    float ClaimStrength,
    float WarStrength,
    float OfferingStrength);
