using System;

namespace System.NativeTray;

/// <summary>
/// Defines the interface for a tray menu item.
/// </summary>
public interface ITrayMenuItemBase
{
    /// <summary>
    /// Gets or sets the submenu associated with this menu item.
    /// </summary>
    public TrayMenu? Menu { get; set; }

    /// <summary>
    /// Gets or sets the icon displayed next to the menu item.
    /// </summary>
    public object? Icon { get; set; }

    /// <summary>
    /// Gets or sets the text displayed for the menu item.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the menu item is visible.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the menu item is checked.
    /// </summary>
    public bool IsChecked { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the menu item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the menu item is displayed in bold.
    /// </summary>
    public bool IsBold { get; set; }

    /// <summary>
    /// Gets or sets a user-defined tag object.
    /// </summary>
    public object? Tag { get; set; }

    /// <summary>
    /// Gets or sets the command to execute when the menu item is clicked.
    /// </summary>
    public Action<object?>? Command { get; set; }

    /// <summary>
    /// Gets or sets the parameter to pass to the command.
    /// </summary>
    public object? CommandParameter { get; set; }
}
