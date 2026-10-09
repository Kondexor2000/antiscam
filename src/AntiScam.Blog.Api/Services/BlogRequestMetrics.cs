namespace AntiScam.Blog.Api.Services;

public sealed class BlogRequestMetrics
{
    private long _requests;
    private long _failedRequests;
    private long _totalElapsedTicks;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    public void Record(TimeSpan elapsed, int statusCode)
    {
        Interlocked.Increment(ref _requests);
        Interlocked.Add(ref _totalElapsedTicks, elapsed.Ticks);
        if (statusCode >= 400)
        {
            Interlocked.Increment(ref _failedRequests);
        }
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
            averageLatencyMs = requests == 0
                ? 0
                : TimeSpan.FromTicks(elapsedTicks / requests).TotalMilliseconds
        };
    }
}
