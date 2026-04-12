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

        bool isBase64 = false;
        string[] metadataParts = metadata.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < metadataParts.Length; i++)
        {
            string part = metadataParts[i].Trim();
            if (part.Equals("base64", StringComparison.OrdinalIgnoreCase))
            {
                isBase64 = true;
                break;
            }
        }

        if (!isBase64)
            throw new FormatException("Only base64-encoded data URLs are supported.");

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
        char[] buffer = new char[base64.Length];
        int index = 0;
        for (int i = 0; i < base64.Length; i++)
        {
            char c = base64[i];
            if (!char.IsWhiteSpace(c))
                buffer[index++] = c;
        }

        var filtered = new string(buffer, 0, index);
        if (filtered.Length == 0)
            throw new FormatException("Base64 content is empty.");

        try
        {
            return Convert.FromBase64String(filtered);
        }
        catch (FormatException ex)
        {
            throw new FormatException("Invalid base64 content.", ex);
        }
    }
}
