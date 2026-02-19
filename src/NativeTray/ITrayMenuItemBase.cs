using System;

namespace NativeTray;

public interface ITrayMenuItemBase
{
    public TrayMenu? Menu { get; set; }

    /// <summary>
    /// Bitmap
    /// </summary>
    public object? Icon { get; set; }

    public string? Header { get; set; }

    public bool IsVisible { get; set; }

    public bool IsChecked { get; set; }

    public bool IsEnabled { get; set; }

    public bool IsBold { get; set; }

    public object? Tag { get; set; }

    public Action<object?>? Command { get; set; }

    public object? CommandParameter { get; set; }
}
