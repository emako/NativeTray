using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace System.NativeTray.Win32;

internal static class GdiPlus
{
    private static readonly object SyncRoot = new();
    private static nint _token;
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (SyncRoot)
        {
            if (_initialized)
                return;

            var startupInput = new GdiplusStartupInput
            {
                GdiplusVersion = 1,
                DebugEventCallback = IntPtr.Zero,
                SuppressBackgroundThread = false,
                SuppressExternalCodecs = false,
            };

            int status = GdiplusStartup(out _token, ref startupInput, IntPtr.Zero);
            if (status != 0)
                throw new InvalidOperationException($"GdiplusStartup failed with status code {status}.");

            _initialized = true;
        }
    }

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipCreateBitmapFromStream(IStream stream, out nint bitmap);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipCreateHBITMAPFromBitmap(nint bitmap, out nint hbmReturn, uint background);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipDisposeImage(nint image);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    private static extern int GdiplusStartup(out nint token, ref GdiplusStartupInput input, nint output);

    [StructLayout(LayoutKind.Sequential)]
    private struct GdiplusStartupInput
    {
        public uint GdiplusVersion;
        public nint DebugEventCallback;

        [MarshalAs(UnmanagedType.Bool)]
        public bool SuppressBackgroundThread;

        [MarshalAs(UnmanagedType.Bool)]
        public bool SuppressExternalCodecs;
    }
}
