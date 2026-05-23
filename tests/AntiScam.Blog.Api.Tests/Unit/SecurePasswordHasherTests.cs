using AntiScam.Blog.Api.Security;

namespace AntiScam.Blog.Api.Tests.Unit;

public sealed class SecurePasswordHasherTests
{
    [Fact]
    public void Hash_VerifyAcceptsOriginalPassword()
    {
        var hasher = new SecurePasswordHasher();

        var result = hasher.Hash("correct horse battery staple");

        Assert.Equal(SecurePasswordHasher.AlgorithmName, result.Algorithm);
        Assert.True(hasher.Verify("correct horse battery staple", result));
    }

    [Fact]
    public void Verify_RejectsWrongPassword()
    {
        var hasher = new SecurePasswordHasher();
        var result = hasher.Hash("correct horse battery staple");

        Assert.False(hasher.Verify("wrong password", result));
    }
}
