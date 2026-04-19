using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.NativeTray;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

namespace App1;

internal partial class TrayIconManager
{
    private static TrayIconManager _instance = null!;

    private readonly TrayIconHost? _iconHost = null;

    private TrayIconManager()
    {
        Stream iconStream = ResourceHelper.GetStream("ms-appx:///logo.ico");
        using Win32Icon icon = new(iconStream);

        _iconHost = new TrayIconHost()
        {
            ToolTipText = "NativeTray.Demo.UWP",
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
                    Header = "Show Window",
                    Command = new TrayCommand(ShowWindow),
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

    private static void ActivateOrRestoreMainWindow()
    {
        try
        {
            Window.Current?.Activate();
        }
        catch
        {
            // Ignore activation errors on UWP
        }
    }

    private void ShowWindow(object? commandParameter)
    {
        try
        {
            Window.Current?.Activate();
        }
        catch
        {
        }
    }

    private void ShowNotification(object? commandParameter)
    {
        ShowNotificationTip("NativeTray", "This is a balloon tip from UWP!", TrayToolTipIcon.Info, 3000);
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
        try
        {
            Application.Current.Exit();
        }
        catch
        {
        }
    }
}

file static class ResourceHelper
{
    public static Stream GetStream(string uriString)
    {
        try
        {
            StorageFile file = StorageFile.GetFileFromApplicationUriAsync(new Uri(uriString)).AsTask().GetAwaiter().GetResult();
            IRandomAccessStream randomAccessStream = file.OpenAsync(FileAccessMode.Read).AsTask().GetAwaiter().GetResult();
            Stream stream = randomAccessStream.AsStreamForRead();
            return stream;
        }
        catch
        {
            string path = uriString.Replace("ms-appx:///", string.Empty).Replace('/', Path.DirectorySeparatorChar);
            string full = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            return File.OpenRead(full);
        }
    }
}

#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
