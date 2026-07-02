namespace App.Configuration;

/// <summary>
/// Serializes all Mistral API calls and enforces a minimum interval between them.
/// The free tier allows ~1 request/second — without this gate, five concurrent
/// scraper sources hammer the API and everything turns into 429 retries.
/// </summary>
public class MistralRateLimiter
{
    private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(1100);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTime _nextAllowedUtc = DateTime.MinValue;

    public async Task WaitAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var wait = _nextAllowedUtc - DateTime.UtcNow;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, ct);
            _nextAllowedUtc = DateTime.UtcNow + MinInterval;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Pushes the next allowed call further into the future, e.g. after a 429 with Retry-After.</summary>
    public void Penalize(TimeSpan delay)
    {
        var until = DateTime.UtcNow + delay;
        if (until > _nextAllowedUtc)
            _nextAllowedUtc = until;
    }
}
