[![NuGet](https://img.shields.io/nuget/v/NativeTray.svg)](https://nuget.org/packages/NativeTray) [![Actions](https://github.com/emako/NativeTray/actions/workflows/library.nuget.yml/badge.svg)](https://github.com/emako/NativeTray/actions/workflows/library.nuget.yml) [![Platform](https://img.shields.io/badge/platform-Windows-blue?logo=windowsxp&color=1E9BFA)](https://dotnet.microsoft.com/zh-cn/download/dotnet/latest/runtime)

# NativeTray

NativeTray is a modern, easy-to-use library for displaying tray icons (NotifyIcon) in .NET applications. It supports WPF, WinForms, and other .NET platforms, providing non-intrusive system notifications and quick access functionality in the Windows taskbar.

> Supports dark mode, custom icons, checkable menu items, and submenus.

## Features

- Native Win32 tray icon integration.
- Context menu with checkable, disabled, and bold items.
- Balloon notifications with custom title, text, and icon.
- Theme mode support (light, dark, system).
- High DPI support for crisp icon rendering.
- Easy API for menu and icon management.

## Usage

Install NativeTray via NuGet:

```bash
dotnet add package NativeTray
```

Example usage:

```csharp
using System.NativeTray;

var trayIcon = new TrayIconHost
{
    ToolTipText = "NativeTray Demo.",
    Icon = ... // Load your icon handle here.
};

trayIcon.Menu = new TrayMenu
{
    new TrayMenuItem { Header = "Item 1", Command = new TrayCommand(_ => { /* action */ }) },
    new TraySeparator(),
    new TrayMenuItem { Header = "Exit", Command = new TrayCommand(_ => Environment.Exit(0)) }
};

trayIcon.ShowBalloonTip(3000, "Hello", "This is a balloon tip.", TrayToolTipIcon.Info);
```

Menu item commands use the `ITrayCommand` abstraction. For simple actions, use `TrayCommand`.

You can set the tray icon theme mode:

```csharp
trayIcon.ThemeMode = TrayThemeMode.Dark;
```

## Demo

- [NativeTray.Demo.WPF](https://github.com/emako/NativeTray/tree/master/src/NativeTray.Demo.WPF) for [WPF](https://github.com/dotnet/wpf) applications.
- [NativeTray.Demo.WinForms](https://github.com/emako/NativeTray/tree/master/src/NativeTray.Demo.WinForms) for [WinForms](https://github.com/dotnet/winforms) applications.
- [NativeTray.Demo.Avalonia](https://github.com/emako/NativeTray/tree/master/src/NativeTray.Demo.Avalonia) for [Avalonia](https://github.com/AvaloniaUI/Avalonia) applications.
- [NativeTray.Demo.WinUI](https://github.com/emako/NativeTray/tree/master/src/NativeTray.Demo.WinUI) for [WinUI](https://github.com/microsoft/microsoft-ui-xaml) applications.
- [NativeTray.Demo.Maui](https://github.com/emako/NativeTray/tree/master/src/NativeTray.Demo.Maui) for [MAUI](https://github.com/dotnet/maui) applications.
- [NativeTray.Demo.ConsoleApp](https://github.com/emako/NativeTray/tree/master/src/NativeTray.Demo.ConsoleApp) for Console applications.

## Note

UWP is not supported.

NativeTray relies on Win32 tray APIs (such as Shell_NotifyIcon), while UWP runs in a sandboxed app model and does not provide the required system tray/NotifyIcon capabilities. In short, this limitation comes from UWP itself rather than NativeTray.

For UWP applications, migration to WinUI3 is recommended, and WinUI3 is supported by NativeTray.

## License

NativeTray is released under the MIT license. You are free to use and modify it, as long as you comply with the terms of the license.

