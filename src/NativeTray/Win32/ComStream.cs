using System.Runtime.InteropServices;

namespace System.NativeTray.Win32;

/// <summary>
/// Calls IStream through its vtable so Native AOT does not need COM marshalling.
/// </summary>
internal static class ComStream
{
    private const int StreamSeekSet = 0;
    private const int StreamSeekEnd = 2;

    public static bool Write(nint stream, byte[] data)
    {
        var gch = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            var fn = Get<WriteDlg>(stream, 4);
            return fn(stream, gch.AddrOfPinnedObject(), data.Length, IntPtr.Zero) >= 0;
        }
        finally
        {
            gch.Free();
        }
    }

    public static bool SeekBegin(nint stream)
        => TrySeek(stream, 0, StreamSeekSet, out _);

    public static bool TryReadAll(nint stream, out byte[] bytes)
    {
        bytes = [];
        if (!TrySeek(stream, 0, StreamSeekEnd, out long size) || size <= 0 || size > int.MaxValue)
            return false;
        if (!TrySeek(stream, 0, StreamSeekSet, out _))
            return false;

        bytes = new byte[(int)size];
        var gch = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            var fn = Get<ReadDlg>(stream, 3);
            return fn(stream, gch.AddrOfPinnedObject(), bytes.Length, IntPtr.Zero) >= 0;
        }
        finally
        {
            gch.Free();
        }
    }

    private static bool TrySeek(nint stream, long move, int origin, out long position)
    {
        var buf = new long[1];
        var gch = GCHandle.Alloc(buf, GCHandleType.Pinned);
        try
        {
            var fn = Get<SeekDlg>(stream, 5);
            if (fn(stream, move, origin, gch.AddrOfPinnedObject()) < 0)
            {
                position = 0;
                return false;
            }

            position = buf[0];
            return true;
        }
        finally
        {
            gch.Free();
        }
    }

    private static T Get<T>(nint pUnk, int slot) where T : Delegate
    {
        nint vtbl = Marshal.ReadIntPtr(pUnk);
        nint fn = Marshal.ReadIntPtr(vtbl, slot * IntPtr.Size);
        return Marshal.GetDelegateForFunctionPointer<T>(fn);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int ReadDlg(nint self, nint pv, int cb, nint pcbRead);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int WriteDlg(nint self, nint pv, int cb, nint pcbWritten);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SeekDlg(nint self, long dlibMove, int dwOrigin, nint plibNewPosition);
}
