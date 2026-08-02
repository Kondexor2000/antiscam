using AntiScam.Blog.Api.Models;

namespace AntiScam.Blog.Api.Data;

public interface IUserRepository
{
    Task<BlogUser?> RegisterAsync(RegisterInput input, string passwordAlgorithm, int passwordIterations, string salt, string hash, CancellationToken cancellationToken = default);
    Task<(BlogUser User, string Algorithm, int Iterations, string Salt, string Hash)?> GetForLoginAsync(string userName, CancellationToken cancellationToken = default);
    Task<string> CreateSessionAsync(int userId, string tokenHash, string remoteIp, CancellationToken cancellationToken = default);
    Task RevokeSessionAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<AuthenticatedUser?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlogUser>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> BlockAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> UnblockAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> HasLoggedInFromDifferentIpAsync(int userId, string remoteIp, CancellationToken cancellationToken = default);
}
