using KeyGlance.Server.Models;
using KeyGlance.Server.Services;

namespace KeyGlance.Server.Tests;

public sealed class JobQueueTests
{
    [Fact]
    public void Claim_ReturnsSoonestDueDate()
    {
        var queue = new JobQueue();

        queue.Add(Job("late", DateTime.UtcNow.AddHours(2)));
        queue.Add(Job("soon", DateTime.UtcNow.AddMinutes(1)));

        Assert.Equal("soon", queue.Claim()!.Id);
        Assert.Equal("late", queue.Claim()!.Id);
        Assert.Null(queue.Claim());
    }

    [Fact]
    public async Task Claim_IsExecuteOnceUnderConcurrency()
    {
        var queue = new JobQueue();
        queue.Add(Job("only", DateTime.UtcNow));

        var results = await Task.WhenAll(
            Enumerable.Range(0, 100)
                .Select(_ => Task.Run(queue.Claim)));

        Assert.Single(results.Where(x => x is not null));
        Assert.Equal("only", results.Single(x => x is not null)!.Id);
    }

    private static ImportJob Job(string id, DateTime dueDate) => new()
    {
        Id = id,
        Client = "Test Client",
        Year = 2025,
        DueDate = dueDate,
        Fields = new Dictionary<string, string> { ["Box2"] = "10" }
    };
}
