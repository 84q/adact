using System.Windows;
using System.Windows.Controls;

namespace SampleApp.Tabs;

public partial class BasicControlsTab : UserControl
{
    public BasicControlsTab()
    {
        InitializeComponent();
    }

    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        StatusLabel.Content = $"Submitted: {NameInput.Text}";
        StatusProgressBar.Value = 100;
    }
}
