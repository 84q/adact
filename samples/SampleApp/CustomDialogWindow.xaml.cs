using System.Windows;

namespace SampleApp;

/// <summary>
/// Provides the custom modal dialog used by the dialogs sample.
/// </summary>
public partial class CustomDialogWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomDialogWindow"/> class.
    /// </summary>
    public CustomDialogWindow()
    {
        InitializeComponent();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
