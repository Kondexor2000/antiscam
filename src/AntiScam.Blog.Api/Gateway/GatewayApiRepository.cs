using Microsoft.Data.SqlClient;
using AntiScam.Blog.Api.Data;

namespace AntiScam.Blog.Api.Gateway;

public sealed class GatewayApiRepository(BlogDatabaseOptions options)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            IF OBJECT_ID(N'dbo.GatewayApis', N'U') IS NULL
            CREATE TABLE dbo.GatewayApis (
                Id NVARCHAR(32) NOT NULL PRIMARY KEY,
                Name NVARCHAR(200) COLLATE Latin1_General_100_CI_AS NOT NULL UNIQUE,
                Version NVARCHAR(50) NOT NULL,
                UpstreamUrl NVARCHAR(2048) NOT NULL,
                ApiKeyHash NVARCHAR(128) NOT NULL,
                CreatedAt NVARCHAR(40) NOT NULL,
                Enabled BIT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GatewayApi>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, Version, UpstreamUrl, ApiKeyHash, CreatedAt, Enabled
            FROM GatewayApis
            ORDER BY Name;
            """;

        var apis = new List<GatewayApi>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            apis.Add(ReadApi(reader));
        }

        return apis;
    }

    public async Task<GatewayApi?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, Version, UpstreamUrl, ApiKeyHash, CreatedAt, Enabled
            FROM GatewayApis
            WHERE Id = @id;
            """;
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadApi(reader) : null;
    }

    public async Task<GatewayApi> CreateAsync(
        GatewayApiInput input,
        string apiKeyHash,
        CancellationToken cancellationToken = default)
    {
        var api = new GatewayApi(
            Guid.NewGuid().ToString("N"),
            input.Name.Trim(),
            input.Version.Trim(),
            input.UpstreamUrl.Trim().TrimEnd('/'),
            apiKeyHash,
            DateTimeOffset.UtcNow,
            input.Enabled);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO GatewayApis (Id, Name, Version, UpstreamUrl, ApiKeyHash, CreatedAt, Enabled)
            VALUES (@id, @name, @version, @upstreamUrl, @apiKeyHash, @createdAt, @enabled);
            """;
        AddApiParameters(command, api);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return api;
    }

    public async Task<bool> UpdateAsync(
        string id,
        GatewayApiInput input,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE GatewayApis
            SET Name = @name, Version = @version, UpstreamUrl = @upstreamUrl, Enabled = @enabled
            WHERE Id = @id;
            """;
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", input.Name.Trim());
        command.Parameters.AddWithValue("@version", input.Version.Trim());
        command.Parameters.AddWithValue("@upstreamUrl", input.UpstreamUrl.Trim().TrimEnd('/'));
        command.Parameters.AddWithValue("@enabled", input.Enabled);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateApiKeyHashAsync(
        string id,
        string apiKeyHash,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE GatewayApis SET ApiKeyHash = @hash WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@hash", apiKeyHash);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM GatewayApis WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private SqlConnection CreateConnection() => new(options.ConnectionString);

    private static GatewayApi ReadApi(SqlDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            DateTimeOffset.Parse(reader.GetString(5)),
            reader.GetBoolean(6));

    private static void AddApiParameters(SqlCommand command, GatewayApi api)
    {
        command.Parameters.AddWithValue("@id", api.Id);
        command.Parameters.AddWithValue("@name", api.Name);
        command.Parameters.AddWithValue("@version", api.Version);
        command.Parameters.AddWithValue("@upstreamUrl", api.UpstreamUrl);
        command.Parameters.AddWithValue("@apiKeyHash", api.ApiKeyHash);
        command.Parameters.AddWithValue("@createdAt", api.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@enabled", api.Enabled ? 1 : 0);
    }
}
