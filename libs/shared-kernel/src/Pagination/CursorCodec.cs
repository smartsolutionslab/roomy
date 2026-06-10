using System.Buffers.Text;
using System.Text.Json;

namespace SmartSolutionsLab.Roomy.SharedKernel.Pagination;

public static class CursorCodec
{
    public static string Encode<TKey>(TKey key)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(key);
        return Base64Url.EncodeToString(json);
    }

    public static bool TryDecode<TKey>(string cursor, out TKey key)
    {
        key = default!;

        byte[] json;
        try
        {
            json = Base64Url.DecodeFromChars(cursor);
        }
        catch (FormatException)
        {
            return false;
        }

        try
        {
            var decoded = JsonSerializer.Deserialize<TKey>(json);
            if (decoded is null)
            {
                return false;
            }

            key = decoded;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
