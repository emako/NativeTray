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
using NativeTray;

var trayIcon = new TrayIconHost
{
    ToolTipText = "NativeTray Demo.",
    Icon = ... // Load your icon handle here.
};

trayIcon.Menu = new TrayMenu
{
    new TrayMenuItem { Header = "Item 1", Command = _ => { /* action */ } },
    new TraySeparator(),
    new TrayMenuItem { Header = "Exit", Command = _ => Environment.Exit(0) }
};

trayIcon.ShowBalloonTip(3000, "Hello", "This is a balloon tip.", ToolTipIcon.Info);
```

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

## License

NativeTray is released under the MIT license. You are free to use and modify it, as long as you comply with the terms of the license.

