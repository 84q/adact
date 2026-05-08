using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace SampleApp.Tabs;

/// <summary>
/// Demonstrates data grid scenarios with large, wide, and virtualized data sets.
/// </summary>
public partial class DataGridTab : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridTab"/> class.
    /// </summary>
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

/// <summary>
/// Represents a row in the data grid sample.
/// </summary>
public class DataItem
{
    /// <summary>
    /// Gets or sets the item identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the item owner.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item region.
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notes column text.
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}
