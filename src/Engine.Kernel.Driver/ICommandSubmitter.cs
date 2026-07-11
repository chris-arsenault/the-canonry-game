namespace Engine.Kernel.Driver;

public interface ICommandSubmitter
{
    bool TrySetGeneratorEnabled(in SetGeneratorEnabledCommand command, out CommandHandle handle);

    bool TrySetGambitEnabled(in SetGambitEnabledCommand command, out CommandHandle handle);

    bool TryBlessCellCulture(in BlessCellCultureCommand command, out CommandHandle handle);

    bool TryFavorEntity(in FavorEntityCommand command, out CommandHandle handle);
}
