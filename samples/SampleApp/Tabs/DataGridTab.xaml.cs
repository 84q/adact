using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace SampleApp.Tabs;

public partial class DataGridTab : UserControl
{
    public DataGridTab()
    {
        InitializeComponent();
        var categories = Enumerable.Range(1, 10).Select(i => $"Category {i}").ToArray();
        CategoryColumn.ItemsSource = categories;
        MainDataGrid.ItemsSource = GenerateData(100);
        WideDataGrid.ItemsSource = GenerateData(80);
        VirtualizedDataGrid.ItemsSource = GenerateData(1200);
    }

    private static ObservableCollection<DataItem> GenerateData(int count)
    {
        var items = new ObservableCollection<DataItem>();
        for (int i = 1; i <= count; i++)
        {
            items.Add(new DataItem
            {
                Id = i,
                Name = $"Item {i}",
                Description = $"This is the description for item number {i} in the data grid sample data.",
                Category = $"Category {(i % 10) + 1}",
                Price = i * 1.99m,
                IsActive = i % 3 != 0,
                Owner = $"Owner {(i % 7) + 1}",
                Region = $"Region {(i % 4) + 1}",
                Status = i % 2 == 0 ? "Open" : "Closed",
                Notes = $"Long note for item {i}; use this column to force horizontal scrolling and table header inspection."
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
    public bool IsActive { get; set; }
    public string Owner { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
