using System.Security.Cryptography;
using System.Text;

namespace AntiScam.Blog.Api.Security;

public interface ITokenService
{
    string Create();
    string Hash(string token);
}

public sealed class TokenService : ITokenService
{
    public string Create() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
