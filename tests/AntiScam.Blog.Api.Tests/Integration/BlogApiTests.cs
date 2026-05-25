using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AntiScam.Blog.Api.Models;

namespace AntiScam.Blog.Api.Tests.Integration;

public sealed class BlogApiTests : IClassFixture<BlogApiFactory>
{
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
    public async Task HomePage_ReturnsStaticHtml()
    {
        var html = await _client.GetStringAsync("/");

        Assert.Contains("AntiScam Blog", html);
        Assert.Contains("Blog bezpieczeństwa", html);
    }
}
