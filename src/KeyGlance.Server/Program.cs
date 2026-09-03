using KeyGlance.Server.Models;
using KeyGlance.Server.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<JobQueue>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/claim", (JobQueue queue) =>
{
    var job = queue.Claim();
    return job is null ? Results.NoContent() : Results.Ok(job);
});

app.MapPost("/jobs", (ImportJob job, JobQueue queue) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(job.Id) ||
            string.IsNullOrWhiteSpace(job.Client) ||
            job.Year < 1900 ||
            job.Fields.Count == 0)
        {
            return Results.BadRequest("Job requires id, client, year and at least one field.");
        }

        queue.Add(job);
        return Results.Created($"/jobs/{job.Id}", job);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(ex.Message);
    }
});

app.MapGet("/jobs", (JobQueue queue) => Results.Ok(queue.Snapshot()));

app.MapPost("/jobs/{id}/result", (string id, JobResult result, JobQueue queue) =>
{
    var status = result.Status?.Trim().ToLowerInvariant();
    if (status is not ("imported" or "partial" or "stopped"))
        return Results.BadRequest("Status must be imported, partial, or stopped.");

    var normalized = new JobResult
    {
        Status = status,
        LandedFields = result.LandedFields ?? [],
        Reason = result.Reason
    };

    return queue.RecordResult(id, normalized)
        ? Results.Ok(normalized)
        : Results.NotFound();
});

app.Run();

public partial class Program { }
