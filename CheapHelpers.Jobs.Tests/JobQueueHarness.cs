using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace CheapHelpers.Jobs.Tests;

/// <summary>A queue and worker over an in-memory SQLite database, a fake clock and a recording processor.</summary>
public sealed class JobQueueHarness : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public JobQueueHarness(Action<JobQueueOptions>? configure = null)
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Factory = new TestContextFactory(_connection);
        using (var db = Factory.CreateDbContext())
        {
            db.Database.EnsureCreated();
        }

        var options = new JobQueueOptions();
        configure?.Invoke(options);
        var wrapped = Microsoft.Extensions.Options.Options.Create(options);

        _services = new ServiceCollection().AddSingleton<IJobProcessor<TestPayload>>(Processor).BuildServiceProvider();
        Queue = new JobQueue<TestPayload, TestJobDbContext>(Factory, wrapped, Time, NullLogger<JobQueue<TestPayload, TestJobDbContext>>.Instance);
        Worker = new JobWorker<TestPayload, TestJobDbContext>(Queue, _services.GetRequiredService<IServiceScopeFactory>(), wrapped, Time,
            NullLogger<JobWorker<TestPayload, TestJobDbContext>>.Instance);
    }

    public FakeTimeProvider Time { get; } = new();

    public RecordingProcessor Processor { get; } = new();

    public TestContextFactory Factory { get; }

    public JobQueue<TestPayload, TestJobDbContext> Queue { get; }

    public JobWorker<TestPayload, TestJobDbContext> Worker { get; }

    /// <summary>Claims what it can and waits until those jobs finish.</summary>
    public async Task RunAsync(CancellationToken ct)
    {
        await Worker.RunOnceAsync(ct);
        await Worker.WhenIdleAsync();
    }

    public async Task<JobRecord> GetAsync(Guid id) => (await Queue.GetAsync(id, TestContext.Current.CancellationToken))!;

    /// <summary>Inserts a row directly, bypassing the queue.</summary>
    public async Task<Guid> SeedAsync(JobStatus status, string? machineName = null, int? processId = null)
    {
        var now = Time.GetUtcNow();
        var job = new JobRecord
        {
            Id = Guid.NewGuid(),
            PayloadJson = """{"Name":"seed","Count":0}""",
            Status = status,
            MaxRetries = 3,
            CreatedAt = now,
            AvailableAt = now,
            LastUpdatedAt = now,
            MachineName = machineName,
            ProcessId = processId,
        };

        await using var db = Factory.CreateDbContext();
        db.Add(job);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return job.Id;
    }

    public void Dispose()
    {
        Worker.Dispose();
        _services.Dispose();
        _connection.Dispose();
    }
}
