using EveJFTool.Core.Interfaces;
using EveJFTool.Data.Persistence;
using EveJFTool.Data.Universe;

namespace EveJFTool.Tests;

public sealed class DataTests
{
    [Fact]
    public async Task BundledSde_ResolvesSystemsCaseInsensitively()
    {
        var packaged = Path.Combine(AppContext.BaseDirectory, "Resources", "mapSolarSystems.jsonl");
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), "EveJFTool.Tests", Guid.NewGuid().ToString("N"));
        var cached = Path.Combine(temporaryDirectory, "mapSolarSystems.jsonl");
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            var repository = new SdeUniverseRepository(packaged, cached, new NullLogger());
            await repository.InitializeAsync();

            Assert.True(repository.TryGetSystem("jItA", out var jita));
            Assert.Equal(30000142, jita!.Id);
            Assert.True(repository.SystemNames.Count > 5_000);
            Assert.True(File.Exists(cached));
        }
        finally
        {
            Directory.Delete(temporaryDirectory, true);
        }
    }

    [Fact]
    public async Task JsonStores_RoundTripSettingsAndPerLegRouteConfiguration()
    {
        var directory = Path.Combine(Path.GetTempPath(), "EveJFTool.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var stores = new JsonStores(Path.Combine(directory, "settings.json"), Path.Combine(directory, "routes.json"), new NullLogger());
            var settings = new UserSettings { SelectedShip = "Nomad", JumpDriveCalibration = 4 };
            settings.ManualPrices[17889] = 987.65m;
            var route = new SavedRoute
            {
                Name = "Staging to Jita",
                Systems = ["O-LJOO", "Ihakana", "Jita"],
                Legs =
                [
                    new() { From = "O-LJOO", To = "Ihakana", Kind = EveJFTool.Core.Models.LegKind.Jump, EconomizerTypeIds = [34126, 34126, 34124] },
                    new() { From = "Ihakana", To = "Jita", Kind = EveJFTool.Core.Models.LegKind.Gate }
                ]
            };

            await stores.SaveSettingsAsync(settings);
            await stores.SaveRoutesAsync([route]);

            var loadedSettings = await stores.LoadSettingsAsync();
            var loadedRoute = Assert.Single(await stores.LoadRoutesAsync());
            Assert.Equal("Nomad", loadedSettings.SelectedShip);
            Assert.Equal(987.65m, loadedSettings.ManualPrices[17889]);
            Assert.Equal(EveJFTool.Core.Models.LegKind.Gate, loadedRoute.Legs[1].Kind);
            Assert.Equal([34126, 34126, 34124], loadedRoute.Legs[0].EconomizerTypeIds);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private sealed class NullLogger : IAppLogger
    {
        public void Info(string message) { }
        public void Error(string message, Exception? exception = null) { }
    }
}
