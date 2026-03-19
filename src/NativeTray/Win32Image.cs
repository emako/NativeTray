using System.IO;
using System.NativeTray.Win32;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace System.NativeTray;

public class Win32Image : IDisposable
{
    public nint Handle { get; private set; }

    public Win32Image(Stream stream)
    {
        _ = stream ?? throw new ArgumentNullException(nameof(stream));

        GdiPlus.EnsureInitialized();

        IStream? imageStream = null;
        nint gpBitmap = IntPtr.Zero;

        try
        {
            using MemoryStream ms = new();
            stream.CopyTo(ms);
            byte[] bytes = ms.ToArray();

            int createStreamResult = CreateStreamOnHGlobal(IntPtr.Zero, true, out imageStream);
            if (createStreamResult != 0 || imageStream is null)
                throw new InvalidOperationException($"CreateStreamOnHGlobal failed with HRESULT 0x{createStreamResult:X8}.");

            imageStream.Write(bytes, bytes.Length, IntPtr.Zero);
            imageStream.Seek(0, 0, IntPtr.Zero);

            int createBitmapResult = GdiPlus.GdipCreateBitmapFromStream(imageStream, out gpBitmap);
            if (createBitmapResult != 0 || gpBitmap == IntPtr.Zero)
                throw new InvalidDataException($"Image decode failed with GDI+ status code {createBitmapResult}.");

            int createHBitmapResult = GdiPlus.GdipCreateHBITMAPFromBitmap(gpBitmap, out nint hBitmap, 0);
            if (createHBitmapResult != 0 || hBitmap == IntPtr.Zero)
                throw new InvalidOperationException($"GdipCreateHBITMAPFromBitmap failed with status code {createHBitmapResult}.");

            Handle = hBitmap;
        }
        finally
        {
            if (gpBitmap != IntPtr.Zero)
                _ = GdiPlus.GdipDisposeImage(gpBitmap);

            if (imageStream is not null)
                Marshal.ReleaseComObject(imageStream);
        }
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            _ = Gdi32.DeleteObject(Handle);
            Handle = IntPtr.Zero;
        }

        GC.SuppressFinalize(this);
    }

    [DllImport("ole32.dll")]
    private static extern int CreateStreamOnHGlobal(nint hGlobal, bool fDeleteOnRelease, out IStream ppstm);
}
