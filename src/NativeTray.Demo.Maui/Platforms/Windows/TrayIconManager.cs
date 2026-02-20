using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MauiApp1;

internal partial class TrayIconManager
{
    private static TrayIconManager _instance = null!;

    private readonly NativeTray.TrayIconHost? _iconHost = null;

    private TrayIconManager()
    {
        using var iconStream = ResourceHelper.TryOpen("logo.ico");
        using var icon = new NativeTray.Win32Icon(iconStream);

        _iconHost = new NativeTray.TrayIconHost()
        {
            ToolTipText = "NativeTray.Demo.Maui",
            Icon = icon.Handle,
            ThemeMode = NativeTray.TrayThemeMode.System,
            Menu =
            [
                new NativeTray.TrayMenuItem()
                {
                    Header = Version,
                    IsEnabled = false,
                },
                new NativeTray.TraySeparator(),
                new NativeTray.TrayMenuItem()
                {
                    Header = "Option",
                    Menu =
                    [
                        new NativeTray.TrayMenuItem()
                        {
                            Header = "Option1",
                            Menu =
                            [
                                new NativeTray.TrayMenuItem()
                                {
                                    Header = "Option1-1",
                                    Command = ShowNotification,
                                },
                                new NativeTray.TrayMenuItem()
                                {
                                    Header = "Option1-2",
                                }
                            ],
                        },
                        new NativeTray.TrayMenuItem()
                        {
                            Header = "Option2",
                        }
                    ],
                },
                new NativeTray.TrayMenuItem()
                {
                    Header = "Show Window",
                    Command = ShowWindow,
                },
                new NativeTray.TrayMenuItem()
                {
                    Header = "Restart",
                    Command = Restart,
                    IsBold = true,
                },
                new NativeTray.TrayMenuItem()
                {
                    Header = "Exit",
                    Command = Exit,
                }
            ],
        };

        _iconHost.LeftDoubleClick += (_, _) => ActivateOrRestoreMainWindow();
    }

    public static TrayIconManager GetInstance()
    {
        return _instance ??= new TrayIconManager();
    }

    public static void Start()
    {
        _ = GetInstance();
    }

    public static void ShowNotificationTip(string title, string content, NativeTray.ToolTipIcon icon = default, int timeout = 5000, Action? clickEvent = null, Action? closeEvent = null)
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

    private static void ActivateOrRestoreMainWindow()
    {
        try
        {
            var h = Process.GetCurrentProcess().MainWindowHandle;
            if (h != nint.Zero)
            {
                // If the window is minimized, restore it first
                try
                {
                    if (IsIconic(h))
                    {
                        _ = ShowWindow(h, SW_RESTORE);
                    }
                }
                catch
                {
                    // ignore errors from restore attempt
                }

                SetForegroundWindow(h);
                BringWindowToTop(h);
            }
        }
        catch
        {
            // ignore
        }
    }

    private void ShowWindow(object? _)
    {
        ActivateOrRestoreMainWindow();
    }

    private void ShowNotification(object? _)
    {
        ShowNotificationTip("NativeTray", "This is a balloon tip from MAUI!", NativeTray.ToolTipIcon.Info, 3000);
    }

    private void Restart(object? _)
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

    private void Exit(object? _)
    {
        Application.Current?.Quit();
    }

    [DllImport("user32.dll")]
    [SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    [SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time")]
    private static extern bool BringWindowToTop(nint hWnd);

    [DllImport("user32.dll")]
    [SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time")]
    private static extern bool IsIconic(nint hWnd);

    private const int SW_RESTORE = 9;
}

file static class ResourceHelper
{
    public static Stream TryOpen(string name)
    {
        // Try manifest resource first
        var asm = Assembly.GetExecutingAssembly();
        Stream? s = asm.GetManifestResourceStream(name);
        if (s is not null) return s;

        // Try without namespace
        foreach (var res in asm.GetManifestResourceNames())
        {
            if (res.EndsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                return asm.GetManifestResourceStream(res)!;
            }
        }

        return null!;
    }
}
