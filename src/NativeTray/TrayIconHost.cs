using NativeTray.Win32;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NativeTray;

/// <summary>
/// Manages a Win32 tray icon and its context menu for WPF applications.
/// </summary>
public partial class TrayIconHost : IDisposable
{
    /// <summary>
    /// Handle to the hidden window hosting the tray icon.
    /// </summary>
    private readonly nint hWnd = IntPtr.Zero;

    /// <summary>
    /// Delegate for window procedure.
    /// </summary>
    private readonly User32.WndProcDelegate wndProcDelegate = null!;

    /// <summary>
    /// Data structure for the tray icon.
    /// </summary>
    private Shell32.NotifyIconData notifyIconData = default;

    /// <summary>
    /// Unique ID for this tray icon.
    /// </summary>
    private readonly int id = default;

    /// <summary>
    /// Static counter for unique IDs.
    /// </summary>
    private static int nextId = 0;

    /// <summary>
    /// Windows message ID for TaskbarCreated broadcast.
    /// </summary>
    private readonly uint taskbarCreatedMessageId = 0;

    /// <summary>
    /// ???
    /// </summary>
    public TrayThemeMode ThemeMode
    {
        get => field;
        set => SetThemeMode(field = value);
    } = TrayThemeMode.None;

    /// <summary>
    /// Tooltip text for the tray icon.
    /// </summary>
    public string ToolTipText
    {
        get => notifyIconData.szTip;
        set
        {
            notifyIconData.szTip = value;
            notifyIconData.uFlags |= (int)Shell32.NotifyIconFlags.NIF_TIP;
            _ = Shell32.Shell_NotifyIcon((int)Shell32.NOTIFY_COMMAND.NIM_MODIFY, ref notifyIconData);
        }
    }

    /// <summary>
    /// Handle to the icon image.
    /// </summary>
    public nint Icon
    {
        get => notifyIconData.hIcon;
        set
        {
            if (notifyIconData.hIcon != IntPtr.Zero)
                _ = User32.DestroyIcon(notifyIconData.hIcon);
            notifyIconData.hIcon = User32.CopyIcon(value);
            notifyIconData.uFlags |= (int)Shell32.NotifyIconFlags.NIF_ICON;
            _ = Shell32.Shell_NotifyIcon((int)Shell32.NOTIFY_COMMAND.NIM_MODIFY, ref notifyIconData);
        }
    }

    /// <summary>
    /// Whether the tray icon is visible.
    /// </summary>
    public bool IsVisible
    {
        get => notifyIconData.dwState != (uint)Shell32.NotifyIconState.NIS_HIDDEN;
        set
        {
            notifyIconData.dwState = value ? 0 : (uint)Shell32.NotifyIconState.NIS_HIDDEN;
            notifyIconData.dwStateMask = (uint)(Shell32.NotifyIconState.NIS_HIDDEN | Shell32.NotifyIconState.NIS_SHAREDICON);
            notifyIconData.uFlags |= (int)Shell32.NotifyIconFlags.NIF_STATE;
            _ = Shell32.Shell_NotifyIcon((int)Shell32.NOTIFY_COMMAND.NIM_MODIFY, ref notifyIconData);
        }
    }

    /// <summary>
    /// Balloon tip text.
    /// </summary>
    public string BalloonTipText
    {
        get => field;
        set
        {
            if (value != field)
            {
                field = value;
            }
        }
    } = string.Empty;

    /// <summary>
    /// Balloon tip icon type.
    /// </summary>
    public ToolTipIcon BalloonTipIcon
    {
        get => field;
        set
        {
            if ((int)value < 0 || (int)value > 3)
            {
                throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ToolTipIcon));
            }

            if (value != field)
            {
                field = value;
            }
        }
    }

    /// <summary>
    /// Balloon tip title.
    /// </summary>
    public string BalloonTipTitle
    {
        get => field;
        set
        {
            if (value != field)
            {
                field = value;
            }
        }
    } = string.Empty;

    /// <summary>
    /// User-defined tag.
    /// </summary>
    public object? Tag { get; set; } = null;

    /// <summary>
    /// Context menu for the tray icon.
    /// </summary>
    public TrayMenu Menu { get; set; } = null!;

    /// <summary>
    /// ???
    /// </summary>
    public event EventHandler<EventArgs>? UserPreferenceChanged = null;

    /// <summary>
    /// Occurs when the balloon tip is clicked.
    /// </summary>
    public event EventHandler<EventArgs>? BalloonTipClicked = null;

    /// <summary>
    /// Occurs when the balloon tip is closed.
    /// </summary>
    public event EventHandler<EventArgs>? BalloonTipClosed = null;

    /// <summary>
    /// Occurs when the balloon tip is shown.
    /// </summary>
    public event EventHandler<EventArgs>? BalloonTipShown = null;

    /// <summary>
    /// Occurs when the tray icon is clicked.
    /// </summary>
    public event EventHandler<EventArgs>? Click = null;

    /// <summary>
    /// Occurs when the right mouse button is pressed down on the tray icon.
    /// </summary>
    public event EventHandler<EventArgs>? RightDown = null;

    /// <summary>
    /// Occurs when the right mouse button is clicked on the tray icon.
    /// </summary>
    public event EventHandler<EventArgs>? RightClick = null;

    /// <summary>
    /// Occurs when the right mouse button is double-clicked on the tray icon.
    /// </summary>
    public event EventHandler<EventArgs>? RightDoubleClick = null;

    /// <summary>
    /// Occurs when the left mouse button is pressed down on the tray icon.
    /// </summary>
    public event EventHandler<EventArgs>? LeftDown = null;

    /// <summary>
    /// Occurs when the left mouse button is clicked on the tray icon.
    /// </summary>
    public event EventHandler<EventArgs>? LeftClick = null;

    /// <summary>
    /// Occurs when the left mouse button is double-clicked on the tray icon.
    /// </summary>
    public event EventHandler<EventArgs>? LeftDoubleClick = null;

    /// <summary>
    /// Occurs when the middle mouse button is pressed down on the tray icon.
    /// </summary>
    public event EventHandler<EventArgs>? MiddleDown = null;

    /// <summary>
    /// Occurs when the middle mouse button is clicked on the tray icon.
    /// </summary>
    public event EventHandler<EventArgs>? MiddleClick = null;

    /// <summary>
    /// Occurs when the middle mouse button is double-clicked on the tray icon.
    /// </summary>
    public event EventHandler<EventArgs>? MiddleDoubleClick = null;

    public TrayIconHost()
    {
        id = ++nextId;

        // Register for TaskbarCreated message to handle Explorer restarts
        taskbarCreatedMessageId = User32.RegisterWindowMessage("TaskbarCreated");

        wndProcDelegate = new User32.WndProcDelegate(WndProc);

        User32.WNDCLASS wc = new()
        {
            lpszClassName = "TrayIconHostWindowClass",
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate)
        };
        User32.RegisterClass(ref wc);

        hWnd = User32.CreateWindowEx(0, "TrayIconHostWindowClass", "TrayIconHostWindow", 0, 0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        notifyIconData = new Shell32.NotifyIconData()
        {
            cbSize = Marshal.SizeOf<Shell32.NotifyIconData>(),
            hWnd = hWnd,
            uID = id,
            uFlags = (int)(Shell32.NotifyIconFlags.NIF_ICON | Shell32.NotifyIconFlags.NIF_MESSAGE | Shell32.NotifyIconFlags.NIF_TIP),
            uCallbackMessage = (int)User32.WindowMessage.WM_TRAYICON,
            hIcon = IntPtr.Zero,
            szTip = null!,
        };

        _ = Shell32.Shell_NotifyIcon((int)Shell32.NOTIFY_COMMAND.NIM_ADD, ref notifyIconData);
    }

    protected virtual nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        // Handle TaskbarCreated message to re-register tray icon after Explorer restart
        if (msg == taskbarCreatedMessageId)
        {
            // Re-add the tray icon
            _ = Shell32.Shell_NotifyIcon((int)Shell32.NOTIFY_COMMAND.NIM_ADD, ref notifyIconData);
            return IntPtr.Zero;
        }

        if (msg == (uint)User32.WindowMessage.WM_TRAYICON)
        {
            if ((int)wParam == id)
            {
                User32.WindowMessage mouseMsg = (User32.WindowMessage)lParam;

                switch (mouseMsg)
                {
                    case User32.WindowMessage.WM_QUERYENDSESSION:
                    case User32.WindowMessage.WM_ENDSESSION:
                        _ = Shell32.Shell_NotifyIcon((int)Shell32.NOTIFY_COMMAND.NIM_DELETE, ref notifyIconData);
                        break;

                    case User32.WindowMessage.WM_LBUTTONDOWN:
                        LeftDown?.Invoke(this, EventArgs.Empty);
                        break;

                    case User32.WindowMessage.WM_LBUTTONUP:
                        LeftClick?.Invoke(this, EventArgs.Empty);
                        Click?.Invoke(this, EventArgs.Empty);
                        break;

                    case User32.WindowMessage.WM_LBUTTONDBLCLK:
                        LeftDoubleClick?.Invoke(this, EventArgs.Empty);
                        break;

                    case User32.WindowMessage.WM_RBUTTONDOWN:
                        RightDown?.Invoke(this, EventArgs.Empty);
                        break;

                    case User32.WindowMessage.WM_RBUTTONUP:
                        RightClick?.Invoke(this, EventArgs.Empty);
                        ShowContextMenu();
                        break;

                    case User32.WindowMessage.WM_RBUTTONDBLCLK:
                        RightDoubleClick?.Invoke(this, EventArgs.Empty);
                        break;

                    case User32.WindowMessage.WM_MBUTTONDOWN:
                        MiddleDown?.Invoke(this, EventArgs.Empty);
                        break;

                    case User32.WindowMessage.WM_MBUTTONUP:
                        MiddleClick?.Invoke(this, EventArgs.Empty);
                        Click?.Invoke(this, EventArgs.Empty);
                        break;

                    case User32.WindowMessage.WM_MBUTTONDBLCLK:
                        MiddleDoubleClick?.Invoke(this, EventArgs.Empty);
                        break;

                    case User32.WindowMessage.WM_NOTIFYICON_BALLOONSHOW:
                        BalloonTipShown?.Invoke(this, EventArgs.Empty);
                        break;

                    case User32.WindowMessage.WM_NOTIFYICON_BALLOONHIDE:
                        BalloonTipClosed?.Invoke(this, EventArgs.Empty);
                        break;

                    case User32.WindowMessage.WM_NOTIFYICON_BALLOONTIMEOUT:
                        BalloonTipClosed?.Invoke(this, EventArgs.Empty);
                        break;

                    case User32.WindowMessage.WM_NOTIFYICON_BALLOONUSERCLICK:
                        BalloonTipClicked?.Invoke(this, EventArgs.Empty);
                        break;
                }
            }
        }
        else if (msg == (uint)User32.WindowMessage.WM_SETTINGCHANGE)
        {
            if (ThemeMode != TrayThemeMode.None)
            {
                string? area = Marshal.PtrToStringUni(lParam);

                if (string.Equals(area, "ImmersiveColorSet", StringComparison.Ordinal))
                {
                    UserPreferenceChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        else if (msg == (uint)User32.WindowMessage.WM_THEMECHANGED)
        {
            if (ThemeMode != TrayThemeMode.None)
            {
                UserPreferenceChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        return User32.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    /// <summary>
    /// Shows the context menu associated with the tray icon.
    /// </summary>
    public virtual void ShowContextMenu()
    {
        Menu?.Open(hWnd);
    }

    /// <summary>
    /// Shows a balloon tip with the specified timeout.
    /// </summary>
    public virtual void ShowBalloonTip(int timeout)
    {
        ShowBalloonTip(timeout, BalloonTipTitle, BalloonTipText, BalloonTipIcon);
    }

    /// <summary>
    /// Shows a balloon tip with the specified parameters.
    /// </summary>
    public virtual void ShowBalloonTip(int timeout, string tipTitle, string tipText, ToolTipIcon tipIcon)
    {
        if (timeout < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        if (string.IsNullOrEmpty(tipText))
        {
            throw new ArgumentException("NotifyIconEmptyOrNullTipText");
        }

        if ((int)tipIcon < 0 || (int)tipIcon > 3)
        {
            throw new InvalidEnumArgumentException(nameof(tipIcon), (int)tipIcon, typeof(ToolTipIcon));
        }

        var notifyIconData = new Shell32.NotifyIconData()
        {
            cbSize = Marshal.SizeOf<Shell32.NotifyIconData>(),
            hWnd = hWnd,
            uID = id,
            uFlags = (int)Shell32.NotifyIconFlags.NIF_INFO,
            uTimeoutOrVersion = (uint)timeout,
            szInfoTitle = tipTitle,
            szInfo = tipText,
            dwInfoFlags = tipIcon switch
            {
                ToolTipIcon.Info => 1,
                ToolTipIcon.Warning => 2,
                ToolTipIcon.Error => 3,
                ToolTipIcon.None or _ => 0,
            },
        };

        _ = Shell32.Shell_NotifyIcon((int)Shell32.NOTIFY_COMMAND.NIM_MODIFY, ref notifyIconData);
    }

    /// <summary>
    /// Disposes the tray icon and its resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Remove tray icon
            _ = Shell32.Shell_NotifyIcon((int)Shell32.NOTIFY_COMMAND.NIM_DELETE, ref notifyIconData);

            // Clean up icon resources
            if (notifyIconData.hIcon != IntPtr.Zero)
            {
                _ = User32.DestroyIcon(notifyIconData.hIcon);
                notifyIconData.hIcon = IntPtr.Zero;
            }

            // Destroy window
            if (hWnd != IntPtr.Zero)
            {
                _ = User32.DestroyWindow(hWnd);
            }
        }
    }

    ~TrayIconHost()
    {
        Dispose(false);
    }
}
