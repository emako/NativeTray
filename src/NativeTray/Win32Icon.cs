using Microsoft.Win32.SafeHandles;
using System.IO;
using System.NativeTray.Win32;

namespace System.NativeTray;

public class Win32Icon : IDisposable
{
    private readonly byte[] _iconBytes;
    private readonly SafeHIconHandle _handle = new();
    private bool _showAsMonochrome;
    private TrayThemeMode _themeMode = TrayThemeMode.System;
    private nint _monoIconOnDark;
    private nint _monoIconOnLight;
    private bool _hasAppliedMonochromeArgb;
    private uint _appliedMonochromeArgb;

    public SafeHIconHandle SafeHandle => _handle;

    /// <summary>
    /// Gets the icon handle. When <see cref="ShowAsMonochrome"/> is enabled, this is a
    /// theme-aware monochrome <c>HICON</c> suitable for <see cref="TrayIconHost.IconSource"/>.
    /// </summary>
    public nint Handle => TryGetHandle(out nint handle) ? handle : IntPtr.Zero;

    /// <summary>
    /// Occurs after the underlying <c>HICON</c> is rebuilt (for example when the system theme changes).
    /// </summary>
    public event EventHandler? HandleChanged;

    /// <summary>
    /// Gets or sets whether the icon should be rendered as monochrome.
    /// Affects both <see cref="Handle"/> (tray) and menu bitmaps.
    /// When set, <see cref="MonoIconOnDark"/> / <see cref="MonoIconOnLight"/> are preferred over GDI conversion.
    /// </summary>
    public bool ShowAsMonochrome
    {
        get => _showAsMonochrome;
        set
        {
            if (_showAsMonochrome == value)
                return;

            _showAsMonochrome = value;
            RebuildHandle();
        }
    }

    /// <summary>
    /// Gets or sets the theme mode used when rendering as monochrome.
    /// Use <see cref="TrayThemeMode.System"/> with <see cref="TrayIconHost.IconSource"/> to follow OS light/dark changes.
    /// </summary>
    public TrayThemeMode ThemeMode
    {
        get => _themeMode;
        set
        {
            if (_themeMode == value)
                return;

            _themeMode = value;
            if (_showAsMonochrome)
                RebuildHandle();
        }
    }

    /// <summary>
    /// Optional <c>HICON</c> used when <see cref="ShowAsMonochrome"/> is enabled and the effective theme is dark.
    /// When non-zero, this handle is copied as-is and GDI monochrome conversion is skipped for that theme.
    /// The caller owns the handle; it is not destroyed by this <see cref="Win32Icon"/>.
    /// </summary>
    public nint MonoIconOnDark
    {
        get => _monoIconOnDark;
        set
        {
            if (_monoIconOnDark == value)
                return;

            _monoIconOnDark = value;
            if (_showAsMonochrome)
                RebuildHandle();
        }
    }

    /// <summary>
    /// Optional <c>HICON</c> used when <see cref="ShowAsMonochrome"/> is enabled and the effective theme is light.
    /// When non-zero, this handle is copied as-is and GDI monochrome conversion is skipped for that theme.
    /// The caller owns the handle; it is not destroyed by this <see cref="Win32Icon"/>.
    /// </summary>
    public nint MonoIconOnLight
    {
        get => _monoIconOnLight;
        set
        {
            if (_monoIconOnLight == value)
                return;

            _monoIconOnLight = value;
            if (_showAsMonochrome)
                RebuildHandle();
        }
    }

    public Win32Icon(Stream stream)
    {
        _ = stream ?? throw new ArgumentNullException(nameof(stream));

        using MemoryStream ms = new();
        stream.CopyTo(ms);
        _iconBytes = ms.ToArray();

        nint hIcon = CreateIconHandleFromBytes(_iconBytes);
        _handle = new SafeHIconHandle(hIcon);
    }

    public Win32Icon(byte[] iconBytes)
    {
        _ = iconBytes ?? throw new ArgumentNullException(nameof(iconBytes));
        if (iconBytes.Length == 0)
            throw new ArgumentException("Icon bytes cannot be empty.", nameof(iconBytes));

        _iconBytes = (byte[])iconBytes.Clone();
        nint hIcon = CreateIconHandleFromBytes(_iconBytes);
        _handle = new SafeHIconHandle(hIcon);
    }

    /// <summary>
    /// Rebuilds the monochrome handle when <see cref="ThemeMode"/> is <see cref="TrayThemeMode.System"/>
    /// and the effective system colors have changed.
    /// </summary>
    /// <returns><see langword="true"/> when the handle was rebuilt.</returns>
    public bool RefreshMonochromeForSystemTheme()
    {
        if (!_showAsMonochrome || _themeMode != TrayThemeMode.System)
            return false;

        uint argb = Win32Monochrome.ResolveArgb(TrayThemeMode.System);
        if (_hasAppliedMonochromeArgb && argb == _appliedMonochromeArgb)
            return false;

        RebuildHandle();
        return true;
    }

    private void RebuildHandle()
    {
        nint hIcon;
        if (_showAsMonochrome)
        {
            if (!TryCopyMonochromeOverrideHandle(out hIcon)
                && !Win32Monochrome.TryCreateMonochromeIcon(_iconBytes, _themeMode, out hIcon))
            {
                hIcon = CreateIconHandleFromBytes(_iconBytes);
            }

            _appliedMonochromeArgb = Win32Monochrome.ResolveArgb(_themeMode);
            _hasAppliedMonochromeArgb = true;
        }
        else
        {
            hIcon = CreateIconHandleFromBytes(_iconBytes);
            _hasAppliedMonochromeArgb = false;
        }

        _handle.ReplaceHandle(hIcon);
        HandleChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool TryCopyMonochromeOverrideHandle(out nint hIcon)
    {
        hIcon = IntPtr.Zero;

        if (!TryGetMonochromeOverride(out nint sourceHandle) || sourceHandle == IntPtr.Zero)
            return false;

        // Do not copy our own live handle — ReplaceHandle would destroy it.
        if (TryGetHandle(out nint currentHandle) && sourceHandle == currentHandle)
            return false;

        hIcon = User32.CopyIcon(sourceHandle);
        return hIcon != IntPtr.Zero;
    }

    private bool TryGetMonochromeOverride(out nint sourceHandle)
    {
        sourceHandle = IntPtr.Zero;

        TrayThemeMode effective = _themeMode;
        if (effective == TrayThemeMode.System)
            effective = OSThemeHelper.SystemUsesDarkTheme() ? TrayThemeMode.Dark : TrayThemeMode.Light;

        if (effective == TrayThemeMode.Dark)
            sourceHandle = _monoIconOnDark;
        else if (effective == TrayThemeMode.Light)
            sourceHandle = _monoIconOnLight;

        return sourceHandle != IntPtr.Zero;
    }

    private static nint CreateIconHandleFromBytes(byte[] bytes)
    {
        if (bytes.Length == 0)
            throw new InvalidDataException("Icon stream is empty.");

        if (!Win32Ico.IsIco(bytes))
            throw new InvalidDataException("Invalid ICO header.");

        return Win32Ico.CreateIconHandle(bytes);
    }

    internal bool TryCreateMenuBitmap(out nint hBitmap, out bool shouldDisposeBitmap)
    {
        hBitmap = IntPtr.Zero;
        shouldDisposeBitmap = false;

        // Prefer the current handle (override or GDI-built mono) so menu icons match the tray.
        if (TryCreateBitmapFromHandle(out hBitmap))
        {
            shouldDisposeBitmap = true;
            return true;
        }

        if (ShowAsMonochrome
            && Win32Monochrome.TryCreateMonochromeBitmap(_iconBytes, ThemeMode, out hBitmap))
        {
            shouldDisposeBitmap = true;
            return true;
        }

        return false;
    }

    private bool TryCreateBitmapFromHandle(out nint hBitmap)
    {
        hBitmap = IntPtr.Zero;

        if (!TryGetHandle(out nint hIcon))
            return false;

        if (!User32.GetIconInfo(hIcon, out User32.ICONINFO iconInfo))
            return false;

        nint selectedBitmap = iconInfo.hbmColor != IntPtr.Zero ? iconInfo.hbmColor : iconInfo.hbmMask;
        if (selectedBitmap == IntPtr.Zero)
            return false;

        nint unusedBitmap = selectedBitmap == iconInfo.hbmColor ? iconInfo.hbmMask : iconInfo.hbmColor;
        if (unusedBitmap != IntPtr.Zero)
            _ = Gdi32.DeleteObject(unusedBitmap);

        hBitmap = selectedBitmap;
        return true;
    }

    private bool TryGetHandle(out nint handle)
    {
        handle = IntPtr.Zero;

        if (_handle.IsClosed || _handle.IsInvalid)
            return false;

        handle = _handle.DangerousGetHandle();
        return handle != IntPtr.Zero;
    }

    public void Dispose()
    {
        HandleChanged = null;
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}

public sealed class SafeHIconHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeHIconHandle() : base(true)
    {
    }

    public SafeHIconHandle(nint preexistingHandle, bool ownsHandle = true) : base(ownsHandle)
    {
        SetHandle(preexistingHandle);
    }

    internal void ReplaceHandle(nint newHandle)
    {
        nint oldHandle = handle;
        SetHandle(newHandle);

        if (oldHandle != IntPtr.Zero && oldHandle != newHandle)
            _ = User32.DestroyIcon(oldHandle);
    }

    protected override bool ReleaseHandle()
    {
        return User32.DestroyIcon(handle) != 0;
    }
}
