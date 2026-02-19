using NativeTray.Win32;
using System;

namespace NativeTray;

public partial class TrayIconHost
{
    protected void SetThemeMode(TrayThemeMode theme = TrayThemeMode.System)
    {
        // Enable dark mode for context menus if using dark theme
        if (Environment.OSVersion.Platform == PlatformID.Win32NT
            && NTdll.GetOSVersion().Build >= 18362) // Windows 10 1903
        {
            if (theme == TrayThemeMode.System)
            {
                // UxTheme methods will apply all of menus.
                // However, the Windows style system prefers that
                // Windows System Menu is based on `Apps Theme`,
                // and Tray Context Menu is based on `System Theme` when using a custom theme.
                // But actually we can't have our cake and eat it too.
                // Finally, we synchronize the theme styles of tray with higher usage rates.
                if (OSThemeHelper.SystemUsesDarkTheme())
                {
                    _ = UxTheme.SetPreferredAppMode(UxTheme.PreferredAppMode.ForceDark);
                    UxTheme.FlushMenuThemes();
                }

                // Synchronize the theme with system settings
                UserPreferenceChanged -= OnUserPreferenceChangedEventHandler;
                UserPreferenceChanged += OnUserPreferenceChangedEventHandler;
            }
            else if (theme == TrayThemeMode.Dark)
            {
                UserPreferenceChanged -= OnUserPreferenceChangedEventHandler;
                _ = UxTheme.SetPreferredAppMode(UxTheme.PreferredAppMode.ForceDark);
                UxTheme.FlushMenuThemes();
            }
            else if (theme == TrayThemeMode.Light)
            {
                UserPreferenceChanged -= OnUserPreferenceChangedEventHandler;
                _ = UxTheme.SetPreferredAppMode(UxTheme.PreferredAppMode.ForceLight);
                UxTheme.FlushMenuThemes();
            }
        }
    }

    protected static void OnUserPreferenceChangedEventHandler(object? sender, EventArgs e)
    {
        if (OSThemeHelper.SystemUsesDarkTheme())
        {
            _ = UxTheme.SetPreferredAppMode(UxTheme.PreferredAppMode.ForceDark);
            UxTheme.FlushMenuThemes();
        }
        else
        {
            _ = UxTheme.SetPreferredAppMode(UxTheme.PreferredAppMode.ForceLight);
            UxTheme.FlushMenuThemes();
        }
    }
}
