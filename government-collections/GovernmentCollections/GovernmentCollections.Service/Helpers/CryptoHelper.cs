using System.Text.Json;
using System.Text;

namespace GovernmentCollections.Service.Helpers;

public static class CryptoHelper
{
    public static string EncryptJson(JsonElement text, string key)
    {
        string stringified;

        if (text.ValueKind == JsonValueKind.String)
        {
            var s = text.GetString();
            if (string.IsNullOrWhiteSpace(s))
                throw new ArgumentException("JSON string is empty.");

            try
            {
                using var doc = JsonDocument.Parse(s, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
                stringified = doc.RootElement.GetRawText();
            }
            catch (JsonException)
            {
                stringified = JsonSerializer.Serialize(s);
            }
        }
        else
        {
            stringified = text.GetRawText();
        }

        return stringified.EncryptWithSecreteKey(key);
    }

    public static JsonElement DecryptJson(string cipherText, string key)
    {
        var normalized = NormalizeBase64(cipherText);
        var plain = normalized.DecryptWithSecreteKey(key);

        var parseOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        try
        {
            using var doc = JsonDocument.Parse(plain, parseOptions);
            var root = doc.RootElement.Clone();

            if (root.ValueKind == JsonValueKind.String)
            {
                var inner = root.GetString();
                if (!string.IsNullOrWhiteSpace(inner))
                {
                    try
                    {
                        using var innerDoc = JsonDocument.Parse(inner, parseOptions);
                        root = innerDoc.RootElement.Clone();
                    }
                    catch (JsonException)
                    {
                        // keep as string element
                    }
                }
            }

            return root;
        }
        catch (JsonException)
        {
            return JsonDocument.Parse(JsonSerializer.Serialize(plain)).RootElement.Clone();
        }
    }

    private static string NormalizeBase64(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return input.Replace('-', '+').Replace('_', '/');
    }
}

public static class CryptoExtensions
{
    public static string EncryptWithSecreteKey(this string plainText, string key)
    {
        // Simple encryption implementation - replace with actual encryption
        var bytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(bytes);
    }

    public static string DecryptWithSecreteKey(this string cipherText, string key)
    {
        // Simple decryption implementation - replace with actual decryption
        var bytes = Convert.FromBase64String(cipherText);
        return Encoding.UTF8.GetString(bytes);
    }
}