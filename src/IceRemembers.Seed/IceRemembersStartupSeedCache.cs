namespace IceRemembers.Seed;

using Engine.Host;

internal enum IceRemembersStartupSeedCacheStage
{
    CheckingCache,
    ReadingCache,
    GeneratingWorld,
    WritingCache,
}

internal sealed record IceRemembersStartupSeedCacheResult(
    DomainStartupSeedResult StartupSeed,
    bool CacheHit,
    string CachePath,
    string? Warning);

internal static class IceRemembersStartupSeedCache
{
    private const int WorldGenerationVersion = 4;

    public static IceRemembersStartupSeedCacheResult LoadOrCreate(
        int baseSeed,
        string cacheDirectory,
        bool regenerate,
        Action<IceRemembersStartupSeedCacheStage>? reportStage = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseSeed);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheDirectory);

        var cachePath = Path.Combine(
            cacheDirectory,
            $"ice-remembers-world-v{WorldGenerationVersion}-seed-{baseSeed}.bin");
        string? warning = null;

        reportStage?.Invoke(IceRemembersStartupSeedCacheStage.CheckingCache);
        if (!regenerate && File.Exists(cachePath))
        {
            reportStage?.Invoke(IceRemembersStartupSeedCacheStage.ReadingCache);
            try
            {
                using var stream = OpenRead(cachePath);
                var startupSeed = IceRemembersStartupSeedCacheSerializer.Read(
                    stream,
                    WorldGenerationVersion,
                    baseSeed);
                return new IceRemembersStartupSeedCacheResult(startupSeed, true, cachePath, null);
            }
            catch (Exception exception) when (IsRecoverableCacheFailure(exception))
            {
                warning = $"Ignored unreadable startup cache '{cachePath}': {exception.Message}";
            }
        }

        reportStage?.Invoke(IceRemembersStartupSeedCacheStage.GeneratingWorld);
        var generated = IceRemembersKernelDomainModule.Instance.BuildStartupSeed(
            new DomainStartupSeedRequest(baseSeed));

        reportStage?.Invoke(IceRemembersStartupSeedCacheStage.WritingCache);
        try
        {
            WriteAtomically(cachePath, generated, baseSeed);
        }
        catch (Exception exception) when (IsRecoverableCacheFailure(exception))
        {
            var writeWarning = $"Could not write startup cache '{cachePath}': {exception.Message}";
            warning = warning is null ? writeWarning : $"{warning} {writeWarning}";
        }

        return new IceRemembersStartupSeedCacheResult(generated, false, cachePath, warning);
    }

    private static FileStream OpenRead(string path) => new(
        path,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 1024 * 1024,
        FileOptions.SequentialScan);

    private static void WriteAtomically(
        string cachePath,
        DomainStartupSeedResult startupSeed,
        int baseSeed)
    {
        var directory = Path.GetDirectoryName(cachePath) ??
            throw new InvalidOperationException("Startup cache path has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{cachePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1024 * 1024,
                FileOptions.SequentialScan))
            {
                IceRemembersStartupSeedCacheSerializer.Write(
                    stream,
                    WorldGenerationVersion,
                    baseSeed,
                    startupSeed);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, cachePath, overwrite: true);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _ = exception;
        }
    }

    private static bool IsRecoverableCacheFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or InvalidDataException;
}
