using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AntiScam.Blog.Api.Models;
using AntiScam.Blog.Api.Gateway;

namespace AntiScam.Blog.Api.Tests.Integration;

public sealed class BlogApiTests : IClassFixture<BlogApiFactory>
{
    public const string ManagementKey = "test-control-plane-management-key";

    private readonly HttpClient _client;

    public BlogApiTests(BlogApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPosts_ReturnsSeededPosts()
    {
        var posts = await _client.GetFromJsonAsync<List<BlogPost>>("/api/posts");

        Assert.NotNull(posts);
        Assert.True(posts.Count >= 2);
    }

    [Fact]
    public async Task GetLatestPost_ReturnsOnlyTheMostRecentlyPublishedPost()
    {
        var allPosts = await _client.GetFromJsonAsync<List<BlogPost>>("/api/posts");
        var latestPost = await _client.GetFromJsonAsync<BlogPost>("/api/posts/latest");

        Assert.NotNull(allPosts);
        Assert.NotNull(latestPost);
        Assert.Equal(allPosts[0].Id, latestPost.Id);
    }

    [Fact]
    public async Task BlogApiMetrics_CountsBlogEndpointRequestsSeparately()
    {
        using var initialMetricsRequest = new HttpRequestMessage(HttpMethod.Get, "/api/blog/metrics");
        initialMetricsRequest.Headers.Add("X-Control-Plane-Key", ManagementKey);
        using var initialMetricsResponse = await _client.SendAsync(initialMetricsRequest);
        initialMetricsResponse.EnsureSuccessStatusCode();
        using var initialMetrics = JsonDocument.Parse(await initialMetricsResponse.Content.ReadAsStringAsync());
        var initialRequests = initialMetrics.RootElement.GetProperty("requests").GetInt64();

        using var blogResponse = await _client.GetAsync("/api/posts/latest");
        Assert.True(blogResponse.IsSuccessStatusCode || blogResponse.StatusCode == HttpStatusCode.NotFound);

        using var metricsRequest = new HttpRequestMessage(HttpMethod.Get, "/api/blog/metrics");
        metricsRequest.Headers.Add("X-Control-Plane-Key", ManagementKey);
        using var metricsResponse = await _client.SendAsync(metricsRequest);
        metricsResponse.EnsureSuccessStatusCode();
        using var metrics = JsonDocument.Parse(await metricsResponse.Content.ReadAsStringAsync());

        Assert.True(metrics.RootElement.GetProperty("requests").GetInt64() >= initialRequests + 1);
    }

    [Fact]
    public async Task Storage_ReportsSqlServerAsPrimaryAndEnablesMongoIncidentStore()
    {
        using var response = await _client.GetAsync("/api/storage");

        response.EnsureSuccessStatusCode();
        using var storage = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("SQL Server", storage.RootElement.GetProperty("primary").GetProperty("provider").GetString());
        Assert.Equal("MongoDB", storage.RootElement.GetProperty("secondary").GetProperty("provider").GetString());
        Assert.True(storage.RootElement.GetProperty("secondary").GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task GetIncidents_ReturnsEmptyListWithTheTestIncidentStore()
    {
        var incidents = await _client.GetFromJsonAsync<List<JsonElement>>("/api/incidents");

        Assert.NotNull(incidents);
        Assert.Empty(incidents);
    }

    [Fact]
    public async Task CreatePost_PersistsPostAndReturnsCreated()
    {
        var input = new BlogPostInput(
            "Nowy alarm phishingowy",
            "Opis świeżego scenariusza ataku.",
            "Nie klikaj w skrócone linki podszywające się pod operatora płatności.",
            "Tester");

        var response = await _client.PostAsJsonAsync("/api/posts", input);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<BlogPost>();
        Assert.NotNull(created);
        Assert.Equal("nowy-alarm-phishingowy", created.Slug);

        var fetched = await _client.GetFromJsonAsync<BlogPost>($"/api/posts/{created.Slug}");
        Assert.Equal(input.Title, fetched?.Title);
    }

    [Fact]
    public async Task CreatePost_ReturnsValidationProblemForEmptyTitle()
    {
        var input = new BlogPostInput("", "Opis", "Treść", "Tester");

        var response = await _client.PostAsJsonAsync("/api/posts", input);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePost_DoesNotPersistPostWhenRiskIsDetected()
    {
        var input = new BlogPostInput(
            "Pilny BLIK",
            "Konto zablokowane.",
            "Wyślij kod BLIK 123456 natychmiast i kliknij teraz.",
            "Scammer");

        var response = await _client.PostAsJsonAsync("/api/posts", input);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var risk = problem.RootElement.GetProperty("risk");
        var aiExplanation = problem.RootElement.GetProperty("aiExplanation").GetString();
        Assert.Contains("ai.py", aiExplanation);
        Assert.Contains("BLIK CONFIRMED", aiExplanation);
        Assert.Contains("zablokowana", risk.GetProperty("blockExplanation").GetString());
        Assert.Contains("BLIK CONFIRMED", risk.GetProperty("blockExplanation").GetString());

        var fetched = await _client.GetAsync("/api/posts/pilny-blik");
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);
    }

    [Fact]
    public async Task CreatePost_BlocksObfuscatedBlikAndTyposquattingLinks()
    {
        var input = new BlogPostInput(
            "n",
            "n",
            "B L I K 123456 k-o-d natychmiast https://g00gle.com/login",
            "AntiScam Team");

        var response = await _client.PostAsJsonAsync("/api/posts", input);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var risk = problem.RootElement.GetProperty("risk");
        var blockExplanation = risk.GetProperty("blockExplanation").GetString();
        Assert.Contains("BLIK CONFIRMED", blockExplanation);
        Assert.Contains("Typosquatting links", blockExplanation);

        var fetched = await _client.GetAsync("/api/posts/n");
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);
    }

    [Fact]
    public async Task CreateComment_PersistsLowRiskComment()
    {
        var posts = await _client.GetFromJsonAsync<List<BlogPost>>("/api/posts");
        Assert.NotNull(posts);
        var input = new BlogCommentInput("Dziekuje za przydatne wskazowki.", "Czytelnik");

        var response = await _client.PostAsJsonAsync($"/api/posts/{posts[0].Id}/comments", input);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var comments = await _client.GetFromJsonAsync<List<BlogComment>>($"/api/posts/{posts[0].Id}/comments");
        Assert.Contains(comments!, comment => comment.Content == input.Content && comment.Author == input.Author);
    }

    [Fact]
    public async Task CreateComment_BlocksScamContent()
    {
        var posts = await _client.GetFromJsonAsync<List<BlogPost>>("/api/posts");
        Assert.NotNull(posts);
        var input = new BlogCommentInput("Wyslij kod BLIK 123456 natychmiast.", "Oszust");

        var response = await _client.PostAsJsonAsync($"/api/posts/{posts[0].Id}/comments", input);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("HIGH RISK", problem.RootElement.GetProperty("risk").GetProperty("status").GetString());

        var comments = await _client.GetFromJsonAsync<List<BlogComment>>($"/api/posts/{posts[0].Id}/comments");
        Assert.DoesNotContain(comments!, comment => comment.Content == input.Content);
    }

    [Fact]
    public async Task HomePage_ReturnsStaticHtml()
    {
        var html = await _client.GetStringAsync("/");

        Assert.Contains("API Control Plane", html);
        Assert.Contains("href=\"/blog.html\"", html);
        Assert.Contains("Blog bezpieczeństwa", html);
        Assert.Contains("id=\"api-dialog\"", html);
        Assert.Contains("id=\"key-dialog\"", html);
        Assert.Contains("id=\"api-upstream\"", html);
        Assert.Contains("id=\"request-dialog\"", html);
        Assert.Contains("id=\"request-form\"", html);
        Assert.Contains("id=\"request-api-key\"", html);
        Assert.Contains("id=\"request-method\"", html);
        Assert.Contains("id=\"request-response\"", html);
    }

    [Fact]
    public async Task GatewayManagement_RequiresControlPlaneKey()
    {
        using var response = await _client.GetAsync("/api/gateway/apis");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GatewayApi_RegistersProxyWithGeneratedClientKeyAndRecordsMetrics()
    {
        var input = new GatewayApiInput(
            $"Payments {Guid.NewGuid():N}",
            "v1",
            "http://upstream.test/base");
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/gateway/apis")
        {
            Content = JsonContent.Create(input)
        };
        createRequest.Headers.Add("X-Control-Plane-Key", ManagementKey);

        using var createResponse = await _client.SendAsync(createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var api = created.RootElement.GetProperty("api");
        var apiId = api.GetProperty("id").GetString()!;
        var apiKey = created.RootElement.GetProperty("apiKey").GetString()!;
        Assert.Equal($"/gateway/{apiId}", api.GetProperty("gatewayUrl").GetString());

        using var detailRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/gateway/apis/{apiId}");
        detailRequest.Headers.Add("X-Control-Plane-Key", ManagementKey);
        using var detailResponse = await _client.SendAsync(detailRequest);
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        using var missingKey = await _client.GetAsync($"/gateway/{apiId}/orders?take=2");
        Assert.Equal(HttpStatusCode.Unauthorized, missingKey.StatusCode);

        using var proxyRequest = new HttpRequestMessage(HttpMethod.Get, $"/gateway/{apiId}/orders?take=2");
        proxyRequest.Headers.Add("X-API-Key", apiKey);
        using var proxyResponse = await _client.SendAsync(proxyRequest);

        Assert.Equal(HttpStatusCode.OK, proxyResponse.StatusCode);
        Assert.Equal("/base/orders?take=2", await proxyResponse.Content.ReadAsStringAsync());

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/gateway/apis");
        listRequest.Headers.Add("X-Control-Plane-Key", ManagementKey);
        using var listResponse = await _client.SendAsync(listRequest);
        using var catalog = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var registered = catalog.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("id").GetString() == apiId);
        Assert.Equal(2, registered.GetProperty("requests").GetInt64());
        Assert.Equal(1, registered.GetProperty("failedRequests").GetInt64());

        using var metricsRequest = new HttpRequestMessage(HttpMethod.Get, "/api/gateway/metrics");
        metricsRequest.Headers.Add("X-Control-Plane-Key", ManagementKey);
        using var metricsResponse = await _client.SendAsync(metricsRequest);
        using var metrics = JsonDocument.Parse(await metricsResponse.Content.ReadAsStringAsync());
        Assert.True(metrics.RootElement.GetProperty("requests").GetInt64() >= 2);
    }

    [Fact]
    public async Task GatewayApi_RejectsInvalidUpstreamAddress()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/gateway/apis")
        {
            Content = JsonContent.Create(new GatewayApiInput("Invalid API", "v1", "file:///etc/passwd"))
        };
        request.Headers.Add("X-Control-Plane-Key", ManagementKey);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GatewayApi_CanUpdateRotateKeyAndDeleteRegistration()
    {
        var input = new GatewayApiInput(
            $"Orders {Guid.NewGuid():N}",
            "v1",
            "http://upstream.test/orders");
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/gateway/apis")
        {
            Content = JsonContent.Create(input)
        };
        createRequest.Headers.Add("X-Control-Plane-Key", ManagementKey);
        using var createResponse = await _client.SendAsync(createRequest);
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var apiId = created.RootElement.GetProperty("api").GetProperty("id").GetString()!;
        var oldApiKey = created.RootElement.GetProperty("apiKey").GetString()!;

        using var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/gateway/apis/{apiId}")
        {
            Content = JsonContent.Create(input with { Version = "v2", Enabled = false })
        };
        updateRequest.Headers.Add("X-Control-Plane-Key", ManagementKey);
        using var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var disabledRequest = new HttpRequestMessage(HttpMethod.Get, $"/gateway/{apiId}/health");
        disabledRequest.Headers.Add("X-API-Key", oldApiKey);
        using var disabledResponse = await _client.SendAsync(disabledRequest);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, disabledResponse.StatusCode);

        using var rotateRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/gateway/apis/{apiId}/rotate-key");
        rotateRequest.Headers.Add("X-Control-Plane-Key", ManagementKey);
        using var rotateResponse = await _client.SendAsync(rotateRequest);
        using var rotated = JsonDocument.Parse(await rotateResponse.Content.ReadAsStringAsync());
        var newApiKey = rotated.RootElement.GetProperty("apiKey").GetString()!;
        Assert.NotEqual(oldApiKey, newApiKey);

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/gateway/apis/{apiId}");
        deleteRequest.Headers.Add("X-Control-Plane-Key", ManagementKey);
        using var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var deletedRequest = new HttpRequestMessage(HttpMethod.Get, $"/gateway/{apiId}/health");
        deletedRequest.Headers.Add("X-API-Key", newApiKey);
        using var deletedResponse = await _client.SendAsync(deletedRequest);
        Assert.Equal(HttpStatusCode.NotFound, deletedResponse.StatusCode);
    }
}
