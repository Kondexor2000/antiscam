namespace AntiScam.Blog.Api.Models;

public sealed record RegisterInput(string UserName, string Password);

public sealed record LoginInput(string UserName, string Password);

public sealed record AuthenticatedUser(int Id, string UserName, string Role, bool IsBlocked);

public sealed record AuthResponse(string AccessToken, AuthenticatedUser User);

public sealed record BlogUser(int Id, string UserName, string Role, bool IsBlocked, DateTimeOffset CreatedAt);
