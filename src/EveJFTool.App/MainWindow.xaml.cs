using System.Windows;
using EveJFTool.App.ViewModels;

namespace EveJFTool.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
            Task.Run(() => viewModel.RememberWindowAsync(bounds.Width, bounds.Height, WindowState == WindowState.Maximized))
                .GetAwaiter().GetResult();
        }

        base.OnClosing(e);
    }
}
