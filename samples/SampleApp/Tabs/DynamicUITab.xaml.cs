using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace SampleApp.Tabs;

public partial class DynamicUITab : UserControl
{
    private int _textBoxCounter;
    private int _mixedCounter;

    public DynamicUITab()
    {
        InitializeComponent();
        DynamicItemsControl.ItemsSource = new ObservableCollection<DynamicItem>();
        DynamicPanelComboBox.Items.Add("Dynamic Option A");
        DynamicPanelComboBox.Items.Add("Dynamic Option B");
        DynamicPanelComboBox.SelectedIndex = 0;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        _textBoxCounter++;
        var item = new DynamicItem
        {
            Text = $"Dynamic TextBox {_textBoxCounter}",
            AutomationId = $"Dynamic_TextBox_{_textBoxCounter}",
            AutomationName = $"Dynamic TextBox {_textBoxCounter}"
        };
        ((ObservableCollection<DynamicItem>)DynamicItemsControl.ItemsSource).Add(item);
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        var items = (ObservableCollection<DynamicItem>)DynamicItemsControl.ItemsSource;
        if (items.Count > 0)
        {
            items.RemoveAt(items.Count - 1);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        var items = (ObservableCollection<DynamicItem>)DynamicItemsControl.ItemsSource;
        items.Clear();
        _textBoxCounter = 0;
        _mixedCounter = 0;
        DynamicMixedControlsPanel.Children.Clear();
        NotificationTextBlock.Text = "Notification: dynamic text boxes cleared";
    }

    private void AddMixedButton_Click(object sender, RoutedEventArgs e)
    {
        _mixedCounter++;
        var suffix = _mixedCounter.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var button = new Button { Content = $"Dynamic Mixed Button {suffix}", Margin = new Thickness(0, 4, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        AutomationProperties.SetAutomationId(button, $"Dynamic_Button_Mixed_{suffix}");
        AutomationProperties.SetName(button, $"Dynamic Mixed Button {suffix}");

        var checkBox = new CheckBox { Content = $"Dynamic Mixed CheckBox {suffix}", Margin = new Thickness(0, 4, 0, 0) };
        AutomationProperties.SetAutomationId(checkBox, $"Dynamic_CheckBox_Mixed_{suffix}");
        AutomationProperties.SetName(checkBox, $"Dynamic Mixed CheckBox {suffix}");

        var comboBox = new ComboBox { Margin = new Thickness(0, 4, 0, 0), Width = 220, HorizontalAlignment = HorizontalAlignment.Left };
        comboBox.Items.Add($"Dynamic Mixed Option {suffix}-A");
        comboBox.Items.Add($"Dynamic Mixed Option {suffix}-B");
        comboBox.SelectedIndex = 0;
        AutomationProperties.SetAutomationId(comboBox, $"Dynamic_ComboBox_Mixed_{suffix}");
        AutomationProperties.SetName(comboBox, $"Dynamic Mixed ComboBox {suffix}");

        var listBox = new ListBox { Margin = new Thickness(0, 4, 0, 0), Height = 56, Width = 220, HorizontalAlignment = HorizontalAlignment.Left };
        listBox.Items.Add($"Dynamic Mixed List Item {suffix}-A");
        listBox.Items.Add($"Dynamic Mixed List Item {suffix}-B");
        AutomationProperties.SetAutomationId(listBox, $"Dynamic_ListBox_Mixed_{suffix}");
        AutomationProperties.SetName(listBox, $"Dynamic Mixed ListBox {suffix}");

        DynamicMixedControlsPanel.Children.Add(button);
        DynamicMixedControlsPanel.Children.Add(checkBox);
        DynamicMixedControlsPanel.Children.Add(comboBox);
        DynamicMixedControlsPanel.Children.Add(listBox);
        NotificationTextBlock.Text = $"Notification: added mixed Button/CheckBox/ComboBox/ListBox set {suffix}";
    }

    private void StateToggleButton_Checked(object sender, RoutedEventArgs e)
    {
        PatternPanel.IsEnabled = false;
        StateToggleButton.Content = "Enable Panel";
        NotificationTextBlock.Text = "Notification: pattern panel disabled";
    }

    private void StateToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        PatternPanel.IsEnabled = true;
        StateToggleButton.Content = "Disable Panel";
        NotificationTextBlock.Text = "Notification: pattern panel enabled";
    }

    private void VisibilityCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (PatternPanel is null || NotificationTextBlock is null)
        {
            return;
        }

        var isChecked = sender is CheckBox checkBox ? checkBox.IsChecked == true : VisibilityCheckBox.IsChecked == true;
        PatternPanel.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
        NotificationTextBlock.Text = PatternPanel.Visibility == Visibility.Visible
            ? "Notification: pattern panel visible"
            : "Notification: pattern panel hidden";
    }

    private void CreateVirtualizedListButton_Click(object sender, RoutedEventArgs e)
    {
        DynamicVirtualizedListBox.Items.Clear();
        for (int i = 1; i <= 750; i++)
        {
            DynamicVirtualizedListBox.Items.Add($"Dynamic Virtualized Item {i:000}");
        }

        NotificationTextBlock.Text = "Notification: generated 750 virtualized list items";
    }
}

public class DynamicItem
{
    public string Text { get; set; } = string.Empty;
    public string AutomationId { get; set; } = string.Empty;
    public string AutomationName { get; set; } = string.Empty;
}
