namespace KeyGlance.Helper.Automation;

using KeyGlance.Helper.Services;

public interface IFieldDriver
{
    bool TryGetField(TargetWindow window, string automationId, out IFieldControl? control);
    bool IsFocused(IFieldControl control);
    bool SelectAll(IFieldControl control);
    bool TypeCharacter(IFieldControl control, char character);
    string ReadValue(IFieldControl control);
}

public interface IFieldControl
{
    string AutomationId { get; }
}
