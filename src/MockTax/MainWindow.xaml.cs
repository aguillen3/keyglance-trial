using System.Windows;

namespace MockTax;

public partial class MainWindow : Window
{
    public MainWindow(string client, int year, string? readonlyField)
    {
        InitializeComponent();

        Title = $"MockTax - {client} {year}";
        RecipientNameBox.Text = client;

        if (string.Equals(readonlyField, "Box22", StringComparison.OrdinalIgnoreCase))
        {
            Box22Box.IsReadOnly = true;
        }
    }
}
