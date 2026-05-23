namespace AntiScam.Blog.Api.Security;

public interface IPasswordHasher
{
    PasswordHashResult Hash(string password);

    bool Verify(string password, PasswordHashResult storedHash);
}
