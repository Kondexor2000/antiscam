using System.Net;
using System.Net.Http.Json;
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
    public async Task HomePage_ReturnsStaticHtml()
    {
        var html = await _client.GetStringAsync("/");

        Assert.Contains("AntiScam Blog", html);
        Assert.Contains("Blog bezpieczeństwa", html);
    }
}
