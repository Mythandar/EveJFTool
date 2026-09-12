using System.Windows;
using EveJFTool.App.ViewModels;

namespace EveJFTool.App;

public partial class NearbySystemsWindow : Window
{
    public NearbySystemsWindow() => InitializeComponent();

    private void Insert_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is NearbySystemsViewModel { Selected: not null }) DialogResult = true;
    }
}
