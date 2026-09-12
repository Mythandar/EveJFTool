using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EveJFTool.App.ViewModels;

namespace EveJFTool.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void RouteSystem_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem item) return;
        item.IsSelected = true;
        var action = new MenuItem { Header = "Find systems in jump range…", DataContext = item.DataContext };
        action.Click += FindNearby_Click;
        item.ContextMenu = new ContextMenu { Items = { action } };
    }

    private void FindNearby_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel main) return;
        var source = (sender as MenuItem)?.DataContext as RouteSystemViewModel ?? main.SelectedSystem;
        if (source is null)
        {
            MessageBox.Show(this, "Select a route system first, then choose In Range.", "Find next jump system");
            return;
        }
        var model = new NearbySystemsViewModel
        {
            Heading = $"From {source.Name} · {main.SelectedShip.Name} · up to {main.MaximumRangeText}"
        };
        var dialog = new NearbySystemsWindow { Owner = this, DataContext = model };
        dialog.Loaded += async (_, _) =>
        {
            try { model.SetResults(await main.FindNearbySystemsAsync(source)); }
            catch (Exception exception) { model.Status = $"Unable to find systems: {exception.Message}"; }
        };
        if (dialog.ShowDialog() == true && model.Selected is { } candidate)
            main.InsertNearbySystem(source, candidate);
    }

    public void ApplyPersistedWindowState()
    {
        if (DataContext is not MainViewModel viewModel) return;
        Width = Math.Max(MinWidth, viewModel.WindowWidth);
        Height = Math.Max(MinHeight, viewModel.WindowHeight);
        if (viewModel.WindowMaximized) WindowState = WindowState.Maximized;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            var bounds = RestoreBounds;
            var maximized = WindowState == WindowState.Maximized;
            Task.Run(() => viewModel.RememberWindowAsync(bounds.Width, bounds.Height, maximized))
                .GetAwaiter().GetResult();
        }

        base.OnClosing(e);
    }
}
