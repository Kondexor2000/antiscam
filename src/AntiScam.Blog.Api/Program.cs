using AntiScam.Blog.Api;
using AntiScam.Blog.Api.Data;
using AntiScam.Blog.Api.Models;
using AntiScam.Blog.Api.Services;

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

builder.Services.AddSingleton(new WorkspaceOptions(workspacePath));
builder.Services.AddSingleton(new BlogDatabaseOptions(databasePath));
builder.Services.AddSingleton<ISlugGenerator, SlugGenerator>();
builder.Services.AddSingleton<IRiskAnalyzer, RiskAnalyzer>();
builder.Services.AddSingleton<IBlockExplanationProvider, PythonAiBlockExplanationProvider>();
builder.Services.AddSingleton<IBlogRepository, SqliteBlogRepository>();

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
    storage = "SQLite"
}));

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

app.MapGet("/api/posts/{slug}", async (string slug, IBlogRepository blogRepository) =>
{
    var post = await blogRepository.GetBySlugAsync(slug);
    return post is null ? Results.NotFound() : Results.Ok(post);
});

app.MapPost("/api/posts", async (
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

app.MapDelete("/api/posts/{id:int}", async (int id, IBlogRepository blogRepository) =>
{
    var deleted = await blogRepository.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();

public partial class Program
{
}
