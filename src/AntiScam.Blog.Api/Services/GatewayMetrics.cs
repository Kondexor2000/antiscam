using System.Collections.Concurrent;

namespace AntiScam.Blog.Api.Services;

public sealed class GatewayMetrics
{
    private readonly ConcurrentDictionary<string, ApiCounter> _apiCounters = new();
    private long _requests;
    private long _failedRequests;
    private long _totalElapsedTicks;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    public void Record(string apiId, TimeSpan elapsed, int statusCode)
    {
        Interlocked.Increment(ref _requests);
        Interlocked.Add(ref _totalElapsedTicks, elapsed.Ticks);
        if (statusCode >= 400)
        {
            Interlocked.Increment(ref _failedRequests);
        }

        _apiCounters.GetOrAdd(apiId, static _ => new ApiCounter()).Record(elapsed, statusCode);
    }

    public object Snapshot()
    {
        var requests = Interlocked.Read(ref _requests);
        var elapsedTicks = Interlocked.Read(ref _totalElapsedTicks);

        return new
        {
            startedAt = _startedAt,
            uptimeSeconds = (long)(DateTimeOffset.UtcNow - _startedAt).TotalSeconds,
            requests,
            failedRequests = Interlocked.Read(ref _failedRequests),
            averageLatencyMs = requests == 0 ? 0 : TimeSpan.FromTicks(elapsedTicks / requests).TotalMilliseconds,
            apis = _apiCounters.ToDictionary(
                entry => entry.Key,
                entry =>
                {
                    var (apiRequests, apiFailures, apiLatency) = entry.Value.Snapshot();
                    return new
                    {
                        requests = apiRequests,
                        failedRequests = apiFailures,
                        averageLatencyMs = apiLatency
                    };
                })
        };
    }

    public (long Requests, long FailedRequests, double? AverageLatencyMs) ForApi(string apiId)
    {
        return _apiCounters.TryGetValue(apiId, out var counter)
            ? counter.Snapshot()
            : (0, 0, null);
    }

    private sealed class ApiCounter
    {
        private long _requests;
        private long _failedRequests;
        private long _elapsedTicks;

        public void Record(TimeSpan elapsed, int statusCode)
        {
            Interlocked.Increment(ref _requests);
            Interlocked.Add(ref _elapsedTicks, elapsed.Ticks);
            if (statusCode >= 400)
            {
                Interlocked.Increment(ref _failedRequests);
            }
        }

        public (long Requests, long FailedRequests, double? AverageLatencyMs) Snapshot()
        {
            var requests = Interlocked.Read(ref _requests);
            var elapsedTicks = Interlocked.Read(ref _elapsedTicks);
            return (
                requests,
                Interlocked.Read(ref _failedRequests),
                requests == 0 ? null : TimeSpan.FromTicks(elapsedTicks / requests).TotalMilliseconds);
        }
    }
}
