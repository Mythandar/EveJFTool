using EveJFTool.Core.Models;

namespace EveJFTool.App.ViewModels;

public sealed class RouteLegViewModel : ObservableObject
{
    private LegKind _kind;
    private EconomizerOption _selectedEconomizer;
    private RouteLegResult? _result;

    public RouteLegViewModel(
        string from,
        string to,
        LegKind kind,
        EconomizerOption selectedEconomizer,
        IReadOnlyList<EconomizerOption> economizerOptions)
    {
        From = from;
        To = to;
        _kind = kind;
        _selectedEconomizer = selectedEconomizer;
        EconomizerOptions = economizerOptions;
    }

    public event EventHandler? ConfigurationChanged;

    public string From { get; }
    public string To { get; }
    public IReadOnlyList<LegKind> LegKinds { get; } = Enum.GetValues<LegKind>();
    public IReadOnlyList<EconomizerOption> EconomizerOptions { get; }

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

    public EconomizerOption SelectedEconomizer
    {
        get => _selectedEconomizer;
        set
        {
            if (value is not null && SetProperty(ref _selectedEconomizer, value))
            {
                ConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public RouteLegRequest ToRequest() => new(From, To, Kind, SelectedEconomizer.Loadout);

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
}
