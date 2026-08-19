using System.Runtime.InteropServices;

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
    public static extern int GdipCreateBitmapFromStream(nint stream, out nint bitmap);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipCreateHBITMAPFromBitmap(nint bitmap, out nint hbmReturn, uint background);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipCreateHICONFromBitmap(nint bitmap, out nint hicon);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipGetImageWidth(nint image, out uint width);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipGetImageHeight(nint image, out uint height);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipBitmapLockBits(nint bitmap, ref GpRect rect, uint flags, int format, ref BitmapData lockedBitmapData);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipBitmapUnlockBits(nint bitmap, ref BitmapData lockedBitmapData);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipDisposeImage(nint image);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipCloneBitmapAreaI(
        int x,
        int y,
        int width,
        int height,
        int format,
        nint srcBitmap,
        out nint dstBitmap);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipCreateBitmapFromScan0(
        int width,
        int height,
        int stride,
        int format,
        nint scan0,
        out nint bitmap);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipCreateBitmapFromHBITMAP(nint hbm, nint hpal, out nint bitmap);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipCreateBitmapFromHICON(nint hicon, out nint bitmap);

    [DllImport("gdiplus.dll", ExactSpelling = true)]
    public static extern int GdipSaveImageToStream(nint image, nint stream, ref Guid clsidEncoder, nint encoderParams);

    /// <summary>image/png encoder CLSID used by GDI+.</summary>
    public static readonly Guid PngEncoderClsid = new("557CF406-1A04-11D3-9A73-0000F81EF32E");

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

    [StructLayout(LayoutKind.Sequential)]
    public struct GpRect
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BitmapData
    {
        public uint Width;
        public uint Height;
        public int Stride;
        public int PixelFormat;
        public nint Scan0;
        public nint Reserved;
    }

    [Flags]
    public enum ImageLockMode : uint
    {
        Read = 0x0001,
        Write = 0x0002,
        UserInputBuffer = 0x0004,
    }

    public const int PixelFormat32bppArgb = 0x26200A;
}
