namespace CheapHelpers.Jobs.Tests;

public sealed class JobQueueTests
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Enqueued_job_runs_to_completion_with_its_payload()
    {
        var ct = TestContext.Current.CancellationToken;
        using var harness = new JobQueueHarness();
        var payload = new TestPayload("report", 3);

        var id = await harness.Queue.EnqueueAsync(payload, "export", ct);
        await harness.RunAsync(ct);

        Assert.Equal(payload, Assert.Single(harness.Processor.Received));
        var job = await harness.GetAsync(id);
        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.Equal(100, job.ProgressPercent);
        Assert.Equal(harness.Time.GetUtcNow(), job.CompletedAt);
        Assert.Equal("export", job.Kind);
    }

    [Fact]
    public async Task Failed_attempt_goes_back_to_pending_with_a_two_second_backoff()
    {
        var ct = TestContext.Current.CancellationToken;
        using var harness = new JobQueueHarness();
        harness.Processor.Behaviour = (_, _) => throw new InvalidOperationException("boom");

        var id = await harness.Queue.EnqueueAsync(new TestPayload("a", 1), ct: ct);
        await harness.RunAsync(ct);

        var job = await harness.GetAsync(id);
        Assert.Equal(JobStatus.Pending, job.Status);
        Assert.Equal(1, job.RetryCount);
        Assert.Equal(harness.Time.GetUtcNow().AddSeconds(2), job.AvailableAt);
        Assert.Equal("boom", job.LastError);
    }

    [Fact]
    public async Task Second_failure_with_one_retry_is_dead_letter_and_retry_brings_it_back()
    {
        var ct = TestContext.Current.CancellationToken;
        using var harness = new JobQueueHarness(o => o.MaxRetries = 1);
        harness.Processor.Behaviour = (_, _) => throw new InvalidOperationException("boom");

        var id = await harness.Queue.EnqueueAsync(new TestPayload("a", 1), ct: ct);
        await harness.RunAsync(ct);
        harness.Time.Advance(TimeSpan.FromSeconds(2));
        await harness.RunAsync(ct);

        var job = await harness.GetAsync(id);
        Assert.Equal(JobStatus.DeadLetter, job.Status);
        Assert.Equal(2, job.RetryCount);

        Assert.True(await harness.Queue.RetryAsync(id, ct));
        job = await harness.GetAsync(id);
        Assert.Equal(JobStatus.Pending, job.Status);
        Assert.Equal(0, job.RetryCount);
    }

    [Fact]
    public async Task Job_is_not_claimed_before_its_available_time()
    {
        var ct = TestContext.Current.CancellationToken;
        using var harness = new JobQueueHarness();
        var attempts = 0;
        harness.Processor.Behaviour = (_, _) => ++attempts == 1 ? throw new InvalidOperationException("first") : Task.CompletedTask;

        var id = await harness.Queue.EnqueueAsync(new TestPayload("a", 1), ct: ct);
        await harness.RunAsync(ct);
        harness.Time.Advance(TimeSpan.FromSeconds(1));
        await harness.RunAsync(ct);
        Assert.Equal(1, attempts);
        Assert.Equal(JobStatus.Pending, (await harness.GetAsync(id)).Status);

        harness.Time.Advance(TimeSpan.FromSeconds(1));
        await harness.RunAsync(ct);
        Assert.Equal(2, attempts);
        Assert.Equal(JobStatus.Completed, (await harness.GetAsync(id)).Status);
    }

    [Fact]
    public async Task Cancelling_a_pending_job_means_it_never_runs()
    {
        var ct = TestContext.Current.CancellationToken;
        using var harness = new JobQueueHarness();

        var id = await harness.Queue.EnqueueAsync(new TestPayload("a", 1), ct: ct);
        Assert.True(await harness.Queue.CancelAsync(id, ct));
        await harness.RunAsync(ct);

        Assert.Empty(harness.Processor.Received);
        Assert.Equal(JobStatus.Cancelled, (await harness.GetAsync(id)).Status);
    }

    [Fact]
    public async Task Cancelling_a_running_job_records_cancelled()
    {
        var ct = TestContext.Current.CancellationToken;
        using var harness = new JobQueueHarness();
        var never = new TaskCompletionSource();
        harness.Processor.Behaviour = (_, token) => never.Task.WaitAsync(token);

        var id = await harness.Queue.EnqueueAsync(new TestPayload("a", 1), ct: ct);
        await harness.Worker.RunOnceAsync(ct);
        await harness.Processor.Started.Task.WaitAsync(WaitLimit, ct);
        Assert.True(await harness.Queue.CancelAsync(id, ct));
        await harness.Worker.WhenIdleAsync().WaitAsync(WaitLimit, ct);

        Assert.Equal(JobStatus.Cancelled, (await harness.GetAsync(id)).Status);
    }

    [Fact]
    public async Task Throwing_progress_subscriber_does_not_stop_the_next_one()
    {
        var ct = TestContext.Current.CancellationToken;
        using var harness = new JobQueueHarness();
        JobProgress? seen = null;
        harness.Queue.ProgressChanged += _ => throw new InvalidOperationException("subscriber");
        harness.Queue.ProgressChanged += progress => seen = progress;
        harness.Processor.Behaviour = (job, token) => job.ReportProgressAsync(10, "working", token).AsTask();

        var id = await harness.Queue.EnqueueAsync(new TestPayload("a", 1), ct: ct);
        await harness.RunAsync(ct);

        Assert.NotNull(seen);
        Assert.Equal(id, seen.Id);
        Assert.Equal(10, seen.Percent);
        Assert.Equal(JobStatus.Completed, (await harness.GetAsync(id)).Status);
    }

    [Fact]
    public async Task Pause_stops_claiming_and_resume_claims_again()
    {
        var ct = TestContext.Current.CancellationToken;
        using var harness = new JobQueueHarness();
        var pausedValues = new List<bool>();
        harness.Queue.PausedChanged += pausedValues.Add;

        harness.Queue.Pause();
        var id = await harness.Queue.EnqueueAsync(new TestPayload("a", 1), ct: ct);
        await harness.RunAsync(ct);
        Assert.Equal(JobStatus.Pending, (await harness.GetAsync(id)).Status);

        harness.Queue.Resume();
        await harness.RunAsync(ct);
        Assert.Equal(JobStatus.Completed, (await harness.GetAsync(id)).Status);
        Assert.Equal([true, false], pausedValues);
    }

    [Fact]
    public async Task Clear_finished_keeps_pending_and_running_jobs()
    {
        var ct = TestContext.Current.CancellationToken;
        using var harness = new JobQueueHarness();
        foreach (var status in Enum.GetValues<JobStatus>())
        {
            await harness.SeedAsync(status);
        }

        Assert.Equal(new JobStatistics(1, 1, 1, 1, 1), await harness.Queue.GetStatisticsAsync(ct));
        Assert.Equal(3, await harness.Queue.ClearFinishedAsync(ct));

        var left = await harness.Queue.ListAsync(null, ct);
        Assert.Equal([JobStatus.Pending, JobStatus.Running], left.Select(j => j.Status).Order());
    }

    [Fact]
    public async Task Started_worker_picks_up_an_enqueued_job()
    {
        var ct = TestContext.Current.CancellationToken;
        using var harness = new JobQueueHarness();
        var completed = new TaskCompletionSource<JobRecord>(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Queue.StatusChanged += job =>
        {
            if (job.Status == JobStatus.Completed)
            {
                completed.TrySetResult(job);
            }
        };

        await harness.Worker.StartAsync(ct);
        var id = await harness.Queue.EnqueueAsync(new TestPayload("a", 1), ct: ct);
        var job = await completed.Task.WaitAsync(WaitLimit, ct);
        await harness.Worker.StopAsync(ct);

        Assert.Equal(id, job.Id);
    }
}
