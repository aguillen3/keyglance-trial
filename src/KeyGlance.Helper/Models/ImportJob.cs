namespace KeyGlance.Helper.Models;

public sealed class ImportJob
{
    public required string Id { get; init; }
    public required string Client { get; init; }
    public required int Year { get; init; }
    public required DateTime DueDate { get; init; }
    public required Dictionary<string, string> Fields { get; init; }
}
