namespace CheapHelpers.Jobs.Tests;

public sealed class JobProgressTests
{
    [Fact]
    public async Task Twenty_reports_in_one_second_write_the_first_and_the_final_one()
    {
        var ct = TestContext.Current.CancellationToken;
        using var harness = new JobQueueHarness();
        var id = await harness.Queue.EnqueueAsync(new TestPayload("a", 1), ct: ct);
        var writes = 0;
        var job = CreateContext(harness, id, (progress, token) =>
        {
            writes++;
            return harness.Queue.WriteProgressAsync(progress, token);
        });

        for (var report = 0; report < 20; report++)
        {
            if (report > 0)
            {
                harness.Time.Advance(TimeSpan.FromMilliseconds(50));
            }

            await job.ReportProgressAsync(report == 19 ? 100 : report * 5, cancellationToken: ct);
        }

        Assert.Equal(2, writes);
        Assert.Equal(100, (await harness.GetAsync(id)).ProgressPercent);
    }

    [Fact]
    public async Task Half_done_after_ten_seconds_estimates_ten_seconds_left()
    {
        var ct = TestContext.Current.CancellationToken;
        using var harness = new JobQueueHarness();
        var id = await harness.Queue.EnqueueAsync(new TestPayload("a", 1), ct: ct);
        var job = CreateContext(harness, id, harness.Queue.WriteProgressAsync);

        await job.ReportProgressAsync(0, cancellationToken: ct);
        harness.Time.Advance(TimeSpan.FromSeconds(10));
        await job.ReportProgressAsync(50, "halfway", ct);

        var record = await harness.GetAsync(id);
        Assert.Equal(50, record.ProgressPercent);
        Assert.Equal("halfway", record.ProgressMessage);
        Assert.Equal(TimeSpan.FromSeconds(10), record.EstimatedRemaining);
    }

    private static JobContext<TestPayload> CreateContext(JobQueueHarness harness, Guid id, Func<JobProgress, CancellationToken, Task> write) =>
        new(id, "", new TestPayload("a", 1), 1, new JobQueueOptions(), harness.Time, write, harness.Queue.RaiseProgress);
}
