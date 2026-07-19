namespace IceRemembers.Seed;

using Engine.Host;

internal sealed record IceRemembersPregeneratedWorldInfo(
    int BaseSeed,
    string Path,
    long SizeBytes);

internal static class IceRemembersPregeneratedWorldStore
{
    private const string WorldExtension = ".world";

    public static IReadOnlyList<IceRemembersPregeneratedWorldInfo> Discover(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var worlds = new List<IceRemembersPregeneratedWorldInfo>();
        foreach (var path in Directory.EnumerateFiles(directory, $"*{WorldExtension}", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var world = ReadInfo(path);
                if (world is not null)
                {
                    worlds.Add(world);
                }
            }
            catch (Exception exception) when (IsRecoverableWorldFailure(exception))
            {
                _ = exception;
            }
        }

        return worlds
            .OrderBy(static world => world.BaseSeed)
            .ThenBy(static world => world.Path, StringComparer.Ordinal)
            .ToArray();
    }

    public static DomainStartupSeedResult Read(IceRemembersPregeneratedWorldInfo world)
    {
        ArgumentNullException.ThrowIfNull(world);
        using (var stream = OpenRead(world.Path))
        {
            return IceRemembersStartupSeedCacheSerializer.Read(
                stream,
                IceRemembersWorldGenerationVersion.Current,
                world.BaseSeed);
        }
    }

    public static void Write(string path, DomainStartupSeedResult startupSeed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(startupSeed);
        if (!string.Equals(Path.GetExtension(path), WorldExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Pregenerated world files must use the .world extension.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ??
            throw new InvalidOperationException("Pregenerated world path has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{fullPath}.{Guid.NewGuid():N}.tmp";
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
                    IceRemembersWorldGenerationVersion.Current,
                    startupSeed.Identity.BaseSeed,
                    startupSeed);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private static FileStream OpenRead(string path) => new(
        path,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 1024 * 1024,
        FileOptions.SequentialScan);

    private static IceRemembersPregeneratedWorldInfo? ReadInfo(string path)
    {
        using (var stream = OpenRead(path))
        {
            var header = IceRemembersStartupSeedCacheSerializer.ReadHeader(stream);
            return header.WorldGenerationVersion == IceRemembersWorldGenerationVersion.Current
                ? new IceRemembersPregeneratedWorldInfo(
                    header.BaseSeed,
                    Path.GetFullPath(path),
                    stream.Length)
                : null;
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

    private static bool IsRecoverableWorldFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or InvalidDataException;
}
