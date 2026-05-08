using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SampleApp.Tabs;

/// <summary>
/// Demonstrates asynchronous work, cancellation, and progress updates.
/// </summary>
public partial class AsyncDelayTab : UserControl, IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDelayTab"/> class.
    /// </summary>
    public AsyncDelayTab()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    /// <summary>
    /// Releases the resources used by the tab.
    /// </summary>
    public void Dispose()
    {
        Unloaded -= OnUnloaded;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        GC.SuppressFinalize(this);
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        StartButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        StatusLabel.Content = "Running...";
        TaskProgressBar.IsIndeterminate = false;
        TaskProgressBar.Value = 0;
        AsyncResultsListBox.Items.Clear();
        AsyncResultsDataGrid.Visibility = Visibility.Collapsed;

        try
        {
            for (int i = 1; i <= 10; i++)
            {
                await Task.Delay(500, _cancellationTokenSource.Token);
                TaskProgressBar.Value = i * 10;
                StatusLabel.Content = $"Running... {TaskProgressBar.Value:0}%";
            }

            StatusLabel.Content = "Completed";
            AsyncResultsListBox.Items.Add("Async result A");
            AsyncResultsListBox.Items.Add("Async result B");
            AsyncResultsListBox.Items.Add("Async result C");
            AsyncResultsDataGrid.ItemsSource = new List<AsyncResult>
            {
                new("A", "Completed"),
                new("B", "Completed")
            };
            AsyncResultsDataGrid.Visibility = Visibility.Visible;
        }
        catch (TaskCanceledException)
        {
            StatusLabel.Content = "Cancelled";
        }
        finally
        {
            CancelButton.IsEnabled = false;
            StartButton.IsEnabled = true;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Dispose();
    }

    /// <summary>
    /// Represents a single row in the asynchronous results grid.
    /// </summary>
    private sealed record AsyncResult(string Name, string Status);
}
