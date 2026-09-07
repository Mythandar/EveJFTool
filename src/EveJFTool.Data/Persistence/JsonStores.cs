using System.Text.Json;
using System.Text.Json.Serialization;
using EveJFTool.Core.Interfaces;

namespace EveJFTool.Data.Persistence;

public sealed class JsonStores(string settingsPath, string routesPath, IAppLogger logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<UserSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(settingsPath))
        {
            return new UserSettings();
        }

        try
        {
            await using var stream = File.OpenRead(settingsPath);
            return await JsonSerializer.DeserializeAsync<UserSettings>(stream, JsonOptions, cancellationToken)
                   ?? new UserSettings();
        }
        catch (Exception exception)
        {
            logger.Error("Could not load settings; defaults will be used.", exception);
            return new UserSettings();
        }
    }

    public async Task SaveSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        await WriteAtomicallyAsync(settingsPath, settings, cancellationToken);
    }

    public async Task<IReadOnlyList<SavedRoute>> LoadRoutesAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(routesPath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(routesPath);
            return await JsonSerializer.DeserializeAsync<List<SavedRoute>>(stream, JsonOptions, cancellationToken)
                   ?? [];
        }
        catch (Exception exception)
        {
            logger.Error("Could not load saved routes.", exception);
            return [];
        }
    }

    public async Task SaveRoutesAsync(IEnumerable<SavedRoute> routes, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(routesPath)!);
        await WriteAtomicallyAsync(routesPath, routes.OrderBy(route => route.Name).ToArray(), cancellationToken);
    }

    private static async Task WriteAtomicallyAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var temporaryPath = path + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
        }

        File.Move(temporaryPath, path, true);
    }
}
