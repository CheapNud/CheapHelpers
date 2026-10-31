using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Jobs.Tests;

public sealed class JobRegistrationTests
{
    [Fact]
    public void AddCheapJobs_registers_the_queue_worker_processor_and_options()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var services = new ServiceCollection()
            .AddLogging()
            .AddDbContextFactory<TestJobDbContext>(o => o.UseSqlite(connection))
            .AddCheapJobs<TestPayload, TestJobDbContext, RecordingProcessor>(o => o.MaxConcurrentJobs = 2);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Same(provider.GetRequiredService<JobQueue<TestPayload, TestJobDbContext>>(), provider.GetRequiredService<JobQueue<TestPayload, TestJobDbContext>>());
        Assert.IsType<JobWorker<TestPayload, TestJobDbContext>>(Assert.Single(provider.GetServices<IHostedService>()));
        Assert.IsType<RecordingProcessor>(scope.ServiceProvider.GetRequiredService<IJobProcessor<TestPayload>>());
        Assert.Equal(2, provider.GetRequiredService<IOptions<JobQueueOptions>>().Value.MaxConcurrentJobs);
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void AddCheapJobs_rejects_a_second_queue_on_the_same_context()
    {
        var services = new ServiceCollection().AddCheapJobs<TestPayload, TestJobDbContext, RecordingProcessor>();

        Assert.Throws<InvalidOperationException>(() => services.AddCheapJobs<string, TestJobDbContext, StringProcessor>());
    }

    private sealed class StringProcessor : IJobProcessor<string>
    {
        public Task ProcessAsync(JobContext<string> job, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
