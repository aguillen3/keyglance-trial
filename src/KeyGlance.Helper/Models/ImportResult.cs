namespace KeyGlance.Helper.Models;

public enum ImportStatus
{
    Imported,
    Partial,
    Stopped
}

public sealed record ImportResult(
    ImportStatus Status,
    IReadOnlyList<string> LandedFields,
    string? Reason);
