using AntiScam.Blog.Api;
using AntiScam.Blog.Api.Data;
using AntiScam.Blog.Api.Gateway;
using AntiScam.Blog.Api.Models;
using AntiScam.Blog.Api.Security;
using AntiScam.Blog.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.OutputCaching;
using System.Net;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var configuredWorkspace = builder.Configuration["Workspace:RootPath"];
var workspacePath = string.IsNullOrWhiteSpace(configuredWorkspace)
    ? builder.Environment.ContentRootPath
    : ResolvePath(configuredWorkspace, builder.Environment.ContentRootPath);

var configuredConnectionString = builder.Configuration["Blog:ConnectionString"];
var environmentConnectionString = Environment.GetEnvironmentVariable("ANTISCAM_BLOG_CONNECTION_STRING");
var connectionString = !string.IsNullOrWhiteSpace(environmentConnectionString)
    ? environmentConnectionString
    : configuredConnectionString;
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure Blog:ConnectionString or ANTISCAM_BLOG_CONNECTION_STRING for SQL Server.");
}
var managementKey = builder.Configuration["ControlPlane:ManagementKey"]
    ?? builder.Configuration["ANTISCAM_CONTROL_PLANE_KEY"]
    ?? Environment.GetEnvironmentVariable("ANTISCAM_CONTROL_PLANE_KEY")
    ?? string.Empty;
if (!string.IsNullOrEmpty(managementKey) && Encoding.UTF8.GetByteCount(managementKey) < 32)
{
    throw new InvalidOperationException("ANTISCAM_CONTROL_PLANE_KEY must contain at least 32 bytes.");
}

var noSqlOptions = builder.Configuration.GetSection("NoSql").Get<NoSqlDatabaseOptions>()
    ?? new NoSqlDatabaseOptions();
var mongoConnectionString = Environment.GetEnvironmentVariable("ANTISCAM_MONGO_CONNECTION_STRING");
if (!string.IsNullOrWhiteSpace(mongoConnectionString))
{
    noSqlOptions = noSqlOptions with { ConnectionString = mongoConnectionString };
}
var httpsOptions = builder.Configuration.GetSection("Https").Get<HttpsOptions>() ?? new HttpsOptions();
httpsOptions = httpsOptions with { CertificatePath = ResolvePath(httpsOptions.CertificatePath, workspacePath) };
var networkOptions = builder.Configuration.GetSection("Network").Get<NetworkOptions>() ?? new NetworkOptions();
var redisConnectionString = Environment.GetEnvironmentVariable("ANTISCAM_REDIS_CONNECTION_STRING")
    ?? builder.Configuration["Cache:RedisConnectionString"];
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
builder.Services.AddSingleton(new BlogDatabaseOptions(connectionString));
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisOutputCache(options => options.Configuration = redisConnectionString);
}
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("BlogPosts", policy => policy.Expire(TimeSpan.FromSeconds(60)).Tag("blog-posts"));
    options.AddPolicy("BlogComments", policy => policy.Expire(TimeSpan.FromSeconds(60)).Tag("blog-comments"));
});
builder.Services.AddSingleton<GatewayApiRepository>();
builder.Services.AddSingleton(new GatewayMetrics());
builder.Services.AddSingleton<BlogRequestMetrics>();
builder.Services.AddHttpClient("gateway")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton(noSqlOptions);
builder.Services.AddSingleton<ISlugGenerator, SlugGenerator>();
builder.Services.AddSingleton<IRiskAnalyzer, RiskAnalyzer>();
builder.Services.AddSingleton<IBlockExplanationProvider, PythonAiBlockExplanationProvider>();
builder.Services.AddSingleton<IScamIncidentStore>(serviceProvider =>
    noSqlOptions.Enabled
        ? new MongoScamIncidentStore(
            noSqlOptions,
            serviceProvider.GetRequiredService<ILogger<MongoScamIncidentStore>>())
        : new NullScamIncidentStore());
builder.Services.AddSingleton<IBlogRepository, SqlServerBlogRepository>();

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
app.UseOutputCache();

var blogRequestMetrics = app.Services.GetRequiredService<BlogRequestMetrics>();
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api/posts"))
    {
        await next();
        return;
    }

    var startedAt = Stopwatch.GetTimestamp();
    context.Response.OnCompleted(() =>
    {
        blogRequestMetrics.Record(Stopwatch.GetElapsedTime(startedAt), context.Response.StatusCode);
        return Task.CompletedTask;
    });
    await next();
});

var repository = app.Services.GetRequiredService<IBlogRepository>();
await repository.InitializeAsync();
await app.Services.GetRequiredService<GatewayApiRepository>().InitializeAsync();

IResult? RequireManagementKey(HttpRequest request)
{
    if (string.IsNullOrEmpty(managementKey))
    {
        return Results.Problem(
            "Set ANTISCAM_CONTROL_PLANE_KEY before using the Control Plane management API.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return GatewayCredential.HasManagementAccess(request, managementKey)
        ? null
        : Results.Unauthorized();
}

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    application = "AntiScam API Platform",
    storage = "SQL Server",
    secondaryStorage = noSqlOptions.Enabled ? "MongoDB" : "disabled",
    distributedCache = string.IsNullOrWhiteSpace(redisConnectionString) ? "in-memory" : "Redis"
}));

app.MapGet("/api/gateway/apis", async (
    HttpRequest request,
    GatewayApiRepository gatewayRepository,
    GatewayMetrics metrics,
    CancellationToken cancellationToken) =>
{
    var accessResult = RequireManagementKey(request);
    if (accessResult is not null)
    {
        return accessResult;
    }

    var apis = await gatewayRepository.GetAllAsync(cancellationToken);
    return Results.Ok(apis.Select(api => ToGatewayApiView(api, metrics)));
});

app.MapGet("/api/gateway/apis/{id}", async (
    string id,
    HttpRequest request,
    GatewayApiRepository gatewayRepository,
    GatewayMetrics metrics,
    CancellationToken cancellationToken) =>
{
    var accessResult = RequireManagementKey(request);
    if (accessResult is not null)
    {
        return accessResult;
    }

    var api = await gatewayRepository.GetByIdAsync(id, cancellationToken);
    return api is null
        ? Results.NotFound()
        : Results.Ok(ToGatewayApiView(api, metrics));
});

app.MapPost("/api/gateway/apis", async (
    HttpRequest request,
    GatewayApiInput input,
    GatewayApiRepository gatewayRepository,
    CancellationToken cancellationToken) =>
{
    var accessResult = RequireManagementKey(request);
    if (accessResult is not null)
    {
        return accessResult;
    }

    var validation = GatewayApiInputValidator.Validate(input);
    if (validation.Count > 0)
    {
        return Results.ValidationProblem(validation);
    }

    var apiKey = GatewayCredential.CreateApiKey();
    GatewayApi api;
    try
    {
        api = await gatewayRepository.CreateAsync(input, GatewayCredential.Hash(apiKey), cancellationToken);
    }
    catch (SqlException exception) when (exception.Number is 2601 or 2627)
    {
        return Results.Conflict(new { message = "An API with this name is already registered." });
    }

    return Results.Created(
        $"/api/gateway/apis/{api.Id}",
        new { api = ToGatewayApiView(api, app.Services.GetRequiredService<GatewayMetrics>()), apiKey });
});

app.MapPut("/api/gateway/apis/{id}", async (
    string id,
    HttpRequest request,
    GatewayApiInput input,
    GatewayApiRepository gatewayRepository,
    CancellationToken cancellationToken) =>
{
    var accessResult = RequireManagementKey(request);
    if (accessResult is not null)
    {
        return accessResult;
    }

    var validation = GatewayApiInputValidator.Validate(input);
    if (validation.Count > 0)
    {
        return Results.ValidationProblem(validation);
    }

    try
    {
        var updated = await gatewayRepository.UpdateAsync(id, input, cancellationToken);
        return updated
            ? Results.Ok(new { message = "API configuration updated." })
            : Results.NotFound();
    }
    catch (SqlException exception) when (exception.Number is 2601 or 2627)
    {
        return Results.Conflict(new { message = "An API with this name is already registered." });
    }
});

app.MapPost("/api/gateway/apis/{id}/rotate-key", async (
    string id,
    HttpRequest request,
    GatewayApiRepository gatewayRepository,
    CancellationToken cancellationToken) =>
{
    var accessResult = RequireManagementKey(request);
    if (accessResult is not null)
    {
        return accessResult;
    }

    var api = await gatewayRepository.GetByIdAsync(id, cancellationToken);
    if (api is null)
    {
        return Results.NotFound();
    }

    var apiKey = GatewayCredential.CreateApiKey();
    await gatewayRepository.UpdateApiKeyHashAsync(id, GatewayCredential.Hash(apiKey), cancellationToken);
    return Results.Ok(new { apiKey });
});

app.MapDelete("/api/gateway/apis/{id}", async (
    string id,
    HttpRequest request,
    GatewayApiRepository gatewayRepository,
    CancellationToken cancellationToken) =>
{
    var accessResult = RequireManagementKey(request);
    if (accessResult is not null)
    {
        return accessResult;
    }

    return await gatewayRepository.DeleteAsync(id, cancellationToken)
        ? Results.NoContent()
        : Results.NotFound();
});

app.MapGet("/api/gateway/metrics", (
    HttpRequest request,
    GatewayMetrics metrics) =>
{
    var accessResult = RequireManagementKey(request);
    return accessResult ?? Results.Ok(metrics.Snapshot());
});

app.MapGet("/api/blog/metrics", (
    HttpRequest request,
    BlogRequestMetrics metrics) =>
{
    var accessResult = RequireManagementKey(request);
    return accessResult ?? Results.Ok(metrics.Snapshot());
});

app.MapMethods("/gateway/{apiId}/{**path}", ["GET", "HEAD", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"], async Task (
    HttpContext context,
    string apiId,
    string? path,
    GatewayApiRepository gatewayRepository,
    GatewayMetrics metrics,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger) =>
{
    await GatewayProxy.ForwardAsync(
        context,
        apiId,
        path,
        gatewayRepository,
        metrics,
        httpClientFactory,
        logger);
});

GatewayApiView ToGatewayApiView(GatewayApi api, GatewayMetrics metrics)
{
    var (requests, failedRequests, averageLatencyMs) = metrics.ForApi(api.Id);
    return new GatewayApiView(
        api.Id,
        api.Name,
        api.Version,
        api.UpstreamUrl,
        $"/gateway/{api.Id}",
        requests,
        failedRequests,
        averageLatencyMs,
        api.Enabled ? "Aktywne" : "Wyłączone");
}

app.MapGet("/api/storage", (NoSqlDatabaseOptions mongo) => Results.Ok(new
{
    primary = new { provider = "SQL Server" },
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

app.MapGet("/api/workspace", (WorkspaceOptions options) =>
{
    var directory = new DirectoryInfo(options.RootPath);
    return Results.Ok(new
    {
        rootPath = options.RootPath,
        exists = directory.Exists,
        databaseProvider = "SQL Server"
    });
});

app.MapGet("/api/posts", async (IBlogRepository blogRepository) =>
{
    var posts = await blogRepository.GetAllAsync();
    return Results.Ok(posts);
}).CacheOutput("BlogPosts");

app.MapGet("/api/posts/latest", async (IBlogRepository blogRepository) =>
{
    var post = await blogRepository.GetLatestAsync();
    return post is null ? Results.NotFound() : Results.Ok(post);
}).CacheOutput("BlogPosts");

app.MapGet("/api/posts/{slug}", async (string slug, IBlogRepository blogRepository) =>
{
    var post = await blogRepository.GetBySlugAsync(slug);
    return post is null ? Results.NotFound() : Results.Ok(post);
}).CacheOutput("BlogPosts");

app.MapGet("/api/posts/{postId:int}/comments", async (int postId, IBlogRepository blogRepository) =>
{
    var comments = await blogRepository.GetCommentsAsync(postId);
    return Results.Ok(comments);
}).CacheOutput("BlogComments");

app.MapPost("/api/posts/{postId:int}/comments", async (
    int postId,
    BlogCommentInput input,
    IBlogRepository blogRepository,
    IRiskAnalyzer riskAnalyzer,
    IOutputCacheStore outputCacheStore,
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
    if (created is not null)
    {
        await outputCacheStore.EvictByTagAsync("blog-comments", cancellationToken);
    }
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
    IOutputCacheStore outputCacheStore,
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
    await outputCacheStore.EvictByTagAsync("blog-posts", cancellationToken);
    return Results.Created($"/api/posts/{created.Slug}", created);
});

app.MapPut("/api/posts/{id:int}", async (
    int id,
    BlogPostInput input,
    IBlogRepository blogRepository,
    IRiskAnalyzer riskAnalyzer,
    IBlockExplanationProvider blockExplanationProvider,
    IOutputCacheStore outputCacheStore,
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
    if (updated is not null)
    {
        await outputCacheStore.EvictByTagAsync("blog-posts", cancellationToken);
    }
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.Run();

public partial class Program
{
    private static string ResolvePath(string path, string basePath) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(path, basePath);

}
