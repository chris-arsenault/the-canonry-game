namespace TheCanonryGame.Game.Tests;

using Engine.Constants;
using Engine.Kinds;
using Engine.Projection;

using Xunit.Abstractions;

public sealed class RegistryHierarchySmokeTests
{
    private readonly ITestOutputHelper _output;

    public RegistryHierarchySmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SeededWorld_HasRealmsPlacesAndLivesTagged()
    {
        using var runtime = CanonryGameRuntimeFactory.Start(GameSeedPolicy.Requested(73));
        runtime.Controls.Pause();
        runtime.Update();

        var entities = runtime.ReadModels.Entities;

        var realmCount = entities.Entities.Values.Count(
            e => e.StatusCode == EntityStatusCodes.Active && entities.EntityTierOf(e.EntityId) == EntityTier.Realm);
        var placeCount = entities.Entities.Values.Count(
            e => e.StatusCode == EntityStatusCodes.Active && entities.EntityTierOf(e.EntityId) == EntityTier.Place);
        var lifeCount = entities.Entities.Values.Count(
            e => e.StatusCode == EntityStatusCodes.Active && entities.EntityTierOf(e.EntityId) == EntityTier.Life);

        Assert.True(realmCount > 0, "Expected at least one Realm-tier entity from seed.");
        Assert.True(placeCount > 0, "Expected at least one Place-tier entity from seed.");
        Assert.True(lifeCount > 0, "Expected at least one Life-tier entity from seed.");
    }

    [Fact]
    public void SeededWorld_PlacesAffiliateToRealmsViaControlledBy()
    {
        using var runtime = CanonryGameRuntimeFactory.Start(GameSeedPolicy.Requested(73));
        runtime.Controls.Pause();
        runtime.Update();

        var entities = runtime.ReadModels.Entities;
        var places = entities.Entities.Values
            .Where(e => e.StatusCode == EntityStatusCodes.Active && entities.EntityTierOf(e.EntityId) == EntityTier.Place)
            .ToList();
        var realmIds = entities.Entities.Values
            .Where(e => e.StatusCode == EntityStatusCodes.Active && entities.EntityTierOf(e.EntityId) == EntityTier.Realm)
            .Select(e => e.EntityId)
            .ToHashSet();

        var placesUnderRealms = 0;
        foreach (var place in places)
        {
            foreach (var rel in entities.ActiveOutgoingRelationships(place.EntityId))
            {
                if (entities.IsAffiliationRelationship(rel.Key.RelationshipKindId) &&
                    realmIds.Contains(rel.Key.TargetEntityId))
                {
                    placesUnderRealms++;
                    break;
                }
            }
        }

        Assert.True(
            placesUnderRealms > 0,
            $"Expected at least one Place to be affiliated to a Realm. Places={places.Count}, Realms={realmIds.Count}");
    }

    [Fact]
    public void SeededWorld_DumpsTreeShape()
    {
        using var runtime = CanonryGameRuntimeFactory.Start(GameSeedPolicy.Requested(73));
        runtime.Controls.Resume();
        runtime.Controls.Step(50);
        for (var i = 0; i < 70; i++)
        {
            runtime.Update();
        }

        var entities = runtime.ReadModels.Entities;
        var symbols = runtime.ReadModels.Symbols;

        var realms = entities.Entities.Values
            .Where(e => e.StatusCode == EntityStatusCodes.Active && entities.EntityTierOf(e.EntityId) == EntityTier.Realm)
            .OrderBy(e => Name(symbols, entities, e.EntityId), StringComparer.Ordinal)
            .ToList();

        _output.WriteLine($"=== Registry tree @ tick {entities.CurrentTick} ===");
        foreach (var realm in realms)
        {
            _output.WriteLine($"+ {Name(symbols, entities, realm.EntityId)}");
            DumpRealmPlaces(entities, symbols, realm.EntityId);
        }

        Assert.NotEmpty(realms);
    }

    private void DumpRealmPlaces(
        UiProjectionState entities,
        IProjectionSymbolCatalog symbols,
        int realmId)
    {
        var placeChildren = CollectAffiliationChildren(entities, realmId, EntityTier.Place);
        foreach (var placeId in placeChildren.OrderBy(id => Name(symbols, entities, id), StringComparer.Ordinal))
        {
            _output.WriteLine($"    - {Name(symbols, entities, placeId)}");
            var lifeChildren = CollectAffiliationChildren(entities, placeId, EntityTier.Life);
            foreach (var lifeId in lifeChildren.OrderBy(id => Name(symbols, entities, id), StringComparer.Ordinal))
            {
                _output.WriteLine($"        . {Name(symbols, entities, lifeId)}");
            }
        }
    }

    private static List<int> CollectAffiliationChildren(
        UiProjectionState entities,
        int parentId,
        EntityTier expectedTier)
    {
        var children = new HashSet<int>();
        foreach (var rel in entities.ActiveIncomingRelationships(parentId))
        {
            if (!entities.IsAffiliationRelationship(rel.Key.RelationshipKindId))
            {
                continue;
            }

            if (entities.EntityTierOf(rel.Key.SourceEntityId) != expectedTier)
            {
                continue;
            }

            children.Add(rel.Key.SourceEntityId);
        }

        return [.. children];
    }

    private static string Name(
        IProjectionSymbolCatalog symbols,
        UiProjectionState entities,
        int entityId)
    {
        if (entities.Entities.TryGetValue(entityId, out var entity))
        {
            var key = new ProjectionEntityNameKey(
                entityId,
                entity.KindId,
                entity.SubtypeId,
                entity.CultureId,
                entity.NameSeed);
            if (symbols.TryResolveEntityName(key, out var displayName))
            {
                return displayName;
            }
        }

        return $"entity:{entityId}";
    }
}
