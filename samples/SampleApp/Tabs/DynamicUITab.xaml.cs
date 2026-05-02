using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace SampleApp.Tabs;

public partial class DynamicUITab : UserControl
{
    private int _textBoxCounter;

    public DynamicUITab()
    {
        InitializeComponent();
        DynamicItemsControl.ItemsSource = new ObservableCollection<DynamicItem>();
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
    }
}

public class DynamicItem
{
    public string Text { get; set; } = string.Empty;
    public string AutomationId { get; set; } = string.Empty;
    public string AutomationName { get; set; } = string.Empty;
}
