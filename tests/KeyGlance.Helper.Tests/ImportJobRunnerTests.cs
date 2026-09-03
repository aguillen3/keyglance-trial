using KeyGlance.Helper.Automation;
using KeyGlance.Helper.Models;
using KeyGlance.Helper.Services;

namespace KeyGlance.Helper.Tests;

public sealed class ImportJobRunnerTests
{
    [Fact]
    public void ExactWindowMismatch_StopsWithoutTyping()
    {
        var fields = new FakeFieldDriver();
        var runner = new ImportJobRunner(
            new FakeWindowFinder([]),
            new FakeForegroundWindow(true),
            fields,
            new JobExecutionGuard());

        var result = runner.Execute(Job("j1", "Margaret Buttle", 2025));

        Assert.Equal(ImportStatus.Stopped, result.Status);
        Assert.Empty(fields.Typed);
    }

    [Fact]
    public void Box2AndBox22_AreDistinct()
    {
        var fields = new FakeFieldDriver();
        fields.Add("Box2");
        fields.Add("Box22");

        var runner = new ImportJobRunner(
            new FakeWindowFinder([new TargetWindow((nint)123, "MockTax - Margaret Buttle 2025")]),
            new FakeForegroundWindow(true),
            fields,
            new JobExecutionGuard());

        var result = runner.Execute(new ImportJob
        {
            Id = "j2",
            Client = "Margaret Buttle",
            Year = 2025,
            DueDate = DateTime.UtcNow,
            Fields = new Dictionary<string, string>
            {
                ["Box2"] = "111",
                ["Box22"] = "999"
            }
        });

        Assert.Equal(ImportStatus.Imported, result.Status);
        Assert.Equal("111", fields.Values["Box2"]);
        Assert.Equal("999", fields.Values["Box22"]);
    }

    [Fact]
    public void SameJobCannotExecuteTwice()
    {
        var fields = new FakeFieldDriver();
        fields.Add("Box2");

        var runner = new ImportJobRunner(
            new FakeWindowFinder([new TargetWindow((nint)123, "MockTax - Margaret Buttle 2025")]),
            new FakeForegroundWindow(true),
            fields,
            new JobExecutionGuard());

        var job = Job("duplicate", "Margaret Buttle", 2025);

        var first = runner.Execute(job);
        var second = runner.Execute(job);

        Assert.Equal(ImportStatus.Imported, first.Status);
        Assert.Equal(ImportStatus.Stopped, second.Status);
        Assert.Equal(1, fields.TypeCount);
    }

    private static ImportJob Job(string id, string client, int year) => new()
    {
        Id = id,
        Client = client,
        Year = year,
        DueDate = DateTime.UtcNow,
        Fields = new Dictionary<string, string> { ["Box2"] = "123" }
    };

    private sealed class FakeWindowFinder(IReadOnlyList<TargetWindow> matches) : ITargetWindowFinder
    {
        public IReadOnlyList<TargetWindow> FindExact(string client, int year) => matches;
    }

    private sealed class FakeForegroundWindow(bool foreground) : IForegroundWindow
    {
        public nint GetForegroundWindow() => foreground ? (nint)123 : nint.Zero;
        public bool IsForeground(nint expectedHandle) => foreground;
    }

    private sealed class FakeFieldDriver : IFieldDriver
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);
        public List<string> Typed { get; } = [];
        public int TypeCount { get; private set; }

        public void Add(string id) => Values[id] = string.Empty;

        public bool TryGetField(TargetWindow window, string automationId, out IFieldControl? control)
        {
            if (!Values.ContainsKey(automationId))
            {
                control = null;
                return false;
            }

            control = new FakeField(automationId);
            return true;
        }

        public bool IsFocused(IFieldControl control) => true;

        public bool SelectAll(IFieldControl control)
        {
            Values[control.AutomationId] = string.Empty;
            return true;
        }

        public bool TypeCharacter(IFieldControl control, char character)
        {
            Values[control.AutomationId] += character;
            Typed.Add(control.AutomationId);
            TypeCount++;
            return true;
        }

        public string ReadValue(IFieldControl control) => Values[control.AutomationId];

        private sealed record FakeField(string AutomationId) : IFieldControl;
    }
}
