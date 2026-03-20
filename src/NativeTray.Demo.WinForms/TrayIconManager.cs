using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.NativeTray;

namespace WinFormsApp;

internal partial class TrayIconManager
{
    private static TrayIconManager _instance = null!;

    private readonly TrayIconHost? _iconHost = null;

    private TrayIconManager()
    {
        using Win32Icon icon = new(ResourceHelper.GetStream("NativeTray.Demo.WinForms.logo.ico"));

        _iconHost = new TrayIconHost()
        {
            ToolTipText = "NativeTray.Demo.WinForms",
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

    private static void ActivateOrRestoreMainWindow()
    {
        Form? mainForm = Application.OpenForms["MainForm"];
        if (mainForm is not null)
        {
            if (mainForm.Visible)
            {
                mainForm.Hide();
            }
            else
            {
                mainForm.Show();
                mainForm.Activate();
            }
        }
    }

    private void ShowWindow(object? _)
    {
        Form? mainForm = Application.OpenForms["MainForm"];
        if (mainForm is not null)
        {
            mainForm.Show();
            mainForm.Activate();
        }
    }

    private void ShowNotification(object? _)
    {
        ShowNotificationTip("NativeTray", "This is a balloon tip from WinForms!", TrayToolTipIcon.Info, 3000);
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
        Application.Exit();
    }
}

file static class ResourceHelper
{
    public static Stream GetStream(string name, Assembly assembly = null!)
    {
        Stream stream = (assembly ?? Assembly.GetExecutingAssembly()).GetManifestResourceStream(name)!;
        return stream;
    }
}
