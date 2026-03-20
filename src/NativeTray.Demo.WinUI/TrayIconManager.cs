using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Diagnostics;
using System.NativeTray;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WinUIApp1;

internal partial class TrayIconManager
{
    private static TrayIconManager _instance = null!;

    private readonly TrayIconHost? _iconHost = null;

    private TrayIconManager()
    {
        // Use ms-appx URI for packaged app resource access in WinUI
        using Win32Icon icon = new(ResourceHelper.GetStream("ms-appx:///Assets/logo.ico"));

        _iconHost = new TrayIconHost()
        {
            ToolTipText = "NativeTray.Demo.WinUI",
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
                                    Command = new TrayRelayCommand(ShowNotification),
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
                        }
                    ],
                },
                new TrayMenuItem()
                {
                    Header = "Show Window",
                    Command = new TrayRelayCommand(ShowWindow),
                },
                new TrayMenuItem()
                {
                    Header = "Restart",
                    Command = new TrayRelayCommand(Restart),
                    IsBold = true,
                },
                new TrayMenuItem()
                {
                    Header = "Exit",
                    Command = new TrayRelayCommand(Exit),
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

    private static Window? MainWindow => (Application.Current as App)?.GetType()
        .GetField("m_window", BindingFlags.NonPublic | BindingFlags.Instance)
        ?.GetValue(Application.Current as App) as Window;

    private static void ActivateOrRestoreMainWindow()
    {
        var mainWindow = MainWindow;
        if (mainWindow is not null)
        {
            mainWindow.Activate();
        }
    }

    private void ShowWindow(object? _)
    {
        var mainWindow = MainWindow;
        if (mainWindow is not null)
        {
            mainWindow.Activate();
        }
    }

    private void ShowNotification(object? _)
    {
        ShowNotificationTip("NativeTray", "This is a balloon tip from WinUI!", TrayToolTipIcon.Info, 3000);
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
        Application.Current.Exit();
    }
}

file static class ResourceHelper
{
    public static Stream GetStream(string uriString)
    {
        // Try ms-appx first (packaged resource). Fallback to file path if needed.
        try
        {
            StorageFile file = StorageFile.GetFileFromApplicationUriAsync(new Uri(uriString)).AsTask().GetAwaiter().GetResult();
            IRandomAccessStream randomAccessStream = file.OpenAsync(FileAccessMode.Read).AsTask().GetAwaiter().GetResult();
            Stream stream = randomAccessStream.AsStreamForRead();
            return stream;
        }
        catch
        {
            // Fallback: try local file
            string path = uriString.Replace("ms-appx:///", string.Empty).Replace('/', Path.DirectorySeparatorChar);
            string full = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            return File.OpenRead(full);
        }
    }
}
