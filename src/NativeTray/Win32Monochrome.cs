using System.NativeTray.Win32;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace System.NativeTray;

internal static class Win32Monochrome
{
    public static uint ResolveArgb(TrayThemeMode themeMode)
    {
        TrayThemeMode effectiveTheme = themeMode;

        if (effectiveTheme == TrayThemeMode.System)
            effectiveTheme = OSThemeHelper.SystemUsesDarkTheme() ? TrayThemeMode.Dark : TrayThemeMode.Light;

        return effectiveTheme == TrayThemeMode.Dark ? 0xFFFFFFFFu : 0xFF000000u;
    }

    public static bool TryCreateMonochromeBitmap(byte[] imageBytes, TrayThemeMode themeMode, out nint hBitmap)
    {
        hBitmap = IntPtr.Zero;
        return TryCreateMonochromeGpBitmap(imageBytes, themeMode, out nint gpBitmap)
            && FinishAsHBitmap(gpBitmap, out hBitmap);
    }

    public static bool TryCreateMonochromeIcon(byte[] imageBytes, TrayThemeMode themeMode, out nint hIcon)
    {
        hIcon = IntPtr.Zero;
        return TryCreateMonochromeGpBitmap(imageBytes, themeMode, out nint gpBitmap)
            && FinishAsHIcon(gpBitmap, out hIcon);
    }

    private static bool FinishAsHBitmap(nint gpBitmap, out nint hBitmap)
    {
        hBitmap = IntPtr.Zero;
        try
        {
            int createHBitmapResult = GdiPlus.GdipCreateHBITMAPFromBitmap(gpBitmap, out hBitmap, 0);
            return createHBitmapResult == 0 && hBitmap != IntPtr.Zero;
        }
        finally
        {
            _ = GdiPlus.GdipDisposeImage(gpBitmap);
        }
    }

    private static bool FinishAsHIcon(nint gpBitmap, out nint hIcon)
    {
        hIcon = IntPtr.Zero;
        try
        {
            int createHIconResult = GdiPlus.GdipCreateHICONFromBitmap(gpBitmap, out hIcon);
            return createHIconResult == 0 && hIcon != IntPtr.Zero;
        }
        finally
        {
            _ = GdiPlus.GdipDisposeImage(gpBitmap);
        }
    }

    private static bool TryCreateMonochromeGpBitmap(byte[] imageBytes, TrayThemeMode themeMode, out nint gpBitmap)
    {
        gpBitmap = IntPtr.Zero;

        if (imageBytes is null || imageBytes.Length == 0)
            return false;

        GdiPlus.EnsureInitialized();

        if (!TryLoadSourceBitmap(imageBytes, out nint sourceBitmap))
            return false;

        nint workingBitmap = IntPtr.Zero;

        try
        {
            int widthResult = GdiPlus.GdipGetImageWidth(sourceBitmap, out uint width);
            int heightResult = GdiPlus.GdipGetImageHeight(sourceBitmap, out uint height);
            if (widthResult != 0 || heightResult != 0 || width == 0 || height == 0)
                return false;

            // Clone to a real 32bpp ARGB bitmap. LockBits with format conversion on ICO/PNG
            // frames often does not write pixels back to the original image.
            int cloneResult = GdiPlus.GdipCloneBitmapAreaI(
                0,
                0,
                (int)width,
                (int)height,
                GdiPlus.PixelFormat32bppArgb,
                sourceBitmap,
                out workingBitmap);

            if (cloneResult != 0 || workingBitmap == IntPtr.Zero)
                return false;

            var rect = new GdiPlus.GpRect
            {
                X = 0,
                Y = 0,
                Width = (int)width,
                Height = (int)height,
            };

            var bitmapData = default(GdiPlus.BitmapData);
            int lockBitsResult = GdiPlus.GdipBitmapLockBits(
                workingBitmap,
                ref rect,
                (uint)(GdiPlus.ImageLockMode.Read | GdiPlus.ImageLockMode.Write),
                GdiPlus.PixelFormat32bppArgb,
                ref bitmapData);

            if (lockBitsResult != 0 || bitmapData.Scan0 == IntPtr.Zero)
                return false;

            try
            {
                ApplyMonochrome(bitmapData, (int)height, ResolveArgb(themeMode));
            }
            finally
            {
                _ = GdiPlus.GdipBitmapUnlockBits(workingBitmap, ref bitmapData);
            }

            gpBitmap = workingBitmap;
            workingBitmap = IntPtr.Zero;
            return true;
        }
        finally
        {
            if (sourceBitmap != IntPtr.Zero)
                _ = GdiPlus.GdipDisposeImage(sourceBitmap);

            if (workingBitmap != IntPtr.Zero)
                _ = GdiPlus.GdipDisposeImage(workingBitmap);
        }
    }

    private static bool TryLoadSourceBitmap(byte[] imageBytes, out nint bitmap)
    {
        bitmap = IntPtr.Zero;

        // Prefer the largest PNG frame inside an ICO so tray icons stay sharp.
        if (TryExtractBestPngFromIco(imageBytes, out byte[] pngBytes))
        {
            if (TryLoadBitmapFromBytes(pngBytes, out bitmap))
                return true;
        }

        return TryLoadBitmapFromBytes(imageBytes, out bitmap);
    }

    private static bool TryLoadBitmapFromBytes(byte[] imageBytes, out nint bitmap)
    {
        bitmap = IntPtr.Zero;
        IStream? imageStream = null;

        try
        {
            int createStreamResult = Ole32.CreateStreamOnHGlobal(IntPtr.Zero, true, out imageStream);
            if (createStreamResult != 0 || imageStream is null)
                return false;

            imageStream.Write(imageBytes, imageBytes.Length, IntPtr.Zero);
            imageStream.Seek(0, 0, IntPtr.Zero);

            int createBitmapResult = GdiPlus.GdipCreateBitmapFromStream(imageStream, out bitmap);
            return createBitmapResult == 0 && bitmap != IntPtr.Zero;
        }
        finally
        {
            if (imageStream is not null)
                Marshal.ReleaseComObject(imageStream);
        }
    }

    private static bool TryExtractBestPngFromIco(byte[] bytes, out byte[] pngBytes)
    {
        pngBytes = new byte[0];

        if (bytes.Length < 6)
            return false;

        ushort reserved = BitConverter.ToUInt16(bytes, 0);
        ushort type = BitConverter.ToUInt16(bytes, 2);
        ushort count = BitConverter.ToUInt16(bytes, 4);
        if (reserved != 0 || type != 1 || count == 0)
            return false;

        int bestArea = -1;
        int bestOffset = 0;
        int bestSize = 0;

        for (int i = 0; i < count; i++)
        {
            int entryOffset = 6 + (i * 16);
            if (bytes.Length < entryOffset + 16)
                return false;

            int width = bytes[entryOffset];
            int height = bytes[entryOffset + 1];
            if (width == 0) width = 256;
            if (height == 0) height = 256;

            uint imageSize = BitConverter.ToUInt32(bytes, entryOffset + 8);
            uint imageOffset = BitConverter.ToUInt32(bytes, entryOffset + 12);
            if (imageOffset + imageSize > bytes.Length || imageSize < 8)
                continue;

            int start = (int)imageOffset;
            bool isPng = bytes[start] == 0x89
                && bytes[start + 1] == (byte)'P'
                && bytes[start + 2] == (byte)'N'
                && bytes[start + 3] == (byte)'G';

            if (!isPng)
                continue;

            int area = width * height;
            if (area <= bestArea)
                continue;

            bestArea = area;
            bestOffset = start;
            bestSize = (int)imageSize;
        }

        if (bestArea < 0)
            return false;

        pngBytes = new byte[bestSize];
        Buffer.BlockCopy(bytes, bestOffset, pngBytes, 0, bestSize);
        return true;
    }

    private static void ApplyMonochrome(GdiPlus.BitmapData bitmapData, int height, uint monochromeArgb)
    {
        int stride = Math.Abs(bitmapData.Stride);
        byte[] pixels = new byte[stride * height];
        Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);

        byte blue = (byte)(monochromeArgb & 0xFF);
        byte green = (byte)((monochromeArgb >> 8) & 0xFF);
        byte red = (byte)((monochromeArgb >> 16) & 0xFF);

        for (int row = 0; row < height; row++)
        {
            int rowOffset = row * stride;
            for (int offset = rowOffset; offset + 3 < rowOffset + stride; offset += 4)
            {
                byte alpha = pixels[offset + 3];
                if (alpha == 0)
                    continue;

                pixels[offset] = blue;
                pixels[offset + 1] = green;
                pixels[offset + 2] = red;
            }
        }

        Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
    }
}
