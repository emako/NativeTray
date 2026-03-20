using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.NativeTray;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Resources;

namespace WpfApp1;

internal partial class TrayIconManager
{
    private static TrayIconManager _instance = null!;

    private readonly TrayIconHost? _iconHost = null;

    private TrayIconManager()
    {
        using Win32Icon icon = new(ResourceHelper.GetStream("pack://application:,,,/NativeTray.Demo.WPF;component/logo.ico"));

        _iconHost = new TrayIconHost()
        {
            ToolTipText = "NativeTray.Demo.WPF",
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
                    Header = "Restart",
                    Command = Restart,
                    IsBold = true, // Test the bold style
                },
                new TrayMenuItem()
                {
                    Header = "Exit",
                    Command = Exit,
                    Icon = new Win32Image(ResourceHelper.GetStream("pack://application:,,,/NativeTray.Demo.WPF;component/close.png"))
                    {
                        ShowAsMonochrome = true,
                    },
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

    public static void ShowNotification(string title, string content, TrayToolTipIcon icon = default, int timeout = 5000, Action? clickEvent = null, Action? closeEvent = null)
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
        if (Application.Current.MainWindow is not null)
        {
            if (Application.Current.MainWindow.IsVisible)
            {
                Application.Current.MainWindow.Hide();
            }
            else
            {
                Application.Current.MainWindow.Show();
                Application.Current.MainWindow.Activate();
            }
        }
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
        Application.Current.Shutdown();
    }
}

file static class ResourceHelper
{
    static ResourceHelper()
    {
        if (!UriParser.IsKnownScheme("pack"))
            _ = PackUriHelper.UriSchemePack;
    }

    public static Stream GetStream(string uriString)
    {
        Uri uri = new(uriString);
        StreamResourceInfo info = Application.GetResourceStream(uri);
        return info?.Stream!;
    }
}
