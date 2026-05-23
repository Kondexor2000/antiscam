namespace AntiScam.Blog.Api.Security;

public sealed record PasswordHashResult(
    string Algorithm,
    int Iterations,
    string Salt,
    string Hash);
