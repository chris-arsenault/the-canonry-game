namespace Engine.Domain;

using Engine.Gambits;
using Engine.Kinds;

public enum DomainIdCategory
{
    Kind,
    Attribute,
    Trait,
    Gambit,
    StructuralFact,
    RelationshipKind,
    Terrain,
    TravelStructure,
    Subtype,
    Culture,
    Generator,
    Verb,
    EventCode,
    ReasonCode,
    SourceCode,
    Tag,
    KnowledgeTech,
}

public readonly record struct DomainIdEntry(
    DomainIdCategory Category,
    int Id,
    string StableName);

public sealed class DomainIdRegistry
{
    private readonly StableNameRegistry<KindRuntimeId> _kinds =
        new(static value => new KindRuntimeId(value), static id => id.Value, "kind");
    private readonly StableNameRegistry<AttributeRuntimeId> _attributes =
        new(static value => new AttributeRuntimeId(value), static id => id.Value, "attribute");
    private readonly StableNameRegistry<TraitRuntimeId> _traits =
        new(static value => new TraitRuntimeId(value), static id => id.Value, "trait");
    private readonly StableNameRegistry<GambitRuntimeId> _gambits =
        new(static value => new GambitRuntimeId(value), static id => id.Value, "gambit");
    private readonly StableNameRegistry<StructuralFactRuntimeId> _structuralFacts =
        new(static value => new StructuralFactRuntimeId(value), static id => id.Value, "structural fact");
    private readonly StableNameRegistry<RelationshipKindRuntimeId> _relationshipKinds =
        new(static value => new RelationshipKindRuntimeId(value), static id => id.Value, "relationship kind");
    private readonly StableNameRegistry<TerrainRuntimeId> _terrains =
        new(static value => new TerrainRuntimeId(value), static id => id.Value, "terrain");
    private readonly StableNameRegistry<TerrainBoundaryGroupRuntimeId> _terrainBoundaryGroups =
        new(static value => new TerrainBoundaryGroupRuntimeId(value), static id => id.Value, "terrain boundary group");
    private readonly StableNameRegistry<TravelStructureRuntimeId> _travelStructures =
        new(static value => new TravelStructureRuntimeId(value), static id => id.Value, "travel structure");
    private readonly StableNameRegistry<SubtypeRuntimeId> _subtypes =
        new(static value => new SubtypeRuntimeId(value), static id => id.Value, "subtype");
    private readonly StableNameRegistry<CultureRuntimeId> _cultures =
        new(static value => new CultureRuntimeId(value), static id => id.Value, "culture");
    private readonly StableNameRegistry<GeneratorRuntimeId> _generators =
        new(static value => new GeneratorRuntimeId(value), static id => id.Value, "generator");
    private readonly StableNameRegistry<VerbRuntimeId> _verbs =
        new(static value => new VerbRuntimeId(value), static id => id.Value, "verb");
    private readonly StableNameRegistry<EventCodeId> _eventCodes =
        new(static value => new EventCodeId(value), static id => id.Value, "event code");
    private readonly StableNameRegistry<ReasonCodeId> _reasonCodes =
        new(static value => new ReasonCodeId(value), static id => id.Value, "reason code");
    private readonly StableNameRegistry<SourceCodeId> _sourceCodes =
        new(static value => new SourceCodeId(value), static id => id.Value, "source code");
    private readonly StableNameRegistry<TagRuntimeId> _tags =
        new(static value => new TagRuntimeId(value), static id => id.Value, "tag");
    private readonly StableNameRegistry<KnowledgeTechRuntimeId> _knowledgeTechs =
        new(static value => new KnowledgeTechRuntimeId(value), static id => id.Value, "knowledge tech");

    public KindRuntimeId RegisterKind(string stableName) => _kinds.Register(stableName);
    public KindRuntimeId RegisterKind(KindId kind) => RegisterKind(kind.Value);
    public KindRuntimeId KindIdOf(string stableName) => _kinds.Resolve(stableName);
    public KindRuntimeId KindIdOf(KindId kind) => KindIdOf(kind.Value);
    public string KindNameOf(KindRuntimeId id) => _kinds.NameOf(id);

    public AttributeRuntimeId RegisterAttribute(string stableName) => _attributes.Register(stableName);
    public AttributeRuntimeId AttributeIdOf(string stableName) => _attributes.Resolve(stableName);
    public string AttributeNameOf(AttributeRuntimeId id) => _attributes.NameOf(id);

    public TraitRuntimeId RegisterTrait(string stableName) => _traits.Register(stableName);
    public TraitRuntimeId TraitIdOf(string stableName) => _traits.Resolve(stableName);
    public string TraitNameOf(TraitRuntimeId id) => _traits.NameOf(id);

    public GambitRuntimeId RegisterGambit(string stableName) => _gambits.Register(stableName);
    public GambitRuntimeId GambitIdOf(string stableName) => _gambits.Resolve(stableName);
    public string GambitNameOf(GambitRuntimeId id) => _gambits.NameOf(id);

    public StructuralFactRuntimeId RegisterStructuralFact(string stableName) => _structuralFacts.Register(stableName);
    public StructuralFactRuntimeId StructuralFactIdOf(string stableName) => _structuralFacts.Resolve(stableName);
    public string StructuralFactNameOf(StructuralFactRuntimeId id) => _structuralFacts.NameOf(id);

    public RelationshipKindRuntimeId RegisterRelationshipKind(string stableName) =>
        _relationshipKinds.Register(stableName);
    public RelationshipKindRuntimeId RelationshipKindIdOf(string stableName) =>
        _relationshipKinds.Resolve(stableName);
    public string RelationshipKindNameOf(RelationshipKindRuntimeId id) =>
        _relationshipKinds.NameOf(id);

    public TerrainRuntimeId RegisterTerrain(string stableName) => _terrains.Register(stableName);
    public TerrainRuntimeId TerrainIdOf(string stableName) => _terrains.Resolve(stableName);
    public string TerrainNameOf(TerrainRuntimeId id) => _terrains.NameOf(id);

    public TerrainBoundaryGroupRuntimeId RegisterTerrainBoundaryGroup(string stableName) =>
        _terrainBoundaryGroups.Register(stableName);

    public TerrainBoundaryGroupRuntimeId TerrainBoundaryGroupIdOf(string stableName) =>
        _terrainBoundaryGroups.Resolve(stableName);

    public string TerrainBoundaryGroupNameOf(TerrainBoundaryGroupRuntimeId id) =>
        _terrainBoundaryGroups.NameOf(id);

    public TravelStructureRuntimeId RegisterTravelStructure(string stableName) => _travelStructures.Register(stableName);
    public TravelStructureRuntimeId TravelStructureIdOf(string stableName) => _travelStructures.Resolve(stableName);
    public string TravelStructureNameOf(TravelStructureRuntimeId id) => _travelStructures.NameOf(id);

    public SubtypeRuntimeId RegisterSubtype(string stableName) => _subtypes.Register(stableName);
    public SubtypeRuntimeId SubtypeIdOf(string stableName) => _subtypes.Resolve(stableName);
    public string SubtypeNameOf(SubtypeRuntimeId id) => _subtypes.NameOf(id);

    public CultureRuntimeId RegisterCulture(string stableName) => _cultures.Register(stableName);
    public CultureRuntimeId CultureIdOf(string stableName) => _cultures.Resolve(stableName);
    public string CultureNameOf(CultureRuntimeId id) => _cultures.NameOf(id);

    public GeneratorRuntimeId RegisterGenerator(string stableName) => _generators.Register(stableName);
    public GeneratorRuntimeId GeneratorIdOf(string stableName) => _generators.Resolve(stableName);
    public string GeneratorNameOf(GeneratorRuntimeId id) => _generators.NameOf(id);

    public VerbRuntimeId RegisterVerb(string stableName) => _verbs.Register(stableName);
    public VerbRuntimeId VerbIdOf(string stableName) => _verbs.Resolve(stableName);
    public string VerbNameOf(VerbRuntimeId id) => _verbs.NameOf(id);

    public EventCodeId RegisterEventCode(string stableName) => _eventCodes.Register(stableName);
    public EventCodeId EventCodeIdOf(string stableName) => _eventCodes.Resolve(stableName);
    public string EventCodeNameOf(EventCodeId id) => _eventCodes.NameOf(id);

    public ReasonCodeId RegisterReasonCode(string stableName) => _reasonCodes.Register(stableName);
    public ReasonCodeId ReasonCodeIdOf(string stableName) => _reasonCodes.Resolve(stableName);
    public string ReasonCodeNameOf(ReasonCodeId id) => _reasonCodes.NameOf(id);

    public SourceCodeId RegisterSourceCode(string stableName) => _sourceCodes.Register(stableName);
    public SourceCodeId SourceCodeIdOf(string stableName) => _sourceCodes.Resolve(stableName);
    public string SourceCodeNameOf(SourceCodeId id) => _sourceCodes.NameOf(id);

    public TagRuntimeId RegisterTag(string stableName) => _tags.Register(stableName);
    public TagRuntimeId TagIdOf(string stableName) => _tags.Resolve(stableName);
    public string TagNameOf(TagRuntimeId id) => _tags.NameOf(id);

    public KnowledgeTechRuntimeId RegisterKnowledgeTech(string stableName) => _knowledgeTechs.Register(stableName);
    public KnowledgeTechRuntimeId KnowledgeTechIdOf(string stableName) => _knowledgeTechs.Resolve(stableName);
    public string KnowledgeTechNameOf(KnowledgeTechRuntimeId id) => _knowledgeTechs.NameOf(id);
    public int KnowledgeTechCount => _knowledgeTechs.Count;

    public IEnumerable<DomainIdEntry> Entries()
    {
        foreach (var entry in PrimaryEntries()) yield return entry;
        foreach (var entry in SecondaryEntries()) yield return entry;
        foreach (var entry in TertiaryEntries()) yield return entry;
    }

    private IEnumerable<DomainIdEntry> PrimaryEntries()
    {
        foreach (var entry in _kinds.Entries(DomainIdCategory.Kind)) yield return entry;
        foreach (var entry in _attributes.Entries(DomainIdCategory.Attribute)) yield return entry;
        foreach (var entry in _traits.Entries(DomainIdCategory.Trait)) yield return entry;
        foreach (var entry in _gambits.Entries(DomainIdCategory.Gambit)) yield return entry;
        foreach (var entry in _structuralFacts.Entries(DomainIdCategory.StructuralFact)) yield return entry;
        foreach (var entry in _relationshipKinds.Entries(DomainIdCategory.RelationshipKind)) yield return entry;
    }

    private IEnumerable<DomainIdEntry> SecondaryEntries()
    {
        foreach (var entry in _terrains.Entries(DomainIdCategory.Terrain)) yield return entry;
        foreach (var entry in _travelStructures.Entries(DomainIdCategory.TravelStructure)) yield return entry;
        foreach (var entry in _subtypes.Entries(DomainIdCategory.Subtype)) yield return entry;
        foreach (var entry in _cultures.Entries(DomainIdCategory.Culture)) yield return entry;
        foreach (var entry in _generators.Entries(DomainIdCategory.Generator)) yield return entry;
        foreach (var entry in _verbs.Entries(DomainIdCategory.Verb)) yield return entry;
        foreach (var entry in _eventCodes.Entries(DomainIdCategory.EventCode)) yield return entry;
        foreach (var entry in _reasonCodes.Entries(DomainIdCategory.ReasonCode)) yield return entry;
        foreach (var entry in _sourceCodes.Entries(DomainIdCategory.SourceCode)) yield return entry;
    }

    private IEnumerable<DomainIdEntry> TertiaryEntries()
    {
        foreach (var entry in _tags.Entries(DomainIdCategory.Tag)) yield return entry;
        foreach (var entry in _knowledgeTechs.Entries(DomainIdCategory.KnowledgeTech)) yield return entry;
    }

    public DomainIdRegistry Clone()
    {
        var clone = new DomainIdRegistry();
        foreach (var entry in Entries())
        {
            RegisterCloneEntry(clone, entry);
        }

        return clone;
    }

    private static void RegisterCloneEntry(DomainIdRegistry clone, DomainIdEntry entry)
    {
        if (RegisterPrimaryCloneEntry(clone, entry))
        {
            return;
        }

        if (RegisterSecondaryCloneEntry(clone, entry))
        {
            return;
        }

        if (entry.Category == DomainIdCategory.Tag)
        {
            clone.RegisterTag(entry.StableName);
            return;
        }

        if (entry.Category == DomainIdCategory.KnowledgeTech)
        {
            clone.RegisterKnowledgeTech(entry.StableName);
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(entry), entry.Category, null);
    }

    private static bool RegisterPrimaryCloneEntry(DomainIdRegistry clone, DomainIdEntry entry)
    {
        switch (entry.Category)
        {
            case DomainIdCategory.Kind:
                clone.RegisterKind(entry.StableName);
                return true;
            case DomainIdCategory.Attribute:
                clone.RegisterAttribute(entry.StableName);
                return true;
            case DomainIdCategory.Trait:
                clone.RegisterTrait(entry.StableName);
                return true;
            case DomainIdCategory.Gambit:
                clone.RegisterGambit(entry.StableName);
                return true;
            case DomainIdCategory.StructuralFact:
                clone.RegisterStructuralFact(entry.StableName);
                return true;
            case DomainIdCategory.RelationshipKind:
                clone.RegisterRelationshipKind(entry.StableName);
                return true;
            default:
                return false;
        }
    }

    private static bool RegisterSecondaryCloneEntry(DomainIdRegistry clone, DomainIdEntry entry)
    {
        switch (entry.Category)
        {
            case DomainIdCategory.Terrain:
                clone.RegisterTerrain(entry.StableName);
                return true;
            case DomainIdCategory.TravelStructure:
                clone.RegisterTravelStructure(entry.StableName);
                return true;
            case DomainIdCategory.Subtype:
                clone.RegisterSubtype(entry.StableName);
                return true;
            case DomainIdCategory.Culture:
                clone.RegisterCulture(entry.StableName);
                return true;
            case DomainIdCategory.Generator:
                clone.RegisterGenerator(entry.StableName);
                return true;
            case DomainIdCategory.Verb:
                clone.RegisterVerb(entry.StableName);
                return true;
            case DomainIdCategory.EventCode:
                clone.RegisterEventCode(entry.StableName);
                return true;
            case DomainIdCategory.ReasonCode:
                clone.RegisterReasonCode(entry.StableName);
                return true;
            case DomainIdCategory.SourceCode:
                clone.RegisterSourceCode(entry.StableName);
                return true;
            default:
                return false;
        }
    }

    private sealed class StableNameRegistry<TId>(
        Func<int, TId> createId,
        Func<TId, int> valueOf,
        string category)
        where TId : struct
    {
        private readonly Dictionary<string, TId> _idsByName = new(StringComparer.Ordinal);
        private readonly List<string> _namesById = [string.Empty];

        public int Count => _namesById.Count - 1;

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

        public IEnumerable<DomainIdEntry> Entries(DomainIdCategory domainCategory)
        {
            for (var id = 1; id < _namesById.Count; id++)
            {
                yield return new DomainIdEntry(domainCategory, id, _namesById[id]);
            }
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
