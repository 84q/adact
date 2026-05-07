using System.ComponentModel;
using System.Windows;

namespace SampleApp;

public partial class MainWindow : Window
{
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
