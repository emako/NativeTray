namespace System.NativeTray;

/// <summary>
/// Extension entry points for constructing <see cref="Win32Image"/> instances with a fluent builder API.
/// </summary>
public static class Win32ImageBuildersExtensions
{
    /// <summary>
    /// Creates a <see cref="Win32ImageBuilder"/> from a raw base64 string or a browser-style data URL.
    /// </summary>
    /// <param name="base64OrDataUrl">Image content in base64 or <c>data:image/...;base64,...</c> format.</param>
    /// <returns>A configured <see cref="Win32ImageBuilder"/> instance.</returns>
    public static Win32ImageBuilder CreateWin32ImageBuilder(this string base64OrDataUrl)
    {
        return new Win32ImageBuilder().FromBase64(base64OrDataUrl);
    }

    /// <summary>
    /// Creates a <see cref="Win32ImageBuilder"/> from raw image bytes.
    /// </summary>
    /// <param name="imageBytes">Binary image data.</param>
    /// <returns>A configured <see cref="Win32ImageBuilder"/> instance.</returns>
    public static Win32ImageBuilder CreateWin32ImageBuilder(this byte[] imageBytes)
    {
        return new Win32ImageBuilder().FromBytes(imageBytes);
    }

    /// <summary>
    /// Fluent builder for creating <see cref="Win32Image"/> objects.
    /// </summary>
    public sealed class Win32ImageBuilder
    {
        private byte[]? _imageBytes;

        /// <summary>
        /// Sets the image source from raw bytes.
        /// </summary>
        /// <param name="imageBytes">Binary image data.</param>
        /// <returns>The current builder instance.</returns>
        public Win32ImageBuilder FromBytes(byte[] imageBytes)
        {
            _ = imageBytes ?? throw new ArgumentNullException(nameof(imageBytes));
            if (imageBytes.Length == 0)
                throw new ArgumentException("Image bytes cannot be empty.", nameof(imageBytes));

            _imageBytes = (byte[])imageBytes.Clone();
            return this;
        }

        /// <summary>
        /// Sets the image source from a raw base64 string or a data URL.
        /// </summary>
        /// <param name="base64">Raw base64 text or <c>data:image/...;base64,...</c> content.</param>
        /// <returns>The current builder instance.</returns>
        public Win32ImageBuilder FromBase64(string base64)
        {
            _imageBytes = Base64Converter.DecodeBase64(base64);
            return this;
        }

        /// <summary>
        /// Sets the image source from a browser-style data URL.
        /// </summary>
        /// <param name="dataUrl">Image data URL, for example <c>data:image/png;base64,...</c>.</param>
        /// <returns>The current builder instance.</returns>
        public Win32ImageBuilder FromDataUrl(string dataUrl)
        {
            _imageBytes = Base64Converter.DecodeBase64(dataUrl);
            return this;
        }

        /// <summary>
        /// Builds a new <see cref="Win32Image"/> instance.
        /// </summary>
        /// <returns>A newly created <see cref="Win32Image"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no source data was configured.</exception>
        public Win32Image Build()
        {
            if (_imageBytes is null)
                throw new InvalidOperationException("Image source is not configured. Call FromBytes, FromBase64, or FromDataUrl first.");

            return new Win32Image(_imageBytes);
        }
    }
}
