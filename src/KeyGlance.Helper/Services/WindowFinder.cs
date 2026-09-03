using System.Runtime.InteropServices;
using System.Text;

namespace KeyGlance.Helper.Services;

public sealed record TargetWindow(nint Handle, string Title);

public interface ITargetWindowFinder
{
    IReadOnlyList<TargetWindow> FindExact(string client, int year);
}

public sealed class WindowsTargetWindowFinder : ITargetWindowFinder
{
    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    public IReadOnlyList<TargetWindow> FindExact(string client, int year)
    {
        var expected = $"MockTax - {client} {year}";
        var matches = new List<TargetWindow>();

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            var title = GetTitle(hWnd);
            if (string.Equals(title, expected, StringComparison.Ordinal))
                matches.Add(new TargetWindow(hWnd, title));

            return true;
        }, nint.Zero);

        return matches;
    }

    private static string GetTitle(nint hWnd)
    {
        var buffer = new StringBuilder(512);
        _ = GetWindowText(hWnd, buffer, buffer.Capacity);
        return buffer.ToString();
    }
}
