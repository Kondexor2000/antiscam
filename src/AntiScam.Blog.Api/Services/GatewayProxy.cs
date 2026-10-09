using System.Diagnostics;
using AntiScam.Blog.Api.Gateway;
using AntiScam.Blog.Api.Security;

namespace AntiScam.Blog.Api.Services;

public static class GatewayProxy
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
        "Host"
    };

    public static async Task ForwardAsync(
        HttpContext context,
        string apiId,
        string? path,
        GatewayApiRepository repository,
        GatewayMetrics metrics,
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        var stopwatch = Stopwatch.StartNew();
        var statusCode = StatusCodes.Status500InternalServerError;
        try
        {
            var api = await repository.GetByIdAsync(apiId, context.RequestAborted);
            if (api is null)
            {
                statusCode = StatusCodes.Status404NotFound;
                context.Response.StatusCode = statusCode;
                return;
            }

            if (!api.Enabled)
            {
                statusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.StatusCode = statusCode;
                return;
            }

            var apiKey = context.Request.Headers["X-API-Key"].ToString();
            if (string.IsNullOrEmpty(apiKey) || !GatewayCredential.Matches(apiKey, api.ApiKeyHash))
            {
                statusCode = StatusCodes.Status401Unauthorized;
                context.Response.StatusCode = statusCode;
                context.Response.Headers.WWWAuthenticate = "ApiKey";
                return;
            }

            var baseUrl = api.UpstreamUrl.TrimEnd('/') + "/";
            var relativePath = path ?? string.Empty;
            var query = context.Request.QueryString.Value ?? string.Empty;
            var target = new Uri(baseUrl + relativePath + query, UriKind.Absolute);

            var requestConnectionHeaders = ParseConnectionHeaders(context.Request.Headers["Connection"]);
            using var outgoingRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);
            if (context.Request.ContentLength is > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
            {
                outgoingRequest.Content = new StreamContent(context.Request.Body);
            }

            foreach (var header in context.Request.Headers)
            {
                if (HopByHopHeaders.Contains(header.Key) ||
                    requestConnectionHeaders.Contains(header.Key) ||
                    string.Equals(header.Key, "X-API-Key", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(header.Key, "X-Control-Plane-Key", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!outgoingRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    outgoingRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            using var client = httpClientFactory.CreateClient("gateway");
            using var upstreamResponse = await client.SendAsync(
                outgoingRequest,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);

            statusCode = (int)upstreamResponse.StatusCode;
            context.Response.StatusCode = statusCode;
            CopyResponseHeaders(upstreamResponse, context.Response);

            if (upstreamResponse.Content is not null)
            {
                await upstreamResponse.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
            }
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Gateway request for API {ApiId} could not reach its upstream.", apiId);
            if (!context.Response.HasStarted)
            {
                statusCode = StatusCodes.Status502BadGateway;
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Bad Gateway",
                    status = statusCode,
                    detail = "The configured upstream could not be reached."
                }, context.RequestAborted);
            }
            else
            {
                statusCode = context.Response.StatusCode;
            }
        }
        catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
        {
            statusCode = StatusCodes.Status504GatewayTimeout;
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Gateway Timeout",
                    status = statusCode,
                    detail = "The configured upstream did not respond before the timeout."
                }, context.RequestAborted);
            }
        }
        catch (OperationCanceledException)
        {
            statusCode = 499;
            throw;
        }
        finally
        {
            metrics.Record(apiId, stopwatch.Elapsed, statusCode);
        }
    }

    private static void CopyResponseHeaders(HttpResponseMessage upstream, HttpResponse downstream)
    {
        var connectionHeaders = upstream.Headers.TryGetValues("Connection", out var values)
            ? ParseConnectionHeaders(values)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in upstream.Headers)
        {
            if (!HopByHopHeaders.Contains(header.Key) && !connectionHeaders.Contains(header.Key))
            {
                downstream.Headers[header.Key] = header.Value.ToArray();
            }
        }

        foreach (var header in upstream.Content.Headers)
        {
            if (!HopByHopHeaders.Contains(header.Key) &&
                !connectionHeaders.Contains(header.Key) &&
                !string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                downstream.Headers[header.Key] = header.Value.ToArray();
            }
        }

        downstream.Headers.Remove("transfer-encoding");
        downstream.Headers.Remove("connection");
    }

    private static HashSet<string> ParseConnectionHeaders(IEnumerable<string> values) =>
        values.SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
