using System.Runtime.InteropServices;

namespace System.NativeTray.Win32;

internal static class Ole32
{
    [DllImport("ole32.dll")]
    public static extern int CreateStreamOnHGlobal(nint hGlobal, bool fDeleteOnRelease, out nint ppstm);
}
