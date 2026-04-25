namespace Engine.Kernel;

public interface IRelationshipTraversalVisitor
{
    void Visit(int entityId, int relationshipId, int depth);
}
