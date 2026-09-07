using System.Windows;
using System.Net.Http;
using System.IO;
using EveJFTool.App.ViewModels;
using EveJFTool.Data;
using EveJFTool.Data.Market;
using EveJFTool.Data.Persistence;
using EveJFTool.Data.Universe;

namespace EveJFTool.App;

public partial class App : Application
{
    private HttpClient? _httpClient;
    private FileLogger? _logger;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var paths = new AppPaths();
        paths.EnsureDirectories();
        _logger = new FileLogger(paths.LogFile);
        _logger.Info("EveJFTool starting.");

        DispatcherUnhandledException += (_, args) =>
        {
            _logger.Error("Unhandled UI exception.", args.Exception);
            MessageBox.Show(
                $"EveJFTool encountered an error. Details were written to:\n{paths.LogFile}\n\n{args.Exception.Message}",
                "EveJFTool error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("EveJFTool/1.0 (desktop route calculator)");

        var packagedUniverse = Path.Combine(AppContext.BaseDirectory, "Resources", "mapSolarSystems.jsonl");
        var universe = new SdeUniverseRepository(packagedUniverse, paths.UniverseFile, _logger);
        var market = new EsiMarketPriceProvider(_httpClient, paths.MarketCacheFile, _logger);
        var stores = new JsonStores(paths.SettingsFile, paths.RoutesFile, _logger);
        var viewModel = new MainViewModel(universe, market, stores, _logger);

        var window = new MainWindow { DataContext = viewModel };
        MainWindow = window;
        window.Show();
        await viewModel.InitializeAsync();
        window.ApplyPersistedWindowState();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Info("EveJFTool exiting.");
        _httpClient?.Dispose();
        base.OnExit(e);
    }
}
