using EveJFTool.Core.Models;

namespace EveJFTool.App.ViewModels;

public sealed class RouteLegViewModel : ObservableObject
{
    private LegKind _kind;
    private EconomizerModuleOption _economizer1;
    private EconomizerModuleOption _economizer2;
    private EconomizerModuleOption _economizer3;
    private RouteLegResult? _result;

    public RouteLegViewModel(string from, string to, LegKind kind, EconomizerLoadout loadout)
    {
        From = from;
        To = to;
        _kind = kind;

        var selected = OptionsFor(loadout);
        _economizer1 = selected[0];
        _economizer2 = selected[1];
        _economizer3 = selected[2];
    }

    public event EventHandler? ConfigurationChanged;

    public string From { get; }
    public string To { get; }
    public IReadOnlyList<LegKind> LegKinds { get; } = Enum.GetValues<LegKind>();
    public IReadOnlyList<EconomizerModuleOption> EconomizerTypes => EconomizerModuleOption.All;

    public LegKind Kind
    {
        get => _kind;
        set
        {
            if (SetProperty(ref _kind, value))
            {
                OnPropertyChanged(nameof(IsJump));
                ConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool IsJump => Kind == LegKind.Jump;

    public EconomizerModuleOption Economizer1
    {
        get => _economizer1;
        set => SetEconomizer(ref _economizer1, value, nameof(Economizer1));
    }

    public EconomizerModuleOption Economizer2
    {
        get => _economizer2;
        set => SetEconomizer(ref _economizer2, value, nameof(Economizer2));
    }

    public EconomizerModuleOption Economizer3
    {
        get => _economizer3;
        set => SetEconomizer(ref _economizer3, value, nameof(Economizer3));
    }

    public EconomizerLoadout EconomizerLoadout => new(
        new[] { Economizer1.Module, Economizer2.Module, Economizer3.Module }
            .OfType<EconomizerDefinition>()
            .ToArray());

    public RouteLegRequest ToRequest() => new(From, To, Kind, EconomizerLoadout);

    public void SetEconomizerLoadout(EconomizerLoadout loadout)
    {
        var selected = OptionsFor(loadout);
        _economizer1 = selected[0];
        _economizer2 = selected[1];
        _economizer3 = selected[2];
        OnPropertyChanged(nameof(Economizer1));
        OnPropertyChanged(nameof(Economizer2));
        OnPropertyChanged(nameof(Economizer3));
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetResult(RouteLegResult result)
    {
        _result = result;
        OnPropertyChanged(nameof(Distance));
        OnPropertyChanged(nameof(FuelUsed));
        OnPropertyChanged(nameof(FuelType));
        OnPropertyChanged(nameof(FuelPrice));
        OnPropertyChanged(nameof(IskCost));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsValid));
    }

    public string Distance => Kind == LegKind.Gate ? "—" : _result is null ? "—" : $"{_result.DistanceLightYears:N3}";
    public string FuelUsed => _result is null ? "—" : $"{_result.FuelUsed:N0}";
    public string FuelType => _result?.FuelType ?? "—";
    public string FuelPrice => _result?.FuelPrice is { } value ? $"{value:N2}" : "—";
    public string IskCost => _result?.IskCost is { } value ? $"{value:N0}" : "—";
    public string Status => _result?.Status ?? "Pending";
    public bool IsValid => _result?.IsValid ?? true;

    private static EconomizerModuleOption[] OptionsFor(EconomizerLoadout loadout) =>
        loadout.Modules
            .OrderByDescending(module => module.FuelReductionFraction)
            .Select(EconomizerModuleOption.FromModule)
            .Concat(Enumerable.Repeat(EconomizerModuleOption.All[0], 3))
            .Take(3)
            .ToArray();

    private void SetEconomizer(
        ref EconomizerModuleOption field,
        EconomizerModuleOption value,
        string propertyName)
    {
        if (value is not null && SetProperty(ref field, value, propertyName))
        {
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
