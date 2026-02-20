namespace System.NativeTray;

/// <summary>
/// Represents a separator line in a tray context menu.
/// </summary>
public sealed class TraySeparator : ITrayMenuItemBase
{
    /// <summary>
    /// Separators do not have submenus.
    /// </summary>
    public TrayMenu? Menu
    {
        get => null;
        set => throw new NotImplementedException();
    }

    /// <summary>
    /// Separators do not have icons.
    /// </summary>
    public object? Icon
    {
        get => null;
        set => throw new NotImplementedException();
    }

    public string? Header
    {
        get => "-";
        set => throw new NotImplementedException();
    }

    public bool IsVisible
    {
        get => true;
        set => throw new NotImplementedException();
    }

    public bool IsChecked
    {
        get => false;
        set => throw new NotImplementedException();
    }

    public bool IsEnabled
    {
        get => false;
        set => throw new NotImplementedException();
    }

    public bool IsBold
    {
        get => false;
        set => throw new NotImplementedException();
    }

    public Action<object?>? Command
    {
        get => null;
        set => throw new NotImplementedException();
    }

    public object? CommandParameter
    {
        get => null;
        set => throw new NotImplementedException();
    }

    public object? Tag { get; set; } = null;
}
