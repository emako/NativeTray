using Avalonia.Controls;

namespace AvaloniaApp1;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Width = 800;
        Height = 600;
        Title = "AvaloniaApp1";
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
}
