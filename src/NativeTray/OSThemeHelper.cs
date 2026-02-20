using System.NativeTray.Win32;

namespace System.NativeTray;

public static class OSThemeHelper
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private const string ValueName = "SystemUsesLightTheme";

    public static bool SystemUsesDarkTheme()
    {
        uint dataSize = sizeof(uint);
        int result = AdvApi32.RegGetValue(AdvApi32.HKEY_CURRENT_USER, KeyPath, ValueName, AdvApi32.RRF_RT_REG_DWORD, IntPtr.Zero, out uint data, ref dataSize);

        // Failed to read -> Process as dark color
        if (result != 0)
            return true;

        // 0 = Dark, 1 = Light
        return data == 0;
    }
}
