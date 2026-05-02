using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32;

namespace SampleApp.Tabs;

public partial class DialogsTab : UserControl
{
    public DialogsTab()
    {
        InitializeComponent();
    }

    private void ShowInfo_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("This is an information message.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowWarning_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("This is a warning message.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void ShowError_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("This is an error message.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ShowConfirmation_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Do you want to proceed?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        MessageBox.Show($"You selected: {result}", "Result");
    }

    private void OpenCustomDialog_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CustomDialogWindow();
        dialog.ShowDialog();
    }

    private void OpenFileDialog_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open File",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };
        dialog.ShowDialog();
    }

    private void SaveFileDialog_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save File",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };
        dialog.ShowDialog();
    }
}
