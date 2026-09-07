using System.Text.Json;
using EveJFTool.Core.Interfaces;
using EveJFTool.Core.Models;

namespace EveJFTool.Data.Universe;

public sealed class SdeUniverseRepository(
    string packagedDataPath,
    string cachedDataPath,
    IAppLogger logger) : IUniverseRepository
{
    private readonly Dictionary<string, SolarSystem> _systems = new(StringComparer.OrdinalIgnoreCase);
    private string[] _systemNames = [];

    public IReadOnlyList<string> SystemNames => _systemNames;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        logger.Info("Loading CCP SDE solar-system data.");
        var cacheDirectory = Path.GetDirectoryName(cachedDataPath)!;
        Directory.CreateDirectory(cacheDirectory);

        if (!File.Exists(cachedDataPath) ||
            (File.Exists(packagedDataPath) && File.GetLastWriteTimeUtc(packagedDataPath) > File.GetLastWriteTimeUtc(cachedDataPath)))
        {
            File.Copy(packagedDataPath, cachedDataPath, true);
        }

        await using var stream = File.OpenRead(cachedDataPath);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            var item = JsonSerializer.Deserialize<SdeSystem>(line);
            if (item?.Name?.English is not { Length: > 0 } name || item.Position is null)
            {
                continue;
            }

            // New Eden cluster IDs. Includes normal space and Pochven; jump validity is separately validated.
            if (item.Id is < 30_000_000 or > 30_999_999)
            {
                continue;
            }

            _systems[name] = new SolarSystem(
                item.Id,
                name,
                item.Position.X,
                item.Position.Y,
                item.Position.Z,
                item.SecurityStatus,
                item.RegionId);
        }

        _systemNames = _systems.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
        logger.Info($"Loaded {_systems.Count:N0} solar systems from the local SDE cache.");
    }

    public bool TryGetSystem(string name, out SolarSystem? system)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            system = null;
            return false;
        }

        return _systems.TryGetValue(name.Trim(), out system);
    }

    private sealed class SdeSystem
    {
        [System.Text.Json.Serialization.JsonPropertyName("_key")]
        public int Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public LocalizedName? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("position")]
        public Position? Position { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("securityStatus")]
        public double SecurityStatus { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("regionID")]
        public int RegionId { get; set; }
    }

    private sealed class LocalizedName
    {
        [System.Text.Json.Serialization.JsonPropertyName("en")]
        public string? English { get; set; }
    }

    private sealed class Position
    {
        [System.Text.Json.Serialization.JsonPropertyName("x")]
        public double X { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("y")]
        public double Y { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("z")]
        public double Z { get; set; }
    }
}
