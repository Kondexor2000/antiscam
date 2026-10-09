using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using AntiScam.Blog.Api.Data;
using AntiScam.Blog.Api.Models;
using AntiScam.Blog.Api.Services;
using System.Net;
using Microsoft.Data.Sqlite;

namespace AntiScam.Blog.Api.Tests.Integration;

public sealed class BlogApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private string? _previousControlPlaneKey;

    public string DatabasePath { get; } = Path.Combine(
        Path.GetTempPath(),
        "antiscam-blog-tests",
        $"{Guid.NewGuid():N}.sqlite");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _previousControlPlaneKey = Environment.GetEnvironmentVariable("ANTISCAM_CONTROL_PLANE_KEY");
        Environment.SetEnvironmentVariable("ANTISCAM_CONTROL_PLANE_KEY", BlogApiTests.ManagementKey);
        Environment.SetEnvironmentVariable("ANTISCAM_BLOG_DB", DatabasePath);
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ControlPlane:ManagementKey"] = BlogApiTests.ManagementKey
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBlockExplanationProvider>();
            services.AddSingleton<IBlockExplanationProvider, TestBlockExplanationProvider>();
            services.RemoveAll<IScamIncidentStore>();
            services.AddSingleton<IScamIncidentStore, NullScamIncidentStore>();
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory, TestGatewayHttpClientFactory>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Environment.SetEnvironmentVariable("ANTISCAM_BLOG_DB", null);
        Environment.SetEnvironmentVariable("ANTISCAM_CONTROL_PLANE_KEY", _previousControlPlaneKey);
        SqliteConnection.ClearAllPools();

        if (File.Exists(DatabasePath))
        {
            File.Delete(DatabasePath);
        }
    }

    private sealed class TestBlockExplanationProvider : IBlockExplanationProvider
    {
        public Task<string> ExplainAsync(BlogPostInput input, RiskAssessment risk, CancellationToken cancellationToken) =>
            Task.FromResult($"AI z ai.py wyjasnia blokade. Sygnały: {string.Join("; ", risk.Reasons)}.");
    }

    private sealed class TestGatewayHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new TestGatewayMessageHandler());
    }

    private sealed class TestGatewayMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(request.RequestUri?.PathAndQuery ?? string.Empty)
            });
    }
}
