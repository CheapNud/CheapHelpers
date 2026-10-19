namespace CheapHelpers.Llm.Tests;

/// <summary>Pins the per-purpose token tally: recording, bucketing and the grand total must be correct and thread-safe.</summary>
public sealed class LlmUsageTrackerTests
{
    private const string Summary = "summary";
    private const string Vision = "vision";

    private static LlmUsage Usage(int prompt, int completion, int? total = null) =>
        new() { PromptTokens = prompt, CompletionTokens = completion, TotalTokens = total ?? prompt + completion };

    [Fact]
    public void TotalTokens_tracks_the_running_grand_total_across_purposes()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(UsagePurpose.Chat, Usage(100, 50));     // 150
        tracker.Record(Vision, Usage(1000, 30));  // 1030
        Assert.Equal(1180, tracker.TotalTokens);
    }

    [Fact]
    public void IsOverBudget_trips_once_the_total_reaches_the_cap()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(UsagePurpose.Chat, Usage(60_000, 40_000)); // 100k

        Assert.False(tracker.IsOverBudget(200_000));   // under
        Assert.True(tracker.IsOverBudget(100_000));    // exactly at the cap = over
        Assert.True(tracker.IsOverBudget(50_000));     // past
    }

    [Fact]
    public void IsOverBudget_is_disabled_when_cap_is_zero_or_negative()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(UsagePurpose.Chat, Usage(5_000_000, 5_000_000)); // huge
        Assert.False(tracker.IsOverBudget(0));
        Assert.False(tracker.IsOverBudget(-1));
    }

    [Fact]
    public void Reset_zeroes_the_running_total_too()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(UsagePurpose.Chat, Usage(100, 50));
        tracker.Reset();
        Assert.Equal(0, tracker.TotalTokens);
        Assert.False(tracker.IsOverBudget(1));
    }

    [Fact]
    public void Records_accumulate_within_a_purpose()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(UsagePurpose.Chat, Usage(100, 50));
        tracker.Record(UsagePurpose.Chat, Usage(20, 10));

        var snapshot = tracker.Snapshot();
        var chat = Assert.Single(snapshot.ByPurpose);
        Assert.Equal(UsagePurpose.Chat, chat.Purpose);
        Assert.Equal(2, chat.Calls);
        Assert.Equal(120, chat.PromptTokens);
        Assert.Equal(60, chat.CompletionTokens);
        Assert.Equal(180, chat.TotalTokens);
    }

    [Fact]
    public void Different_purposes_are_bucketed_separately_and_summed_in_the_total()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(UsagePurpose.Chat, Usage(100, 50));
        tracker.Record(Summary, Usage(40, 20));
        tracker.Record(Vision, Usage(1000, 30));

        var snapshot = tracker.Snapshot();
        Assert.Equal(3, snapshot.ByPurpose.Count);
        Assert.Equal(3, snapshot.Total.Calls);
        Assert.Equal(1140, snapshot.Total.PromptTokens);
        Assert.Equal(100, snapshot.Total.CompletionTokens);
        Assert.Equal(1240, snapshot.Total.TotalTokens);
    }

    [Fact]
    public void Snapshot_is_ordered_by_total_tokens_descending()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(Summary, Usage(10, 5));      // 15
        tracker.Record(Vision, Usage(1000, 200));    // 1200
        tracker.Record(UsagePurpose.Chat, Usage(100, 50));        // 150

        var purposes = tracker.Snapshot().ByPurpose.Select(r => r.Purpose).ToList();
        Assert.Equal([Vision, UsagePurpose.Chat, Summary], purposes);
    }

    [Fact]
    public void Total_tokens_are_derived_when_the_provider_omits_them()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(UsagePurpose.Chat, Usage(80, 20, total: 0)); // provider sent total_tokens=0

        Assert.Equal(100, tracker.Snapshot().Total.TotalTokens);
    }

    [Fact]
    public void Null_or_empty_usage_is_ignored()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(UsagePurpose.Chat, null);
        tracker.Record(UsagePurpose.Chat, Usage(0, 0, total: 0));

        Assert.Empty(tracker.Snapshot().ByPurpose);
        Assert.Equal(0, tracker.Snapshot().Total.Calls);
    }

    [Fact]
    public void Blank_purpose_falls_back_to_chat()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record("  ", Usage(10, 5));

        Assert.Equal(UsagePurpose.Chat, Assert.Single(tracker.Snapshot().ByPurpose).Purpose);
    }

    [Fact]
    public void Reset_clears_all_buckets()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(UsagePurpose.Chat, Usage(100, 50));
        tracker.Reset();

        Assert.Empty(tracker.Snapshot().ByPurpose);
        Assert.Equal(0, tracker.Snapshot().Total.TotalTokens);
    }

    [Fact]
    public void Changed_fires_on_record_and_reset()
    {
        var tracker = new LlmUsageTracker();
        var fires = 0;
        tracker.Changed += () => Interlocked.Increment(ref fires);

        tracker.Record(UsagePurpose.Chat, Usage(10, 5));
        tracker.Record(UsagePurpose.Chat, null); // ignored, no fire
        tracker.Reset();

        Assert.Equal(2, fires);
    }

    [Fact]
    public void Concurrent_records_do_not_lose_counts()
    {
        var tracker = new LlmUsageTracker();
        Parallel.For(0, 1000, _ => tracker.Record(UsagePurpose.Chat, Usage(1, 1)));

        var chat = Assert.Single(tracker.Snapshot().ByPurpose);
        Assert.Equal(1000, chat.Calls);
        Assert.Equal(1000, chat.PromptTokens);
        Assert.Equal(2000, chat.TotalTokens);
    }

    [Fact]
    public void Cache_reads_count_one_tenth_toward_the_budget_and_show_in_their_own_column()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(UsagePurpose.Chat, new LlmUsage
        {
            PromptTokens = 100,
            CompletionTokens = 50,
            CacheReadInputTokens = 2000,
            CacheCreationInputTokens = 300,
        });

        // 100 + 50 + 300 + 2000/10
        Assert.Equal(650, tracker.TotalTokens);

        var row = Assert.Single(tracker.Snapshot().ByPurpose);
        Assert.Equal(2000, row.CacheReadTokens);
        Assert.Equal(300, row.CacheCreationTokens);
        Assert.Equal(650, tracker.Snapshot().Total.TotalTokens);
    }

    [Fact]
    public void Compat_usage_without_cache_fields_is_unchanged()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(UsagePurpose.Chat, Usage(100, 50));
        Assert.Equal(150, tracker.TotalTokens);
        Assert.Equal(0, tracker.Snapshot().Total.CacheReadTokens);
    }
}
