using System.NativeTray.Win32;
using System.IO;

namespace System.NativeTray;

public class Win32Icon : IDisposable
{
    public nint Handle { get; private set; }

    public Win32Icon(Stream stream)
    {
        _ = stream ?? throw new ArgumentNullException(nameof(stream));

        using MemoryStream ms = new();
        stream.CopyTo(ms);
        byte[] bytes = ms.ToArray();

        // Parse ICONDIR (first 6 bytes)
        if (bytes.Length < 6)
            throw new InvalidDataException("Stream too short for ICONDIR.");

        ushort reserved = BitConverter.ToUInt16(bytes, 0); // Must be 0
        ushort type = BitConverter.ToUInt16(bytes, 2);     // 1 = ICON
        ushort count = BitConverter.ToUInt16(bytes, 4);    // Number of icons

        if (reserved != 0 || type != 1 || count == 0)
            throw new InvalidDataException("Invalid ICO header.");

        // Use only the first icon entry
        int entryOffset = 6;
        if (bytes.Length < entryOffset + 16)
            throw new InvalidDataException("Stream too short for ICONDIRENTRY.");

        uint imageSize = BitConverter.ToUInt32(bytes, entryOffset + 8);
        uint imageOffset = BitConverter.ToUInt32(bytes, entryOffset + 12);

        if (imageOffset + imageSize > bytes.Length)
            throw new InvalidDataException("Icon image out of bounds.");

        nint hIcon = User32.CreateIconFromResourceEx(
            ref bytes[imageOffset],
            imageSize,
            true,
            0x00030000,
            0, 0,
            0);

        if (hIcon == IntPtr.Zero)
            throw new InvalidOperationException("CreateIconFromResourceEx failed.");

        Handle = hIcon;
    }

    internal bool TryCreateMenuBitmap(out nint hBitmap, out bool shouldDisposeBitmap)
    {
        hBitmap = IntPtr.Zero;
        shouldDisposeBitmap = false;

        if (Handle == IntPtr.Zero)
            return false;

        if (!User32.GetIconInfo(Handle, out User32.ICONINFO iconInfo))
            return false;

        nint selectedBitmap = iconInfo.hbmColor != IntPtr.Zero ? iconInfo.hbmColor : iconInfo.hbmMask;
        if (selectedBitmap == IntPtr.Zero)
            return false;

        nint unusedBitmap = selectedBitmap == iconInfo.hbmColor ? iconInfo.hbmMask : iconInfo.hbmColor;
        if (unusedBitmap != IntPtr.Zero)
            _ = Gdi32.DeleteObject(unusedBitmap);

        hBitmap = selectedBitmap;
        shouldDisposeBitmap = true;
        return true;
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            _ = User32.DestroyIcon(Handle);
            Handle = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}
