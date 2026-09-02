using System.Text.RegularExpressions;

namespace MockTax;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        string? client = null;
        int? year = null;
        bool readonlyBox22 = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--client" when i + 1 < args.Length:
                    client = args[++i]; break;
                case "--year" when i + 1 < args.Length && int.TryParse(args[++i], out var y):
                    year = y; break;
                case "--readonly" when i + 1 < args.Length:
                    readonlyBox22 = args[++i].Equals("Box22", StringComparison.OrdinalIgnoreCase); break;
            }
        }

        if (string.IsNullOrWhiteSpace(client) || year is null)
        {
            MessageBox.Show("Usage: MockTax.exe --client \"Margaret Buttle\" --year 2025 [--readonly Box22]");
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(client, year.Value, readonlyBox22));
    }
}

public sealed class MainForm : Form
{
    public MainForm(string client, int year, bool readonlyBox22)
    {
        Text = $"MockTax - {client} {year}";
        Width = 430;
        Height = 300;
        StartPosition = FormStartPosition.CenterScreen;
        AccessibleName = Text;

        var panel = new TableLayoutPanel {
            Dock = DockStyle.Fill, Padding = new Padding(15),
            ColumnCount = 2, RowCount = 5
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddField(panel, "RecipientName", client, 0, readOnly: true);
        AddField(panel, "Box2", "", 1, false);
        AddField(panel, "Box14", "", 2, false);
        AddField(panel, "Box22", "", 3, readonlyBox22);

        var save = new Button { Text = "Save", Dock = DockStyle.Fill, AccessibleName = "Save" };
        save.Click += (_, _) => MessageBox.Show("Saved.", "MockTax");
        panel.Controls.Add(save, 1, 4);

        Controls.Add(panel);
    }

    static void AddField(TableLayoutPanel panel, string name, string value, int row, bool readOnly)
    {
        var label = new Label { Text = name, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        var box = new TextBox {
            Name = name, Text = value, Dock = DockStyle.Fill,
            ReadOnly = readOnly, AccessibleName = name, AccessibleDescription = name
        };
        box.AccessibleRole = AccessibleRole.Text;
        // UI Automation clients can reliably locate these by AutomationId/Name.
        box.Name = name;
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(box, 1, row);
    }
}
