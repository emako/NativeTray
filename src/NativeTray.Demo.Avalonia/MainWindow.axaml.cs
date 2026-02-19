using Avalonia.Controls;

namespace NativeTray.Demo.Avalonia;

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
        Title = "NativeTray.Demo.Avalonia";
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
}
