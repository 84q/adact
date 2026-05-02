using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SampleApp.Tabs;

public partial class AsyncDelayTab : UserControl
{
    public AsyncDelayTab()
    {
        InitializeComponent();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StartButton.IsEnabled = false;
        StatusLabel.Content = "Running...";
        TaskProgressBar.IsIndeterminate = true;

        await Task.Run(() =>
        {
            System.Threading.Thread.Sleep(30000);
        });

        TaskProgressBar.IsIndeterminate = false;
        StatusLabel.Content = "Completed";
        StartButton.IsEnabled = true;
    }
}
