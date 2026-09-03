namespace KeyGlance.Helper.Services;

public sealed class JobExecutionGuard
{
    private readonly object _gate = new();
    private readonly HashSet<string> _started = new(StringComparer.Ordinal);

    public bool TryStart(string jobId)
    {
        lock (_gate)
        {
            return _started.Add(jobId);
        }
    }
}
