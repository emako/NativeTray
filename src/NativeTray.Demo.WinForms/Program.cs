using System;
using System.Windows.Forms;

namespace NativeTray.Demo.WinForms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        TrayIconManager.Start();

        Application.Run(new MainForm());
    }
}
