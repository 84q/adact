using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SampleApp.Tabs;

public partial class MultiWindowTab : UserControl
{
    public MultiWindowTab()
    {
        InitializeComponent();
    }

    private void OpenModal_Click(object sender, RoutedEventArgs e)
    {
        var window = new Window
        {
            Title = "ADACT SampleApp - Modal Window",
            Width = 400,
            Height = 300,
            Content = new TextBlock { Text = "This is a modal window.", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };
        window.ShowDialog();
    }

    private void OpenModeless_Click(object sender, RoutedEventArgs e)
    {
        var window = new Window
        {
            Title = "ADACT SampleApp - Modeless Window",
            Width = 400,
            Height = 300,
            Content = new TextBlock { Text = "This is a modeless window.", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };
        window.Show();
    }

    private void LaunchCalculator_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("calc.exe") { UseShellExecute = true });
    }
}
