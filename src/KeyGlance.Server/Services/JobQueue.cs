using KeyGlance.Server.Models;

namespace KeyGlance.Server.Services;

public sealed class JobQueue
{
    private readonly object _gate = new();
    private readonly Dictionary<string, ImportJob> _jobs = new(StringComparer.Ordinal);

    public void Add(ImportJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        lock (_gate)
        {
            if (_jobs.ContainsKey(job.Id))
                throw new InvalidOperationException($"Job '{job.Id}' already exists.");

            _jobs.Add(job.Id, job);
        }
    }

    public ImportJob? Claim()
    {
        lock (_gate)
        {
            var job = _jobs.Values
                .Where(j => !j.Claimed)
                .OrderBy(j => j.DueDate)
                .ThenBy(j => j.Id, StringComparer.Ordinal)
                .FirstOrDefault();

            if (job is null)
                return null;

            job.Claimed = true;
            return job;
        }
    }

    public bool RecordResult(string id, JobResult result)
    {
        lock (_gate)
        {
            if (!_jobs.TryGetValue(id, out var job))
                return false;

            job.Result = result;
            return true;
        }
    }

    public IReadOnlyList<ImportJob> Snapshot()
    {
        lock (_gate)
        {
            return _jobs.Values
                .OrderBy(j => j.DueDate)
                .ThenBy(j => j.Id, StringComparer.Ordinal)
                .ToList();
        }
    }
}
