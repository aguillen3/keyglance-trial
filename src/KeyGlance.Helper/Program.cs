using KeyGlance.Helper.Automation;
using KeyGlance.Helper.Services;

var serverUrl = GetOption(args, "--server") ?? "http://localhost:5080";
var once = args.Contains("--once", StringComparer.OrdinalIgnoreCase);
var intervalMs = int.TryParse(GetOption(args, "--interval-ms"), out var parsed) ? parsed : 1000;

using var http = new HttpClient { BaseAddress = new Uri(serverUrl.TrimEnd('/') + "/") };
var server = new ServerClient(http);
var runner = new ImportJobRunner(
    new WindowsTargetWindowFinder(),
    new WindowsForegroundWindow(),
    new UiAutomationFieldDriver(),
    new JobExecutionGuard());

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

Console.WriteLine($"KeyGlance Helper polling {serverUrl}");

do
{
    try
    {
        var job = await server.ClaimAsync(cancellation.Token);

        if (job is null)
        {
            if (once)
                break;

            await Task.Delay(intervalMs, cancellation.Token);
            continue;
        }

        Console.WriteLine($"Claimed job {job.Id} for {job.Client} {job.Year}.");

        var result = runner.Execute(job);

        Console.WriteLine(
            $"{job.Id}: {result.Status}; landed=[{string.Join(", ", result.LandedFields)}]; reason={result.Reason ?? "none"}");

        await server.ReportResultAsync(job.Id, result, cancellation.Token);

        if (once)
            break;
    }
    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
    {
        break;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Helper error: {ex.Message}");
        if (once)
            break;

        await Task.Delay(intervalMs, cancellation.Token);
    }
}
while (!cancellation.IsCancellationRequested);

static string? GetOption(string[] args, string name)
{
    var index = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
