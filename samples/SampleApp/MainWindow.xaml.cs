using System.ComponentModel;
using System.Windows;

namespace SampleApp;

/// <summary>
/// Hosts the sample tabs and blocks window closing when requested.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (BlockCloseMenuItem.IsChecked)
        {
            e.Cancel = true;
        }
    }
}
