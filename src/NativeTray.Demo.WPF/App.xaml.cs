using System.Windows;

namespace NativeTray.Demo.WPF;

public partial class App : Application
{
    public App()
    {
        //SystemMenuThemeManager.Apply();
        TrayIconManager.Start();
        InitializeComponent();
    }
}
