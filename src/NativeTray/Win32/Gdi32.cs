using System.Runtime.InteropServices;

namespace System.NativeTray.Win32;

internal static class Gdi32
{
    [DllImport("gdi32.dll", SetLastError = false, ExactSpelling = true)]
    public static extern int GetDeviceCaps(nint hdc, DeviceCap nIndex);

    public enum DeviceCap
    {
        LOGPIXELSX = 88,
        LOGPIXELSY = 90,
    }
}
