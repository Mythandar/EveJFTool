namespace EveJFTool.Data;

public sealed class AppPaths
{
    public AppPaths(string? root = null)
    {
        Root = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EveJFTool");
    }

    public string Root { get; }
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
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(UniverseDirectory);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(LogDirectory);
    }
}
