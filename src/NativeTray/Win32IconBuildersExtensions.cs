namespace System.NativeTray;

/// <summary>
/// Extension entry points for constructing <see cref="Win32Icon"/> instances with a fluent builder API.
/// </summary>
public static class Win32IconBuildersExtensions
{
    /// <summary>
    /// Creates a <see cref="Win32IconBuilder"/> from a raw base64 string or a browser-style data URL.
    /// </summary>
    /// <param name="base64OrDataUrl">Icon content in base64 or <c>data:image/...;base64,...</c> format.</param>
    /// <returns>A configured <see cref="Win32IconBuilder"/> instance.</returns>
    public static Win32IconBuilder CreateWin32IconBuilder(this string base64OrDataUrl)
    {
        return new Win32IconBuilder().FromBase64(base64OrDataUrl);
    }

    /// <summary>
    /// Creates a <see cref="Win32IconBuilder"/> from raw icon bytes.
    /// </summary>
    /// <param name="iconBytes">Binary icon data.</param>
    /// <returns>A configured <see cref="Win32IconBuilder"/> instance.</returns>
    public static Win32IconBuilder CreateWin32IconBuilder(this byte[] iconBytes)
    {
        return new Win32IconBuilder().FromBytes(iconBytes);
    }

    /// <summary>
    /// Fluent builder for creating <see cref="Win32Icon"/> objects.
    /// </summary>
    public sealed class Win32IconBuilder
    {
        private byte[]? _iconBytes;

        /// <summary>
        /// Sets the icon source from raw bytes.
        /// </summary>
        /// <param name="iconBytes">Binary icon data.</param>
        /// <returns>The current builder instance.</returns>
        public Win32IconBuilder FromBytes(byte[] iconBytes)
        {
            _ = iconBytes ?? throw new ArgumentNullException(nameof(iconBytes));
            if (iconBytes.Length == 0)
                throw new ArgumentException("Icon bytes cannot be empty.", nameof(iconBytes));

            _iconBytes = (byte[])iconBytes.Clone();
            return this;
        }

        /// <summary>
        /// Sets the icon source from a raw base64 string or a data URL.
        /// </summary>
        /// <param name="base64">Raw base64 text or <c>data:image/...;base64,...</c> content.</param>
        /// <returns>The current builder instance.</returns>
        public Win32IconBuilder FromBase64(string base64)
        {
            _iconBytes = Base64Converter.DecodeBase64(base64);
            return this;
        }

        /// <summary>
        /// Sets the icon source from a browser-style data URL.
        /// </summary>
        /// <param name="dataUrl">Icon data URL, for example <c>data:image/x-icon;base64,...</c>.</param>
        /// <returns>The current builder instance.</returns>
        public Win32IconBuilder FromDataUrl(string dataUrl)
        {
            _iconBytes = Base64Converter.DecodeBase64(dataUrl);
            return this;
        }

        /// <summary>
        /// Builds a new <see cref="Win32Icon"/> instance.
        /// </summary>
        /// <returns>A newly created <see cref="Win32Icon"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no source data was configured.</exception>
        public Win32Icon Build()
        {
            if (_iconBytes is null)
                throw new InvalidOperationException("Icon source is not configured. Call FromBytes, FromBase64, or FromDataUrl first.");

            return new Win32Icon(_iconBytes);
        }
    }
}
