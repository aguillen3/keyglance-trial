namespace KeyGlance.Server.Models;

public sealed class JobResult
{
    public required string Status { get; init; }
    public List<string> LandedFields { get; init; } = [];
    public string? Reason { get; init; }
    public DateTime RecordedAtUtc { get; init; } = DateTime.UtcNow;
}
