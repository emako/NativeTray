using System.Runtime.InteropServices;

namespace System.NativeTray.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct POINT(int x, int y)
{
    public int X = x;
    public int Y = y;

    public void Offset(int dx, int dy)
    {
        X += dx;
        Y += dy;
    }
}
