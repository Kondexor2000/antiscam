using System.Security.Cryptography;
using System.Text;

namespace AntiScam.Blog.Api.Security;

public static class GatewayCredential
{
    public static string CreateApiKey() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static bool Matches(string providedKey, string expectedHash)
    {
        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedKey));
        return CryptographicOperations.FixedTimeEquals(
            providedHash,
            Convert.FromHexString(expectedHash));
    }

    public static bool HasManagementAccess(HttpRequest request, string configuredKey)
    {
        if (string.IsNullOrEmpty(configuredKey))
        {
            return false;
        }

        var providedKey = request.Headers["X-Control-Plane-Key"].ToString();
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);
        var expectedBytes = Encoding.UTF8.GetBytes(configuredKey);
        return providedBytes.Length == expectedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
