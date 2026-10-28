using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using AntiScam.Blog.Api.Data;
using AntiScam.Blog.Api.Models;
using AntiScam.Blog.Api.Services;
using System.Net;
using Microsoft.Data.SqlClient;

namespace AntiScam.Blog.Api.Tests.Integration;

public sealed class BlogApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private string? _previousControlPlaneKey;
    private string? _previousConnectionString;
    private readonly string _databaseName = $"AntiScamBlogTests_{Guid.NewGuid():N}";

    private string ConnectionString
    {
        get
        {
            var baseConnectionString = Environment.GetEnvironmentVariable("ANTISCAM_TEST_SQLSERVER")
                ?? "Server=(localdb)\\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True";
            var builder = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = _databaseName };
            return builder.ConnectionString;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _previousControlPlaneKey = Environment.GetEnvironmentVariable("ANTISCAM_CONTROL_PLANE_KEY");
        _previousConnectionString = Environment.GetEnvironmentVariable("ANTISCAM_BLOG_CONNECTION_STRING");
        Environment.SetEnvironmentVariable("ANTISCAM_CONTROL_PLANE_KEY", BlogApiTests.ManagementKey);
        Environment.SetEnvironmentVariable("ANTISCAM_BLOG_CONNECTION_STRING", ConnectionString);
        CreateTestDatabase();
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
        Environment.SetEnvironmentVariable("ANTISCAM_BLOG_CONNECTION_STRING", _previousConnectionString);
        Environment.SetEnvironmentVariable("ANTISCAM_CONTROL_PLANE_KEY", _previousControlPlaneKey);
        SqlConnection.ClearAllPools();
        try
        {
            var builder = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" };
            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"IF DB_ID(N'{_databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]; END";
            command.ExecuteNonQuery();
        }
        catch (SqlException)
        {
            // The database may not have been created if the test host failed during startup.
        }
    }

    private void CreateTestDatabase()
    {
        var connectionBuilder = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" };
        using var connection = new SqlConnection(connectionBuilder.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(N'{_databaseName}') IS NULL CREATE DATABASE [{_databaseName}]";
        command.ExecuteNonQuery();
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
