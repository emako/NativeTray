using System.Windows;

namespace NativeTray.Demo.WPF;

public partial class App : Application
{
    public App()
    {
        TrayIconManager.Start();
        InitializeComponent();
    }
}
