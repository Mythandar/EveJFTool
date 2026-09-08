using EveJFTool.Core.Interfaces;

namespace EveJFTool.Data;

public sealed class AppPaths
{
    private readonly string? _fallbackRoot;
    private readonly string? _legacyRoot;

    public AppPaths(string? root = null, string? legacyRoot = null)
    {
        var localAppDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EveJFTool");

        if (root is null)
        {
            Root = Path.Combine(AppContext.BaseDirectory, "Data");
            _fallbackRoot = localAppDataRoot;
            _legacyRoot = legacyRoot ?? localAppDataRoot;
        }
        else
        {
            Root = Path.GetFullPath(root);
            _legacyRoot = legacyRoot is null ? null : Path.GetFullPath(legacyRoot);
        }
    }

    public string Root { get; private set; }
    public bool FellBackToUserProfile { get; private set; }
    public string SettingsFile => Path.Combine(Root, "settings.json");
    public string RoutesFile => Path.Combine(Root, "routes.json");
    public string UniverseDirectory => Path.Combine(Root, "universe");
    public string UniverseFile => Path.Combine(UniverseDirectory, "mapSolarSystems.jsonl");
    public string CacheDirectory => Path.Combine(Root, "cache");
    public string MarketCacheFile => Path.Combine(CacheDirectory, "market-prices.json");
    public string LogDirectory => Path.Combine(Root, "logs");
    public string LogFile => Path.Combine(LogDirectory, "EveJFTool.log");

    public void EnsureDirectories()
    {
        try
        {
            CreateDirectories();
        }
        catch (Exception exception) when (
            _fallbackRoot is not null &&
            exception is UnauthorizedAccessException or IOException)
        {
            Root = _fallbackRoot;
            FellBackToUserProfile = true;
            CreateDirectories();
        }
    }

    public void MigrateLegacyData(IAppLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        if (_legacyRoot is null || PathsEqual(Root, _legacyRoot) || !Directory.Exists(_legacyRoot))
        {
            return;
        }

        CopyIfMissing("settings.json", logger);
        CopyIfMissing("routes.json", logger);
        CopyIfMissing(Path.Combine("universe", "mapSolarSystems.jsonl"), logger);
        CopyIfMissing(Path.Combine("cache", "market-prices.json"), logger);
    }

    private void CreateDirectories()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(UniverseDirectory);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(LogDirectory);
    }

    private void CopyIfMissing(string relativePath, IAppLogger logger)
    {
        var source = Path.Combine(_legacyRoot!, relativePath);
        var destination = Path.Combine(Root, relativePath);
        if (!File.Exists(source) || File.Exists(destination))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: false);
            logger.Info($"Migrated legacy data file to portable storage: {relativePath}");
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            logger.Error($"Could not migrate legacy data file: {relativePath}", exception);
        }
    }

    private static bool PathsEqual(string first, string second) =>
        string.Equals(
            Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
}
