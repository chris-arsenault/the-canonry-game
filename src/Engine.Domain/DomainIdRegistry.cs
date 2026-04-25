namespace Engine.Domain;

public sealed class DomainIdRegistry
{
    private readonly StableNameRegistry<AttributeRuntimeId> _attributes =
        new(static value => new AttributeRuntimeId(value), static id => id.Value, "attribute");
    private readonly StableNameRegistry<RelationshipKindRuntimeId> _relationshipKinds =
        new(static value => new RelationshipKindRuntimeId(value), static id => id.Value, "relationship kind");
    private readonly StableNameRegistry<RelationshipFactRuntimeId> _relationshipFacts =
        new(static value => new RelationshipFactRuntimeId(value), static id => id.Value, "relationship fact");
    private readonly StableNameRegistry<PressureRuntimeId> _pressures =
        new(static value => new PressureRuntimeId(value), static id => id.Value, "pressure");
    private readonly StableNameRegistry<ResourceRuntimeId> _resources =
        new(static value => new ResourceRuntimeId(value), static id => id.Value, "resource");
    private readonly StableNameRegistry<TerrainRuntimeId> _terrains =
        new(static value => new TerrainRuntimeId(value), static id => id.Value, "terrain");
    private readonly StableNameRegistry<SubtypeRuntimeId> _subtypes =
        new(static value => new SubtypeRuntimeId(value), static id => id.Value, "subtype");
    private readonly StableNameRegistry<CultureRuntimeId> _cultures =
        new(static value => new CultureRuntimeId(value), static id => id.Value, "culture");
    private readonly StableNameRegistry<GeneratorRuntimeId> _generators =
        new(static value => new GeneratorRuntimeId(value), static id => id.Value, "generator");
    private readonly StableNameRegistry<DetectorRuntimeId> _detectors =
        new(static value => new DetectorRuntimeId(value), static id => id.Value, "detector");
    private readonly StableNameRegistry<EventCodeId> _eventCodes =
        new(static value => new EventCodeId(value), static id => id.Value, "event code");
    private readonly StableNameRegistry<ReasonCodeId> _reasonCodes =
        new(static value => new ReasonCodeId(value), static id => id.Value, "reason code");
    private readonly StableNameRegistry<SourceCodeId> _sourceCodes =
        new(static value => new SourceCodeId(value), static id => id.Value, "source code");

    public AttributeRuntimeId RegisterAttribute(string stableName) => _attributes.Register(stableName);
    public AttributeRuntimeId AttributeIdOf(string stableName) => _attributes.Resolve(stableName);
    public string AttributeNameOf(AttributeRuntimeId id) => _attributes.NameOf(id);

    public RelationshipKindRuntimeId RegisterRelationshipKind(string stableName) =>
        _relationshipKinds.Register(stableName);
    public RelationshipKindRuntimeId RelationshipKindIdOf(string stableName) =>
        _relationshipKinds.Resolve(stableName);
    public string RelationshipKindNameOf(RelationshipKindRuntimeId id) =>
        _relationshipKinds.NameOf(id);

    public RelationshipFactRuntimeId RegisterRelationshipFact(string stableName) =>
        _relationshipFacts.Register(stableName);
    public RelationshipFactRuntimeId RelationshipFactIdOf(string stableName) =>
        _relationshipFacts.Resolve(stableName);
    public string RelationshipFactNameOf(RelationshipFactRuntimeId id) =>
        _relationshipFacts.NameOf(id);

    public PressureRuntimeId RegisterPressure(string stableName) => _pressures.Register(stableName);
    public PressureRuntimeId PressureIdOf(string stableName) => _pressures.Resolve(stableName);
    public string PressureNameOf(PressureRuntimeId id) => _pressures.NameOf(id);

    public ResourceRuntimeId RegisterResource(string stableName) => _resources.Register(stableName);
    public ResourceRuntimeId ResourceIdOf(string stableName) => _resources.Resolve(stableName);
    public string ResourceNameOf(ResourceRuntimeId id) => _resources.NameOf(id);

    public TerrainRuntimeId RegisterTerrain(string stableName) => _terrains.Register(stableName);
    public TerrainRuntimeId TerrainIdOf(string stableName) => _terrains.Resolve(stableName);
    public string TerrainNameOf(TerrainRuntimeId id) => _terrains.NameOf(id);

    public SubtypeRuntimeId RegisterSubtype(string stableName) => _subtypes.Register(stableName);
    public SubtypeRuntimeId SubtypeIdOf(string stableName) => _subtypes.Resolve(stableName);
    public string SubtypeNameOf(SubtypeRuntimeId id) => _subtypes.NameOf(id);

    public CultureRuntimeId RegisterCulture(string stableName) => _cultures.Register(stableName);
    public CultureRuntimeId CultureIdOf(string stableName) => _cultures.Resolve(stableName);
    public string CultureNameOf(CultureRuntimeId id) => _cultures.NameOf(id);

    public GeneratorRuntimeId RegisterGenerator(string stableName) => _generators.Register(stableName);
    public GeneratorRuntimeId GeneratorIdOf(string stableName) => _generators.Resolve(stableName);
    public string GeneratorNameOf(GeneratorRuntimeId id) => _generators.NameOf(id);

    public DetectorRuntimeId RegisterDetector(string stableName) => _detectors.Register(stableName);
    public DetectorRuntimeId DetectorIdOf(string stableName) => _detectors.Resolve(stableName);
    public string DetectorNameOf(DetectorRuntimeId id) => _detectors.NameOf(id);

    public EventCodeId RegisterEventCode(string stableName) => _eventCodes.Register(stableName);
    public EventCodeId EventCodeIdOf(string stableName) => _eventCodes.Resolve(stableName);
    public string EventCodeNameOf(EventCodeId id) => _eventCodes.NameOf(id);

    public ReasonCodeId RegisterReasonCode(string stableName) => _reasonCodes.Register(stableName);
    public ReasonCodeId ReasonCodeIdOf(string stableName) => _reasonCodes.Resolve(stableName);
    public string ReasonCodeNameOf(ReasonCodeId id) => _reasonCodes.NameOf(id);

    public SourceCodeId RegisterSourceCode(string stableName) => _sourceCodes.Register(stableName);
    public SourceCodeId SourceCodeIdOf(string stableName) => _sourceCodes.Resolve(stableName);
    public string SourceCodeNameOf(SourceCodeId id) => _sourceCodes.NameOf(id);

    private sealed class StableNameRegistry<TId>(
        Func<int, TId> createId,
        Func<TId, int> valueOf,
        string category)
        where TId : struct
    {
        private readonly Dictionary<string, TId> _idsByName = new(StringComparer.Ordinal);
        private readonly List<string> _namesById = [string.Empty];

        public TId Register(string stableName)
        {
            ValidateStableName(stableName);
            if (_idsByName.ContainsKey(stableName))
            {
                throw new InvalidOperationException($"Duplicate {category} registration '{stableName}'.");
            }

            var id = createId(_namesById.Count);
            _idsByName.Add(stableName, id);
            _namesById.Add(stableName);
            return id;
        }

        public TId Resolve(string stableName)
        {
            ValidateStableName(stableName);
            if (_idsByName.TryGetValue(stableName, out var id))
            {
                return id;
            }

            throw new KeyNotFoundException($"Unknown {category} '{stableName}'.");
        }

        public string NameOf(TId id)
        {
            var value = valueOf(id);
            if (value <= 0 || value >= _namesById.Count)
            {
                throw new KeyNotFoundException($"Unknown {category} id {value}.");
            }

            return _namesById[value];
        }

        private static void ValidateStableName(string stableName)
        {
            ArgumentNullException.ThrowIfNull(stableName);
            if (string.IsNullOrWhiteSpace(stableName))
            {
                throw new ArgumentException("Stable names must be non-empty.", nameof(stableName));
            }
        }
    }
}
