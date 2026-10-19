using System.Collections.Concurrent;

namespace CheapHelpers.Llm;

/// <summary>
/// Purpose tags for LLM spend, so the usage tracker buckets calls the same way everywhere.
/// The tracker accepts any string; apps add their own tags next to these.
/// </summary>
public static class UsagePurpose
{
    public const string Chat = "chat";
}

/// <summary>
/// Session-scoped tally of LLM token spend, bucketed by purpose. Thread-safe. Counts are
/// in memory and reset on demand or restart. The clients check <see cref="IsOverBudget"/>
/// against <see cref="LlmOptions.MaxSessionTokens"/> before every call.
/// </summary>
public sealed class LlmUsageTracker
{
    private const string TotalRowName = "total";
    private const int CacheReadBillingDivisor = 10;

    private readonly ConcurrentDictionary<string, Bucket> _byPurpose = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Fires after every recorded call so a UI can refresh. Subscribers must marshal to their own thread.</summary>
    public event Action? Changed;

    /// <summary>
    /// Total tokens spent this session across all purposes. Summed from the buckets rather
    /// than a separate running field, so it can never drift from the per-purpose totals.
    /// </summary>
    public long TotalTokens => _byPurpose.Values.Sum(bucket => bucket.TotalTokens);

    /// <summary>
    /// True once the session token total reaches <paramref name="maxTokens"/>. A
    /// <paramref name="maxTokens"/> of 0 (or negative) means no cap and always returns false.
    /// </summary>
    public bool IsOverBudget(long maxTokens) => maxTokens > 0 && TotalTokens >= maxTokens;

    /// <summary>Records one call's token usage under a purpose. Null or zero usage is ignored.</summary>
    public void Record(string purpose, LlmUsage? usage)
    {
        if (usage is null) return;
        if (usage.PromptTokens == 0 && usage.CompletionTokens == 0 && usage.TotalTokens == 0
            && usage.CacheReadInputTokens == 0 && usage.CacheCreationInputTokens == 0) return;

        var bucket = _byPurpose.GetOrAdd(
            string.IsNullOrWhiteSpace(purpose) ? UsagePurpose.Chat : purpose,
            _ => new Bucket());
        bucket.Add(usage);
        Changed?.Invoke();
    }

    /// <summary>Per-purpose totals plus a grand total, ordered by total tokens descending.</summary>
    public UsageSnapshot Snapshot()
    {
        var rows = _byPurpose
            .Select(kv => kv.Value.ToRow(kv.Key))
            .OrderByDescending(row => row.TotalTokens)
            .ToList();

        var grand = new UsageRow(
            TotalRowName,
            rows.Sum(r => r.Calls),
            rows.Sum(r => r.PromptTokens),
            rows.Sum(r => r.CompletionTokens),
            rows.Sum(r => r.TotalTokens),
            rows.Sum(r => r.CacheReadTokens),
            rows.Sum(r => r.CacheCreationTokens));

        return new UsageSnapshot(rows, grand);
    }

    /// <summary>Clears all counts.</summary>
    public void Reset()
    {
        _byPurpose.Clear();
        Changed?.Invoke();
    }

    private sealed class Bucket
    {
        private long _calls;
        private long _prompt;
        private long _completion;
        private long _cacheRead;
        private long _cacheCreation;
        private long _total;
        private readonly Lock _lock = new();

        public void Add(LlmUsage usage)
        {
            lock (_lock)
            {
                _calls++;
                _prompt += usage.PromptTokens;
                _completion += usage.CompletionTokens;
                _cacheRead += usage.CacheReadInputTokens;
                _cacheCreation += usage.CacheCreationInputTokens;
                // Some providers omit total_tokens; derive it when missing. Cache reads
                // bill at a tenth of the input rate, so they count a tenth toward the
                // session budget: the cap bounds spend, not token traffic.
                var billable = usage.TotalTokens > 0 ? usage.TotalTokens : usage.PromptTokens + usage.CompletionTokens;
                _total += billable + usage.CacheCreationInputTokens + usage.CacheReadInputTokens / CacheReadBillingDivisor;
            }
        }

        public long TotalTokens
        {
            get { lock (_lock) return _total; }
        }

        public UsageRow ToRow(string purpose)
        {
            lock (_lock)
                return new UsageRow(purpose, _calls, _prompt, _completion, _total, _cacheRead, _cacheCreation);
        }
    }
}

public sealed record UsageRow(
    string Purpose,
    long Calls,
    long PromptTokens,
    long CompletionTokens,
    long TotalTokens,
    long CacheReadTokens = 0,
    long CacheCreationTokens = 0);

public sealed record UsageSnapshot(IReadOnlyList<UsageRow> ByPurpose, UsageRow Total);
