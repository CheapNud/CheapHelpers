namespace CheapHelpers.Jobs.Tests;

public sealed class JobRecoveryTests
{
    // Above the highest process id Windows and Linux hand out
    private const int MissingProcessId = int.MaxValue;

    [Fact]
    public async Task Job_left_running_by_a_dead_process_on_this_machine_goes_back_to_pending()
    {
        var ct = TestContext.Current.CancellationToken;
        using var harness = new JobQueueHarness();
        var orphan = await harness.SeedAsync(JobStatus.Running, Environment.MachineName, MissingProcessId);
        var reusedId = await harness.SeedAsync(JobStatus.Running, Environment.MachineName, Environment.ProcessId);
        var elsewhere = await harness.SeedAsync(JobStatus.Running, "another-machine", MissingProcessId);

        await harness.Queue.RecoverAsync(ct);

        var recovered = await harness.GetAsync(orphan);
        Assert.Equal(JobStatus.Pending, recovered.Status);
        Assert.Equal(1, recovered.RetryCount);

        Assert.Equal(JobStatus.Pending, (await harness.GetAsync(reusedId)).Status);

        var untouched = await harness.GetAsync(elsewhere);
        Assert.Equal(JobStatus.Running, untouched.Status);
        Assert.Equal(0, untouched.RetryCount);
    }
}
