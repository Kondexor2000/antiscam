using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace AntiScam.Blog.Api.Tests.Integration;

public sealed class BlogApiFactory : WebApplicationFactory<Program>, IDisposable
{
    public string DatabasePath { get; } = Path.Combine(
        Path.GetTempPath(),
        "antiscam-blog-tests",
        $"{Guid.NewGuid():N}.sqlite");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ANTISCAM_BLOG_DB", DatabasePath);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Environment.SetEnvironmentVariable("ANTISCAM_BLOG_DB", null);

        if (File.Exists(DatabasePath))
        {
            File.Delete(DatabasePath);
        }
    }
}
