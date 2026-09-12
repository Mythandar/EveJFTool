using EveJFTool.Core.Services;

namespace EveJFTool.App.ViewModels;

public sealed class NearbySystemsViewModel : ObservableObject
{
    private IReadOnlyList<NearbySystem> _all = [];
    private IReadOnlyList<NearbySystem> _matches = [];
    private string _filter = string.Empty;
    private string _status = "Finding systems…";
    private NearbySystem? _selected;

    public required string Heading { get; init; }
    public IReadOnlyList<NearbySystem> Matches { get => _matches; private set => SetProperty(ref _matches, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public NearbySystem? Selected { get => _selected; set => SetProperty(ref _selected, value); }
    public string Filter
    {
        get => _filter;
        set { if (SetProperty(ref _filter, value)) ApplyFilter(); }
    }

    public void SetResults(IReadOnlyList<NearbySystem> systems)
    {
        _all = systems;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Matches = _all.Where(system => system.Name.Contains(Filter.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
        if (Selected is not null && !Matches.Contains(Selected)) Selected = null;
        Status = $"{Matches.Count:N0} of {_all.Count:N0} candidates · nearest first";
    }
}
