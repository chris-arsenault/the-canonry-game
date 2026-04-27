namespace Engine.Kernel;

internal interface IRelationshipTraversalVisitor
{
    void Visit(int entityId, int relationshipId, int depth);
}
