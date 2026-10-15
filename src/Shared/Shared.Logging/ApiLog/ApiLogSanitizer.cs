using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Shared.Logging.ApiLog;

/// <summary>Che field nhạy cảm + cắt chuỗi dài trước khi ghi log (không lộ secret, không phình document Mongo).</summary>
public static class ApiLogSanitizer
{
    public const int MaxBodyLength = 10_000;
    public const int MaxValueLength = 500;
    private const int KeepValueLength = 100;
    private const string MaskedValue = "***";
    // Khớp kiểu "chứa" trên tên field, không phân biệt hoa thường: newPassword, refreshToken, clientSecret, otpauthUri...
    private static readonly string[] SensitiveKeys = ["password", "pwd", "token", "secret", "otp", "apikey", "connectionstring"];
    // Giữ nguyên tiếng Việt khi ghi log (mặc định System.Text.Json escape thành \uXXXX)
    internal static readonly JsonSerializerOptions JsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string Sanitize(string? strBody)
    {
        if (string.IsNullOrWhiteSpace(strBody)) return string.Empty;
        try
        {
            JsonNode? objNode = JsonNode.Parse(strBody);
            if (objNode != null) strBody = SanitizeNode(objNode).ToJsonString(JsonOptions);
        }
        catch (JsonException)
        {
            // Không phải JSON → chỉ cắt độ dài bên dưới
        }
        return Truncate(strBody, MaxBodyLength);
    }

    private static JsonNode SanitizeNode(JsonNode objNode)
    {
        if (objNode is JsonObject objJson)
        {
            foreach (string strKey in objJson.Select(x => x.Key).ToList())
            {
                if (objJson[strKey] is not JsonNode objChild) continue;
                if (IsSensitiveKey(strKey) && !IsBoolOrNumber(objChild)) objJson[strKey] = MaskedValue;
                else if (SanitizeNode(objChild) is var objResult && objResult != objChild) objJson[strKey] = objResult;
            }
        }
        else if (objNode is JsonArray arrJson)
        {
            for (int i = 0; i < arrJson.Count; i++)
                if (arrJson[i] is JsonNode objChild && SanitizeNode(objChild) is var objResult && objResult != objChild) arrJson[i] = objResult;
        }
        else if (objNode is JsonValue objValue && objValue.TryGetValue(out string? strValue) && strValue.Length > MaxValueLength)
        {
            // VD ảnh base64: chỉ giữ đoạn đầu để biết là gì
            return JsonValue.Create(Truncate(strValue, KeepValueLength));
        }
        return objNode;
    }

    private static bool IsSensitiveKey(string strKey) => SensitiveKeys.Any(x => strKey.Contains(x, StringComparison.OrdinalIgnoreCase));

    private static bool IsBoolOrNumber(JsonNode objNode) => objNode.GetValueKind() is JsonValueKind.True or JsonValueKind.False or JsonValueKind.Number;

    private static string Truncate(string strValue, int intMaxLength)
        => strValue.Length <= intMaxLength ? strValue : $"{strValue[..intMaxLength]}...(cắt, dài {strValue.Length} ký tự)";
}
