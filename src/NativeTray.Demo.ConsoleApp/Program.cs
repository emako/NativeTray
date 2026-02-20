using System;
using System.Threading;

namespace ConsoleApp1;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        Console.WriteLine("NativeTray Console Demo starting...");
        Console.WriteLine("Tray icon will appear in the system tray. Use the tray menu to Exit.");

        // Start tray icon
        TrayIconManager.Start();

        // Keep the console app running until exit
        using var mre = new ManualResetEventSlim(false);

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("Exiting...");
            mre.Set();
        };

        mre.Wait();
    }
}
