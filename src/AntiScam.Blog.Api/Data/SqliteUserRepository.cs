using AntiScam.Blog.Api.Models;
using Microsoft.Data.Sqlite;

namespace AntiScam.Blog.Api.Data;

public sealed class SqliteUserRepository(BlogDatabaseOptions options) : IUserRepository
{
    public async Task<BlogUser?> RegisterAsync(RegisterInput input, string passwordAlgorithm, int passwordIterations, string salt, string hash, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction();
        var userName = input.UserName.Trim();
        await using var count = connection.CreateCommand();
        count.Transaction = transaction;
        count.CommandText = "SELECT COUNT(*) FROM Users;";
        var isFirstUser = (long)(await count.ExecuteScalarAsync(cancellationToken) ?? 0L) == 0;
        var createdAt = DateTimeOffset.UtcNow;
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Users (UserName, PasswordAlgorithm, PasswordIterations, PasswordSalt, PasswordHash, Role, IsBlocked, CreatedAt)
            VALUES ($userName, $algorithm, $iterations, $salt, $hash, $role, 0, $createdAt)
            RETURNING Id, UserName, Role, IsBlocked, CreatedAt;
            """;
        command.Parameters.AddWithValue("$userName", userName);
        command.Parameters.AddWithValue("$algorithm", passwordAlgorithm);
        command.Parameters.AddWithValue("$iterations", passwordIterations);
        command.Parameters.AddWithValue("$salt", salt);
        command.Parameters.AddWithValue("$hash", hash);
        command.Parameters.AddWithValue("$role", isFirstUser ? "Admin" : "User");
        command.Parameters.AddWithValue("$createdAt", createdAt.ToString("O"));
        try
        {
            BlogUser? user;
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                user = await reader.ReadAsync(cancellationToken) ? ReadUser(reader) : null;
            }
            if (user is null) return null;
            transaction.Commit();
            return user;
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            transaction.Rollback();
            return null;
        }
    }

    public async Task<(BlogUser User, string Algorithm, int Iterations, string Salt, string Hash)?> GetForLoginAsync(string userName, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, UserName, Role, IsBlocked, CreatedAt, PasswordAlgorithm, PasswordIterations, PasswordSalt, PasswordHash FROM Users WHERE UserName = $userName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$userName", userName.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return (ReadUser(reader), reader.GetString(5), reader.GetInt32(6), reader.GetString(7), reader.GetString(8));
    }

    public async Task<string> CreateSessionAsync(int userId, string tokenHash, string remoteIp, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO UserSessions (UserId, TokenHash, RemoteIp, CreatedAt) VALUES ($userId, $tokenHash, $remoteIp, $createdAt);";
        command.Parameters.AddWithValue("$userId", userId); command.Parameters.AddWithValue("$tokenHash", tokenHash);
        command.Parameters.AddWithValue("$remoteIp", remoteIp); command.Parameters.AddWithValue("$createdAt", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
        return tokenHash;
    }

    public async Task RevokeSessionAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM UserSessions WHERE TokenHash = $tokenHash;";
        command.Parameters.AddWithValue("$tokenHash", tokenHash);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<AuthenticatedUser?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT u.Id, u.UserName, u.Role, u.IsBlocked FROM Users u JOIN UserSessions s ON s.UserId = u.Id WHERE s.TokenHash = $tokenHash;";
        command.Parameters.AddWithValue("$tokenHash", tokenHash);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? new AuthenticatedUser(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3) == 1) : null;
    }

    public async Task<IReadOnlyList<BlogUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, UserName, Role, IsBlocked, CreatedAt FROM Users ORDER BY CreatedAt ASC, Id ASC;";
        var users = new List<BlogUser>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) users.Add(ReadUser(reader));
        return users;
    }

    public async Task<bool> BlockAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand(); command.CommandText = "UPDATE Users SET IsBlocked = 1 WHERE Id = $id AND IsBlocked = 0;"; command.Parameters.AddWithValue("$id", userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UnblockAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Users SET IsBlocked = 0 WHERE Id = $id AND IsBlocked = 1;";
        command.Parameters.AddWithValue("$id", userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> HasLoggedInFromDifferentIpAsync(int userId, string remoteIp, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand(); command.CommandText = "SELECT EXISTS(SELECT 1 FROM UserSessions WHERE UserId = $id AND RemoteIp <> $ip);"; command.Parameters.AddWithValue("$id", userId); command.Parameters.AddWithValue("$ip", remoteIp);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = options.DatabasePath, Pooling = false, ForeignKeys = true }.ConnectionString);
        await connection.OpenAsync(cancellationToken); return connection;
    }

    private static BlogUser ReadUser(SqliteDataReader reader) => new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3) == 1, DateTimeOffset.Parse(reader.GetString(4)));
}
