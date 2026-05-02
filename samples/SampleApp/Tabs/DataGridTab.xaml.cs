using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace SampleApp.Tabs;

public partial class DataGridTab : UserControl
{
    public DataGridTab()
    {
        InitializeComponent();
        MainDataGrid.ItemsSource = GenerateData();
    }

    private static ObservableCollection<DataItem> GenerateData()
    {
        var items = new ObservableCollection<DataItem>();
        for (int i = 1; i <= 100; i++)
        {
            items.Add(new DataItem
            {
                Id = i,
                Name = $"Item {i}",
                Description = $"This is the description for item number {i} in the data grid sample data.",
                Category = $"Category {(i % 10) + 1}",
                Price = i * 1.99m
            });
        }
        return items;
    }
}

public class DataItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
