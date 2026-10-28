using AntiScam.Blog.Api.Models;
using AntiScam.Blog.Api.Services;
using Microsoft.Data.SqlClient;

namespace AntiScam.Blog.Api.Data;

public sealed class SqlServerBlogRepository(BlogDatabaseOptions options, ISlugGenerator slugGenerator) : IBlogRepository
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                IF OBJECT_ID(N'dbo.Posts', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Posts (
                        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        Title NVARCHAR(300) NOT NULL,
                        Slug NVARCHAR(300) NOT NULL UNIQUE,
                        Summary NVARCHAR(2000) NOT NULL,
                        Content NVARCHAR(MAX) NOT NULL,
                        Author NVARCHAR(200) NOT NULL,
                        PublishedAt NVARCHAR(40) NOT NULL,
                        UpdatedAt NVARCHAR(40) NOT NULL,
                        IsActive BIT NOT NULL CONSTRAINT DF_Posts_IsActive DEFAULT 1
                    );
                END;
                IF OBJECT_ID(N'dbo.Comments', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Comments (
                        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        PostId INT NOT NULL,
                        Content NVARCHAR(2000) NOT NULL,
                        Author NVARCHAR(200) NOT NULL,
                        PublishedAt NVARCHAR(40) NOT NULL,
                        CONSTRAINT FK_Comments_Posts FOREIGN KEY (PostId) REFERENCES dbo.Posts(Id) ON DELETE CASCADE
                    );
                END;
                IF COL_LENGTH(N'dbo.Posts', N'IsActive') IS NULL
                    ALTER TABLE dbo.Posts ADD IsActive BIT NOT NULL CONSTRAINT DF_Posts_IsActive_Migration DEFAULT 1;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Comments_PostId_PublishedAt' AND object_id = OBJECT_ID(N'dbo.Comments'))
                    CREATE INDEX IX_Comments_PostId_PublishedAt ON dbo.Comments (PostId, PublishedAt, Id);
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await SeedAsync(connection, cancellationToken);
    }

    public async Task<IReadOnlyList<BlogPost>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Title, Slug, Summary, Content, Author, PublishedAt, UpdatedAt, IsActive
            FROM Posts
            WHERE IsActive = 1 OR @includeInactive = 1
            ORDER BY PublishedAt DESC, Id DESC;
            """;
        command.Parameters.AddWithValue("@includeInactive", includeInactive);

        var posts = new List<BlogPost>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            posts.Add(ReadPost(reader));
        }

        return posts;
    }

    public async Task<BlogPost?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (1) Id, Title, Slug, Summary, Content, Author, PublishedAt, UpdatedAt, IsActive
            FROM Posts
            WHERE IsActive = 1
            ORDER BY PublishedAt DESC, Id DESC;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadPost(reader) : null;
    }

    public async Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Title, Slug, Summary, Content, Author, PublishedAt, UpdatedAt, IsActive
            FROM Posts
            WHERE Slug = @slug AND IsActive = 1;
            """;
        command.Parameters.AddWithValue("@slug", slug);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadPost(reader) : null;
    }

    public async Task<IReadOnlyList<BlogComment>> GetCommentsAsync(int postId, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, PostId, Content, Author, PublishedAt
            FROM Comments
            WHERE PostId = @postId
            ORDER BY PublishedAt ASC, Id ASC;
            """;
        command.Parameters.AddWithValue("@postId", postId);

        var comments = new List<BlogComment>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            comments.Add(ReadComment(reader));
        }

        return comments;
    }

    public async Task<BlogPost> CreateAsync(BlogPostInput input, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var slug = await CreateUniqueSlugAsync(connection, input.Title, null, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Posts (Title, Slug, Summary, Content, Author, PublishedAt, UpdatedAt, IsActive)
            OUTPUT INSERTED.Id, INSERTED.Title, INSERTED.Slug, INSERTED.Summary, INSERTED.Content, INSERTED.Author, INSERTED.PublishedAt, INSERTED.UpdatedAt, INSERTED.IsActive
            VALUES (@title, @slug, @summary, @content, @author, @publishedAt, @updatedAt, 1);
            """;
        AddPostParameters(command, input, slug, now, now);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Post creation did not return a row.");
        }

        return ReadPost(reader);
    }

    public async Task<BlogComment?> CreateCommentAsync(int postId, BlogCommentInput input, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        if (!await ExistsAsync(connection, postId, cancellationToken))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Comments (PostId, Content, Author, PublishedAt)
            OUTPUT INSERTED.Id, INSERTED.PostId, INSERTED.Content, INSERTED.Author, INSERTED.PublishedAt
            VALUES (@postId, @content, @author, @publishedAt);
            """;
        command.Parameters.AddWithValue("@postId", postId);
        command.Parameters.AddWithValue("@content", input.Content.Trim());
        command.Parameters.AddWithValue("@author", input.Author.Trim());
        command.Parameters.AddWithValue("@publishedAt", now.ToString("O"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadComment(reader) : null;
    }

    public async Task<BlogPost?> UpdateAsync(int id, BlogPostInput input, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        if (!await ExistsAsync(connection, id, cancellationToken))
        {
            return null;
        }

        var current = DateTimeOffset.UtcNow;
        var slug = await CreateUniqueSlugAsync(connection, input.Title, id, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Posts
            SET Title = @title,
                Slug = @slug,
                Summary = @summary,
                Content = @content,
                Author = @author,
                UpdatedAt = @updatedAt
            WHERE Id = @id
            OUTPUT INSERTED.Id, INSERTED.Title, INSERTED.Slug, INSERTED.Summary, INSERTED.Content, INSERTED.Author, INSERTED.PublishedAt, INSERTED.UpdatedAt, INSERTED.IsActive;
            """;
        command.Parameters.AddWithValue("@id", id);
        AddPostParameters(command, input, slug, null, current);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadPost(reader) : null;
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Posts SET IsActive = 0, UpdatedAt = @updatedAt WHERE Id = @id AND IsActive = 1;";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@updatedAt", DateTimeOffset.UtcNow.ToString("O"));

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Posts SET IsActive = 1, UpdatedAt = @updatedAt WHERE Id = @id AND IsActive = 0;";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@updatedAt", DateTimeOffset.UtcNow.ToString("O"));
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(options.ConnectionString);
    }

    private static async Task SeedAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Posts;";
        var count = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken) ?? 0);
        if (count > 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var seedPosts = new[]
        {
            new BlogPostInput(
                "Jak rozpoznać fałszywą wiadomość BLIK",
                "Krótki przewodnik po sygnałach ostrzegawczych w wiadomościach proszących o kod BLIK.",
                "Nie podawaj kodu BLIK osobie, która naciska na szybkie działanie. Najpierw zadzwoń do znajomego innym kanałem i potwierdź prośbę.",
                "AntiScam Team"),
            new BlogPostInput(
                "Checklist bezpieczeństwa linków",
                "Lista szybkich kontroli przed kliknięciem w link z SMS-a, maila lub komunikatora.",
                "Sprawdź domenę, certyfikat, literówki i kontekst. Banki oraz urzędy nie powinny wymuszać pilnych logowań przez skrócone linki.",
                "AntiScam Team")
        };

        foreach (var post in seedPosts)
        {
            await using var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO Posts (Title, Slug, Summary, Content, Author, PublishedAt, UpdatedAt)
                VALUES (@title, @slug, @summary, @content, @author, @publishedAt, @updatedAt);
                """;
            insert.Parameters.AddWithValue("@title", post.Title);
            insert.Parameters.AddWithValue("@slug", SlugGenerator.CreateSlug(post.Title));
            insert.Parameters.AddWithValue("@summary", post.Summary);
            insert.Parameters.AddWithValue("@content", post.Content);
            insert.Parameters.AddWithValue("@author", post.Author);
            insert.Parameters.AddWithValue("@publishedAt", now.ToString("O"));
            insert.Parameters.AddWithValue("@updatedAt", now.ToString("O"));
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task<string> CreateUniqueSlugAsync(
        SqlConnection connection,
        string title,
        int? currentPostId,
        CancellationToken cancellationToken)
    {
        var baseSlug = slugGenerator.Generate(title);
        var slug = baseSlug;
        var suffix = 2;

        while (await SlugExistsAsync(connection, slug, currentPostId, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private static async Task<bool> SlugExistsAsync(
        SqlConnection connection,
        string slug,
        int? currentPostId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = currentPostId is null
            ? "SELECT COUNT(*) FROM Posts WHERE Slug = @slug;"
            : "SELECT COUNT(*) FROM Posts WHERE Slug = @slug AND Id <> @id;";
        command.Parameters.AddWithValue("@slug", slug);
        if (currentPostId is not null)
        {
            command.Parameters.AddWithValue("@id", currentPostId.Value);
        }

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        return count > 0;
    }

    private static async Task<bool> ExistsAsync(SqlConnection connection, int id, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Posts WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        return count > 0;
    }

    private static void AddPostParameters(
        SqlCommand command,
        BlogPostInput input,
        string slug,
        DateTimeOffset? publishedAt,
        DateTimeOffset updatedAt)
    {
        command.Parameters.AddWithValue("@title", input.Title.Trim());
        command.Parameters.AddWithValue("@slug", slug);
        command.Parameters.AddWithValue("@summary", input.Summary.Trim());
        command.Parameters.AddWithValue("@content", input.Content.Trim());
        command.Parameters.AddWithValue("@author", input.Author.Trim());
        command.Parameters.AddWithValue("@updatedAt", updatedAt.ToString("O"));
        if (publishedAt is not null)
        {
            command.Parameters.AddWithValue("@publishedAt", publishedAt.Value.ToString("O"));
        }
    }

    private static BlogPost ReadPost(SqlDataReader reader)
    {
        return new BlogPost(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            DateTimeOffset.Parse(reader.GetString(6)),
            DateTimeOffset.Parse(reader.GetString(7)),
            reader.GetBoolean(8));
    }

    private static BlogComment ReadComment(SqlDataReader reader)
    {
        return new BlogComment(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetString(2),
            reader.GetString(3),
            DateTimeOffset.Parse(reader.GetString(4)));
    }
}
