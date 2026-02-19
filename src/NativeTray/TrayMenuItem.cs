using System;

namespace NativeTray;

public class TrayMenuItem : ITrayMenuItemBase
{
    public TrayMenu? Menu { get; set; }

    /// <summary>
    /// Bitmap
    /// </summary>
    public object? Icon { get; set; }

    public string? Header { get; set; }

    public bool IsChecked { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsVisible { get; set; } = true;

    public bool IsBold { get; set; }

    public object? Tag { get; set; } = null;

    public Action<object?>? Command { get; set; }

    public object? CommandParameter { get; set; }
}
