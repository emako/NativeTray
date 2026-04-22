using System.ComponentModel;
using System.Diagnostics;
using System.NativeTray;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WiceApp1;

internal partial class TrayIconManager
{
    private static TrayIconManager _instance = null!;

    private readonly TrayIconHost? _iconHost = null;

    private TrayIconManager()
    {
        using var icon = new Win32Icon(ResourceHelper.GetStream("NativeTray.Demo.Wice.logo.ico"));

        _iconHost = new TrayIconHost()
        {
            ToolTipText = "NativeTray.Demo.Wice",
            Icon = icon.Handle,
            ThemeMode = TrayThemeMode.System,
            Menu =
            [
                new TrayMenuItem()
                {
                    Header = Version,
                    IsEnabled = false,
                },
                new TraySeparator(),
                new TrayMenuItem()
                {
                    Header = "Option",
                    Menu =
                    [
                        new TrayMenuItem()
                        {
                            Header = "Option1",
                            Menu =
                            [
                                new TrayMenuItem()
                                {
                                    Header = "Option1-1",
                                    Command = new TrayCommand(ShowNotification),
                                },
                                new TrayMenuItem()
                                {
                                    Header = "Option1-2",
                                }
                            ],
                        },
                        new TrayMenuItem()
                        {
                            Header = "Option2",
                            IsChecked = true,
                        }
                    ],
                },
                new TrayMenuItem()
                {
                    Header = "Show Wice",
                    //Command = new TrayCommand(ShowWindow),
                },
                new TrayMenuItem()
                {
                    Header = "Show Notification\ttest",
                    Command = new TrayCommand(ShowNotification),
                    IsBold = true,
                },
                new TrayMenuItem()
                {
                    Header = "Restart",
                    Command = new TrayCommand(Restart),
                },
                new TrayMenuItem()
                {
                    Header = "Exit",
                    Command = new TrayCommand(Exit),
                }
            ],
        };

        //_iconHost.LeftDoubleClick += (_, _) => ActivateOrRestoreMainWindow();
    }

    public static TrayIconManager GetInstance()
    {
        return _instance ??= new TrayIconManager();
    }

    public static void Start()
    {
        _ = GetInstance();
    }

    public static void ShowNotificationTip(string title, string content, TrayToolTipIcon icon = default, int timeout = 5000, Action? clickEvent = null, Action? closeEvent = null)
    {
        var iconHost = GetInstance()._iconHost;
        if (iconHost is null) return;

        iconHost.ShowBalloonTip(timeout, title, content, icon);
        iconHost.BalloonTipClicked += OnIconOnBalloonTipClicked;
        iconHost.BalloonTipClosed += OnIconOnBalloonTipClosed;

        void OnIconOnBalloonTipClicked(object? sender, EventArgs e)
        {
            clickEvent?.Invoke();
            iconHost.BalloonTipClicked -= OnIconOnBalloonTipClicked;
        }

        void OnIconOnBalloonTipClosed(object? sender, EventArgs e)
        {
            closeEvent?.Invoke();
            iconHost.BalloonTipClosed -= OnIconOnBalloonTipClosed;
        }
    }
}

internal partial class TrayIconManager
{
    private static string Version => $"v{Assembly.GetExecutingAssembly().GetName().Version!.ToString(3)}";

    private void ShowNotification(object? commandParameter)
    {
        ShowNotificationTip("NativeTray", "This is a balloon tip from Console demo!", TrayToolTipIcon.Info, 3000);
    }

    private void Restart(object? commandParameter)
    {
        try
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = GetExecutablePath(),
                    WorkingDirectory = Environment.CurrentDirectory,
                    UseShellExecute = true,
                },
            };
            process.Start();
        }
        catch (Win32Exception)
        {
            return;
        }

        Process.GetCurrentProcess().Kill();

        static string GetExecutablePath()
        {
            string fileName = AppDomain.CurrentDomain.FriendlyName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                fileName += ".exe";

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }
    }

    private void Exit(object? commandParameter)
    {
        Environment.Exit(0);
    }

    [DllImport("kernel32.dll")]
    private static extern nint GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(nint hWnd);

    private const int SW_RESTORE = 9;
}

file static class ResourceHelper
{
    public static Stream GetStream(string name, Assembly assembly = null!)
    {
        Stream stream = (assembly ?? Assembly.GetExecutingAssembly()).GetManifestResourceStream(name)!;
        return stream;
    }
}
