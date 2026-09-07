namespace EveJFTool.App.ViewModels;

public sealed class RouteSystemViewModel(string name) : ObservableObject
{
    private string _name = name;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}
