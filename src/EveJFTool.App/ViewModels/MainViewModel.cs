using System.Collections.ObjectModel;
using EveJFTool.Core.Catalogs;
using EveJFTool.Core.Interfaces;
using EveJFTool.Core.Models;
using EveJFTool.Core.Services;
using EveJFTool.Data.Persistence;

namespace EveJFTool.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IUniverseRepository _universe;
    private readonly IMarketPriceProvider _market;
    private readonly JsonStores _stores;
    private readonly IAppLogger _logger;
    private readonly RouteCalculator _calculator;
    private readonly Dictionary<int, MarketPrice> _prices = [];
    private UserSettings _settings = new();
    private bool _initialized;
    private string? _loadedRouteName;

    private ShipDefinition _selectedShip = JumpFreighterCatalog.All[0];
    private int _jumpDriveCalibration = 5;
    private int _jumpFuelConservation = 5;
    private int _jumpFreighters = 5;
    private PriceMode _selectedPriceMode = PriceMode.JitaSell;
    private decimal? _manualPrice;
    private string _newSystemName = string.Empty;
    private IReadOnlyList<string> _systemSuggestions = [];
    private string? _selectedSystemSuggestion;
    private bool _isSystemSuggestionOpen;
    private RouteSystemViewModel? _selectedSystem;
    private SavedRoute? _selectedSavedRoute;
    private string _routeName = string.Empty;
    private string _statusMessage = "Loading universe data…";
    private bool _isBusy;
    private string _priceValueText = "Unavailable";
    private string _priceSourceText = "No price loaded";
    private string _priceUpdatedText = string.Empty;
    private string _jumpLegsText = "0";
    private string _gateLegsText = "0";
    private string _totalDistanceText = "0.00 LY";
    private string _totalFuelText = "0";
    private string _totalCostText = "0 ISK";

    public MainViewModel(
        IUniverseRepository universe,
        IMarketPriceProvider market,
        JsonStores stores,
        IAppLogger logger)
    {
        _universe = universe;
        _market = market;
        _stores = stores;
        _logger = logger;
        _calculator = new RouteCalculator(universe);

        AddSystemCommand = new RelayCommand(AddSystem, () => !string.IsNullOrWhiteSpace(NewSystemName));
        InsertSystemCommand = new RelayCommand(InsertSystem, () => SelectedSystem is not null && !string.IsNullOrWhiteSpace(NewSystemName));
        RemoveSystemCommand = new RelayCommand(RemoveSystem, () => SelectedSystem is not null);
        MoveSystemUpCommand = new RelayCommand(() => MoveSelectedSystem(-1), () => SelectedSystem is not null && Systems.IndexOf(SelectedSystem) > 0);
        MoveSystemDownCommand = new RelayCommand(() => MoveSelectedSystem(1), () => SelectedSystem is not null && Systems.IndexOf(SelectedSystem) < Systems.Count - 1);
        AddReturnTripCommand = new RelayCommand(AddReturnTrip, () => Legs.Count > 0);
        ClearRouteCommand = new RelayCommand(ClearRoute, () => Systems.Count > 0);
        NewRouteCommand = new RelayCommand(NewRoute);
        RefreshPricesCommand = new AsyncRelayCommand(RefreshPricesAsync, () => SelectedPriceMode != PriceMode.Manual && !IsBusy);
        SaveRouteCommand = new AsyncRelayCommand(() => SaveRouteAsync(false), () => Systems.Count >= 2);
        SaveAsRouteCommand = new AsyncRelayCommand(() => SaveRouteAsync(true), () => Systems.Count >= 2);
        LoadRouteCommand = new RelayCommand(LoadRoute, () => SelectedSavedRoute is not null);
        DeleteRouteCommand = new AsyncRelayCommand(DeleteRouteAsync, () => SelectedSavedRoute is not null);
    }

    public IReadOnlyList<ShipDefinition> Ships => JumpFreighterCatalog.All;
    public IReadOnlyList<int> SkillLevels { get; } = [0, 1, 2, 3, 4, 5];
    public IReadOnlyList<PriceMode> PriceModes { get; } = Enum.GetValues<PriceMode>();
    public IReadOnlyList<string> SystemNames { get; private set; } = [];
    public ObservableCollection<RouteSystemViewModel> Systems { get; } = [];
    public ObservableCollection<RouteLegViewModel> Legs { get; } = [];
    public ObservableCollection<SavedRoute> SavedRoutes { get; } = [];

    public RelayCommand AddSystemCommand { get; }
    public RelayCommand InsertSystemCommand { get; }
    public RelayCommand RemoveSystemCommand { get; }
    public RelayCommand MoveSystemUpCommand { get; }
    public RelayCommand MoveSystemDownCommand { get; }
    public RelayCommand AddReturnTripCommand { get; }
    public RelayCommand ClearRouteCommand { get; }
    public RelayCommand NewRouteCommand { get; }
    public AsyncRelayCommand RefreshPricesCommand { get; }
    public AsyncRelayCommand SaveRouteCommand { get; }
    public AsyncRelayCommand SaveAsRouteCommand { get; }
    public RelayCommand LoadRouteCommand { get; }
    public AsyncRelayCommand DeleteRouteCommand { get; }

    public ShipDefinition SelectedShip
    {
        get => _selectedShip;
        set
        {
            if (value is not null && SetProperty(ref _selectedShip, value))
            {
                _manualPrice = _settings.ManualPrices.GetValueOrDefault(value.IsotopeTypeId);
                OnPropertyChanged(nameof(ManualPrice));
                OnPropertyChanged(nameof(SelectedFuelType));
                OnPropertyChanged(nameof(MaximumRangeText));
                UpdatePriceDisplay();
                Recalculate();
                PersistSettingsSoon();
            }
        }
    }

    public int JumpDriveCalibration
    {
        get => _jumpDriveCalibration;
        set
        {
            if (SetProperty(ref _jumpDriveCalibration, value))
            {
                OnPropertyChanged(nameof(MaximumRangeText));
                Recalculate();
                PersistSettingsSoon();
            }
        }
    }

    public int JumpFuelConservation
    {
        get => _jumpFuelConservation;
        set
        {
            if (SetProperty(ref _jumpFuelConservation, value))
            {
                Recalculate();
                PersistSettingsSoon();
            }
        }
    }

    public int JumpFreighters
    {
        get => _jumpFreighters;
        set
        {
            if (SetProperty(ref _jumpFreighters, value))
            {
                Recalculate();
                PersistSettingsSoon();
            }
        }
    }

    public PriceMode SelectedPriceMode
    {
        get => _selectedPriceMode;
        set
        {
            if (SetProperty(ref _selectedPriceMode, value))
            {
                OnPropertyChanged(nameof(IsManualPrice));
                RefreshPricesCommand.RaiseCanExecuteChanged();
                UpdatePriceDisplay();
                Recalculate();
                PersistSettingsSoon();
            }
        }
    }

    public bool IsManualPrice => SelectedPriceMode == PriceMode.Manual;

    public decimal? ManualPrice
    {
        get => _manualPrice;
        set
        {
            var sanitized = value is > 0 ? value : null;
            if (SetProperty(ref _manualPrice, sanitized))
            {
                if (sanitized.HasValue)
                {
                    _settings.ManualPrices[SelectedShip.IsotopeTypeId] = sanitized.Value;
                }
                else
                {
                    _settings.ManualPrices.Remove(SelectedShip.IsotopeTypeId);
                }

                UpdatePriceDisplay();
                Recalculate();
                PersistSettingsSoon();
            }
        }
    }

    public string NewSystemName
    {
        get => _newSystemName;
        set
        {
            if (SetProperty(ref _newSystemName, value))
            {
                AddSystemCommand.RaiseCanExecuteChanged();
                InsertSystemCommand.RaiseCanExecuteChanged();
                UpdateSystemSuggestions();
            }
        }
    }

    public IReadOnlyList<string> SystemSuggestions
    {
        get => _systemSuggestions;
        private set => SetProperty(ref _systemSuggestions, value);
    }

    public string? SelectedSystemSuggestion
    {
        get => _selectedSystemSuggestion;
        set
        {
            if (SetProperty(ref _selectedSystemSuggestion, value) && !string.IsNullOrWhiteSpace(value))
            {
                NewSystemName = value;
                IsSystemSuggestionOpen = false;
            }
        }
    }

    public bool IsSystemSuggestionOpen
    {
        get => _isSystemSuggestionOpen;
        set => SetProperty(ref _isSystemSuggestionOpen, value);
    }

    public RouteSystemViewModel? SelectedSystem
    {
        get => _selectedSystem;
        set
        {
            if (SetProperty(ref _selectedSystem, value))
            {
                RaiseRouteCommandStates();
            }
        }
    }

    public SavedRoute? SelectedSavedRoute
    {
        get => _selectedSavedRoute;
        set
        {
            if (SetProperty(ref _selectedSavedRoute, value))
            {
                LoadRouteCommand.RaiseCanExecuteChanged();
                DeleteRouteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string RouteName
    {
        get => _routeName;
        set => SetProperty(ref _routeName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RefreshPricesCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedFuelType => SelectedShip.IsotopeName;
    public string MaximumRangeText => $"{FuelCalculator.MaximumRange(SelectedShip, CurrentSkills):N2} LY max";
    public string PriceValueText { get => _priceValueText; private set => SetProperty(ref _priceValueText, value); }
    public string PriceSourceText { get => _priceSourceText; private set => SetProperty(ref _priceSourceText, value); }
    public string PriceUpdatedText { get => _priceUpdatedText; private set => SetProperty(ref _priceUpdatedText, value); }
    public string JumpLegsText { get => _jumpLegsText; private set => SetProperty(ref _jumpLegsText, value); }
    public string GateLegsText { get => _gateLegsText; private set => SetProperty(ref _gateLegsText, value); }
    public string TotalDistanceText { get => _totalDistanceText; private set => SetProperty(ref _totalDistanceText, value); }
    public string TotalFuelText { get => _totalFuelText; private set => SetProperty(ref _totalFuelText, value); }
    public string TotalCostText { get => _totalCostText; private set => SetProperty(ref _totalCostText, value); }
    public double WindowWidth => _settings.WindowWidth;
    public double WindowHeight => _settings.WindowHeight;
    public bool WindowMaximized => _settings.WindowMaximized;

    private SkillProfile CurrentSkills => new(JumpDriveCalibration, JumpFuelConservation, JumpFreighters);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _universe.InitializeAsync(cancellationToken);
            SystemNames = _universe.SystemNames;
            OnPropertyChanged(nameof(SystemNames));
            UpdateSystemSuggestions();

            _settings = await _stores.LoadSettingsAsync(cancellationToken);
            _selectedShip = JumpFreighterCatalog.All.FirstOrDefault(ship => ship.Name == _settings.SelectedShip) ?? JumpFreighterCatalog.All[0];
            _jumpDriveCalibration = Math.Clamp(_settings.JumpDriveCalibration, 0, 5);
            _jumpFuelConservation = Math.Clamp(_settings.JumpFuelConservation, 0, 5);
            _jumpFreighters = Math.Clamp(_settings.JumpFreighters, 0, 5);
            _selectedPriceMode = _settings.PriceMode;
            _manualPrice = _settings.ManualPrices.GetValueOrDefault(_selectedShip.IsotopeTypeId);
            OnPropertyChanged(string.Empty);

            var routes = await _stores.LoadRoutesAsync(cancellationToken);
            foreach (var route in routes.OrderBy(route => route.Name))
            {
                SavedRoutes.Add(route);
            }

            _initialized = true;
            UpdatePriceDisplay();
            Recalculate();
            StatusMessage = $"Ready — {SystemNames.Count:N0} systems loaded";
            if (SelectedPriceMode != PriceMode.Manual)
            {
                await RefreshPricesAsync();
            }
        }
        catch (Exception exception)
        {
            _logger.Error("Application initialization failed.", exception);
            StatusMessage = $"Startup error: {exception.Message}";
        }
    }

    public async Task RememberWindowAsync(double width, double height, bool maximized)
    {
        if (double.IsFinite(width) && width >= 900) _settings.WindowWidth = width;
        if (double.IsFinite(height) && height >= 600) _settings.WindowHeight = height;
        _settings.WindowMaximized = maximized;
        await SaveSettingsAsync();
    }

    private void AddSystem()
    {
        if (!TryResolveEnteredSystem(out var canonicalName)) return;
        Systems.Add(new RouteSystemViewModel(canonicalName));
        NewSystemName = string.Empty;
        RebuildLegs();
    }

    private void InsertSystem()
    {
        if (SelectedSystem is null || !TryResolveEnteredSystem(out var canonicalName)) return;
        var index = Systems.IndexOf(SelectedSystem);
        Systems.Insert(index, new RouteSystemViewModel(canonicalName));
        NewSystemName = string.Empty;
        RebuildLegs();
    }

    private bool TryResolveEnteredSystem(out string canonicalName)
    {
        if (_universe.TryGetSystem(NewSystemName, out var system))
        {
            canonicalName = system!.Name;
            return true;
        }

        canonicalName = string.Empty;
        StatusMessage = $"Unknown solar system: {NewSystemName.Trim()}";
        return false;
    }

    private void UpdateSystemSuggestions()
    {
        var query = NewSystemName.Trim();
        if (query.Length < 2 || SystemNames.Count == 0)
        {
            SystemSuggestions = [];
            IsSystemSuggestionOpen = false;
            return;
        }

        SystemSuggestions = SystemNames
            .Where(name => name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(name => name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        IsSystemSuggestionOpen =
            SystemSuggestions.Count > 0 &&
            !_universe.TryGetSystem(query, out _);
    }

    private void RemoveSystem()
    {
        if (SelectedSystem is null) return;
        var index = Systems.IndexOf(SelectedSystem);
        Systems.Remove(SelectedSystem);
        SelectedSystem = Systems.Count == 0 ? null : Systems[Math.Min(index, Systems.Count - 1)];
        RebuildLegs();
    }

    private void MoveSelectedSystem(int offset)
    {
        if (SelectedSystem is null) return;
        var oldIndex = Systems.IndexOf(SelectedSystem);
        var newIndex = oldIndex + offset;
        if (newIndex < 0 || newIndex >= Systems.Count) return;
        Systems.Move(oldIndex, newIndex);
        RebuildLegs();
        RaiseRouteCommandStates();
    }

    private void AddReturnTrip()
    {
        var returnLegs = ReturnTripBuilder.CreateReturnLegs(
            Legs.Select(leg => leg.ToRequest()).ToArray());

        foreach (var returnLeg in returnLegs)
        {
            Systems.Add(new RouteSystemViewModel(returnLeg.ToSystem));
            var leg = new RouteLegViewModel(
                returnLeg.FromSystem,
                returnLeg.ToSystem,
                returnLeg.Kind,
                returnLeg.Economizers);
            leg.ConfigurationChanged += OnLegConfigurationChanged;
            Legs.Add(leg);
        }

        SelectedSystem = Systems.LastOrDefault();
        Recalculate();
        RaiseRouteCommandStates();
        StatusMessage = $"Added return trip ({returnLegs.Count:N0} legs).";
    }

    private void ClearRoute()
    {
        Systems.Clear();
        Legs.Clear();
        SelectedSystem = null;
        Recalculate();
        RaiseRouteCommandStates();
    }

    private void NewRoute()
    {
        ClearRoute();
        RouteName = string.Empty;
        _loadedRouteName = null;
        SelectedSavedRoute = null;
        StatusMessage = "New route";
    }

    private void RebuildLegs()
    {
        var prior = Legs
            .GroupBy(leg => (leg.From, leg.To))
            .ToDictionary(group => group.Key, group => group.Last());
        Legs.Clear();
        for (var index = 0; index < Systems.Count - 1; index++)
        {
            var from = Systems[index].Name;
            var to = Systems[index + 1].Name;
            var leg = prior.TryGetValue((from, to), out var existing)
                ? new RouteLegViewModel(from, to, existing.Kind, existing.EconomizerLoadout)
                : new RouteLegViewModel(from, to, LegKind.Jump, EconomizerLoadout.None);
            leg.ConfigurationChanged += OnLegConfigurationChanged;
            Legs.Add(leg);
        }

        Recalculate();
        RaiseRouteCommandStates();
    }

    private void OnLegConfigurationChanged(object? sender, EventArgs eventArgs) => Recalculate();

    private void Recalculate()
    {
        if (!_initialized) return;
        try
        {
            var calculation = _calculator.Calculate(Legs.Select(leg => leg.ToRequest()).ToArray(), SelectedShip, CurrentSkills, CurrentPrice());
            for (var index = 0; index < Legs.Count; index++)
            {
                Legs[index].SetResult(calculation.Legs[index]);
            }

            JumpLegsText = calculation.Totals.JumpLegs.ToString("N0");
            GateLegsText = calculation.Totals.GateLegs.ToString("N0");
            TotalDistanceText = $"{calculation.Totals.TotalJumpDistanceLightYears:N2} LY";
            TotalFuelText = calculation.Totals.FuelByType.Count == 0
                ? "0"
                : string.Join(" · ", calculation.Totals.FuelByType.Select(pair => $"{pair.Key}: {pair.Value:N0}"));
            TotalCostText = $"{calculation.Totals.TotalIskCost:N0} ISK" +
                            (calculation.Totals.HasCompletePricing ? string.Empty : " (price incomplete)");

            var invalidCount = calculation.Legs.Count(leg => !leg.IsValid);
            if (invalidCount > 0)
            {
                StatusMessage = $"Route has {invalidCount} invalid leg{(invalidCount == 1 ? string.Empty : "s")}.";
            }
        }
        catch (Exception exception)
        {
            _logger.Error("Route calculation failed.", exception);
            StatusMessage = $"Calculation error: {exception.Message}";
        }
    }

    private decimal? CurrentPrice()
    {
        if (SelectedPriceMode == PriceMode.Manual)
        {
            return ManualPrice;
        }

        return _prices.GetValueOrDefault(SelectedShip.IsotopeTypeId)?.Price;
    }

    private void UpdatePriceDisplay()
    {
        if (SelectedPriceMode == PriceMode.Manual)
        {
            PriceValueText = ManualPrice is { } value ? $"{value:N2} ISK/unit" : "Enter a manual price";
            PriceSourceText = "Manual price";
            PriceUpdatedText = "Available offline";
            return;
        }

        var price = _prices.GetValueOrDefault(SelectedShip.IsotopeTypeId);
        if (price is null)
        {
            PriceValueText = "Unavailable";
            PriceSourceText = SelectedPriceMode == PriceMode.JitaSell ? "Jita sell" : "Jita buy";
            PriceUpdatedText = "Refresh to retrieve CCP ESI orders";
            return;
        }

        PriceValueText = $"{price.Price:N2} ISK/unit";
        PriceSourceText = price.Source + (price.IsStale ? " — STALE" : price.IsCached ? " — cached" : string.Empty);
        PriceUpdatedText = $"Updated {price.UpdatedAt.ToLocalTime():g}";
    }

    private async Task RefreshPricesAsync()
    {
        IsBusy = true;
        StatusMessage = "Refreshing Jita isotope prices from CCP ESI…";
        try
        {
            var prices = await _market.GetPricesAsync(Ships, SelectedPriceMode);
            _prices.Clear();
            foreach (var pair in prices) _prices[pair.Key] = pair.Value;
            UpdatePriceDisplay();
            Recalculate();
            StatusMessage = prices.Count > 0
                ? "Prices updated"
                : "Price API unavailable and no matching cached price exists; use Manual price.";
        }
        catch (Exception exception)
        {
            _logger.Error("Price refresh failed in the view model.", exception);
            StatusMessage = $"Price update failed: {exception.Message}. Manual pricing remains available.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveRouteAsync(bool saveAs)
    {
        var name = RouteName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Enter a route name before saving.";
            return;
        }

        if (saveAs && SavedRoutes.Any(route => string.Equals(route.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            var baseName = name + " Copy";
            name = baseName;
            var suffix = 2;
            while (SavedRoutes.Any(route => string.Equals(route.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                name = $"{baseName} {suffix++}";
            }
            RouteName = name;
        }

        var route = new SavedRoute
        {
            Name = name,
            Systems = Systems.Select(system => system.Name).ToList(),
            Legs = Legs.Select(leg => new SavedLeg
            {
                From = leg.From,
                To = leg.To,
                Kind = leg.Kind,
                EconomizerTypeIds = leg.EconomizerLoadout.Modules.Select(module => module.TypeId).ToList()
            }).ToList(),
            ShipName = SelectedShip.Name,
            JumpDriveCalibration = JumpDriveCalibration,
            JumpFuelConservation = JumpFuelConservation,
            JumpFreighters = JumpFreighters,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var existing = SavedRoutes.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) SavedRoutes.Remove(existing);
        SavedRoutes.Add(route);
        SortSavedRoutes();
        SelectedSavedRoute = route;
        _loadedRouteName = name;
        await _stores.SaveRoutesAsync(SavedRoutes);
        StatusMessage = $"Saved route “{name}”";
    }

    private void LoadRoute()
    {
        if (SelectedSavedRoute is null) return;
        var route = SelectedSavedRoute;
        Systems.Clear();
        foreach (var name in route.Systems) Systems.Add(new RouteSystemViewModel(name));
        RebuildLegs();

        for (var index = 0; index < Math.Min(Legs.Count, route.Legs.Count); index++)
        {
            var saved = route.Legs[index];
            Legs[index].Kind = saved.Kind;
            var economizers = saved.EconomizerTypeIds
                .Select(typeId => EconomizerCatalog.All.FirstOrDefault(module => module.TypeId == typeId))
                .OfType<EconomizerDefinition>()
                .Take(3)
                .ToArray();
            Legs[index].SetEconomizerLoadout(new EconomizerLoadout(economizers));
        }

        var ship = Ships.FirstOrDefault(item => item.Name == route.ShipName);
        if (ship is not null) SelectedShip = ship;
        if (route.JumpDriveCalibration is { } jdc) JumpDriveCalibration = Math.Clamp(jdc, 0, 5);
        if (route.JumpFuelConservation is { } jfc) JumpFuelConservation = Math.Clamp(jfc, 0, 5);
        if (route.JumpFreighters is { } jf) JumpFreighters = Math.Clamp(jf, 0, 5);
        RouteName = route.Name;
        _loadedRouteName = route.Name;
        Recalculate();
        StatusMessage = $"Loaded route “{route.Name}”";
    }

    private async Task DeleteRouteAsync()
    {
        if (SelectedSavedRoute is null) return;
        var name = SelectedSavedRoute.Name;
        SavedRoutes.Remove(SelectedSavedRoute);
        SelectedSavedRoute = null;
        if (string.Equals(_loadedRouteName, name, StringComparison.OrdinalIgnoreCase)) _loadedRouteName = null;
        await _stores.SaveRoutesAsync(SavedRoutes);
        StatusMessage = $"Deleted route “{name}”";
    }

    private void SortSavedRoutes()
    {
        var sorted = SavedRoutes.OrderBy(route => route.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        SavedRoutes.Clear();
        foreach (var route in sorted) SavedRoutes.Add(route);
    }

    private void PersistSettingsSoon()
    {
        if (_initialized) _ = SaveSettingsAsync();
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            _settings.SelectedShip = SelectedShip.Name;
            _settings.JumpDriveCalibration = JumpDriveCalibration;
            _settings.JumpFuelConservation = JumpFuelConservation;
            _settings.JumpFreighters = JumpFreighters;
            _settings.PriceMode = SelectedPriceMode;
            await _stores.SaveSettingsAsync(_settings);
        }
        catch (Exception exception)
        {
            _logger.Error("Could not save user settings.", exception);
        }
    }

    private void RaiseRouteCommandStates()
    {
        InsertSystemCommand.RaiseCanExecuteChanged();
        RemoveSystemCommand.RaiseCanExecuteChanged();
        MoveSystemUpCommand.RaiseCanExecuteChanged();
        MoveSystemDownCommand.RaiseCanExecuteChanged();
        AddReturnTripCommand.RaiseCanExecuteChanged();
        ClearRouteCommand.RaiseCanExecuteChanged();
        SaveRouteCommand.RaiseCanExecuteChanged();
        SaveAsRouteCommand.RaiseCanExecuteChanged();
    }
}
