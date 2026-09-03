using System.Runtime.InteropServices;

namespace KeyGlance.Helper.Services;

public interface IForegroundWindow
{
    nint GetForegroundWindow();
    bool IsForeground(nint expectedHandle);
}

public sealed class WindowsForegroundWindow : IForegroundWindow
{
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    public nint GetForegroundWindow() => GetForegroundWindow();

    public bool IsForeground(nint expectedHandle) =>
        expectedHandle != nint.Zero && GetForegroundWindow() == expectedHandle;
}
