using KeyGlance.Helper.Automation;
using KeyGlance.Helper.Models;

namespace KeyGlance.Helper.Services;

public sealed class ImportJobRunner
{
    private static readonly HashSet<string> AllowedFields =
        new(StringComparer.Ordinal) { "RecipientName", "Box2", "Box14", "Box22" };

    private readonly ITargetWindowFinder _windowFinder;
    private readonly IForegroundWindow _foreground;
    private readonly IFieldDriver _fields;
    private readonly JobExecutionGuard _guard;

    public ImportJobRunner(
        ITargetWindowFinder windowFinder,
        IForegroundWindow foreground,
        IFieldDriver fields,
        JobExecutionGuard guard)
    {
        _windowFinder = windowFinder;
        _foreground = foreground;
        _fields = fields;
        _guard = guard;
    }

    public ImportResult Execute(ImportJob job)
    {
        if (!_guard.TryStart(job.Id))
            return new(ImportStatus.Stopped, [], "Duplicate job execution blocked.");

        if (job.Fields.Count == 0)
            return new(ImportStatus.Stopped, [], "Job contains no fields.");

        foreach (var field in job.Fields.Keys)
        {
            if (!AllowedFields.Contains(field))
                return new(ImportStatus.Stopped, [], $"Unsupported field '{field}'.");
        }

        var matches = _windowFinder.FindExact(job.Client, job.Year);

        if (matches.Count == 0)
            return new(
                ImportStatus.Stopped,
                [],
                $"Exact window not found: MockTax - {job.Client} {job.Year}");

        if (matches.Count > 1)
            return new(
                ImportStatus.Stopped,
                [],
                $"Multiple exact windows found for {job.Client} {job.Year}; refusing to guess.");

        var target = matches[0];

        if (!_foreground.IsForeground(target.Handle))
            return new(
                ImportStatus.Stopped,
                [],
                "Target MockTax window is not foreground.");

        var landed = new List<string>();

        foreach (var (fieldName, expectedValue) in job.Fields)
        {
            if (!_foreground.IsForeground(target.Handle))
            {
                return new(
                    ImportStatus.Stopped,
                    landed,
                    $"Foreground window changed. Landed fields: {string.Join(", ", landed)}.");
            }

            if (!_fields.TryGetField(target, fieldName, out var control) || control is null)
            {
                return new(
                    ImportStatus.Partial,
                    landed,
                    $"Field '{fieldName}' was not found.");
            }

            if (!controlFocus(control))
            {
                return new(
                    ImportStatus.Stopped,
                    landed,
                    $"Could not focus field '{fieldName}'.");
            }

            if (!_foreground.IsForeground(target.Handle))
            {
                return new(
                    ImportStatus.Stopped,
                    landed,
                    $"Foreground window changed before typing '{fieldName}'.");
            }

            if (!_fields.SelectAll(control))
            {
                return new(
                    ImportStatus.Stopped,
                    landed,
                    $"Could not prepare field '{fieldName}' for input.");
            }

            foreach (var character in expectedValue)
            {
                if (!_foreground.IsForeground(target.Handle))
                {
                    return new(
                        ImportStatus.Stopped,
                        landed,
                        $"Foreground window changed while typing '{fieldName}'. Landed fields: {string.Join(", ", landed)}.");
                }

                if (!controlFocus(control))
                {
                    return new(
                        ImportStatus.Stopped,
                        landed,
                        $"Field focus changed while typing '{fieldName}'.");
                }

                if (!_fields.TypeCharacter(control, character))
                {
                    return new(
                        ImportStatus.Stopped,
                        landed,
                        $"Input failed while typing '{fieldName}'.");
                }
            }

            if (!_foreground.IsForeground(target.Handle))
            {
                return new(
                    ImportStatus.Stopped,
                    landed,
                    $"Foreground window changed after typing '{fieldName}'.");
            }

            var actual = _fields.ReadValue(control);
            if (!string.Equals(actual, expectedValue, StringComparison.Ordinal))
            {
                return new(
                    ImportStatus.Partial,
                    landed,
                    $"Field '{fieldName}' did not land. Expected '{expectedValue}', read back '{actual}'.");
            }

            landed.Add(fieldName);
        }

        return new(ImportStatus.Imported, landed, null);

        bool controlFocus(IFieldControl c)
        {
            try
            {
                return _fields.IsFocused(c);
            }
            catch
            {
                return false;
            }
        }
    }
}
