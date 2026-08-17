using System.Collections.Generic;
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

        if (TryBuildMonochromeIco(imageBytes, themeMode, out byte[] monoIco)
            && TryLoadBestFrameBitmap(monoIco, out nint gpBitmap))
        {
            return FinishAsHBitmap(gpBitmap, out hBitmap);
        }

        return TryCreateMonochromeGpBitmap(imageBytes, themeMode, out nint gpBitmapFallback)
            && FinishAsHBitmap(gpBitmapFallback, out hBitmap);
    }

    public static bool TryCreateMonochromeIcon(byte[] imageBytes, TrayThemeMode themeMode, out nint hIcon)
    {
        hIcon = IntPtr.Zero;

        // Multi-size ICO: monochrome every frame, rebuild ICO, then create HICON from the
        // best-sized PNG via GDI+ (preserves alpha; CreateIconFromResourceEx / FromHICON do not).
        if (TryBuildMonochromeIco(imageBytes, themeMode, out byte[] monoIco)
            && TryLoadBestFrameBitmap(monoIco, out nint gpBitmap))
        {
            return FinishAsHIcon(gpBitmap, out hIcon);
        }

        return TryCreateMonochromeGpBitmap(imageBytes, themeMode, out nint gpBitmapFallback)
            && FinishAsHIcon(gpBitmapFallback, out hIcon);
    }

    /// <summary>
    /// Monochromes every image frame inside an ICO (any embedded format) and rebuilds a multi-size ICO.
    /// </summary>
    public static bool TryBuildMonochromeIco(byte[] imageBytes, TrayThemeMode themeMode, out byte[] monoIco)
    {
        monoIco = new byte[0];

        if (imageBytes is null || imageBytes.Length == 0 || !Win32Ico.IsIco(imageBytes))
            return false;

        var frames = new List<Win32Ico.IcoFrame>();
        if (!Win32Ico.TryGetFrames(imageBytes, frames))
            return false;

        GdiPlus.EnsureInitialized();
        uint argb = ResolveArgb(themeMode);
        var monoFrames = new List<Win32Ico.BuiltFrame>(frames.Count);

        foreach (Win32Ico.IcoFrame frame in frames)
        {
            if (!TryMonochromeIcoFrame(imageBytes, frame, argb, out byte[] monoPng, out int width, out int height))
                return false;

            monoFrames.Add(new Win32Ico.BuiltFrame(width, height, monoPng));
        }

        if (monoFrames.Count == 0)
            return false;

        monoIco = Win32Ico.BuildIco(monoFrames);
        return true;
    }

    private static bool TryLoadBestFrameBitmap(byte[] monoIco, out nint gpBitmap)
    {
        gpBitmap = IntPtr.Zero;

        Win32Ico.GetSmallIconSize(out int cx, out int cy);
        if (!Win32Ico.TrySelectBestImage(monoIco, cx, cy, out uint imageOffset, out uint imageSize))
            return false;

        byte[] pngBytes = new byte[imageSize];
        Buffer.BlockCopy(monoIco, (int)imageOffset, pngBytes, 0, (int)imageSize);
        return TryLoadBitmapFromBytes(pngBytes, out gpBitmap);
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

    private static bool TryMonochromeIcoFrame(
        byte[] icoBytes,
        Win32Ico.IcoFrame frame,
        uint argb,
        out byte[] monoPng,
        out int width,
        out int height)
    {
        monoPng = new byte[0];
        width = frame.Width;
        height = frame.Height;

        if (!TryDecodeIcoFrameToBitmap(icoBytes, frame, out nint sourceBitmap))
            return false;

        try
        {
            if (!TryMonochromeGpBitmap(sourceBitmap, argb, out nint monoBitmap))
                return false;

            try
            {
                if (GdiPlus.GdipGetImageWidth(monoBitmap, out uint w) != 0
                    || GdiPlus.GdipGetImageHeight(monoBitmap, out uint h) != 0
                    || w == 0
                    || h == 0)
                {
                    return false;
                }

                width = (int)w;
                height = (int)h;
                return TrySavePng(monoBitmap, out monoPng);
            }
            finally
            {
                _ = GdiPlus.GdipDisposeImage(monoBitmap);
            }
        }
        finally
        {
            _ = GdiPlus.GdipDisposeImage(sourceBitmap);
        }
    }

    private static bool TryDecodeIcoFrameToBitmap(byte[] icoBytes, Win32Ico.IcoFrame frame, out nint bitmap)
    {
        bitmap = IntPtr.Zero;

        if (Win32Ico.IsPng(icoBytes, frame.ImageOffset))
        {
            byte[] pngBytes = new byte[frame.ImageSize];
            Buffer.BlockCopy(icoBytes, frame.ImageOffset, pngBytes, 0, frame.ImageSize);
            return TryLoadBitmapFromBytes(pngBytes, out bitmap);
        }

        return TryDecodeIcoBmpFrame(icoBytes, frame, out bitmap);
    }

    /// <summary>
    /// Decodes a classic ICO BMP/DIB frame into a 32bpp ARGB bitmap, preserving XOR alpha and/or AND mask.
    /// <see cref="GdiPlus.GdipCreateBitmapFromHICON"/> is intentionally avoided — it collapses alpha to opaque.
    /// </summary>
    private static bool TryDecodeIcoBmpFrame(byte[] icoBytes, Win32Ico.IcoFrame frame, out nint bitmap)
    {
        bitmap = IntPtr.Zero;

        int offset = frame.ImageOffset;
        int length = frame.ImageSize;
        if (length < 40 || offset < 0 || offset + length > icoBytes.Length)
            return false;

        int biSize = BitConverter.ToInt32(icoBytes, offset);
        if (biSize < 40)
            return false;

        int biWidth = BitConverter.ToInt32(icoBytes, offset + 4);
        int biHeight = BitConverter.ToInt32(icoBytes, offset + 8);
        ushort biBitCount = BitConverter.ToUInt16(icoBytes, offset + 14);
        int biCompression = BitConverter.ToInt32(icoBytes, offset + 16);
        int biClrUsed = BitConverter.ToInt32(icoBytes, offset + 32);

        if (biCompression != 0) // BI_RGB only
            return false;

        int width = biWidth != 0 ? Math.Abs(biWidth) : frame.Width;
        int totalHeight = Math.Abs(biHeight);
        int height = totalHeight > 0 ? totalHeight / 2 : frame.Height;
        if (width <= 0 || height <= 0)
            return false;

        int paletteEntries = 0;
        if (biBitCount <= 8)
            paletteEntries = biClrUsed > 0 ? biClrUsed : (1 << biBitCount);

        int colorTableOffset = offset + biSize;
        int xorOffset = colorTableOffset + (paletteEntries * 4);
        int xorStride = ((width * biBitCount + 31) / 32) * 4;
        int xorSize = xorStride * height;
        int andStride = ((width + 31) / 32) * 4;
        int andOffset = xorOffset + xorSize;

        if (xorOffset < offset || andOffset + (andStride * height) > offset + length)
            return false;

        byte[] argb = new byte[width * height * 4];
        bool hasNonZeroAlpha = false;

        for (int y = 0; y < height; y++)
        {
            // DIB rows are bottom-up.
            int srcRow = height - 1 - y;
            int dstRow = y * width * 4;

            for (int x = 0; x < width; x++)
            {
                if (!TryReadXorPixel(
                        icoBytes,
                        xorOffset,
                        colorTableOffset,
                        paletteEntries,
                        xorStride,
                        biBitCount,
                        srcRow,
                        x,
                        out byte b,
                        out byte g,
                        out byte r,
                        out byte a))
                {
                    return false;
                }

                if (a != 0)
                    hasNonZeroAlpha = true;

                int dst = dstRow + (x * 4);
                argb[dst] = b;
                argb[dst + 1] = g;
                argb[dst + 2] = r;
                argb[dst + 3] = a;
            }
        }

        // 1/4/8/24bpp (and some 32bpp) icons rely on the AND mask for transparency.
        if (!hasNonZeroAlpha || biBitCount < 32)
        {
            for (int y = 0; y < height; y++)
            {
                int srcRow = height - 1 - y;
                int dstRow = y * width * 4;

                for (int x = 0; x < width; x++)
                {
                    bool transparent = ReadAndMaskBit(icoBytes, andOffset, andStride, srcRow, x);
                    int dst = dstRow + (x * 4);
                    if (transparent)
                    {
                        argb[dst] = 0;
                        argb[dst + 1] = 0;
                        argb[dst + 2] = 0;
                        argb[dst + 3] = 0;
                    }
                    else if (argb[dst + 3] == 0)
                    {
                        argb[dst + 3] = 255;
                    }
                }
            }
        }

        return TryCreateBitmapFromArgb(argb, width, height, out bitmap);
    }

    private static bool TryReadXorPixel(
        byte[] data,
        int xorOffset,
        int colorTableOffset,
        int paletteEntries,
        int xorStride,
        ushort bitCount,
        int row,
        int x,
        out byte b,
        out byte g,
        out byte r,
        out byte a)
    {
        b = g = r = 0;
        a = 255;

        int rowOffset = xorOffset + (row * xorStride);

        switch (bitCount)
        {
            case 32:
            {
                int i = rowOffset + (x * 4);
                if (i + 3 >= data.Length)
                    return false;
                b = data[i];
                g = data[i + 1];
                r = data[i + 2];
                a = data[i + 3];
                return true;
            }
            case 24:
            {
                int i = rowOffset + (x * 3);
                if (i + 2 >= data.Length)
                    return false;
                b = data[i];
                g = data[i + 1];
                r = data[i + 2];
                a = 255;
                return true;
            }
            case 8:
            {
                int i = rowOffset + x;
                if (i >= data.Length)
                    return false;
                return TryReadPaletteColor(data, colorTableOffset, paletteEntries, data[i], out b, out g, out r);
            }
            case 4:
            {
                int i = rowOffset + (x / 2);
                if (i >= data.Length)
                    return false;
                int index = (x & 1) == 0 ? (data[i] >> 4) & 0xF : data[i] & 0xF;
                return TryReadPaletteColor(data, colorTableOffset, paletteEntries, index, out b, out g, out r);
            }
            case 1:
            {
                int i = rowOffset + (x / 8);
                if (i >= data.Length)
                    return false;
                int index = (data[i] >> (7 - (x & 7))) & 1;
                return TryReadPaletteColor(data, colorTableOffset, paletteEntries, index, out b, out g, out r);
            }
            default:
                return false;
        }
    }

    private static bool TryReadPaletteColor(
        byte[] data,
        int colorTableOffset,
        int paletteEntries,
        int index,
        out byte b,
        out byte g,
        out byte r)
    {
        b = g = r = 0;
        if (index < 0 || index >= paletteEntries)
            return false;

        int i = colorTableOffset + (index * 4);
        if (i + 2 >= data.Length)
            return false;

        b = data[i];
        g = data[i + 1];
        r = data[i + 2];
        return true;
    }

    private static bool ReadAndMaskBit(byte[] data, int andOffset, int andStride, int row, int x)
    {
        int i = andOffset + (row * andStride) + (x / 8);
        if (i < 0 || i >= data.Length)
            return false;

        // In ICO AND masks, set bits are transparent.
        return ((data[i] >> (7 - (x & 7))) & 1) != 0;
    }

    private static bool TryCreateBitmapFromArgb(byte[] argb, int width, int height, out nint bitmap)
    {
        bitmap = IntPtr.Zero;
        int stride = width * 4;

        int createResult = GdiPlus.GdipCreateBitmapFromScan0(
            width,
            height,
            stride,
            GdiPlus.PixelFormat32bppArgb,
            IntPtr.Zero,
            out bitmap);

        if (createResult != 0 || bitmap == IntPtr.Zero)
            return false;

        var rect = new GdiPlus.GpRect
        {
            X = 0,
            Y = 0,
            Width = width,
            Height = height,
        };

        var bitmapData = default(GdiPlus.BitmapData);
        int lockResult = GdiPlus.GdipBitmapLockBits(
            bitmap,
            ref rect,
            (uint)GdiPlus.ImageLockMode.Write,
            GdiPlus.PixelFormat32bppArgb,
            ref bitmapData);

        if (lockResult != 0 || bitmapData.Scan0 == IntPtr.Zero)
        {
            _ = GdiPlus.GdipDisposeImage(bitmap);
            bitmap = IntPtr.Zero;
            return false;
        }

        try
        {
            int destStride = Math.Abs(bitmapData.Stride);
            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(argb, y * stride, bitmapData.Scan0 + (y * destStride), stride);
            }
        }
        finally
        {
            _ = GdiPlus.GdipBitmapUnlockBits(bitmap, ref bitmapData);
        }

        return true;
    }

    private static bool TryMonochromeGpBitmap(nint sourceBitmap, uint argb, out nint monoBitmap)
    {
        monoBitmap = IntPtr.Zero;

        int widthResult = GdiPlus.GdipGetImageWidth(sourceBitmap, out uint width);
        int heightResult = GdiPlus.GdipGetImageHeight(sourceBitmap, out uint height);
        if (widthResult != 0 || heightResult != 0 || width == 0 || height == 0)
            return false;

        int cloneResult = GdiPlus.GdipCloneBitmapAreaI(
            0,
            0,
            (int)width,
            (int)height,
            GdiPlus.PixelFormat32bppArgb,
            sourceBitmap,
            out nint workingBitmap);

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
        {
            _ = GdiPlus.GdipDisposeImage(workingBitmap);
            return false;
        }

        try
        {
            ApplyMonochrome(bitmapData, argb);
        }
        finally
        {
            _ = GdiPlus.GdipBitmapUnlockBits(workingBitmap, ref bitmapData);
        }

        monoBitmap = workingBitmap;
        return true;
    }

    private static bool TryCreateMonochromeGpBitmap(byte[] imageBytes, TrayThemeMode themeMode, out nint gpBitmap)
    {
        return TryCreateMonochromeGpBitmap(imageBytes, ResolveArgb(themeMode), out gpBitmap);
    }

    private static bool TryCreateMonochromeGpBitmap(byte[] imageBytes, uint argb, out nint gpBitmap)
    {
        gpBitmap = IntPtr.Zero;

        if (imageBytes is null || imageBytes.Length == 0)
            return false;

        GdiPlus.EnsureInitialized();

        if (!TryLoadBitmapFromBytes(imageBytes, out nint sourceBitmap))
            return false;

        try
        {
            return TryMonochromeGpBitmap(sourceBitmap, argb, out gpBitmap);
        }
        finally
        {
            if (sourceBitmap != IntPtr.Zero)
                _ = GdiPlus.GdipDisposeImage(sourceBitmap);
        }
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

    private static bool TrySavePng(nint gpBitmap, out byte[] pngBytes)
    {
        pngBytes = new byte[0];
        IStream? stream = null;

        try
        {
            int createStreamResult = Ole32.CreateStreamOnHGlobal(IntPtr.Zero, true, out stream);
            if (createStreamResult != 0 || stream is null)
                return false;

            Guid pngClsid = GdiPlus.PngEncoderClsid;
            int saveResult = GdiPlus.GdipSaveImageToStream(gpBitmap, stream, ref pngClsid, IntPtr.Zero);
            if (saveResult != 0)
                return false;

            stream.Seek(0, 0, IntPtr.Zero);
            stream.Stat(out System.Runtime.InteropServices.ComTypes.STATSTG stat, 0);
            long size = stat.cbSize;
            if (size <= 0 || size > int.MaxValue)
                return false;

            pngBytes = new byte[(int)size];
            stream.Read(pngBytes, pngBytes.Length, IntPtr.Zero);
            return pngBytes.Length > 0;
        }
        finally
        {
            if (stream is not null)
                Marshal.ReleaseComObject(stream);
        }
    }

    private static void ApplyMonochrome(GdiPlus.BitmapData bitmapData, uint monochromeArgb)
    {
        int stride = Math.Abs(bitmapData.Stride);
        int width = (int)bitmapData.Width;
        int height = (int)bitmapData.Height;
        int rowBytes = width * 4;
        byte[] pixels = new byte[stride * height];
        Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);

        byte blue = (byte)(monochromeArgb & 0xFF);
        byte green = (byte)((monochromeArgb >> 8) & 0xFF);
        byte red = (byte)((monochromeArgb >> 16) & 0xFF);

        for (int row = 0; row < height; row++)
        {
            int rowOffset = row * stride;
            for (int offset = rowOffset; offset + 3 < rowOffset + rowBytes; offset += 4)
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
