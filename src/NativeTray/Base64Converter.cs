namespace System.NativeTray;

public static class Base64Converter
{
    public static byte[] DecodeBase64(string value)
    {
        _ = value ?? throw new ArgumentNullException(nameof(value));

        string input = value.Trim();
        if (input.Length == 0)
            throw new ArgumentException("Value cannot be empty.", nameof(value));

        if (!input.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return DecodeRawBase64(input);

        int commaIndex = input.IndexOf(',');
        if (commaIndex < 0)
            throw new FormatException("Invalid data URL: missing ',' separator.");

        string metadata = input.Substring(5, commaIndex - 5);
        string payload = input.Substring(commaIndex + 1);

        if (!ContainsBase64Flag(metadata))
            throw new FormatException("Only base64-encoded data URLs are supported.");

        if (payload.IndexOf('%') >= 0)
            payload = Uri.UnescapeDataString(payload);

        return DecodeRawBase64(payload);
    }

    public static string EncodeBase64(byte[] bytes)
    {
        _ = bytes ?? throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length == 0)
            throw new ArgumentException("Bytes cannot be empty.", nameof(bytes));

        return Convert.ToBase64String(bytes);
    }

    private static byte[] DecodeRawBase64(string base64)
    {
        bool hasContent = false;
        for (int i = 0; i < base64.Length; i++)
        {
            if (!char.IsWhiteSpace(base64[i]))
            {
                hasContent = true;
                break;
            }
        }

        if (!hasContent)
            throw new FormatException("Base64 content is empty.");

        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException ex)
        {
            throw new FormatException("Invalid base64 content.", ex);
        }
    }

    private static bool ContainsBase64Flag(string metadata)
    {
        int index = 0;
        while (index < metadata.Length)
        {
            int nextSemicolon = metadata.IndexOf(';', index);
            int end = nextSemicolon >= 0 ? nextSemicolon : metadata.Length;

            string token = metadata.Substring(index, end - index).Trim();
            if (token.Equals("base64", StringComparison.OrdinalIgnoreCase))
                return true;

            if (nextSemicolon < 0)
                break;

            index = nextSemicolon + 1;
        }

        return false;
    }
}
