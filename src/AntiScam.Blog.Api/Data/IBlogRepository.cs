using AntiScam.Blog.Api.Models;

namespace AntiScam.Blog.Api.Data;

public interface IBlogRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlogPost>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<BlogPost> CreateAsync(BlogPostInput input, CancellationToken cancellationToken = default);

    Task<BlogPost?> UpdateAsync(int id, BlogPostInput input, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
