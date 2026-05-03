using System.Windows;
using System.Windows.Controls;

namespace SampleApp.Tabs;

public partial class BasicControlsTab : UserControl
{
    public BasicControlsTab()
    {
        InitializeComponent();
        ValueSliderValueLabel.Text = $"Current Value Slider value: {ValueSlider.Value:0}";
        ValueSlider.ValueChanged += (_, _) => ValueSliderValueLabel.Text = $"Current Value Slider value: {ValueSlider.Value:0}";
    }

    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        StatusLabel.Content = $"Submitted: {NameInput.Text}";
        StatusProgressBar.Value = 100;
    }

    private void IndeterminateProgressToggle_Changed(object sender, RoutedEventArgs e)
    {
        StatusProgressBar.IsIndeterminate = IndeterminateProgressToggle.IsChecked == true;
        StatusLabel.Content = StatusProgressBar.IsIndeterminate
            ? "ProgressBar is indeterminate"
            : $"ProgressBar value: {StatusProgressBar.Value:0}";
    }
}
