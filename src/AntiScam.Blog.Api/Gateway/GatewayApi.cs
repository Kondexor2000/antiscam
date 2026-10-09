namespace AntiScam.Blog.Api.Gateway;

public sealed record GatewayApi(
    string Id,
    string Name,
    string Version,
    string UpstreamUrl,
    string ApiKeyHash,
    DateTimeOffset CreatedAt,
    bool Enabled);

public sealed record GatewayApiInput(
    string Name,
    string Version,
    string UpstreamUrl,
    bool Enabled = true);

public sealed record GatewayApiView(
    string Id,
    string Name,
    string Version,
    string UpstreamUrl,
    string GatewayUrl,
    long Requests,
    long FailedRequests,
    double? AverageLatencyMs,
    string Status);
