using System.Windows;

namespace MockTax;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var args = e.Args;
        var client = args.Length > 0 ? args[0] : "Demo Client";
        var year = args.Length > 1 && int.TryParse(args[1], out var parsedYear) ? parsedYear : 2025;

        var readonlyField = args
            .Skip(2)
            .Select((value, index) => value == "--readonly" && index + 3 < args.Length ? args[index + 3] : null)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (args.Contains("--readonly", StringComparer.OrdinalIgnoreCase))
        {
            var index = Array.FindIndex(args, a => string.Equals(a, "--readonly", StringComparison.OrdinalIgnoreCase));
            if (index >= 0 && index + 1 < args.Length)
                readonlyField = args[index + 1];
        }

        var window = new MainWindow(client, year, readonlyField);
        window.Show();
    }
}
