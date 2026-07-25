namespace Engine.Kernel;

internal sealed partial class WorldState
{
    internal long CultureFieldRevision(int cultureId) =>
        (uint)cultureId < (uint)_cultureFieldRevisions.Length
            ? _cultureFieldRevisions[cultureId]
            : 0;

    internal long CultureWorkRevision(int cultureId) =>
        (uint)cultureId < (uint)_cultureWorkRevisions.Length
            ? _cultureWorkRevisions[cultureId]
            : 0;

    private void MarkCultureFieldDirty(int cultureId)
    {
        if ((uint)cultureId == 0u || cultureId >= _cultureFieldRevisions.Length)
        {
            return;
        }

        if (_cultureFieldRevisions[cultureId] < long.MaxValue)
        {
            _cultureFieldRevisions[cultureId]++;
        }

        MarkCultureWorkDirty(cultureId);
    }

    private void MarkCultureWorkDirty(int cultureId)
    {
        if ((uint)cultureId == 0u || cultureId >= _cultureWorkRevisions.Length)
        {
            return;
        }

        if (_cultureWorkRevisions[cultureId] < long.MaxValue)
        {
            _cultureWorkRevisions[cultureId]++;
        }
    }

    private void MarkEntityCultureWorkDirty(int entityId)
    {
        if ((uint)entityId == 0u || entityId >= _entityCultureId.Length)
        {
            return;
        }

        MarkCultureWorkDirty(_entityCultureId[entityId]);
    }
}
