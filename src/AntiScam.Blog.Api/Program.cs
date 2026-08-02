using AntiScam.Blog.Api;
using AntiScam.Blog.Api.Data;
using AntiScam.Blog.Api.Models;
using AntiScam.Blog.Api.Services;
using AntiScam.Blog.Api.Security;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

var configuredWorkspace = builder.Configuration["Workspace:RootPath"];
var workspacePath = string.IsNullOrWhiteSpace(configuredWorkspace)
    ? @"C:\Users\kondz\antiscam"
    : configuredWorkspace;

var configuredDatabasePath = builder.Configuration["Blog:DatabasePath"];
var environmentDatabasePath = Environment.GetEnvironmentVariable("ANTISCAM_BLOG_DB");
var databasePath = !string.IsNullOrWhiteSpace(environmentDatabasePath)
    ? environmentDatabasePath
    : !string.IsNullOrWhiteSpace(configuredDatabasePath)
        ? configuredDatabasePath
        : Path.Combine(workspacePath, "data", "antiscam-blog.sqlite");

var noSqlOptions = builder.Configuration.GetSection("NoSql").Get<NoSqlDatabaseOptions>()
    ?? new NoSqlDatabaseOptions();
var mongoConnectionString = Environment.GetEnvironmentVariable("ANTISCAM_MONGO_CONNECTION_STRING");
if (!string.IsNullOrWhiteSpace(mongoConnectionString))
{
    noSqlOptions = noSqlOptions with { ConnectionString = mongoConnectionString };
}
var backupOptions = builder.Configuration.GetSection("Backup").Get<BackupOptions>() ?? new BackupOptions();
var backupKey = Environment.GetEnvironmentVariable("ANTISCAM_BACKUP_KEY");
if (!string.IsNullOrWhiteSpace(backupKey)) backupOptions = backupOptions with { EncryptionKey = backupKey };
var httpsOptions = builder.Configuration.GetSection("Https").Get<HttpsOptions>() ?? new HttpsOptions();
var networkOptions = builder.Configuration.GetSection("Network").Get<NetworkOptions>() ?? new NetworkOptions();
var httpsPassword = Environment.GetEnvironmentVariable("ANTISCAM_HTTPS_CERT_PASSWORD");
if (!string.IsNullOrWhiteSpace(httpsPassword)) httpsOptions = httpsOptions with { CertificatePassword = httpsPassword };
if (httpsOptions.Enabled)
{
    if (!IPAddress.TryParse(httpsOptions.ListenAddress, out var listenAddress))
        throw new InvalidOperationException("Https:ListenAddress must be a valid IP address.");
    if (!File.Exists(httpsOptions.CertificatePath) || string.IsNullOrEmpty(httpsOptions.CertificatePassword))
        throw new InvalidOperationException("HTTPS requires an existing PFX certificate and ANTISCAM_HTTPS_CERT_PASSWORD.");
    builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(listenAddress, httpsOptions.Port,
        listen => listen.UseHttps(httpsOptions.CertificatePath, httpsOptions.CertificatePassword)));
}
else if (networkOptions.BindToLan)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{networkOptions.HttpPort}");
}

builder.Services.AddSingleton(new WorkspaceOptions(workspacePath));
builder.Services.AddSingleton(new BlogDatabaseOptions(databasePath));
builder.Services.AddSingleton(noSqlOptions);
builder.Services.AddSingleton(backupOptions);
builder.Services.AddSingleton<ISlugGenerator, SlugGenerator>();
builder.Services.AddSingleton<IRiskAnalyzer, RiskAnalyzer>();
builder.Services.AddSingleton<IBlockExplanationProvider, PythonAiBlockExplanationProvider>();
builder.Services.AddSingleton<IScamIncidentStore>(serviceProvider =>
    noSqlOptions.Enabled
        ? new MongoScamIncidentStore(
            noSqlOptions,
            serviceProvider.GetRequiredService<ILogger<MongoScamIncidentStore>>())
        : new NullScamIncidentStore());
builder.Services.AddSingleton<IBlogRepository, SqliteBlogRepository>();
builder.Services.AddSingleton<IUserRepository, SqliteUserRepository>();
builder.Services.AddSingleton<IPasswordHasher, SecurePasswordHasher>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<ISecureBackupService, SecureBackupService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Context.Response.Headers.Pragma = "no-cache";
        context.Context.Response.Headers.Expires = "0";
    }
});

var repository = app.Services.GetRequiredService<IBlogRepository>();
await repository.InitializeAsync();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    application = "AntiScam Blog API",
    storage = "SQLite",
    secondaryStorage = noSqlOptions.Enabled ? "MongoDB" : "disabled"
}));

app.MapGet("/api/storage", (BlogDatabaseOptions sqlite, NoSqlDatabaseOptions mongo) => Results.Ok(new
{
    primary = new { provider = "SQLite", path = sqlite.DatabasePath },
    secondary = new
    {
        provider = "MongoDB",
        enabled = mongo.Enabled,
        database = mongo.DatabaseName,
        collection = mongo.CollectionName
    }
}));

app.MapGet("/api/incidents", async (int? limit, IScamIncidentStore incidentStore, CancellationToken cancellationToken) =>
{
    var safeLimit = Math.Clamp(limit ?? 50, 1, 100);
    var incidents = await incidentStore.GetRecentAsync(safeLimit, cancellationToken);
    return Results.Ok(incidents);
});

app.MapGet("/api/workspace", (WorkspaceOptions options, BlogDatabaseOptions database) =>
{
    var directory = new DirectoryInfo(options.RootPath);
    return Results.Ok(new
    {
        rootPath = options.RootPath,
        exists = directory.Exists,
        databasePath = database.DatabasePath
    });
});

app.MapGet("/api/posts", async (IBlogRepository blogRepository) =>
{
    var posts = await blogRepository.GetAllAsync();
    return Results.Ok(posts);
});

app.MapPost("/api/auth/register", async (RegisterInput input, IUserRepository users, IPasswordHasher passwords, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(input.UserName) || input.UserName.Trim().Length is < 3 or > 100 || string.IsNullOrWhiteSpace(input.Password) || input.Password.Length < 12)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["credentials"] = ["Nazwa użytkownika musi mieć 3–100 znaków, a hasło co najmniej 12 znaków."] });
    var password = passwords.Hash(input.Password);
    var user = await users.RegisterAsync(input, password.Algorithm, password.Iterations, password.Salt, password.Hash, cancellationToken);
    return user is null ? Results.Conflict(new { message = "Nazwa użytkownika jest już zajęta." }) : Results.Created($"/api/users/{user.Id}", user);
});

app.MapPost("/api/auth/login", async (HttpContext context, LoginInput input, IUserRepository users, IPasswordHasher passwords, ITokenService tokens, ISecureBackupService backup, ILoggerFactory loggers, CancellationToken cancellationToken) =>
{
    var stored = await users.GetForLoginAsync(input.UserName, cancellationToken);
    if (stored is null || !passwords.Verify(input.Password, new PasswordHashResult(stored.Value.Algorithm, stored.Value.Iterations, stored.Value.Salt, stored.Value.Hash))) return Results.Unauthorized();
    if (stored.Value.User.IsBlocked) return Results.StatusCode(StatusCodes.Status403Forbidden);
    var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var shouldBackup = await users.HasLoggedInFromDifferentIpAsync(stored.Value.User.Id, remoteIp, cancellationToken);
    var token = tokens.Create();
    await users.CreateSessionAsync(stored.Value.User.Id, tokens.Hash(token), remoteIp, cancellationToken);
    if (shouldBackup)
    {
        try { await backup.CreateIfChangedAsync(cancellationToken); }
        catch (Exception exception) { loggers.CreateLogger("SecureBackup").LogError(exception, "Automatic secure backup failed after IP change."); }
    }
    return Results.Ok(new AuthResponse(token, new AuthenticatedUser(stored.Value.User.Id, stored.Value.User.UserName, stored.Value.User.Role, false)));
});

app.MapPost("/api/auth/logout", async (HttpContext context, IUserRepository users, ITokenService tokens, CancellationToken cancellationToken) =>
{
    var authorization = context.Request.Headers.Authorization.ToString();
    if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Results.Unauthorized();
    await users.RevokeSessionAsync(tokens.Hash(authorization[7..].Trim()), cancellationToken);
    return Results.NoContent();
});

app.MapPost("/api/admin/users/{id:int}/block", async (int id, HttpContext context, IUserRepository users, ITokenService tokens, CancellationToken cancellationToken) =>
{
    var admin = await GetAdminAsync(context, users, tokens, cancellationToken);
    if (admin is null) return Results.Unauthorized();
    if (admin.Id == id) return Results.BadRequest(new { message = "Administrator nie może zablokować własnego konta." });
    return await users.BlockAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound();
});

app.MapPost("/api/admin/users/{id:int}/unblock", async (int id, HttpContext context, IUserRepository users, ITokenService tokens, CancellationToken cancellationToken) =>
{
    var admin = await GetAdminAsync(context, users, tokens, cancellationToken);
    if (admin is null) return Results.Unauthorized();
    return await users.UnblockAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/api/admin/posts", async (HttpContext context, IBlogRepository posts, IUserRepository users, ITokenService tokens, CancellationToken cancellationToken) =>
    await GetAdminAsync(context, users, tokens, cancellationToken) is null ? Results.Unauthorized() : Results.Ok(await posts.GetAllAsync(true, cancellationToken)));

app.MapGet("/api/admin/users", async (HttpContext context, IUserRepository users, ITokenService tokens, CancellationToken cancellationToken) =>
    await GetAdminAsync(context, users, tokens, cancellationToken) is null ? Results.Unauthorized() : Results.Ok(await users.GetAllAsync(cancellationToken)));

app.MapPost("/api/admin/posts/{id:int}/deactivate", async (int id, HttpContext context, IBlogRepository posts, IUserRepository users, ITokenService tokens, CancellationToken cancellationToken) =>
    await GetAdminAsync(context, users, tokens, cancellationToken) is null ? Results.Unauthorized() : await posts.DeactivateAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound());

app.MapPost("/api/admin/posts/{id:int}/restore", async (int id, HttpContext context, IBlogRepository posts, IUserRepository users, ITokenService tokens, CancellationToken cancellationToken) =>
    await GetAdminAsync(context, users, tokens, cancellationToken) is null ? Results.Unauthorized() : await posts.RestoreAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound());

app.MapGet("/api/posts/latest", async (IBlogRepository blogRepository) =>
{
    var post = await blogRepository.GetLatestAsync();
    return post is null ? Results.NotFound() : Results.Ok(post);
});

app.MapGet("/api/posts/{slug}", async (string slug, IBlogRepository blogRepository) =>
{
    var post = await blogRepository.GetBySlugAsync(slug);
    return post is null ? Results.NotFound() : Results.Ok(post);
});

app.MapGet("/api/posts/{postId:int}/comments", async (int postId, IBlogRepository blogRepository) =>
{
    var comments = await blogRepository.GetCommentsAsync(postId);
    return Results.Ok(comments);
});

app.MapPost("/api/posts/{postId:int}/comments", async (
    int postId,
    BlogCommentInput input,
    IBlogRepository blogRepository,
    IRiskAnalyzer riskAnalyzer,
    CancellationToken cancellationToken) =>
{
    var validation = BlogCommentValidator.Validate(input);
    if (validation.Count > 0)
    {
        return Results.ValidationProblem(validation);
    }

    // Use exactly the same analyzer as posts; comments simply have no title or summary.
    var risk = riskAnalyzer.Analyze(new BlogPostInput(string.Empty, string.Empty, input.Content, input.Author));
    if (!risk.CanPublish)
    {
        return Results.Json(new
        {
            message = "Comment was not published because scam risk was detected.",
            risk
        }, statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    var created = await blogRepository.CreateCommentAsync(postId, input, cancellationToken);
    return created is null
        ? Results.NotFound()
        : Results.Created($"/api/posts/{postId}/comments/{created.Id}", created);
});

app.MapPost("/api/posts", async (
    BlogPostInput input,
    IBlogRepository blogRepository,
    IRiskAnalyzer riskAnalyzer,
    IBlockExplanationProvider blockExplanationProvider,
    IScamIncidentStore scamIncidentStore,
    CancellationToken cancellationToken) =>
{
    var validation = BlogPostValidator.Validate(input);
    if (validation.Count > 0)
    {
        return Results.ValidationProblem(validation);
    }

    var risk = riskAnalyzer.Analyze(input);
    if (!risk.CanPublish)
    {
        var aiExplanation = await blockExplanationProvider.ExplainAsync(input, risk, cancellationToken);
        await scamIncidentStore.RecordAsync(input, risk, cancellationToken);
        return Results.Json(new
        {
            message = "Post was not published because scam risk was detected.",
            aiExplanation,
            risk
        }, statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    var created = await blogRepository.CreateAsync(input);
    return Results.Created($"/api/posts/{created.Slug}", created);
});

app.MapPut("/api/posts/{id:int}", async (
    int id,
    BlogPostInput input,
    IBlogRepository blogRepository,
    IRiskAnalyzer riskAnalyzer,
    IBlockExplanationProvider blockExplanationProvider,
    CancellationToken cancellationToken) =>
{
    var validation = BlogPostValidator.Validate(input);
    if (validation.Count > 0)
    {
        return Results.ValidationProblem(validation);
    }

    var risk = riskAnalyzer.Analyze(input);
    if (!risk.CanPublish)
    {
        var aiExplanation = await blockExplanationProvider.ExplainAsync(input, risk, cancellationToken);
        return Results.Json(new
        {
            message = "Post was not updated because scam risk was detected.",
            aiExplanation,
            risk
        }, statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    var updated = await blogRepository.UpdateAsync(id, input);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.MapDelete("/api/posts/{id:int}", async (int id, HttpContext context, IBlogRepository blogRepository, IUserRepository users, ITokenService tokens, CancellationToken cancellationToken) =>
{
    if (await GetAdminAsync(context, users, tokens, cancellationToken) is null) return Results.Unauthorized();
    var deleted = await blogRepository.DeactivateAsync(id, cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();

public partial class Program
{
    private static async Task<AuthenticatedUser?> GetAdminAsync(HttpContext context, IUserRepository users, ITokenService tokens, CancellationToken cancellationToken)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        var user = await users.GetByTokenHashAsync(tokens.Hash(authorization[7..].Trim()), cancellationToken);
        return user is { IsBlocked: false, Role: "Admin" } ? user : null;
    }
}
