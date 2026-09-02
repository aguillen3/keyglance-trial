using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Automation;

record Job(string id, string client, int year, string dueDate, Dictionary<string,string> fields, string claimToken);
record ResultPayload(string outcome, string[] landedFields, string? reason);

internal static class Program
{
    static readonly HttpClient Http = new();
    static readonly HashSet<string> Processed = new(StringComparer.OrdinalIgnoreCase);
    static string BaseUrl = "http://localhost:3000";
    static int PollMs = 1000;

    static async Task Main(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith("--server=")) BaseUrl = arg["--server=".Length..].TrimEnd('/');
            if (arg.StartsWith("--poll-ms=") && int.TryParse(arg["--poll-ms=".Length..], out var ms)) PollMs = ms;
        }

        Console.WriteLine($"KeyGlance Helper polling {BaseUrl}/claim");
        while (true)
        {
            try
            {
                using var response = await Http.GetAsync(BaseUrl + "/claim");
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    await Task.Delay(PollMs);
                    continue;
                }
                response.EnsureSuccessStatusCode();
                var job = await response.Content.ReadFromJsonAsync<Job>();
                if (job is null) continue;

                if (Processed.Contains(job.id))
                {
                    Console.WriteLine($"SKIP duplicate job {job.id}");
                    await Report(job, "stopped", [], "Duplicate job ID was already processed by this helper.");
                    continue;
                }

                Processed.Add(job.id); // mark before touching the desktop
                await RunJob(job);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Poll error: " + ex.Message);
                await Task.Delay(1000);
            }
        }
    }

    static async Task RunJob(Job job)
    {
        IntPtr hwnd = FindExactWindow($"MockTax - {job.client} {job.year}");
        if (hwnd == IntPtr.Zero)
        {
            await Report(job, "stopped", [], $"Exact MockTax window not found: MockTax - {job.client} {job.year}");
            return;
        }

        // If multiple exact windows exist, do not guess.
        if (FindExactWindowCount($"MockTax - {job.client} {job.year}") != 1)
        {
            await Report(job, "stopped", [], "More than one exact matching MockTax window is open; refusing to guess.");
            return;
        }

        var landed = new List<string>();
        foreach (var pair in job.fields)
        {
            if (!IsForeground(hwnd))
            {
                await Report(job, "stopped", landed.ToArray(), $"Foreground window changed before {pair.Key}; stopped after: {string.Join(", ", landed)}");
                return;
            }

            AutomationElement? field = FindField(hwnd, pair.Key);
            if (field is null)
            {
                await Report(job, "stopped", landed.ToArray(), $"Field not found: {pair.Key}");
                return;
            }

            if (!IsForeground(hwnd))
            {
                await Report(job, "stopped", landed.ToArray(), $"Foreground window changed before typing {pair.Key}; stopped after: {string.Join(", ", landed)}");
                return;
            }

            field.SetFocus();
            if (!IsForeground(hwnd))
            {
                await Report(job, "stopped", landed.ToArray(), $"Foreground window changed after focusing {pair.Key}; stopped after: {string.Join(", ", landed)}");
                return;
            }

            // Clear and type one character at a time so a foreground change is
            // detected before every additional character is sent.
            SendKeys.SendWait("^a");
            foreach (char ch in pair.Value)
            {
                if (!IsForeground(hwnd))
                {
                    await Report(job, "stopped", landed.ToArray(), $"Foreground window changed while typing {pair.Key}; stopped after: {string.Join(", ", landed)}");
                    return;
                }
                SendKeys.SendWait(ch.ToString());
            }

            if (!IsForeground(hwnd))
            {
                await Report(job, "stopped", landed.ToArray(), $"Foreground window changed after typing {pair.Key}; stopped after: {string.Join(", ", landed)}");
                return;
            }

            string actual = ReadValue(field);
            if (!string.Equals(actual, pair.Value, StringComparison.Ordinal))
            {
                await Report(job, "partial", landed.ToArray(), $"{pair.Key} did not land. Expected '{pair.Value}', read back '{actual}'.");
                return;
            }

            landed.Add(pair.Key);
            Console.WriteLine($"OK {job.id}: {pair.Key}={actual}");
        }

        await Report(job, "imported", landed.ToArray(), null);
    }

    static AutomationElement? FindField(IntPtr hwnd, string fieldName)
    {
        var root = AutomationElement.FromHandle(hwnd);
        // Name is exact and AutomationId is checked when available.
        var condition = new PropertyCondition(AutomationElement.NameProperty, fieldName);
        var matches = root.FindAll(TreeScope.Descendants, condition);
        foreach (AutomationElement e in matches)
        {
            if (e.Current.ControlType == ControlType.Edit)
                return e;
        }
        return null;
    }

    static string ReadValue(AutomationElement field)
    {
        if (field.TryGetCurrentPattern(ValuePattern.Pattern, out object pattern))
            return ((ValuePattern)pattern).Current.Value;
        return field.Current.Name ?? "";
    }

    static async Task Report(Job job, string outcome, string[] landed, string? reason)
    {
        var payload = new ResultPayload(outcome, landed, reason);
        using var response = await Http.PostAsJsonAsync($"{BaseUrl}/jobs/{Uri.EscapeDataString(job.id)}/result", payload);
        response.EnsureSuccessStatusCode();
        Console.WriteLine($"RESULT {job.id}: {outcome}" + (reason is null ? "" : $" - {reason}"));
    }

    static bool IsForeground(IntPtr hwnd) => GetForegroundWindow() == hwnd;

    static int FindExactWindowCount(string title)
    {
        int count = 0;
        EnumWindows((h, _) => {
            if (!IsWindowVisible(h)) return true;
            var text = GetTitle(h);
            if (text == title) count++;
            return true;
        }, IntPtr.Zero);
        return count;
    }

    static IntPtr FindExactWindow(string title)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((h, _) => {
            if (!IsWindowVisible(h)) return true;
            if (GetTitle(h) == title) { found = h; return false; }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    static string GetTitle(IntPtr hwnd)
    {
        var sb = new StringBuilder(512);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
}
