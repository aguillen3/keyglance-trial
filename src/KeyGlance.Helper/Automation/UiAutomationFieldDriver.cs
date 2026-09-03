using System.Runtime.InteropServices;
using System.Windows.Automation;
using KeyGlance.Helper.Services;

namespace KeyGlance.Helper.Automation;

public sealed class UiAutomationFieldDriver : IFieldDriver
{
    public bool TryGetField(TargetWindow window, string automationId, out IFieldControl? control)
    {
        control = null;

        try
        {
            var root = AutomationElement.FromHandle(window.Handle);
            var condition = new PropertyCondition(
                AutomationElement.AutomationIdProperty,
                automationId);

            var element = root.FindFirst(TreeScope.Descendants, condition);
            if (element is null)
                return false;

            control = new UiAutomationFieldControl(element, automationId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsFocused(IFieldControl control)
    {
        if (control is not UiAutomationFieldControl ui)
            return false;

        try
        {
            var focused = AutomationElement.FocusedElement;
            return focused.GetRuntimeId().SequenceEqual(ui.Element.GetRuntimeId());
        }
        catch
        {
            return false;
        }
    }

    public bool SelectAll(IFieldControl control)
    {
        if (control is not UiAutomationFieldControl ui)
            return false;

        try
        {
            ui.Element.SetFocus();
            SendVirtualKey(VK_CONTROL, true);
            SendVirtualKey(VK_A, true);
            SendVirtualKey(VK_A, false);
            SendVirtualKey(VK_CONTROL, false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TypeCharacter(IFieldControl control, char character)
    {
        if (control is not UiAutomationFieldControl ui)
            return false;

        try
        {
            ui.Element.SetFocus();
            var inputs = new[]
            {
                MakeUnicodeInput(character, keyUp: false),
                MakeUnicodeInput(character, keyUp: true)
            };

            return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>()) == inputs.Length;
        }
        catch
        {
            return false;
        }
    }

    public string ReadValue(IFieldControl control)
    {
        if (control is not UiAutomationFieldControl ui)
            return string.Empty;

        try
        {
            if (ui.Element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
                return ((ValuePattern)pattern).Current.Value ?? string.Empty;

            return ui.Element.Current.Name ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void SendVirtualKey(ushort key, bool down)
    {
        var input = new[]
        {
            new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = key,
                        wScan = 0,
                        dwFlags = down ? 0u : KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = nint.Zero
                    }
                }
            }
        };

        _ = SendInput(1, input, Marshal.SizeOf<INPUT>());
    }

    private static INPUT MakeUnicodeInput(char character, bool keyUp)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = character,
                    dwFlags = KEYEVENTF_UNICODE | (keyUp ? KEYEVENTF_KEYUP : 0u),
                    time = 0,
                    dwExtraInfo = nint.Zero
                }
            }
        };
    }

    private sealed class UiAutomationFieldControl(AutomationElement element, string automationId) : IFieldControl
    {
        public AutomationElement Element { get; } = element;
        public string AutomationId { get; } = automationId;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_A = 0x41;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}
