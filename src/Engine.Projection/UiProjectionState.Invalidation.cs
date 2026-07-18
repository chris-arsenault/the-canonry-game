namespace Engine.Projection;

public sealed partial class UiProjectionState
{
    private void MarkEntityVisualDirty(int entityId)
    {
        if (_entities.TryGetValue(entityId, out var entity))
        {
            MarkEntityVisualDirty(entity);
        }
    }

    private void MarkEntityVisualDirty(UiEntityProjection entity)
    {
        if (entity.StatusCode != EntityStatusCodes.Active)
        {
            return;
        }

        MarkEntityHexDirty(PlaneCodes.Physical, new HexCoord(entity.PhysicalQ, entity.PhysicalR));
        MarkEntityHexDirty(PlaneCodes.Semantic, new HexCoord(entity.SemanticQ, entity.SemanticR));
    }

    private void MarkEntityBrowserStructureChanged(int entityId)
    {
        EntityBrowserVersion++;
        EntityBrowserStructureVersion++;
        EntityBrowserRowVersion++;
        AddDirtyEntityBrowserRow(entityId);
    }

    private void MarkEntityBrowserStructureChanged(int firstEntityId, int secondEntityId)
    {
        EntityBrowserVersion++;
        EntityBrowserStructureVersion++;
        EntityBrowserRowVersion++;
        AddDirtyEntityBrowserRow(firstEntityId);
        AddDirtyEntityBrowserRow(secondEntityId);
    }

    private void MarkEntityBrowserRowChanged(int entityId)
    {
        EntityBrowserVersion++;
        EntityBrowserRowVersion++;
        AddDirtyEntityBrowserRow(entityId);
    }

    private void AddDirtyEntityBrowserRow(int entityId)
    {
        if (entityId > 0)
        {
            _dirtyEntityBrowserRows.Add(entityId);
        }
    }
}
