namespace System.NativeTray;

/// <summary>
/// Defines a set of standardized icons that can be associated with a ToolTip.
/// </summary>
public enum TrayToolTipIcon
{
    /// <summary>
    /// Not a standard icon.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// An information icon.
    /// </summary>
    Info = 0x01,

    /// <summary>
    /// A warning icon.
    /// </summary>
    Warning = 0x02,

    /// <summary>
    /// An error icon.
    /// </summary>
    Error = 0x03,
}
