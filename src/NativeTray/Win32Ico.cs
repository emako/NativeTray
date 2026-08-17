using System.Collections.Generic;
using System.IO;
using System.NativeTray.Win32;

namespace System.NativeTray;

/// <summary>
/// Parses multi-size ICO files and creates an <c>HICON</c> matched to the desired metrics.
/// </summary>
internal static class Win32Ico
{
    private const uint IconVersion = 0x00030000;

    public readonly struct IcoFrame
    {
        public IcoFrame(int width, int height, int imageOffset, int imageSize)
        {
            Width = width;
            Height = height;
            ImageOffset = imageOffset;
            ImageSize = imageSize;
        }

        public int Width { get; }
        public int Height { get; }
        public int ImageOffset { get; }
        public int ImageSize { get; }
    }

    public readonly struct BuiltFrame
    {
        public BuiltFrame(int width, int height, byte[] pngBytes)
        {
            Width = width;
            Height = height;
            PngBytes = pngBytes;
        }

        public int Width { get; }
        public int Height { get; }
        public byte[] PngBytes { get; }
    }

    public static void GetSmallIconSize(out int cx, out int cy)
    {
        cx = User32.GetSystemMetrics(User32.SM_CXSMICON);
        cy = User32.GetSystemMetrics(User32.SM_CYSMICON);
        if (cx <= 0)
            cx = 16;
        if (cy <= 0)
            cy = 16;
    }

    public static bool IsIco(byte[] bytes)
    {
        if (bytes is null || bytes.Length < 6)
            return false;

        ushort reserved = BitConverter.ToUInt16(bytes, 0);
        ushort type = BitConverter.ToUInt16(bytes, 2);
        ushort count = BitConverter.ToUInt16(bytes, 4);
        return reserved == 0 && type == 1 && count > 0;
    }

    public static bool IsPng(byte[] bytes, int offset)
    {
        return bytes.Length >= offset + 8
            && bytes[offset] == 0x89
            && bytes[offset + 1] == (byte)'P'
            && bytes[offset + 2] == (byte)'N'
            && bytes[offset + 3] == (byte)'G';
    }

    public static bool TryGetFrames(byte[] bytes, List<IcoFrame> frames)
    {
        frames.Clear();

        if (!IsIco(bytes))
            return false;

        ushort count = BitConverter.ToUInt16(bytes, 4);
        for (int i = 0; i < count; i++)
        {
            int entryOffset = 6 + (i * 16);
            if (bytes.Length < entryOffset + 16)
                return false;

            int width = bytes[entryOffset];
            int height = bytes[entryOffset + 1];
            if (width == 0)
                width = 256;
            if (height == 0)
                height = 256;

            uint imageSize = BitConverter.ToUInt32(bytes, entryOffset + 8);
            uint imageOffset = BitConverter.ToUInt32(bytes, entryOffset + 12);
            if (imageSize == 0 || imageOffset + imageSize > bytes.Length)
                continue;

            frames.Add(new IcoFrame(width, height, (int)imageOffset, (int)imageSize));
        }

        return frames.Count > 0;
    }

    /// <summary>
    /// Builds a multi-size ICO whose image payloads are PNG (works for any source frame format after conversion).
    /// </summary>
    public static byte[] BuildIco(IReadOnlyList<BuiltFrame> frames)
    {
        if (frames is null || frames.Count == 0)
            throw new ArgumentException("At least one frame is required.", nameof(frames));

        int count = frames.Count;
        int dataOffset = 6 + (16 * count);
        int totalSize = dataOffset;
        for (int i = 0; i < count; i++)
            totalSize += frames[i].PngBytes.Length;

        byte[] ico = new byte[totalSize];
        ico[0] = 0;
        ico[1] = 0;
        ico[2] = 1; // ICON
        ico[3] = 0;
        BitConverter.GetBytes((ushort)count).CopyTo(ico, 4);

        int imageOffset = dataOffset;
        for (int i = 0; i < count; i++)
        {
            BuiltFrame frame = frames[i];
            int entryOffset = 6 + (i * 16);
            int width = frame.Width;
            int height = frame.Height;
            byte[] png = frame.PngBytes;

            ico[entryOffset] = width >= 256 ? (byte)0 : (byte)width;
            ico[entryOffset + 1] = height >= 256 ? (byte)0 : (byte)height;
            ico[entryOffset + 2] = 0; // color count
            ico[entryOffset + 3] = 0; // reserved
            BitConverter.GetBytes((ushort)1).CopyTo(ico, entryOffset + 4);  // planes
            BitConverter.GetBytes((ushort)32).CopyTo(ico, entryOffset + 6); // bit count
            BitConverter.GetBytes(png.Length).CopyTo(ico, entryOffset + 8);
            BitConverter.GetBytes(imageOffset).CopyTo(ico, entryOffset + 12);

            Buffer.BlockCopy(png, 0, ico, imageOffset, png.Length);
            imageOffset += png.Length;
        }

        return ico;
    }

    public static nint CreateIconHandle(byte[] bytes, int desiredCx = 0, int desiredCy = 0)
    {
        if (!TryCreateIconHandle(bytes, out nint hIcon, desiredCx, desiredCy))
            throw new InvalidOperationException("CreateIconFromResourceEx failed.");

        return hIcon;
    }

    public static bool TryCreateIconHandle(byte[] bytes, out nint hIcon, int desiredCx = 0, int desiredCy = 0)
    {
        hIcon = IntPtr.Zero;

        if (bytes is null || bytes.Length == 0)
            return false;

        if (desiredCx <= 0 || desiredCy <= 0)
            GetSmallIconSize(out desiredCx, out desiredCy);

        if (!TrySelectBestImage(bytes, desiredCx, desiredCy, out uint imageOffset, out uint imageSize))
            return false;

        hIcon = User32.CreateIconFromResourceEx(
            ref bytes[imageOffset],
            imageSize,
            fIcon: true,
            IconVersion,
            desiredCx,
            desiredCy,
            uFlags: 0);

        return hIcon != IntPtr.Zero;
    }

    public static bool TrySelectBestImage(
        byte[] bytes,
        int desiredCx,
        int desiredCy,
        out uint imageOffset,
        out uint imageSize)
    {
        imageOffset = 0;
        imageSize = 0;

        if (!IsIco(bytes))
            return false;

        ushort count = BitConverter.ToUInt16(bytes, 4);
        int bestWidth = 0;
        int bestHeight = 0;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            int entryOffset = 6 + (i * 16);
            if (bytes.Length < entryOffset + 16)
                return false;

            int width = bytes[entryOffset];
            int height = bytes[entryOffset + 1];
            if (width == 0)
                width = 256;
            if (height == 0)
                height = 256;

            uint size = BitConverter.ToUInt32(bytes, entryOffset + 8);
            uint offset = BitConverter.ToUInt32(bytes, entryOffset + 12);
            if (size == 0 || offset + size > bytes.Length)
                continue;

            if (!found || IsBetterSize(width, height, bestWidth, bestHeight, desiredCx, desiredCy))
            {
                found = true;
                bestWidth = width;
                bestHeight = height;
                imageOffset = offset;
                imageSize = size;
            }
        }

        return found;
    }

    /// <summary>
    /// Prefers an exact match, then the smallest size that still covers the desired metrics,
    /// then the largest smaller size. Used only when creating a single <c>HICON</c> for the tray.
    /// </summary>
    private static bool IsBetterSize(int width, int height, int bestWidth, int bestHeight, int desiredCx, int desiredCy)
    {
        bool exact = width == desiredCx && height == desiredCy;
        bool bestExact = bestWidth == desiredCx && bestHeight == desiredCy;
        if (exact != bestExact)
            return exact;

        bool covers = width >= desiredCx && height >= desiredCy;
        bool bestCovers = bestWidth >= desiredCx && bestHeight >= desiredCy;
        if (covers != bestCovers)
            return covers;

        int area = width * height;
        int bestArea = bestWidth * bestHeight;
        return covers
            ? area < bestArea
            : area > bestArea;
    }
}
