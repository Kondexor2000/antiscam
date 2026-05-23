using System.Security.Cryptography;

namespace AntiScam.Blog.Api.Security;

public sealed class SecurePasswordHasher : IPasswordHasher
{
    public const string AlgorithmName = "PBKDF2-HMAC-SHA256";

    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DefaultIterations = 210_000;

    private readonly int _iterations;

    public SecurePasswordHasher(int iterations = DefaultIterations)
    {
        if (iterations < 100_000)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), "Use at least 100000 PBKDF2 iterations.");
        }

        _iterations = iterations;
    }

    public PasswordHashResult Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            _iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return new PasswordHashResult(
            AlgorithmName,
            _iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, PasswordHashResult storedHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        if (storedHash.Algorithm != AlgorithmName)
        {
            return false;
        }

        var salt = Convert.FromBase64String(storedHash.Salt);
        var expectedHash = Convert.FromBase64String(storedHash.Hash);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            storedHash.Iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
